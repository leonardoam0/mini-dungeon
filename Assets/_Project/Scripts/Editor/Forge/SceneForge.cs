using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Ruinas.EditorTools
{
    /// <summary>Gera as cenas completas a partir dos layouts, com iluminação, pós-processamento, lógica e NavMesh.</summary>
    public static class SceneForge
    {
        public const string Dir = "Assets/_Project/Scenes";

        public static void BuildAll()
        {
            ConfigureNavAgent();
            var arenaLight = ArenaLighting();
            var missionLight = MissionLighting();
            var menuLight = MenuLighting();
            BuildBoot();
            BuildMenu(menuLight);
            BuildReference(arenaLight);
            BuildMission(missionLight);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{Dir}/Boot.unity", true),
                new EditorBuildSettingsScene($"{Dir}/Menu.unity", true),
                new EditorBuildSettingsScene($"{Dir}/ReferenceArena.unity", true),
                new EditorBuildSettingsScene($"{Dir}/Mission.unity", true),
            };
            AssetDatabase.SaveAssets();
        }

        static void ConfigureNavAgent()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var settings = so.FindProperty("m_Settings");
            if (settings == null || settings.arraySize == 0) return;
            var s = settings.GetArrayElementAtIndex(0);
            void F(string n, float v) { var p = s.FindPropertyRelative(n); if (p != null) p.floatValue = v; }
            F("agentRadius", 0.35f);
            F("agentHeight", 1.8f);
            F("agentClimb", 0.55f);
            F("agentSlope", 45f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ Perfis de iluminação

        static LightingProfile ArenaLighting()
        {
            var p = DataForge.Asset<LightingProfile>("Profiles/Iluminacao_Arena");
            // Comparação com o vídeo: piso quente e entorno azul-petróleo (G≈B), sem o verde dominante da versão anterior.
            // Luz principal quase neutra (o piso do vídeo é terracota viva mesmo longe do fogo); o azul-petróleo
            // fica no ambiente, na névoa e nas paredes.
            p.ambientSky = new Color(0.28f, 0.32f, 0.33f);
            p.ambientEquator = new Color(0.18f, 0.19f, 0.19f);
            p.ambientGround = new Color(0.09f, 0.08f, 0.07f);
            p.ambientIntensity = 1f;
            p.sunColor = new Color(0.93f, 0.9f, 0.86f);
            p.sunIntensity = 1f;
            p.sunEuler = new Vector3(58f, 140f, 0f);
            p.shadowStrength = 0.85f;
            p.fog = true;
            p.fogColor = new Color(0.05f, 0.19f, 0.19f);
            p.fogStart = 52f;
            p.fogEnd = 105f;
            p.cameraBackground = new Color(0.03f, 0.09f, 0.095f);
            p.heightFogColor = new Color(0.045f, 0.16f, 0.16f);
            p.saturation = 6f;
            // Capturas ficavam ~17% mais escuras e mais quentes que o vídeo no centro da arena.
            p.postExposure = 0.5f;
            p.whiteBalanceTemperature = -7f;
            // No vídeo o fundo (alto da tela, mais distante) é desfocado; o plano do jogador fica nítido.
            p.dofStart = 50f;
            p.dofEnd = 68f;
            p.dofMaxRadius = 1.1f;
            // Tonalização suave: realces laranja fortes deslocavam o ciano do contorno para amarelo-limão.
            p.splitShadows = new Color(0.42f, 0.55f, 0.57f);
            p.splitHighlights = new Color(0.66f, 0.57f, 0.49f);
            p.splitBalance = 0f;
            p.heightFogBelowPlayer = 1.6f;
            p.heightFogRange = 9f;
            p.heightFogMax = 0.65f;
            return p;
        }

        static LightingProfile MissionLighting()
        {
            var p = DataForge.Asset<LightingProfile>("Profiles/Iluminacao_Missao");
            var a = ArenaLighting();
            EditorUtility.CopySerialized(a, p);
            p.name = "Iluminacao_Missao";
            return p;
        }

        static LightingProfile MenuLighting()
        {
            var p = DataForge.Asset<LightingProfile>("Profiles/Iluminacao_Menu");
            var a = ArenaLighting();
            EditorUtility.CopySerialized(a, p);
            p.name = "Iluminacao_Menu";
            p.fogStart = 50f;
            p.fogEnd = 110f;
            p.vignette = 0.38f;
            return p;
        }

        static VolumeProfile Volume(string name, LightingProfile p)
        {
            string path = $"Assets/_Project/Settings/Volumes/{name}.asset";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(path) != null) AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            T Add<T>() where T : VolumeComponent
            {
                var c = profile.Add<T>(true);
                c.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(c, profile);
                return c;
            }
            var bloom = Add<Bloom>();
            bloom.threshold.Override(p.bloomThreshold);
            bloom.intensity.Override(p.bloomIntensity);
            bloom.scatter.Override(p.bloomScatter);
            bloom.highQualityFiltering.Override(true);
            var tone = Add<Tonemapping>();
            tone.mode.Override(TonemappingMode.Neutral);
            var ca = Add<ColorAdjustments>();
            ca.postExposure.Override(p.postExposure);
            ca.contrast.Override(p.contrast);
            ca.saturation.Override(p.saturation);
            if (p.dofEnd > p.dofStart && p.dofStart > 0f)
            {
                var dof = Add<DepthOfField>();
                dof.mode.Override(DepthOfFieldMode.Gaussian);
                dof.gaussianStart.Override(p.dofStart);
                dof.gaussianEnd.Override(p.dofEnd);
                dof.gaussianMaxRadius.Override(p.dofMaxRadius);
                dof.highQualitySampling.Override(true);
            }
            var wb = Add<WhiteBalance>();
            wb.temperature.Override(p.whiteBalanceTemperature);
            var st = Add<SplitToning>();
            st.shadows.Override(p.splitShadows);
            st.highlights.Override(p.splitHighlights);
            st.balance.Override(p.splitBalance);
            var vig = Add<Vignette>();
            vig.intensity.Override(p.vignette);
            vig.smoothness.Override(0.45f);
            vig.color.Override(Color.black);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        // ------------------------------------------------------------------ Blocos comuns

        static Scene NewScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        static Light Sun(LightingProfile p)
        {
            var go = new GameObject("Lua");
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = p.sunColor;
            l.intensity = p.sunIntensity;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = p.shadowStrength;
            l.shadowBias = 0.05f;
            l.shadowNormalBias = 0.35f;
            go.transform.rotation = Quaternion.Euler(p.sunEuler);
            return l;
        }

        static Camera MakeCamera(Transform parent, LightingProfile p)
        {
            var go = new GameObject("Camera");
            go.tag = "MainCamera";
            if (parent != null) go.transform.SetParent(parent, false);
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = p.cameraBackground;
            cam.fieldOfView = 28f;
            cam.nearClipPlane = 8f;
            cam.farClipPlane = 160f;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.renderShadows = true;
            data.requiresDepthOption = CameraOverrideOption.On;
            go.AddComponent<AudioListener>();
            return cam;
        }

        static Volume GlobalVolume(VolumeProfile profile)
        {
            var go = new GameObject("VolumeGlobal");
            var v = go.AddComponent<Volume>();
            v.isGlobal = true;
            v.priority = 1f;
            v.sharedProfile = profile;
            return v;
        }

        static void ApplyRenderSettings(LightingProfile p)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = p.ambientSky;
            RenderSettings.ambientEquatorColor = p.ambientEquator;
            RenderSettings.ambientGroundColor = p.ambientGround;
            RenderSettings.fog = p.fog;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = p.fogColor;
            RenderSettings.fogStartDistance = p.fogStart;
            RenderSettings.fogEndDistance = p.fogEnd;
            RenderSettings.skybox = null;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        }

        static LevelContext Context(LevelKind kind, LightingProfile light, Light sun, Volume volume, MapData map, CameraRig rig, Transform spawn)
        {
            var go = new GameObject("NivelContexto");
            var ctx = go.AddComponent<LevelContext>();
            ctx.kind = kind;
            ctx.lighting = light;
            ctx.sun = sun;
            ctx.postVolume = volume;
            ctx.map = map;
            ctx.cameraRig = rig;
            ctx.defaultSpawn = spawn;
            var atm = go.AddComponent<AtmosphereController>();
            atm.profile = light;
            atm.sun = sun;
            atm.volume = volume;
            return ctx;
        }

        static CameraRig Rig(LightingProfile p)
        {
            var go = new GameObject("CameraRig");
            var rig = go.AddComponent<CameraRig>();
            rig.profile = DataForge.Cache.db.gameplayCamera;
            rig.cam = MakeCamera(go.transform, p);
            return rig;
        }

        static void Save(Scene scene, string name)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, $"{Dir}/{name}.unity");
        }

        // ------------------------------------------------------------------ Cenas

        static void BuildBoot()
        {
            var scene = NewScene();
            var cam = new GameObject("Camera").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.06f, 0.055f);
            cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            new GameObject("Boot").AddComponent<BootSceneController>();
            Directory.CreateDirectory(Dir);
            Save(scene, "Boot");
        }

        static void BuildMenu(LightingProfile p)
        {
            var scene = NewScene();
            ApplyRenderSettings(p);
            var sun = Sun(p);
            var volume = GlobalVolume(Volume("Volume_Menu", p));
            var b = LevelLayouts.Menu();
            var res = LevelAssembler.Assemble(b, "Menu");
            var pivot = new GameObject("PivoCamera").transform;
            pivot.position = b.Anchors["menu"];
            pivot.rotation = Quaternion.Euler(0f, 45f, 0f);
            var cam = MakeCamera(pivot, p);
            cam.transform.localRotation = Quaternion.Euler(38f, 0f, 0f);
            cam.transform.localPosition = Quaternion.Euler(38f, 0f, 0f) * new Vector3(0f, 0f, -44f) + new Vector3(0f, 1f, 0f);
            cam.fieldOfView = 30f;
            var ctx = Context(LevelKind.Menu, p, sun, volume, res.map, null, null);
            if (res.obelisk != null) res.obelisk.beamInterval = 9f;
            var menu = new GameObject("MenuPrincipal").AddComponent<MainMenuController>();
            menu.cameraPivot = pivot;
            menu.orbitSpeed = 3f;
            // Perímetro aceso no diorama.
            var glowGo = new GameObject("Perimetro");
            var glow = glowGo.AddComponent<PerimeterGlow>();
            glow.strips = res.strips.ToArray();
            glow.activeIntensity = 3.2f;
            glow.dormantIntensity = 2.6f;
            Save(scene, "Menu");
        }

        static ArenaDirector MakeArena(LevelAssembler.Result res, string key, EncounterDefinition def, bool withObelisk)
        {
            var go = new GameObject("Encontro_" + key);
            var ad = go.AddComponent<ArenaDirector>();
            ad.definition = def;
            ad.encounterId = def.id;
            var spawns = new List<SpawnPoint>();
            foreach (var kv in res.spawns) if (kv.Key.StartsWith(key + ":")) spawns.AddRange(kv.Value);
            ad.spawnPoints = spawns.ToArray();
            if (res.zones.TryGetValue(key + "_ativacao", out var zone))
            {
                ad.activationZone = zone;
                zone.encounter = ad;
            }
            if (res.anchors.TryGetValue(key, out var anchor)) go.transform.position = anchor.position;
            else if (spawns.Count > 0) go.transform.position = spawns[0].transform.position;
            if (withObelisk)
            {
                ad.obelisk = res.obelisk;
                var glowGo = new GameObject("Perimetro");
                glowGo.transform.SetParent(go.transform, false);
                var glow = glowGo.AddComponent<PerimeterGlow>();
                glow.strips = res.strips.ToArray();
                glow.lights = res.stripLights.ToArray();
                ad.perimeter = glow;
                ad.arenaRadius = 9f;
            }
            return ad;
        }

        static void BuildReference(LightingProfile p)
        {
            var scene = NewScene();
            ApplyRenderSettings(p);
            var sun = Sun(p);
            var volume = GlobalVolume(Volume("Volume_Arena", p));
            var b = LevelLayouts.Reference();
            var res = LevelAssembler.Assemble(b, "ReferenceArena");
            var rig = Rig(p);
            var anchor = res.anchors["arena"];
            var spawn = new GameObject("InicioJogador").transform;
            spawn.position = anchor.position + new Vector3(-1.2f, 0f, -0.8f);
            var ctx = Context(LevelKind.Reference, p, sun, volume, res.map, rig, spawn);
            var cache = DataForge.Cache;
            var arena = MakeArena(res, "arena", cache.encRef, true);
            if (res.gates.TryGetValue("arena_portao", out var gate))
            {
                gate.startsOpen = false;
                arena.exitGates = new[] { gate };
            }
            var refGo = new GameObject("Referencia");
            var rd = refGo.AddComponent<ReferenceDirector>();
            rd.arena = arena;
            rd.anchor = anchor;
            rd.script = cache.db.referenceScript;
            VfxForge.AmbientMotes(res.root.transform, anchor.position + new Vector3(0f, 3f, 6f), new Vector3(30f, 6f, 16f), new Color(0.7f, 1f, 0.5f), 5f);
            LevelAssembler.BakeNavMesh(res, "ReferenceArena");
            Save(scene, "ReferenceArena");
        }

        static void BuildMission(LightingProfile p)
        {
            var scene = NewScene();
            ApplyRenderSettings(p);
            var sun = Sun(p);
            var volume = GlobalVolume(Volume("Volume_Missao", p));
            var b = LevelLayouts.Mission();
            var res = LevelAssembler.Assemble(b, "Mission");
            var rig = Rig(p);
            var ctx = Context(LevelKind.Mission, p, sun, volume, res.map, rig, res.spawnDefault);
            var cache = DataForge.Cache;

            GateController G(string id) => res.gates.TryGetValue(id, out var g) ? g : null;
            Chest Ch(string id) => res.chests.TryGetValue(id, out var c) ? c : null;

            var corredor = MakeArena(res, "corredor", cache.encCorredor, false);
            var sala = MakeArena(res, "sala", cache.encSala, false);
            sala.lockGates = new[] { G("sala_entrada"), G("sala_leste"), G("sala_norte") };
            sala.rewardChest = Ch("bau_sala");
            var desvio = MakeArena(res, "desvio", cache.encDesvio, false);
            var arena = MakeArena(res, "arena", cache.encArena, true);
            var arenaGate = G("arena_portao");
            if (arenaGate != null) arenaGate.startsOpen = false;
            arena.exitGates = new[] { arenaGate };
            arena.rewardChest = Ch("bau_arena");
            var salao = MakeArena(res, "salao", cache.encFinal, false);
            salao.lockGates = new[] { G("salao_entrada") };
            var saida = G("salao_saida");
            if (saida != null) saida.startsOpen = false;
            salao.exitGates = new[] { saida };
            salao.rewardChest = Ch("bau_final");

            var md = new GameObject("Missao").AddComponent<MissionDirector>();
            md.encounters = new[] { corredor, sala, desvio, arena, salao };
            md.chests = new List<Chest>(res.chests.Values).ToArray();
            md.staticPickups = res.pickups.ToArray();
            md.exit = res.exit;
            md.finalEncounter = salao;
            md.startCheckpointId = "cp_acampamento";
            md.objectives = new[]
            {
                new ObjectiveStep { id = "obj_corredor", text = "Siga pelo corredor iluminado e derrote os guardas", encounterId = "enc_corredor" },
                new ObjectiveStep { id = "obj_sala", text = "Limpe a sala das colunas", encounterId = "enc_sala" },
                new ObjectiveStep { id = "obj_desvio", text = "Opcional: explore o santuário ao norte da sala", encounterId = "enc_desvio", optional = true },
                new ObjectiveStep { id = "obj_arena", text = "Suba até a arena e ative o obelisco", encounterId = "enc_arena" },
                new ObjectiveStep { id = "obj_salao", text = "Enfrente o Guardião Musgoso no salão ao norte", encounterId = "enc_final" },
                new ObjectiveStep { id = "obj_saida", text = "Abra o cofre e saia pelo portal" },
            };

            // Rota do piloto automático.
            var routeGo = new GameObject("RotaAutomatica");
            var route = routeGo.AddComponent<AutoPilotRoute>();
            var steps = new List<RouteStep>();
            var encByKey = new Dictionary<string, ArenaDirector> { ["corredor"] = corredor, ["sala"] = sala, ["desvio"] = desvio, ["arena"] = arena, ["salao"] = salao };
            foreach (var r in b.Route)
            {
                var pt = new GameObject("Ponto_" + r.label).transform;
                pt.SetParent(routeGo.transform, false);
                pt.position = r.pos;
                var step = new RouteStep { label = r.label, point = pt, action = r.action };
                if (r.action == RouteAction.OpenChest) step.chest = Ch(r.refId);
                if (r.action == RouteAction.ClearEncounter && r.refId != null && encByKey.TryGetValue(r.refId, out var enc)) step.encounter = enc;
                steps.Add(step);
            }
            route.steps = steps.ToArray();

            VfxForge.AmbientMotes(res.root.transform, new Vector3(52f, 9f, 36f), new Vector3(100f, 8f, 70f), new Color(0.75f, 1f, 0.5f), 18f);
            LevelAssembler.BakeNavMesh(res, "Mission");
            Save(scene, "Mission");
        }
    }
}

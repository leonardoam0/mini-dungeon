using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Ruinas.EditorTools
{
    /// <summary>Adereços do cenário montados por caixas (cor por vértice ou tiles do atlas), salvos como prefabs.</summary>
    public static class PropForge
    {
        public const string Dir = "Assets/_Project/Prefabs/Props";
        static readonly Rect WhiteUV = new Rect(0.5f, 0.5f, 0f, 0f);

        public static GameObject Obelisk, Brazier, Lantern, Gate, Chest, RedFlower, Reeds, GrassTuft, Fern, Checkpoint, Merchant, Exit, Tent, Campfire, Crate, Pot, Banner;

        static Color32 C(string hex) => (Color32)Pal.Hex(hex);

        static MeshRenderer Mesh(Transform parent, string name, Mesh mesh, Material mat, bool shadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return mr;
        }

        static GameObject SavePrefab(GameObject go, string name)
        {
            Directory.CreateDirectory(Dir);
            var p = PrefabUtility.SaveAsPrefabAsset(go, $"{Dir}/{name}.prefab");
            Object.DestroyImmediate(go);
            return p;
        }

        static void Solid(GameObject go, Vector3 center, Vector3 size, int layer = Layers.Environment)
        {
            var bc = go.AddComponent<BoxCollider>();
            bc.center = center;
            bc.size = size;
            go.layer = layer;
        }

        static Light PointLight(Transform parent, Vector3 pos, Color c, float range, float intensity, bool flicker = false)
        {
            var go = new GameObject("Luz");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            if (flicker) go.AddComponent<LightFlicker>().baseIntensity = intensity;
            return l;
        }

        public static void BuildAll()
        {
            var prop = MaterialForge.Get("M_Prop");
            var tex = MaterialForge.Get("M_PropTextured");
            var glowWhite = MaterialForge.Get("M_GlowWhite");
            var glowEyes = MaterialForge.Get("M_GlowEyes");
            var glowGreen = MaterialForge.Get("M_GlowGreen");
            var glowFire = MaterialForge.Get("M_GlowFire");

            // ---------------------------------------------------------------- Obelisco
            {
                var root = new GameObject("Obelisco");
                var ctrl = root.AddComponent<ObeliskController>();
                var b = new MeshForge.Builder();
                var white = new Color32(255, 255, 255, 255);
                // ESTIMADO no vídeo: ~1,2 u de largura e ~5,1 u até a face superior luminosa.
                b.TiledBox(new Vector3(-0.78f, 0f, -0.78f), new Vector3(1.56f, 0.3f, 1.56f), Tile.ObsidianTop, Tile.ObsidianSide, white);
                // Corpo alto de tijolos avermelhados com uma cinta escura no meio.
                b.TiledBox(new Vector3(-0.6f, 0.3f, -0.6f), new Vector3(1.2f, 1.75f, 1.2f), Tile.RedBrickTop, Tile.RedBrickSide, white);
                b.TiledBox(new Vector3(-0.63f, 2.05f, -0.63f), new Vector3(1.26f, 0.12f, 1.26f), Tile.ObsidianTop, Tile.ObsidianSide, white);
                b.TiledBox(new Vector3(-0.6f, 2.17f, -0.6f), new Vector3(1.2f, 1.73f, 1.2f), Tile.RedBrickTop, Tile.RedBrickSide, white);
                // Faixa escura do "rosto" com os sinais laterais.
                b.TiledBox(new Vector3(-0.62f, 3.9f, -0.62f), new Vector3(1.24f, 0.62f, 1.24f), Tile.ObsidianTop, Tile.ObsidianSide, new Color32(210, 200, 220, 255));
                b.TiledBox(new Vector3(-0.65f, 4.52f, -0.65f), new Vector3(1.3f, 0.06f, 1.3f), Tile.ObsidianTop, Tile.ObsidianSide, white);
                Mesh(root.transform, "Corpo", MeshForge.Save(b.ToMesh("obelisco_corpo"), "Props/obelisco_corpo"), tex);
                // Bloco luminoso do topo (faces laterais também brilham, como no vídeo).
                var top = new MeshForge.Builder();
                top.Box(new Vector3(-0.56f, 4.58f, -0.56f), new Vector3(1.12f, 0.52f, 1.12f), white, WhiteUV);
                var topR = Mesh(root.transform, "Topo", MeshForge.Save(top.ToMesh("obelisco_topo"), "Props/obelisco_topo"), glowWhite, false);
                var eyes = new MeshForge.Builder();
                // Dois sinais em fenda por face (em relevo mínimo para não "flutuar").
                for (int f = 0; f < 4; f++)
                {
                    var rot = Quaternion.Euler(0f, f * 90f, 0f);
                    foreach (float x in new[] { -0.3f, 0.3f })
                    {
                        Vector3 center = rot * new Vector3(x, 4.23f, 0.63f);
                        Vector3 size = rot * new Vector3(0.26f, 0.08f, 0.03f);
                        size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
                        eyes.Box(center - size * 0.5f, size, white, WhiteUV);
                        Vector3 c2 = rot * new Vector3(x + (x < 0 ? 0.09f : -0.09f), 4.16f, 0.63f);
                        Vector3 s2 = rot * new Vector3(0.08f, 0.07f, 0.03f);
                        s2 = new Vector3(Mathf.Abs(s2.x), Mathf.Abs(s2.y), Mathf.Abs(s2.z));
                        eyes.Box(c2 - s2 * 0.5f, s2, white, WhiteUV);
                    }
                }
                var eyesR = Mesh(root.transform, "Sinais", MeshForge.Save(eyes.ToMesh("obelisco_sinais"), "Props/obelisco_sinais"), glowEyes, false);
                ctrl.top = topR;
                ctrl.eyes = new Renderer[] { eyesR };
                ctrl.topLight = PointLight(root.transform, new Vector3(0f, 5.6f, 0f), new Color(0.85f, 1f, 0.97f), 9f, 3f);
                ctrl.topLight.shadows = LightShadows.None;
                var motes = VfxForge.AmbientMotes(root.transform, new Vector3(0f, 5.5f, 0f), new Vector3(0.9f, 0.5f, 0.9f), new Color(0.85f, 1f, 1f), 6f);
                var mps = motes.GetComponentInChildren<ParticleSystem>();
                var mm = mps.main; mm.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.7f); mm.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.8f);
                var vel = mps.velocityOverLifetime; vel.enabled = true; vel.y = new ParticleSystem.MinMaxCurve(0.6f);
                ctrl.motes = mps;
                var sparks = VfxForge.AmbientMotes(root.transform, new Vector3(0f, 5.3f, 0f), new Vector3(2.2f, 0.4f, 2.2f), new Color(0.6f, 1f, 0.95f), 55f);
                var sps = sparks.GetComponentInChildren<ParticleSystem>();
                var sm = sps.main; sm.playOnAwake = false; sm.prewarm = false; sm.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
                var sv = sps.velocityOverLifetime; sv.enabled = true; sv.y = new ParticleSystem.MinMaxCurve(2.2f);
                ctrl.risingSparks = sps;
                Solid(root, new Vector3(0f, 2.55f, 0f), new Vector3(1.3f, 5.1f, 1.3f));
                Obelisk = SavePrefab(root, "Obelisco");
            }

            // ---------------------------------------------------------------- Braseiro de latão
            {
                var root = new GameObject("Braseiro");
                var b = new MeshForge.Builder();
                var gold = C("#caa13e");
                var goldDark = C("#8a6624");
                b.Box(new Vector3(-0.5f, 0f, -0.5f), new Vector3(1f, 0.08f, 1f), goldDark, WhiteUV);
                b.Box(new Vector3(-0.5f, 0.08f, -0.5f), new Vector3(1f, 0.26f, 0.12f), gold, WhiteUV);
                b.Box(new Vector3(-0.5f, 0.08f, 0.38f), new Vector3(1f, 0.26f, 0.12f), gold, WhiteUV);
                b.Box(new Vector3(-0.5f, 0.08f, -0.38f), new Vector3(0.12f, 0.26f, 0.76f), gold, WhiteUV);
                b.Box(new Vector3(0.38f, 0.08f, -0.38f), new Vector3(0.12f, 0.26f, 0.76f), gold, WhiteUV);
                b.Box(new Vector3(-0.54f, 0.3f, -0.54f), new Vector3(0.14f, 0.1f, 0.14f), C("#efd070"), WhiteUV);
                b.Box(new Vector3(0.4f, 0.3f, -0.54f), new Vector3(0.14f, 0.1f, 0.14f), C("#efd070"), WhiteUV);
                b.Box(new Vector3(-0.54f, 0.3f, 0.4f), new Vector3(0.14f, 0.1f, 0.14f), C("#efd070"), WhiteUV);
                b.Box(new Vector3(0.4f, 0.3f, 0.4f), new Vector3(0.14f, 0.1f, 0.14f), C("#efd070"), WhiteUV);
                Mesh(root.transform, "Bacia", MeshForge.Save(b.ToMesh("braseiro"), "Props/braseiro"), prop);
                var coals = new MeshForge.Builder();
                var rnd = new System.Random(3);
                for (int i = 0; i < 7; i++)
                {
                    float x = (float)rnd.NextDouble() * 0.56f - 0.3f, z = (float)rnd.NextDouble() * 0.56f - 0.3f;
                    coals.Box(new Vector3(x, 0.08f, z), new Vector3(0.14f, 0.1f, 0.14f), new Color32(255, 255, 255, 255), WhiteUV);
                }
                Mesh(root.transform, "Brasas", MeshForge.Save(coals.ToMesh("brasas"), "Props/brasas"), glowFire, false);
                VfxForge.TorchFire(root.transform, new Vector3(0f, 0.2f, 0f), 1f, false);
                Brazier = SavePrefab(root, "Braseiro");
            }

            // ---------------------------------------------------------------- Lanterna verde (topo de pilares)
            {
                var root = new GameObject("LanternaVerde");
                var b = new MeshForge.Builder();
                b.Box(new Vector3(-0.42f, 0f, -0.42f), new Vector3(0.84f, 0.84f, 0.84f), new Color32(255, 255, 255, 255), WhiteUV);
                Mesh(root.transform, "Nucleo", MeshForge.Save(b.ToMesh("lanterna"), "Props/lanterna"), glowGreen, false);
                var holes = new MeshForge.Builder();
                var dark = C("#132016");
                for (int f = 0; f < 4; f++)
                {
                    var rot = Quaternion.Euler(0f, f * 90f, 0f);
                    foreach (var p in new[] { new Vector2(-0.2f, 0.5f), new Vector2(0.2f, 0.5f), new Vector2(0f, 0.28f) })
                    {
                        Vector3 center = rot * new Vector3(p.x, p.y, 0.43f);
                        Vector3 size = rot * new Vector3(0.16f, 0.12f, 0.03f);
                        size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
                        holes.Box(center - size * 0.5f, size, dark, WhiteUV);
                    }
                }
                holes.Box(new Vector3(-0.46f, 0.84f, -0.46f), new Vector3(0.92f, 0.08f, 0.92f), C("#2d4a44"), WhiteUV);
                Mesh(root.transform, "Grade", MeshForge.Save(holes.ToMesh("lanterna_grade"), "Props/lanterna_grade"), prop);
                PointLight(root.transform, new Vector3(0f, 0.5f, 0f), new Color(0.4f, 1f, 0.45f), 5f, 1.1f, true);
                Lantern = SavePrefab(root, "LanternaVerde");
            }

            // ---------------------------------------------------------------- Portão de barras com campo violeta
            {
                var root = new GameObject("Portao");
                var gate = root.AddComponent<GateController>();
                var frame = new MeshForge.Builder();
                frame.TiledBox(new Vector3(-1.9f, 0f, -0.25f), new Vector3(0.5f, 3.6f, 0.5f), Tile.BambooTop, Tile.BambooSide, new Color32(255, 255, 255, 255));
                frame.TiledBox(new Vector3(1.4f, 0f, -0.25f), new Vector3(0.5f, 3.6f, 0.5f), Tile.BambooTop, Tile.BambooSide, new Color32(255, 255, 255, 255));
                frame.TiledBox(new Vector3(-1.9f, 3.35f, -0.3f), new Vector3(3.8f, 0.4f, 0.6f), Tile.WoodTop, Tile.WoodSide, new Color32(255, 255, 255, 255));
                Mesh(root.transform, "Moldura", MeshForge.Save(frame.ToMesh("portao_moldura"), "Props/portao_moldura"), tex);
                var bars = new MeshForge.Builder();
                for (int i = 0; i < 7; i++)
                {
                    float x = -1.2f + i * 0.4f;
                    bars.Box(new Vector3(x - 0.08f, 0f, -0.08f), new Vector3(0.16f, 3.3f, 0.16f), i % 2 == 0 ? C("#a3b04a") : C("#8a9a3a"), WhiteUV);
                    bars.Box(new Vector3(x - 0.1f, 1.2f + (i % 3) * 0.5f, -0.1f), new Vector3(0.2f, 0.06f, 0.2f), C("#5a6a26"), WhiteUV);
                }
                bars.Box(new Vector3(-1.4f, 2.4f, -0.06f), new Vector3(2.8f, 0.12f, 0.12f), C("#6a4a2c"), WhiteUV);
                bars.Box(new Vector3(-1.4f, 0.9f, -0.06f), new Vector3(2.8f, 0.12f, 0.12f), C("#6a4a2c"), WhiteUV);
                var barsR = Mesh(root.transform, "Barras", MeshForge.Save(bars.ToMesh("portao_barras"), "Props/portao_barras"), prop);
                var fieldGo = new GameObject("Campo");
                fieldGo.transform.SetParent(root.transform, false);
                fieldGo.transform.localPosition = new Vector3(0f, 0f, 0f);
                fieldGo.transform.localScale = new Vector3(2.9f, 2.4f, 1f);
                fieldGo.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{MeshForge.Dir}/VFX/beam.asset");
                var fmr = fieldGo.AddComponent<MeshRenderer>();
                fmr.sharedMaterial = MaterialForge.Get("M_GateField");
                fmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                gate.bars = barsR.transform;
                gate.energyField = fmr;
                gate.energyLight = PointLight(root.transform, new Vector3(0f, 1f, 0.6f), new Color(0.75f, 0.35f, 1f), 6f, 2.2f);
                var blockerGo = new GameObject("Bloqueio");
                blockerGo.transform.SetParent(root.transform, false);
                blockerGo.layer = Layers.InvisibleWall;
                var bc = blockerGo.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 1.6f, 0f);
                bc.size = new Vector3(2.9f, 3.2f, 0.5f);
                gate.blocker = bc;
                var obs = root.AddComponent<NavMeshObstacle>();
                obs.shape = NavMeshObstacleShape.Box;
                obs.center = new Vector3(0f, 1.5f, 0f);
                obs.size = new Vector3(2.9f, 3f, 0.6f);
                obs.carving = true;
                obs.carveOnlyStationary = false;
                gate.obstacle = obs;
                gate.openDepth = 3.3f;
                var posts = new GameObject("Postes");
                posts.transform.SetParent(root.transform, false);
                posts.layer = Layers.Environment;
                var pc1 = posts.AddComponent<BoxCollider>(); pc1.center = new Vector3(-1.65f, 1.8f, 0f); pc1.size = new Vector3(0.5f, 3.6f, 0.5f);
                var pc2 = posts.AddComponent<BoxCollider>(); pc2.center = new Vector3(1.65f, 1.8f, 0f); pc2.size = new Vector3(0.5f, 3.6f, 0.5f);
                root.AddComponent<MapPoi>().kind = MapPoiKind.Gate;
                Gate = SavePrefab(root, "Portao");
            }

            // ---------------------------------------------------------------- Baú
            {
                var root = new GameObject("Bau");
                var chest = root.AddComponent<Chest>();
                chest.radius = 1.9f;
                var b = new MeshForge.Builder();
                var wood = C("#7a5230");
                var woodDark = C("#5a3a20");
                var band = C("#d6a93c");
                b.Box(new Vector3(-0.45f, 0f, -0.32f), new Vector3(0.9f, 0.52f, 0.64f), wood, WhiteUV);
                b.Box(new Vector3(-0.46f, 0f, -0.33f), new Vector3(0.1f, 0.53f, 0.66f), band, WhiteUV);
                b.Box(new Vector3(0.36f, 0f, -0.33f), new Vector3(0.1f, 0.53f, 0.66f), band, WhiteUV);
                b.Box(new Vector3(-0.45f, 0.44f, -0.33f), new Vector3(0.9f, 0.06f, 0.66f), woodDark, WhiteUV);
                Mesh(root.transform, "Base", MeshForge.Save(b.ToMesh("bau_base"), "Props/bau_base"), prop);
                var lidPivot = new GameObject("Tampa");
                lidPivot.transform.SetParent(root.transform, false);
                lidPivot.transform.localPosition = new Vector3(0f, 0.52f, -0.32f);
                var l = new MeshForge.Builder();
                l.Box(new Vector3(-0.45f, 0f, 0f), new Vector3(0.9f, 0.28f, 0.64f), wood, WhiteUV);
                l.Box(new Vector3(-0.46f, 0f, -0.01f), new Vector3(0.1f, 0.29f, 0.66f), band, WhiteUV);
                l.Box(new Vector3(0.36f, 0f, -0.01f), new Vector3(0.1f, 0.29f, 0.66f), band, WhiteUV);
                l.Box(new Vector3(-0.08f, -0.14f, 0.63f), new Vector3(0.16f, 0.2f, 0.05f), C("#efd070"), WhiteUV);
                Mesh(lidPivot.transform, "TampaMalha", MeshForge.Save(l.ToMesh("bau_tampa"), "Props/bau_tampa"), prop);
                chest.lid = lidPivot.transform;
                chest.glow = PointLight(root.transform, new Vector3(0f, 0.9f, 0.4f), new Color(1f, 0.82f, 0.45f), 2.6f, 1.2f);
                Solid(root, new Vector3(0f, 0.4f, 0f), new Vector3(0.9f, 0.8f, 0.64f));
                root.AddComponent<MapPoi>().kind = MapPoiKind.Chest;
                Chest = SavePrefab(root, "Bau");
            }

            // ---------------------------------------------------------------- Flor vermelha (topo do pilar)
            {
                var root = new GameObject("FlorVermelha");
                var b = new MeshForge.Builder();
                // No vídeo: copa volumosa de folhas largas e pontudas, vermelho saturado com sombras vinho,
                // subindo em leque sobre o pilar (não uma flor rasa).
                var red = C("#d0161c");
                var redLight = C("#f4362e");
                var redDark = C("#7a0a0e");
                var rnd = new System.Random(9);
                void Leaf(float ang, float tilt, float len, float width, float y0)
                {
                    var rot = Quaternion.Euler(0f, ang, 0f) * Quaternion.Euler(-tilt, 0f, 0f);
                    int segs = 3;
                    for (int s = 0; s < segs; s++)
                    {
                        float u = s / (float)segs;
                        float w = Mathf.Lerp(width, width * 0.3f, u);
                        Vector3 p = rot * new Vector3(0f, 0f, 0.12f + (s + 0.5f) * len / segs);
                        AddRotBox(b, p + Vector3.up * y0, new Vector3(w, 0.1f, len / segs + 0.04f), rot, s == 0 ? redDark : (s == segs - 1 ? redLight : red));
                    }
                }
                // Copa cheia: anel externo baixo, anel médio e miolo alto, folhas largas que se sobrepõem.
                for (int i = 0; i < 10; i++) Leaf(i * 36f + (float)rnd.NextDouble() * 12f, 8f + (float)rnd.NextDouble() * 12f, 0.85f, 0.62f, 0.16f);
                for (int i = 0; i < 8; i++) Leaf(i * 45f + 20f + (float)rnd.NextDouble() * 10f, 32f + (float)rnd.NextDouble() * 14f, 0.75f, 0.58f, 0.3f);
                for (int i = 0; i < 5; i++) Leaf(i * 72f + 40f, 58f + (float)rnd.NextDouble() * 12f, 0.6f, 0.5f, 0.44f);
                b.Box(new Vector3(-0.3f, 0f, -0.3f), new Vector3(0.6f, 0.34f, 0.6f), C("#5a0e0c"), WhiteUV);
                b.Box(new Vector3(-0.12f, 0.46f, -0.12f), new Vector3(0.24f, 0.14f, 0.24f), C("#f0a030"), WhiteUV);
                Mesh(root.transform, "Flor", MeshForge.Save(b.ToMesh("flor_vermelha"), "Props/flor_vermelha"), prop);
                RedFlower = SavePrefab(root, "FlorVermelha");
            }

            // ---------------------------------------------------------------- Juncos amarelados, tufos, samambaias
            {
                var root = new GameObject("Juncos");
                var b = new MeshForge.Builder();
                var rnd = new System.Random(11);
                for (int i = 0; i < 11; i++)
                {
                    float x = (float)rnd.NextDouble() * 0.8f - 0.4f, z = (float)rnd.NextDouble() * 0.8f - 0.4f;
                    float h = 0.5f + (float)rnd.NextDouble() * 0.7f;
                    var col = Color32.Lerp(C("#d8b64a"), C("#9a8a2e"), (float)rnd.NextDouble());
                    var rot = Quaternion.Euler((float)rnd.NextDouble() * 16f - 8f, 0f, (float)rnd.NextDouble() * 16f - 8f);
                    AddRotBox(b, new Vector3(x, h * 0.5f, z), new Vector3(0.06f, h, 0.06f), rot, col);
                    AddRotBox(b, new Vector3(x, h + 0.05f, z), new Vector3(0.1f, 0.12f, 0.1f), rot, C("#e8d070"));
                }
                Mesh(root.transform, "Juncos", MeshForge.Save(b.ToMesh("juncos"), "Props/juncos"), prop);
                Reeds = SavePrefab(root, "Juncos");
            }
            {
                var root = new GameObject("Tufo");
                var b = new MeshForge.Builder();
                var rnd = new System.Random(12);
                for (int i = 0; i < 9; i++)
                {
                    float x = (float)rnd.NextDouble() * 0.7f - 0.35f, z = (float)rnd.NextDouble() * 0.7f - 0.35f;
                    float h = 0.18f + (float)rnd.NextDouble() * 0.3f;
                    var col = Color32.Lerp(C("#3e6a35"), C("#5a8a44"), (float)rnd.NextDouble());
                    b.Box(new Vector3(x - 0.05f, 0f, z - 0.05f), new Vector3(0.1f, h, 0.1f), col, WhiteUV);
                }
                Mesh(root.transform, "Tufo", MeshForge.Save(b.ToMesh("tufo"), "Props/tufo"), prop, false);
                GrassTuft = SavePrefab(root, "Tufo");
            }
            {
                var root = new GameObject("Samambaia");
                var b = new MeshForge.Builder();
                for (int i = 0; i < 7; i++)
                {
                    float ang = i * 51f;
                    var rot = Quaternion.Euler(0f, ang, 0f) * Quaternion.Euler(-35f, 0f, 0f);
                    for (int s = 0; s < 3; s++)
                        AddRotBox(b, rot * new Vector3(0f, 0f, 0.25f + s * 0.28f) + Vector3.up * 0.12f, new Vector3(0.24f - s * 0.05f, 0.05f, 0.3f), rot, s == 2 ? C("#5f9a48") : C("#3f7536"));
                }
                Mesh(root.transform, "Folhas", MeshForge.Save(b.ToMesh("samambaia"), "Props/samambaia"), prop);
                Fern = SavePrefab(root, "Samambaia");
            }

            // ---------------------------------------------------------------- Ponto de retorno (estandarte)
            {
                var root = new GameObject("PontoDeRetorno");
                var cp = root.AddComponent<Checkpoint>();
                var b = new MeshForge.Builder();
                b.TiledBox(new Vector3(-0.45f, 0f, -0.45f), new Vector3(0.9f, 0.3f, 0.9f), Tile.PillarTop, Tile.PillarSide, new Color32(255, 255, 255, 255));
                Mesh(root.transform, "Base", MeshForge.Save(b.ToMesh("retorno_base"), "Props/retorno_base"), tex);
                var p = new MeshForge.Builder();
                p.Box(new Vector3(-0.06f, 0.3f, -0.06f), new Vector3(0.12f, 2.6f, 0.12f), C("#6a4a2c"), WhiteUV);
                p.Box(new Vector3(0.06f, 1.7f, -0.04f), new Vector3(0.9f, 1.1f, 0.06f), C("#2f8a9a"), WhiteUV);
                p.Box(new Vector3(0.06f, 1.7f, -0.05f), new Vector3(0.9f, 0.1f, 0.08f), C("#d6a93c"), WhiteUV);
                p.Box(new Vector3(0.4f, 2.1f, -0.06f), new Vector3(0.22f, 0.22f, 0.1f), C("#b8f4ff"), WhiteUV);
                p.Box(new Vector3(-0.16f, 2.9f, -0.16f), new Vector3(0.32f, 0.12f, 0.32f), C("#caa13e"), WhiteUV);
                Mesh(root.transform, "Estandarte", MeshForge.Save(p.ToMesh("retorno_estandarte"), "Props/retorno_estandarte"), prop);
                var flame = PointLight(root.transform, new Vector3(0f, 3.2f, 0f), new Color(0.5f, 0.95f, 1f), 6f, 2f, true);
                flame.enabled = false;
                cp.flame = flame;
                Solid(root, new Vector3(0f, 0.15f, 0f), new Vector3(0.9f, 0.3f, 0.9f));
                root.AddComponent<MapPoi>().kind = MapPoiKind.Checkpoint;
                Checkpoint = SavePrefab(root, "PontoDeRetorno");
            }

            // ---------------------------------------------------------------- Altar do mercador
            {
                var root = new GameObject("AltarDeRefino");
                root.AddComponent<MerchantStation>().radius = 2.2f;
                var b = new MeshForge.Builder();
                b.TiledBox(new Vector3(-0.9f, 0f, -0.5f), new Vector3(1.8f, 0.9f, 1f), Tile.CarvedTop, Tile.PillarSide, new Color32(255, 255, 255, 255));
                Mesh(root.transform, "Mesa", MeshForge.Save(b.ToMesh("altar_mesa"), "Props/altar_mesa"), tex);
                var d = new MeshForge.Builder();
                d.Box(new Vector3(-0.6f, 0.9f, -0.2f), new Vector3(0.6f, 0.25f, 0.35f), C("#3a3d44"), WhiteUV);
                d.Box(new Vector3(-0.7f, 1.15f, -0.25f), new Vector3(0.8f, 0.12f, 0.45f), C("#4e525c"), WhiteUV);
                for (int i = 0; i < 6; i++) d.Box(new Vector3(0.25f + (i % 3) * 0.14f, 0.9f + (i / 3) * 0.12f, -0.1f + (i % 2) * 0.12f), new Vector3(0.12f, 0.12f, 0.12f), C("#3ec24a"), WhiteUV);
                Mesh(root.transform, "Objetos", MeshForge.Save(d.ToMesh("altar_objetos"), "Props/altar_objetos"), prop);
                var lan = (GameObject)PrefabUtility.InstantiatePrefab(Lantern, root.transform);
                lan.transform.localPosition = new Vector3(0.95f, 0f, 0.8f);
                lan.transform.localScale = Vector3.one * 0.6f;
                Solid(root, new Vector3(0f, 0.45f, 0f), new Vector3(1.8f, 0.9f, 1f));
                root.AddComponent<MapPoi>().kind = MapPoiKind.Merchant;
                Merchant = SavePrefab(root, "AltarDeRefino");
            }

            // ---------------------------------------------------------------- Portal de saída
            {
                var root = new GameObject("PortalDeSaida");
                var exit = root.AddComponent<ExitPortal>();
                exit.radius = 2.4f;
                var b = new MeshForge.Builder();
                b.TiledBox(new Vector3(-2f, 0f, -0.4f), new Vector3(0.8f, 4f, 0.8f), Tile.PillarTop, Tile.PillarSide, new Color32(255, 255, 255, 255));
                b.TiledBox(new Vector3(1.2f, 0f, -0.4f), new Vector3(0.8f, 4f, 0.8f), Tile.PillarTop, Tile.PillarSide, new Color32(255, 255, 255, 255));
                b.TiledBox(new Vector3(-2.2f, 4f, -0.5f), new Vector3(4.4f, 0.8f, 1f), Tile.CarvedTop, Tile.WallBrick, new Color32(255, 255, 255, 255));
                Mesh(root.transform, "Arco", MeshForge.Save(b.ToMesh("portal_arco"), "Props/portal_arco"), tex);
                var active = new GameObject("Ativo");
                active.transform.SetParent(root.transform, false);
                var surf = new GameObject("Superficie");
                surf.transform.SetParent(active.transform, false);
                surf.transform.localScale = new Vector3(2.4f, 4f, 1f);
                surf.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{MeshForge.Dir}/VFX/beam.asset");
                var smr = surf.AddComponent<MeshRenderer>();
                smr.sharedMaterial = MaterialForge.Get("M_GateField");
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                PointLight(active.transform, new Vector3(0f, 2f, 0.8f), new Color(0.8f, 0.4f, 1f), 7f, 3f, true);
                VfxForge.AmbientMotes(active.transform, new Vector3(0f, 2f, 0f), new Vector3(2.4f, 3.6f, 0.4f), new Color(1f, 0.6f, 1f), 20f);
                exit.activeVisual = active;
                var cols = new GameObject("Colunas");
                cols.transform.SetParent(root.transform, false);
                cols.layer = Layers.Environment;
                var c1 = cols.AddComponent<BoxCollider>(); c1.center = new Vector3(-1.6f, 2f, 0f); c1.size = new Vector3(0.8f, 4f, 0.8f);
                var c2 = cols.AddComponent<BoxCollider>(); c2.center = new Vector3(1.6f, 2f, 0f); c2.size = new Vector3(0.8f, 4f, 0.8f);
                root.AddComponent<MapPoi>().kind = MapPoiKind.Exit;
                Exit = SavePrefab(root, "PortalDeSaida");
            }

            // ---------------------------------------------------------------- Acampamento
            {
                var root = new GameObject("Tenda");
                var b = new MeshForge.Builder();
                var cloth = C("#b3834a");
                var cloth2 = C("#8f6436");
                for (int i = 0; i < 6; i++)
                {
                    float y = i * 0.34f;
                    float w = 2.6f - i * 0.42f;
                    b.Box(new Vector3(-w * 0.5f, y, -1.3f), new Vector3(w, 0.34f, 2.6f), i % 2 == 0 ? cloth : cloth2, WhiteUV);
                }
                b.Box(new Vector3(-0.35f, 0f, 1.28f), new Vector3(0.7f, 1f, 0.04f), C("#2a1e14"), WhiteUV);
                Mesh(root.transform, "Tenda", MeshForge.Save(b.ToMesh("tenda"), "Props/tenda"), prop);
                Solid(root, new Vector3(0f, 1f, 0f), new Vector3(2.6f, 2f, 2.6f));
                Tent = SavePrefab(root, "Tenda");
            }
            {
                var root = new GameObject("Fogueira");
                var b = new MeshForge.Builder();
                for (int i = 0; i < 4; i++)
                {
                    var rot = Quaternion.Euler(0f, i * 45f, 0f);
                    AddRotBox(b, new Vector3(0f, 0.08f, 0f), new Vector3(0.14f, 0.14f, 0.9f), rot, C("#5a3a20"));
                }
                for (int i = 0; i < 8; i++)
                {
                    var rot = Quaternion.Euler(0f, i * 45f, 0f);
                    AddRotBox(b, rot * new Vector3(0f, 0.06f, 0.55f), new Vector3(0.22f, 0.14f, 0.2f), rot, C("#6a6a62"));
                }
                Mesh(root.transform, "Lenha", MeshForge.Save(b.ToMesh("fogueira"), "Props/fogueira"), prop);
                VfxForge.TorchFire(root.transform, new Vector3(0f, 0.1f, 0f), 1.1f, true, 3.5f, 9f);
                Campfire = SavePrefab(root, "Fogueira");
            }
            {
                var root = new GameObject("Caixote");
                var b = new MeshForge.Builder();
                b.TiledBox(new Vector3(-0.42f, 0f, -0.42f), new Vector3(0.84f, 0.84f, 0.84f), Tile.WoodTop, Tile.WoodSide, new Color32(255, 255, 255, 255));
                Mesh(root.transform, "Caixote", MeshForge.Save(b.ToMesh("caixote"), "Props/caixote"), tex);
                Solid(root, new Vector3(0f, 0.42f, 0f), new Vector3(0.84f, 0.84f, 0.84f));
                Crate = SavePrefab(root, "Caixote");
            }
            {
                var root = new GameObject("Vaso");
                var b = new MeshForge.Builder();
                b.Box(new Vector3(-0.25f, 0f, -0.25f), new Vector3(0.5f, 0.5f, 0.5f), C("#a0583a"), WhiteUV);
                b.Box(new Vector3(-0.18f, 0.5f, -0.18f), new Vector3(0.36f, 0.12f, 0.36f), C("#8a4a30"), WhiteUV);
                b.Box(new Vector3(-0.26f, 0.2f, -0.26f), new Vector3(0.52f, 0.06f, 0.52f), C("#c8a060"), WhiteUV);
                Mesh(root.transform, "Vaso", MeshForge.Save(b.ToMesh("vaso"), "Props/vaso"), prop);
                Solid(root, new Vector3(0f, 0.3f, 0f), new Vector3(0.5f, 0.6f, 0.5f));
                Pot = SavePrefab(root, "Vaso");
            }
            {
                var root = new GameObject("Estandarte");
                var b = new MeshForge.Builder();
                b.Box(new Vector3(-0.5f, 0f, 0f), new Vector3(1f, 1.8f, 0.06f), C("#6a2a7a"), WhiteUV);
                b.Box(new Vector3(-0.5f, 1.7f, -0.02f), new Vector3(1f, 0.1f, 0.1f), C("#d6a93c"), WhiteUV);
                b.Box(new Vector3(-0.2f, 0.7f, -0.01f), new Vector3(0.4f, 0.4f, 0.08f), C("#d6a93c"), WhiteUV);
                Mesh(root.transform, "Pano", MeshForge.Save(b.ToMesh("estandarte"), "Props/estandarte"), prop);
                Banner = SavePrefab(root, "Estandarte");
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Caixa rotacionada (centro, tamanho, rotação) adicionada ao construtor.</summary>
        public static void AddRotBox(MeshForge.Builder b, Vector3 center, Vector3 size, Quaternion rot, Color32 col)
        {
            int start = b.v.Count;
            b.Box(-size * 0.5f, size, col, WhiteUV);
            for (int i = start; i < b.v.Count; i++)
            {
                b.v[i] = center + rot * b.v[i];
                b.n[i] = rot * b.n[i];
            }
        }
    }
}

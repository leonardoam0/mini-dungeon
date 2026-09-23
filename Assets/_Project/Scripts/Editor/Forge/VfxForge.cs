using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ruinas.EditorTools
{
    /// <summary>Prefabs de efeitos (partículas cúbicas, feixes, esferas) e a VfxLibrary com as chaves usadas pelo jogo.</summary>
    public static class VfxForge
    {
        public const string Dir = "Assets/_Project/Prefabs/VFX";
        const string LibraryPath = "Assets/_Project/Data/Vfx/VfxLibrary.asset";

        static Mesh cube, quad, sphere, beam;

        struct PS
        {
            public Material mat;
            public bool cubes;
            public float life0, life1, speed0, speed1, size0, size1;
            public Color colA, colB;
            public int burst;
            public float rate, duration, gravity;
            public bool loop, world;
            public ParticleSystemShapeType shape;
            public float radius, angle, arc;
            public Vector3 boxSize;
            public bool fadeOut, shrink, growThenShrink;
            public int maxParticles;
            public float rotSpeed;
            public float noise;
            public Vector3 velocityOverLife;
            public float orbital;
            public float startDelay;
        }

        static ParticleSystem Make(GameObject parent, string name, PS p)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.duration = Mathf.Max(0.05f, p.duration > 0f ? p.duration : 1f);
            main.loop = p.loop;
            main.startLifetime = new ParticleSystem.MinMaxCurve(p.life0, Mathf.Max(p.life0, p.life1));
            main.startSpeed = new ParticleSystem.MinMaxCurve(p.speed0, p.speed1);
            main.startSize = new ParticleSystem.MinMaxCurve(p.size0, p.size1);
            main.startColor = new ParticleSystem.MinMaxGradient(p.colA, p.colB);
            main.gravityModifier = p.gravity;
            main.simulationSpace = p.world ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.maxParticles = p.maxParticles > 0 ? p.maxParticles : 200;
            main.startRotation3D = p.cubes;
            if (p.cubes)
            {
                main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            }
            main.startDelay = p.startDelay;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var em = ps.emission;
            em.enabled = true;
            em.rateOverTime = p.rate;
            if (p.burst > 0) em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)p.burst) });
            else em.SetBursts(new ParticleSystem.Burst[0]);

            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = p.shape;
            sh.radius = Mathf.Max(0.01f, p.radius);
            sh.angle = p.angle;
            sh.arc = p.arc > 0f ? p.arc : 360f;
            if (p.shape == ParticleSystemShapeType.Box) sh.scale = p.boxSize;
            if (p.shape == ParticleSystemShapeType.Circle) sh.rotation = new Vector3(-90f, 0f, 0f);

            if (p.fadeOut || p.shrink || p.growThenShrink)
            {
                var col = ps.colorOverLifetime;
                col.enabled = true;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(p.fadeOut ? 0.85f : 1f, 0.6f), new GradientAlphaKey(p.fadeOut ? 0f : 1f, 1f) });
                col.color = new ParticleSystem.MinMaxGradient(g);
            }
            if (p.shrink || p.growThenShrink)
            {
                var sz = ps.sizeOverLifetime;
                sz.enabled = true;
                var curve = p.growThenShrink
                    ? new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(0.3f, 1f), new Keyframe(0.75f, 0.9f), new Keyframe(1f, 0f))
                    : new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
                sz.size = new ParticleSystem.MinMaxCurve(1f, curve);
            }
            if (p.rotSpeed != 0f)
            {
                var rot = ps.rotationOverLifetime;
                rot.enabled = true;
                rot.separateAxes = p.cubes;
                rot.x = new ParticleSystem.MinMaxCurve(-p.rotSpeed, p.rotSpeed);
                rot.y = new ParticleSystem.MinMaxCurve(-p.rotSpeed, p.rotSpeed);
                rot.z = new ParticleSystem.MinMaxCurve(-p.rotSpeed, p.rotSpeed);
            }
            if (p.noise > 0f)
            {
                var n = ps.noise;
                n.enabled = true;
                n.strength = p.noise;
                n.frequency = 0.6f;
                n.scrollSpeed = 0.4f;
            }
            if (p.velocityOverLife != Vector3.zero || p.orbital != 0f)
            {
                var v = ps.velocityOverLifetime;
                v.enabled = true;
                v.space = ParticleSystemSimulationSpace.Local;
                v.x = new ParticleSystem.MinMaxCurve(p.velocityOverLife.x);
                v.y = new ParticleSystem.MinMaxCurve(p.velocityOverLife.y);
                v.z = new ParticleSystem.MinMaxCurve(p.velocityOverLife.z);
                if (p.orbital != 0f)
                {
                    v.orbitalX = new ParticleSystem.MinMaxCurve(0f);
                    v.orbitalY = new ParticleSystem.MinMaxCurve(p.orbital);
                    v.orbitalZ = new ParticleSystem.MinMaxCurve(0f);
                }
            }

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = p.mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (p.cubes)
            {
                r.renderMode = ParticleSystemRenderMode.Mesh;
                r.mesh = cube;
                r.alignment = ParticleSystemRenderSpace.World;
                r.SetActiveVertexStreams(new List<ParticleSystemVertexStream> { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Normal, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV });
            }
            else
            {
                r.renderMode = ParticleSystemRenderMode.Billboard;
                r.SetActiveVertexStreams(new List<ParticleSystemVertexStream> { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Normal, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV });
            }
            return ps;
        }

        static Light AddLight(GameObject parent, Color c, float range, float intensity, bool flicker = false, Vector3? pos = null)
        {
            var go = new GameObject("Luz");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos ?? new Vector3(0f, 0.6f, 0f);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            if (flicker)
            {
                var f = go.AddComponent<LightFlicker>();
                f.baseIntensity = intensity;
            }
            return l;
        }

        static MeshRenderer MeshChild(GameObject parent, string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return mr;
        }

        static readonly List<VfxEntry> entries = new List<VfxEntry>();

        static void Save(GameObject go, string key, float lifetime, int prewarm = 2, int max = 24)
        {
            Directory.CreateDirectory(Dir);
            go.AddComponent<PooledVfx>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{Dir}/{key}.prefab");
            Object.DestroyImmediate(go);
            entries.Add(new VfxEntry { key = key, prefab = prefab, lifetime = lifetime, prewarm = prewarm, maxInstances = max });
        }

        public static void BuildAll()
        {
            entries.Clear();
            cube = MeshForge.Save(MeshForge.Cube("vfx_cube"), "VFX/cube");
            quad = MeshForge.Save(MeshForge.QuadMesh("vfx_quad"), "VFX/quad");
            sphere = MeshForge.Save(MeshForge.Sphere("vfx_sphere"), "VFX/sphere");
            beam = MeshForge.Save(MeshForge.BeamCross("vfx_beam"), "VFX/beam");
            var cubeMat = MaterialForge.Get("M_ParticleCube");
            var softMat = MaterialForge.Get("M_ParticleSoft");
            var addSq = MaterialForge.Get("M_AddSquare");
            var addSoft = MaterialForge.Get("M_AddSoft");

            Color C(string hex, float a = 1f) => Pal.Hex(hex, a);

            // Impacto de arma: clarão breve + estilhaços.
            {
                var go = new GameObject("impact_small");
                var flash = MeshChild(go, "Clarao", quad, addSoft);
                var fx = go.AddComponent<FlashFx>();
                fx.quad = flash; fx.duration = 0.09f; fx.startScale = 0.5f; fx.endScale = 1.4f; fx.intensity = 2.4f;
                Make(go, "Estilhacos", new PS { mat = addSq, cubes = true, life0 = 0.15f, life1 = 0.3f, speed0 = 2f, speed1 = 5f, size0 = 0.05f, size1 = 0.11f, colA = C("#fff4d0"), colB = C("#ffd27a"), burst = 9, duration = 0.3f, gravity = 0.8f, world = true, shape = ParticleSystemShapeType.Sphere, radius = 0.1f, shrink = true, rotSpeed = 8f });
                Save(go, "impact_small", 0.6f, 6, 40);
            }
            // Poeira ao rolar/aterrissar.
            {
                var go = new GameObject("dust_puff");
                Make(go, "Poeira", new PS { mat = cubeMat, cubes = true, life0 = 0.4f, life1 = 0.7f, speed0 = 0.6f, speed1 = 1.8f, size0 = 0.12f, size1 = 0.22f, colA = C("#8a8378", 0.8f), colB = C("#6d675e", 0.8f), burst = 12, duration = 0.5f, gravity = -0.05f, world = true, shape = ParticleSystemShapeType.Circle, radius = 0.35f, fadeOut = true, shrink = true, rotSpeed = 3f });
                Save(go, "dust_puff", 0.9f, 3, 20);
            }
            // Penas (salto).
            {
                var go = new GameObject("feather_burst");
                Make(go, "Penas", new PS { mat = cubeMat, cubes = true, life0 = 0.5f, life1 = 0.9f, speed0 = 1.2f, speed1 = 3f, size0 = 0.06f, size1 = 0.12f, colA = C("#f4f8ff"), colB = C("#9fdcff"), burst = 16, duration = 0.6f, gravity = -0.2f, world = true, shape = ParticleSystemShapeType.Sphere, radius = 0.4f, fadeOut = true, noise = 0.6f, rotSpeed = 6f });
                Save(go, "feather_burst", 1.1f, 2, 8);
            }
            // Onda de choque no chão.
            {
                var go = new GameObject("shockwave_ring");
                var ring = MeshChild(go, "Anel", quad, MaterialForge.Get("M_AddRing"));
                ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var fx = go.AddComponent<RingFx>();
                fx.ring = ring;
                Make(go, "Faiscas", new PS { mat = addSq, cubes = true, life0 = 0.25f, life1 = 0.45f, speed0 = 3f, speed1 = 6f, size0 = 0.05f, size1 = 0.1f, colA = C("#ffffff"), colB = C("#cfe8ff"), burst = 18, duration = 0.4f, world = true, shape = ParticleSystemShapeType.Circle, radius = 0.3f, shrink = true });
                Save(go, "shockwave_ring", 0.6f, 3, 12);
            }
            // Carga mágica (antes do pulso).
            {
                var go = new GameObject("magic_charge");
                Make(go, "Convergencia", new PS { mat = addSq, cubes = true, life0 = 0.15f, life1 = 0.25f, speed0 = -6f, speed1 = -4f, size0 = 0.06f, size1 = 0.12f, colA = C("#ff9cff"), colB = C("#c568ef"), rate = 90f, duration = 0.3f, loop = true, world = false, shape = ParticleSystemShapeType.Sphere, radius = 1.3f, shrink = true });
                AddLight(go, C("#d45ad8"), 5f, 3f, false, Vector3.zero);
                Save(go, "magic_charge", 0.4f, 2, 8);
            }
            // Pulso magenta: esfera, arcos e partículas.
            {
                var go = new GameObject("pulse_magenta");
                var sph = MeshChild(go, "Esfera", sphere, MaterialForge.Get("M_Bubble"));
                sph.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                var fx = go.AddComponent<PulseSphereFx>();
                fx.sphere = sph;
                var arcs = new List<LineRenderer>();
                for (int i = 0; i < 6; i++)
                {
                    var a = new GameObject("Arco" + i);
                    a.transform.SetParent(go.transform, false);
                    var lr = a.AddComponent<LineRenderer>();
                    lr.useWorldSpace = false;
                    lr.positionCount = 6;
                    lr.widthMultiplier = 0.06f;
                    lr.sharedMaterial = MaterialForge.Get("M_Trail");
                    lr.startColor = C("#ffd6ff");
                    lr.endColor = C("#e05ae0");
                    lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    arcs.Add(lr);
                }
                fx.arcs = arcs.ToArray();
                fx.pulseLight = AddLight(go, C("#e070ff"), 11f, 6f, false, new Vector3(0f, 1f, 0f));
                // No vídeo a esfera fica visível ~0,2 s e se desfaz em bolhas lima/amarelas.
                fx.growTime = 0.12f; fx.holdTime = 0.06f; fx.fadeTime = 0.2f;
                Make(go, "Cubos", new PS { mat = addSq, cubes = true, life0 = 0.35f, life1 = 0.6f, speed0 = 5f, speed1 = 9f, size0 = 0.08f, size1 = 0.16f, colA = C("#ffb8ff"), colB = C("#c568ef"), burst = 36, duration = 0.5f, world = true, shape = ParticleSystemShapeType.Sphere, radius = 0.6f, shrink = true, rotSpeed = 6f });
                var blobs = Make(go, "Bolhas", new PS { mat = cubeMat, cubes = true, life0 = 0.45f, life1 = 0.8f, speed0 = 2.5f, speed1 = 5f, size0 = 0.45f, size1 = 0.95f, colA = C("#e4ff6a", 0.95f), colB = C("#8ee03c", 0.9f), burst = 28, duration = 0.5f, world = true, shape = ParticleSystemShapeType.Sphere, radius = 2.6f, growThenShrink = true, rotSpeed = 2f, startDelay = 0.12f, maxParticles = 60 });
                blobs.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                Save(go, "pulse_magenta", 1.2f, 2, 6);
            }
            // Orbe violeta (carga do obelisco antes do feixe).
            {
                var go = new GameObject("orb_violet");
                var sph = MeshChild(go, "Esfera", sphere, MaterialForge.Get("M_Bubble"));
                var fx = go.AddComponent<OrbFx>();
                fx.sphere = sph;
                fx.defaultColor = new Color(2.2f, 0.6f, 2.8f);
                fx.intensity = 1.4f;
                fx.orbLight = AddLight(go, C("#c060ff"), 8f, 4.5f, false, Vector3.zero);
                Make(go, "Faiscas", new PS { mat = addSq, cubes = true, life0 = 0.25f, life1 = 0.45f, speed0 = 1f, speed1 = 2.5f, size0 = 0.06f, size1 = 0.12f, colA = C("#ffc8ff"), colB = C("#b040e0"), rate = 60f, duration = 0.6f, loop = true, world = true, shape = ParticleSystemShapeType.Sphere, radius = 0.9f, shrink = true });
                Save(go, "orb_violet", 0.8f, 1, 4);
            }
            // Coluna de fogo (labareda alta e breve).
            {
                var go = new GameObject("fire_column");
                Make(go, "Labareda", new PS { mat = addSq, cubes = true, life0 = 0.35f, life1 = 0.6f, speed0 = 5f, speed1 = 9f, size0 = 0.25f, size1 = 0.55f, colA = C("#fff0a0"), colB = C("#ff7a1a"), rate = 150f, duration = 0.6f, loop = true, world = true, shape = ParticleSystemShapeType.Circle, radius = 0.45f, shrink = true, noise = 0.8f, maxParticles = 220 });
                Make(go, "Nucleo", new PS { mat = addSq, cubes = true, life0 = 0.25f, life1 = 0.4f, speed0 = 7f, speed1 = 11f, size0 = 0.15f, size1 = 0.3f, colA = C("#ffffff"), colB = C("#ffe07a"), rate = 80f, duration = 0.6f, loop = true, world = true, shape = ParticleSystemShapeType.Circle, radius = 0.2f, shrink = true });
                Make(go, "Brasas", new PS { mat = addSq, cubes = true, life0 = 0.8f, life1 = 1.4f, speed0 = 2f, speed1 = 4f, size0 = 0.04f, size1 = 0.08f, colA = C("#ffd060"), colB = C("#ff8a3a"), rate = 30f, duration = 0.6f, loop = true, world = true, shape = ParticleSystemShapeType.Circle, radius = 0.6f, noise = 1f, fadeOut = true });
                AddLight(go, C("#ff9a3a"), 10f, 6f, true, new Vector3(0f, 2f, 0f));
                Save(go, "fire_column", 0.6f, 2, 6);
            }
            // Explosão: clarão de fogo, casca incandescente, anel de choque, brasas e fumaça.
            {
                var go = new GameObject("explosion_fire");
                var core = MeshChild(go, "Clarao", quad, addSoft);
                core.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                var flash = go.AddComponent<FlashFx>();
                flash.quad = core; flash.duration = 0.45f; flash.startScale = 3f; flash.endScale = 7.5f; flash.intensity = 3f;
                var sph = MeshChild(go, "Casca", sphere, MaterialForge.Get("M_Bubble"));
                sph.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                var orb = go.AddComponent<OrbFx>();
                orb.sphere = sph; orb.growTime = 0.12f; orb.fadeTime = 0.25f; orb.defaultRadius = 2.6f; orb.defaultHold = 0.12f; orb.intensity = 2f; orb.pulseAmount = 0.03f; orb.endScale = 1.3f;
                orb.defaultColor = new Color(2.6f, 1.4f, 0.4f);
                orb.orbLight = AddLight(go, C("#ffb040"), 14f, 9f, false, new Vector3(0f, 1.2f, 0f));
                var ring = MeshChild(go, "Anel", quad, MaterialForge.Get("M_AddRing"));
                ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                ring.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                var rfx = go.AddComponent<RingFx>();
                rfx.ring = ring; rfx.duration = 0.55f; rfx.defaultRadius = 4.5f; rfx.intensity = 2.2f;
                Make(go, "Fogo", new PS { mat = addSq, cubes = true, life0 = 0.3f, life1 = 0.6f, speed0 = 5f, speed1 = 10f, size0 = 0.3f, size1 = 0.7f, colA = C("#fff0a0"), colB = C("#ff6a1a"), burst = 70, duration = 0.5f, world = true, shape = ParticleSystemShapeType.Sphere, radius = 0.8f, shrink = true, maxParticles = 120 });
                Make(go, "Brasas", new PS { mat = addSq, cubes = true, life0 = 0.7f, life1 = 1.3f, speed0 = 3f, speed1 = 7f, size0 = 0.05f, size1 = 0.1f, colA = C("#ffd060"), colB = C("#ff8a3a"), burst = 40, duration = 0.5f, gravity = 0.6f, world = true, shape = ParticleSystemShapeType.Sphere, radius = 0.5f, fadeOut = true });
                Make(go, "Fumaca", new PS { mat = softMat, cubes = false, life0 = 0.8f, life1 = 1.3f, speed0 = 1f, speed1 = 2.5f, size0 = 1.2f, size1 = 2.2f, colA = C("#3a3230", 0.5f), colB = C("#5a4a40", 0.4f), burst = 12, duration = 0.5f, world = true, shape = ParticleSystemShapeType.Sphere, radius = 1f, fadeOut = true, startDelay = 0.15f });
                Save(go, "explosion_fire", 1.6f, 1, 4);
            }
            // Clarão branco ascendente (encontro resolvido ao longe).
            {
                var go = new GameObject("flash_white");
                var core = MeshChild(go, "Clarao", quad, addSoft);
                var flash = go.AddComponent<FlashFx>();
                flash.quad = core; flash.duration = 0.5f; flash.startScale = 2.2f; flash.endScale = 3.5f; flash.intensity = 3.2f;
                AddLight(go, Color.white, 12f, 6f, false, Vector3.zero);
                Make(go, "Nuvem", new PS { mat = softMat, cubes = false, life0 = 0.4f, life1 = 0.7f, speed0 = 0.8f, speed1 = 1.8f, size0 = 0.9f, size1 = 1.6f, colA = C("#ffffff", 0.7f), colB = C("#e8eef0", 0.6f), burst = 10, duration = 0.4f, world = true, shape = ParticleSystemShapeType.Sphere, radius = 0.6f, fadeOut = true, velocityOverLife = new Vector3(0f, 2.5f, 0f), startDelay = 0.1f });
                Save(go, "flash_white", 1f, 1, 3);
            }
            // Coluna magenta.
            {
                var go = new GameObject("beam_magenta");
                var b1 = MeshChild(go, "Feixe", beam, MaterialForge.Get("M_AddBeam"));
                var fx = go.AddComponent<BeamFx>();
                fx.beams = new Renderer[] { b1 };
                fx.width = 0.95f;
                fx.defaultHeight = 10f;
                fx.beamLight = AddLight(go, C("#d45ad8"), 9f, 4f, false, new Vector3(0f, 1.5f, 0f));
                Make(go, "Subida", new PS { mat = addSq, cubes = true, life0 = 0.5f, life1 = 0.9f, speed0 = 3f, speed1 = 6f, size0 = 0.07f, size1 = 0.14f, colA = C("#ffc8ff"), colB = C("#c568ef"), rate = 40f, duration = 1f, loop = true, world = true, shape = ParticleSystemShapeType.Circle, radius = 0.45f, shrink = true, velocityOverLife = new Vector3(0f, 2f, 0f) });
                Save(go, "beam_magenta", 1.2f, 2, 6);
            }
            // Chama magenta (topo do obelisco).
            {
                var go = new GameObject("magic_flame");
                Make(go, "Chama", new PS { mat = addSq, cubes = true, life0 = 0.35f, life1 = 0.6f, speed0 = 1.2f, speed1 = 2.6f, size0 = 0.12f, size1 = 0.26f, colA = C("#ffb0ff"), colB = C("#b040e0"), rate = 55f, duration = 1f, loop = true, world = true, shape = ParticleSystemShapeType.Box, boxSize = new Vector3(0.9f, 0.1f, 0.9f), shrink = true, velocityOverLife = new Vector3(0f, 1f, 0f), noise = 0.4f });
                Save(go, "magic_flame", 1.5f, 1, 4);
            }
            // Fogo localizado (explosão/queima de chão).
            {
                var go = new GameObject("fire_burst");
                Make(go, "Fogo", new PS { mat = addSq, cubes = true, life0 = 0.35f, life1 = 0.7f, speed0 = 1.5f, speed1 = 3.5f, size0 = 0.14f, size1 = 0.32f, colA = C("#ffe07a"), colB = C("#ff7a26"), rate = 70f, duration = 1f, loop = true, world = true, shape = ParticleSystemShapeType.Sphere, radius = 0.55f, shrink = true, velocityOverLife = new Vector3(0f, 1.8f, 0f), noise = 0.5f });
                Make(go, "Brasas", new PS { mat = addSq, cubes = true, life0 = 0.6f, life1 = 1.1f, speed0 = 1f, speed1 = 2.2f, size0 = 0.04f, size1 = 0.07f, colA = C("#ffd060"), colB = C("#ff8a3a"), rate = 14f, duration = 1f, loop = true, world = true, shape = ParticleSystemShapeType.Sphere, radius = 0.5f, velocityOverLife = new Vector3(0f, 1.2f, 0f), noise = 0.8f, fadeOut = true });
                AddLight(go, C("#ffb34d"), 6f, 3.2f, true);
                Save(go, "fire_burst", 2f, 2, 8);
            }
            // Status: queimadura (loop preso ao ator).
            {
                var go = new GameObject("burning");
                Make(go, "Fogo", new PS { mat = addSq, cubes = true, life0 = 0.3f, life1 = 0.55f, speed0 = 0.8f, speed1 = 1.8f, size0 = 0.1f, size1 = 0.22f, colA = C("#ffe07a"), colB = C("#ff6a1a"), rate = 38f, duration = 1f, loop = true, world = true, shape = ParticleSystemShapeType.Box, boxSize = new Vector3(0.6f, 1.2f, 0.6f), shrink = true, velocityOverLife = new Vector3(0f, 1.6f, 0f) });
                AddLight(go, C("#ffa040"), 4f, 1.8f, true, new Vector3(0f, 0.2f, 0f));
                Save(go, "burning", -1f, 3, 16);
            }
            // Status: atordoado (estrelas orbitando).
            {
                var go = new GameObject("stun_stars");
                var ps = Make(go, "Estrelas", new PS { mat = addSq, cubes = true, life0 = 1f, life1 = 1f, speed0 = 0f, speed1 = 0f, size0 = 0.1f, size1 = 0.1f, colA = C("#fff07a"), colB = C("#ffffff"), rate = 3f, duration = 1f, loop = true, world = false, shape = ParticleSystemShapeType.Circle, radius = 0.45f, orbital = 5f });
                ps.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                Save(go, "stun_stars", -1f, 2, 12);
            }
            // Status: vulnerável (motas magenta).
            {
                var go = new GameObject("vulnerable_glow");
                Make(go, "Motas", new PS { mat = addSq, cubes = true, life0 = 0.6f, life1 = 1f, speed0 = 0.3f, speed1 = 0.8f, size0 = 0.05f, size1 = 0.1f, colA = C("#ff9cff"), colB = C("#c568ef"), rate = 16f, duration = 1f, loop = true, world = true, shape = ParticleSystemShapeType.Box, boxSize = new Vector3(0.7f, 1.6f, 0.7f), velocityOverLife = new Vector3(0f, 0.8f, 0f), shrink = true });
                Save(go, "vulnerable_glow", -1f, 3, 16);
            }
            // Status: veneno.
            {
                var go = new GameObject("poison_bubbles");
                Make(go, "Bolhas", new PS { mat = cubeMat, cubes = true, life0 = 0.5f, life1 = 0.9f, speed0 = 0.3f, speed1 = 0.8f, size0 = 0.06f, size1 = 0.12f, colA = C("#7cff5a"), colB = C("#3aa82e"), rate = 12f, duration = 1f, loop = true, world = true, shape = ParticleSystemShapeType.Box, boxSize = new Vector3(0.6f, 1.4f, 0.6f), velocityOverLife = new Vector3(0f, 0.7f, 0f), shrink = true });
                Save(go, "poison_bubbles", -1f, 3, 16);
            }
            // Cura.
            {
                var go = new GameObject("heal_swirl");
                Make(go, "Espiral", new PS { mat = addSq, cubes = true, life0 = 0.6f, life1 = 0.9f, speed0 = 0.5f, speed1 = 1f, size0 = 0.06f, size1 = 0.12f, colA = C("#8aff8a"), colB = C("#ffffff"), rate = 50f, duration = 0.8f, world = false, shape = ParticleSystemShapeType.Circle, radius = 0.55f, orbital = 4f, velocityOverLife = new Vector3(0f, 1.6f, 0f), shrink = true });
                AddLight(go, C("#7aff8a"), 4f, 1.5f, false, new Vector3(0f, 1f, 0f));
                Save(go, "heal_swirl", 1.2f, 2, 6);
            }
            // Subida de nível.
            {
                var go = new GameObject("level_up");
                Make(go, "Coluna", new PS { mat = addSq, cubes = true, life0 = 0.7f, life1 = 1.2f, speed0 = 2f, speed1 = 4f, size0 = 0.07f, size1 = 0.14f, colA = C("#e6c0ff"), colB = C("#ffd54a"), rate = 70f, duration = 1f, world = false, shape = ParticleSystemShapeType.Circle, radius = 0.7f, velocityOverLife = new Vector3(0f, 1f, 0f), shrink = true });
                AddLight(go, C("#c890ff"), 5f, 2.5f, false, new Vector3(0f, 1f, 0f));
                Save(go, "level_up", 2f, 1, 3);
            }
            // Brilhos de baú e coleta.
            {
                var go = new GameObject("chest_sparkle");
                Make(go, "Brilho", new PS { mat = addSq, cubes = true, life0 = 0.5f, life1 = 1f, speed0 = 1.5f, speed1 = 3.5f, size0 = 0.05f, size1 = 0.1f, colA = C("#fff0a0"), colB = C("#ffc040"), burst = 30, duration = 0.5f, gravity = 0.4f, world = true, shape = ParticleSystemShapeType.Hemisphere, radius = 0.5f, shrink = true });
                AddLight(go, C("#ffd070"), 4f, 2f, false);
                Save(go, "chest_sparkle", 1.2f, 1, 4);
            }
            {
                var go = new GameObject("pickup_sparkle");
                Make(go, "Brilho", new PS { mat = addSq, cubes = true, life0 = 0.3f, life1 = 0.5f, speed0 = 1f, speed1 = 2.5f, size0 = 0.04f, size1 = 0.08f, colA = C("#ffffff"), colB = C("#bfffd0"), burst = 10, duration = 0.4f, world = true, shape = ParticleSystemShapeType.Sphere, radius = 0.2f, shrink = true });
                Save(go, "pickup_sparkle", 0.6f, 3, 16);
            }
            // Brilho de loot (feixe na cor da raridade; não cobre o piso).
            {
                var go = new GameObject("loot_beam");
                var b1 = MeshChild(go, "Feixe", beam, MaterialForge.Get("M_LootBeam"));
                var fx = go.AddComponent<BeamFx>();
                fx.beams = new Renderer[] { b1 };
                fx.width = 0.45f;
                fx.defaultHeight = 3f;
                fx.baseIntensity = 0.9f;
                fx.pulse = 0.1f;
                fx.beamLight = AddLight(go, Color.white, 3.5f, 1.4f, false, new Vector3(0f, 0.6f, 0f));
                var glow = MeshChild(go, "Brilho", quad, addSoft);
                glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                glow.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                glow.transform.localScale = Vector3.one * 1.1f;
                Save(go, "loot_beam", -1f, 2, 16);
            }
            // Surgimento de inimigo.
            {
                var go = new GameObject("spawn_burst");
                Make(go, "Terra", new PS { mat = cubeMat, cubes = true, life0 = 0.4f, life1 = 0.8f, speed0 = 2f, speed1 = 4f, size0 = 0.1f, size1 = 0.2f, colA = C("#4a3a2a"), colB = C("#3e5a30"), burst = 16, duration = 0.5f, gravity = 1.2f, world = true, shape = ParticleSystemShapeType.Circle, radius = 0.4f, angle = 25f, shrink = true, rotSpeed = 6f });
                Make(go, "Fumaca", new PS { mat = softMat, cubes = false, life0 = 0.5f, life1 = 0.9f, speed0 = 0.4f, speed1 = 1f, size0 = 0.6f, size1 = 1.1f, colA = C("#2a3a30", 0.55f), colB = C("#4a5a48", 0.45f), burst = 6, duration = 0.5f, world = true, shape = ParticleSystemShapeType.Circle, radius = 0.4f, fadeOut = true });
                Save(go, "spawn_burst", 1f, 3, 12);
            }
            // Derrota: dissolução em cubos (cor por inimigo) + fumaça.
            {
                var go = new GameObject("death_cubes");
                Make(go, "Cubos", new PS { mat = cubeMat, cubes = true, life0 = 0.5f, life1 = 0.9f, speed0 = 1.5f, speed1 = 4.5f, size0 = 0.1f, size1 = 0.22f, colA = Color.white, colB = new Color(0.8f, 0.8f, 0.8f), burst = 22, duration = 0.5f, gravity = 1.5f, world = true, shape = ParticleSystemShapeType.Box, boxSize = new Vector3(0.6f, 1.6f, 0.4f), shrink = true, rotSpeed = 8f });
                Save(go, "death_cubes", 1.2f, 4, 16);
            }
            {
                var go = new GameObject("death_puff");
                Make(go, "Fumaca", new PS { mat = softMat, cubes = false, life0 = 0.5f, life1 = 0.8f, speed0 = 0.5f, speed1 = 1.3f, size0 = 0.7f, size1 = 1.2f, colA = C("#d8d8d0", 0.55f), colB = C("#a0a098", 0.45f), burst = 8, duration = 0.5f, world = true, shape = ParticleSystemShapeType.Sphere, radius = 0.5f, fadeOut = true });
                Save(go, "death_puff", 1f, 4, 16);
            }
            // Invocação.
            {
                var go = new GameObject("summon_puff");
                Make(go, "Nuvem", new PS { mat = softMat, cubes = false, life0 = 0.5f, life1 = 0.8f, speed0 = 0.6f, speed1 = 1.5f, size0 = 0.8f, size1 = 1.4f, colA = C("#fff4d0", 0.6f), colB = C("#ffe08a", 0.5f), burst = 10, duration = 0.5f, world = true, shape = ParticleSystemShapeType.Sphere, radius = 0.6f, fadeOut = true });
                Make(go, "Brilhos", new PS { mat = addSq, cubes = true, life0 = 0.4f, life1 = 0.7f, speed0 = 1.5f, speed1 = 3f, size0 = 0.05f, size1 = 0.1f, colA = C("#fff080"), colB = C("#ffffff"), burst = 20, duration = 0.5f, world = true, shape = ParticleSystemShapeType.Sphere, radius = 0.5f, shrink = true });
                Save(go, "summon_puff", 1f, 2, 8);
            }
            // Checkpoint.
            {
                var go = new GameObject("checkpoint_flare");
                Make(go, "Coluna", new PS { mat = addSq, cubes = true, life0 = 0.6f, life1 = 1f, speed0 = 2f, speed1 = 4f, size0 = 0.06f, size1 = 0.12f, colA = C("#b8f4ff"), colB = C("#5ad0e0"), burst = 40, duration = 0.6f, world = true, shape = ParticleSystemShapeType.Circle, radius = 0.5f, velocityOverLife = new Vector3(0f, 1f, 0f), shrink = true });
                AddLight(go, C("#7ae8ff"), 5f, 2.5f);
                Save(go, "checkpoint_flare", 1.3f, 1, 3);
            }
            // Nuvem verde (raio vindo da área de dano).
            {
                var go = new GameObject("cloud_green");
                var cubes = Make(go, "Cubos", new PS { mat = cubeMat, cubes = true, life0 = 0.9f, life1 = 1.4f, speed0 = 0.1f, speed1 = 0.35f, size0 = 0.32f, size1 = 0.62f, colA = C("#4ad83c", 0.92f), colB = C("#2fa834", 0.9f), rate = 40f, duration = 1f, loop = true, world = true, shape = ParticleSystemShapeType.Circle, radius = 1.5f, growThenShrink = true, rotSpeed = 1.2f, velocityOverLife = new Vector3(0f, 0.25f, 0f), maxParticles = 160 });
                var wisps = Make(go, "Nevoa", new PS { mat = softMat, cubes = false, life0 = 1f, life1 = 1.6f, speed0 = 0.05f, speed1 = 0.2f, size0 = 1f, size1 = 1.8f, colA = C("#6aff5a", 0.18f), colB = C("#3acc3a", 0.14f), rate = 8f, duration = 1f, loop = true, world = true, shape = ParticleSystemShapeType.Circle, radius = 1.2f, fadeOut = true, maxParticles = 40 });
                cubes.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                wisps.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                var fx = go.AddComponent<CloudFx>();
                fx.cubes = cubes;
                fx.wisps = wisps;
                AddLight(go, C("#4ad83c"), 4f, 0.9f, false, new Vector3(0f, 0.8f, 0f));
                Save(go, "cloud_green", 4f, 4, 16);
            }
            {
                var go = new GameObject("spore_burst");
                Make(go, "Respingo", new PS { mat = cubeMat, cubes = true, life0 = 0.3f, life1 = 0.6f, speed0 = 2f, speed1 = 4.5f, size0 = 0.1f, size1 = 0.2f, colA = C("#7cff5a"), colB = C("#b45aa0"), burst = 16, duration = 0.4f, gravity = 1.2f, world = true, shape = ParticleSystemShapeType.Hemisphere, radius = 0.3f, shrink = true });
                Save(go, "spore_burst", 0.8f, 3, 12);
            }
            {
                var go = new GameObject("impact_spit");
                Make(go, "Respingo", new PS { mat = cubeMat, cubes = true, life0 = 0.25f, life1 = 0.45f, speed0 = 1.5f, speed1 = 3f, size0 = 0.06f, size1 = 0.12f, colA = C("#eef8e8"), colB = C("#b8e0a8"), burst = 10, duration = 0.3f, gravity = 1f, world = true, shape = ParticleSystemShapeType.Sphere, radius = 0.15f, shrink = true });
                Save(go, "impact_spit", 0.6f, 3, 12);
            }

            // Biblioteca
            Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath));
            var lib = AssetDatabase.LoadAssetAtPath<VfxLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<VfxLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }
            lib.entries = entries.ToArray();
            lib.telegraphMaterial = MaterialForge.Get("M_Telegraph");
            lib.glowAdditive = MaterialForge.Get("M_Trail");
            lib.particleCube = cubeMat;
            lib.cubeMesh = cube;
            lib.quadMesh = quad;
            lib.sphereMesh = sphere;
            EditorUtility.SetDirty(lib);
            if (DataForge.Cache != null)
            {
                DataForge.Cache.db.vfx = lib;
                EditorUtility.SetDirty(DataForge.Cache.db);
            }
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ Efeitos fixos de cenário (não pooled)

        public static GameObject TorchFire(Transform parent, Vector3 localPos, float scale, bool shadows, float intensity = 3.2f, float range = 8f)
        {
            var go = new GameObject("Fogo");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var addSq = MaterialForge.Get("M_AddSquare");
            Make(go, "Chama", new PS { mat = addSq, cubes = true, life0 = 0.35f, life1 = 0.6f, speed0 = 0.8f, speed1 = 1.6f, size0 = 0.12f * scale, size1 = 0.26f * scale, colA = Pal.Hex("#ffe07a"), colB = Pal.Hex("#ff7a26"), rate = 42f, duration = 1f, loop = true, world = true, shape = ParticleSystemShapeType.Box, boxSize = new Vector3(0.55f, 0.1f, 0.55f) * scale, shrink = true, velocityOverLife = new Vector3(0f, 1.1f, 0f), noise = 0.3f }).Play();
            Make(go, "Faiscas", new PS { mat = addSq, cubes = true, life0 = 0.8f, life1 = 1.4f, speed0 = 0.6f, speed1 = 1.4f, size0 = 0.035f, size1 = 0.06f, colA = Pal.Hex("#ffd060"), colB = Pal.Hex("#ff9a3a"), rate = 5f, duration = 1f, loop = true, world = true, shape = ParticleSystemShapeType.Box, boxSize = new Vector3(0.4f, 0.1f, 0.4f) * scale, velocityOverLife = new Vector3(0f, 0.8f, 0f), noise = 0.9f, fadeOut = true }).Play();
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                main.playOnAwake = true;
                main.prewarm = true;
            }
            var light = AddLight(go, Pal.Hex("#ffae50"), range, intensity, true, new Vector3(0f, 0.7f, 0f));
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = 0.8f;
            return go;
        }

        public static GameObject AmbientMotes(Transform parent, Vector3 pos, Vector3 size, Color color, float rate)
        {
            var go = new GameObject("Motas");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var ps = Make(go, "Motas", new PS { mat = MaterialForge.Get("M_AddSquare"), cubes = true, life0 = 3f, life1 = 6f, speed0 = 0.05f, speed1 = 0.2f, size0 = 0.03f, size1 = 0.06f, colA = color, colB = Color.Lerp(color, Color.white, 0.4f), rate = rate, duration = 5f, loop = true, world = true, shape = ParticleSystemShapeType.Box, boxSize = size, noise = 0.3f, fadeOut = true, maxParticles = 120 });
            var main = ps.main;
            main.playOnAwake = true;
            main.prewarm = true;
            return go;
        }
    }
}

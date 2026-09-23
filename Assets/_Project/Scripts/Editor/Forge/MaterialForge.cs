using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ruinas.EditorTools
{
    /// <summary>Materiais do projeto (cor-base separada de emissão; brilho só nas fontes emissivas).</summary>
    public static class MaterialForge
    {
        public const string Dir = "Assets/_Project/Art/Materials";

        public static Material Get(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/{name}.mat");

        static Material Make(string name, string shaderName)
        {
            Directory.CreateDirectory(Dir);
            string path = $"{Dir}/{name}.mat";
            var shader = Shader.Find(shaderName);
            if (shader == null) Debug.LogError($"[Forge] Shader não encontrado: {shaderName}");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            else if (shader != null && m.shader != shader) m.shader = shader;
            return m;
        }

        static Material Voxel(string name, Texture tex, bool seeThrough, float tint, float ao, float aoIndirect, float fog = 1f)
        {
            var m = Make(name, "Ruinas/VoxelLit");
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_SeeThrough", seeThrough ? 1f : 0f);
            m.SetFloat("_TintStrength", tint);
            m.SetFloat("_AOStrength", ao);
            m.SetFloat("_AOIndirect", aoIndirect);
            m.SetFloat("_HeightFogAffect", fog);
            m.SetFloat("_HitFlash", 0f);
            m.SetColor("_FlashColor", Color.white);
            m.SetFloat("_UseEmission", 0f);
            m.DisableKeyword("_EMISSION");
            m.SetFloat("_UseSpecular", 0f);
            m.DisableKeyword("_SPECULAR_COLOR");
            m.enableInstancing = true;
            return m;
        }

        static void Emission(Material m, Texture map, Color c)
        {
            m.SetTexture("_EmissionMap", map);
            m.SetColor("_EmissionColor", c);
            m.SetFloat("_UseEmission", 1f);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        static void Specular(Material m, Color spec)
        {
            m.SetFloat("_UseSpecular", 1f);
            m.SetColor("_SpecColor", spec);
            m.EnableKeyword("_SPECULAR_COLOR");
        }

        static Material Glow(string name, string shader, Texture tex, Color c, float intensity)
        {
            var m = Make(name, shader);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetColor("_Color", c);
            m.SetFloat("_Intensity", intensity);
            m.enableInstancing = true;
            return m;
        }

        public static void GenerateAll()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureForge.WorldAtlasPath);
            var emission = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureForge.WorldEmissionPath);
            var white = TextureForge.LoadTexture("UI/white.png");
            Texture2D Skin(string n) => TextureForge.LoadTexture($"Characters/{n}.png");
            var soft = TextureForge.LoadTexture("FX/soft_circle.png");
            var noise = TextureForge.LoadTexture("FX/noise.png");
            var beam = TextureForge.LoadTexture("FX/beam.png");
            var ring = TextureForge.LoadTexture("FX/ring.png");
            var square = TextureForge.LoadTexture("FX/square.png");

            // Cenário
            var world = Voxel("M_World", atlas, true, 1f, 1f, 0.55f);
            Emission(world, emission, new Color(0.9f, 1.1f, 1.05f));
            Voxel("M_WorldNoCut", atlas, false, 1f, 1f, 0.55f);
            var water = Voxel("M_Water", atlas, false, 1f, 0f, 0f);
            Emission(water, white, new Color(0.02f, 0.08f, 0.09f));
            Voxel("M_Prop", white, false, 1f, 0f, 0f, 0.6f);
            Voxel("M_PropTextured", atlas, false, 1f, 0f, 0f, 0.6f);
            var item = Voxel("M_Item", white, false, 1f, 0f, 0f, 0f);
            Specular(item, new Color(0.18f, 0.18f, 0.2f, 0.35f));

            // Personagens (brilho moderado nos metais, sem cromado). Uma fração do próprio albedo como emissão
            // mantém os personagens legíveis na noite azulada, como no vídeo (eles se destacam do cenário).
            Material Character(string name, string skin, float lift)
            {
                var tex = Skin(skin);
                var m = Voxel(name, tex, false, 0f, 0f, 0f, name == "M_Hero" || name == "M_HeroMoss" ? 0.2f : 0.3f);
                Emission(m, tex, new Color(lift, lift, lift));
                return m;
            }
            var hero = Character("M_Hero", "hero", 0.24f);
            Specular(hero, new Color(0.22f, 0.22f, 0.25f, 0.42f));
            var heroMoss = Character("M_HeroMoss", "hero_moss", 0.2f);
            Specular(heroMoss, new Color(0.12f, 0.12f, 0.12f, 0.3f));
            Character("M_Zombie", "zombie", 0.16f);
            Character("M_Skeleton", "skeleton", 0.2f);
            var brute = Character("M_Brute", "brute", 0.14f);
            Specular(brute, new Color(0.08f, 0.08f, 0.08f, 0.2f));
            Character("M_Vine", "vine", 0.14f);
            Character("M_Llama", "llama", 0.2f);

            // Emissivos opacos
            Glow("M_GlowCyan", "Ruinas/GlowOpaque", white, new Color(0.62f, 1f, 0.94f), 3.2f);
            Glow("M_GlowWhite", "Ruinas/GlowOpaque", white, new Color(1.6f, 2f, 1.95f), 1f);
            Glow("M_GlowEyes", "Ruinas/GlowOpaque", white, new Color(1.4f, 1.9f, 1.9f), 1f);
            Glow("M_GlowGreen", "Ruinas/GlowOpaque", white, new Color(0.35f, 1.6f, 0.4f), 1.6f);
            Glow("M_GlowFire", "Ruinas/GlowOpaque", white, new Color(2.4f, 0.9f, 0.25f), 1.4f);
            Glow("M_GlowMagenta", "Ruinas/GlowOpaque", white, new Color(1.8f, 0.4f, 2.2f), 1.2f);

            // Aditivos
            var add = Glow("M_AddSoft", "Ruinas/GlowAdditive", soft, Color.white, 1f);
            add.SetFloat("_Cull", 0f);
            var addSq = Glow("M_AddSquare", "Ruinas/GlowAdditive", square, Color.white, 1f);
            addSq.SetFloat("_Shade", 0.5f);
            var addBeam = Glow("M_AddBeam", "Ruinas/GlowAdditive", beam, new Color(2.2f, 0.5f, 2.6f), 1f);
            addBeam.SetFloat("_FadeBottom", 0.04f);
            addBeam.SetFloat("_FadeTop", 0.45f);
            addBeam.SetFloat("_ScrollSpeed", 0.7f);
            var gate = Glow("M_GateField", "Ruinas/GlowAdditive", beam, new Color(1.4f, 0.4f, 2.4f), 1.4f);
            gate.SetFloat("_FadeTop", 0.9f);
            gate.SetFloat("_ScrollSpeed", 0.4f);
            Glow("M_AddRing", "Ruinas/GlowAdditive", ring, Color.white, 1.4f);
            var trail = Glow("M_Trail", "Ruinas/GlowAdditive", white, Color.white, 1.2f);
            trail.SetFloat("_Cull", 0f);
            var lootBeam = Glow("M_LootBeam", "Ruinas/GlowAdditive", beam, Color.white, 0.8f);
            lootBeam.SetFloat("_FadeBottom", 0.05f);
            lootBeam.SetFloat("_FadeTop", 0.6f);
            lootBeam.SetFloat("_ScrollSpeed", 0.3f);
            var bubble = Glow("M_Bubble", "Ruinas/FresnelBubble", noise, new Color(2.2f, 0.55f, 2.6f), 1f);
            bubble.SetFloat("_RimPower", 2.2f);
            bubble.SetFloat("_InnerAlpha", 0.2f);
            var tele = Glow("M_Telegraph", "Ruinas/GroundRing", white, new Color(1f, 0.28f, 0.2f, 0.85f), 1f);
            tele.SetFloat("_RingRadius", 0.47f);
            tele.SetFloat("_RingWidth", 0.028f);
            tele.SetFloat("_InnerAlpha", 0.26f);

            // Partículas
            var pc = Glow("M_ParticleCube", "Ruinas/ParticleCube", square, Color.white, 1f);
            pc.SetFloat("_Shade", 0.85f);
            var ps = Glow("M_ParticleSoft", "Ruinas/ParticleCube", soft, Color.white, 1f);
            ps.SetFloat("_Shade", 0f);
            ps.SetFloat("_Cull", 0f);

            AssetDatabase.SaveAssets();
        }
    }
}

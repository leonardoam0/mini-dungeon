using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ruinas.EditorTools
{
    /// <summary>
    /// Configuração reprodutível do projeto: jogador (Windows x64, cor linear, Input System), camadas,
    /// três níveis de qualidade URP (Forward+, sombras suaves, SSAO) e matriz de colisão.
    /// </summary>
    public static class ProjectSetup
    {
        public const string UrpDir = "Assets/_Project/Settings/URP";
        public static readonly string[] QualityNames = { "Baixa", "Média", "Alta" };

        public static void Apply()
        {
            ConfigurePlayer();
            ConfigureLayers();
            ConfigureRendering();
            Layers.ConfigureCollisionMatrix();
            Time.fixedDeltaTime = 1f / 60f;
            AssetDatabase.SaveAssets();
            Debug.Log("[Forge] ProjectSetup aplicado.");
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Projeto Autoral";
            PlayerSettings.productName = "Ruínas do Obelisco";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.enableFrameTimingStats = true;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);

            // Somente o Input System (nenhum código usa UnityEngine.Input).
            var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (ps != null && ps.Length > 0)
            {
                var so = new SerializedObject(ps[0]);
                var prop = so.FindProperty("activeInputHandler");
                if (prop != null) prop.intValue = 1;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void ConfigureLayers()
        {
            var tm = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tm == null || tm.Length == 0) return;
            var so = new SerializedObject(tm[0]);
            var layers = so.FindProperty("layers");
            for (int i = 6; i < Layers.Names.Length; i++)
                if (!string.IsNullOrEmpty(Layers.Names[i])) layers.GetArrayElementAtIndex(i).stringValue = Layers.Names[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        struct Tier
        {
            public string name;
            public int msaa, mainShadow, addShadow, perObject;
            public float scale, shadowDist;
            public bool addShadows, ssao, soft;
            public int softQuality; // 1 baixa, 2 média, 3 alta
        }

        static void ConfigureRendering()
        {
            Directory.CreateDirectory(UrpDir);
            var tiers = new[]
            {
                // A câmera fica a ~46 unidades do foco: a primeira cascata precisa alcançar o plano do jogador.
                new Tier { name = "Baixa", msaa = 1, mainShadow = 1024, addShadow = 1024, perObject = 4, scale = 0.85f, shadowDist = 72f, addShadows = false, ssao = false, soft = true, softQuality = 1 },
                new Tier { name = "Media", msaa = 2, mainShadow = 2048, addShadow = 2048, perObject = 6, scale = 1f, shadowDist = 82f, addShadows = true, ssao = true, soft = true, softQuality = 2 },
                new Tier { name = "Alta", msaa = 4, mainShadow = 4096, addShadow = 4096, perObject = 8, scale = 1f, shadowDist = 88f, addShadows = true, ssao = true, soft = true, softQuality = 3 },
            };
            var assets = new UniversalRenderPipelineAsset[tiers.Length];
            for (int i = 0; i < tiers.Length; i++) assets[i] = CreateTier(tiers[i]);

            GraphicsSettings.defaultRenderPipeline = assets[2];
            var qs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (qs != null && qs.Length > 0)
            {
                var so = new SerializedObject(qs[0]);
                var levels = so.FindProperty("m_QualitySettings");
                levels.arraySize = 3;
                for (int i = 0; i < 3; i++)
                {
                    var el = levels.GetArrayElementAtIndex(i);
                    el.FindPropertyRelative("name").stringValue = QualityNames[i];
                    var rp = el.FindPropertyRelative("customRenderPipeline");
                    if (rp != null) rp.objectReferenceValue = assets[i];
                    var vs = el.FindPropertyRelative("vSyncCount");
                    if (vs != null) vs.intValue = 1;
                    var aa = el.FindPropertyRelative("antiAliasing");
                    if (aa != null) aa.intValue = 0;
                    var lod = el.FindPropertyRelative("lodBias");
                    if (lod != null) lod.floatValue = 2f;
                }
                var current = so.FindProperty("m_CurrentQuality");
                if (current != null) current.intValue = 2;
                var perPlatform = so.FindProperty("m_PerPlatformDefaultQuality");
                if (perPlatform != null)
                {
                    for (int i = 0; i < perPlatform.arraySize; i++)
                    {
                        var pair = perPlatform.GetArrayElementAtIndex(i);
                        var second = pair.FindPropertyRelative("second");
                        if (second != null) second.intValue = 2;
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            QualitySettings.SetQualityLevel(2, true);
        }

        static UniversalRenderPipelineAsset CreateTier(Tier t)
        {
            string rdPath = $"{UrpDir}/URP_{t.name}_Renderer.asset";
            string assetPath = $"{UrpDir}/URP_{t.name}.asset";

            var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rdPath);
            if (rd == null)
            {
                rd = ScriptableObject.CreateInstance<UniversalRendererData>();
                rd.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                AssetDatabase.CreateAsset(rd, rdPath);
            }
            rd.renderingMode = RenderingMode.ForwardPlus;
            rd.depthPrimingMode = DepthPrimingMode.Disabled;
            rd.copyDepthMode = CopyDepthMode.AfterOpaques;
            EnsureSsao(rd, t.ssao);
            EditorUtility.SetDirty(rd);

            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (asset == null)
            {
                asset = UniversalRenderPipelineAsset.Create(rd);
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            asset.supportsHDR = true;
            asset.msaaSampleCount = t.msaa;
            asset.renderScale = t.scale;
            asset.mainLightShadowmapResolution = t.mainShadow;
            asset.shadowDistance = t.shadowDist;
            asset.shadowCascadeCount = 2;
            asset.cascade2Split = 0.66f;
            asset.shadowDepthBias = 1.1f;
            asset.shadowNormalBias = 0.8f;
            asset.additionalLightsShadowmapResolution = t.addShadow;
            asset.useSRPBatcher = true;
            asset.supportsCameraDepthTexture = true;
            asset.supportsCameraOpaqueTexture = false;
            asset.colorGradingMode = ColorGradingMode.LowDynamicRange;

            var so = new SerializedObject(asset);
            void SetInt(string name, int v) { var p = so.FindProperty(name); if (p != null) p.intValue = v; }
            void SetBool(string name, bool v) { var p = so.FindProperty(name); if (p != null) p.boolValue = v; }
            SetBool("m_MainLightShadowsSupported", true);
            SetBool("m_SoftShadowsSupported", t.soft);
            SetInt("m_AdditionalLightsRenderingMode", (int)LightRenderingMode.PerPixel);
            SetBool("m_AdditionalLightShadowsSupported", t.addShadows);
            SetInt("m_SoftShadowQuality", t.softQuality);
            SetInt("m_AdditionalLightsPerObjectLimit", t.perObject);
            SetBool("m_SupportsLightCookies", false);
            SetBool("m_SupportsTerrainHoles", false);
            SetBool("m_MixedLightingSupported", false);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static void EnsureSsao(UniversalRendererData rd, bool enabled)
        {
            ScreenSpaceAmbientOcclusion ssao = null;
            foreach (var f in rd.rendererFeatures) if (f is ScreenSpaceAmbientOcclusion s) ssao = s;
            if (ssao == null)
            {
                ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
                ssao.name = "SSAO";
                AssetDatabase.AddObjectToAsset(ssao, rd);
                rd.rendererFeatures.Add(ssao);
                var rso = new SerializedObject(rd);
                var map = rso.FindProperty("m_RendererFeatureMap");
                if (map != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ssao, out string _, out long localId))
                {
                    map.arraySize = rd.rendererFeatures.Count;
                    map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                }
                rso.ApplyModifiedPropertiesWithoutUndo();
            }
            ssao.SetActive(enabled);
            var so = new SerializedObject(ssao);
            void F(string n, float v) { var p = so.FindProperty("m_Settings." + n); if (p != null) p.floatValue = v; }
            void B(string n, bool v) { var p = so.FindProperty("m_Settings." + n); if (p != null) p.boolValue = v; }
            void E(string n, int v) { var p = so.FindProperty("m_Settings." + n); if (p != null) p.enumValueIndex = v; }
            F("Intensity", 1.15f);
            F("Radius", 0.42f);
            F("DirectLightingStrength", 0.35f);
            F("Falloff", 60f);
            B("Downsample", false);
            B("AfterOpaque", false);
            E("AOMethod", 1);     // InterleavedGradient: ruído fixo por pixel (sem cintilação entre quadros; capturas repetíveis)
            E("Source", 1);       // DepthNormals
            E("NormalSamples", 1);
            E("Samples", 1);      // Medium
            E("BlurQuality", 0);  // High (bilateral)
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ssao);
        }
    }
}

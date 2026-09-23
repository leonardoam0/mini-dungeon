// Shader principal do cenário e dos personagens de "Ruínas do Obelisco".
// Base: passes do Simple Lit do URP 17.6, acrescidos de:
//  - oclusão ambiente e variação de tom por vértice (cor do vértice: rgb = tinta, a = AO);
//  - névoa de altura para dar profundidade aos níveis inferiores;
//  - recorte com dithering da geometria que oculta o jogador;
//  - flash de impacto controlado por MaterialPropertyBlock.
Shader "Ruinas/VoxelLit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Cutoff("Alpha Clipping", Range(0.0, 1.0)) = 0.5
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clip", Float) = 0.0

        [Toggle(_SPECULAR_COLOR)] _UseSpecular("Specular", Float) = 0.0
        _SpecColor("Specular Color (A = smoothness)", Color) = (0.2, 0.2, 0.2, 0.4)

        [Toggle(_EMISSION)] _UseEmission("Emission", Float) = 0.0
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        [NoScaleOffset] _EmissionMap("Emission Map", 2D) = "white" {}

        _AOStrength("Vertex AO on Albedo", Range(0, 1)) = 1.0
        _AOIndirect("Vertex AO on Ambient", Range(0, 1)) = 0.6
        _TintStrength("Vertex Tint", Range(0, 1)) = 1.0
        [ToggleUI] _SeeThrough("See-through Cutout", Float) = 0.0
        _HeightFogAffect("Height Fog Affect", Range(0, 1)) = 1.0

        _HitFlash("Hit Flash", Range(0, 1)) = 0.0
        _FlashColor("Flash Color", Color) = (1, 1, 1, 1)

        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2.0
        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "SimpleLit"
            "IgnoreProjector" = "True"
            "Queue" = "Geometry"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex VoxelLitVertex
            #pragma fragment VoxelLitFragment

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _SPECULAR_COLOR
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #define RUINAS_FORWARD_PASS 1
            #include "VoxelLitInput.hlsl"
            #include "VoxelLitPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "VoxelLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex VoxelDepthOnlyVertex
            #pragma fragment VoxelDepthOnlyFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing

            #define RUINAS_DEPTH_ONLY_PASS 1
            #include "VoxelLitInput.hlsl"
            #include "VoxelLitPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex VoxelDepthNormalsVertex
            #pragma fragment VoxelDepthNormalsFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            #define RUINAS_DEPTH_NORMALS_PASS 1
            #include "VoxelLitInput.hlsl"
            #include "VoxelLitPasses.hlsl"
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}

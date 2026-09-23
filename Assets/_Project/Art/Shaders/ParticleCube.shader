// Partículas (sprites ou cubos) com mistura alfa e sombreamento falso por face.
// Usado nas nuvens verdes, poeira, destroços cúbicos e fumaça.
Shader "Ruinas/ParticleCube"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        [HDR] _Color("Color", Color) = (1, 1, 1, 1)
        _Intensity("Intensity", Float) = 1
        _Shade("Fake Shade", Range(0, 1)) = 0.8
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "ParticleCube"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex EffectVertex
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "RuinasEffects.hlsl"

            half4 frag(EffectVaryings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                half3 c = tex.rgb * _Color.rgb * i.color.rgb * _Intensity * EffectFakeShade(i.normalWS);
                half a = tex.a * _Color.a * i.color.a;
                return half4(c, a);
            }
            ENDHLSL
        }
    }
}

// Emissivo opaco sem iluminação: contorno luminoso da arena, faces de lanternas, núcleos brilhantes.
// Escreve profundidade para não "flutuar" sobre a geometria; o bloom cuida do halo.
Shader "Ruinas/GlowOpaque"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        [HDR] _Color("Color", Color) = (1, 1, 1, 1)
        _Intensity("Intensity", Float) = 1
        _Shade("Fake Shade", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+5" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "GlowOpaque"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back

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
                return half4(c, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex EffectVertex
            #pragma fragment fragDepth
            #include "RuinasEffects.hlsl"
            half fragDepth(EffectVaryings i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex EffectVertex
            #pragma fragment fragDN
            #include "RuinasEffects.hlsl"
            half4 fragDN(EffectVaryings i) : SV_Target { return half4(normalize(i.normalWS), 0); }
            ENDHLSL
        }
    }
}

// Esfera translúcida do pulso magenta: centro claro, borda colorida e intensa.
Shader "Ruinas/FresnelBubble"
{
    Properties
    {
        _BaseMap("Noise", 2D) = "white" {}
        [HDR] _Color("Rim Color", Color) = (2.2, 0.6, 2.6, 1)
        _Intensity("Intensity", Float) = 1
        _RimPower("Rim Power", Range(0.5, 8)) = 2.4
        _InnerAlpha("Inner Alpha", Range(0, 1)) = 0.18
        _ScrollSpeed("Noise Scroll", Float) = 0.4
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+20" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Bubble"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
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
                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                half rim = pow(1 - saturate(dot(n, v)), _RimPower);
                float2 uv = i.uv * float2(3, 2);
                uv.y += _RuinasTime * _ScrollSpeed;
                half noise = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).r;
                half body = _InnerAlpha * (0.65 + 0.7 * noise);
                half3 inner = lerp(half3(1, 0.85, 1), _Color.rgb, 0.35);
                half3 c = (inner * body + _Color.rgb * rim * (0.8 + 0.4 * noise)) * _Intensity * i.color.rgb * i.color.a;
                return half4(c, 0);
            }
            ENDHLSL
        }
    }
}

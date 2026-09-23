// Indicador no chão (aviso de ataque, área de efeito). Desenhado em um quad horizontal:
// anel com traços opcionais + disco que se preenche até o instante do golpe.
Shader "Ruinas/GroundRing"
{
    Properties
    {
        [HDR] _Color("Color", Color) = (1, 0.25, 0.2, 1)
        _Intensity("Intensity", Float) = 1
        _RingRadius("Ring Radius", Range(0, 0.5)) = 0.47
        _RingWidth("Ring Width", Range(0.001, 0.2)) = 0.03
        _Fill("Fill", Range(0, 1)) = 0
        _Dashes("Dashes", Float) = 0
        _DashSpin("Dash Spin", Float) = 0.3
        _InnerAlpha("Disc Alpha", Range(0, 1)) = 0.28
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Ring"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex EffectVertex
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "RuinasEffects.hlsl"

            half4 frag(EffectVaryings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float2 p = i.uv - 0.5;
                float r = length(p);
                // Pixeliza levemente o raio para manter a linguagem cúbica.
                float px = 1.0 / 48.0;
                float rq = floor(r / px) * px;
                half ring = step(abs(rq - _RingRadius), _RingWidth);
                if (_Dashes > 0.5)
                {
                    float ang = atan2(p.y, p.x) / 6.2831853 + 0.5 + _RuinasTime * _DashSpin;
                    ring *= step(0.45, frac(ang * _Dashes));
                }
                half disc = step(rq, _RingRadius * _Fill) * _InnerAlpha;
                half outer = step(rq, _RingRadius) * 0.08;
                half a = saturate(max(ring, max(disc, outer))) * _Color.a * i.color.a;
                return half4(_Color.rgb * _Intensity * i.color.rgb, a);
            }
            ENDHLSL
        }
    }
}

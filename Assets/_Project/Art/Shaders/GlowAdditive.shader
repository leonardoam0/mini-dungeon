// Brilho aditivo para feixes, cartões de luz, flashes, brilho de loot e partículas luminosas.
Shader "Ruinas/GlowAdditive"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        [HDR] _Color("Color", Color) = (1, 1, 1, 1)
        _Intensity("Intensity", Float) = 1
        _FadeBottom("Fade Bottom", Range(0, 1)) = 0
        _FadeTop("Fade Top", Range(0, 1)) = 0
        _FadeSides("Fade Sides", Range(0, 0.5)) = 0
        _ScrollSpeed("Scroll Speed", Float) = 0
        _Shade("Fake Shade", Range(0, 1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Glow"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
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
                float2 uv = i.uv;
                uv.y -= _RuinasTime * _ScrollSpeed;
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half fade = EffectEdgeFade(i.uv);
                half3 c = tex.rgb * _Color.rgb * i.color.rgb * _Intensity * EffectFakeShade(i.normalWS);
                c *= tex.a * i.color.a * _Color.a * fade;
                return half4(c, 0);
            }
            ENDHLSL
        }
    }
}

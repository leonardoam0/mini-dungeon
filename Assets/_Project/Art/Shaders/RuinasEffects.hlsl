#ifndef RUINAS_EFFECTS_INCLUDED
#define RUINAS_EFFECTS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _Color;
    half _Intensity;
    half _FadeBottom;     // esmaece perto de uv.y = 0 (feixes que "nascem" do chão)
    half _FadeTop;        // esmaece perto de uv.y = 1
    half _FadeSides;      // esmaece nas laterais (uv.x)
    half _ScrollSpeed;    // rolagem vertical da textura
    half _Shade;          // sombreamento falso por normal (partículas cúbicas)
    half _RimPower;       // bolha fresnel
    half _InnerAlpha;     // bolha fresnel: opacidade do interior
    half _RingRadius;     // anel: raio (0..0.5 em UV)
    half _RingWidth;      // anel: espessura
    half _Fill;           // anel: preenchimento do disco (0..1)
    half _Dashes;         // anel: número de traços (0 = contínuo)
    half _DashSpin;       // anel: velocidade de giro dos traços
CBUFFER_END

// Relógio das animações dos efeitos, enviado pelo jogo antes de cada quadro (DeterministicVfx): igual a Time.time
// na jogabilidade e começando em zero com o roteiro na captura determinística.
float _RuinasTime;

struct EffectAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float2 uv         : TEXCOORD0;
    half4  color      : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct EffectVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    half4  color      : TEXCOORD1;
    float3 normalWS   : TEXCOORD2;
    float3 positionWS : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

EffectVaryings EffectVertex(EffectAttributes input)
{
    EffectVaryings o = (EffectVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    o.positionCS = TransformWorldToHClip(o.positionWS);
    o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    o.color = input.color;
    o.normalWS = TransformObjectToWorldNormal(input.normalOS);
    return o;
}

half EffectEdgeFade(float2 uv)
{
    half f = 1;
    if (_FadeBottom > 0) f *= saturate(uv.y / _FadeBottom);
    if (_FadeTop > 0) f *= saturate((1 - uv.y) / _FadeTop);
    if (_FadeSides > 0) f *= saturate(min(uv.x, 1 - uv.x) / _FadeSides);
    return f;
}

// Sombreamento falso: topo claro, laterais médias, base escura (lê como cubo sólido).
half EffectFakeShade(float3 normalWS)
{
    float3 n = normalize(normalWS);
    half s = 0.62 + 0.38 * saturate(n.y) + 0.14 * saturate(dot(n, normalize(float3(-0.6, 0.2, 0.35))));
    return lerp(1, s, _Shade);
}

#endif

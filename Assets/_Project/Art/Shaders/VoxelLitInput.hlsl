#ifndef RUINAS_VOXEL_LIT_INPUT_INCLUDED
#define RUINAS_VOXEL_LIT_INPUT_INCLUDED

// Impede que algum include do URP traga o SimpleLitInput com um CBUFFER diferente.
#define UNIVERSAL_SIMPLE_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/SurfaceType.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseMap_TexelSize;
    half4 _BaseColor;
    half4 _SpecColor;
    half4 _EmissionColor;
    half4 _FlashColor;
    half _Cutoff;
    half _AOStrength;
    half _AOIndirect;
    half _TintStrength;
    half _SeeThrough;
    half _HitFlash;
    half _HeightFogAffect;
CBUFFER_END

// Globais definidos em runtime (AtmosphereController / CameraRig). Ficam fora do CBUFFER
// para manter a compatibilidade com o SRP Batcher.
float4 _RuinasSeeThrough;        // xyz: posição do jogador (peito), w: raio em fração da altura da tela
float4 _RuinasSeeThroughParams;  // x: ativo, y: aspecto, z: altura mínima acima do jogador, w: margem de profundidade
float4 _RuinasHeightFogColor;
float4 _RuinasHeightFogParams;   // x: y de referência, y: faixa, z: intensidade máxima, w: ativo

static const float kRuinasBayer4[16] =
{
     0.0,  8.0,  2.0, 10.0,
    12.0,  4.0, 14.0,  6.0,
     3.0, 11.0,  1.0,  9.0,
    15.0,  7.0, 13.0,  5.0
};

half4 RuinasSampleSpecular(half4 specColor)
{
    half4 specularSmoothness = half4(0, 0, 0, 1);
#if defined(_SPECULAR_COLOR)
    specularSmoothness = specColor;
#endif
    return specularSmoothness;
}

// Dither ordenado 4x4 usado no recorte de oclusão.
float RuinasBayer4(float2 pixel)
{
    uint2 p = uint2(pixel) & 3u;
    return (kRuinasBayer4[p.y * 4u + p.x] + 0.5) / 16.0;
}

// Recorta a geometria que fica entre a câmera e o jogador, perto dele na tela.
void RuinasSeeThroughClip(float3 positionWS, float4 positionCSPixel)
{
    if (_SeeThrough < 0.5 || _RuinasSeeThroughParams.x < 0.5) return;

    float3 playerWS = _RuinasSeeThrough.xyz;
    if (positionWS.y < playerWS.y + _RuinasSeeThroughParams.z) return;

    float fragDepth = -TransformWorldToView(positionWS).z;
    float playerDepth = -TransformWorldToView(playerWS).z;
    if (fragDepth > playerDepth - _RuinasSeeThroughParams.w) return;

    float4 pCS = TransformWorldToHClip(playerWS);
    float4 fCS = TransformWorldToHClip(positionWS);
    float2 d = (fCS.xy / fCS.w - pCS.xy / pCS.w) * float2(_RuinasSeeThroughParams.y, 1.0);
    float dist = length(d) * 0.5;
    float r = _RuinasSeeThrough.w;
    if (dist > r) return;

    float fade = saturate((r - dist) / max(r * 0.35, 1e-4));
    clip(RuinasBayer4(positionCSPixel.xy) - fade * 0.97);
}

half3 RuinasApplyHeightFog(half3 color, float3 positionWS)
{
    if (_RuinasHeightFogParams.w < 0.5) return color;
    float h = saturate((_RuinasHeightFogParams.x - positionWS.y) / max(_RuinasHeightFogParams.y, 1e-3));
    h = h * h * (3.0 - 2.0 * h);
    return lerp(color, (half3)_RuinasHeightFogColor.rgb, (half)(h * _RuinasHeightFogParams.z * _HeightFogAffect));
}

inline void InitializeVoxelSurfaceData(float2 uv, half4 vertexColor, out SurfaceData outSurfaceData)
{
    outSurfaceData = (SurfaceData)0;

    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);

    half3 tint = lerp(half3(1, 1, 1), vertexColor.rgb, _TintStrength);
    half ao = lerp(half(1), vertexColor.a, _AOStrength);
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb * tint * ao;

    half4 specularSmoothness = RuinasSampleSpecular(_SpecColor);
    outSurfaceData.metallic = 0;
    outSurfaceData.specular = specularSmoothness.rgb;
    outSurfaceData.smoothness = specularSmoothness.a;
    outSurfaceData.normalTS = half3(0, 0, 1);
    outSurfaceData.occlusion = lerp(half(1), vertexColor.a, _AOIndirect);
    outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
}

#endif

#ifndef RUINAS_VOXEL_LIT_PASSES_INCLUDED
#define RUINAS_VOXEL_LIT_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// ---------------------------------------------------------------------------
// ForwardLit — Blinn-Phong do URP com AO/tinta por vértice, névoa de altura e flash.
// ---------------------------------------------------------------------------
#if defined(RUINAS_FORWARD_PASS)

struct Attributes
{
    float4 positionOS        : POSITION;
    float3 normalOS          : NORMAL;
    float4 tangentOS         : TANGENT;
    float2 texcoord          : TEXCOORD0;
    float2 staticLightmapUV  : TEXCOORD1;
    half4  color             : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                : TEXCOORD0;
    float3 positionWS        : TEXCOORD1;
    half3  normalWS          : TEXCOORD2;
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    half4 fogFactorAndVertexLight : TEXCOORD3;
#else
    half  fogFactor          : TEXCOORD3;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord       : TEXCOORD4;
#endif
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
    half4 color              : TEXCOORD6;
#ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion    : TEXCOORD7;
#endif
    float4 positionCS        : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings VoxelLitVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

#if defined(_FOG_FRAGMENT)
    half fogFactor = 0;
#else
    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
#endif

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionWS = vertexInput.positionWS;
    output.positionCS = vertexInput.positionCS;
    output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);
    output.color = input.color;

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
    OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
#else
    output.fogFactor = fogFactor;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif
    return output;
}

void VoxelLitFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    RuinasSeeThroughClip(input.positionWS, input.positionCS);

    SurfaceData surfaceData;
    InitializeVoxelSurfaceData(input.uv, input.color, surfaceData);

    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
#if defined(DEBUG_DISPLAY)
    inputData.positionCS = input.positionCS;
#endif
    inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
    inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceNormalizeViewDir(input.positionWS));

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#else
    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactor);
    inputData.vertexLighting = half3(0, 0, 0);
#endif
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

#if !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.vertexSH,
        GetAbsolutePositionWS(inputData.positionWS),
        inputData.normalWS,
        inputData.viewDirectionWS,
        input.positionCS.xy,
        input.probeOcclusion,
        inputData.shadowMask);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#endif

    half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);
    color.rgb = RuinasApplyHeightFog(color.rgb, input.positionWS);
    color.rgb = lerp(color.rgb, _FlashColor.rgb, _HitFlash);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent());
    outColor = color;

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif // RUINAS_FORWARD_PASS

// ---------------------------------------------------------------------------
// DepthOnly — com o mesmo recorte de oclusão do passe principal.
// ---------------------------------------------------------------------------
#if defined(RUINAS_DEPTH_ONLY_PASS)

struct DepthAttributes
{
    float4 positionOS : POSITION;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct DepthVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

DepthVaryings VoxelDepthOnlyVertex(DepthAttributes input)
{
    DepthVaryings output = (DepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    return output;
}

half VoxelDepthOnlyFragment(DepthVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
#if defined(_ALPHATEST_ON)
    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
#endif
    RuinasSeeThroughClip(input.positionWS, input.positionCS);
    return input.positionCS.z;
}

#endif // RUINAS_DEPTH_ONLY_PASS

// ---------------------------------------------------------------------------
// DepthNormals — necessário para o SSAO.
// ---------------------------------------------------------------------------
#if defined(RUINAS_DEPTH_NORMALS_PASS)

struct DNAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct DNVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    half3  normalWS   : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

DNVaryings VoxelDepthNormalsVertex(DNAttributes input)
{
    DNVaryings output = (DNVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.normalWS = NormalizeNormalPerVertex(TransformObjectToWorldNormal(input.normalOS));
    return output;
}

void VoxelDepthNormalsFragment(
    DNVaryings input
    , out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
#if defined(_ALPHATEST_ON)
    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
#endif
    RuinasSeeThroughClip(input.positionWS, input.positionCS);

#if defined(_GBUFFER_NORMALS_OCT)
    float3 normalWS = normalize(input.normalWS);
    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
    outNormalWS = half4(packedNormalWS, 0.0);
#else
    half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
    outNormalWS = half4(normalWS, 0.0);
#endif

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif // RUINAS_DEPTH_NORMALS_PASS

#endif

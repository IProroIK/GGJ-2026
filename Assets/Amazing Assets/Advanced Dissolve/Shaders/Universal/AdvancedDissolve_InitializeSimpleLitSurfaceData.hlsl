// Advanced Dissolve <https://u3d.as/16cX>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
#ifndef ADVANCED_DISSOLVE_INITIALIZE_LIT_SURFACE_DATA
#define ADVANCED_DISSOLVE_INITIALIZE_LIT_SURFACE_DATA


inline void AdvancedDissolve_InitializeSimpleLitSurfaceData(float2 uv, out SurfaceData outSurfaceData, float4 cutoutSource)
{
    outSurfaceData = (SurfaceData)0;

    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    outSurfaceData.alpha = albedoAlpha.a * _BaseColor.a;


    float cutout = _Cutoff;
    //Advanced Dissolve/////////////////////////////////////////
    #if defined(_AD_STATE_ENABLED) && defined(_ALPHATEST_ON)
        AdvancedDissolveCalculateAlphaAndClip(cutoutSource, outSurfaceData.alpha, cutout);
    #endif

    outSurfaceData.alpha = AlphaDiscard(outSurfaceData.alpha, cutout);

    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, outSurfaceData.alpha);

    half4 specularSmoothness = SampleSpecularSmoothness(uv, outSurfaceData.alpha, _SpecColor, TEXTURE2D_ARGS(_SpecGlossMap, sampler_SpecGlossMap));
    outSurfaceData.metallic = 0.0; // unused
    outSurfaceData.specular = specularSmoothness.rgb;
    outSurfaceData.smoothness = specularSmoothness.a;
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
    outSurfaceData.occlusion = 1.0;
    outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
}

#endif

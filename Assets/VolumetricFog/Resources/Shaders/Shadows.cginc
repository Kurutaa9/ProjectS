// Native directional shadow support functions

#if FOG_SUN_SHADOWS_ON
 sampler2D_float _VolumetricFogSunDepthTexture;
 float4 _VolumetricFogSunDepthTexture_TexelSize;
 float4x4 _VolumetricFogSunProj;
 float4 _VolumetricFogSunWorldPos;
 half4 _VolumetricFogSunShadowsData;
 float3 _VolumetricFogSunShadowsCameraWorldPos;
 
 #if defined(FOG_UNITY_DIR_SHADOWS)
 UNITY_DECLARE_SHADOWMAP(_VolumetricFogShadowMapCopy);

 #if defined(FOG_DIR_SHADOWS_COOKIE)
 float4x4 _VolumetricFogLightMatrix;
 sampler2D _VolumetricFogLightCookie;
 float _VolumetricFogCookieSize;
 #endif
                
inline fixed4 GetCascadeWeights_SplitSpheres(float3 wpos) {
     float3 fromCenter0 = wpos.xyz - unity_ShadowSplitSpheres[0].xyz;
     float3 fromCenter1 = wpos.xyz - unity_ShadowSplitSpheres[1].xyz;
     float3 fromCenter2 = wpos.xyz - unity_ShadowSplitSpheres[2].xyz;
     float3 fromCenter3 = wpos.xyz - unity_ShadowSplitSpheres[3].xyz;
     float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

     fixed4 weights = float4(distances2 < unity_ShadowSplitSqRadii);
     weights.yzw = saturate(weights.yzw - weights.xyz);
     return weights;
}

inline float4 GetCascadeShadowCoord(float4 wpos, fixed4 cascadeWeights) {
     float3 sc0 = mul(unity_WorldToShadow[0], wpos).xyz;
     float3 sc1 = mul(unity_WorldToShadow[1], wpos).xyz;
     float3 sc2 = mul(unity_WorldToShadow[2], wpos).xyz;
     float3 sc3 = mul(unity_WorldToShadow[3], wpos).xyz;
            
     float4 shadowMapCoordinate = float4(sc0 * cascadeWeights[0] + sc1 * cascadeWeights[1] + sc2 * cascadeWeights[2] + sc3 * cascadeWeights[3], 1);
#if defined(UNITY_REVERSED_Z)
     float  noCascadeWeights = 1 - dot(cascadeWeights, float4(1, 1, 1, 1));
     shadowMapCoordinate.z += noCascadeWeights;
#endif
     return shadowMapCoordinate;
}
        
inline float getShadowFade_SplitSpheres( float3 wpos ) {
    float sphereDist = distance(wpos.xyz, unity_ShadowFadeCenterAndType.xyz);
    half shadowFade = saturate(sphereDist * _LightShadowData.z + _LightShadowData.w);
    return shadowFade;  
}

#define GET_SHADOW_FADE(wpos)        getShadowFade_SplitSpheres(wpos)
        
float GetLightAttenuation(float3 wpos, float fade) {

    // get shadow cascade
    float4 cascadeWeights = GetCascadeWeights_SplitSpheres(wpos);
      
    // get shadow map texture coords
    float4 samplePos = GetCascadeShadowCoord(float4(wpos, 1), cascadeWeights);
      
    // sample shadow map
    float shadow = UNITY_SAMPLE_SHADOW(_VolumetricFogShadowMapCopy, samplePos.xyz);

    // apply shadow cookie
    #if defined(FOG_DIR_SHADOWS_COOKIE)
        float4 uvCookie = mul(_VolumetricFogLightMatrix, float4(wpos, 1));
	    float atten = tex2Dlod(_VolumetricFogLightCookie, float4(uvCookie.xy * _VolumetricFogCookieSize + 0.5.xx, 0, 0)).a;
   	    shadow *= atten;
   	#endif
      
    // apply shadow fade out
    shadow += fade;
      
    return saturate(shadow);
}

float GetShadowFade(float3 wpos) {
    return GET_SHADOW_FADE(wpos);
}

#else

// internal pass shadow support functions
 float3 getShadowCoords(float3 worldPos) {
     float4 shadowClipPos = mul(_VolumetricFogSunProj, float4(worldPos, 1.0));
     // transform from clip to texture space
     shadowClipPos.xy /= shadowClipPos.w;
     shadowClipPos.xy = shadowClipPos.xy * 0.5 + 0.5;
     shadowClipPos.z = 0;
     return shadowClipPos.xyz;
 }
 
#endif // FOG_UNITY_DIR_SHADOWS

#endif // FOG_SUN_SHADOWS_ON

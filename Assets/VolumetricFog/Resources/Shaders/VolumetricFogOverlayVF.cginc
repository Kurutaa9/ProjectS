#ifndef VOLUMETRIC_FOG_INTEGRATION_VF_INCLUDED
#define VOLUMETRIC_FOG_INTEGRATION_VF_INCLUDED

    #define OVERLAY_FOG
    #define SHADER_API_GLES 1
    #include "VolumetricFog.cginc"

    // Final fog function for Vertex/Fragment Shaders
    // Returns the final color
    half4 overlayFog(float3 worldPos, float4 screenPos, half4 color) {

        #if FOG_USE_XY_PLANE
            wsCameraPos = float3(_WorldSpaceCameraPos.x, _WorldSpaceCameraPos.y, _WorldSpaceCameraPos.z - _FogData.x);
            worldPos.z -= _FogData.x;
        #else
            wsCameraPos = float3(_WorldSpaceCameraPos.x, _WorldSpaceCameraPos.y - _FogData.x, _WorldSpaceCameraPos.z);
            worldPos.y -= _FogData.x;
        #endif

        float3 uv = screenPos.xyz / screenPos.w;
        float depth01 = Linear01Depth(uv.z);

        SetDither(uv.xy);

        half4 sum = getFogColor(worldPos, depth01);

        sum *= 1.0 + dither * _FogStepping.w;

        #if defined(FOG_DEBUG)
             color = sum;
             return;
        #endif
    
        #if FOG_BLUR_ON
             half4 blurColor = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_BlurTex, uv.xy);
             color.rgb = lerp(color.rgb, blurColor.rgb, sum.a);
        #endif

        color.rgb = color.rgb * saturate(1.0 - sum.a) + sum.rgb;

        return color;
    }

  #endif // VOLUMETRIC_FOG_INTEGRATION_VF_INCLUDED
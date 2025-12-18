#ifndef VOLUMETRIC_FOG_SURFACE
#define VOLUMETRIC_FOG_SURFACE

#if FOG_SURFACE

    float4x4 _SurfaceCaptureMatrix;
    sampler2D_float _SurfaceDepthTexture;
    float4 _SurfaceData;
    float surfaceLoopDenominator;

    #define SURFACE_CAM_ALTITUDE _SurfaceData.x
    #define TERRAIN_FOG_HEIGHT _SurfaceData.y
    #define TERRAIN_FOG_MIN_ALTITUDE _SurfaceData.z
    #define TERRAIN_FOG_MAX_ALTITUDE _SurfaceData.w

    float4 surfaceTexCoordsStart, surfaceTexCoordsEnd;

    void SurfaceComputeEndPoints(float3 wposStart, float3 wposEnd, float steps, inout float fogHeight) {
        surfaceTexCoordsStart = mul(_SurfaceCaptureMatrix, float4(wposStart, 1.0));
        surfaceTexCoordsStart.xy /= surfaceTexCoordsStart.w;
        surfaceTexCoordsStart.xy = (surfaceTexCoordsStart * 0.5) + 0.5;
        surfaceTexCoordsEnd = mul(_SurfaceCaptureMatrix, float4(wposEnd, 1.0));
        surfaceTexCoordsEnd.xy /= surfaceTexCoordsEnd.w;
        surfaceTexCoordsEnd.xy = (surfaceTexCoordsEnd * 0.5) + 0.5;
        fogHeight = TERRAIN_FOG_HEIGHT;
        surfaceLoopDenominator = 1.0 / steps;
    }

    float SurfaceApply(float y, float baseline, float step) {
        float2 surfaceTexCoords = lerp(surfaceTexCoordsStart.xy, surfaceTexCoordsEnd.xy, step * surfaceLoopDenominator);
        float surfaceDepth = tex2Dlod(_SurfaceDepthTexture, float4(surfaceTexCoords, 0, 0)).r;
        #if UNITY_REVERSED_Z
            surfaceDepth = 1.0 - surfaceDepth;
        #endif
	    float alt = clamp(SURFACE_CAM_ALTITUDE - surfaceDepth * 10000, TERRAIN_FOG_MIN_ALTITUDE, TERRAIN_FOG_MAX_ALTITUDE);
        y += (baseline - alt) / TERRAIN_FOG_HEIGHT;
        return y;
    }

#else

    #define SurfaceComputeEndPoints(wposStart, wposEnd, step, fogHeight)
    #define SurfaceApply(y, baseline, step) y

#endif // FOG_SURFACE

#endif // VOLUMETRIC_FOG_SURFACE
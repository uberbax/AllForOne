#ifndef FANTASY_CAMP_WATER_COMMON_INCLUDED
#define FANTASY_CAMP_WATER_COMMON_INCLUDED

// Shared material contract. Adapters define this for UnityPerMaterial in SRP.
#if defined(FANTASY_CAMP_WATER_SRP)
CBUFFER_START(UnityPerMaterial)
#endif
float4 _SurfaceColor;
float4 _DeepColor;
float4 _FoamColor;
float4 _RefreactionNormal_ST;
float4 _RefractionScale;
float _Distance;
float _RefractionSpeed;
float _RefractionPower;
float _NormalPower;
float _FoamAmount;
float _FoamSpeed;
float _FoamScale;
float _FoamCuttoff;
float _Smoothness;
float _HightFrequency;
float _WaveSpeed;
float _WaveAmplitude;
float _DepthEnabled;
float _RefractionEnabled;
float _FoamEnabled;
float _WavesEnabled;
float _Cull;
float _ZWrite;
#if defined(FANTASY_CAMP_WATER_SRP)
CBUFFER_END
#endif

float3 FantasyCampWaterSafeNormal(float3 value)
{
    float lengthSquared = dot(value, value);
    return lengthSquared > 1e-8 ? value * rsqrt(max(lengthSquared, 1e-8))
        : float3(0.0, 0.0, 1.0);
}

float2 FantasyCampWaterMoveUV(float2 uv, float timeSeconds, float speed, float2 scale)
{
    return uv * scale + timeSeconds * speed;
}

float2 FantasyCampWaterNormalUV(float2 uv, float timeSeconds, float direction)
{
    return FantasyCampWaterMoveUV(uv, timeSeconds,
        _RefractionSpeed * direction, _RefractionScale.xy)
        * _RefreactionNormal_ST.xy + _RefreactionNormal_ST.zw;
}

float3 FantasyCampWaterPositionOS(float3 positionOS, float timeSeconds)
{
    if (_WavesEnabled > 0.5)
        positionOS.y += sin(timeSeconds * _WaveSpeed
            + positionOS.x * (_HightFrequency * 6.28)) * _WaveAmplitude;
    return positionOS;
}

// Same gradient-noise construction as the source graph's Gradient Noise node.
// Float precision is required for the permutation polynomial.
float2 FantasyCampWaterNoiseDirection(float2 cell)
{
    cell = fmod(cell, 289.0);
    float hashValue = fmod((34.0 * cell.x + 1.0) * cell.x, 289.0) + cell.y;
    hashValue = fmod((34.0 * hashValue + 1.0) * hashValue, 289.0);
    hashValue = frac(hashValue / 41.0) * 2.0 - 1.0;
    float2 direction = float2(hashValue - floor(hashValue + 0.5), abs(hashValue) - 0.5);
    return direction * rsqrt(max(dot(direction, direction), 1e-8));
}

float FantasyCampWaterGradientNoise(float2 uv)
{
    float2 cell = floor(uv);
    float2 localUV = frac(uv);
    float noise00 = dot(FantasyCampWaterNoiseDirection(cell), localUV);
    float noise01 = dot(FantasyCampWaterNoiseDirection(cell + float2(0, 1)), localUV - float2(0, 1));
    float noise10 = dot(FantasyCampWaterNoiseDirection(cell + float2(1, 0)), localUV - float2(1, 0));
    float noise11 = dot(FantasyCampWaterNoiseDirection(cell + float2(1, 1)), localUV - float2(1, 1));
    float2 blendUV = localUV * localUV * localUV
        * (localUV * (localUV * 6.0 - 15.0) + 10.0);
    return lerp(lerp(noise00, noise01, blendUV.y),
        lerp(noise10, noise11, blendUV.y), blendUV.x) + 0.5;
}

float FantasyCampWaterDepthFade(float depthDifference, float distance)
{
    return saturate(max(depthDifference, 0.0) / max(distance, 0.0001));
}

// Unlike raw screen-position W, this also works for orthographic cameras.
float FantasyCampWaterEyeDepth(float rawDepth)
{
    float linear01 = rawDepth;
#if defined(UNITY_REVERSED_Z)
    linear01 = 1.0 - linear01;
#endif
    float orthoDepth = lerp(_ProjectionParams.y, _ProjectionParams.z, linear01);
    float perspectiveDepth = rcp(max(_ZBufferParams.z * rawDepth + _ZBufferParams.w, 1e-8));
    return lerp(perspectiveDepth, orthoDepth, unity_OrthoParams.w);
}

struct FantasyCampWaterSurface
{
    float4 color;
    float3 normalTS;
    float2 refractionOffset;
};

FantasyCampWaterSurface FantasyCampEvaluateWater(float2 uv, float timeSeconds,
    float3 normalA, float3 normalB, float depthDifference)
{
    FantasyCampWaterSurface result;
    float depthFade = _DepthEnabled > 0.5
        ? FantasyCampWaterDepthFade(depthDifference, _Distance) : 1.0;
    result.color = lerp(_SurfaceColor, _DeepColor, depthFade);

    if (_DepthEnabled > 0.5 && _FoamEnabled > 0.5 && _FoamAmount > 0.0)
    {
        float2 foamUV = FantasyCampWaterMoveUV(uv, timeSeconds, _FoamSpeed,
            float2(_FoamScale, _FoamScale));
        float noiseValue = FantasyCampWaterGradientNoise(foamUV);
        float foamDepth = FantasyCampWaterDepthFade(depthDifference, _FoamAmount);
        float foamMask = step(foamDepth * _FoamCuttoff, noiseValue) * saturate(_FoamColor.a);
        result.color = lerp(result.color, _FoamColor, foamMask);
    }

    float3 blendedNormal = FantasyCampWaterSafeNormal(
        float3(normalA.xy + normalB.xy, normalA.z * normalB.z));
    float strength = max(_NormalPower, 0.0) * depthFade;
    // Keep signed XY; the original graph's Saturate node distorted half the waves.
    result.normalTS = FantasyCampWaterSafeNormal(float3(blendedNormal.xy * strength,
        lerp(1.0, blendedNormal.z, saturate(strength))));
    // Preserve the graph's artistic multiplier and existing material values.
    result.refractionOffset = blendedNormal.xy * (_RefractionPower * 14.0);
    return result;
}

// Reject off-screen distortion instead of dragging the screen border over water.
float2 FantasyCampWaterRefractionUV(float2 screenUV, float2 distortion, float2 texelSize)
{
    float2 candidate = screenUV + distortion;
    float2 border = max(abs(texelSize) * 0.5, float2(0.00001, 0.00001));
    bool inside = all(candidate >= border) && all(candidate <= 1.0 - border);
    return inside ? candidate : clamp(screenUV, border, 1.0 - border);
}

float3 FantasyCampWaterComposite(float4 waterColor, float3 sceneColor)
{
    // Color alpha is density INSIDE this composition. Final pass alpha stays one.
    return _RefractionEnabled > 0.5
        ? lerp(sceneColor, waterColor.rgb, saturate(waterColor.a))
        : waterColor.rgb;
}
#endif

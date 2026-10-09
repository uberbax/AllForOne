#ifndef FANTASY_CAMP_SURFACE_COMMON_INCLUDED
#define FANTASY_CAMP_SURFACE_COMMON_INCLUDED

// Pipeline-independent surface math shared by the Built-in, URP and HDRP
// adapters. Callers pass coordinates in the space required by each function,
// so this file remains independent of pipeline transform helpers.

// Measure coverage along world Y in world units from the renderer pivot.
// Translation is excluded; rotation and non-uniform/negative scale still apply.
float FantasyCampMossHeightFromPivot(float3 positionOS, float3x3 objectToWorld)
{
    return mul(objectToWorld, positionOS).y;
}

float FantasyCampMossMask(
    float surfaceHeight,
    float groundHeight,
    float mossHeight,
    float edgeSoftness,
    float intensity)
{
    if (mossHeight <= 0.0 || intensity <= 0.0)
        return 0.0;

    float safeSoftness = min(max(edgeSoftness, 0.0001), mossHeight);
    float mossTop = groundHeight + mossHeight;
    float fadeStart = mossTop - safeSoftness;
    float heightMask = 1.0 - smoothstep(fadeStart, mossTop, surfaceHeight);
    return saturate(heightMask * intensity);
}

float3 FantasyCampTriplanarWeights(float3 geometricNormalWS, float sharpness, float seamSmoothing)
{
    float3 weights = pow(abs(normalize(geometricNormalWS)), max(sharpness, 1.0));
    weights /= max(weights.x + weights.y + weights.z, 0.0001);
    // Equal weighting is completely continuous between differently oriented
    // polygons. Blending toward it softens seams without losing all direction.
    return lerp(weights, float3(1.0 / 3.0, 1.0 / 3.0, 1.0 / 3.0), saturate(seamSmoothing));
}

float3 FantasyCampApplyDetail(
    float3 baseColor,
    float4 detailSample,
    float3 detailTint,
    float intensity,
    float blendMode)
{
    float3 detail = detailSample.rgb * detailTint;
    float3 multiplied = baseColor * detail;
    float3 alphaBlended = lerp(baseColor, detail, detailSample.a);
    float3 overlay = lerp(
        2.0 * baseColor * detail,
        1.0 - 2.0 * (1.0 - baseColor) * (1.0 - detail),
        step(0.5, baseColor));
    float3 result = blendMode < 0.5 ? multiplied : (blendMode < 1.5 ? overlay : alphaBlended);
    return lerp(baseColor, result, saturate(intensity));
}

float FantasyCampRustTopDownNormalizedMask(
    float normalizedHeight,
    float downwardCoverage01,
    float edgeSoftness01,
    float intensity)
{
    if (downwardCoverage01 <= 0.0 || intensity <= 0.0)
        return 0.0;

    float heightFromTop01 = 1.0 - saturate(normalizedHeight);
    float coverage = saturate(downwardCoverage01);
    float softness = min(max(edgeSoftness01, 0.0001), coverage);
    return saturate((1.0 - smoothstep(coverage - softness, coverage, heightFromTop01)) * intensity);
}

float FantasyCampSelectNormalizedHeight(
    float3 heightSources,
    float source,
    float invert)
{
    float height;
    if (source < 0.5)
        height = heightSources.x;              // UV0 V
    else if (source < 1.5)
        height = heightSources.y;              // UV1 V
    else
        height = heightSources.z;              // Vertex Color R

    height = saturate(height);
    return invert > 0.5 ? 1.0 - height : height;
}

float3 FantasyCampVoronoiHash(float3 cell, float seed)
{
    cell += seed * float3(13.17, 7.31, 19.97);
    float3 q;
    q.x = dot(cell, float3(127.1, 311.7, 74.7));
    q.y = dot(cell, float3(269.5, 183.3, 246.1));
    q.z = dot(cell, float3(113.5, 271.9, 124.6));
    return frac(sin(q) * 43758.5453);
}

float FantasyCampVoronoi3D(float3 position, float seed)
{
    float3 baseCell = floor(position);
    float3 localPosition = frac(position);
    float nearestDistanceSquared = 8.0;

    [unroll]
    for (int z = -1; z <= 1; ++z)
    {
        [unroll]
        for (int y = -1; y <= 1; ++y)
        {
            [unroll]
            for (int x = -1; x <= 1; ++x)
            {
                float3 neighbor = float3(x, y, z);
                float3 cellPoint = neighbor + FantasyCampVoronoiHash(baseCell + neighbor, seed);
                float3 delta = cellPoint - localPosition;
                nearestDistanceSquared = min(nearestDistanceSquared, dot(delta, delta));
            }
        }
    }
    return sqrt(nearestDistanceSquared);
}

float FantasyCampProceduralRustMask(
    float3 positionWS,
    float scale,
    float coverage,
    float softness,
    float seed)
{
    float distanceToCell = FantasyCampVoronoi3D(positionWS * max(abs(scale), 0.0001), seed);
    float safeSoftness = max(softness, 0.0001);
    return 1.0 - smoothstep(coverage, coverage + safeSoftness, distanceToCell);
}

float FantasyCampValueNoise3D(float3 position)
{
    float3 cell = floor(position);
    float3 local = frac(position);
    float3 blend = local * local * (3.0 - 2.0 * local);

    float n000 = FantasyCampVoronoiHash(cell + float3(0, 0, 0), 0.0).x;
    float n100 = FantasyCampVoronoiHash(cell + float3(1, 0, 0), 0.0).x;
    float n010 = FantasyCampVoronoiHash(cell + float3(0, 1, 0), 0.0).x;
    float n110 = FantasyCampVoronoiHash(cell + float3(1, 1, 0), 0.0).x;
    float n001 = FantasyCampVoronoiHash(cell + float3(0, 0, 1), 0.0).x;
    float n101 = FantasyCampVoronoiHash(cell + float3(1, 0, 1), 0.0).x;
    float n011 = FantasyCampVoronoiHash(cell + float3(0, 1, 1), 0.0).x;
    float n111 = FantasyCampVoronoiHash(cell + float3(1, 1, 1), 0.0).x;

    float z0 = lerp(lerp(n000, n100, blend.x), lerp(n010, n110, blend.x), blend.y);
    float z1 = lerp(lerp(n001, n101, blend.x), lerp(n011, n111, blend.x), blend.y);
    return lerp(z0, z1, blend.z);
}

float FantasyCampFBM3D(float3 position)
{
    float value = 0.0;
    float amplitude = 0.5;
    [unroll]
    for (int octave = 0; octave < 4; ++octave)
    {
        value += FantasyCampValueNoise3D(position) * amplitude;
        position = position * 2.03 + float3(17.1, 9.2, 13.7);
        amplitude *= 0.5;
    }
    return value;
}

float FantasyCampRustFlowMask(
    float3 positionWS,
    float scale,
    float verticalStretch,
    float distortion,
    float coverage,
    float softness,
    float seed)
{
    float safeScale = max(abs(scale), 0.0001);
    float stretch = max(verticalStretch, 1.0);
    float3 basePosition = float3(positionWS.x * safeScale,
        positionWS.y * safeScale / stretch,
        positionWS.z * safeScale);
    float warp = FantasyCampFBM3D(basePosition * 0.55 + seed) - 0.5;
    basePosition.xz += warp * distortion;
    float streakNoise = FantasyCampFBM3D(basePosition + seed * 3.17);
    float threshold = 1.0 - saturate(coverage);
    return smoothstep(threshold - softness, threshold + softness, streakNoise);
}

// Mirror Ground Up in the caller's coordinate space: full coverage above
// topHeight, fading to zero at topHeight - mossHeight. No mesh-bounds inference.
float FantasyCampTopMossMask(
    float surfaceHeight,
    float topHeight,
    float mossHeight,
    float edgeSoftness,
    float intensity)
{
    return FantasyCampMossMask(-surfaceHeight, -topHeight, mossHeight,
        edgeSoftness, intensity);
}

#endif

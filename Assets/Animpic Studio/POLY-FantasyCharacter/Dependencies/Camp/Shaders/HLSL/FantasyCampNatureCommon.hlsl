#ifndef FANTASY_CAMP_NATURE_COMMON_INCLUDED
#define FANTASY_CAMP_NATURE_COMMON_INCLUDED

#include "Assets/Animpic Studio/POLY-FantasyCharacter/Dependencies/Camp/Shaders/HLSL/FantasyCampGlobalWind.hlsl"

float FantasyCampNatureHash(float2 value)
{
    value = frac(value * float2(123.34, 456.21));
    value += dot(value, value + 45.32);
    return frac(value.x * value.y);
}

float FantasyCampNatureNoise(float2 position)
{
    float2 cell = floor(position);
    float2 localPosition = frac(position);
    float2 blend = localPosition * localPosition * (3.0 - 2.0 * localPosition);

    float bottom = lerp(FantasyCampNatureHash(cell),
        FantasyCampNatureHash(cell + float2(1.0, 0.0)), blend.x);
    float top = lerp(FantasyCampNatureHash(cell + float2(0.0, 1.0)),
        FantasyCampNatureHash(cell + float2(1.0, 1.0)), blend.x);
    return lerp(bottom, top, blend.y);
}

float FantasyCampNatureSelectMask(float2 uv0, float2 uv1, float4 vertexColor,
    float3 positionOS, float source, float objectBottom, float objectTop, float invertMask)
{
    float objectHeight = saturate((positionOS.y - objectBottom) /
        max(objectTop - objectBottom, 0.0001));
    float mask = uv0.y;
    if (source > 0.5)
        mask = uv1.y;
    if (source > 1.5)
        mask = vertexColor.r;
    if (source > 2.5)
        mask = objectHeight;
    mask = saturate(mask);
    return invertMask > 0.5 ? 1.0 - mask : mask;
}

float3 FantasyCampNatureWindOffsetWS(float3 positionWS, float bendMask, float time,
    float windPower, float2 windDirection, float windSpeed, float windScale,
    float turbulence, float leafScale, float leafPower, float leafSwing,
    float maskPower)
{
    float directionLength = max(length(windDirection), 0.0001);
    float2 direction = windDirection / directionLength;
    float scale = max(abs(windScale), 0.0001);
    float speed = max(abs(windSpeed), 0.0001);
    float shapedMask = pow(saturate(bendMask), max(maskPower, 0.01));

    float2 movingPosition = positionWS.xz * scale + direction * (time * speed * 0.35);
    float gust = FantasyCampNatureNoise(movingPosition);
    float phase = dot(positionWS.xz, direction) * scale + time * speed;
    phase += (gust - 0.5) * turbulence * 6.2831853;

    // The original graph used values close to ten for WindPower. Keeping this
    // conversion preserves a useful material range in world units.
    float mainAmplitude = windPower * 0.02 * shapedMask;
    float mainWave = sin(phase) * lerp(0.55, 1.0, gust);
    float3 offsetWS = float3(direction.x, 0.0, direction.y) * mainWave * mainAmplitude;

    float flutterFrequency = max(abs(leafPower), 0.01);
    float flutterPhase = time * speed * flutterFrequency;
    flutterPhase += dot(positionWS.xz, float2(1.73, 2.31)) * max(abs(leafScale), 0.0001);
    float flutterNoise = FantasyCampNatureNoise(positionWS.xz * max(abs(leafScale), 0.0001)
        + time * speed);
    float flutter = (sin(flutterPhase) + (flutterNoise - 0.5) * turbulence)
        * leafSwing * shapedMask;
    float3 crossWind = float3(-direction.y, 0.15, direction.x);
    offsetWS += crossWind * flutter;
    return offsetWS;
}

float3 FantasyCampNatureGradient(float2 uv0, float2 uv1, float4 vertexColor,
    float3 positionOS, float source, float objectBottom, float objectTop,
    float invertMask, float gradientPosition, float gradientOffset,
    float gradientNoiseAmount, float gradientNoiseScale, float3 bottomColor,
    float3 topColor)
{
    float height = FantasyCampNatureSelectMask(uv0, uv1, vertexColor, positionOS,
        source, objectBottom, objectTop, invertMask);
    float noise = FantasyCampNatureNoise(positionOS.xz * max(abs(gradientNoiseScale), 0.0001));
    height += (noise - 0.5) * gradientNoiseAmount;
    height = saturate(height * max(gradientPosition, 0.0001) + gradientOffset);
    return lerp(bottomColor, topColor, height);
}

float3 FantasyCampNatureNormalFromColor(float3 color, float strength)
{
    float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
    float2 slope = float2(ddx(luminance), ddy(luminance)) * strength;
    return normalize(float3(-slope.x, -slope.y, 1.0));
}

#endif

#ifndef FANTASY_CAMP_VEGETATION_WIND_INCLUDED
#define FANTASY_CAMP_VEGETATION_WIND_INCLUDED

#include "Assets/Animpic Studio/POLY-FantasyCharacter/Dependencies/Camp/Shaders/HLSL/FantasyCampGlobalWind.hlsl"

float FantasyCampVegetationHash(float2 value)
{
    value = frac(value * float2(123.34, 456.21));
    value += dot(value, value + 45.32);
    return frac(value.x * value.y);
}

float FantasyCampVegetationNoise(float2 position)
{
    float2 cell = floor(position);
    float2 localPosition = frac(position);
    float2 blend = localPosition * localPosition * (3.0 - 2.0 * localPosition);

    float lower = lerp(FantasyCampVegetationHash(cell),
        FantasyCampVegetationHash(cell + float2(1.0, 0.0)), blend.x);
    float upper = lerp(FantasyCampVegetationHash(cell + float2(0.0, 1.0)),
        FantasyCampVegetationHash(cell + float2(1.0, 1.0)), blend.x);
    return lerp(lower, upper, blend.y);
}

float2 FantasyCampVegetationDirection(float2 windDirection)
{
    float directionLength = length(windDirection);
    return directionLength > 0.0001
        ? windDirection / directionLength
        : float2(1.0, 0.0);
}

float FantasyCampVegetationBendWeight(float positionOSY, float rootHeight,
    float flexibility)
{
    float height = max(positionOSY - rootHeight, 0.0);
    return 1.0 - exp2(-height * max(flexibility, 0.0001));
}

float3 FantasyCampVegetationRotateAroundAxis(float3 value, float3 axis,
    float angle)
{
    float sine;
    float cosine;
    sincos(angle, sine, cosine);
    return value * cosine + cross(axis, value) * sine
        + axis * dot(axis, value) * (1.0 - cosine);
}

// Shared by Tree and Leaf. The shared object origin supplies a stable phase for
// every modular part, while absolute positionOS.y supplies a size-independent
// bend curve without UV, vertex-color, or object-bound data.
float3 FantasyCampVegetationMainWindOffsetWS(float3 positionWS,
    float3 originWS, float positionOSY, float time, float windPower,
    float2 windDirection, float windSpeed, float2 windScale, float turbulence,
    float rootHeight, float flexibility)
{
    float2 direction = FantasyCampVegetationDirection(windDirection);
    float2 scale = max(abs(windScale), float2(0.0001, 0.0001));
    float speed = max(abs(windSpeed), 0.0001);
    float bendWeight = FantasyCampVegetationBendWeight(positionOSY,
        rootHeight, flexibility);

    float2 movingPosition = originWS.xz * scale
        + direction * (time * speed * 0.25);
    float gust = FantasyCampVegetationNoise(movingPosition);
    float objectSeed = FantasyCampVegetationHash(floor(originWS.xz * 4.0));
    float phase = dot(originWS.xz * scale, direction) + time * speed;
    phase += objectSeed * 6.2831853;
    phase += (gust - 0.5) * turbulence * 6.2831853;

    float wave = sin(phase) * lerp(0.55, 1.0, gust);
    float bendAngle = windPower * 0.015 * wave * bendWeight;
    float3 bendAxis = normalize(float3(direction.y, 0.0, -direction.x));
    float3 relativeWS = positionWS - originWS;
    float3 bentRelativeWS = FantasyCampVegetationRotateAroundAxis(relativeWS,
        bendAxis, bendAngle);
    return bentRelativeWS - relativeWS;
}

// Leaf-only high-frequency motion. It is intentionally evaluated after the
// shared main offset so the foliage follows the tree before it flutters.
float3 FantasyCampVegetationLeafFlutterOffsetWS(float3 positionWS,
    float bendWeight, float time, float2 windDirection, float windSpeed,
    float turbulence, float flutterScale, float flutterSpeed,
    float flutterStrength)
{
    float2 direction = FantasyCampVegetationDirection(windDirection);
    float speed = max(abs(windSpeed), 0.0001);
    float scale = max(abs(flutterScale), 0.0001);

    float phase = time * speed * max(abs(flutterSpeed), 0.01);
    phase += dot(positionWS.xz, float2(1.73, 2.31)) * scale;
    float flutterNoise = FantasyCampVegetationNoise(positionWS.xz * scale
        + time * speed);
    float flutter = (sin(phase) + (flutterNoise - 0.5) * turbulence)
        * flutterStrength * bendWeight;

    float3 crossWind = float3(-direction.y, 0.15, direction.x);
    return crossWind * flutter;
}

#endif

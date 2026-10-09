#ifndef FANTASY_CAMP_TREE_COMMON_INCLUDED
#define FANTASY_CAMP_TREE_COMMON_INCLUDED

#include "Assets/Animpic Studio/POLY-FantasyCharacter/Dependencies/Camp/Shaders/HLSL/FantasyCampVegetationWind.hlsl"
#include "Assets/Animpic Studio/POLY-FantasyCharacter/Dependencies/Camp/Shaders/HLSL/FantasyCampSurfaceCommon.hlsl"

float FantasyCampTreeSelectMask(float2 uv0, float2 uv1, float4 vertexColor,
    float3 positionOS, float source, float objectBottom, float objectTop,
    float invertMask)
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

float3 FantasyCampTreeWindOffsetWS(float3 positionWS, float3 originWS,
    float positionOSY, float time, float windPower, float2 windDirection,
    float windSpeed, float2 windScale, float turbulence, float rootHeight,
    float flexibility)
{
    return FantasyCampVegetationMainWindOffsetWS(positionWS, originWS,
        positionOSY, time, windPower, windDirection, windSpeed, windScale,
        turbulence, rootHeight, flexibility);
}

float FantasyCampTreeGradientFactor(float2 uv0, float2 uv1,
    float4 vertexColor, float3 positionOS, float source, float objectBottom,
    float objectTop, float invertMask, float gradientScale, float gradientOffset,
    float breakupAmount, float breakupScale)
{
    float height = FantasyCampTreeSelectMask(uv0, uv1, vertexColor, positionOS,
        source, objectBottom, objectTop, invertMask);
    float breakup = FantasyCampVegetationNoise(positionOS.xz
        * max(abs(breakupScale), 0.0001));
    height += (breakup - 0.5) * breakupAmount;
    return saturate(height * max(gradientScale, 0.0001) + gradientOffset);
}

float3 FantasyCampTreeNormalFromColor(float3 color, float strength)
{
    float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
    float2 slope = float2(ddx(luminance), ddy(luminance)) * strength;
    return normalize(float3(-slope.x, -slope.y, 1.0));
}

#endif

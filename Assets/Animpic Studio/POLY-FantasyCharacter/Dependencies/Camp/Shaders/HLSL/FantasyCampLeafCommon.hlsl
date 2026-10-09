#ifndef FANTASY_CAMP_LEAF_COMMON_INCLUDED
#define FANTASY_CAMP_LEAF_COMMON_INCLUDED

#include "Assets/Animpic Studio/POLY-FantasyCharacter/Dependencies/Camp/Shaders/HLSL/FantasyCampVegetationWind.hlsl"

float FantasyCampLeafSelectMask(float2 uv0, float2 uv1, float4 vertexColor,
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

float3 FantasyCampLeafWindOffsetWS(float3 positionWS, float3 originWS,
    float positionOSY, float time, float windPower, float2 windDirection,
    float windSpeed, float2 windScale, float turbulence, float rootHeight,
    float flexibility, float flutterScale, float flutterSpeed,
    float flutterStrength)
{
    float bendWeight = FantasyCampVegetationBendWeight(positionOSY, rootHeight,
        flexibility);
    float3 offsetWS = FantasyCampVegetationMainWindOffsetWS(positionWS,
        originWS, positionOSY, time, windPower, windDirection, windSpeed,
        windScale, turbulence, rootHeight, flexibility);
    offsetWS += FantasyCampVegetationLeafFlutterOffsetWS(positionWS,
        bendWeight, time, windDirection, windSpeed, turbulence, flutterScale,
        flutterSpeed, flutterStrength);
    return offsetWS;
}

float3 FantasyCampLeafGradient(float2 uv0, float2 uv1, float4 vertexColor,
    float3 positionOS, float source, float objectBottom, float objectTop,
    float invertMask, float gradientPosition, float gradientOffset,
    float gradientNoiseAmount, float gradientNoiseScale, float3 bottomColor,
    float3 topColor)
{
    float height = FantasyCampLeafSelectMask(uv0, uv1, vertexColor, positionOS,
        source, objectBottom, objectTop, invertMask);
    float noise = FantasyCampVegetationNoise(positionOS.xz
        * max(abs(gradientNoiseScale), 0.0001));
    height += (noise - 0.5) * gradientNoiseAmount;
    height = saturate(height * max(gradientPosition, 0.0001) + gradientOffset);
    return lerp(bottomColor, topColor, height);
}

float3 FantasyCampLeafNormalFromColor(float3 color, float strength)
{
    float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
    float2 slope = float2(ddx(luminance), ddy(luminance)) * strength;
    return normalize(float3(-slope.x, -slope.y, 1.0));
}

float3 FantasyCampLeafAlbedo(float3 textureColor,
    float textureColorStrength, float3 tintColor, float tintStrength,
    float3 gradientColor, float gradientEnabled)
{
    float3 white = float3(1.0, 1.0, 1.0);
    float3 sourceColor = lerp(white, textureColor,
        saturate(textureColorStrength));
    float3 tintMultiplier = lerp(white, tintColor, saturate(tintStrength));
    float3 gradientMultiplier = lerp(white, gradientColor,
        saturate(gradientEnabled));
    return sourceColor * tintMultiplier * gradientMultiplier;
}

float FantasyCampLeafTransmission(float3 normalWS, float3 lightDirectionWS,
    float transmissionPower)
{
    float3 safeNormalWS = normalWS
        * rsqrt(max(dot(normalWS, normalWS), 0.00000001));
    float3 safeLightDirectionWS = lightDirectionWS
        * rsqrt(max(dot(lightDirectionWS, lightDirectionWS), 0.00000001));
    float backLighting = saturate(dot(-safeNormalWS, safeLightDirectionWS));
    return pow(backLighting, max(transmissionPower, 0.0001));
}

#endif

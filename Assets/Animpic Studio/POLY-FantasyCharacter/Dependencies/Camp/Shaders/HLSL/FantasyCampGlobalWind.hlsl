#ifndef FANTASY_CAMP_GLOBAL_WIND_INCLUDED
#define FANTASY_CAMP_GLOBAL_WIND_INCLUDED

// These values intentionally do not exist in a shader Properties block.
// Material properties would override Shader.SetGlobal* values.
float _POLYFantasyCharacterFantasyCampGlobalWindAvailable;
float _POLYFantasyCharacterFantasyCampGlobalWindEnabled;
float4 _POLYFantasyCharacterFantasyCampGlobalWindDirection;
float4 _POLYFantasyCharacterFantasyCampGlobalWindMain;
float4 _POLYFantasyCharacterFantasyCampGlobalWindWaveScale;
float4 _POLYFantasyCharacterFantasyCampGlobalWindBend;
float4 _POLYFantasyCharacterFantasyCampGlobalWindFlutter;

float FantasyCampGlobalWindWeight(float useGlobalWind)
{
    return step(0.5, useGlobalWind)
        * step(0.5, _POLYFantasyCharacterFantasyCampGlobalWindAvailable);
}

float FantasyCampResolveWindEnabled(float localValue, float useGlobalWind)
{
    return lerp(localValue, _POLYFantasyCharacterFantasyCampGlobalWindEnabled,
        FantasyCampGlobalWindWeight(useGlobalWind));
}

float2 FantasyCampResolveWindDirection(float2 localValue, float useGlobalWind)
{
    return lerp(localValue, _POLYFantasyCharacterFantasyCampGlobalWindDirection.xy,
        FantasyCampGlobalWindWeight(useGlobalWind));
}

float FantasyCampResolveWindStrength(float localValue, float useGlobalWind,
    float globalResponse)
{
    return lerp(localValue, _POLYFantasyCharacterFantasyCampGlobalWindMain.x * globalResponse,
        FantasyCampGlobalWindWeight(useGlobalWind));
}

float FantasyCampResolveWindSpeed(float localValue, float useGlobalWind)
{
    return lerp(localValue, _POLYFantasyCharacterFantasyCampGlobalWindMain.y,
        FantasyCampGlobalWindWeight(useGlobalWind));
}

float FantasyCampResolveWindTurbulence(float localValue, float useGlobalWind)
{
    return lerp(localValue, _POLYFantasyCharacterFantasyCampGlobalWindMain.z,
        FantasyCampGlobalWindWeight(useGlobalWind));
}

float2 FantasyCampResolveWindWaveScale(float2 localValue, float useGlobalWind)
{
    return lerp(localValue, _POLYFantasyCharacterFantasyCampGlobalWindWaveScale.xy,
        FantasyCampGlobalWindWeight(useGlobalWind));
}

float FantasyCampResolveWindRootHeight(float localValue, float useGlobalWind)
{
    return lerp(localValue, _POLYFantasyCharacterFantasyCampGlobalWindBend.x,
        FantasyCampGlobalWindWeight(useGlobalWind));
}

float FantasyCampResolveWindFlexibility(float localValue, float useGlobalWind)
{
    return lerp(localValue, _POLYFantasyCharacterFantasyCampGlobalWindBend.y,
        FantasyCampGlobalWindWeight(useGlobalWind));
}

float FantasyCampResolveFlutterStrength(float localValue, float useGlobalWind)
{
    return lerp(localValue, _POLYFantasyCharacterFantasyCampGlobalWindFlutter.x,
        FantasyCampGlobalWindWeight(useGlobalWind));
}

float FantasyCampResolveFlutterScale(float localValue, float useGlobalWind)
{
    return lerp(localValue, _POLYFantasyCharacterFantasyCampGlobalWindFlutter.y,
        FantasyCampGlobalWindWeight(useGlobalWind));
}

float FantasyCampResolveFlutterSpeed(float localValue, float useGlobalWind)
{
    return lerp(localValue, _POLYFantasyCharacterFantasyCampGlobalWindFlutter.z,
        FantasyCampGlobalWindWeight(useGlobalWind));
}

#endif

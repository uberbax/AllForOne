Shader "Animpic Studio/Fantasy Character/Leaf/Built-in"
{
    Properties
    {
        [Header(Appearance)]
        [MainTexture] _BaseColor("Base Color Texture", 2D) = "white" {}
        [HideInInspector] _MainTex("Legacy Main Texture", 2D) = "white" {}
        [MainColor] _Tint("Texture Tint", Color) = (1, 1, 1, 1)
        _TextureColorStrength("Texture Color Strength", Range(0, 1)) = 1
        _TintStrength("Tint Strength", Range(0, 1)) = 1
        _Alpha("Alpha", Range(0, 2)) = 1
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.05
        _Occlusion("Occlusion", Range(0, 1)) = 1
        _NormalStrength("Normal From Color", Range(0, 8)) = 0
        _BackfaceLighting("Backface Lighting", Range(0, 1)) = 0.65
        _ReceiveShadowStrength("Receive Shadow Strength", Range(0, 1)) = 0.8
        [Enum(Off,0,Front,1,Back,2)] _Cull("Cull Mode", Float) = 0

        [Header(Leaf Transmission)]
        _TransmissionColor("Transmission Tint", Color) = (1, 1, 1, 1)
        _TransmissionStrength("Transmission Strength", Range(0, 2)) = 0.65
        _TransmissionPower("Transmission Sharpness", Range(0.5, 8)) = 1.5
        _TransmissionAmbient("Indirect Transmission", Range(0, 1)) = 0.12

        [Header(Color Gradient)]
        [Toggle] _IfGradient("Gradient Enabled", Float) = 0
        _BotColor("Bottom Color", Color) = (0.56, 0.18, 0.03, 1)
        _TopColor("Top Color", Color) = (1, 0.6, 0, 1)
        [Enum(UV0 V,0,UV1 V,1,Vertex Color R,2,Object Y,3)] _GradientSource("Gradient Source", Float) = 0
        _GradientPosition("Gradient Height", Range(0.05, 4)) = 1
        _GradientOffset("Gradient Offset", Range(-1, 1)) = 0
        [Toggle] _GradientInvert("Invert Gradient", Float) = 0
        _GradientObjectBottom("Object Bottom", Float) = 0
        _GradientObjectTop("Object Top", Float) = 1
        _GradientNoiseAmount("Gradient Breakup", Range(0, 1)) = 0
        _GradientNoiseScale("Gradient Breakup Scale", Float) = 2

        [Header(Wind)]
        [Toggle] _UseGlobalWind("Use Global Wind", Float) = 1
        _WindResponse("Wind Response", Range(0, 2)) = 1
        [Toggle] _Wind_Enabled("Wind Enabled", Float) = 1
        _WindPower("Wind Strength", Range(0, 20)) = 5
        _WindDirection("Wind Direction", Vector) = (1, 0, 0, 0)
        _WindSpeed("Wind Speed", Range(0, 10)) = 1
        _UV("World Wave Scale", Vector) = (0.35, 0.35, 0, 0)
        _WindTurbulence("Wind Turbulence", Range(0, 3)) = 1
        _LeafWindScale("Flutter Scale", Range(0.01, 20)) = 5
        _LeafWindPower("Flutter Speed", Range(0.01, 5)) = 1
        _LeafWindSwing("Flutter Strength", Range(0, 0.5)) = 0.05
        _WindRootHeight("Root Height (Object Y)", Float) = 0
        _WindFlexibility("Flexibility per Meter", Range(0.01, 2)) = 0.65
    }

    SubShader
    {
        Tags
        {
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
        }
        LOD 300

        CGINCLUDE
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"
        #include "Assets/Animpic Studio/POLY-FantasyCharacter/Dependencies/Camp/Shaders/HLSL/FantasyCampLeafCommon.hlsl"

        sampler2D _BaseColor;
        float4 _BaseColor_ST;
        float4 _Tint;
        float _TextureColorStrength;
        float _TintStrength;
        float _Alpha;
        float _Cutoff;
        float _Metallic;
        float _Smoothness;
        float _Occlusion;
        float _NormalStrength;
        float _BackfaceLighting;
        float _ReceiveShadowStrength;
        float4 _TransmissionColor;
        float _TransmissionStrength;
        float _TransmissionPower;
        float _TransmissionAmbient;
        float _IfGradient;
        float4 _BotColor;
        float4 _TopColor;
        float _GradientSource;
        float _GradientPosition;
        float _GradientOffset;
        float _GradientInvert;
        float _GradientObjectBottom;
        float _GradientObjectTop;
        float _GradientNoiseAmount;
        float _GradientNoiseScale;
        float _UseGlobalWind;
        float _WindResponse;
        float _Wind_Enabled;
        float _WindPower;
        float4 _WindDirection;
        float _WindSpeed;
        float4 _UV;
        float _WindTurbulence;
        float _LeafWindScale;
        float _LeafWindPower;
        float _LeafWindSwing;
        float _WindRootHeight;
        float _WindFlexibility;

        float3 LeafDeformedPositionOS(float3 originalOS)
        {
            float windEnabled = FantasyCampResolveWindEnabled(_Wind_Enabled,
                _UseGlobalWind);
            if (windEnabled <= 0.5)
                return originalOS;

            float3 positionWS = mul(unity_ObjectToWorld,
                float4(originalOS, 1.0)).xyz;
            float3 originWS = mul(unity_ObjectToWorld,
                float4(0.0, 0.0, 0.0, 1.0)).xyz;
            float windStrength = FantasyCampResolveWindStrength(_WindPower,
                _UseGlobalWind, _WindResponse);
            float2 windDirection = FantasyCampResolveWindDirection(
                _WindDirection.xy, _UseGlobalWind);
            float windSpeed = FantasyCampResolveWindSpeed(_WindSpeed,
                _UseGlobalWind);
            float2 waveScale = FantasyCampResolveWindWaveScale(_UV.xy,
                _UseGlobalWind);
            float turbulence = FantasyCampResolveWindTurbulence(
                _WindTurbulence, _UseGlobalWind);
            float rootHeight = FantasyCampResolveWindRootHeight(
                _WindRootHeight, _UseGlobalWind);
            float flexibility = FantasyCampResolveWindFlexibility(
                _WindFlexibility, _UseGlobalWind);
            float flutterScale = FantasyCampResolveFlutterScale(
                _LeafWindScale, _UseGlobalWind);
            float flutterSpeed = FantasyCampResolveFlutterSpeed(
                _LeafWindPower, _UseGlobalWind);
            float flutterStrength = FantasyCampResolveFlutterStrength(
                _LeafWindSwing, _UseGlobalWind);
            float3 windOffsetWS = FantasyCampLeafWindOffsetWS(positionWS,
                originWS, originalOS.y, _Time.y, windStrength,
                windDirection, windSpeed, waveScale, turbulence,
                rootHeight, flexibility, flutterScale, flutterSpeed,
                flutterStrength);
            return originalOS + mul((float3x3)unity_WorldToObject,
                windOffsetWS);
        }

        float LeafGradientFactor(float2 uv0, float2 uv1, float4 vertexColor,
            float3 originalOS)
        {
            return FantasyCampLeafGradient(uv0, uv1, vertexColor, originalOS,
                _GradientSource, _GradientObjectBottom, _GradientObjectTop,
                _GradientInvert, _GradientPosition, _GradientOffset,
                _GradientNoiseAmount, _GradientNoiseScale,
                float3(0.0, 0.0, 0.0), float3(1.0, 1.0, 1.0)).x;
        }

        half3 LeafVisibleAlbedo(half3 textureColor, float gradientFactor)
        {
            half3 gradientColor = lerp(_BotColor.rgb, _TopColor.rgb,
                saturate(gradientFactor));
            return FantasyCampLeafAlbedo(textureColor, _TextureColorStrength,
                _Tint.rgb, _TintStrength, gradientColor, _IfGradient);
        }

        half4 LeafSample(float2 uv)
        {
            half4 sampleValue = tex2D(_BaseColor, uv);
            clip(sampleValue.a * _Alpha * _Tint.a - _Cutoff);
            return sampleValue;
        }
        ENDCG

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            Blend One Zero
            ColorMask RGBA

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex LeafVertex
            #pragma fragment LeafFragment
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct LeafAttributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct LeafVaryings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;
                half gradientFactor : TEXCOORD4;
                half3 vertexLighting : TEXCOORD5;
                UNITY_FOG_COORDS(6)
                UNITY_SHADOW_COORDS(7)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            LeafVaryings LeafVertex(LeafAttributes v)
            {
                LeafVaryings output;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(LeafVaryings, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 originalOS = v.vertex.xyz;
                v.vertex.xyz = LeafDeformedPositionOS(originalOS);
                output.pos = UnityObjectToClipPos(v.vertex);
                output.positionWS = mul(unity_ObjectToWorld,
                    v.vertex).xyz;
                output.normalWS = UnityObjectToWorldNormal(v.normal);
                output.tangentWS.xyz = UnityObjectToWorldDir(v.tangent.xyz);
                output.tangentWS.w = v.tangent.w
                    * unity_WorldTransformParams.w;
                output.uv = TRANSFORM_TEX(v.uv0, _BaseColor);
                output.gradientFactor = LeafGradientFactor(v.uv0,
                    v.uv1, v.color, originalOS);
                output.vertexLighting = 0.0h;

                #ifdef VERTEXLIGHT_ON
                output.vertexLighting = Shade4PointLights(
                    unity_4LightPosX0, unity_4LightPosY0,
                    unity_4LightPosZ0, unity_LightColor[0].rgb,
                    unity_LightColor[1].rgb, unity_LightColor[2].rgb,
                    unity_LightColor[3].rgb, unity_4LightAtten0,
                    output.positionWS, normalize(output.normalWS));
                #endif

                UNITY_TRANSFER_SHADOW(output, v.uv1);
                UNITY_TRANSFER_FOG(output, output.pos);
                return output;
            }

            fixed4 LeafFragment(LeafVaryings input,
                fixed facing : VFACE) : SV_Target
            {
                half4 textureSample = LeafSample(input.uv);
                half3 albedo = LeafVisibleAlbedo(textureSample.rgb,
                    input.gradientFactor);
                half faceSign = facing >= 0.0h ? 1.0h : -1.0h;
                half3 baseNormalWS = normalize(input.normalWS);
                half3 normalWS = baseNormalWS * faceSign;

                if (_NormalStrength > 0.0001)
                {
                    half3 normalTS = FantasyCampLeafNormalFromColor(
                        textureSample.rgb, _NormalStrength);
                    normalTS.z *= faceSign;
                    half3 tangentWS = normalize(input.tangentWS.xyz);
                    half3 bitangentWS = normalize(cross(baseNormalWS,
                        tangentWS) * input.tangentWS.w);
                    normalWS = normalize(mul(normalTS,
                        half3x3(tangentWS, bitangentWS, baseNormalWS)));
                }

                half3 viewDirectionWS = normalize(_WorldSpaceCameraPos
                    - input.positionWS);
                half3 lightVectorWS = UnityWorldSpaceLightDir(
                    input.positionWS);
                half3 lightDirectionWS = lightVectorWS
                    * rsqrt(max(dot(lightVectorWS, lightVectorWS), 0.000001h));
                half ndotl = saturate(dot(normalWS, lightDirectionWS));
                half realShadow = UNITY_SHADOW_ATTENUATION(input,
                    input.positionWS);
                half directAttenuation = lerp(1.0h, realShadow,
                    saturate(_ReceiveShadowStrength));
                half faceLighting = facing >= 0.0h
                    ? 1.0h : saturate(_BackfaceLighting);
                directAttenuation *= faceLighting;
                half3 ambient = max(ShadeSH9(half4(normalWS, 1.0h)), 0.0h)
                    * _Occlusion;
                ambient += input.vertexLighting * _Occlusion;

                half3 color = albedo * (ambient
                    + _LightColor0.rgb * ndotl * directAttenuation);

                if (_TransmissionStrength > 0.0001)
                {
                    half backLighting = FantasyCampLeafTransmission(normalWS,
                        lightDirectionWS, _TransmissionPower);
                    half3 transmittedLight = _LightColor0.rgb * backLighting
                        * directAttenuation
                        + ambient * _TransmissionAmbient;
                    color += albedo * _TransmissionColor.rgb
                        * _TransmissionStrength * (1.0h - _Metallic)
                        * transmittedLight;
                }

                half3 halfDirection = normalize(lightDirectionWS
                    + viewDirectionWS);
                half specularPower = exp2(1.0h + _Smoothness * 10.0h);
                half specular = pow(saturate(dot(normalWS, halfDirection)),
                    specularPower) * lerp(0.04h, 1.0h, _Metallic);
                color += _LightColor0.rgb * specular * directAttenuation
                    * lerp(half3(1.0h, 1.0h, 1.0h), albedo, _Metallic);

                UNITY_APPLY_FOG(input.fogCoord, color);
                return fixed4(color, 1.0h);
            }
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            ColorMask 0

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex LeafShadowVertex
            #pragma fragment LeafShadowFragment
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing

            struct LeafShadowVaryings
            {
                V2F_SHADOW_CASTER;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            LeafShadowVaryings LeafShadowVertex(appdata_full v)
            {
                LeafShadowVaryings output;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(LeafShadowVaryings, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 originalOS = v.vertex.xyz;
                v.vertex.xyz = LeafDeformedPositionOS(originalOS);
                output.uv = TRANSFORM_TEX(v.texcoord.xy, _BaseColor);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(output)
                return output;
            }

            fixed4 LeafShadowFragment(LeafShadowVaryings input) : SV_Target
            {
                LeafSample(input.uv);
                SHADOW_CASTER_FRAGMENT(input)
            }
            ENDCG
        }
    }

    FallBack Off
    CustomEditor "Animpic.Local.POLYFantasyCharacter.Camp.FantasyCampLeafShaderGUI"
}

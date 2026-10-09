Shader "Animpic Studio/Fantasy Character/Water/Built-in"
{
    Properties
    {
        _SurfaceColor("Shallow Color (A = Density)", Color) = (0, 0.74793315, 1, 1)
        _DeepColor("Deep Color (A = Density)", Color) = (0.0064524733, 0.2375831, 0.2735849, 1)
        _Distance("Depth Color Distance", Float) = 2
        [Normal] _RefreactionNormal("Water Normal Map", 2D) = "bump" {}
        _RefractionScale("Normal Tiling", Vector) = (5, 5, 0, 0)
        _RefractionSpeed("Normal Scroll Speed", Float) = 0.25
        _RefractionPower("Refraction Strength", Float) = 1
        _NormalPower("Normal Strength", Float) = 1
        _Smoothness("Smoothness", Range(0, 1)) = 0
        _FoamColor("Foam Color (A = Coverage)", Color) = (0.8242257, 0.9245283, 0.92248, 1)
        _FoamAmount("Foam Depth Distance", Float) = 2
        _FoamSpeed("Foam Scroll Speed", Float) = 1
        _FoamScale("Foam Pattern Scale", Float) = 30.73
        _FoamCuttoff("Foam Threshold", Float) = 1
        _HightFrequency("Wave Tiling / Density", Float) = 1
        _WaveSpeed("Wave Speed", Float) = 1
        _WaveAmplitude("Wave Amplitude (Object Units)", Float) = 1
        [ToggleUI] _DepthEnabled("Depth Color Enabled", Float) = 1
        [ToggleUI] _RefractionEnabled("Scene Refraction Enabled", Float) = 1
        [ToggleUI] _FoamEnabled("Shore Foam Enabled", Float) = 1
        [ToggleUI] _WavesEnabled("Vertex Waves Enabled", Float) = 1
        [Enum(Off,0,Front,1,Back,2)] _Cull("Cull Mode", Float) = 0
        [ToggleUI] _ZWrite("Write Depth", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "DisableBatching"="True" }
        LOD 300
        Cull [_Cull]
        ZWrite [_ZWrite]
        ZTest LEqual
        // The source graph composites the background into RGB and outputs alpha 1.
        Blend One Zero

        // Named grab: shared by Water materials, not one screen copy per object.
        GrabPass { "_FantasyCampWaterGrab" }

        CGPROGRAM
        #pragma target 3.0
        #pragma surface Surface Standard fullforwardshadows vertex:Vertex keepalpha nometa exclude_path:deferred exclude_path:prepass
        #pragma multi_compile_instancing
        #include "UnityCG.cginc"
        #include "Assets/Animpic Studio/POLY-FantasyCharacter/Dependencies/Camp/Shaders/HLSL/FantasyCampWaterCommon.hlsl"

        sampler2D _RefreactionNormal;
        UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
        // Unity 2021's non-XR screen-texture macro already ends with ';'.
        // Explicit declarations avoid an empty global statement in the Surface parser.
        #if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
            UNITY_DECLARE_TEX2DARRAY(_FantasyCampWaterGrab);
        #else
            sampler2D_float _FantasyCampWaterGrab;
        #endif
        float4 _FantasyCampWaterGrab_TexelSize;

        struct Input
        {
            float4 screenPos;
            // Original UV0 and post-wave camera-space depth; only one custom register.
            float3 waterUVAndDepth;
            float facing : VFACE;
        };

        void Vertex(inout appdata_full vertex, out Input output)
        {
            UNITY_INITIALIZE_OUTPUT(Input, output);
            vertex.vertex.xyz = FantasyCampWaterPositionOS(vertex.vertex.xyz, _Time.y);
            output.waterUVAndDepth = float3(vertex.texcoord.xy,
                -UnityObjectToViewPos(vertex.vertex).z);
        }

        float2 WaterEyeScreenUV(float4 screenPosition)
        {
            float2 uv = screenPosition.xy / max(screenPosition.w, 0.00001);
        #if defined(UNITY_SINGLE_PASS_STEREO)
            float4 stereoScaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
            uv = (uv - stereoScaleOffset.zw) / stereoScaleOffset.xy;
        #endif
            return uv;
        }

        float WaterSceneEyeDepth(float2 eyeUV)
        {
            float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,
                UnityStereoTransformScreenSpaceTex(eyeUV));
            return FantasyCampWaterEyeDepth(rawDepth);
        }

        float3 WaterGrabColor(float2 eyeUV)
        {
            // ComputeScreenPos and GrabPass use different Y conventions on D3D.
        #if UNITY_UV_STARTS_AT_TOP
            const float grabYSign = -1.0;
        #else
            const float grabYSign = 1.0;
        #endif
            float2 grabUV = eyeUV;
            grabUV.y = (grabUV.y - 0.5) * (grabYSign * _ProjectionParams.x) + 0.5;
            return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_FantasyCampWaterGrab,
                UnityStereoTransformScreenSpaceTex(grabUV)).rgb;
        }

        void Surface(Input input, inout SurfaceOutputStandard output)
        {
            float2 uv = input.waterUVAndDepth.xy;
            float2 screenUV = WaterEyeScreenUV(input.screenPos);
            float waterEyeDepth = input.waterUVAndDepth.z;
            float depthDifference = 100000.0;
            if (_DepthEnabled > 0.5)
                depthDifference = max(WaterSceneEyeDepth(screenUV) - waterEyeDepth, 0.0);

            float3 normalA = UnpackNormal(tex2D(_RefreactionNormal,
                FantasyCampWaterNormalUV(uv, _Time.y, 1.0)));
            float3 normalB = UnpackNormal(tex2D(_RefreactionNormal,
                FantasyCampWaterNormalUV(uv, _Time.y, -1.0)));
            FantasyCampWaterSurface water = FantasyCampEvaluateWater(uv, _Time.y,
                normalA, normalB, depthDifference);

            float3 sceneColor = water.color.rgb;
            if (_RefractionEnabled > 0.5 && water.color.a < 0.9999)
            {
                float2 texelSize = abs(_FantasyCampWaterGrab_TexelSize.xy);
            #if defined(UNITY_SINGLE_PASS_STEREO)
                texelSize /= unity_StereoScaleOffset[unity_StereoEyeIndex].xy;
            #endif
                float2 refractedUV = FantasyCampWaterRefractionUV(screenUV,
                    water.refractionOffset, texelSize);
                if (_DepthEnabled > 0.5
                    && WaterSceneEyeDepth(refractedUV) <= waterEyeDepth + 0.001)
                    refractedUV = screenUV;
                sceneColor = WaterGrabColor(refractedUV);
            }

            output.Albedo = FantasyCampWaterComposite(water.color, sceneColor);
            output.Normal = water.normalTS * (input.facing >= 0.0 ? 1.0 : -1.0);
            output.Metallic = 0.0;
            output.Smoothness = saturate(_Smoothness);
            output.Occlusion = 1.0;
            output.Emission = 0.0;
            output.Alpha = 1.0;
        }
        ENDCG
    }
    // No solid ShadowCaster/depth replacement pass for a refractive water sheet.
    FallBack Off
    CustomEditor "Animpic.Local.POLYFantasyCharacter.Camp.FantasyCampWaterShaderGUI"
}

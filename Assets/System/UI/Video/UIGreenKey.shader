Shader "AllForOne/UI/Green Key Video"
{
    Properties
    {
        [PerRendererData] _MainTex ("Video", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GreenSimilarity ("Green similarity", Range(0,1)) = 0.25
        _GreenSoftness ("Edge softness", Range(0.001,0.5)) = 0.15
        _Despill ("Green spill suppression", Range(0,1)) = 0.9
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 worldPosition:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            fixed4 _Color;
            float4 _ClipRect;
            float _GreenSimilarity, _GreenSoftness, _Despill;
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 pixel=tex2D(_MainTex,i.uv);
                // Chromaticity removes green backgrounds across different brightness levels.
                // Black remains distinct from green, so dark clothing stays opaque.
                float3 chroma=pixel.rgb/max(pixel.r+pixel.g+pixel.b,0.0001);
                float distanceToGreen=distance(chroma,float3(0,1,0));
                float alpha=smoothstep(_GreenSimilarity,_GreenSimilarity+max(_GreenSoftness,0.001),distanceToGreen);
                pixel.g=lerp(pixel.g,min(pixel.g,max(pixel.r,pixel.b)),_Despill);
                pixel.a*=alpha; pixel*=i.color;
                #ifdef UNITY_UI_CLIP_RECT
                pixel.a*=UnityGet2DClipping(i.worldPosition.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(pixel.a-0.001);
                #endif
                return pixel;
            }
            ENDCG
        }
    }
}

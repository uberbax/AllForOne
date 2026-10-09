Shader "Animpic Studio/Fantasy Character/Studio Smooth UI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GradientStart ("Gradient Blue", Color) = (0.03137255,0.49803922,1,1)
        _GradientMiddle ("Gradient Middle", Color) = (0,0.72549020,1,1)
        _GradientEnd ("Gradient Cyan", Color) = (0.09411765,0.83529412,0.81960784,1)
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
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            Name "Smooth UI"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float4 shape : TEXCOORD0;
                float4 radii : TEXCOORD1;
                float4 border : TEXCOORD2;
                float4 style : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 shape : TEXCOORD0;
                float4 radii : TEXCOORD1;
                float4 border : TEXCOORD2;
                float4 style : TEXCOORD3;
                float4 mask : TEXCOORD4;
                float3 fill : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            float4 _Color, _GradientStart, _GradientMiddle, _GradientEnd;
            float4 _ClipRect;
            float _UIMaskSoftnessX, _UIMaskSoftnessY;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.shape = v.shape; o.radii = v.radii; o.border = v.border; o.style = v.style;
                float rg = floor(v.style.z + .5);
                o.fill = float3(floor(rg / 256), rg - floor(rg / 256) * 256, floor(v.style.w + .5)) / 255;
                // UV colours bypass UGUI's vertex colour conversion. Convert once per
                // vertex, with the exact sRGB curve so dark brand colours stay accurate.
                #ifndef UNITY_COLORSPACE_GAMMA
                o.fill = float3(GammaToLinearSpaceExact(o.fill.r), GammaToLinearSpaceExact(o.fill.g), GammaToLinearSpaceExact(o.fill.b));
                o.border.rgb = float3(GammaToLinearSpaceExact(o.border.r), GammaToLinearSpaceExact(o.border.g), GammaToLinearSpaceExact(o.border.b));
                #endif
                // Match UGUI RectMask2D's pixel-scaled soft clipping, including Screen Space Overlay.
                float2 pixelSize = o.vertex.w;
                pixelSize /= max(abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy)), float2(.000001, .000001));
                float4 clipRect = clamp(_ClipRect, -2e10, 2e10);
                o.mask = float4(v.vertex.xy * 2 - clipRect.xy - clipRect.zw,
                    .25 / (.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)));
                return o;
            }

            float RoundedDistance(float2 p, float2 halfSize, float4 radii)
            {
                float radius = p.y > 0 ? (p.x > 0 ? radii.y : radii.x) : (p.x > 0 ? radii.z : radii.w);
                float2 q = abs(p) - halfSize + radius;
                return min(max(q.x, q.y), 0) + length(max(q, 0)) - radius;
            }
            float Coverage(float distance)
            {
                return saturate(.5 - distance / max(fwidth(distance), .00001));
            }
            float4 frag(v2f i) : SV_Target
            {
                float outer = Coverage(RoundedDistance(i.shape.xy, i.shape.zw, i.radii));
                float width = max(i.style.x, 0);
                float inner = outer;
                if (width > 0)
                {
                    float2 innerHalf = max(i.shape.zw - width, 0);
                    inner = Coverage(RoundedDistance(i.shape.xy, innerHalf, max(i.radii - width, 0)));
                    if (min(innerHalf.x, innerHalf.y) <= 0) inner = 0;
                    inner = min(inner, outer);
                }
                float3 fill = i.fill;
                float3 borderRgb = i.border.rgb;
                if (i.style.y > .5)
                {
                    float2 uv = i.shape.xy / max(i.shape.zw * 2, float2(.00001, .00001)) + .5;
                    float t = saturate(uv.x - .08 * (uv.y - .5));
                    fill *= t < .52 ? lerp(_GradientStart.rgb, _GradientMiddle.rgb, t / .52) :
                        lerp(_GradientMiddle.rgb, _GradientEnd.rgb, (t - .52) / .48);
                }
                float border = max(outer - inner, 0) * saturate(i.border.a);
                float alpha = inner + border;
                float3 rgb = (fill * inner + borderRgb * border) / max(alpha, .00001);
                float4 result = float4(rgb, alpha) * i.color;
                #ifdef UNITY_UI_CLIP_RECT
                float2 mask = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
                result.a *= mask.x * mask.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - .001);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
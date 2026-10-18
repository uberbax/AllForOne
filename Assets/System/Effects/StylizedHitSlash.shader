Shader "AllForOne/Effects/StylizedHitSlash"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1,0.3,0.05,1)
        _Shape ("Shape: ribbon / diamond / glow", Float) = 0
        _Fade ("Opacity", Float) = 1
        _Erode ("Dissolve", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend SrcAlpha One
        Cull Off
        ZWrite Off
        Lighting Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            float4 _Color;
            float _Shape, _Fade, _Erode;
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            float4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*2-1;
                float profile;
                if (_Shape < .5) profile=saturate(1-abs(p.x))*pow(saturate(sin(i.uv.y*3.14159265)),.24);
                else if (_Shape < 1.5) profile=saturate(1-abs(p.x)-abs(p.y));
                else if (_Shape < 2.5) profile=pow(saturate(1-dot(p,p)),2);
                else profile=saturate(1-abs(length(p)-.62)*8);
                float noise=frac(sin(dot(floor(i.uv*float2(12,43)),float2(127.1,311.7)))*43758.5453);
                float mask=smoothstep(_Erode, _Erode+.16, noise+.18);
                float core=pow(profile,18);
                float3 tint=_Color.rgb*i.color.rgb;
                float3 rgb=tint*(.7+profile*1.2)+core*float3(1.5,1.5,1.5);
                return float4(rgb, pow(profile,1.5)*i.color.a*_Color.a*_Fade*mask);
            }
            ENDCG
        }
    }
    Fallback Off
}

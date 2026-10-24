Shader "AllForOne/Effects/PlagueGround"
{
    Properties
    {
        _Soil ("Infected soil", Color) = (0.035,0.025,0.050,1)
        _Vein ("Sickly veins", Color) = (0.20,0.27,0.065,1)
        _Opacity ("Opacity", Range(0,1)) = 0.92
        _TrailTime ("Trail time", Float) = 0
        _GrowTime ("Spread time", Float) = .55
        _Lifetime ("Lifetime (0 = persistent)", Float) = 90
        _FadeTime ("Fade time", Float) = 8
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float4 stamp:TEXCOORD0; float2 ground:TEXCOORD1; };
            struct v2f { float4 pos:SV_POSITION; float4 stamp:TEXCOORD0; float2 ground:TEXCOORD1; };
            float4 _Soil, _Vein;
            float _Opacity, _TrailTime, _GrowTime, _Lifetime, _FadeTime;
            float hash21(float2 p) { p=frac(p*float2(123.34,456.21)); p+=dot(p,p+45.32); return frac(p.x*p.y); }
            float noise(float2 p)
            {
                float2 i=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash21(i),hash21(i+float2(1,0)),f.x),lerp(hash21(i+float2(0,1)),hash21(i+1),f.x),f.y);
            }
            float fbm(float2 p) { return noise(p)*.58+noise(p*2.03+7.1)*.28+noise(p*4.07-3.2)*.14; }
            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.stamp=v.stamp; o.ground=v.ground;
                if (v.stamp.z < -999 || (_Lifetime>0 && _TrailTime-v.stamp.z>_Lifetime)) o.pos=float4(2,2,2,1);
                return o;
            }
            float4 frag(v2f i):SV_Target
            {
                float age=max(0,_TrailTime-i.stamp.z);
                float growth=smoothstep(0,_GrowTime,age);
                float fade=_Lifetime>0 ? 1-smoothstep(max(0,_Lifetime-_FadeTime),_Lifetime,age) : 1;
                float2 p=i.stamp.xy;
                float coarse=fbm(i.ground*3.1);
                float fine=noise(i.ground*17.0);
                // No perfect circles: broad lobes, fine fungal filaments and a porous fringe.
                float edge=length(p)+(coarse-.5)*.46+(fine-.5)*.07;
                float reach=lerp(.16,.82,growth);
                float coverage=1-smoothstep(reach-.12,reach+.04,edge);
                clip(coverage*fade-.008);
                float2 warp=float2(noise(i.ground*2.3),noise(i.ground*2.3+31.2))-.5;
                float veinNoise=fbm(i.ground*6.5+warp*2.2);
                float veins=1-smoothstep(.016,.045,abs(veinNoise-.5));
                float mottling=fbm(i.ground*10.5+warp);
                float pulse=.76+.24*sin(_TrailTime*.95+coarse*9);
                float edgeBand=smoothstep(reach-.22,reach-.07,edge)*(1-smoothstep(reach-.04,reach+.04,edge));
                float3 soil=_Soil.rgb*lerp(.65,1.6,mottling);
                float3 bruising=float3(.055,.018,.075)*pow(saturate(mottling),2);
                float3 veinsColor=_Vein.rgb*veins*(.18+.50*edgeBand)*pulse;
                float spores=pow(saturate(fine-.70)*3.3,4)*edgeBand;
                float3 rgb=soil+bruising+veinsColor+_Vein.rgb*spores*.5;
                return float4(rgb,coverage*fade*_Opacity);
            }
            ENDCG
        }
    }
    Fallback Off
}


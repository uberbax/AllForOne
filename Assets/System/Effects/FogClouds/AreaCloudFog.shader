Shader "AllForOne/Effects/AreaCloudFog"
{
    Properties
    {
        _MainTex ("Cloud atlas",2D)="white" {}
        _Area ("Local area: minimum XY, size XY",Vector)=(0,0,1,1)
        _Shadow ("Cloud shadow",Color)=(.22,.29,.36,1)
        _Light ("Cloud light",Color)=(.72,.81,.87,1)
        _Opacity ("Density",Range(0,1))=.9
        _EdgeRoughness ("Edge irregularity",Range(0,3))=1.35
        _EdgeSoftness ("Edge softness",Range(.05,1.5))=.5
        _ClearProgress ("Dispersal",Range(0,1))=0
        _RevealOrigin ("Dispersal origin",Vector)=(.5,.5,0,0)
        _PreviewTime ("Preview time (-1 = game time)",Float)=-1
    }
    SubShader
    {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 cloud:TEXCOORD1;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float2 local:TEXCOORD1;};
            sampler2D _MainTex;
            float4 _Area,_Shadow,_Light,_RevealOrigin;
            float _Opacity,_ClearProgress,_PreviewTime,_EdgeRoughness,_EdgeSoftness;
            float cloudHash(float2 p)
            {
                p=frac(p*float2(123.34,456.21));
                p+=dot(p,p+45.32);
                return frac(p.x*p.y);
            }
            float cloudNoise(float2 p)
            {
                float2 cell=floor(p),f=frac(p);
                f=f*f*(3-2*f);
                return lerp(lerp(cloudHash(cell),cloudHash(cell+float2(1,0)),f.x),
                    lerp(cloudHash(cell+float2(0,1)),cloudHash(cell+1),f.x),f.y);
            }
            v2f vert(appdata v)
            {
                float time=_PreviewTime>=0?_PreviewTime:_Time.y;
                float2 centre=v.cloud.xy;
                float2 origin=_Area.xy+_Area.zw*_RevealOrigin.xy;
                float2 away=normalize(centre-origin+float2(.001,.002));
                float2 drift=float2(sin(time*.21*v.cloud.w+v.cloud.z),cos(time*.17+v.cloud.z*1.7))*float2(.25,.17);
                float breathing=1+.025*sin(time*.31+v.cloud.z);
                float2 cloudPosition=centre+(v.vertex.xy-centre)*breathing+drift;
                cloudPosition+=away*(_ClearProgress*_ClearProgress)*length(_Area.zw)*.65;
                v.vertex.xy=cloudPosition;
                v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.local=cloudPosition;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 offset=i.local-_Area.xy;
                // Rounded area distance with slowly drifting, multiscale edge erosion.
                float corner=min(1.1,min(_Area.z,_Area.w)*.18);
                float2 q=abs(offset-_Area.zw*.5)-(_Area.zw*.5-corner);
                float edge=corner-length(max(q,0))-min(max(q.x,q.y),0);
                float time=_PreviewTime>=0?_PreviewTime:_Time.y;
                float2 noiseUV=i.local*.72+float2(time*.035,-time*.024);
                float noise=cloudNoise(noiseUV)*.65+cloudNoise(noiseUV*2.3+17.7)*.25
                    +cloudNoise(noiseUV*5.1-8.4)*.1;
                float erosion=_EdgeRoughness*(.12+noise);
                float border=smoothstep(erosion,erosion+max(.05,_EdgeSoftness),edge);
                fixed4 texel=tex2D(_MainTex,i.uv);
                float light=dot(texel.rgb,float3(.3,.59,.11));
                float3 rgb=lerp(_Shadow.rgb,_Light.rgb,saturate(light*1.7));
                float reveal=1;
                if(_ClearProgress>.001)
                {
                    float2 origin=_Area.xy+_Area.zw*_RevealOrigin.xy;
                    float wave=sin(i.local.x*2.1+i.local.y*.9)*sin(i.local.y*1.7-i.local.x*.6)*.35;
                    float distance=length(i.local-origin)+wave;
                    float2 farthest=max(_RevealOrigin.xy,1-_RevealOrigin.xy)*_Area.zw;
                    float radius=_ClearProgress*length(farthest)*1.08;
                    reveal=smoothstep(radius-.65,radius+.65,distance);
                }
                float alpha=texel.a*_Opacity*border*reveal*(1-_ClearProgress*_ClearProgress);
                clip(alpha-.003);
                return float4(rgb,alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}



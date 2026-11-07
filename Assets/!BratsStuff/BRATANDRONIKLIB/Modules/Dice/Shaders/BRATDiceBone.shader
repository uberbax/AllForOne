Shader "BRATANDRONIK/DiceBone"
{
    Properties { _Color("Bone tint", Color) = (0.79,0.66,0.43,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 vertex:SV_POSITION; float3 normal:TEXCOORD0; float3 local:TEXCOORD1; float3 view:TEXCOORD2; };
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex);
                o.normal=UnityObjectToWorldNormal(v.normal);
                o.local=v.vertex.xyz;
                o.view=WorldSpaceViewDir(v.vertex); return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 n=normalize(i.normal), l=normalize(float3(-.45,.85,-.65));
                float light=.46+.51*saturate(dot(n,l))+.13*saturate(dot(n,normalize(float3(.6,.3,.8))));
                float grain=sin(i.local.x*87+i.local.z*31)*sin(i.local.y*113-i.local.x*19);
                float stain=sin(i.local.x*11+i.local.y*9)*sin(i.local.z*14-i.local.x*5);
                float shine=pow(saturate(dot(n,normalize(l+normalize(i.view)))),36)*.09;
                float3 color=_Color.rgb*(light*(.96+.018*grain+.035*stain))+shine*_Color.rgb;
                return fixed4(color,1);
            }
            ENDCG
        }
    }
}

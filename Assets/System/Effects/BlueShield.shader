Shader "AllForOne/Effects/BlueShield"
{
    Properties
    {
        [HDR] _Color ("Surface blue", Color) = (0.025,0.3,0.8,1)
        [HDR] _EdgeColor ("Edge glow", Color) = (0.12,0.85,2,1)
        _Opacity ("Surface opacity", Range(0,1)) = .14
        _HexScale ("Hex density", Float) = 9
        _Visibility ("Visibility", Range(0,1)) = 1
        _ShieldTime ("Animation time", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; float3 local:TEXCOORD2; float3 localNormal:TEXCOORD3; };
            float4 _Color, _EdgeColor;
            float _Opacity, _HexScale, _Visibility, _ShieldTime;

            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.normal=UnityObjectToWorldNormal(v.normal);
                o.local=v.vertex.xyz; o.localNormal=v.normal; return o;
            }
            float hexGrid(float2 p)
            {
                float2 lattice=float2(1,1.7320508);
                float2 a=(frac(p/lattice)-.5)*lattice;
                float2 b=(frac((p-.5*lattice)/lattice)-.5)*lattice;
                float2 cell=dot(a,a)<dot(b,b)?a:b;
                cell=abs(cell);
                float d=max(cell.x,dot(cell,float2(.5,.8660254)));
                return smoothstep(.465,.495,d);
            }
            float4 frag(v2f i):SV_Target
            {
                float3 normal=normalize(i.normal);
                float3 view=normalize(_WorldSpaceCameraPos-i.world);
                float rim=pow(1-saturate(dot(normal,view)),2.6);
                float3 weights=pow(abs(normalize(i.localNormal)),6);
                weights/=max(.0001,weights.x+weights.y+weights.z);
                float3 p=i.local*_HexScale;
                float grid=hexGrid(p.yz)*weights.x+hexGrid(p.xz)*weights.y+hexGrid(p.xy)*weights.z;
                float sweep=pow(.5+.5*sin(i.local.y*32-_ShieldTime*2.8),18);
                float pulse=.88+.12*sin(_ShieldTime*2);
                float alpha=saturate((_Opacity+rim*.58+grid*.16+sweep*.08)*_Visibility*pulse);
                float3 rgb=lerp(_Color.rgb,_EdgeColor.rgb,saturate(rim+grid*.4+sweep*.18));
                return float4(rgb,alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}

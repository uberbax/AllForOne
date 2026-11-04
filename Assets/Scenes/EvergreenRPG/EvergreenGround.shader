Shader "AllForOne/Evergreen RPG Ground"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _GrassTex ("Grass", 2D) = "white" {}
        _StoneTex ("Stone path", 2D) = "white" {}
        _GrassScale ("Grass tiling per metre", Float) = .15
        _StoneScale ("Stone tiling per metre", Float) = .25
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150
        CGPROGRAM
        #pragma surface surf Lambert vertex:vert addshadow
        #pragma target 3.0
        fixed4 _Color;
        sampler2D _GrassTex, _StoneTex;
        float _GrassScale, _StoneScale;
        struct Input { float4 vertexColor; float3 worldPos; };
        void vert(inout appdata_full v, out Input o) { UNITY_INITIALIZE_OUTPUT(Input,o); o.vertexColor=v.color; }
        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed3 grass=tex2D(_GrassTex,IN.worldPos.xz*_GrassScale).rgb;
            fixed3 stone=tex2D(_StoneTex,IN.worldPos.xz*_StoneScale).rgb;
            o.Albedo=lerp(grass,stone,saturate(IN.vertexColor.a))*_Color.rgb;
            o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}

Shader "BRATANDRONIK/DiceShadow"
{
    Properties { _Opacity("Opacity",Range(0,1))=.3 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off Cull Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Opacity;
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_base v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
            fixed4 frag(v2f i):SV_Target{float2 p=(i.uv-.5)*2;float a=pow(saturate(1-dot(p,p)),2)*_Opacity;return fixed4(.08,.045,.02,a);}
            ENDCG
        }
    }
}

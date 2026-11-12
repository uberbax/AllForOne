Shader "BRAT/Story/Illustration Color"
{
 Properties
 {
  [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
  _Color ("Tint", Color) = (1,1,1,1)
  _Saturation ("Saturation", Range(0,4)) = 2.8
  _Balance ("Color balance", Vector) = (1.07,0.99,0.92,1)
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
  Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
  Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  ColorMask [_ColorMask]
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
   struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; };
   sampler2D _MainTex;
   float4 _Color, _Balance, _TextureSampleAdd, _ClipRect;
   float _Saturation;
   v2f vert(appdata v)
   {
    v2f o; o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;
   }
   float4 frag(v2f i):SV_Target
   {
    float4 c=tex2D(_MainTex,i.uv)+_TextureSampleAdd;
    // Work in display colour so the adjustment is stable in Gamma and Linear projects.
    #ifndef UNITY_COLORSPACE_GAMMA
     c.rgb=LinearToGammaSpace(c.rgb);
    #endif
    c.rgb*= _Balance.rgb;
    float luminance=dot(c.rgb,float3(.2126,.7152,.0722));
    c.rgb=saturate(lerp(luminance.xxx,c.rgb,_Saturation));
    #ifndef UNITY_COLORSPACE_GAMMA
     c.rgb=GammaToLinearSpace(c.rgb);
    #endif
    c*=i.color;
    #ifdef UNITY_UI_CLIP_RECT
     c.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
     clip(c.a-.001);
    #endif
    return c;
   }
   ENDCG
  }
 }
}

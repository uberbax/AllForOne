Shader "AllForOne/Effects/WitheredTree"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture",2D)="white" {}
        _Color ("Tint",Color)=(1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor",Color)=(1,1,1,1)
        [HideInInspector] _Flip ("Flip",Vector)=(1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha",2D)="white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha",Float)=0
        _PlagueTransform ("Scale XY and offset XY",Vector)=(1,1,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" "DisableBatching"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex WitheredVert
            #pragma fragment SpriteFrag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float4 _PlagueTransform;
            v2f WitheredVert(appdata_t input)
            {
                input.vertex.xy=input.vertex.xy*_PlagueTransform.xy+_PlagueTransform.zw;
                return SpriteVert(input);
            }
            ENDCG
        }
    }
}

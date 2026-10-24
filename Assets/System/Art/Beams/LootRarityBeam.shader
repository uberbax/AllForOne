Shader "AllForOne/Effects/LootRarityBeam"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite",2D)="white" {}
        _Color ("Tint",Color)=(1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color",Color)=(1,1,1,1)
        [HideInInspector] _Flip ("Flip",Vector)=(1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha",2D)="white" {}
        [PerRendererData] _EnableExternalAlpha ("External Alpha Enabled",Float)=0
        [HDR] _BeamTint ("Rarity Color",Color)=(1,1,1,1)
        _Opacity ("Opacity",Range(0,1))=1
        _Ground ("Ground Glow",Float)=0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Lighting Off
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float4 _BeamTint;
            float _Opacity, _Ground;
            fixed4 frag(v2f i):SV_Target
            {
                float mask;
                if (_Ground>.5)
                {
                    float2 p=i.texcoord*2-1;
                    float r=dot(p,p);
                    mask=pow(saturate(1-r),2.5)*.58;
                }
                else
                {
                    fixed4 texel=SampleSpriteTexture(i.texcoord);
                    float value=max(texel.r,max(texel.g,texel.b));
                    mask=texel.a*sqrt(max(0,value));
                }
                return float4(_BeamTint.rgb*1.5+pow(saturate(mask),6)*.22,mask*_Opacity*i.color.a);
            }
            ENDCG
        }
    }
}


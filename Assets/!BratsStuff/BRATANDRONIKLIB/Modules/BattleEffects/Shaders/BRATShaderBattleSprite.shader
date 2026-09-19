Shader "BratAndronik/Battle Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [MaterialToggle] PixelSnap ("Pixel Snap", Float) = 0
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip ("Flip", Vector) = (1, 1, 1, 1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

        _BattleBlur ("Battle Blur (Texture Pixels)", Range(0, 4)) = 0
        _BattleTint ("Battle Tint (RGB)", Color) = (1, 1, 1, 1)
        _BattleFlash ("Battle Flash (A = Strength)", Color) = (1, 1, 1, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment BattleSpriteFrag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnitySprites.cginc"

            float4 _MainTex_TexelSize;
            float _BattleBlur;
            half4 _BattleTint;
            half4 _BattleFlash;

            half4 SampleBattleSprite(float2 uv)
            {
                half4 color = SampleSpriteTexture(uv);
                // Filter premultiplied samples so transparent texels add no dark fringe.
                color.rgb *= color.a;
                return color;
            }

            half4 BattleSpriteFrag(v2f input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 color = SampleBattleSprite(input.texcoord);
                float radius = clamp(_BattleBlur, 0.0, 4.0);
                if (radius > 0.0)
                {
                    float2 offset = _MainTex_TexelSize.xy * radius;
                    // A normalized 3 x 3 Gaussian kernel: 1/2/1, 2/4/2, 1/2/1.
                    color *= 4.0h;
                    color += SampleBattleSprite(input.texcoord + float2(-offset.x, 0.0)) * 2.0h;
                    color += SampleBattleSprite(input.texcoord + float2( offset.x, 0.0)) * 2.0h;
                    color += SampleBattleSprite(input.texcoord + float2(0.0, -offset.y)) * 2.0h;
                    color += SampleBattleSprite(input.texcoord + float2(0.0,  offset.y)) * 2.0h;
                    color += SampleBattleSprite(input.texcoord + float2(-offset.x, -offset.y));
                    color += SampleBattleSprite(input.texcoord + float2( offset.x, -offset.y));
                    color += SampleBattleSprite(input.texcoord + float2(-offset.x,  offset.y));
                    color += SampleBattleSprite(input.texcoord + float2( offset.x,  offset.y));
                    color *= 0.0625h;
                }

                // SpriteVert supplies vertex, material and SpriteRenderer tint/opacity.
                color.rgb *= input.color.rgb * input.color.a * _BattleTint.rgb;
                color.a *= input.color.a;
                color.rgb = lerp(color.rgb, _BattleFlash.rgb * color.a, saturate(_BattleFlash.a));
                return color;
            }
            ENDCG
        }
    }
}

Shader "AllForOne/Sprites/Death Disintegration"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _DeathDissolve ("Death Disintegration", Range(0,1)) = 0
        _DeathBounds ("Sprite Bounds", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            #include "Assets/System/Effects/DeathDisintegration.cginc"

            float _DeathDissolve;
            float4 _DeathBounds;
            struct Interpolators
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 dissolveUV : TEXCOORD1;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Interpolators vert(appdata_t input)
            {
                Interpolators output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.vertex = UnityObjectToClipPos(UnityFlipSprite(input.vertex.xyz, _Flip));
                output.uv = input.texcoord;
                output.dissolveUV = (input.vertex.xy - _DeathBounds.xy) / max(_DeathBounds.zw, 0.0001);
                output.dissolveUV.y = lerp(1 - output.dissolveUV.y, output.dissolveUV.y, step(0, _Flip.y));
                output.color = input.color * _Color * _RendererColor;
                return output;
            }

            fixed4 frag(Interpolators input) : SV_Target
            {
                float2 dx = ddx(input.dissolveUV), dy = ddy(input.dissolveUV);
                float2 uvDx = ddx(input.uv), uvDy = ddy(input.uv);
                float4 pixel = SampleSpriteTexture(input.uv);
                if (_DeathDissolve > 0)
                {
                    bool flipped = dx.x < 0;
                    float2 burnUV = float2(flipped ? 1 - input.dissolveUV.x : input.dissolveUV.x, input.dissolveUV.y);
                    pixel = ApplyDeathDisintegration(pixel, burnUV, _DeathDissolve);
                    float2 sourceUV;
                    float ash = FlyingAsh(burnUV, _DeathDissolve, sourceUV);
                    float determinant = dx.x * dy.y - dx.y * dy.x;
                    if (ash > 0 && abs(determinant) > 1e-9)
                    {
                        if (flipped) sourceUV.x = 1 - sourceUV.x;
                        float2 delta = sourceUV - input.dissolveUV;
                        float2 screenOffset = float2(delta.x * dy.y - delta.y * dy.x,
                            dx.x * delta.y - dx.y * delta.x) / determinant;
                        float4 fragment = SampleSpriteTexture(input.uv + screenOffset.x * uvDx + screenOffset.y * uvDy);
                        pixel = CompositeAsh(pixel, fragment, ash);
                    }
                }
                pixel *= input.color;
                pixel.rgb *= pixel.a;
                return pixel;
            }
            ENDCG
        }
    }
}

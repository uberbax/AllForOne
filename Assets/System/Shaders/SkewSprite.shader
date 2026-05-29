Shader "Sprites/AdvancedShear"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        _ShearX ("Shear X", Range(-2, 2)) = 0
        _ShearY ("Shear Y", Range(-2, 2)) = 0
        _ShearCenterX ("Shear Center X", Range(0, 1)) = 0.5
        _ShearCenterY ("Shear Center Y", Range(0, 1)) = 0.5
        _PivotType ("Pivot Type", Range(0, 2)) = 0
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma multi_compile _ PIXELSNAP_ON
        #include "UnityCG.cginc"

        struct appdata_t
        {
            float4 vertex   : POSITION;
            float4 color    : COLOR;
            float2 texcoord : TEXCOORD0;
        };

        struct v2f
        {
            float4 vertex   : SV_POSITION;
            fixed4 color    : COLOR;
            float2 texcoord : TEXCOORD0;
        };

        fixed4 _Color;
        float _ShearX;
        float _ShearY;
        float _ShearCenterX;
        float _ShearCenterY;
        float _PivotType;

        v2f vert(appdata_t v)
        {
            v2f o;
            
            // Calculate object bounds (assuming quad from -0.5 to 0.5)
            float width = 1.0;
            float height = 1.0;
            
            // Calculate shear center based on pivot type
            float2 shearCenter = float2(0, 0);
            
            if (_PivotType < 0.5) // Custom center
            {
                shearCenter = float2(
                    (_ShearCenterX - 0.5) * width,
                    (_ShearCenterY - 0.5) * height
                );
            }
            else if (_PivotType < 1.5) // Center pivot
            {
                shearCenter = float2(0, 0); // Already centered
            }
            else // Bottom-left pivot
            {
                shearCenter = float2(-0.5, -0.5);
            }
            
            // Move to shear center, apply shear, then move back
            float4 pos = v.vertex;
            pos.xy -= shearCenter;
            
            // Apply shear transformation
            float4 shearedPos = pos;
            shearedPos.x += pos.y * _ShearX;
            shearedPos.y += pos.x * _ShearY;
            
            shearedPos.xy += shearCenter;
            
            o.vertex = UnityObjectToClipPos(shearedPos);
            o.texcoord = v.texcoord;
            o.color = v.color * _Color;

            #ifdef PIXELSNAP_ON
            o.vertex = UnityPixelSnap(o.vertex);
            #endif

            return o;
        }

        sampler2D _MainTex;

        fixed4 frag(v2f i) : SV_Target
        {
            fixed4 c = tex2D(_MainTex, i.texcoord) * i.color;
            c.rgb *= c.a;
            return c;
        }
        ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
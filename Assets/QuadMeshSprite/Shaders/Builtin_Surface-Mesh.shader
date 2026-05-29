Shader "Sprites/Surface-Mesh"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        [HDR] _Color("Tint", Color) = (0.5,0.5,0.5,1)
        _Flip("Flip", Vector) = (-1,1,1,1)
        _Clip("Clip Alpha", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        LOD 200
        CULL OFF

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert alpha

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _MaskTex;

        half _Clip;
        fixed4 _Flip;
        fixed4 _Color;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_MaskTex;
        };

        void vert(inout appdata_full v) {
            if (_Flip.x < 0) { v.vertex.x *= -1; }
            if (_Flip.y < 0) { v.vertex.y *= -1; }

        }

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 m = tex2D (_MaskTex, IN.uv_MaskTex);
            clip(m.a - _Clip);
            fixed4 main = tex2D(_MainTex, IN.uv_MainTex);
            clip(main.a-0.5);

            half4 y1 = 2 * main * _Color;
            half4 y2 = -(1 - 2 * main) + (1 - main) * 2 * _Color;
            half4 color = saturate((1 - _Color) * y1 + (_Color * y2));
            color.a = main.a * _Color.a;

            o.Albedo = color.rgb;
            // Metallic and smoothness come from slider variables
            o.Alpha = color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

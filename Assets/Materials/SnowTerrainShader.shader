Shader "Custom/SnowTerrainShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Color2 ("Color2", Color) = (0.5, 0.5, 0.5, 1)
        _ColorBlend ("ColorBlend", float) = 1
        _ColorBlendAngle ("ColorBlendAngle", float) = 0.5
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Glossiness2 ("Smoothness2", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM


        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        //#include "noiseSimplex.cginc"

        sampler2D _MainTex;

        struct Input
        {
            //float2 uv_MainTex;
            float3 worldNormal;
        };

        half _Glossiness;
        half _Glossiness2;
        half _Metallic;
        fixed4 _Color;
        fixed4 _Color2;
        float _ColorBlend;
        float _ColorBlendAngle;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float d = (dot(IN.worldNormal, float3(0, 1, 0))) * 0.5;

            d *= _ColorBlend;
            d += _ColorBlendAngle;

            if (d > 1) d = 1;
            if (d < 0) d = 0;

            //float colorOffset = snoise();

            //_Color = _Color + fixed4();

            fixed4 c = _Color * d + (_Color2 * (1-d));

            // Albedo comes from a texture tinted by color
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness * d + (_Glossiness2 * (1-d));
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

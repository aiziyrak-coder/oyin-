// Daraxt barglari va butalar: alfa bo'yicha kesiladi (Cutout), ikki tomoni ham ko'rinadi (Cull Off),
// orqa tomondan tushgan quyosh nuri barg orqali biroz o'tadi (yengil "translucency").
Shader "CraDev/Foliage"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        [Normal] _BumpMap ("Normal", 2D) = "bump" {}
        _Cutoff ("Alpha cutoff", Range(0, 1)) = 0.45
        _Glossiness ("Smoothness", Range(0, 1)) = 0.25
        _Translucency ("Translucency", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" }
        Cull Off
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard alphatest:_Cutoff addshadow fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        fixed4 _Color;
        half _Glossiness;
        half _Translucency;

        struct Input
        {
            float2 uv_MainTex;
            fixed facing : VFACE;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            float3 n = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            // Orqa tomonda normal teskari: barg ikki tomondan ham to'g'ri yoritiladi
            n.z *= IN.facing > 0 ? 1 : -1;
            o.Normal = n;
            o.Smoothness = _Glossiness;
            o.Metallic = 0;
            o.Emission = c.rgb * _Translucency * 0.15;
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}

// Ko'rinmas pol: faqat unga tushgan soya chiziladi. Bosh menyuda 3D qahramon orqa fondagi rasm (platforma)
// ustida turadi - shu pol qahramon soyasini rasmga tushiradi, qahramon "havoda" turgandek ko'rinmaydi.
Shader "CraDev/ShadowCatcher"
{
    Properties
    {
        _ShadowColor ("Shadow color", Color) = (0.02, 0.04, 0.08, 1)
        _Strength ("Shadow strength", Range(0, 1)) = 0.55
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest+50" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        #pragma surface surf ShadowOnly alpha:fade fullforwardshadows noambient nolightmap nodynlightmap nodirlightmap nometa noforwardadd
        #pragma target 3.0

        fixed4 _ShadowColor;
        half _Strength;

        struct Input { float3 worldPos; };

        void surf (Input IN, inout SurfaceOutput o)
        {
            o.Albedo = _ShadowColor.rgb;
            o.Alpha = 1;
        }

        half4 LightingShadowOnly (SurfaceOutput s, half3 lightDir, half atten)
        {
            half4 c;
            c.rgb = s.Albedo;
            c.a = (1 - atten) * _Strength;
            return c;
        }
        ENDCG
    }
    FallBack Off
}

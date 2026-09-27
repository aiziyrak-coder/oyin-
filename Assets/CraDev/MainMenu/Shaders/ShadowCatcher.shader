// Ko'rinmas pol: faqat unga tushgan soya chiziladi. Lobbyda 3D qahramon orqa fondagi rasm ustida turadi - shu pol
// qahramonning quyosh soyasini va oyoq ostidagi yumshoq "kontakt" soyani rasmga tushiradi, qahramon havoda
// turgandek ko'rinmaydi.
// Soya olish uchun pol "Opaque" navbatida (Geometry+10) chiziladi va kamera chuqurlik teksturasiga yoziladi
// (FallBack'dagi ShadowCaster pass); o'zi soya tashlamaydi (renderer.shadowCastingMode = Off).
Shader "CraDev/ShadowCatcher"
{
    Properties
    {
        _ShadowColor ("Shadow color", Color) = (0.02, 0.03, 0.06, 1)
        _Strength ("Sun shadow strength", Range(0, 1)) = 0.55
        _BlobStrength ("Contact shadow strength", Range(0, 1)) = 0.45
        _BlobRadius ("Contact shadow radius (0..1 of the quad)", Range(0.05, 1)) = 0.32
    }
    SubShader
    {
        Tags { "Queue" = "Geometry+10" "RenderType" = "Opaque" "IgnoreProjector" = "True" }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"

            fixed4 _ShadowColor;
            half _Strength, _BlobStrength, _BlobRadius;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                SHADOW_COORDS(1)
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half atten = SHADOW_ATTENUATION(i);
                half d = length(i.uv - 0.5) * 2.0;           // 0 - markaz, 1 - chet
                half blob = saturate(1.0 - d / _BlobRadius);
                blob = blob * blob * _BlobStrength;
                half a = max((1.0 - atten) * _Strength, blob);
                a *= saturate((1.0 - d) * 4.0);              // pol chetida soya yo'qoladi
                return fixed4(_ShadowColor.rgb, a);
            }
            ENDCG
        }
    }
    FallBack "Legacy Shaders/VertexLit"
}

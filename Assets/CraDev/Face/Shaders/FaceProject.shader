// O'yinchi rasmidagi yuzni bosh teksturasiga chizish uchun (FacePainter). Rasm rangi teri rangiga
// moslashtiriladi (_Gain), shaffoflik esa uchburchak uchlaridagi rangdan olinadi: chetlari silliq so'nadi.
Shader "Hidden/CraDev/FaceProject"
{
    Properties
    {
        _MainTex ("Photo", 2D) = "white" {}
        _Gain ("Skin gain", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Gain;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                c.rgb *= _Gain.rgb;
                c.a = i.color.a;
                return c;
            }
            ENDCG
        }
    }
}

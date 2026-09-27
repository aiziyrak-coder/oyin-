Shader "CraDev/WorldUVPBR"
{
    Properties
    {
        _Color ("Tint",Color)=(1,1,1,1)
        _MainTex ("Scanned albedo",2D)="white"{}
        _BumpMap ("Scanned normal",2D)="bump"{}
        _Roughness ("Roughness",2D)="white"{}
        _Occlusion ("Occlusion",2D)="white"{}
        _NormalStrength ("Relief",Range(0,2))=1
        _Smoothness ("Smoothness multiplier",Range(0,1))=.8
    }
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        sampler2D _MainTex,_BumpMap,_Roughness,_Occlusion;
        fixed4 _Color;
        float _NormalStrength,_Smoothness;
        struct Input {float2 uv_MainTex;};
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            o.Albedo=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;
            float3 normal=UnpackNormal(tex2D(_BumpMap,IN.uv_MainTex));normal.xy*=_NormalStrength;o.Normal=normalize(normal);
            o.Metallic=0;o.Smoothness=(1-tex2D(_Roughness,IN.uv_MainTex).r)*_Smoothness;
            o.Occlusion=tex2D(_Occlusion,IN.uv_MainTex).r;o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Standard"
}

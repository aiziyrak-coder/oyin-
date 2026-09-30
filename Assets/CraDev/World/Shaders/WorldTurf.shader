Shader "CraDev/WorldTurf"
{
    Properties
    {
        _Color ("Turf tint", Color) = (.24,.53,.19,1)
        _MainTex ("Grass albedo", 2D) = "white" {}
        _BumpMap ("Grass detail", 2D) = "bump" {}
        _Roughness ("Grass roughness", 2D) = "white" {}
        _Occlusion ("Grass occlusion", 2D) = "white" {}
        _Scale ("Repeats per metre", Float) = 1.1
        _NormalStrength ("Relief", Range(0,2)) = .2
        _Smoothness ("Smoothness", Range(0,1)) = .16
        _MacroStrength ("Variation", Range(0,1)) = .1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 250
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma target 3.0
        sampler2D _MainTex, _BumpMap, _Roughness, _Occlusion;
        fixed4 _Color;
        float _Scale, _NormalStrength, _Smoothness, _MacroStrength;
        struct Input { float3 worldPos; };
        void vert(inout appdata_full v) { v.tangent = float4(1,0,0,-1); }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.worldPos.xz * _Scale;
            float3 raw = tex2D(_MainTex, uv).rgb;
            float detail = dot(raw, float3(.25,.6,.15));
            // Mowing bands follow the field length. They are surface shading, never raised geometry.
            float band = step(.5, frac((IN.worldPos.z - 44.2) / 8.2));
            float stripe = lerp(.86, 1.08, band);
            float fine = tex2D(_MainTex, uv * 3.31).g;
            float blades = lerp(.84,1.12,saturate(detail * 3)) * lerp(.92,1.05,saturate(fine * 3));
            o.Albedo = _Color.rgb * blades * stripe;
            float3 n = UnpackNormal(tex2D(_BumpMap,uv * 2.2)); n.xy *= _NormalStrength;
            o.Normal = normalize(n);
            o.Metallic = 0;
            o.Smoothness = (1 - tex2D(_Roughness, uv).r) * _Smoothness;
            o.Occlusion = lerp(1,tex2D(_Occlusion,uv).r,.25);
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Standard"
}

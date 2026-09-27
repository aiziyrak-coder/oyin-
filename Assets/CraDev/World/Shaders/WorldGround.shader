Shader "CraDev/WorldGround"
{
    Properties
    {
        _Color ("Ground", Color) = (.32,.4,.28,1)
        _Grid ("Survey lines", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        fixed4 _Color;
        float _Grid;
        struct Input { float3 worldPos; };
        float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
        float noise(float2 p)
        {
            float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
            return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float2 p=IN.worldPos.xz;
            float patch=noise(p*.22)*.12+noise(p*3)*.025;
            float2 g=abs(frac(p/5+.5)-.5)*5;
            float gridLine=1-smoothstep(.018,.018+max(fwidth(p.x),fwidth(p.y)),min(g.x,g.y));
            o.Albedo=_Color.rgb*(.94+patch)*(1-gridLine*_Grid*.18);
            o.Metallic=0;o.Smoothness=.08;o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

Shader "CraDev/WorldPBR"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Photographic albedo",2D) = "white" {}
        _BumpMap ("Surface normal",2D) = "bump" {}
        _Roughness ("Roughness",2D) = "white" {}
        _Occlusion ("Occlusion",2D) = "white" {}
        _Scale ("Repeats per metre",Float) = .5
        _NormalStrength ("Relief",Range(0,2)) = 1
        _Smoothness ("Smoothness multiplier",Range(0,1)) = .65
        _MacroStrength ("Landscape variation",Range(0,1)) = 0
    }
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        LOD 300
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0
        sampler2D _MainTex,_BumpMap,_Roughness,_Occlusion;
        fixed4 _Color;
        float _Scale,_NormalStrength,_Smoothness,_MacroStrength;
        struct Input { float3 worldPos; float3 worldNormal; INTERNAL_DATA };
        void vert(inout appdata_full v)
        {
            // Procedural meshlar ham normal xarita uchun barqaror tangent oladi.
            float3 n=normalize(v.normal);
            float3 axis=abs(n.y)<.95?float3(0,1,0):float3(0,0,1);
            v.tangent=float4(normalize(cross(axis,n)),1);
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float3 n=normalize(WorldNormalVector(IN,float3(0,0,1)));
            float3 w=pow(abs(n),4);w/=max(dot(w,1),.0001);
            float3 p=IN.worldPos*_Scale;
            float2 x=p.zy,y=p.xz,z=p.xy;
            fixed3 albedo=tex2D(_MainTex,x).rgb*w.x+tex2D(_MainTex,y).rgb*w.y+tex2D(_MainTex,z).rgb*w.z;
            float rough=dot(float3(tex2D(_Roughness,x).r,tex2D(_Roughness,y).r,tex2D(_Roughness,z).r),w);
            float ao=dot(float3(tex2D(_Occlusion,x).r,tex2D(_Occlusion,y).r,tex2D(_Occlusion,z).r),w);
            float3 nx=UnpackNormal(tex2D(_BumpMap,x));float3 ny=UnpackNormal(tex2D(_BumpMap,y));float3 nz=UnpackNormal(tex2D(_BumpMap,z));
            float3 perturb=float3(0,nx.y,nx.x)*w.x+float3(ny.x,0,ny.y)*w.y+float3(nz.x,nz.y,0)*w.z;
            perturb-=n*dot(perturb,n);
            float3 finalNormal=normalize(n+perturb*_NormalStrength);
            float3 tx=normalize(WorldNormalVector(IN,float3(1,0,0)));
            float3 ty=normalize(WorldNormalVector(IN,float3(0,1,0)));
            o.Normal=normalize(float3(dot(finalNormal,tx),dot(finalNormal,ty),dot(finalNormal,n)));
            float macro=1+.035*sin(IN.worldPos.x*.43+sin(IN.worldPos.z*.27));
            float largeScale=tex2D(_MainTex,IN.worldPos.xz*.023).g;
            macro*=lerp(1,lerp(.62,1.25,saturate(largeScale*2.5)),_MacroStrength);
            o.Albedo=albedo*_Color.rgb*macro;
            o.Metallic=0;o.Smoothness=(1-rough)*_Smoothness;o.Occlusion=lerp(1,ao,.75);o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Standard"
}

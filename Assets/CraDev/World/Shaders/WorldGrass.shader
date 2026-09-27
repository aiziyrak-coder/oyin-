Shader "CraDev/WorldGrass"
{
    Properties { _Color ("Grass tint", Color) = (.22,.29,.12,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0
        fixed4 _Color;
        struct Input { float2 blade; float3 worldPos; };
        void vert(inout appdata_full v,out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            o.blade=v.texcoord.xy;
            float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
            float bend=v.texcoord.y*v.texcoord.y;
            float wind=sin(world.x*.61+world.z*.32+_Time.y*1.15)*.55+sin(world.z*.9-_Time.y*.8)*.25;
            v.vertex.x+=wind*.035*bend;v.vertex.z+=wind*.018*bend;
            // Ingichka bargga yumshoq osmon yoritishi: qora uchburchak silueti hosil bo'lmaydi.
            v.normal=normalize(float3(v.normal.x*.25,1,v.normal.z*.25));
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float variation=.85+.15*sin(IN.worldPos.x*1.73+IN.worldPos.z*2.39);
            fixed3 tip=lerp(_Color.rgb*.55,_Color.rgb*1.3,saturate(IN.blade.y));
            o.Albedo=tip*variation;o.Metallic=0;o.Smoothness=.08;
            o.Occlusion=lerp(.7,1,IN.blade.y);o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}

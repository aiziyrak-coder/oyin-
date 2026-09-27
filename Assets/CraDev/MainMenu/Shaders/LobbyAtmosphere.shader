Shader "CraDev/LobbyAtmosphere"
{
    Properties { [PerRendererData] _MainTex("Background",2D)="white"{} _Motion("Motion",Float)=1 _Weather("Weather",Float)=0 _Night("Night",Float)=0 }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
        Cull Off Lighting Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            sampler2D _MainTex;float _Motion,_Weather,_Night;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=i.uv;float t=_Time.y;
                // Only open sky/water beyond windows move; architecture and floor stay fixed.
                float outside=smoothstep(.40,.46,uv.x)*(1-smoothstep(.94,.96,uv.x))*smoothstep(.36,.43,uv.y)*(1-smoothstep(.47+uv.x*.50,.51+uv.x*.50,uv.y));
                float sky=outside*smoothstep(.61,.68,uv.y);
                float water=outside*smoothstep(.40,.44,uv.y)*(1-smoothstep(.46,.49,uv.y));
                uv.x+=_Motion*(sky*.0025*sin(t*.11+uv.y*13)+water*.0007*sin(uv.y*260+t*.8));
                fixed4 c=tex2D(_MainTex,uv)*i.color;
                float sparkle=step(.997,hash(floor(i.uv*float2(640,360))))*outside*(1-smoothstep(.54,.59,i.uv.y));
                c.rgb+=sparkle*_Night*_Motion*.08*(.5+.5*sin(t*.7+i.uv.x*35));
                if(_Weather>.5&&_Motion>.5)
                {
                    if(_Weather<1.5)
                    {
                        float2 p=i.uv*float2(170,38)+float2(t*2,t*18);
                        float r=hash(floor(p));float2 f=frac(p);
                        float rain=step(.80,r)*(1-smoothstep(.025,.06,abs(f.x-.5)))*(1-smoothstep(.2,.8,f.y));
                        c.rgb+=rain*outside*.16;
                    }
                    else
                    {
                        float2 p=i.uv*float2(65,38)+float2(sin(t*.2)*.6,t*.65);
                        float r=hash(floor(p));float flake=(1-smoothstep(.025,.07,length(frac(p)-.5)))*step(.55,r);
                        c.rgb=lerp(c.rgb,float3(.88,.92,1),flake*outside*.6);
                    }
                }
                return c;
            }
            ENDCG
        }
    }
}

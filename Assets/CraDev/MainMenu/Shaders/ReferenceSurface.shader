Shader "CraDev/ReferenceSurface"
{
    Properties
    {
        [PerRendererData] _MainTex("Texture",2D)="white"{} _Art("Landscape",2D)="white"{}
        _StencilComp("Stencil Comparison",Float)=8 _Stencil("Stencil ID",Float)=0 _StencilOp("Stencil Operation",Float)=0
        _StencilWriteMask("Stencil Write Mask",Float)=255 _StencilReadMask("Stencil Read Mask",Float)=255 _ColorMask("Color Mask",Float)=15
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 shape:TEXCOORD1;fixed4 color:COLOR;};
            struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 shape:TEXCOORD1;fixed4 color:COLOR;float4 world:TEXCOORD2;};
            sampler2D _Art;float4 _ClipRect;
            float3 Art(float2 uv)
            {
                float3 c=tex2D(_Art,uv).rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                c=LinearToGammaSpace(c);
                #endif
                return c;
            }
            v2f vert(appdata v){v2f o;o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.shape=v.shape;o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=i.uv;float2 size=i.shape.xy;float r=min(i.shape.z,min(size.x,size.y)*.5-2);
                float2 q=abs((uv-.5)*size)-(size*.5-2-r);
                float d=length(max(q,0))+min(max(q.x,q.y),0)-r;
                float aa=max(.6,fwidth(d));float body=1-smoothstep(-aa,aa,d);
                float border=(1-smoothstep(1,2.7,abs(d)))*body;
                float gloss=pow(uv.y,7)*.035;
                float3 color=lerp(float3(.13,.12,.13),float3(.23,.20,.21),uv.y)+gloss;
                float alpha=.95;float3 rim=float3(.46,.46,.49);float rimPower=.42;
                int style=(int)(i.shape.w+.5);
                if(style==1)
                {
                    float3 green=lerp(float3(.21,.27,.21),float3(.30,.38,.31),uv.y);
                    color=lerp(green,float3(.89,.64,.36),smoothstep(.53,1,uv.x));
                    float3 art=Art(float2(uv.x*2.8,uv.y))*float3(.66,1.12,.72);
                    color=lerp(color,art,1-smoothstep(.12,.39,uv.x));
                    color+=float3(.32,.77,.44)*exp(-length((uv-float2(.13,1))*float2(2,1))*6)*.65;
                    color+=float3(1,.85,.5)*pow(uv.x,8)*.10;
                    color+=pow(uv.y,12)*.20;rim=lerp(float3(.65,1,.74),float3(1,.94,.72),uv.x);rimPower=.85;alpha=1;
                }
                else if(style==2)
                {
                    color=lerp(float3(.025,.28,.20),float3(.31,.83,.49),saturate(uv.y*.75+(1-uv.x)*.35));
                    color+=pow(saturate(uv.y),8)*.16;rim=float3(.46,.94,.69);rimPower=.7;alpha=.98;
                }
                else if(style==3)
                {
                    float3 art=Art(float2(uv.x,uv.y*.62+.15));
                    color=lerp(float3(.095,.10,.12),art,smoothstep(.22,.70,uv.x));
                    color*=1-.32*(1-uv.y);rim=float3(.62,.60,.56);rimPower=.7;alpha=1;
                }
                else if(style==4)
                {
                    color=lerp(float3(.14,.19,.14),float3(.30,.31,.22),uv.y);rim=float3(.75,.77,.52);rimPower=.7;
                }
                else if(style==5){color=lerp(float3(.105,.108,.124),float3(.155,.16,.18),uv.y)+gloss;rimPower=.16;alpha=.92;}
                else if(style==6){color=lerp(float3(.37,.90,.55),float3(1,.78,.42),uv.x);alpha=exp(-abs(d+18)*.13)*.24;body=1;border=0;}
                color=lerp(color,rim,border*rimPower);
                float halo=exp(-max(d,0)*.30)*(1-body)*(style==1?.24:style==2?.15:.025);
                #ifndef UNITY_COLORSPACE_GAMMA
                color=GammaToLinearSpace(color);
                #endif
                fixed4 result=fixed4(color,body*alpha+halo)*i.color;
                #ifdef UNITY_UI_CLIP_RECT
                result.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
                #endif
                return result;
            }
            ENDCG
        }
    }
}

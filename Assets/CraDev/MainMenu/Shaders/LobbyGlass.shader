Shader "CraDev/UI/LobbyGlass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        // Bir kadrda bitta nusxa; panel ichidagi matnlar qayta xiralashtirilmaydi.
        GrabPass { "_LobbyGlassScene" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float4 uv:TEXCOORD0; float4 shape:TEXCOORD1; float4 state:TEXCOORD2; };
            struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float4 shape:TEXCOORD1; float4 state:TEXCOORD2; float4 grab:TEXCOORD3; float4 world:TEXCOORD4; };
            sampler2D _LobbyGlassScene;
            float4 _LobbyGlassScene_TexelSize, _Color, _ClipRect;
            v2f vert(appdata v)
            {
                v2f o;o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);
                o.color=v.color*_Color;o.uv=v.uv.xy;o.shape=v.shape;o.state=v.state;
                o.grab=ComputeGrabScreenPos(o.vertex);return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=(i.uv-.5)*i.shape.xy;
                float2 q=abs(p)-(i.shape.xy*.5-i.shape.z);
                float distance=length(max(q,0))+min(max(q.x,q.y),0)-i.shape.z;
                float aa=max(fwidth(distance),.65);
                float mask=1-smoothstep(-aa,aa,distance);
                float rim=1-smoothstep(1,2.5,abs(distance));
                float2 uv=i.grab.xy/i.grab.w;
                float2 pixel=abs(_LobbyGlassScene_TexelSize.xy);
                float2 bend=normalize(p+float2(.001,.001))*rim*pixel*1.6;
                uv+=bend;
                float2 d=pixel*6;
                float3 scene=tex2D(_LobbyGlassScene,uv).rgb*.2;
                scene+=tex2D(_LobbyGlassScene,uv+float2(d.x,0)).rgb*.12;
                scene+=tex2D(_LobbyGlassScene,uv-float2(d.x,0)).rgb*.12;
                scene+=tex2D(_LobbyGlassScene,uv+float2(0,d.y)).rgb*.12;
                scene+=tex2D(_LobbyGlassScene,uv-float2(0,d.y)).rgb*.12;
                scene+=tex2D(_LobbyGlassScene,uv+d).rgb*.08;
                scene+=tex2D(_LobbyGlassScene,uv-d).rgb*.08;
                scene+=tex2D(_LobbyGlassScene,uv+float2(d.x,-d.y)).rgb*.08;
                scene+=tex2D(_LobbyGlassScene,uv+float2(-d.x,d.y)).rgb*.08;
                float3 color=lerp(scene*.65,i.color.rgb,.68);
                color=lerp(color,i.color.rgb,i.state.z);
                color+=pow(saturate(i.uv.y),3)*.022;
                color+=rim*lerp(.045,.17,i.uv.y);
                color+=i.state.x*.027-i.state.y*.018;
                float alpha=mask*i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                alpha*=UnityGet2DClipping(i.world.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha-.001);
                #endif
                return fixed4(color,alpha);
            }
            ENDCG
        }
    }
}

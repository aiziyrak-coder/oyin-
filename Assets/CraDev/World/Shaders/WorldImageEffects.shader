Shader "Hidden/CraDev/WorldImageEffects"
{
    Properties
    {
        _MainTex ("Scene", 2D) = "white" {}
        _OcclusionTexture ("Contact shading", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
        sampler2D _CameraDepthNormalsTexture;
        sampler2D _OcclusionTexture;
        float4 _AOParameters;
        float4 _ViewParameters;
        float4 _ColorParameters;
        float4 _BlurDirection;

        struct ImageVertex
        {
            float4 vertex : SV_POSITION;
            float2 uv : TEXCOORD0;
            float2 depthUv : TEXCOORD1;
        };

        ImageVertex ImageVert(appdata_img input)
        {
            ImageVertex output;
            output.vertex = UnityObjectToClipPos(input.vertex);
            output.uv = input.texcoord;
            output.depthUv = input.texcoord;
            // MSAA ishlatilgan DX11/DX12 manbasi chuqurlikdan teskari turishi mumkin.
            #if UNITY_UV_STARTS_AT_TOP
            if (_MainTex_TexelSize.y < 0) output.depthUv.y = 1 - output.depthUv.y;
            #endif
            return output;
        }

        float3 ViewPosition(float2 uv, float depth)
        {
            return float3((uv - .5) * _ViewParameters.xy * depth, -depth);
        }

        float ReadSurface(float2 uv, out float3 normal)
        {
            float depth;
            DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv), depth, normal);
            return depth;
        }

        float OcclusionSample(float2 uv, float2 offset, float3 center, float3 normal)
        {
            float2 sampleUv = uv + offset;
            // Ekran tashqarisidagi namunani chekkaga ko'chirish soxta qora chegara yaratadi.
            if (any(sampleUv < .001) || any(sampleUv > .999)) return 0;
            float3 sampleNormal;
            float sampleDepth = ReadSurface(sampleUv, sampleNormal);
            if (sampleDepth >= .9999 || sampleDepth <= 0) return 0;
            float3 difference = ViewPosition(sampleUv, sampleDepth * _ViewParameters.z) - center;
            float distanceSquared = dot(difference, difference);
            float distance = sqrt(max(distanceSquared, .00001));
            float horizon = max(0, dot(normal, difference) - _AOParameters.z) / distance;
            float falloff = 1 - smoothstep(_AOParameters.x * .2, _AOParameters.x, distance);
            return horizon * falloff;
        }

        half4 ContactAO(ImageVertex input) : SV_Target
        {
            float3 normal;
            float depth01 = ReadSurface(input.depthUv, normal);
            // AO, chiziqli masofa, stereografik normal: blur bir xil UVdagi ma'lumot bilan ishlaydi.
            float2 packedNormal = EncodeViewNormalStereo(normal);
            if (depth01 >= .9999 || depth01 <= 0)
                return half4(1, 1, packedNormal);
            float depth = depth01 * _ViewParameters.z;
            float3 center = ViewPosition(input.depthUv, depth);
            float2 sampleRadius = _AOParameters.x / max(depth, .1) / _ViewParameters.xy;
            // Vaqtga bog'liq shovqin yo'q: kichik, doimiy sakkizta namuna miltillamaydi.
            float ao = OcclusionSample(input.depthUv, float2(.25, 0) * sampleRadius, center, normal);
            ao += OcclusionSample(input.depthUv, float2(-.295, .270) * sampleRadius, center, normal);
            ao += OcclusionSample(input.depthUv, float2(.048, -.548) * sampleRadius, center, normal);
            ao += OcclusionSample(input.depthUv, float2(.426, .555) * sampleRadius, center, normal);
            ao += OcclusionSample(input.depthUv, float2(-.837, -.148) * sampleRadius, center, normal);
            ao += OcclusionSample(input.depthUv, float2(.802, -.510) * sampleRadius, center, normal);
            ao += OcclusionSample(input.depthUv, float2(-.156, .580) * sampleRadius, center, normal);
            ao += OcclusionSample(input.depthUv, float2(-.368, -.710) * sampleRadius, center, normal);
            float distanceFade = 1 - smoothstep(25, 70, depth);
            float visibility = clamp(1 - ao * (.25 * _AOParameters.y) * distanceFade, .60, 1);
            return half4(visibility, depth01, packedNormal);
        }

        float BlurWeight(half4 center, half4 neighbor, float3 normal, float spatial)
        {
            float deltaDepth = abs(center.g - neighbor.g) * _ViewParameters.z;
            float depthWeight = exp2(-deltaDepth / max(.04, _AOParameters.x * .15));
            float3 neighborNormal = DecodeViewNormalStereo(float4(neighbor.ba, 0, 0));
            float normalWeight = pow(saturate(dot(normal, neighborNormal)), 12);
            return spatial * depthWeight * normalWeight;
        }

        half4 BlurAO(ImageVertex input) : SV_Target
        {
            half4 center = tex2D(_MainTex, input.uv);
            float3 normal = DecodeViewNormalStereo(float4(center.ba, 0, 0));
            float ao = center.r * .375;
            float totalWeight = .375;
            [unroll]
            for (int offset = -2; offset <= 2; offset++)
            {
                if (offset == 0) continue;
                half4 neighbor = tex2D(_MainTex, input.uv + _BlurDirection.xy * offset);
                float spatial = abs(offset) == 1 ? .25 : .0625;
                float weight = BlurWeight(center, neighbor, normal, spatial);
                ao += neighbor.r * weight;
                totalWeight += weight;
            }
            return half4(ao / max(totalWeight, .0001), center.gba);
        }

        float3 FilmicTone(float3 color)
        {
            // Krzysztof Narkowicz'ning CC0 ACES yaqinlashuvi (2016).
            // https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/
            return saturate((color * (2.51 * color + .03)) / (color * (2.43 * color + .59) + .14));
        }

        half4 Composite(ImageVertex input) : SV_Target
        {
            float4 scene = tex2D(_MainTex, input.uv);
            float3 color = max(scene.rgb, 0);
            #if defined(UNITY_COLORSPACE_GAMMA)
            color = GammaToLinearSpace(color);
            #endif
            float ao = lerp(1, tex2D(_OcclusionTexture, input.uv).r, _AOParameters.w);
            color = FilmicTone(color * ao * _ColorParameters.x);
            float2 center = input.uv - .5;
            float corner = smoothstep(.07, .50, dot(center, center));
            color *= 1 - corner * _ColorParameters.y;
            #if defined(UNITY_COLORSPACE_GAMMA)
            color = LinearToGammaSpace(color);
            #endif
            return half4(color, scene.a);
        }
        ENDCG

        Pass
        {
            Name "Contact AO"
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex ImageVert
            #pragma fragment ContactAO
            ENDCG
        }
        Pass
        {
            Name "Bilateral AO blur"
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex ImageVert
            #pragma fragment BlurAO
            ENDCG
        }
        Pass
        {
            Name "Filmic composite"
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex ImageVert
            #pragma fragment Composite
            ENDCG
        }
    }
    Fallback Off
}

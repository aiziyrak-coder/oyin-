// Garderob: avatar teksturasidagi kiyim (yoki soch) rangini va matosini almashtiradi (Graphics.Blit bilan).
// _Mask: R, G, B - uchta qatlam (ustki kiyim, shim, oyoq kiyim; sochda faqat R). _MaskFromAlpha = 1 bo'lsa R qatlami
// o'rniga manba teksturaning alfasi olinadi (soch va kiprik teksturasi).
// Har bir qatlam: _Color* (a = yoqilgan), _Base* (asl mato rangi), _Pattern* (0 silliq, 1 jinsi, 2 charm, 3 trikotaj,
// 4 chiziqli, 5 katak, 6 kamuflyaj). Soyalar va burmalar asl teksturadan olinadi: yorug'lik nisbati saqlanadi.
// 1-pass: rang (sRGB RT ga). 2-pass: _MetallicGlossMap (A - silliqlik: charm yaltiroq, jinsi va trikotaj xira).
Shader "Hidden/CraDev/OutfitRecolor"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Mask ("Mask", 2D) = "black" {}
        _MaskFromAlpha ("Mask from alpha", Float) = 0
        _ColorR ("Layer R color", Vector) = (0, 0, 0, 0)
        _ColorG ("Layer G color", Vector) = (0, 0, 0, 0)
        _ColorB ("Layer B color", Vector) = (0, 0, 0, 0)
        _BaseR ("Layer R base", Vector) = (0.5, 0.5, 0.5, 1)
        _BaseG ("Layer G base", Vector) = (0.5, 0.5, 0.5, 1)
        _BaseB ("Layer B base", Vector) = (0.5, 0.5, 0.5, 1)
        _PatternR ("Layer R pattern", Float) = 0
        _PatternG ("Layer G pattern", Float) = 0
        _PatternB ("Layer B pattern", Float) = 0
        _Smoothness ("Default smoothness", Float) = 0.28
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    sampler2D _Mask;
    float _MaskFromAlpha;
    float4 _ColorR, _ColorG, _ColorB;
    float4 _BaseR, _BaseG, _BaseB;
    float _PatternR, _PatternG, _PatternB;
    float _Smoothness;

    float Hash(float2 p)
    {
        p = frac(p * float2(123.34, 456.21));
        p += dot(p, p + 45.32);
        return frac(p.x * p.y);
    }

    float Noise(float2 p)
    {
        float2 i = floor(p), f = frac(p);
        float2 u = f * f * (3.0 - 2.0 * f);
        return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
    }

    float Fbm(float2 p)
    {
        float v = 0.0, a = 0.5;
        for (int k = 0; k < 4; k++) { v += a * Noise(p); p *= 2.03; a *= 0.5; }
        return v;
    }

    // Mato: tanlangan rangni naqsh bilan o'zgartiradi (chiziqli rang fazosida)
    float3 Fabric(float pattern, float3 color, float2 uv)
    {
        if (pattern < 0.5) return color;                                     // silliq
        if (pattern < 1.5)                                                   // jinsi: qiya to'qima va oq tolalar
        {
            float twill = frac((uv.x + uv.y) * 380.0);
            float fleck = Noise(uv * float2(1400.0, 90.0));
            return color * (0.88 + 0.14 * step(0.5, twill)) + fleck * 0.06;
        }
        if (pattern < 2.5)                                                   // charm: mayda g'adir-budir
            return color * (0.93 + 0.12 * Noise(uv * 900.0));
        if (pattern < 3.5)                                                   // trikotaj: tik qatorlar
            return color * (0.86 + 0.14 * (0.5 + 0.5 * cos(uv.x * 6.2832 * 240.0))) * (0.96 + 0.08 * Noise(uv * float2(240.0, 900.0)));
        if (pattern < 4.5)                                                   // chiziqli: gorizontal oq chiziqlar
        {
            float band = step(0.72, frac(uv.y * 34.0));
            return lerp(color, float3(0.82, 0.82, 0.8), band);
        }
        if (pattern < 5.5)                                                   // katak: ikki yo'nalishdagi to'q chiziqlar
        {
            float h = step(0.65, frac(uv.y * 22.0)), v = step(0.65, frac(uv.x * 22.0));
            float thin = max(step(0.94, frac(uv.y * 22.0 + 0.3)), step(0.94, frac(uv.x * 22.0 + 0.3)));
            float3 c = color * (1.0 - 0.28 * h) * (1.0 - 0.28 * v);
            return lerp(c, float3(0.75, 0.72, 0.62), thin * 0.6);
        }
        // kamuflyaj: uch tusli dog'lar
        float n1 = Fbm(uv * 14.0), n2 = Fbm(uv * 14.0 + 17.3);
        float3 dark = color * 0.42, mid = lerp(color, float3(0.16, 0.13, 0.08), 0.45);
        float3 c = color;
        c = lerp(c, mid, smoothstep(0.47, 0.5, n1));
        c = lerp(c, dark, smoothstep(0.53, 0.56, n2));
        return c;
    }

    float Luma(float3 c) { return dot(c, float3(0.2126, 0.7152, 0.0722)); }

    float3 Recolor(float3 src, float weight, float4 target, float4 baseColor, float pattern, float2 uv)
    {
        if (target.a <= 0.0 || weight <= 0.001) return src;
        // Burmalar va soyalar: asl tekselning asl mato rangiga nisbatan yorug'ligi (yumshatilgan)
        float shade = pow((Luma(src) + 0.02) / (Luma(baseColor.rgb) + 0.02), 0.85);
        shade = clamp(shade, 0.25, 2.2);
        float3 result = Fabric(pattern, target.rgb, uv) * shade;
        result = result / (1.0 + max(0.0, Luma(result) - 0.9)); // juda yorqin joylar oqarib ketmasin
        return lerp(src, result, saturate(weight) * target.a);
    }

    struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

    v2f vert(appdata_img v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord;
        return o;
    }

    float3 Weights(float2 uv, float alpha)
    {
        float3 m = tex2D(_Mask, uv).rgb;
        if (_MaskFromAlpha > 0.5) m = float3(saturate(alpha * 2.0 - 0.1), 0, 0);
        return m;
    }

    float4 fragColor(v2f i) : SV_Target
    {
        float4 src = tex2D(_MainTex, i.uv);
        float3 m = Weights(i.uv, src.a);
        float3 c = src.rgb;
        c = Recolor(c, m.r, _ColorR, _BaseR, _PatternR, i.uv);
        c = Recolor(c, m.g, _ColorG, _BaseG, _PatternG, i.uv);
        c = Recolor(c, m.b, _ColorB, _BaseB, _PatternB, i.uv);
        return float4(c, src.a);
    }

    float Smooth(float pattern)
    {
        if (pattern < 0.5) return _Smoothness;
        if (pattern < 1.5) return 0.12;   // jinsi
        if (pattern < 2.5) return 0.62;   // charm
        if (pattern < 3.5) return 0.08;   // trikotaj
        return _Smoothness * 0.8;
    }

    float4 fragGloss(v2f i) : SV_Target
    {
        float4 src = tex2D(_MainTex, i.uv);
        float3 m = Weights(i.uv, src.a);
        float s = _Smoothness;
        s = lerp(s, Smooth(_PatternR), _ColorR.a * saturate(m.r));
        s = lerp(s, Smooth(_PatternG), _ColorG.a * saturate(m.g));
        s = lerp(s, Smooth(_PatternB), _ColorB.a * saturate(m.b));
        return float4(0, 0, 0, s);
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragColor
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragGloss
            ENDCG
        }
    }
}

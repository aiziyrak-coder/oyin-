using CraDev.CharacterCreation;
using UnityEngine;

namespace CraDev.Wardrobe
{
    /// <summary>
    /// Kiyimni avatar teksturasiga "bo'yaydi" (GPU, OutfitRecolor shaderi): tana teksturasida ustki kiyim, shim va oyoq
    /// kiyim, bosh teksturasida va soch/kiprik teksturasida soch. Natija - RenderTexture'lar, material'ga qo'yiladi.
    /// </summary>
    public static class OutfitPainter
    {
        static readonly int MaskId = Shader.PropertyToID("_Mask");
        static readonly int MaskFromAlphaId = Shader.PropertyToID("_MaskFromAlpha");
        static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        static readonly int[] ColorIds = { Shader.PropertyToID("_ColorR"), Shader.PropertyToID("_ColorG"), Shader.PropertyToID("_ColorB") };
        static readonly int[] BaseIds = { Shader.PropertyToID("_BaseR"), Shader.PropertyToID("_BaseG"), Shader.PropertyToID("_BaseB") };
        static readonly int[] PatternIds = { Shader.PropertyToID("_PatternR"), Shader.PropertyToID("_PatternG"), Shader.PropertyToID("_PatternB") };

        /// <summary>Material'dagi standart silliqlik (CraDevModelImporter bilan bir xil).</summary>
        public const float DefaultSmoothness = 0.28f;

        /// <summary>
        /// Tana: ustki kiyim, shim, oyoq kiyim. Hech narsa o'zgartirilmagan bo'lsa false (asl tekstura qoladi).
        /// color va gloss qayta ishlatiladi (o'lchami mos bo'lsa), yo'q bo'lsa yaratiladi.
        /// </summary>
        public static bool PaintBody(Texture source, AvatarOption option, Outfit outfit, Material material, ref RenderTexture color, ref RenderTexture gloss)
        {
            if (source == null || material == null || option == null || !option.SupportsOutfit || outfit == null)
                return false;
            bool any = false;
            for (int k = 0; k < 3; k++)
                any |= SetLayer(material, k, (OutfitSlot)k, outfit, option.outfitBase[k]);
            if (!any)
                return false;
            material.SetTexture(MaskId, option.outfitMask);
            material.SetFloat(MaskFromAlphaId, 0f);
            material.SetFloat(SmoothnessId, DefaultSmoothness);
            color = Ensure(color, source.width, source.height, true, "OutfitBody");
            var previous = RenderTexture.active;
            Graphics.Blit(source, color, material, 0);
            color.GenerateMips();
            gloss = Ensure(gloss, source.width / 2, source.height / 2, false, "OutfitGloss");
            Graphics.Blit(source, gloss, material, 1);
            gloss.GenerateMips();
            RenderTexture.active = previous;
            return true;
        }

        /// <summary>Soch: bosh teksturasida (niqob bo'yicha) yoki soch/kiprik teksturasida (alfa bo'yicha).</summary>
        public static bool PaintHair(Texture source, AvatarOption option, Outfit outfit, Material material, bool fromAlpha, ref RenderTexture color)
        {
            if (source == null || material == null || option == null || !option.SupportsHair || outfit == null)
                return false;
            if (!SetLayer(material, 0, OutfitSlot.Hair, outfit, option.outfitBase[3]))
                return false;
            for (int k = 1; k < 3; k++)
                material.SetVector(ColorIds[k], Vector4.zero);
            material.SetTexture(MaskId, fromAlpha ? Texture2D.blackTexture : option.hairMask);
            material.SetFloat(MaskFromAlphaId, fromAlpha ? 1f : 0f);
            color = Ensure(color, source.width, source.height, true, fromAlpha ? "OutfitHairCards" : "OutfitHead");
            var previous = RenderTexture.active;
            Graphics.Blit(source, color, material, 0);
            color.GenerateMips();
            RenderTexture.active = previous;
            return true;
        }

        static bool SetLayer(Material material, int layer, OutfitSlot slot, Outfit outfit, Color baseColor)
        {
            var item = WardrobeCatalog.Find(outfit.Item(slot));
            if (item == null || !outfit.TryGetColor(slot, out var target))
            {
                material.SetVector(ColorIds[layer], Vector4.zero);
                return false;
            }
            // SetVector: chiziqli rang fazosida SetColor qiymatni yana o'zgartirib yuboradi
            var linear = target.linear;
            var linearBase = baseColor.linear;
            material.SetVector(ColorIds[layer], new Vector4(linear.r, linear.g, linear.b, 1f));
            material.SetVector(BaseIds[layer], new Vector4(linearBase.r, linearBase.g, linearBase.b, 1f));
            material.SetFloat(PatternIds[layer], (float)item.Pattern);
            return true;
        }

        static RenderTexture Ensure(RenderTexture texture, int width, int height, bool srgb, string name)
        {
            if (texture != null && texture.width == width && texture.height == height)
                return texture;
            if (texture != null)
                texture.Release();
            var result = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, srgb ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear)
            {
                useMipMap = true,
                autoGenerateMips = false,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 4,
                name = name,
            };
            result.Create();
            return result;
        }
    }
}

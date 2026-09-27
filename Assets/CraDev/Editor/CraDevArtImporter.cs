using UnityEditor;
using UnityEngine;

namespace CraDev.EditorTools
{
    /// <summary>
    /// Assets/CraDev/.../Art papkalaridagi rasmlarni birinchi importda UI uchun
    /// sifatli sprite sifatida sozlaydi (siqishsiz, mipmap'siz), shunda logolar
    /// katta ekranlarda ham tiniq va yo'l-yo'l bo'lmasdan ko'rinadi.
    /// Keyin Inspector'da qo'lda o'zgartirilgan sozlamalarga tegmaydi.
    /// </summary>
    class CraDevArtImporter : AssetPostprocessor
    {
        public static bool IsCraDevArt(string path) => path.StartsWith("Assets/CraDev/") && path.Contains("/Art/");

        void OnPreprocessTexture()
        {
            if (!IsCraDevArt(assetPath) || !assetImporter.importSettingsMissing)
                return;
            Apply((TextureImporter)assetImporter);
        }

        public static void Apply(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
        }
    }
}

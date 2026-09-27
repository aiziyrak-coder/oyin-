using UnityEditor;
using UnityEngine;

namespace CraDev.Intro.EditorTools
{
    /// <summary>
    /// Assets/CraDev/Intro/Art papkasidagi rasmlarni birinchi importda UI uchun
    /// sifatli sprite sifatida sozlaydi (siqishsiz, mipmap'siz), shunda logo
    /// katta ekranlarda ham tiniq va yo'l-yo'l bo'lmasdan ko'rinadi.
    /// Keyin Inspector'da qo'lda o'zgartirilgan sozlamalarga tegmaydi.
    /// </summary>
    class IntroArtImporter : AssetPostprocessor
    {
        public const string ArtFolder = "Assets/CraDev/Intro/Art/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder) || !assetImporter.importSettingsMissing)
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

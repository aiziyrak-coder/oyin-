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

        public static bool IsAvatarPhoto(string path) => path.StartsWith("Assets/CraDev/Avatars/Photos/");

        void OnPreprocessTexture()
        {
            if (IsAvatarPhoto(assetPath) && assetImporter.importSettingsMissing)
            {
                ApplyPhoto((TextureImporter)assetImporter);
                return;
            }
            if (!IsCraDevArt(assetPath) || !assetImporter.importSettingsMissing)
                return;
            var importer = (TextureImporter)assetImporter;
            Apply(importer);
            // 9-slice spritelar: burchaklari cho'zilmaydi
            var border = SliceBorder(assetPath);
            if (border != Vector4.zero)
                importer.spriteBorder = border;
        }

        /// <summary>UI_Round12_* (24 px radius) va UI_Pill_* (32 px radius) spritelarining 9-slice chegarasi.</summary>
        public static Vector4 SliceBorder(string path)
        {
            string file = System.IO.Path.GetFileName(path);
            if (file.StartsWith("UI_Round12")) return new Vector4(24, 24, 24, 24);
            if (file.StartsWith("UI_Pill")) return new Vector4(32, 32, 32, 32);
            return Vector4.zero;
        }

        /// <summary>
        /// Avatar fotosuratlari: sprite, mipmap bilan (kichik kartalarda ham tiniq) va sifatli siqilgan
        /// (xotirani tejash uchun).
        /// </summary>
        public static void ApplyPhoto(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = true;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.maxTextureSize = 1024;
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

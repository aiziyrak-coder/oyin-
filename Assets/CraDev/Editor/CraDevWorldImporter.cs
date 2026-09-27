using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace CraDev.EditorTools
{
    /// <summary>
    /// Virtual dunyo manbalari (Assets/CraDev/World, Poly Haven CC0) uchun import sozlamalari:
    /// osmon HDRI kubik xarita bo'ladi, normal xaritalar belgilanadi, daraxt va buta materiallari
    /// o'z teksturalari bilan quriladi (barglar - CraDev/Foliage shaderi).
    /// </summary>
    class CraDevWorldImporter : AssetPostprocessor
    {
        const string World = "Assets/CraDev/World/";

        public override uint GetVersion() => 2;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(World))
                return;
            var importer = (TextureImporter)assetImporter;
            string file = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();

            if (assetPath.Contains("/Sky/"))
            {
                // Osmon: kubik xarita, sifatli (HDR), tekis filtr
                importer.textureShape = TextureImporterShape.TextureCube;
                importer.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.filterMode = FilterMode.Trilinear;
                return;
            }

            importer.mipmapEnabled = true;
            importer.anisoLevel = 8;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            if (file.Contains("_nor"))
                importer.textureType = TextureImporterType.NormalMap;
            else if (file.EndsWith("_color") || file.Contains("branches_diff"))
            {
                importer.alphaIsTransparency = true;
                // Uzoqda barglar yo'qolib qolmasligi uchun mipmaplarda alfa saqlanadi
                importer.mipMapsPreserveCoverage = true;
                importer.alphaTestReferenceValue = 0.45f;
            }
            if (file.StartsWith("plaza_"))
                importer.wrapMode = TextureWrapMode.Repeat;
        }

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(World))
                return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }

        void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            if (!assetPath.StartsWith(World))
                return;
            string name = description.materialName;
            string folder = Path.GetDirectoryName(assetPath).Replace('\\', '/') + "/textures/";

            bool foliage = name.EndsWith("_leaves") || name.EndsWith("_branches") || name.StartsWith("shrub");
            // Shader fayldan olinadi: model shaderdan oldin import qilinsa ham topiladi
            const string foliagePath = World + "Shaders/Foliage.shader";
            if (foliage)
                context.DependsOnSourceAsset(foliagePath);
            material.shader = foliage ? AssetDatabase.LoadAssetAtPath<Shader>(foliagePath) ?? Shader.Find("CraDev/Foliage") : Shader.Find("Standard");
            material.color = Color.white;

            // Rang: birlashtirilgan RGBA (_color) yoki asl (_diff_1k)
            var color = Load(folder, name + "_color") ?? Load(folder, name + "_diff_1k");
            var normal = Load(folder, name + "_nor_gl_1k");
            material.SetTexture("_MainTex", color);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            if (!foliage)
            {
                material.SetFloat("_Glossiness", 0.2f);
                material.SetFloat("_Metallic", 0f);
            }
        }

        Texture2D Load(string folder, string stem)
        {
            foreach (var ext in new[] { ".png", ".jpg", ".exr" })
            {
                string path = folder + stem + ext;
                if (!File.Exists(path))
                    continue;
                context.DependsOnArtifact(path);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            return null;
        }
    }
}

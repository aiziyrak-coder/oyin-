using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace CraDev.EditorTools
{
    /// <summary>
    /// Microsoft Rocketbox avatarlari (MIT, github.com/microsoft/Microsoft-Rocketbox) uchun import sozlamalari.
    ///
    /// Modellar: Assets/CraDev/Avatars/Models/&lt;ID&gt;/&lt;Nomi&gt;.fbx, teksturalar yonidagi Textures/ papkasida
    /// (asl .tga lar hajmni kamaytirish uchun .jpg/.png ga aylantirilgan). Animatsiyalar: Avatars/Animations/.
    /// Hammasi Generic skelet (Bip01): bitta idle animatsiya barcha avatarlarga mos keladi.
    /// </summary>
    class CraDevModelImporter : AssetPostprocessor
    {
        const string ModelsFolder = "Assets/CraDev/Avatars/Models/";
        const string AnimationsFolder = "Assets/CraDev/Avatars/Animations/";

        // Import qoidalari o'zgarganda oshiriladi: Unity modellarni qayta import qiladi
        public override uint GetVersion() => 2;

        static bool IsAvatarModel(string path) => path.StartsWith(ModelsFolder) && path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);
        static bool IsAvatarAnimation(string path) => path.StartsWith(AnimationsFolder) && path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ModelsFolder))
                return;
            var importer = (TextureImporter)assetImporter;
            string file = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            importer.textureType = file.Contains("normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.alphaIsTransparency = file.Contains("opacity");
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.anisoLevel = 4;
        }

        void OnPreprocessModel()
        {
            if (!IsAvatarModel(assetPath) && !IsAvatarAnimation(assetPath))
                return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;

            if (IsAvatarModel(assetPath))
            {
                importer.importAnimation = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            }
            else
            {
                importer.importAnimation = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
            }
        }

        void OnPreprocessAnimation()
        {
            if (!IsAvatarAnimation(assetPath))
                return;
            // Idle animatsiyalar to'xtovsiz takrorlanadi
            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = true;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
            }
            importer.clipAnimations = clips;
        }

        /// <summary>Rocketbox'da bir nechta detal darajasi bor: faqat eng sifatlisi (hipoly) qoladi.</summary>
        void OnPostprocessMeshHierarchy(GameObject root)
        {
            if (!IsAvatarModel(assetPath))
                return;
            string name = root.name.ToLowerInvariant();
            if (name.Contains("poly") && !name.Contains("hipoly"))
                root.SetActive(false);
        }

        void OnPostprocessModel(GameObject root)
        {
            if (IsAvatarModel(assetPath) || IsAvatarAnimation(assetPath))
                RenameBip(root.transform);
        }

        // Ba'zi Rocketbox fayllarida skelet "Bip02" deb nomlangan: animatsiya bilan mos bo'lishi uchun "Bip01"
        static void RenameBip(Transform t)
        {
            if (t.name.Contains("Bip02"))
                t.name = t.name.Replace("Bip02", "Bip01");
            foreach (Transform child in t)
                RenameBip(child);
        }

        /// <summary>
        /// 3ds Max materiallarini Standard shaderga o'giradi: rang, normal va (soch/kipriklar uchun) shaffoflik.
        /// FBX .tga fayllarga ishora qiladi, bizda esa ular .jpg/.png: tekstura nomi bo'yicha topiladi.
        /// </summary>
        void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            if (!IsAvatarModel(assetPath))
                return;

            material.shader = Shader.Find("Standard");
            material.color = Color.white;
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", 0.28f);

            // Rocketbox materiallari teksturalari bilan bir xil nomlangan: "m024_body" -> m024_body_color, m024_body_normal
            string stem = description.materialName;
            string folder = Path.GetDirectoryName(assetPath).Replace('\\', '/') + "/Textures/";
            var color = LoadTexture(folder, stem + "_color");
            if (color == null)
            {
                Debug.LogWarning($"[CraDev] {assetPath}: \"{stem}\" materialining teksturasi topilmadi.");
                return;
            }
            var normal = LoadTexture(folder, stem + "_normal");
            material.SetTexture("_MainTex", color);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", 1f);
                material.EnableKeyword("_NORMALMAP");
            }

            if (stem.ToLowerInvariant().Contains("opacity"))
            {
                // Soch, kiprik va qoshlar: alfa bo'yicha kesiladi (Cutout)
                material.SetFloat("_Mode", 1f);
                material.SetFloat("_Cutoff", 0.4f);
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.EnableKeyword("_ALPHATEST_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                material.SetFloat("_Glossiness", 0.15f);
            }
        }

        Texture2D LoadTexture(string folder, string stem)
        {
            foreach (var ext in new[] { ".jpg", ".png", ".tga" })
            {
                string path = folder + stem + ext;
                if (!File.Exists(path))
                    continue;
                // Tekstura o'zgarsa model ham qayta import qilinadi
                context.DependsOnArtifact(path);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            return null;
        }
    }
}

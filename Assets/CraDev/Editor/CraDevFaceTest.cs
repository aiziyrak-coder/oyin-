using System.IO;
using CraDev.Face;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEngine;

namespace CraDev.EditorTools
{
    /// <summary>
    /// Yuz qo'yishni o'yinsiz tekshirish (batchmode):
    /// <c>Unity.exe -batchmode -quit -projectPath . -executeMethod CraDev.EditorTools.CraDevFaceTest.Run -facePhoto rasm.jpg -avatarModel Assets/.../Male_Adult_10.fbx</c>
    /// Natija: Logs/FaceTest/ ichida rasmdagi nuqtalar va yuz chizilgan bosh teksturasi.
    /// </summary>
    public static class CraDevFaceTest
    {
        public static void Run()
        {
            try
            {
                string photoPath = Argument("-facePhoto");
                string modelPath = Argument("-avatarModel") ?? "Assets/CraDev/Avatars/Models/M1/Male_Adult_10.fbx";
                string folder = Path.Combine("Logs", "FaceTest");
                Directory.CreateDirectory(folder);

                // Avval sahna: u ochilganda ishlatilmayotgan rasmlar tozalanadi
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/CraDev/Scenes/CharacterCreation.unity");
                var photo = FacePhoto.Load(photoPath);
                using var tracker = new FaceTracker(
                    AssetDatabase.LoadAssetAtPath<ModelAsset>("Assets/CraDev/Face/Models/face_detector.onnx"),
                    AssetDatabase.LoadAssetAtPath<ModelAsset>("Assets/CraDev/Face/Models/face_landmarks_detector.onnx"));
                if (!tracker.Track(photo.GetPixels32(), photo.width, photo.height, out var landmarks, out var error))
                    throw new System.Exception("Yuz topilmadi: " + error);

                var screen = Object.FindFirstObjectByType<CraDev.CharacterCreation.CharacterCreationScreen>();
                var so = new SerializedObject(screen);
                var avatars = so.FindProperty("avatars");
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                for (int i = 0; i < avatars.arraySize; i++)
                {
                    var item = avatars.GetArrayElementAtIndex(i);
                    if (item.FindPropertyRelative("model").objectReferenceValue != model)
                        continue;
                    var uvProp = item.FindPropertyRelative("faceUv");
                    var triProp = item.FindPropertyRelative("faceTriangles");
                    var uv = new Vector2[uvProp.arraySize];
                    for (int k = 0; k < uv.Length; k++) uv[k] = uvProp.GetArrayElementAtIndex(k).vector2Value;
                    var tris = new int[triProp.arraySize];
                    for (int k = 0; k < tris.Length; k++) tris[k] = triProp.GetArrayElementAtIndex(k).intValue;

                    Texture head = null;
                    foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                        foreach (var m in r.sharedMaterials)
                            if (m != null && m.name.Contains("_head")) head = m.mainTexture;

                    var face = new FaceData { Photo = photo, Landmarks = landmarks };
                    var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/CraDev/Face/FacePaint.mat");
                    var painted = FacePainter.Paint(head, face, uv, tris, material);
                    Save(painted, Path.Combine(folder, "painted.png"));
                    painted.Release();
                    Debug.Log("[CraDev] FACE TEST OK: " + folder);
                    EditorApplication.Exit(0);
                    return;
                }
                throw new System.Exception("Sahnada bu model yo'q: " + modelPath);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[CraDev] FACE TEST XATO: " + e);
                EditorApplication.Exit(1);
            }
        }

        static void Save(RenderTexture rt, string path)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var t = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            t.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
        }

        static string Argument(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}

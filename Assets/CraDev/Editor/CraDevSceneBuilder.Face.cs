using System.Collections.Generic;
using System.IO;
using System.Linq;
using CraDev.Face;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static CraDev.EditorTools.UiBuild;

namespace CraDev.EditorTools
{
    /// <summary>
    /// Avatar yaratish ekranining yuz qismi: har bir avatar uchun "yuz xaritasi" (yuz nuqtalarining bosh
    /// teksturasidagi o'rni), kamera oynasi uchun rasmlar va ikonkalar.
    /// </summary>
    static partial class CraDevSceneBuilder
    {
        const string FaceModels = "Assets/CraDev/Face/Models/";
        const string FacePaintPath = "Assets/CraDev/Face/FacePaint.mat";

        class FaceMap
        {
            public Vector2[] Uv;
            public int[] Triangles;
        }

        /// <summary>
        /// Har bir avatarning yuzi oldidan chiziladi, rasmdagi yuz nuqtalari xuddi o'yinchi rasmidagidek topiladi
        /// (FaceTracker), so'ng har bir nuqtadan boshga nur tashlanib, uning bosh teksturasidagi o'rni (UV) olinadi.
        /// O'yinda o'yinchi rasmining nuqtalari shu o'rinlarga cho'ziladi. Tekshirish uchun Logs/FaceMaps/ ga rasmlar yoziladi.
        /// </summary>
        static Dictionary<string, FaceMap> BakeFaceMaps(RuntimeAnimatorController maleIdle, RuntimeAnimatorController femaleIdle)
        {
            // Grafikasiz muhitda (CI: -nographics) chizib bo'lmaydi: oldin saqlangan xaritalar ishlatiladi
            if (!CanRender)
            {
                var saved = LoadFaceMaps();
                Debug.Log($"[CraDev] Grafika yo'q: {saved.Count} ta yuz xaritasi {FaceMapsFile} dan olindi.");
                return saved;
            }
            var result = BakeFaceMapsNow(maleIdle, femaleIdle);
            SaveFaceMaps(result);
            return result;
        }

        static bool CanRender => SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;

        const string FaceMapsFile = "Assets/CraDev/Avatars/FaceMaps.json";

        [System.Serializable]
        class FaceMapRecord
        {
            public string id;
            public float[] uv;
            public int[] triangles;
        }

        [System.Serializable]
        class FaceMapFile
        {
            public List<FaceMapRecord> avatars = new List<FaceMapRecord>();
        }

        static void SaveFaceMaps(Dictionary<string, FaceMap> maps)
        {
            var file = new FaceMapFile();
            foreach (var pair in maps.OrderBy(p => p.Key))
                file.avatars.Add(new FaceMapRecord
                {
                    id = pair.Key,
                    uv = pair.Value.Uv.SelectMany(v => new[] { (float)System.Math.Round(v.x, 5), (float)System.Math.Round(v.y, 5) }).ToArray(),
                    triangles = pair.Value.Triangles,
                });
            File.WriteAllText(FaceMapsFile, JsonUtility.ToJson(file));
            AssetDatabase.ImportAsset(FaceMapsFile);
        }

        static Dictionary<string, FaceMap> LoadFaceMaps()
        {
            var result = new Dictionary<string, FaceMap>();
            if (!File.Exists(FaceMapsFile))
                return result;
            var file = JsonUtility.FromJson<FaceMapFile>(File.ReadAllText(FaceMapsFile));
            foreach (var record in file.avatars)
            {
                var uv = new Vector2[record.uv.Length / 2];
                for (int i = 0; i < uv.Length; i++)
                    uv[i] = new Vector2(record.uv[i * 2], record.uv[i * 2 + 1]);
                result[record.id] = new FaceMap { Uv = uv, Triangles = record.triangles };
            }
            return result;
        }

        static Dictionary<string, FaceMap> BakeFaceMapsNow(RuntimeAnimatorController maleIdle, RuntimeAnimatorController femaleIdle)
        {
            var result = new Dictionary<string, FaceMap>();
            var detector = AssetDatabase.LoadAssetAtPath<ModelAsset>(FaceModels + "face_detector.onnx");
            var landmarks = AssetDatabase.LoadAssetAtPath<ModelAsset>(FaceModels + "face_landmarks_detector.onnx");
            if (detector == null || landmarks == null)
            {
                Debug.LogWarning("[CraDev] Yuz modellari topilmadi (" + FaceModels + "): yuz qo'yish o'chiq bo'ladi.");
                return result;
            }

            const int size = 512;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            NewLight("Key", null, LightType.Directional, Color.white, 0.8f, Quaternion.Euler(15f, 10f, 0f));
            var camera = new GameObject("FaceCamera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.35f, 0.36f, 0.38f);
            camera.fieldOfView = 22f;
            camera.nearClipPlane = 0.05f;
            camera.aspect = 1f;
            var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            camera.targetTexture = rt;
            string debugFolder = Path.Combine("Logs", "FaceMaps");
            Directory.CreateDirectory(debugFolder);

            using (var tracker = new FaceTracker(detector, landmarks))
            {
                foreach (var info in AvatarList)
                {
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(info.ModelPath);
                    if (model == null)
                        continue;
                    var go = (GameObject)Object.Instantiate(model);
                    go.transform.rotation = Quaternion.Euler(0f, ModelFacing, 0f);
                    ControllerClip(info.Gender == "female" ? femaleIdle : maleIdle)?.SampleAnimation(go, 0.5f);
                    var head = go.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Bip01 Head");
                    if (head == null)
                    {
                        Object.DestroyImmediate(go);
                        continue;
                    }

                    // Yuz markazi bosh suyagidan biroz yuqorida; kamera to'g'ri oldidan qaraydi
                    var target = head.position + new Vector3(0f, 0.07f, 0f);
                    camera.transform.SetPositionAndRotation(target + Vector3.back * 0.62f, Quaternion.identity);
                    camera.Render();
                    var shot = ReadRenderTexture(rt);

                    if (!tracker.Track(shot.GetPixels32(), size, size, out var points, out var error))
                    {
                        Debug.LogWarning($"[CraDev] {info.Id}: avatar yuzida nuqtalar topilmadi ({error}). Bu avatarga yuz qo'yilmaydi.");
                        Object.DestroyImmediate(shot);
                        Object.DestroyImmediate(go);
                        continue;
                    }

                    var map = new FaceMap { Uv = RaycastHead(go, camera, points, out var headTexture) };
                    map.Triangles = FacePainter.Triangulate(map.Uv);
                    int found = map.Uv.Count(uv => uv.x >= 0f);
                    Debug.Log($"[CraDev] {info.Id}: yuz xaritasi tayyor ({found} nuqta, {map.Triangles.Length / 3} uchburchak).");
                    if (found > 300)
                        result[info.Id] = map;
                    else
                        Debug.LogWarning($"[CraDev] {info.Id}: yuz xaritasida nuqtalar juda kam, bu avatarga yuz qo'yilmaydi.");

                    File.WriteAllBytes(Path.Combine(debugFolder, info.Id + "_face.png"), shot.EncodeToPNG()); // sinov uchun toza yuz rasmi
                    WriteDebug(Path.Combine(debugFolder, info.Id + "_render.png"), shot, points, null);
                    if (headTexture != null)
                    {
                        var texture = ReadTexture(headTexture, 1024);
                        WriteDebug(Path.Combine(debugFolder, info.Id + "_uv.png"), texture, map.Uv, map.Triangles);
                        Object.DestroyImmediate(texture);
                    }
                    Object.DestroyImmediate(shot);
                    Object.DestroyImmediate(go);
                }
            }

            camera.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            return result;
        }

        /// <summary>Har bir yuz nuqtasi orqali boshga nur: tegsa - teksturadagi o'rni, tegmasa (-1, -1).</summary>
        static Vector2[] RaycastHead(GameObject avatar, Camera camera, Vector2[] points, out Texture headTexture)
        {
            var uv = Enumerable.Repeat(new Vector2(-1f, -1f), FaceTracker.LandmarkCount).ToArray();
            headTexture = null;
            var colliders = new List<MeshCollider>();
            foreach (var skin in avatar.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var materials = skin.sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                {
                    if (materials[m] == null || !materials[m].name.Contains("_head"))
                        continue;
                    headTexture = materials[m].mainTexture;
                    // Faqat bosh qismi: soch va kipriklar nurni to'smaydi
                    var baked = new Mesh();
                    skin.BakeMesh(baked, true);
                    var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                    mesh.vertices = baked.vertices;
                    mesh.uv = baked.uv;
                    mesh.triangles = baked.GetTriangles(m);
                    var go = new GameObject("HeadCollider");
                    go.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
                    var collider = go.AddComponent<MeshCollider>();
                    collider.sharedMesh = mesh;
                    colliders.Add(collider);
                }
            }
            Physics.SyncTransforms();

            var distances = new float[FaceTracker.MeshLandmarkCount];
            for (int i = 0; i < FaceTracker.MeshLandmarkCount; i++)
            {
                distances[i] = -1f;
                var ray = camera.ViewportPointToRay(new Vector3(points[i].x, points[i].y, 0f));
                float best = float.MaxValue;
                foreach (var collider in colliders)
                    if (collider.Raycast(ray, out var hit, 5f) && hit.distance < best)
                    {
                        best = hit.distance;
                        uv[i] = hit.textureCoord;
                        distances[i] = hit.distance;
                    }
            }

            // Ko'z yoki og'iz teshigidan o'tib, boshning ichki tomoniga tekkan nurlar tashlab yuboriladi
            var valid = distances.Where(d => d > 0f).OrderBy(d => d).ToArray();
            if (valid.Length > 0)
            {
                float median = valid[valid.Length / 2];
                for (int i = 0; i < distances.Length; i++)
                    if (distances[i] > median + 0.07f)
                        uv[i] = new Vector2(-1f, -1f);
            }

            foreach (var collider in colliders)
            {
                Object.DestroyImmediate(collider.sharedMesh);
                Object.DestroyImmediate(collider.gameObject);
            }
            return uv;
        }

        static Texture2D ReadRenderTexture(RenderTexture rt)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            return texture;
        }

        static Texture2D ReadTexture(Texture source, int size)
        {
            var rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(source, rt);
            var texture = ReadRenderTexture(rt);
            RenderTexture.ReleaseTemporary(rt);
            return texture;
        }

        /// <summary>Tekshirish uchun: rasm ustiga nuqtalar (va uchburchaklar) chiziladi.</summary>
        static void WriteDebug(string path, Texture2D image, Vector2[] points, int[] triangles)
        {
            var copy = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
            copy.SetPixels32(image.GetPixels32());
            void Dot(Vector2 p, Color c)
            {
                if (p.x < 0f) return;
                int x = Mathf.RoundToInt(p.x * copy.width), y = Mathf.RoundToInt(p.y * copy.height);
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        copy.SetPixel(x + dx, y + dy, c);
            }
            if (triangles != null)
                for (int t = 0; t < triangles.Length; t += 3)
                    for (int e = 0; e < 3; e++)
                    {
                        Vector2 a = points[triangles[t + e]], b = points[triangles[t + (e + 1) % 3]];
                        for (float s = 0f; s <= 1f; s += 0.05f)
                        {
                            var p = Vector2.Lerp(a, b, s);
                            copy.SetPixel(Mathf.RoundToInt(p.x * copy.width), Mathf.RoundToInt(p.y * copy.height), new Color(0f, 0.7f, 1f));
                        }
                    }
            foreach (var p in points.Take(FaceTracker.MeshLandmarkCount))
                Dot(p, Color.green);
            copy.Apply();
            File.WriteAllBytes(path, copy.EncodeToPNG());
            Object.DestroyImmediate(copy);
        }

        // ---------------------------------------------------------------- Material, rasmlar, ikonkalar

        static Material FacePaintMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(FacePaintPath);
            if (material != null)
                return material;
            material = new Material(Shader.Find("Hidden/CraDev/FaceProject"));
            AssetDatabase.CreateAsset(material, FacePaintPath);
            return material;
        }

        /// <summary>Kamera oynasidagi oval: yuz shu chiziq ichiga joylanadi.</summary>
        static Sprite OvalSprite()
        {
            string path = UiArt + "UI_FaceOval.png";
            if (!File.Exists(path))
            {
                const int w = 560, h = 720;
                const float stroke = 3f;
                var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
                float a = w / 2f - 6f, b = h / 2f - 6f;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float px = x + 0.5f - w / 2f, py = y + 0.5f - h / 2f;
                        float k = Mathf.Sqrt(px * px / (a * a) + py * py / (b * b));
                        // Ellipsgacha taxminiy masofa (piksel)
                        float d = Mathf.Abs(k - 1f) * Mathf.Min(a, b);
                        float alpha = Mathf.Clamp01(stroke - d);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }
            return LoadSprite(path);
        }

        /// <summary>
        /// Oddiy chiziqli ikonka (24x24 birlik, 2x o'lchamda): Camera yoki Upload. Boshqa ikonkalar kabi oq,
        /// rangi Image.color bilan beriladi.
        /// </summary>
        static Sprite DrawnIcon(string name)
        {
            string path = UiArt + "Icon_" + name + ".png";
            if (!File.Exists(path))
            {
                const int px = 48;
                const float half = 1f; // chiziq qalinligining yarmi (birlikda)
                var segments = new List<(Vector2 a, Vector2 b)>();
                var circles = new List<(Vector2 c, float r)>();
                if (name == "Camera")
                {
                    // Tana: yumaloq burchakli to'rtburchak, tepada kichik do'nglik, o'rtada obyektiv
                    AddRoundRect(segments, new Rect(3f, 5f, 18f, 13f));
                    segments.Add((new Vector2(8.5f, 18f), new Vector2(9.5f, 20.5f)));
                    segments.Add((new Vector2(9.5f, 20.5f), new Vector2(14.5f, 20.5f)));
                    segments.Add((new Vector2(14.5f, 20.5f), new Vector2(15.5f, 18f)));
                    circles.Add((new Vector2(12f, 11.5f), 3.6f));
                }
                else
                {
                    // Yuqoriga o'q va pastda patnis
                    segments.Add((new Vector2(12f, 7f), new Vector2(12f, 20f)));
                    segments.Add((new Vector2(12f, 20f), new Vector2(7.5f, 15.5f)));
                    segments.Add((new Vector2(12f, 20f), new Vector2(16.5f, 15.5f)));
                    segments.Add((new Vector2(4f, 9f), new Vector2(4f, 4f)));
                    segments.Add((new Vector2(4f, 4f), new Vector2(20f, 4f)));
                    segments.Add((new Vector2(20f, 4f), new Vector2(20f, 9f)));
                }
                var texture = new Texture2D(px, px, TextureFormat.RGBA32, false);
                for (int y = 0; y < px; y++)
                    for (int x = 0; x < px; x++)
                    {
                        var p = new Vector2((x + 0.5f) / px * 24f, (y + 0.5f) / px * 24f);
                        float d = float.MaxValue;
                        foreach (var s in segments)
                            d = Mathf.Min(d, SegmentDistance(p, s.a, s.b));
                        foreach (var c in circles)
                            d = Mathf.Min(d, Mathf.Abs(Vector2.Distance(p, c.c) - c.r));
                        // 1 birlik = 2 piksel: chetlari yarim piksel silliqlanadi
                        float alpha = Mathf.Clamp01((half - d) * 2f + 0.5f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }
            return LoadSprite(path);
        }

        static void AddRoundRect(List<(Vector2, Vector2)> segments, Rect r)
        {
            const float radius = 2f;
            const int arc = 6;
            var corners = new[]
            {
                (new Vector2(r.xMax - radius, r.yMax - radius), 0f),
                (new Vector2(r.xMin + radius, r.yMax - radius), 90f),
                (new Vector2(r.xMin + radius, r.yMin + radius), 180f),
                (new Vector2(r.xMax - radius, r.yMin + radius), 270f),
            };
            var outline = new List<Vector2>();
            foreach (var (center, start) in corners)
                for (int i = 0; i <= arc; i++)
                {
                    float a = (start + 90f * i / arc) * Mathf.Deg2Rad;
                    outline.Add(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                }
            for (int i = 0; i < outline.Count; i++)
                segments.Add((outline[i], outline[(i + 1) % outline.Count]));
        }

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>Ikkinchi darajali tugma: to'q fon, ingichka ramka, ikonka va matn markazda.</summary>
        static Button CreateSecondaryButton(string name, Transform parent, string text, Sprite icon, float x, float y, float width,
            Sprite roundFill, Sprite roundStroke, Font font, Color field, Color line)
        {
            var fill = CreateSliced(name, parent, roundFill, 12f, 24f, field);
            PlaceTopLeft(fill.rectTransform, x, y, width, 44f);
            fill.raycastTarget = true;
            var border = CreateSliced("Border", fill.transform, roundStroke, 12f, 24f, line);
            Stretch(border.rectTransform);
            var label = CreateLabel("Label", fill.transform, font, text, 15, Color.white, TextAnchor.MiddleLeft);
            if (icon == null)
            {
                // Faqat matn: markazda (skript matnni o'zgartirsa ham markazda qoladi)
                label.alignment = TextAnchor.MiddleCenter;
                Stretch(label.rectTransform);
            }
            else
            {
                float textWidth = label.preferredWidth;
                float total = 18f + 8f + textWidth;
                var image = CreateImage("Icon", fill.transform, icon, new Vector2(18f, 18f), new Vector2(-total / 2f + 9f, 0f), Color.white);
                image.raycastTarget = false;
                Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-total / 2f + 26f, 0f),
                    new Vector2(textWidth + 4f, 24f));
            }

            var button = fill.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.6f, 1.6f, 1.7f, 1f);
            colors.pressedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            return button;
        }
    }
}

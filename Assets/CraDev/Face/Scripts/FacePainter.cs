using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CraDev.Face
{
    /// <summary>O'yinchining yuzi: rasm (o'qiladigan Texture2D) va undagi 478 nuqta (0..1, v yuqoriga).</summary>
    public sealed class FaceData
    {
        public Texture2D Photo;
        public Vector2[] Landmarks;
    }

    /// <summary>
    /// Rasmdagi yuzni qahramon boshining teksturasiga "chizadi".
    ///
    /// Har bir avatar uchun builder oldindan yuz nuqtalarining bosh teksturasidagi o'rnini (faceUv) va ular
    /// orasidagi uchburchaklarni topib qo'ygan. Rasmdagi har bir uchburchak teksturadagi mos uchburchakka
    /// cho'ziladi: yuz qahramon yuzining shakliga aniq tushadi. Chetlari (yuz ovali, ko'zlar va to'rning tashqi
    /// chegarasi) silliq shaffoflashadi, rasmning teri rangi esa qahramon terisiga moslashtiriladi.
    ///
    /// Yuz xaritasidagi xatolar o'yinda to'g'rilanadi (<see cref="CleanTriangles"/>): boshqa UV orolga tushib
    /// qolgan nuqtalar (ko'z, og'iz ichi) va ularga "ko'prik" bo'lgan uzun uchburchaklar tashlanadi, ovalning
    /// topilmagan nuqtalari o'rnida esa to'rning haqiqiy chegarasi so'nadi - chetda keskin chiziq qolmaydi.
    /// </summary>
    public static class FacePainter
    {
        // MediaPipe Face Mesh nuqtalari: yuz ovali va ko'z chiziqlari (u yerda rasm butunlay shaffof)
        static readonly int[] FaceOval =
        {
            10, 338, 297, 332, 284, 251, 389, 356, 454, 323, 361, 288, 397, 365, 379, 378, 400, 377,
            152, 148, 176, 149, 150, 136, 172, 58, 132, 93, 234, 127, 162, 21, 54, 103, 67, 109,
        };
        static readonly int[] Eyes =
        {
            33, 7, 163, 144, 145, 153, 154, 155, 133, 173, 157, 158, 159, 160, 161, 246,
            263, 249, 390, 373, 374, 380, 381, 382, 362, 398, 384, 385, 386, 387, 388, 466,
        };
        // Teri rangini solishtirish uchun nuqtalar: peshona, yonoqlar, iyak
        static readonly int[] SkinSamples = { 151, 108, 337, 50, 280, 205, 425, 199, 118, 347 };
        // Yuz markazi: burun va peshona o'rtasi (UV orolini aniqlash uchun)
        static readonly int[] CoreIds = { 1, 4, 5, 6, 8, 9, 151, 168, 195, 197 };

        /// <summary>Rasm terisi qahramon terisiga qanchalik moslashtiriladi (0 - o'zgarmaydi, 1 - to'liq).</summary>
        const float SkinMatch = 0.8f;

        /// <summary>Rang tusi qanchalik moslashtiriladi (0 - faqat yorug'lik, 1 - tus ham to'liq).</summary>
        const float HueMatch = 0.25f;

        /// <summary>Markazdan (nuqtalar masofasi medianasiga nisbatan) shundan uzoq nuqta - boshqa UV orol.</summary>
        const float OutlierRadius = 3.4f;

        /// <summary>Chegaradan keyingi qator nuqtalarining shaffofligi: so'nish kengroq va yumshoqroq bo'ladi.</summary>
        const float InnerRing = 0.6f;

        sealed class FaceMesh
        {
            public int[] Triangles;
            public float[] Alpha;
        }

        sealed class HeadSkin
        {
            public Vector2[] Uv;
            public Color Sum;
            public int Count;
        }

        // Har avatar xaritasi va bosh teksturasi uchun bir marta hisoblanadi (sahna almashsa o'zi tozalanadi)
        static readonly ConditionalWeakTable<int[], FaceMesh> meshes = new ConditionalWeakTable<int[], FaceMesh>();
        static readonly ConditionalWeakTable<Texture, HeadSkin> skins = new ConditionalWeakTable<Texture, HeadSkin>();

        /// <summary>
        /// Bosh teksturasi ustiga yuzni chizib, yangi RenderTexture qaytaradi (material'ga qo'yiladi).
        /// Uni chaqiruvchi o'zi Release va Destroy qiladi.
        /// </summary>
        public static RenderTexture Paint(Texture head, FaceData face, Vector2[] faceUv, int[] triangles, Material material)
        {
            RenderTexture result = null;
            Paint(head, face, faceUv, triangles, material, ref result);
            return result;
        }

        /// <summary>Yuzni result ga chizadi: o'lchami mos bo'lsa o'sha tekstura qayta ishlatiladi, aks holda yangisi yaratiladi.</summary>
        public static void Paint(Texture head, FaceData face, Vector2[] faceUv, int[] triangles, Material material, ref RenderTexture result, int maxSize = int.MaxValue)
        {
            int width = Mathf.Max(1, Mathf.Min(head.width, maxSize));
            int height = Mathf.Max(1, Mathf.Min(head.height, maxSize));
            if (result != null && (result.width != width || result.height != height))
            {
                result.Release();
                Release(result);
                result = null;
            }
            if (result == null)
                result = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                {
                    useMipMap = true,
                    autoGenerateMips = false,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 4,
                    name = "PlayerFace",
                };
            if (!result.IsCreated())
                result.Create();

            var mesh = Prepare(faceUv, triangles);
            var previous = RenderTexture.active;
            Graphics.Blit(head, result);

            material.mainTexture = face.Photo;
            // SetVector: SetColor chiziqli rang fazosida qiymatni yana o'zgartirib yuboradi
            var gain = SkinGain(head, face, faceUv);
            material.SetVector("_Gain", new Vector4(gain.r, gain.g, gain.b, 1f));

            RenderTexture.active = result;
            GL.PushMatrix();
            GL.LoadOrtho();
            material.SetPass(0);
            GL.Begin(GL.TRIANGLES);
            for (int i = 0; i < mesh.Triangles.Length; i++)
            {
                int k = mesh.Triangles[i];
                GL.Color(new Color(1f, 1f, 1f, mesh.Alpha[k]));
                GL.TexCoord2(face.Landmarks[k].x, face.Landmarks[k].y);
                GL.Vertex3(faceUv[k].x, faceUv[k].y, 0f);
            }
            GL.End();
            GL.PopMatrix();
            RenderTexture.active = previous;

            result.GenerateMips();
        }

        static void Release(Object o)
        {
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        static FaceMesh Prepare(Vector2[] uv, int[] triangles)
        {
            if (meshes.TryGetValue(triangles, out var cached))
                return cached;
            var clean = CleanTriangles(uv, triangles);
            var mesh = new FaceMesh { Triangles = clean, Alpha = BlendWeights(clean, Mathf.Max(uv.Length, FaceTracker.LandmarkCount)) };
            meshes.Add(triangles, mesh);
            return mesh;
        }

        /// <summary>
        /// Yuz to'ridan boshqa UV orolga tushgan nuqtalar (markazdan juda uzoq) va juda uzun uchburchaklar
        /// olib tashlanadi. Toza to'rga qayta qo'llansa natija o'zgarmaydi (builder ham ishlatadi).
        /// </summary>
        public static int[] CleanTriangles(Vector2[] uv, int[] triangles)
        {
            if (uv == null || triangles == null || triangles.Length < 3)
                return triangles ?? new int[0];
            var xs = new List<float>();
            var ys = new List<float>();
            foreach (int i in CoreIds)
                if (i < uv.Length && uv[i].x >= 0f)
                {
                    xs.Add(uv[i].x);
                    ys.Add(uv[i].y);
                }
            if (xs.Count == 0)
                return triangles;
            var center = new Vector2(Median(xs), Median(ys));
            var distances = new List<float>();
            int count = Mathf.Min(uv.Length, FaceTracker.MeshLandmarkCount);
            for (int i = 0; i < count; i++)
                if (uv[i].x >= 0f)
                    distances.Add(Vector2.Distance(uv[i], center));
            float limit = Median(distances) * OutlierRadius;

            bool Inside(int i) => i >= 0 && i < count && uv[i].x >= 0f && Vector2.Distance(uv[i], center) <= limit;
            bool Kept(int t) => Inside(triangles[t]) && Inside(triangles[t + 1]) && Inside(triangles[t + 2]);
            float Longest(int t) => Mathf.Max(Vector2.Distance(uv[triangles[t]], uv[triangles[t + 1]]),
                Mathf.Max(Vector2.Distance(uv[triangles[t + 1]], uv[triangles[t + 2]]), Vector2.Distance(uv[triangles[t + 2]], uv[triangles[t]])));

            var edges = new List<float>();
            for (int t = 0; t + 2 < triangles.Length; t += 3)
                if (Kept(t))
                    edges.Add(Longest(t));
            if (edges.Count == 0)
                return triangles;
            // Oddiy uchburchak qirrasi ~0.015; bundan 5 barobar uzuni - teshik yoki boshqa orol ustidan "ko'prik"
            float maxEdge = Mathf.Max(0.06f, Median(edges) * 5f);
            var result = new List<int>(triangles.Length);
            for (int t = 0; t + 2 < triangles.Length; t += 3)
                if (Kept(t) && Longest(t) <= maxEdge)
                {
                    result.Add(triangles[t]);
                    result.Add(triangles[t + 1]);
                    result.Add(triangles[t + 2]);
                }
            return result.ToArray();
        }

        static float Median(List<float> values)
        {
            if (values.Count == 0)
                return 0f;
            values.Sort();
            return values[values.Count / 2];
        }

        /// <summary>
        /// Har bir nuqtaning shaffofligi: yuz ovali, ko'z chiziqlari va to'rning tashqi chegarasida 0, chegaraning
        /// ichki qo'shnilarida <see cref="InnerRing"/>, qolganlarida 1 (shader ularni silliq egri bilan so'ndiradi).
        /// </summary>
        static float[] BlendWeights(int[] triangles, int count)
        {
            var alpha = new float[count];
            for (int i = 0; i < count; i++)
                alpha[i] = 1f;
            // Faqat bitta uchburchakka tegishli qirralar - to'rning chegarasi
            var uses = new Dictionary<long, int>();
            for (int t = 0; t + 2 < triangles.Length; t += 3)
                for (int e = 0; e < 3; e++)
                {
                    int a = triangles[t + e], b = triangles[t + (e + 1) % 3];
                    long key = (long)Mathf.Min(a, b) * 100000 + Mathf.Max(a, b);
                    uses.TryGetValue(key, out int n);
                    uses[key] = n + 1;
                }
            var outer = new HashSet<int>(FaceOval);
            foreach (var pair in uses)
                if (pair.Value == 1)
                {
                    outer.Add((int)(pair.Key / 100000));
                    outer.Add((int)(pair.Key % 100000));
                }
            foreach (int i in outer)
                if (i < count)
                    alpha[i] = 0f;
            foreach (int i in Eyes)
                if (i < count)
                    alpha[i] = 0f;
            // Tashqi chegaraning ichki qo'shnilari yarim shaffof (ko'z atrofiga tegilmaydi: u yerda aniq chiziq kerak)
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                if (!outer.Contains(triangles[t]) && !outer.Contains(triangles[t + 1]) && !outer.Contains(triangles[t + 2]))
                    continue;
                for (int e = 0; e < 3; e++)
                {
                    int v = triangles[t + e];
                    if (v < count && alpha[v] > InnerRing)
                        alpha[v] = InnerRing;
                }
            }
            return alpha;
        }

        /// <summary>
        /// Rasmdagi teri rangini qahramon terisiga yaqinlashtiruvchi ko'paytiruvchi (chiziqli rang fazosida).
        /// Aks holda yuz bilan bo'yin orasida rang farqi ko'rinib qoladi. Qahramon terisi har bosh teksturasi
        /// uchun bir marta o'qiladi (GPU'dan o'qish sekin).
        /// </summary>
        static Color SkinGain(Texture head, FaceData face, Vector2[] faceUv)
        {
            if (!skins.TryGetValue(head, out var skin) || skin.Uv != faceUv)
            {
                if (skin != null)
                    skins.Remove(head);
                skin = ReadHeadSkin(head, faceUv);
                skins.Add(head, skin);
            }

            Color photoSum = Color.clear;
            int count = 0;
            foreach (int i in SkinSamples)
            {
                if (faceUv[i].x < 0f)
                    continue;
                photoSum += Average(face.Photo, face.Landmarks[i]);
                count++;
            }
            if (count == 0 || skin.Count == 0)
                return Color.white;

            Color headLinear = Linear(skin.Sum / skin.Count), photoLinear = Linear(photoSum / count);
            float headLuma = Luma(headLinear), photoLuma = Luma(photoLinear);
            // Asosan yorug'lik moslashadi (yuz bo'yin bilan bir xil yoritilgandek); rang tusi (ton) esa o'yinchiniki
            // bo'lib qoladi, faqat ozgina yaqinlashtiriladi. Aks holda yuz sarg'ayib yoki ko'karib ketadi.
            float brightness = photoLuma > 0.001f ? Mathf.Clamp(headLuma / photoLuma, 0.5f, 2f) : 1f;
            Color gain = Color.white;
            for (int c = 0; c < 3; c++)
            {
                float full = photoLinear[c] > 0.001f ? Mathf.Clamp(headLinear[c] / photoLinear[c], 0.5f, 2f) : 1f;
                float target = Mathf.Lerp(brightness, full, HueMatch);
                gain[c] = Mathf.Lerp(1f, target, SkinMatch);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[CraDev] Teri rangi: qahramon {skin.Sum / skin.Count}, rasm {photoSum / count}, ko'paytiruvchi {gain}");
#endif
            return gain;
        }

        static HeadSkin ReadHeadSkin(Texture head, Vector2[] faceUv)
        {
            // Bosh teksturasini kichraytirib o'qiymiz (to'liq 2048x2048 ni o'qish shart emas)
            const int size = 256;
            var small = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            Graphics.Blit(head, small);
            RenderTexture.active = small;
            var readback = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            readback.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            readback.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(small);

            var skin = new HeadSkin { Uv = faceUv };
            foreach (int i in SkinSamples)
            {
                Vector2 uv = faceUv[i];
                if (uv.x < 0f)
                    continue;
                skin.Sum += readback.GetPixelBilinear(uv.x, uv.y);
                skin.Count++;
            }
            Release(readback);
            return skin;
        }

        static Color Linear(Color c) => new Color(Mathf.GammaToLinearSpace(c.r), Mathf.GammaToLinearSpace(c.g), Mathf.GammaToLinearSpace(c.b));

        static float Luma(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

        static Color Average(Texture2D photo, Vector2 uv)
        {
            float step = 3f / Mathf.Max(photo.width, photo.height);
            Color sum = Color.clear;
            for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                    sum += photo.GetPixelBilinear(uv.x + x * step, uv.y + y * step);
            return sum / 9f;
        }

        // ------------------------------------------------------------------ Builder uchun

        /// <summary>
        /// Teksturadagi yuz nuqtalari orasida uchburchaklar (Delaunay). Topilmagan nuqtalar (uv.x &lt; 0)
        /// va ko'z qorachiqlari qatnashmaydi. Builder bir marta hisoblab, sahnaga yozadi.
        /// </summary>
        public static int[] Triangulate(Vector2[] uv)
        {
            var ids = new List<int>();
            for (int i = 0; i < FaceTracker.MeshLandmarkCount && i < uv.Length; i++)
                if (uv[i].x >= 0f)
                    ids.Add(i);
            var points = new List<Vector2>();
            foreach (int i in ids)
                points.Add(uv[i]);

            // Bowyer-Watson: hammasini o'rab turgan katta uchburchakdan boshlab nuqtalar birma-bir qo'shiladi
            int n = points.Count;
            points.Add(new Vector2(-10f, -10f));
            points.Add(new Vector2(10f, -10f));
            points.Add(new Vector2(0.5f, 10f));
            var tris = new List<(int a, int b, int c)> { (n, n + 1, n + 2) };
            for (int p = 0; p < n; p++)
            {
                var bad = new List<(int a, int b, int c)>();
                foreach (var t in tris)
                    if (InCircumcircle(points[p], points[t.a], points[t.b], points[t.c]))
                        bad.Add(t);
                var edges = new List<(int, int)>();
                foreach (var t in bad)
                    foreach (var e in new[] { (t.a, t.b), (t.b, t.c), (t.c, t.a) })
                    {
                        int shared = 0;
                        foreach (var o in bad)
                            if (!o.Equals(t) && HasEdge(o, e.Item1, e.Item2))
                                shared++;
                        if (shared == 0)
                            edges.Add(e);
                    }
                tris.RemoveAll(t => bad.Contains(t));
                foreach (var e in edges)
                    tris.Add((e.Item1, e.Item2, p));
            }

            var result = new List<int>();
            foreach (var t in tris)
            {
                if (t.a >= n || t.b >= n || t.c >= n)
                    continue;
                result.Add(ids[t.a]);
                result.Add(ids[t.b]);
                result.Add(ids[t.c]);
            }
            return result.ToArray();
        }

        static bool HasEdge((int a, int b, int c) t, int u, int v) =>
            (t.a == u || t.b == u || t.c == u) && (t.a == v || t.b == v || t.c == v);

        static bool InCircumcircle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            double ax = a.x - p.x, ay = a.y - p.y, bx = b.x - p.x, by = b.y - p.y, cx = c.x - p.x, cy = c.y - p.y;
            double det = (ax * ax + ay * ay) * (bx * cy - cx * by) - (bx * bx + by * by) * (ax * cy - cx * ay)
                       + (cx * cx + cy * cy) * (ax * by - bx * ay);
            double orient = (b.x - a.x) * (double)(c.y - a.y) - (b.y - a.y) * (double)(c.x - a.x);
            return orient > 0 ? det > 0 : det < 0;
        }
    }
}

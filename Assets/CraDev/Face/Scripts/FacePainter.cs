using System.Collections.Generic;
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
    /// cho'ziladi: yuz qahramon yuzining shakliga aniq tushadi. Chetlari (yuz ovali, ko'zlar) silliq
    /// shaffoflashadi, rasmning teri rangi esa qahramon terisiga moslashtiriladi.
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

        /// <summary>Rasm terisi qahramon terisiga qanchalik moslashtiriladi (0 - o'zgarmaydi, 1 - to'liq).</summary>
        const float SkinMatch = 0.8f;

        /// <summary>Rang tusi qanchalik moslashtiriladi (0 - faqat yorug'lik, 1 - tus ham to'liq).</summary>
        const float HueMatch = 0.25f;

        static float[] alpha;

        /// <summary>
        /// Bosh teksturasi ustiga yuzni chizib, yangi RenderTexture qaytaradi (material'ga qo'yiladi).
        /// Eskisini chaqiruvchi o'zi Release qiladi.
        /// </summary>
        public static RenderTexture Paint(Texture head, FaceData face, Vector2[] faceUv, int[] triangles, Material material)
        {
            var result = new RenderTexture(head.width, head.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                useMipMap = true,
                autoGenerateMips = false,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 4,
                name = "PlayerFace",
            };
            result.Create();
            Graphics.Blit(head, result);

            material.mainTexture = face.Photo;
            // SetVector: SetColor chiziqli rang fazosida qiymatni yana o'zgartirib yuboradi
            var gain = SkinGain(head, face, faceUv);
            material.SetVector("_Gain", new Vector4(gain.r, gain.g, gain.b, 1f));

            var previous = RenderTexture.active;
            RenderTexture.active = result;
            GL.PushMatrix();
            GL.LoadOrtho();
            material.SetPass(0);
            GL.Begin(GL.TRIANGLES);
            var weights = Alpha();
            for (int i = 0; i < triangles.Length; i++)
            {
                int k = triangles[i];
                GL.Color(new Color(1f, 1f, 1f, weights[k]));
                GL.TexCoord2(face.Landmarks[k].x, face.Landmarks[k].y);
                GL.Vertex3(faceUv[k].x, faceUv[k].y, 0f);
            }
            GL.End();
            GL.PopMatrix();
            RenderTexture.active = previous;

            result.GenerateMips();
            return result;
        }

        /// <summary>Har bir nuqtaning shaffofligi: ovalda va ko'z chizig'ida 0, qolganlarida 1.</summary>
        static float[] Alpha()
        {
            if (alpha != null)
                return alpha;
            alpha = new float[FaceTracker.LandmarkCount];
            for (int i = 0; i < alpha.Length; i++)
                alpha[i] = 1f;
            foreach (int i in FaceOval) alpha[i] = 0f;
            foreach (int i in Eyes) alpha[i] = 0f;
            return alpha;
        }

        /// <summary>
        /// Rasmdagi teri rangini qahramon terisiga yaqinlashtiruvchi ko'paytiruvchi (chiziqli rang fazosida).
        /// Aks holda yuz bilan bo'yin orasida rang farqi ko'rinib qoladi.
        /// </summary>
        static Color SkinGain(Texture head, FaceData face, Vector2[] faceUv)
        {
            // Bosh teksturasini kichraytirib o'qiymiz (to'liq 2048x2048 ni o'qish shart emas)
            const int size = 256;
            var small = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(head, small);
            var previous = RenderTexture.active;
            RenderTexture.active = small;
            var readback = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            readback.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            readback.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(small);

            Color headSum = Color.clear, photoSum = Color.clear;
            int count = 0;
            foreach (int i in SkinSamples)
            {
                Vector2 uv = faceUv[i];
                if (uv.x < 0f)
                    continue;
                headSum += readback.GetPixelBilinear(uv.x, uv.y);
                photoSum += Average(face.Photo, face.Landmarks[i]);
                count++;
            }
            if (Application.isPlaying) Object.Destroy(readback); else Object.DestroyImmediate(readback);
            if (count == 0)
                return Color.white;

            Color headLinear = Linear(headSum / count), photoLinear = Linear(photoSum / count);
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
            Debug.Log($"[CraDev] Teri rangi: qahramon {headSum / count}, rasm {photoSum / count}, ko'paytiruvchi {gain}");
            return gain;
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

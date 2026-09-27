using System;
using Unity.InferenceEngine;
using UnityEngine;

namespace CraDev.Face
{
    /// <summary>
    /// Rasmdagi yuzni topadi va uning 478 ta nuqtasini (ko'z, qosh, burun, lab, jag' chizig'i) aniqlaydi.
    ///
    /// Ikki bosqich, ikkalasi ham Google MediaPipe modellari (Apache 2.0, Assets/CraDev/Face/Models):
    /// 1) face_detector (BlazeFace, 128x128): yuz qayerda va qaysi burchakka qiyshaygan;
    /// 2) face_landmarks_detector (Face Mesh, 256x256): yuz kesib olinib, tekislanib, nuqtalari topiladi.
    /// Aniqlik uchun 2-bosqich topilgan nuqtalar bo'yicha yana bir marta takrorlanadi.
    ///
    /// Rasm Color32[] ko'rinishida beriladi (Unity tartibi: pastki qatordan boshlanadi).
    /// Natija: nuqtalar tekstura koordinatalarida (0..1, v yuqoriga).
    /// </summary>
    public sealed class FaceTracker : IDisposable
    {
        public const int LandmarkCount = 478;
        public const int MeshLandmarkCount = 468; // qolgan 10 tasi ko'z qorachig'i

        const int DetectorSize = 128;
        const int LandmarkSize = 256;
        const float DetectionThreshold = 0.5f;
        const float PresenceThreshold = 0.5f;
        const float CropScale = 1.5f;

        readonly Worker detector;
        readonly Worker landmarker;
        readonly Vector2[] anchors;

        public FaceTracker(ModelAsset detectorModel, ModelAsset landmarkModel)
        {
            var backend = SystemInfo.supportsComputeShaders ? BackendType.GPUCompute : BackendType.CPU;
            detector = new Worker(ModelLoader.Load(detectorModel), backend);
            landmarker = new Worker(ModelLoader.Load(landmarkModel), backend);
            anchors = BuildAnchors();
        }

        public void Dispose()
        {
            detector?.Dispose();
            landmarker?.Dispose();
        }

        /// <summary>Yuzni topadi. Yuz topilmasa false (error - o'yinchiga ko'rsatiladigan inglizcha sabab).</summary>
        public bool Track(Color32[] pixels, int width, int height, out Vector2[] landmarks, out string error)
        {
            landmarks = null;
            var image = new Image(pixels, width, height);

            if (!Detect(image, out Vector2 center, out float size, out float angle))
            {
                error = "No face found. Use a clear, front-facing photo with good light.";
                return false;
            }

            // Birinchi o'tish: detektor qutisi bo'yicha; ikkinchisi: topilgan nuqtalar bo'yicha aniqroq kesish
            Vector2[] points = null;
            for (int pass = 0; pass < 2; pass++)
            {
                if (!Landmarks(image, center, size, angle, out var found))
                {
                    if (points != null)
                        break;
                    error = "The face is not clear enough. Look straight at the camera.";
                    return false;
                }
                points = found;
                Refine(points, out center, out size, out angle);
            }

            landmarks = new Vector2[LandmarkCount];
            for (int i = 0; i < LandmarkCount; i++)
                landmarks[i] = new Vector2(points[i].x / width, 1f - points[i].y / height);
            error = null;
            return true;
        }

        // ------------------------------------------------------------------ 1. Yuzni topish

        bool Detect(Image image, out Vector2 center, out float size, out float angle)
        {
            // Rasm kvadratga joylanadi (ortiqcha joy qora), 128x128 ga kichraytiriladi, qiymatlar -1..1
            int side = Mathf.Max(image.Width, image.Height);
            float ox = (side - image.Width) / 2f, oy = (side - image.Height) / 2f;
            var input = new float[DetectorSize * DetectorSize * 3];
            for (int y = 0; y < DetectorSize; y++)
                for (int x = 0; x < DetectorSize; x++)
                {
                    float px = (x + 0.5f) / DetectorSize * side - ox;
                    float py = (y + 0.5f) / DetectorSize * side - oy;
                    Color c = image.Sample(px, py);
                    int i = (y * DetectorSize + x) * 3;
                    input[i] = c.r * 2f - 1f;
                    input[i + 1] = c.g * 2f - 1f;
                    input[i + 2] = c.b * 2f - 1f;
                }

            float[] boxes, scores;
            using (var tensor = new Tensor<float>(new TensorShape(1, DetectorSize, DetectorSize, 3), input))
            {
                detector.Schedule(tensor);
                boxes = (detector.PeekOutput("regressors") as Tensor<float>).DownloadToArray();
                scores = (detector.PeekOutput("classificators") as Tensor<float>).DownloadToArray();
            }

            // Eng ishonchli yuz; bir necha yuz bo'lsa kattarog'i afzal
            int best = -1;
            float bestRank = 0f;
            for (int i = 0; i < anchors.Length; i++)
            {
                float score = Sigmoid(scores[i]);
                if (score < DetectionThreshold)
                    continue;
                float rank = score * Mathf.Sqrt(Mathf.Max(boxes[i * 16 + 2], 1f));
                if (rank > bestRank)
                {
                    bestRank = rank;
                    best = i;
                }
            }

            center = Vector2.zero;
            size = angle = 0f;
            if (best < 0)
                return false;

            // Koordinatalar 128 lik kvadratda: langar nuqtasiga nisbatan siljish
            Vector2 anchor = anchors[best];
            int o = best * 16;
            Vector2 ToImage(float rx, float ry) =>
                new Vector2((rx / DetectorSize + anchor.x) * side - ox, (ry / DetectorSize + anchor.y) * side - oy);
            center = ToImage(boxes[o], boxes[o + 1]);
            size = Mathf.Max(boxes[o + 2], boxes[o + 3]) / DetectorSize * side * CropScale;
            // 0 - o'ng ko'z, 1 - chap ko'z (rasmda chapdan o'ngga): yuz shu chiziq bo'yicha tekislanadi
            Vector2 eyeA = ToImage(boxes[o + 4], boxes[o + 5]);
            Vector2 eyeB = ToImage(boxes[o + 6], boxes[o + 7]);
            angle = Mathf.Atan2(eyeB.y - eyeA.y, eyeB.x - eyeA.x);
            return true;
        }

        /// <summary>BlazeFace (short range) langar nuqtalari: 16x16 to'rda 2 tadan va 8x8 da 6 tadan, jami 896.</summary>
        static Vector2[] BuildAnchors()
        {
            var list = new Vector2[896];
            int n = 0;
            foreach (var (grid, perCell) in new[] { (16, 2), (8, 6) })
                for (int y = 0; y < grid; y++)
                    for (int x = 0; x < grid; x++)
                        for (int k = 0; k < perCell; k++)
                            list[n++] = new Vector2((x + 0.5f) / grid, (y + 0.5f) / grid);
            return list;
        }

        // ------------------------------------------------------------------ 2. Yuz nuqtalari

        bool Landmarks(Image image, Vector2 center, float size, float angle, out Vector2[] points)
        {
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            var input = new float[LandmarkSize * LandmarkSize * 3];
            for (int y = 0; y < LandmarkSize; y++)
                for (int x = 0; x < LandmarkSize; x++)
                {
                    float u = (x + 0.5f) / LandmarkSize - 0.5f, v = (y + 0.5f) / LandmarkSize - 0.5f;
                    Color col = image.Sample(center.x + (u * c - v * s) * size, center.y + (u * s + v * c) * size);
                    int i = (y * LandmarkSize + x) * 3;
                    input[i] = col.r;
                    input[i + 1] = col.g;
                    input[i + 2] = col.b;
                }

            float[] raw, presence;
            using (var tensor = new Tensor<float>(new TensorShape(1, LandmarkSize, LandmarkSize, 3), input))
            {
                landmarker.Schedule(tensor);
                raw = (landmarker.PeekOutput("Identity") as Tensor<float>).DownloadToArray();
                presence = (landmarker.PeekOutput("Identity_1") as Tensor<float>).DownloadToArray();
            }

            points = null;
            if (Sigmoid(presence[0]) < PresenceThreshold)
                return false;

            points = new Vector2[LandmarkCount];
            for (int i = 0; i < LandmarkCount; i++)
            {
                float u = raw[i * 3] / LandmarkSize - 0.5f, v = raw[i * 3 + 1] / LandmarkSize - 0.5f;
                points[i] = new Vector2(center.x + (u * c - v * s) * size, center.y + (u * s + v * c) * size);
            }
            return true;
        }

        /// <summary>Topilgan nuqtalardan keyingi kesish: markaz, o'lcham va ko'z chizig'i burchagi.</summary>
        static void Refine(Vector2[] points, out Vector2 center, out float size, out float angle)
        {
            Vector2 min = points[0], max = points[0];
            for (int i = 1; i < MeshLandmarkCount; i++)
            {
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }
            center = (min + max) / 2f;
            Vector2 a = points[33], b = points[263]; // ko'zlarning tashqi burchaklari
            angle = Mathf.Atan2(b.y - a.y, b.x - a.x);
            // Burchakka qaramay yuz o'lchami: ko'zlar orasidan va balandlikdan
            size = Mathf.Max(max.x - min.x, max.y - min.y) * CropScale;
        }

        static float Sigmoid(float x) => 1f / (1f + Mathf.Exp(-Mathf.Clamp(x, -60f, 60f)));

        /// <summary>Piksellarni yuqori chapdan (x o'ngga, y pastga) o'qish; chegaradan tashqarisi qora.</summary>
        readonly struct Image
        {
            readonly Color32[] pixels;
            public readonly int Width, Height;

            public Image(Color32[] pixels, int width, int height)
            {
                this.pixels = pixels;
                Width = width;
                Height = height;
            }

            public Color Sample(float x, float y)
            {
                x -= 0.5f;
                y -= 0.5f;
                int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
                float fx = x - x0, fy = y - y0;
                Color a = At(x0, y0), b = At(x0 + 1, y0), c = At(x0, y0 + 1), d = At(x0 + 1, y0 + 1);
                return Color.Lerp(Color.Lerp(a, b, fx), Color.Lerp(c, d, fx), fy);
            }

            Color At(int x, int y)
            {
                if (x < 0 || y < 0 || x >= Width || y >= Height)
                    return Color.black;
                return pixels[(Height - 1 - y) * Width + x];
            }
        }
    }
}

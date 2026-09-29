using System;
using System.Collections;
using Unity.InferenceEngine;
using UnityEngine;

namespace CraDev.Face
{
    /// <summary>Jonli skanerning maslahati: yuz kadrda qanday turibdi.</summary>
    public enum ScanHint { Find, Closer, Back, Center, Straight, Light, Still, Good }

    /// <summary>
    /// Jonli yuz skaneri (UI'siz, oddiy C# klass): kamera (WebCamTexture) va kadrdagi yuzni tekshirish.
    ///
    /// Kamera: qurilmalar ro'yxati, oxirgi ishlagan kamera eslab qolinadi (PlayerPrefs), birinchi marta esa
    /// virtual kameralar (OBS, Broadcast...) emas, haqiqiy kamera afzal. Birinchi kadr 5 soniyada kelmasa (kamera
    /// boshqa dasturda band yoki Windows maxfiylik sozlamalarida o'chirilgan) - Failed. Ishlab turgan kamera
    /// kadr bermay qolsa ham Failed (Lost).
    ///
    /// Tekshirish (<see cref="Analyze"/>): faqat 128x128 detektor (Face Mesh emas), shuning uchun tez; natija -
    /// maslahat: yaqinroq, markazga, to'g'ri qarang, yorug'lik, qimirlamang yoki "yaxshi".
    /// </summary>
    public sealed class FaceScanner : IDisposable
    {
        public enum CameraState { Off, NoDevice, Starting, Live, Failed }

        const string DeviceKey = "cradev.face.camera";
        const float FirstFrameTimeout = 5f;
        const float StallTimeout = 4f;
        static readonly string[] VirtualCameras =
            { "obs", "virtual", "broadcast", "snap camera", "xsplit", "manycam", "ndi", "streamlabs", "splitcam", "e2esoft" };

        // Sifat chegaralari (kadr balandligiga nisbatan)
        const float MinFace = 0.26f, MaxFace = 0.75f;
        const float MaxOffsetX = 0.13f, MaxOffsetY = 0.16f;
        const float MaxYaw = 0.2f, MaxRoll = 12f;
        const float MinLuma = 0.24f;
        const float MaxMove = 0.04f;

        readonly ModelAsset detectorModel, landmarkModel;
        FaceTracker tracker;
        WebCamTexture webcam;
        WebCamDevice[] devices = Array.Empty<WebCamDevice>();
        int device = -1;
        float startedAt, lastFrameAt;
        Color32[] frame;
        float[] detectorBuffer;
        Vector2 lastCenter;
        bool hasLast;

        public FaceScanner(ModelAsset detectorModel, ModelAsset landmarkModel)
        {
            this.detectorModel = detectorModel;
            this.landmarkModel = landmarkModel;
        }

        public CameraState State { get; private set; }

        /// <summary>Failed: kamera ishlab turib uzildi (true) yoki umuman ochilmadi (false).</summary>
        public bool Lost { get; private set; }

        /// <summary>Yuz aniqlash modellari yuklanmadi yoki ishlamadi (o'yinchiga ko'rsatiladigan matn).</summary>
        public string Error { get; private set; }

        public WebCamTexture Texture => webcam;
        public int DeviceCount => devices.Length;
        public string DeviceName => device >= 0 && device < devices.Length ? devices[device].name : "";
        public bool TrackerReady => tracker != null;

        /// <summary>Yuz aniqlagich (birinchi murojaatda yuklanadi: ~5 MB model).</summary>
        public FaceTracker Tracker => tracker ??= new FaceTracker(detectorModel, landmarkModel);

        // ------------------------------------------------------------------ Kamera

        /// <summary>Kamerani yoqadi. next - ro'yxatdagi keyingi kamera (bir nechta kamera bo'lsa).</summary>
        public void StartCamera(bool next = false)
        {
            string previous = DeviceName;
            StopCamera();
            Lost = false;
            Error = null;
            hasLast = false;
            try { devices = WebCamTexture.devices ?? Array.Empty<WebCamDevice>(); }
            catch (Exception e)
            {
                Debug.LogWarning("[CraDev] Kameralar ro'yxatini olib bo'lmadi: " + e.Message);
                devices = Array.Empty<WebCamDevice>();
            }
            if (devices.Length == 0)
            {
                device = -1;
                State = CameraState.NoDevice;
                return;
            }
            int index = Array.FindIndex(devices, d => d.name == previous);
            // Eslab qolinadi faqat kadr bergan kamera (Remember, Live holatida)
            device = next && index >= 0 ? (index + 1) % devices.Length : Choose(devices, PlayerPrefs.GetString(DeviceKey, ""));

            try
            {
                webcam = new WebCamTexture(devices[device].name, 1280, 720, 30);
                webcam.Play();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CraDev] Kamerani yoqib bo'lmadi: " + e.Message);
                Fail(false);
                return;
            }
            State = CameraState.Starting;
            startedAt = Time.realtimeSinceStartup;
        }

        public void StopCamera()
        {
            if (webcam != null)
            {
                try { webcam.Stop(); }
                catch (Exception e) { Debug.LogWarning("[CraDev] Kamerani to'xtatib bo'lmadi: " + e.Message); }
                UnityEngine.Object.Destroy(webcam);
                webcam = null;
            }
            State = CameraState.Off;
        }

        /// <summary>Har kadrda: birinchi kadr keldimi, kamera uzilmadimi.</summary>
        public void Update()
        {
            if (webcam == null)
                return;
            float now = Time.realtimeSinceStartup;
            bool fresh = webcam.didUpdateThisFrame;
            if (State == CameraState.Starting)
            {
                if (fresh && webcam.width > 16)
                {
                    State = CameraState.Live;
                    lastFrameAt = now;
                    Remember();
                }
                else if (now - startedAt > FirstFrameTimeout)
                    Fail(false);
            }
            else if (State == CameraState.Live)
            {
                if (fresh)
                    lastFrameAt = now;
                else if (!webcam.isPlaying || now - lastFrameAt > StallTimeout)
                    Fail(true);
            }
        }

        void Fail(bool lost)
        {
            StopCamera();
            State = CameraState.Failed;
            Lost = lost;
        }

        void Remember()
        {
            if (string.IsNullOrEmpty(DeviceName))
                return;
            PlayerPrefs.SetString(DeviceKey, DeviceName);
            PlayerPrefs.Save();
        }

        /// <summary>Eslab qolingan kamera; bo'lmasa virtual bo'lmagan (va old) kamera.</summary>
        static int Choose(WebCamDevice[] list, string remembered)
        {
            if (!string.IsNullOrEmpty(remembered))
                for (int i = 0; i < list.Length; i++)
                    if (list[i].name == remembered)
                        return i;
            int best = 0, bestRank = int.MinValue;
            for (int i = 0; i < list.Length; i++)
            {
                int rank = (IsVirtual(list[i].name) ? 0 : 2) + (list[i].isFrontFacing ? 1 : 0);
                if (rank > bestRank)
                {
                    bestRank = rank;
                    best = i;
                }
            }
            return best;
        }

        static bool IsVirtual(string name)
        {
            string lower = (name ?? "").ToLowerInvariant();
            foreach (var word in VirtualCameras)
                if (lower.Contains(word))
                    return true;
            return false;
        }

        /// <summary>Joriy kadr: burilish va teskari tasvir to'g'rilangan rasm (kamera ishlayotgan bo'lsa).</summary>
        public Texture2D Capture() => State == CameraState.Live && webcam != null ? FacePhoto.Capture(webcam) : null;

        /// <summary>Oyna uchun: ko'zgudagidek (chap-o'ng almashgan), kerak bo'lsa tik holatda ham aylantirilgan.</summary>
        public Rect PreviewRect => webcam != null && webcam.videoVerticallyMirrored ? new Rect(1f, 1f, -1f, -1f) : new Rect(1f, 0f, -1f, 1f);

        public float PreviewAngle => webcam != null ? webcam.videoRotationAngle : 0f;

        public float PreviewAspect
        {
            get
            {
                if (webcam == null || webcam.width <= 16)
                    return 16f / 9f;
                float aspect = (float)webcam.width / Mathf.Max(1, webcam.height);
                return (Mathf.RoundToInt(PreviewAngle / 90f) & 1) == 1 ? 1f / aspect : aspect;
            }
        }

        // ------------------------------------------------------------------ Kadrni tekshirish

        /// <summary>Tekshirish to'xtatilsa (korutina kesilsa): umumiy buferlar yangidan yaratiladi.</summary>
        public void ResetBuffers()
        {
            frame = null;
            detectorBuffer = null;
            hasLast = false;
        }

        /// <summary>Joriy kadrni tekshiradi (fon oqimida tayyorlanadi, detektor asosiy oqimda). Natija done() ga.</summary>
        public IEnumerator Analyze(Action<ScanHint> done)
        {
            if (State != CameraState.Live || webcam == null || webcam.width <= 16)
            {
                done(ScanHint.Find);
                yield break;
            }
            int w = webcam.width, h = webcam.height;
            bool flip = webcam.videoVerticallyMirrored;
            FaceTracker current = null;
            if (!ReadFrame(w, h) || !TryTracker(out current))
            {
                done(ScanHint.Find);
                yield break;
            }
            detectorBuffer ??= new float[FaceTracker.DetectorInputLength];
            var job = new FaceJob();
            var pixels = frame;
            yield return current.DetectAsync(pixels, w, h, flip, detectorBuffer, job);
            if (job.Error != null)
            {
                Error = job.Error;
                done(ScanHint.Find);
                yield break;
            }
            done(job.Found ? Assess(job.Box, pixels, w, h, flip) : Missing());
        }

        bool ReadFrame(int w, int h)
        {
            try
            {
                if (frame == null || frame.Length != w * h)
                    frame = new Color32[w * h];
                webcam.GetPixels32(frame);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CraDev] Kamera kadrini o'qib bo'lmadi: " + e.Message);
                return false;
            }
        }

        bool TryTracker(out FaceTracker result)
        {
            result = null;
            try
            {
                result = Tracker;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[CraDev] Yuz aniqlash modellarini yuklab bo'lmadi: " + e);
                Error = Loc.T("face.error");
                return false;
            }
        }

        ScanHint Missing()
        {
            hasLast = false;
            return ScanHint.Find;
        }

        /// <summary>Topilgan yuz qanchalik yaxshi turibdi: o'lchami, markazdan uzoqligi, burilishi, yorug'lik, harakat.</summary>
        ScanHint Assess(FaceBox box, Color32[] pixels, int w, int h, bool flipY)
        {
            float frameSide = Mathf.Min(w, h);
            float ratio = box.Size / frameSide;
            float dx = Mathf.Abs(box.Center.x - w * 0.5f) / w, dy = Mathf.Abs(box.Center.y - h * 0.5f) / h;
            float eyes = Vector2.Distance(box.EyeA, box.EyeB);
            float yaw = eyes > 1f ? (box.Nose.x - (box.EyeA.x + box.EyeB.x) * 0.5f) / eyes : 1f;
            float roll = Mathf.Abs(Mathf.DeltaAngle(0f, box.Angle * Mathf.Rad2Deg));
            bool moved = hasLast && Vector2.Distance(lastCenter, box.Center) > MaxMove * frameSide;
            lastCenter = box.Center;
            hasLast = true;

            if (ratio < MinFace) return ScanHint.Closer;
            if (ratio > MaxFace) return ScanHint.Back;
            if (dx > MaxOffsetX || dy > MaxOffsetY) return ScanHint.Center;
            if (Mathf.Abs(yaw) > MaxYaw || roll > MaxRoll) return ScanHint.Straight;
            if (MeanLuma(pixels, w, h, box, flipY) < MinLuma) return ScanHint.Light;
            if (moved) return ScanHint.Still;
            return ScanHint.Good;
        }

        /// <summary>Yuz qismining o'rtacha yorqinligi (0..1, sRGB): 10x10 nuqta.</summary>
        static float MeanLuma(Color32[] pixels, int w, int h, FaceBox box, bool flipY)
        {
            float sum = 0f;
            int count = 0;
            float half = box.Size * 0.35f;
            for (int j = 0; j < 10; j++)
                for (int i = 0; i < 10; i++)
                {
                    int x = Mathf.RoundToInt(box.Center.x - half + (i + 0.5f) / 10f * half * 2f);
                    int y = Mathf.RoundToInt(box.Center.y - half + (j + 0.5f) / 10f * half * 2f);
                    if (x < 0 || y < 0 || x >= w || y >= h)
                        continue;
                    var c = pixels[(flipY ? y : h - 1 - y) * w + x];
                    sum += (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;
                    count++;
                }
            return count > 0 ? sum / count : 0f;
        }

        public void Dispose()
        {
            StopCamera();
            tracker?.Dispose();
            tracker = null;
            ResetBuffers();
        }
    }
}

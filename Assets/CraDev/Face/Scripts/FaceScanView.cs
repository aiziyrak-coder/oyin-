using System;
using System.Collections;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraDev.Face
{
    /// <summary>
    /// Jonli yuz skaneri oynasi: avatar yaratish ekrani (FaceCapture) va lobbydagi avatar studiyasi uchun umumiy.
    /// Oynaning tuzilishini builder quradi, bu skript faqat holatlarni boshqaradi:
    ///
    /// 1) Kamera: ko'zgudagidek tasvir, oval rangi (qizg'ish-sariq - maslahat bor, yashil - yaxshi) va maslahat
    ///    matni (~5 marta soniyada tekshiriladi). "Suratga olish" faqat yuz yaxshi turganda ishlaydi; yuz bir oz
    ///    barqaror tursa 3-2-1 sanab o'zi suratga oladi.
    /// 2) Tekshirish: rasmda yuz nuqtalari topiladi (FaceTracker.TrackAsync, o'yin qotmaydi).
    /// 3) Natija: yuz 3D qahramonga vaqtincha qo'yiladi (<see cref="Preview"/>), o'yinchi "Ishlatish" yoki
    ///    "Qayta olish"ni tanlaydi. Yuklangan rasm ham shu 2-3 bosqichdan o'tadi.
    /// Kamera ochilmasa (band, o'chirilgan, yo'q) - tushunarli xabar, "Qayta urinish" va "Rasm yuklash".
    ///
    /// Oyna yuzni o'zi saqlamaydi: tanlangan yuz <see cref="Used"/> bilan egasiga beriladi (Texture2D ham).
    /// </summary>
    public class FaceScanView : MonoBehaviour
    {
        enum Mode { Closed, Camera, Processing, Review, Rejected }

        [Header("Modellar (Assets/CraDev/Face/Models)")]
        [SerializeField] ModelAsset detectorModel;
        [SerializeField] ModelAsset landmarkModel;

        [Header("Oyna")]
        [Tooltip("Ochiq/yopiq bo'ladigan obyekt (skaner oynasi yoki sahifasi).")]
        [SerializeField] GameObject window;
        [SerializeField] RawImage preview;
        [SerializeField] AspectRatioFitter previewFitter;
        [SerializeField] Image oval;
        [SerializeField] Text countdown;
        [SerializeField] Image spinner;
        [Tooltip("Kamera ishlamasa: rasm ustidagi xabar.")]
        [SerializeField] GameObject failure;
        [SerializeField] Text failureText;
        [SerializeField] Text hint;
        [SerializeField] Button switchButton;
        [SerializeField] Text switchLabel;
        [SerializeField] Button closeButton;
        [SerializeField] Button secondaryButton;
        [SerializeField] Text secondaryLabel;
        [SerializeField] Button primaryButton;
        [SerializeField] Text primaryLabel;

        [Header("Sozlamalar")]
        [Tooltip("Jonli tekshirish oralig'i (soniya).")]
        [SerializeField] float analyzeInterval = 0.2f;
        [Tooltip("Yuz barqaror yaxshi tursa 3-2-1 sanab o'zi suratga oladi.")]
        [SerializeField] bool autoCapture = true;
        [SerializeField] float countdownStep = 0.6f;

        static readonly Color Ok = new Color32(52, 211, 120, 255);
        static readonly Color Warn = new Color32(245, 165, 36, 255);
        static readonly Color Bad = new Color32(240, 92, 92, 255);
        static readonly Color Muted = new Color32(172, 176, 184, 255);

        FaceScanner scanner;
        Mode mode;
        bool fromCamera;
        bool trackerFailed;
        ScanHint lastHint = ScanHint.Find;
        int goodStreak, badStreak;
        float nextAnalysis;
        float warmAt = -1f;
        float countdownStart = -1f;
        Coroutine analysis, processing;
        bool analyzing;
        Texture2D pending;    // tekshirilayotgan rasm
        Texture2D rejected;   // yuz topilmagan rasm (xabar bilan ko'rsatib turiladi)
        FaceData candidate;   // topilgan, hali tanlanmagan yuz
        string message;
        string notice;
        float noticeUntil;
        FaceScanner.CameraState shownState;

        /// <summary>Vaqtinchalik ko'rinish: topilgan yuz qahramonga qo'yiladi; null - oldingi holat qaytariladi.</summary>
        public event Action<FaceData> Preview;

        /// <summary>O'yinchi "Ishlatish"ni bosdi: yuz (va uning rasmi) endi egasiniki.</summary>
        public event Action<FaceData> Used;

        /// <summary>Oyna yopildi (Ishlatish yoki bekor qilish).</summary>
        public event Action Closed;

        public bool IsOpen => mode != Mode.Closed;
        public bool Busy => mode == Mode.Processing;
        public bool Reviewing => mode == Mode.Review;
        public FaceScanner.CameraState CameraState => scanner != null ? scanner.State : FaceScanner.CameraState.Off;
        public bool FailureShown => failure != null && failure.activeSelf;

        /// <summary>Oyna ochiq yoki shu/oldingi kadrda yopilgan: Enter/Esc orqadagi formaga o'tib ketmasin.</summary>
        public bool BlocksKeys => IsOpen || Time.frameCount - closedFrame <= 1;

        int closedFrame = -10;

        void Awake()
        {
            scanner = new FaceScanner(detectorModel, landmarkModel);
            closeButton.onClick.AddListener(Close);
            primaryButton.onClick.AddListener(Primary);
            secondaryButton.onClick.AddListener(Secondary);
            if (switchButton != null)
                switchButton.onClick.AddListener(() => { Deselect(); StartCamera(true); });
            if (window != null)
                window.SetActive(false);
        }

        void OnEnable() => Loc.Changed += Refresh;

        void OnDisable()
        {
            Loc.Changed -= Refresh;
            // Sahna yopilmoqda: hodisalarsiz, faqat kamera va rasmlar bo'shatiladi
            if (!IsOpen)
                return;
            StopAnalysis();
            processing = null;
            scanner.StopCamera();
            DiscardCandidate();
            DiscardPhoto(ref rejected);
            DiscardPhoto(ref pending);
            mode = Mode.Closed;
        }

        void OnDestroy()
        {
            DiscardCandidate();
            DiscardPhoto(ref rejected);
            DiscardPhoto(ref pending);
            scanner?.Dispose();
        }

        /// <summary>Kamera va yuz modellarini bo'shatadi (oyna yopiq bo'lsa). Keyingi safar qayta yuklanadi.</summary>
        public void Release()
        {
            if (!IsOpen)
                scanner?.Dispose();
        }

        // ------------------------------------------------------------------ Ochish / yopish

        /// <summary>Jonli skaner: kamera yoqiladi.</summary>
        public void OpenCamera()
        {
            if (mode == Mode.Processing)
                return;
            ClearResult();
            Show();
            fromCamera = true;
            StartCamera(false);
        }

        /// <summary>Yuklangan rasm: to'g'ridan-to'g'ri tekshirish va natija bosqichi. Rasm endi oynaniki.</summary>
        public void OpenPhoto(Texture2D photo)
        {
            if (photo == null || mode == Mode.Processing)
                return;
            ClearResult();
            Show();
            StopCamera();
            fromCamera = false;
            Process(photo, false);
        }

        /// <summary>Bekor qilish: vaqtinchalik yuz olib tashlanadi, kamera o'chadi.</summary>
        public void Close()
        {
            if (mode == Mode.Closed)
                return;
            ClearResult();
            Hide();
            Closed?.Invoke();
        }

        void Show()
        {
            window.SetActive(true);
            Deselect();
        }

        void Hide()
        {
            StopAnalysis();
            if (processing != null)
            {
                StopCoroutine(processing);
                processing = null;
            }
            DiscardPhoto(ref pending);
            scanner.StopCamera();
            mode = Mode.Closed;
            countdownStart = -1f;
            notice = null;
            if (preview != null)
                preview.texture = null;
            window.SetActive(false);
            closedFrame = Time.frameCount;
        }

        void StartCamera(bool next)
        {
            StopAnalysis();
            DiscardPhoto(ref rejected);
            mode = Mode.Camera;
            trackerFailed = false;
            goodStreak = badStreak = 0;
            countdownStart = -1f;
            lastHint = ScanHint.Find;
            if (next)
                scanner.StartCamera(true);
            else
                scanner.StartCamera();
            ShowCameraTexture();
            nextAnalysis = Time.unscaledTime + 0.3f;
            // Modellar oyna ekranga chiqqandan keyin, kamera yoqilayotganda yuklanadi (bir martalik kichik to'xtalish)
            warmAt = scanner.TrackerReady ? -1f : Time.unscaledTime + 0.15f;
            Refresh();
        }

        void WarmUp()
        {
            warmAt = -1f;
            try
            {
                if (scanner.Tracker != null)
                    Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError("[CraDev] Yuz aniqlash modellarini yuklab bo'lmadi: " + e);
                trackerFailed = true;
                StopCamera();
                shownState = scanner.State;
                Refresh();
            }
        }

        void StopCamera()
        {
            StopAnalysis();
            countdownStart = -1f;
            scanner.StopCamera();
        }

        void ShowCameraTexture()
        {
            shownState = scanner.State;
            preview.texture = scanner.Texture;
            preview.uvRect = scanner.PreviewRect;
            preview.rectTransform.localEulerAngles = new Vector3(0f, 0f, -scanner.PreviewAngle);
            if (previewFitter != null)
                previewFitter.aspectRatio = scanner.PreviewAspect;
        }

        // ------------------------------------------------------------------ Kadrlar

        void Update()
        {
            if (mode == Mode.Closed)
                return;
            float now = Time.unscaledTime;
            if (spinner != null && spinner.enabled)
                spinner.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -now * 450f);
            if (notice != null && now >= noticeUntil)
            {
                notice = null;
                Refresh();
            }

            if (mode == Mode.Camera)
            {
                if (warmAt >= 0f && now >= warmAt && !Broken)
                    WarmUp();
                scanner.Update();
                if (scanner.State != shownState)
                {
                    if (scanner.State != FaceScanner.CameraState.Live)
                        StopAnalysis();
                    ShowCameraTexture();
                    Refresh();
                }
                if (scanner.State == FaceScanner.CameraState.Live)
                {
                    if (previewFitter != null)
                        previewFitter.aspectRatio = scanner.PreviewAspect;
                    if (!analyzing && now >= nextAnalysis)
                    {
                        // Bayroq alohida: korutina darhol tugasa ham keyingi tekshiruv to'xtab qolmaydi
                        nextAnalysis = now + analyzeInterval;
                        analyzing = true;
                        var routine = StartCoroutine(Analyze());
                        if (analyzing)
                            analysis = routine;
                    }
                    if (countdownStart >= 0f)
                    {
                        int left = 3 - Mathf.FloorToInt((now - countdownStart) / countdownStep);
                        if (left <= 0)
                        {
                            Capture();
                            return;
                        }
                        countdown.text = left.ToString();
                    }
                }
            }

            // Enter - asosiy tugma (Suratga olish / Ishlatish / Qayta urinish)
            if (Anim.SubmitPressed() && primaryButton.interactable && primaryButton.gameObject.activeInHierarchy)
                Primary();
        }

        IEnumerator Analyze()
        {
            var result = ScanHint.Find;
            yield return scanner.Analyze(h => result = h);
            analysis = null;
            analyzing = false;
            if (mode != Mode.Camera || scanner.State != FaceScanner.CameraState.Live)
                yield break;
            if (scanner.Error != null)
            {
                // Modellar ishlamadi: kamera o'chiriladi, xabar va "Qayta urinish"
                trackerFailed = true;
                StopCamera();
                shownState = scanner.State;
                Refresh();
                yield break;
            }
            lastHint = result;
            if (result == ScanHint.Good)
            {
                goodStreak++;
                badStreak = 0;
            }
            else
            {
                badStreak++;
                goodStreak = 0;
            }
            // Sanash paytida yuz ketib qolsa (ikki marta ketma-ket) - to'xtatiladi
            if (countdownStart >= 0f && badStreak >= 2)
                countdownStart = -1f;
            if (autoCapture && countdownStart < 0f && goodStreak >= 3)
            {
                countdownStart = Time.unscaledTime;
                countdown.text = "3";
            }
            Refresh();
        }

        void StopAnalysis()
        {
            if (!analyzing)
                return;
            if (analysis != null)
                StopCoroutine(analysis);
            analysis = null;
            analyzing = false;
            scanner.ResetBuffers();
        }

        // ------------------------------------------------------------------ Suratga olish va tekshirish

        void Capture()
        {
            countdownStart = -1f;
            if (mode != Mode.Camera || scanner.State != FaceScanner.CameraState.Live)
                return;
            Texture2D photo = null;
            try { photo = scanner.Capture(); }
            catch (Exception e) { Debug.LogWarning("[CraDev] Kamera kadrini olib bo'lmadi: " + e.Message); }
            StopCamera();
            if (photo == null)
            {
                message = Loc.T("face.error");
                mode = Mode.Rejected;
                Refresh();
                return;
            }
            Process(photo, true);
        }

        void Process(Texture2D photo, bool mirrored)
        {
            mode = Mode.Processing;
            pending = photo;
            preview.texture = photo;
            // Kameradan olingan rasm ham ko'zgudagidek ko'rsatiladi (jonli tasvir bilan bir xil)
            preview.uvRect = mirrored ? new Rect(1f, 0f, -1f, 1f) : new Rect(0f, 0f, 1f, 1f);
            preview.rectTransform.localEulerAngles = Vector3.zero;
            if (previewFitter != null)
                previewFitter.aspectRatio = (float)photo.width / Mathf.Max(1, photo.height);
            Refresh();
            processing = StartCoroutine(ProcessRoutine(photo));
        }

        IEnumerator ProcessRoutine(Texture2D photo)
        {
            yield return null; // "Yuz aniqlanmoqda" ekranga chiqib olsin
            var job = new FaceJob();
            Color32[] pixels = null;
            FaceTracker tracker = null;
            try
            {
                tracker = scanner.Tracker;
                pixels = photo.GetPixels32();
            }
            catch (Exception e)
            {
                Debug.LogError("[CraDev] Yuzni aniqlashga tayyorlab bo'lmadi: " + e);
                job.Error = Loc.T("face.error");
            }
            if (tracker != null && pixels != null)
                yield return tracker.TrackAsync(pixels, photo.width, photo.height, job);

            processing = null;
            pending = null;
            if (job.Found)
            {
                candidate = new FaceData { Photo = photo, Landmarks = job.Landmarks };
                mode = Mode.Review;
                Refresh();
                Preview?.Invoke(candidate);
                Debug.Log("[CraDev] Yuz topildi: natija qahramonda ko'rsatildi.");
            }
            else
            {
                rejected = photo;
                message = job.Error ?? Loc.T("face.not_found");
                mode = Mode.Rejected;
                Refresh();
            }
        }

        // ------------------------------------------------------------------ Tugmalar

        void Primary()
        {
            Deselect();
            switch (mode)
            {
                case Mode.Camera:
                    if (Broken)
                        StartCamera(false);
                    else if (CanCapture)
                        Capture();
                    break;
                case Mode.Review:
                    Use();
                    break;
                case Mode.Rejected:
                    Again();
                    break;
            }
        }

        void Secondary()
        {
            Deselect();
            switch (mode)
            {
                case Mode.Camera:
                case Mode.Rejected:
                    UploadInstead();
                    break;
                case Mode.Review:
                    Again();
                    break;
            }
        }

        void Use()
        {
            if (mode != Mode.Review || candidate == null)
                return;
            var face = candidate;
            candidate = null;
            Hide();
            Used?.Invoke(face);
            Closed?.Invoke();
        }

        /// <summary>Qayta olish (kamera) yoki boshqa rasm tanlash (yuklash).</summary>
        void Again()
        {
            if (fromCamera)
            {
                ClearResult();
                StartCamera(false);
                return;
            }
            // Yangi rasm tanlanmasa hozirgi natija qoladi
            var photo = PickPhoto();
            if (photo == null)
                return;
            ClearResult();
            Process(photo, false);
        }

        void UploadInstead()
        {
            if (!FacePhoto.CanPickFile)
                return;
            var photo = PickPhoto();
            if (photo == null)
                return;
            ClearResult();
            StopCamera();
            fromCamera = false;
            Process(photo, false);
        }

        Texture2D PickPhoto()
        {
            string path = FacePhoto.PickFile();
            if (string.IsNullOrEmpty(path))
                return null;
            Texture2D photo = null;
            try { photo = FacePhoto.Load(path); }
            catch (Exception e) { Debug.LogWarning("[CraDev] Rasmni o'qib bo'lmadi: " + e.Message); }
            if (photo == null)
            {
                notice = Loc.T("face.bad_file");
                noticeUntil = Time.unscaledTime + 4f;
                Refresh();
            }
            return photo;
        }

        /// <summary>Vaqtinchalik yuz va rad etilgan rasm tozalanadi (egasi avval o'z yuzini qaytaradi).</summary>
        void ClearResult()
        {
            if (candidate != null)
            {
                Preview?.Invoke(null);
                DiscardCandidate();
            }
            DiscardPhoto(ref rejected);
        }

        void DiscardCandidate()
        {
            if (candidate == null)
                return;
            var photo = candidate.Photo;
            candidate = null;
            DiscardPhoto(ref photo);
        }

        void DiscardPhoto(ref Texture2D photo)
        {
            if (photo != null)
            {
                if (preview != null && preview.texture == photo)
                    preview.texture = null;
                Destroy(photo);
            }
            photo = null;
        }

        static void Deselect()
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        // ------------------------------------------------------------------ Ko'rinish

        bool Broken => mode == Mode.Camera && (trackerFailed || scanner.State == FaceScanner.CameraState.Failed ||
                                               scanner.State == FaceScanner.CameraState.NoDevice);

        bool CanCapture => mode == Mode.Camera && scanner.State == FaceScanner.CameraState.Live && scanner.TrackerReady &&
                           lastHint == ScanHint.Good;

        static string HintKey(ScanHint hint)
        {
            switch (hint)
            {
                case ScanHint.Closer: return "face.hint_closer";
                case ScanHint.Back: return "face.hint_back";
                case ScanHint.Center: return "face.hint_center";
                case ScanHint.Straight: return "face.hint_straight";
                case ScanHint.Light: return "face.hint_light";
                case ScanHint.Still: return "face.hint_still";
                case ScanHint.Good: return "face.hint_good";
                default: return "face.hint_find";
            }
        }

        void Refresh()
        {
            if (mode == Mode.Closed || scanner == null)
                return;
            var state = scanner.State;
            bool camera = mode == Mode.Camera;
            bool broken = Broken;
            bool starting = camera && state == FaceScanner.CameraState.Starting;
            bool live = camera && state == FaceScanner.CameraState.Live;
            bool preparing = live && !scanner.TrackerReady;
            bool counting = live && countdownStart >= 0f;

            oval.enabled = camera && !broken;
            oval.color = !live || preparing ? new Color(1f, 1f, 1f, 0.45f)
                : lastHint == ScanHint.Good ? Ok
                : lastHint == ScanHint.Find ? new Color(1f, 1f, 1f, 0.8f)
                : Warn;
            countdown.enabled = counting;
            if (spinner != null)
                spinner.enabled = starting || preparing || mode == Mode.Processing;
            failure.SetActive(broken);
            if (broken)
                failureText.text = trackerFailed ? Loc.T("face.error")
                    : state == FaceScanner.CameraState.NoDevice ? Loc.T("face.no_camera")
                    : scanner.Lost ? Loc.T("face.camera_lost")
                    : Loc.T("face.camera_failed");

            string text;
            Color color = Muted;
            if (notice != null) { text = notice; color = Bad; }
            else if (mode == Mode.Processing) text = Loc.T("face.finding");
            else if (mode == Mode.Review) { text = Loc.T("face.review"); color = Ok; }
            else if (mode == Mode.Rejected) { text = message; color = Bad; }
            else if (broken) text = "";
            else if (starting) text = Loc.T("face.camera_starting");
            else if (preparing) text = Loc.T("face.preparing");
            else if (counting) { text = Loc.T("face.hint_countdown"); color = Ok; }
            else
            {
                text = Loc.T(HintKey(lastHint));
                color = lastHint == ScanHint.Good ? Ok : lastHint == ScanHint.Find ? Muted : Warn;
            }
            hint.text = text;
            hint.color = color;

            bool upload = FacePhoto.CanPickFile;
            string again = fromCamera ? "face.retake" : "face.choose_other";
            switch (mode)
            {
                case Mode.Camera:
                    SetButton(primaryButton, primaryLabel, broken ? "face.retry" : "face.capture", broken || CanCapture);
                    SetButton(secondaryButton, secondaryLabel, "face.upload", true, upload);
                    break;
                case Mode.Processing:
                    SetButton(primaryButton, primaryLabel, "face.use", false);
                    SetButton(secondaryButton, secondaryLabel, again, false);
                    break;
                case Mode.Review:
                    SetButton(primaryButton, primaryLabel, "face.use", true);
                    SetButton(secondaryButton, secondaryLabel, again, true);
                    break;
                case Mode.Rejected:
                    SetButton(primaryButton, primaryLabel, again, true);
                    SetButton(secondaryButton, secondaryLabel, "face.upload", true, upload && fromCamera);
                    break;
            }

            if (switchButton != null)
            {
                bool several = camera && scanner.DeviceCount > 1;
                switchButton.gameObject.SetActive(several);
                if (several && switchLabel != null)
                    switchLabel.text = Loc.F("face.switch_camera", scanner.DeviceName);
            }
        }

        static void SetButton(Button button, Text label, string key, bool interactable, bool visible = true)
        {
            button.gameObject.SetActive(visible);
            button.interactable = interactable;
            if (label != null)
                label.text = Loc.T(key);
        }
    }
}

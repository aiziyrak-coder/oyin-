using System.Collections;
using CraDev.Face;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev.CharacterCreation
{
    /// <summary>
    /// Formadagi "FACE" bo'limi: o'yinchi kamera bilan suratga tushadi yoki kompyuterdan rasm yuklaydi.
    /// Rasmdagi yuz topiladi (FaceTracker) va 3D qahramon yuziga qo'yiladi (AvatarViewer.SetFace).
    /// Yuz shu kompyuterda saqlanadi (FaceStore) va keyingi safar o'zi qo'yiladi.
    /// </summary>
    public class FaceCapture : MonoBehaviour
    {
        [Header("Modellar (Assets/CraDev/Face/Models)")]
        [SerializeField] ModelAsset detectorModel;
        [SerializeField] ModelAsset landmarkModel;
        [SerializeField] AvatarViewer viewer;

        [Header("Forma")]
        [SerializeField] Button takeButton;
        [SerializeField] Button uploadButton;
        [SerializeField] RawImage thumb;
        [SerializeField] Image thumbIcon;
        [SerializeField] Button removeButton;
        [SerializeField] Image statusIcon;
        [SerializeField] Text statusText;

        [Header("Kamera oynasi")]
        [SerializeField] GameObject modal;
        [SerializeField] RawImage preview;
        [SerializeField] AspectRatioFitter previewFitter;
        [SerializeField] Button captureButton;
        [SerializeField] Button cancelButton;
        [SerializeField] Text modalStatus;

        [Header("Ikonkalar")]
        [SerializeField] Sprite checkSprite;
        [SerializeField] Sprite alertSprite;
        [SerializeField] Sprite spinnerSprite;

        const string HintDefault = "Look straight at the camera, good light, no glasses.";

        static readonly Color Ok = new Color32(34, 197, 94, 255);
        static readonly Color Bad = new Color32(240, 82, 82, 255);
        static readonly Color Muted = new Color32(142, 147, 154, 255);

        FaceTracker tracker;
        WebCamTexture webcam;
        FaceData face;
        bool busy;
        bool locked;

        void Start()
        {
            takeButton.onClick.AddListener(OpenCamera);
            uploadButton.onClick.AddListener(Upload);
            removeButton.onClick.AddListener(Remove);
            captureButton.onClick.AddListener(Capture);
            cancelButton.onClick.AddListener(CloseCamera);
            uploadButton.gameObject.SetActive(FacePhoto.CanPickFile);
            modal.SetActive(false);

            var saved = FaceStore.Load();
            if (saved != null)
                SetFace(saved, focus: false, save: false);
            else
                ShowFace(null);
            SetStatus(saved != null ? "Your face is on the avatar." : HintDefault, saved != null ? Ok : Muted, saved != null ? checkSprite : null);
        }

        void Update()
        {
            if (busy && statusIcon.enabled && statusIcon.sprite == spinnerSprite)
                statusIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -Time.unscaledTime * 450f);

            // Kamera kadri kelguncha o'lchami noma'lum: kelgach oynani unga moslaymiz
            if (webcam != null && webcam.width > 16 && previewFitter != null)
                previewFitter.aspectRatio = (float)webcam.width / webcam.height;
        }

        void OnDestroy()
        {
            StopCamera();
            tracker?.Dispose();
        }

        /// <summary>Profil yaratilgach yuzni o'zgartirib bo'lmaydi.</summary>
        public void SetLocked(bool value)
        {
            locked = value;
            UpdateButtons();
        }

        public bool HasFace => face != null;

        // ------------------------------------------------------------------ Kamera

        void OpenCamera()
        {
            if (busy || locked)
                return;
            if (WebCamTexture.devices.Length == 0)
            {
                SetStatus(FacePhoto.CanPickFile ? "No webcam found. Upload a photo instead." : "No webcam found.", Bad, alertSprite);
                return;
            }
            // Old kamera bo'lsa o'sha (noutbuklarda odatda bitta)
            string device = WebCamTexture.devices[0].name;
            foreach (var d in WebCamTexture.devices)
                if (d.isFrontFacing) { device = d.name; break; }

            webcam = new WebCamTexture(device, 1280, 720, 30);
            webcam.Play();
            preview.texture = webcam;
            preview.uvRect = new Rect(1f, 0f, -1f, 1f); // ko'zgudagidek: o'yinchi o'zini tabiiy ko'radi
            modalStatus.text = "Fit your face inside the oval and look straight.";
            modalStatus.color = Muted;
            captureButton.interactable = true;
            modal.SetActive(true);
        }

        void Capture()
        {
            if (webcam == null || !webcam.isPlaying || webcam.width <= 16)
            {
                modalStatus.text = "The camera is starting…";
                return;
            }
            var photo = FacePhoto.Capture(webcam);
            CloseCamera();
            StartCoroutine(Process(photo));
        }

        void CloseCamera()
        {
            StopCamera();
            modal.SetActive(false);
        }

        void StopCamera()
        {
            if (webcam == null)
                return;
            webcam.Stop();
            Destroy(webcam);
            webcam = null;
            if (preview != null)
                preview.texture = null;
        }

        // ------------------------------------------------------------------ Rasm yuklash

        void Upload()
        {
            if (busy || locked)
                return;
            string path = FacePhoto.PickFile();
            if (string.IsNullOrEmpty(path))
                return;
            Texture2D photo = null;
            try
            {
                photo = FacePhoto.Load(path);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[CraDev] Rasmni o'qib bo'lmadi: " + e.Message);
            }
            if (photo == null)
            {
                SetStatus("Can't open this file. Use a JPG or PNG photo.", Bad, alertSprite);
                return;
            }
            StartCoroutine(Process(photo));
        }

        // ------------------------------------------------------------------ Yuzni topish va qo'yish

        IEnumerator Process(Texture2D photo)
        {
            busy = true;
            UpdateButtons();
            SetStatus("Finding your face…", Muted, spinnerSprite);
            yield return null; // yozuv ekranga chiqib olsin

            bool found = false;
            Vector2[] landmarks = null;
            string error = null;
            try
            {
                tracker ??= new FaceTracker(detectorModel, landmarkModel);
                found = tracker.Track(photo.GetPixels32(), photo.width, photo.height, out landmarks, out error);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[CraDev] Yuzni aniqlashda xato: " + e);
                error = "Something went wrong. Try another photo.";
            }

            busy = false;
            statusIcon.rectTransform.localRotation = Quaternion.identity;
            if (!found)
            {
                Destroy(photo);
                SetStatus(error, Bad, alertSprite);
                UpdateButtons();
                yield break;
            }

            SetFace(new FaceData { Photo = photo, Landmarks = landmarks }, focus: true, save: true);
            SetStatus("Face added. Scroll to zoom in or out.", Ok, checkSprite);
            Debug.Log("[CraDev] Yuz topildi va avatarga qo'yildi.");
        }

        void SetFace(FaceData data, bool focus, bool save)
        {
            if (face != null && face != data && face.Photo != null)
                Destroy(face.Photo);
            face = data;
            viewer.SetFace(face);
            if (focus)
                viewer.FocusFace();
            if (save)
            {
                try { FaceStore.Save(face); }
                catch (System.Exception e) { Debug.LogWarning("[CraDev] Yuzni saqlab bo'lmadi: " + e.Message); }
            }
            ShowFace(face);
            UpdateButtons();
        }

        void Remove()
        {
            if (busy || locked || face == null)
                return;
            SetFace(null, focus: false, save: false);
            FaceStore.Delete();
            SetStatus(HintDefault, Muted, null);
        }

        // ------------------------------------------------------------------ Ko'rinish

        /// <summary>Kichik rasm: yuz joylashgan kvadrat qismi.</summary>
        void ShowFace(FaceData data)
        {
            bool has = data != null;
            thumb.enabled = has;
            thumbIcon.enabled = !has;
            removeButton.gameObject.SetActive(has);
            if (!has)
            {
                thumb.texture = null;
                return;
            }
            Vector2 min = data.Landmarks[0], max = data.Landmarks[0];
            for (int i = 1; i < FaceTracker.MeshLandmarkCount; i++)
            {
                min = Vector2.Min(min, data.Landmarks[i]);
                max = Vector2.Max(max, data.Landmarks[i]);
            }
            float w = data.Photo.width, h = data.Photo.height;
            float side = Mathf.Max((max.x - min.x) * w, (max.y - min.y) * h) * 1.15f;
            Vector2 center = (min + max) / 2f;
            thumb.texture = data.Photo;
            thumb.uvRect = new Rect(center.x - side / 2f / w, center.y - side / 2f / h, side / w, side / h);
        }

        void UpdateButtons()
        {
            bool enabled = !busy && !locked;
            takeButton.interactable = enabled;
            uploadButton.interactable = enabled;
            removeButton.interactable = enabled;
        }

        void SetStatus(string text, Color color, Sprite icon)
        {
            statusText.text = text;
            statusText.color = color;
            statusIcon.enabled = icon != null;
            if (icon != null)
            {
                statusIcon.sprite = icon;
                statusIcon.color = color;
            }
            var rect = statusText.rectTransform;
            rect.anchoredPosition = new Vector2(icon != null ? 24f : 0f, rect.anchoredPosition.y);
        }
    }
}

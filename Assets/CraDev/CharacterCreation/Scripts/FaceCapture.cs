using CraDev.Face;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraDev.CharacterCreation
{
    /// <summary>
    /// Formadagi "FACE" bo'limi: o'yinchi jonli skaner (kamera) bilan suratga tushadi yoki kompyuterdan rasm
    /// yuklaydi. Skaner oynasi - <see cref="FaceScanView"/> (lobby studiyasi bilan umumiy): u yuzni topib,
    /// natijani qahramonda ko'rsatadi, o'yinchi "Ishlatish"ni bossa yuz shu yerga qoralama bo'lib keladi.
    ///
    /// Qoralama darhol saqlanmaydi: yangi o'yinchida "Yaratish", tahrirlashda "Saqlash" bosilganda
    /// <see cref="Commit"/> uni shu kompyuterga yozadi (FaceStore). "Bekor qilish" <see cref="Revert"/> bilan
    /// saqlangan yuzni qaytaradi.
    /// </summary>
    public class FaceCapture : MonoBehaviour
    {
        [SerializeField] AvatarViewer viewer;
        [SerializeField] FaceScanView scanner;

        [Header("Forma")]
        [SerializeField] Button takeButton;
        [SerializeField] Button uploadButton;
        [SerializeField] RawImage thumb;
        [SerializeField] Image thumbIcon;
        [SerializeField] Button removeButton;
        [SerializeField] Image statusIcon;
        [SerializeField] Text statusText;

        [Header("Ikonkalar")]
        [SerializeField] Sprite checkSprite;
        [SerializeField] Sprite alertSprite;

        static readonly Color Ok = new Color32(34, 197, 94, 255);
        static readonly Color Bad = new Color32(240, 82, 82, 255);
        static readonly Color Muted = new Color32(142, 147, 154, 255);

        FaceData saved;  // FaceStore'dagi yuz
        FaceData draft;  // hozir qahramonda (saqlanmagan bo'lishi mumkin)
        bool locked;
        string error;

        void Start()
        {
            if (scanner == null)
            {
                // Eski sahna (builder yangi skaner oynasini hali qurmagan): yuz tugmalari o'chiriladi
                Debug.LogWarning("[CraDev] FaceCapture: skaner oynasi yo'q. Sahnalarni qayta yarating (CraDev > Sahnalarni yaratish).");
                takeButton.gameObject.SetActive(false);
                uploadButton.gameObject.SetActive(false);
                removeButton.gameObject.SetActive(false);
                enabled = false;
                return;
            }
            takeButton.onClick.AddListener(OpenCamera);
            uploadButton.onClick.AddListener(Upload);
            removeButton.onClick.AddListener(Remove);
            uploadButton.gameObject.SetActive(FacePhoto.CanPickFile);
            scanner.Preview += face =>
            {
                viewer.SetFace(face ?? draft);
                if (face != null)
                    viewer.FocusFace();
            };
            scanner.Used += SetDraft;
            scanner.Closed += () => { UpdateButtons(); ShowStatus(); };

            saved = draft = FaceStore.Load();
            viewer.SetFace(draft);
            ShowFace(draft);
            ShowStatus();
            UpdateButtons();
        }

        void OnEnable() => Loc.Changed += ShowStatus;

        void OnDisable() => Loc.Changed -= ShowStatus;

        void OnDestroy()
        {
            // Saqlanmagan qoralama rasmi bo'shatiladi
            if (draft != null && draft != saved && draft.Photo != null)
                Destroy(draft.Photo);
        }

        /// <summary>Profil yaratilgach yuzni o'zgartirib bo'lmaydi.</summary>
        public void SetLocked(bool value)
        {
            locked = value;
            if (locked && CameraOpen)
                scanner.Close();
            UpdateButtons();
        }

        public bool HasFace => draft != null;

        /// <summary>Qoralama yuz saqlangan yuzdan farq qiladimi (yangi, boshqa yoki olib tashlangan).</summary>
        public bool Dirty => draft != saved;

        public bool CameraOpen => scanner != null && scanner.IsOpen;

        /// <summary>Skaner yuzni aniqlayapti.</summary>
        public bool Busy => scanner != null && scanner.Busy;

        /// <summary>Skaner ochiq yoki hozirgina yopildi: Enter/Esc orqadagi formaga o'tmasin.</summary>
        public bool BlocksKeys => scanner != null && scanner.BlocksKeys;

        public void CloseCamera() { if (scanner != null) scanner.Close(); }

        // ------------------------------------------------------------------ Qoralama

        /// <summary>Qoralamani shu kompyuterga yozadi. Xato bo'lsa false (qoralama saqlanmay qoladi).</summary>
        public bool Commit()
        {
            if (!Dirty)
                return true;
            try
            {
                if (draft != null)
                    FaceStore.Save(draft);
                else
                    FaceStore.Delete();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[CraDev] Yuzni saqlab bo'lmadi: " + e.Message);
                error = Loc.T("face.save_failed");
                ShowStatus();
                return false;
            }
            if (saved != null && saved != draft && saved.Photo != null)
                Destroy(saved.Photo);
            saved = draft;
            error = null;
            ShowStatus();
            return true;
        }

        /// <summary>Bekor qilish: saqlangan yuz qahramonga qaytadi, qoralama rasmi bo'shatiladi.</summary>
        public void Revert()
        {
            if (CameraOpen)
                scanner.Close();
            if (!Dirty)
                return;
            var previous = draft;
            draft = saved;
            viewer.SetFace(draft);
            if (previous != null && previous.Photo != null)
                Destroy(previous.Photo);
            ShowFace(draft);
            ShowStatus();
            UpdateButtons();
        }

        void SetDraft(FaceData face)
        {
            var previous = draft;
            draft = face;
            error = null;
            viewer.SetFace(draft);
            if (draft != null)
                viewer.FocusFace();
            if (previous != null && previous != face && previous != saved && previous.Photo != null)
                Destroy(previous.Photo);
            ShowFace(draft);
            ShowStatus();
            UpdateButtons();
        }

        // ------------------------------------------------------------------ Tugmalar

        void OpenCamera()
        {
            Deselect();
            if (locked || CameraOpen)
                return;
            error = null;
            scanner.OpenCamera();
            UpdateButtons();
        }

        void Upload()
        {
            Deselect();
            if (locked || CameraOpen)
                return;
            string path = FacePhoto.PickFile();
            if (string.IsNullOrEmpty(path))
                return;
            Texture2D photo = null;
            try { photo = FacePhoto.Load(path); }
            catch (System.Exception e) { Debug.LogWarning("[CraDev] Rasmni o'qib bo'lmadi: " + e.Message); }
            if (photo == null)
            {
                error = Loc.T("face.bad_file");
                ShowStatus();
                return;
            }
            error = null;
            scanner.OpenPhoto(photo);
            UpdateButtons();
        }

        void Remove()
        {
            Deselect();
            if (locked || CameraOpen || draft == null)
                return;
            SetDraft(null);
        }

        static void Deselect()
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        // ------------------------------------------------------------------ Ko'rinish

        /// <summary>Kichik rasm: yuz joylashgan kvadrat qismi.</summary>
        void ShowFace(FaceData data)
        {
            bool has = data != null && data.Photo != null;
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

        void ShowStatus()
        {
            if (error != null)
                SetStatus(error, Bad, alertSprite);
            else if (draft == null)
                SetStatus(Loc.T(saved != null ? "studio.face_removed" : "face.hint"), Muted, null);
            else if (Dirty)
                SetStatus(Loc.T("face.draft"), Ok, checkSprite);
            else
                SetStatus(Loc.T("face.on_avatar"), Ok, checkSprite);
        }

        void UpdateButtons()
        {
            bool enabled = !locked && !CameraOpen;
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
            statusIcon.rectTransform.localRotation = Quaternion.identity;
            var rect = statusText.rectTransform;
            rect.anchoredPosition = new Vector2(icon != null ? 24f : 0f, rect.anchoredPosition.y);
        }
    }
}

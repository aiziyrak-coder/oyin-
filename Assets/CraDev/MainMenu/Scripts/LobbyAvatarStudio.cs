using System;
using System.Collections.Generic;
using CraDev.CharacterCreation;
using CraDev.Face;
using CraDev.Online;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    /// <summary>
    /// Lobby ichidagi avatar studiyasi: sahna almashmaydi, shuning uchun party signali to'xtamaydi.
    ///
    /// O'ngda panel: QAHRAMON (pasportdagi jinsga mos avatar kartalari), YUZ (jonli skaner, rasm yuklash,
    /// olib tashlash), pastda Bekor qilish / Saqlash. Chapda lobbydagi 3D qahramon jonli ko'rinish bo'ladi:
    /// kamera unga yaqinlashadi (LobbyStage), sichqoncha bilan aylantiriladi, g'ildirak yoki "Butun bo'y / Yuz"
    /// tugmalari bilan yaqinlashtiriladi.
    ///
    /// Hamma o'zgarish qoralama: qahramonda darhol ko'rinadi, lekin faqat "Saqlash"da yoziladi - avval serverga
    /// avatar (GameApi.UpdateAvatar), muvaffaqiyatli bo'lsa yuz shu kompyuterga (FaceStore). Avatar saqlanmasa
    /// yuzga tegilmaydi, qoralama qoladi. Bekor qilinsa (o'zgarish bo'lsa - so'raladi) hammasi saqlangan holatga qaytadi.
    /// </summary>
    public class LobbyAvatarStudio : MonoBehaviour
    {
        [SerializeField] MainMenuScreen lobby;

        [Header("Panel")]
        [SerializeField] RectTransform panel;
        [SerializeField] CanvasGroup panelGroup;
        [Tooltip("Qahramon va yuz bo'limi (skaner ochilganda yashiriladi).")]
        [SerializeField] GameObject editorPage;
        [SerializeField] FaceScanView scanner;
        [SerializeField] Button closeButton;

        [Header("Qahramon")]
        [SerializeField] Button[] cards;
        [SerializeField] Image[] cardPictures;
        [SerializeField] GameObject[] cardTicks;
        [SerializeField] Text avatarInfo;

        [Header("Yuz")]
        [SerializeField] RawImage faceThumb;
        [SerializeField] Image faceIcon;
        [SerializeField] Text faceStatus;
        [SerializeField] Button scanButton;
        [SerializeField] Button uploadButton;
        [SerializeField] Button removeButton;

        [Header("Ko'rinish (qahramon ostida)")]
        [SerializeField] CanvasGroup viewGroup;
        [SerializeField] RectTransform viewDock;
        [Tooltip("Butun bo'y, Yuz.")]
        [SerializeField] Button[] viewButtons;

        [Header("Pastki qism")]
        [SerializeField] Text errorText;
        [SerializeField] Button cancelButton;
        [SerializeField] Button saveButton;
        [SerializeField] Text saveLabel;

        [Header("Fon")]
        [Tooltip("Fon rasmi ustidagi qoraytirish (fon kamerasi canvasida: qahramon undan oldinda turadi).")]
        [SerializeField] Graphic backdrop;
        [SerializeField, Range(0f, 1f)] float backdropAlpha = 0.84f;
        [SerializeField] float slide = 64f;

        static readonly Color Ok = new Color32(52, 211, 120, 255);
        static readonly Color Bad = new Color32(240, 92, 92, 255);
        static readonly Color Muted = new Color32(178, 174, 169, 255);

        AvatarOption[] choices = Array.Empty<AvatarOption>();
        string draftAvatar;
        FaceData draftFace;
        bool open, saving;
        float shown;
        float panelX;
        int view;
        readonly Vector3[] corners = new Vector3[4];

        public bool IsOpen => open;
        public bool Saving => saving;
        public bool ScanOpen => scanner != null && scanner.IsOpen;
        public FaceScanView Scanner => scanner;
        public string DraftAvatar => draftAvatar;
        public FaceData DraftFace => draftFace;
        public IReadOnlyList<AvatarOption> Choices => choices;

        /// <summary>Saqlanmagan o'zgarish bormi (avatar yoki yuz).</summary>
        public bool Dirty => open && (draftAvatar != PlayerProfile.AvatarId || draftFace != lobby.PlayerFace);

        /// <summary>Qahramon ekranning qayerida turadi (kenglikka nisbatan): panel chap tomonidagi bo'sh joy o'rtasi.</summary>
        public float ScreenFraction
        {
            get
            {
                var root = (RectTransform)transform;
                float width = root.rect.width;
                if (width <= 0f)
                    return 0.38f;
                panel.GetWorldCorners(corners);
                float left = root.InverseTransformPoint(corners[0]).x - root.rect.xMin - (panel.anchoredPosition.x - panelX);
                return Mathf.Clamp(left / width * 0.5f, 0.25f, 0.5f);
            }
        }

        void Awake()
        {
            panelX = panel.anchoredPosition.x;
            closeButton.onClick.AddListener(Cancel);
            cancelButton.onClick.AddListener(Cancel);
            saveButton.onClick.AddListener(Save);
            scanButton.onClick.AddListener(() => { Deselect(); OpenScanner(null); });
            uploadButton.onClick.AddListener(Upload);
            removeButton.onClick.AddListener(RemoveFace);
            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                cards[i].onClick.AddListener(() => { Deselect(); if (index < choices.Length) SelectAvatar(choices[index]); });
            }
            for (int i = 0; i < viewButtons.Length; i++)
            {
                int index = i;
                viewButtons[i].onClick.AddListener(() => { Deselect(); SetView(index); });
            }
            uploadButton.gameObject.SetActive(FacePhoto.CanPickFile);
            scanner.Preview += face => lobby.Viewer.SetFace(face ?? draftFace);
            scanner.Used += SetDraftFace;
            scanner.Closed += () => { editorPage.SetActive(true); Refresh(); };
            Apply(0f);
            panel.gameObject.SetActive(false);
            viewGroup.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            Loc.Changed += Refresh;
            if (lobby != null && lobby.Viewer != null)
                lobby.Viewer.Scrolled += OnScrolled;
        }

        void OnDisable()
        {
            Loc.Changed -= Refresh;
            if (lobby != null && lobby.Viewer != null)
                lobby.Viewer.Scrolled -= OnScrolled;
        }

        // ------------------------------------------------------------------ Ochish / yopish

        /// <summary>MainMenuScreen.SetAvatarStudio(true) chaqiradi: qoralama saqlangan holatdan boshlanadi.</summary>
        public void Open()
        {
            if (open)
                return;
            open = true;
            saving = false;
            var current = lobby.Viewer.CurrentOption;
            draftAvatar = lobby.FindAvatar(PlayerProfile.AvatarId) != null ? PlayerProfile.AvatarId : current?.id;
            draftFace = lobby.PlayerFace;
            SetError(null);
            BuildChoices();
            editorPage.SetActive(true);
            panel.gameObject.SetActive(true);
            viewGroup.gameObject.SetActive(true);
            panelGroup.blocksRaycasts = panelGroup.interactable = true;
            viewGroup.blocksRaycasts = viewGroup.interactable = true;
            SetView(0);
            Refresh();
            Deselect();
        }

        /// <summary>
        /// MainMenuScreen.SetAvatarStudio(false) chaqiradi: saqlanmagan qoralama bekor qilinadi - qahramon saqlangan
        /// avatar va yuzga qaytadi, qoralama rasmi bo'shatiladi.
        /// </summary>
        public void Close()
        {
            if (!open)
                return;
            open = false;
            saving = false;
            panelGroup.blocksRaycasts = panelGroup.interactable = false;
            viewGroup.blocksRaycasts = viewGroup.interactable = false;
            scanner.Close();
            var viewer = lobby.Viewer;
            var saved = lobby.FindAvatar(PlayerProfile.AvatarId);
            if (saved != null && viewer.CurrentOption != saved)
                viewer.SetAvatar(saved);
            if (viewer.Face != lobby.PlayerFace)
                viewer.SetFace(lobby.PlayerFace);
            // Saqlanmagan qoralama yuz rasmi bo'shatiladi (saqlangani emas)
            if (draftFace != null && draftFace != lobby.PlayerFace && draftFace.Photo != null)
                Destroy(draftFace.Photo);
            draftFace = lobby.PlayerFace;
            scanner.Release(); // kamera va yuz modellari xotiradan bo'shatiladi
            Deselect();
        }

        /// <summary>Esc: skaner ochiq bo'lsa - uni yopadi, aks holda bekor qilish (o'zgarish bo'lsa so'raladi).</summary>
        public void Back()
        {
            if (saving)
                return;
            if (ScanOpen)
                scanner.Close();
            else
                Cancel();
        }

        void Cancel()
        {
            Deselect();
            if (saving)
                return;
            if (ScanOpen)
                scanner.Close();
            if (!Dirty)
            {
                lobby.SetAvatarStudio(false);
                return;
            }
            lobby.Dialog.Show(Loc.T("studio.discard_title"), Loc.T("studio.discard_message"), Loc.T("studio.discard"),
                () => lobby.SetAvatarStudio(false), Loc.T("studio.keep_editing"));
        }

        // ------------------------------------------------------------------ Qahramon

        void BuildChoices()
        {
            // Jins pasportdan: faqat o'sha jinsdagi avatarlar (kartalar builder chizgan)
            string gender = PlayerProfile.Gender;
            var current = lobby.FindAvatar(draftAvatar);
            if (string.IsNullOrEmpty(gender) && current != null)
                gender = current.gender;
            var list = new List<AvatarOption>();
            foreach (var option in lobby.Avatars)
                if (option != null && option.model != null && option.gender == gender)
                    list.Add(option);
            choices = list.ToArray();
            for (int i = 0; i < cards.Length; i++)
            {
                bool used = i < choices.Length;
                cards[i].gameObject.SetActive(used);
                if (!used)
                    continue;
                cardPictures[i].sprite = choices[i].card;
                cardPictures[i].preserveAspect = true;
                cardPictures[i].enabled = choices[i].card != null;
            }
        }

        /// <summary>Qoralama avatar: qahramon darhol almashadi (yuz va kiyim yangisiga ham qo'yiladi).</summary>
        public void SelectAvatar(AvatarOption option)
        {
            if (option == null || saving || !open)
                return;
            draftAvatar = option.id;
            if (lobby.Viewer.CurrentOption != option)
            {
                lobby.Viewer.SetAvatar(option);
                lobby.Stage.SetStudioFocus(lobby.Viewer.BodyHeight, ScreenFraction);
            }
            SetError(null);
            Refresh();
        }

        // ------------------------------------------------------------------ Yuz

        void OpenScanner(Texture2D photo)
        {
            if (saving)
                return;
            editorPage.SetActive(false);
            SetView(1); // natija yuzda yaxshi ko'rinadi
            if (photo != null)
                scanner.OpenPhoto(photo);
            else
                scanner.OpenCamera();
        }

        void Upload()
        {
            Deselect();
            if (saving)
                return;
            string path = FacePhoto.PickFile();
            if (string.IsNullOrEmpty(path))
                return;
            Texture2D photo = null;
            try { photo = FacePhoto.Load(path); }
            catch (Exception e) { Debug.LogWarning("[CraDev] Rasmni o'qib bo'lmadi: " + e.Message); }
            if (photo == null)
            {
                SetError(Loc.T("face.bad_file"));
                return;
            }
            SetError(null);
            OpenScanner(photo);
        }

        void RemoveFace()
        {
            Deselect();
            if (saving || draftFace == null)
                return;
            SetDraftFace(null);
        }

        /// <summary>Qoralama yuz: qahramonga qo'yiladi. Oldingi saqlanmagan rasm bo'shatiladi (saqlangani emas).</summary>
        void SetDraftFace(FaceData face)
        {
            var previous = draftFace;
            draftFace = face;
            lobby.Viewer.SetFace(face);
            if (previous != null && previous != face && previous != lobby.PlayerFace && previous.Photo != null)
                Destroy(previous.Photo);
            SetError(null);
            Refresh();
        }

        // ------------------------------------------------------------------ Saqlash

        void Save()
        {
            Deselect();
            if (!open || saving || ScanOpen || !Dirty)
                return;
            SetError(null);
            if (draftAvatar == PlayerProfile.AvatarId)
            {
                CommitFaceAndClose();
                return;
            }
            saving = true;
            Refresh();
            string requested = draftAvatar;
            StartCoroutine(lobby.Api.UpdateAvatar(PlayerProfile.Token, requested, result =>
            {
                saving = false;
                bool ok = !result.NetworkError && result.Status == 200;
                if (ok)
                    PlayerProfile.SetAvatar(requested);
                if (!open)
                {
                    // Studiya javobdan oldin yopilgan: server saqlagan avatar baribir qahramonga qo'yiladi
                    if (ok && lobby.FindAvatar(requested) is AvatarOption option && lobby.Viewer.CurrentOption != option)
                        lobby.Viewer.SetAvatar(option);
                    if (ok)
                        lobby.RefreshProfileVisuals();
                    return;
                }
                if (ok)
                {
                    lobby.RefreshProfileVisuals();
                    CommitFaceAndClose();
                    return;
                }
                // Avatar saqlanmadi: yuzga ham tegilmaydi, qoralama qoladi - qayta urinish mumkin
                SetError(AvatarError(result));
                Refresh();
            }));
        }

        static string AvatarError(ApiResult<PlayerResponse> result)
        {
            if (result.NetworkError)
                return Loc.T("create.error_network");
            if (result.Status == 401)
                return Loc.T("create.error_profile");
            if (result.Status == 400 && result.Data != null && result.Data.error == "invalid_avatar")
                return Loc.T("create.error_avatar");
            if (result.Status == 429)
                return Loc.T("create.error_limit");
            return Loc.T("create.error_server");
        }

        void CommitFaceAndClose()
        {
            if (draftFace != lobby.PlayerFace)
            {
                try
                {
                    if (draftFace != null)
                        FaceStore.Save(draftFace);
                    else
                        FaceStore.Delete();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[CraDev] Yuzni saqlab bo'lmadi: " + e.Message);
                    SetError(Loc.T("face.save_failed"));
                    Refresh();
                    return;
                }
                lobby.CommitFace(draftFace);
            }
            lobby.RefreshProfileVisuals();
            lobby.Toast(Loc.T("studio.saved"));
            lobby.SetAvatarStudio(false);
        }

        // ------------------------------------------------------------------ Ko'rinish

        void SetView(int index)
        {
            view = Mathf.Clamp(index, 0, 1);
            lobby.Stage.StudioZoom = view;
            RefreshView();
        }

        void OnScrolled(float delta)
        {
            if (!open || Mathf.Approximately(delta, 0f))
                return;
            lobby.Stage.StudioZoom += Mathf.Sign(delta) * 0.25f;
            view = lobby.Stage.StudioZoom >= 0.5f ? 1 : 0;
            RefreshView();
        }

        void RefreshView()
        {
            for (int i = 0; i < viewButtons.Length; i++)
                Highlight(viewButtons[i], i == view);
        }

        static void Highlight(Button button, bool selected)
        {
            var surface = button.GetComponent<ReferenceSurface>();
            if (surface != null)
                surface.Style = selected ? 4 : 5;
        }

        void Refresh()
        {
            if (!open)
                return;
            var current = lobby.FindAvatar(draftAvatar);
            for (int i = 0; i < cards.Length && i < choices.Length; i++)
            {
                bool selected = choices[i] == current;
                Highlight(cards[i], selected);
                if (i < cardTicks.Length && cardTicks[i] != null)
                    cardTicks[i].SetActive(selected);
                cards[i].interactable = !saving;
            }
            avatarInfo.text = current != null ? current.Info : "";

            bool has = draftFace != null && draftFace.Photo != null;
            faceThumb.enabled = has;
            faceIcon.enabled = !has;
            if (has)
            {
                faceThumb.texture = draftFace.Photo;
                faceThumb.uvRect = MainMenuScreen.FaceThumbRect(draftFace);
            }
            else
                faceThumb.texture = null;
            var saved = lobby.PlayerFace;
            faceStatus.text = Loc.T(!has ? (saved != null ? "studio.face_removed" : "studio.face_none")
                : draftFace != saved ? "studio.face_new" : "face.on_avatar");
            faceStatus.color = has && draftFace == saved ? Ok : Muted;
            removeButton.gameObject.SetActive(has);

            scanButton.interactable = uploadButton.interactable = removeButton.interactable = !saving;
            cancelButton.interactable = closeButton.interactable = !saving;
            saveButton.interactable = !saving && Dirty;
            saveLabel.text = Loc.T(saving ? "create.saving" : "studio.save");
        }

        void SetError(string message)
        {
            errorText.text = message ?? "";
            errorText.color = Bad;
        }

        static void Deselect()
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        // ------------------------------------------------------------------ Kadrlar

        void Update()
        {
            float target = open ? 1f : 0f;
            if (!Mathf.Approximately(shown, target))
            {
                shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime / 0.28f);
                Apply(shown);
                if (!open && shown <= 0f)
                {
                    panel.gameObject.SetActive(false);
                    viewGroup.gameObject.SetActive(false);
                }
                return;
            }
            // Oyna o'lchami o'zgarsa: qahramon va uning ostidagi tugmalar panel chapidagi bo'sh joy o'rtasida qoladi
            if (open && viewDock != null)
            {
                float x = ScreenFraction;
                if (Mathf.Abs(viewDock.anchorMin.x - x) > 0.001f)
                {
                    viewDock.anchorMin = viewDock.anchorMax = new Vector2(x, 0f);
                    lobby.Stage.SetStudioFocus(lobby.Viewer.BodyHeight, x);
                }
            }
        }

        void Apply(float value)
        {
            float e = Ease.OutCubic(value);
            panelGroup.alpha = e;
            panelGroup.blocksRaycasts = panelGroup.interactable = open;
            viewGroup.alpha = e;
            viewGroup.blocksRaycasts = viewGroup.interactable = open;
            panel.anchoredPosition = new Vector2(panelX + (1f - e) * slide, panel.anchoredPosition.y);
            if (backdrop != null)
            {
                var color = backdrop.color;
                color.a = e * backdropAlpha;
                backdrop.color = color;
                backdrop.enabled = value > 0f;
            }
            if (viewDock != null)
            {
                float x = ScreenFraction;
                viewDock.anchorMin = new Vector2(x, 0f);
                viewDock.anchorMax = new Vector2(x, 0f);
            }
        }
    }
}

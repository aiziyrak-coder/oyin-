using System.Collections;
using CraDev.CharacterCreation;
using CraDev.Face;
using CraDev.Online;
using CraDev.World;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    /// <summary>
    /// Virtual dunyoga kirish joyi (profili bor o'yinchi uchun). Orqada 3D shahar, markazda o'yinchining qahramoni
    /// (o'z yuzi bilan) yonib turuvchi platformada. Chapda menyu: Kirish, Personajni sozlash, Sozlamalar, Yordam,
    /// Chiqish; yuqorida til, bildirishnomalar, profil; pastda shahar zonalari kartalari - bosilganda kamera
    /// o'sha binoga buriladi (zonalarning ichi keyin quriladi).
    ///
    /// Ochilganda profil serverda tekshiriladi: server o'yinchini tanimasa, yangi qahramon yaratish taklif qilinadi.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        [Header("Server")]
        [SerializeField] string serverUrl = GameApi.DefaultServerUrl;
        [SerializeField] float retryInterval = 10f;

        [Header("Qahramon va kamera")]
        [SerializeField] AvatarOption[] avatars;
        [SerializeField] AvatarViewer viewer;
        [SerializeField] MenuCamera menuCamera;
        [SerializeField] Transform characterAnchor;

        [Header("Matnlar")]
        [SerializeField] Text nicknameText;
        [SerializeField] Text profileNameText;
        [SerializeField] RawImage profileThumb;
        [SerializeField] Image profileIcon;
        [SerializeField] RectTransform nameplate;
        [SerializeField] Text nameplateName;
        [SerializeField] Text nameplateStatus;
        [SerializeField] Image nameplateDot;
        [SerializeField] Image statusDot;
        [SerializeField] Text statusText;
        [SerializeField] Text versionText;
        [SerializeField] CanvasGroup toastGroup;
        [SerializeField] Text toastText;

        [Header("Menyu")]
        [SerializeField] RectTransform menu;
        [SerializeField] CanvasGroup menuGroup;
        [SerializeField] CanvasGroup cardsGroup;
        [SerializeField] Button enterButton;
        [SerializeField] Button customizeButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button helpButton;
        [SerializeField] Button quitButton;
        [SerializeField] Button bellButton;
        [SerializeField] Button profileButton;

        [Header("Zonalar")]
        [SerializeField] string[] zoneIds;
        [SerializeField] Vector3[] zoneTargets;
        [SerializeField] Button[] zoneCards;
        [SerializeField] Image[] zoneBorders;
        [SerializeField] Button nextZoneButton;

        [Header("Oynalar")]
        [SerializeField] ConfirmDialog dialog;
        [SerializeField] SettingsPanel settings;
        [SerializeField] Image fader;

        static readonly Color OnlineColor = new Color32(34, 197, 94, 255);
        static readonly Color OfflineColor = new Color32(245, 165, 36, 255);
        static readonly Color Muted = new Color32(160, 168, 180, 255);
        static readonly Color BorderIdle = new Color(1f, 1f, 1f, 0.14f);
        static readonly Color BorderActive = new Color(0.45f, 0.72f, 1f, 1f);

        enum Status { Connecting, Online, Offline, Error }

        GameApi api;
        Status status;
        float time;
        float toastUntil = -1f;
        float leavingAt = -1f;
        int selectedZone = -1;
        Vector2 menuHome;
        Canvas canvas;

        void Start()
        {
            Cursor.visible = true;
            api = new GameApi(serverUrl);
            canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
            menuHome = menu.anchoredPosition;

            enterButton.onClick.AddListener(() => Toast(Loc.T("menu.enter_soon")));
            customizeButton.onClick.AddListener(Customize);
            profileButton.onClick.AddListener(Customize);
            settingsButton.onClick.AddListener(settings.Show);
            helpButton.onClick.AddListener(() => dialog.Show(Loc.T("menu.help_title"), Loc.T("menu.help_message"), Loc.T("common.ok"), null));
            quitButton.onClick.AddListener(AskQuit);
            bellButton.onClick.AddListener(() => Toast(Loc.T("menu.no_notifications")));
            for (int i = 0; i < zoneCards.Length; i++)
            {
                int index = i;
                zoneCards[i].onClick.AddListener(() => SelectZone(index == selectedZone ? -1 : index));
            }
            nextZoneButton.onClick.AddListener(() => SelectZone((selectedZone + 1) % zoneCards.Length));

            string nickname = PlayerProfile.Nickname;
            nicknameText.text = nickname;
            profileNameText.text = nickname;
            nameplateName.text = nickname;
            versionText.text = "v" + Application.version;
            toastGroup.alpha = 0f;
            ShowAvatar(PlayerProfile.AvatarId);
            var face = FaceStore.Load();
            viewer.SetFace(face);
            ShowProfileThumb(face);
            SelectZone(-1);
            SetStatus(Status.Connecting);
            StartCoroutine(CheckProfile());
        }

        void OnEnable() => Loc.Changed += OnLanguageChanged;

        void OnDisable() => Loc.Changed -= OnLanguageChanged;

        void OnLanguageChanged()
        {
            SetStatus(status);
            ShowAvatar(PlayerProfile.AvatarId, reload: false);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            time += dt;

            // Kirish: menyu chapdan, kartalar pastdan suzib chiqadi
            float intro = Ease.OutCubic(Anim.Progress(time, 0.2f, 0.7f));
            menuGroup.alpha = intro;
            menu.anchoredPosition = menuHome + new Vector2((1f - intro) * -48f, 0f);
            cardsGroup.alpha = Ease.OutCubic(Anim.Progress(time, 0.5f, 0.7f));

            float fadeIn = 1f - Ease.OutCubic(Anim.Progress(time, 0f, 0.8f));
            float fadeOut = leavingAt >= 0f ? Ease.InOutSine(Anim.Progress(time, leavingAt, 0.4f)) : 0f;
            Anim.SetAlpha(fader, Mathf.Max(fadeIn, fadeOut));

            if (toastUntil >= 0f)
                toastGroup.alpha = Mathf.Clamp01((toastUntil - time) / 0.35f) * Mathf.Clamp01((time - (toastUntil - 3.2f)) / 0.2f);

            FollowCharacter();

            if (!ModalWindow.AnyOpen && leavingAt < 0f && Anim.BackPressed())
            {
                if (selectedZone >= 0)
                    SelectZone(-1);
                else
                    AskQuit();
            }
        }

        /// <summary>Qahramon yonidagi nom yorlig'i: ekrandagi joyi qahramonga ergashadi.</summary>
        void FollowCharacter()
        {
            if (characterAnchor == null || nameplate == null)
                return;
            var cam = menuCamera != null ? menuCamera.GetComponent<Camera>() : Camera.main;
            var screen = cam.WorldToScreenPoint(characterAnchor.position + new Vector3(0.62f, 0.72f, 0f));
            bool visible = screen.z > 0f && selectedZone < 0;
            nameplate.gameObject.SetActive(visible);
            if (visible)
                nameplate.position = screen;
        }

        void ShowAvatar(string avatarId, bool reload = true)
        {
            AvatarOption option = null;
            foreach (var a in avatars)
                if (a.id == avatarId) option = a;
            if (option == null)
                foreach (var a in avatars)
                    if (a.gender == PlayerProfile.Gender) { option = a; break; }
            if (option != null && reload)
                viewer.SetAvatar(option);
        }

        void ShowProfileThumb(FaceData face)
        {
            bool has = face != null;
            profileThumb.enabled = has;
            profileIcon.enabled = !has;
            if (!has)
                return;
            // Rasmdagi yuz joylashgan kvadrat
            Vector2 min = face.Landmarks[0], max = face.Landmarks[0];
            for (int i = 1; i < FaceTracker.MeshLandmarkCount; i++)
            {
                min = Vector2.Min(min, face.Landmarks[i]);
                max = Vector2.Max(max, face.Landmarks[i]);
            }
            float w = face.Photo.width, h = face.Photo.height;
            float side = Mathf.Max((max.x - min.x) * w, (max.y - min.y) * h) * 1.2f;
            Vector2 c = (min + max) / 2f;
            profileThumb.texture = face.Photo;
            profileThumb.uvRect = new Rect(c.x - side / 2f / w, c.y - side / 2f / h, side / w, side / h);
        }

        // ------------------------------------------------------------------ Zonalar

        void SelectZone(int index)
        {
            selectedZone = index;
            for (int i = 0; i < zoneBorders.Length; i++)
                zoneBorders[i].color = i == index ? BorderActive : BorderIdle;
            if (index < 0)
            {
                menuCamera.Home();
                return;
            }
            menuCamera.Focus(zoneTargets[index]);
            Toast(Loc.F("menu.zone_soon", Loc.T("zone." + zoneIds[index])));
        }

        // ------------------------------------------------------------------ Server

        IEnumerator CheckProfile()
        {
            while (true)
            {
                ApiResult<PlayerResponse> result = default;
                yield return api.GetMe(PlayerProfile.Token, r => result = r);

                if (result.NetworkError)
                {
                    SetStatus(Status.Offline);
                    yield return new WaitForSecondsRealtime(retryInterval);
                    continue;
                }
                if (result.Status == 401)
                {
                    SetStatus(Status.Offline);
                    dialog.Show(Loc.T("menu.profile_missing_title"), Loc.T("menu.profile_missing_message"), Loc.T("menu.create_character"),
                        () => Leave(() =>
                        {
                            PlayerProfile.Clear();
                            SceneLoader.Load("CharacterCreation");
                        }), dismissable: false);
                    yield break;
                }
                if (result.Status == 200 && result.Data != null)
                {
                    SetStatus(Status.Online);
                    // Server - asosiy manba: avatar boshqa kompyuterda o'zgartirilgan bo'lishi mumkin
                    if (!string.IsNullOrEmpty(result.Data.avatarId) && result.Data.avatarId != PlayerProfile.AvatarId)
                    {
                        PlayerProfile.SetAvatar(result.Data.avatarId);
                        ShowAvatar(result.Data.avatarId);
                    }
                    yield break;
                }
                SetStatus(Status.Error);
                yield return new WaitForSecondsRealtime(retryInterval);
            }
        }

        void SetStatus(Status value)
        {
            status = value;
            string text = Loc.T(value == Status.Online ? "common.online" : value == Status.Offline ? "common.offline"
                : value == Status.Error ? "common.server_error" : "common.connecting");
            var color = value == Status.Online ? OnlineColor : value == Status.Connecting ? Muted : OfflineColor;
            statusText.text = text;
            statusDot.color = color;
            nameplateStatus.text = text;
            nameplateDot.color = color;
        }

        // ------------------------------------------------------------------ Amallar

        void Customize() => Leave(() =>
        {
            CharacterCreationScreen.EditRequested = true;
            SceneLoader.Load("CharacterCreation");
        });

        void Toast(string text)
        {
            toastText.text = text;
            toastUntil = time + 3.2f;
        }

        void AskQuit()
        {
            if (leavingAt >= 0f)
                return;
            dialog.Show(Loc.T("menu.quit_title"), Loc.T("menu.quit_message"), Loc.T("common.quit"), ConfirmDialog.QuitGame, Loc.T("common.cancel"));
        }

        void Leave(System.Action then)
        {
            if (leavingAt >= 0f)
                return;
            leavingAt = time;
            menuGroup.interactable = false;
            StartCoroutine(After(0.45f, then));
        }

        static IEnumerator After(float seconds, System.Action action)
        {
            yield return new WaitForSecondsRealtime(seconds);
            action();
        }
    }
}

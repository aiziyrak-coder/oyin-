using System.Collections;
using CraDev.CharacterCreation;
using CraDev.Face;
using CraDev.Online;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    /// <summary>
    /// Virtual dunyoga kirish joyi (profili bor o'yinchi uchun), foydalanuvchi konsept rasmi bo'yicha: orqada shahar
    /// manzarasi (rasm), markazda o'yinchining 3D qahramoni (o'z yuzi bilan) rasmdagi yonib turuvchi platformada.
    /// Chapda menyu: Kirish, Personajni sozlash, Sozlamalar, Yordam, Chiqish; yuqorida til, bildirishnomalar, profil;
    /// pastda zona kartalari - bosilganda rasmdagi o'sha bino ustida belgi chiqadi (zonalarning ichi keyin quriladi).
    ///
    /// Ochilganda profil serverda tekshiriladi: server o'yinchini tanimasa, yangi qahramon yaratish taklif qilinadi.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        [Header("Server")]
        [SerializeField] string serverUrl = GameApi.DefaultServerUrl;
        [SerializeField] float retryInterval = 10f;

        [Header("Qahramon")]
        [SerializeField] AvatarOption[] avatars;
        [SerializeField] AvatarViewer viewer;
        [SerializeField] Camera stageCamera;
        [Tooltip("Qahramon boshi ustidagi nom yorlig'i (pastki markazi boshdan shuncha piksel yuqorida).")]
        [SerializeField] RectTransform nameplate;
        [SerializeField] float nameplateGap = 30f;

        [Header("Matnlar")]
        [SerializeField] Text nicknameText;
        [SerializeField] Text profileNameText;
        [SerializeField] RawImage profileThumb;
        [SerializeField] Image profileIcon;
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
        [SerializeField] Button[] zoneCards;
        [SerializeField] Image[] zoneBorders;
        [Tooltip("Rasmdagi har bir zona binosi ustidagi belgi (kartalar tartibida).")]
        [SerializeField] CanvasGroup[] zoneMarkers;
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
            nicknameText.supportRichText = true;
            nicknameText.text = Highlight(nickname);
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

            // Tanlangan zona belgisi silliq paydo bo'ladi va yengil "nafas oladi"
            for (int i = 0; i < zoneMarkers.Length; i++)
            {
                var marker = zoneMarkers[i];
                float target = i == selectedZone ? 1f : 0f;
                marker.alpha = Mathf.MoveTowards(marker.alpha, target, dt / 0.2f);
                float pulse = 1f + 0.06f * Mathf.Sin(time * 3f);
                marker.transform.localScale = Vector3.one * Mathf.Lerp(0.85f, pulse, marker.alpha);
            }

            if (!ModalWindow.AnyOpen && leavingAt < 0f && Anim.BackPressed())
            {
                if (selectedZone >= 0)
                    SelectZone(-1);
                else
                    AskQuit();
            }
        }

        void LateUpdate() => PlaceNameplate();

        /// <summary>Nom yorlig'i qahramon boshi ustida turadi (bo'yi har xil avatarlarda ham, ekran nisbati o'zgarsa ham).</summary>
        void PlaceNameplate()
        {
            if (nameplate == null || stageCamera == null)
                return;
            var parent = (RectTransform)nameplate.parent;
            Vector2 screen = stageCamera.WorldToScreenPoint(viewer.TopOfHead);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out var local))
                nameplate.localPosition = local + new Vector2(0f, nameplateGap);
        }

        /// <summary>Yorliq kengligi ism va holat matniga moslanadi.</summary>
        void FitNameplate()
        {
            if (nameplate == null)
                return;
            float text = Mathf.Max(nameplateName.preferredWidth, nameplateStatus.preferredWidth);
            float left = nameplateName.rectTransform.anchoredPosition.x;
            nameplate.sizeDelta = new Vector2(Mathf.Ceil(left + text + 16f), nameplate.sizeDelta.y);
        }

        /// <summary>Konsept rasmdagidek: ismning o'rtadagi harfi ko'k ("Lyn<b>x</b>os").</summary>
        static string Highlight(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length < 3)
                return name;
            int i = name.Length / 2;
            return name.Substring(0, i) + "<color=#5C7CFF>" + name[i] + "</color>" + name.Substring(i + 1);
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
                // Hech biri tanlanmaganda ham birinchi karta ajralib turadi (konsept rasmdagidek)
                zoneBorders[i].color = i == index || (index < 0 && i == 0) ? BorderActive : BorderIdle;
            if (index < 0)
                return;
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
                            SceneLoader.Switch("CharacterCreation");
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
            FitNameplate();
        }

        // ------------------------------------------------------------------ Amallar

        void Customize() => Leave(() =>
        {
            CharacterCreationScreen.EditRequested = true;
            SceneLoader.Switch("CharacterCreation");
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

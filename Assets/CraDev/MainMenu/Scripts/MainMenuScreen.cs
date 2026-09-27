using System.Collections;
using CraDev.CharacterCreation;
using CraDev.Face;
using CraDev.Online;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    /// <summary>
    /// Bosh menyu (profili bor o'yinchi uchun). O'ngda o'yinchining 3D qahramoni (o'z yuzi bilan), chapda nomi va
    /// menyu: Play, Customize (avatar va yuzni o'zgartirish), Settings, Quit.
    ///
    /// Ochilganda profil serverda tekshiriladi: server o'yinchini tanimasa (baza tozalangan), yangi qahramon
    /// yaratish taklif qilinadi. Server javob bermasa, o'yin saqlangan ma'lumot bilan davom etadi ("Offline").
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        [Header("Server")]
        [SerializeField] string serverUrl = GameApi.DefaultServerUrl;
        [SerializeField] float retryInterval = 10f;

        [Header("Qahramon")]
        [SerializeField] AvatarOption[] avatars;
        [SerializeField] AvatarViewer viewer;

        [Header("Matnlar")]
        [SerializeField] Text nicknameText;
        [SerializeField] Text avatarInfoText;
        [SerializeField] Text toastText;
        [SerializeField] Image statusDot;
        [SerializeField] Text statusText;
        [SerializeField] Text versionText;

        [Header("Menyu")]
        [SerializeField] RectTransform menu;
        [SerializeField] CanvasGroup menuGroup;
        [SerializeField] Button playButton;
        [SerializeField] Button customizeButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button quitButton;
        [SerializeField] ConfirmDialog dialog;
        [SerializeField] SettingsPanel settings;
        [SerializeField] Image fader;

        static readonly Color Online = new Color32(34, 197, 94, 255);
        static readonly Color Offline = new Color32(245, 165, 36, 255);
        static readonly Color Muted = new Color32(142, 147, 154, 255);

        GameApi api;
        float time;
        float toastUntil = -1f;
        float leavingAt = -1f;
        Vector2 menuHome;

        void Start()
        {
            Cursor.visible = true;
            api = new GameApi(serverUrl);
            menuHome = menu.anchoredPosition;

            playButton.onClick.AddListener(Play);
            customizeButton.onClick.AddListener(() => Leave(() =>
            {
                CharacterCreationScreen.EditRequested = true;
                SceneLoader.Load("CharacterCreation");
            }));
            settingsButton.onClick.AddListener(settings.Show);
            quitButton.onClick.AddListener(AskQuit);

            nicknameText.text = PlayerProfile.Nickname;
            versionText.text = "v" + Application.version;
            toastText.text = "";
            ShowAvatar(PlayerProfile.AvatarId);
            viewer.SetFace(FaceStore.Load());
            SetStatus("Connecting…", Muted);
            StartCoroutine(CheckProfile());
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            time += dt;

            // Kirish: menyu chapdan suzib chiqadi
            float intro = Ease.OutCubic(Anim.Progress(time, 0.15f, 0.6f));
            menuGroup.alpha = intro;
            menu.anchoredPosition = menuHome + new Vector2((1f - intro) * -40f, 0f);

            float fadeIn = 1f - Ease.OutCubic(Anim.Progress(time, 0f, 0.5f));
            float fadeOut = leavingAt >= 0f ? Ease.InOutSine(Anim.Progress(time, leavingAt, 0.4f)) : 0f;
            Anim.SetAlpha(fader, Mathf.Max(fadeIn, fadeOut));

            if (toastUntil >= 0f)
                Anim.SetAlpha(toastText, Mathf.Clamp01((toastUntil - time) / 0.4f));

            if (!ModalWindow.AnyOpen && leavingAt < 0f && Anim.BackPressed())
                AskQuit();
        }

        void ShowAvatar(string avatarId)
        {
            AvatarOption option = null;
            foreach (var a in avatars)
                if (a.id == avatarId) option = a;
            if (option == null)
                foreach (var a in avatars)
                    if (a.gender == PlayerProfile.Gender) { option = a; break; }
            if (option == null)
                return;
            viewer.SetAvatar(option);
            avatarInfoText.text = $"{option.title} · {option.heightCm} cm";
        }

        IEnumerator CheckProfile()
        {
            while (true)
            {
                ApiResult<PlayerResponse> result = default;
                yield return api.GetMe(PlayerProfile.Token, r => result = r);

                if (result.NetworkError)
                {
                    SetStatus("Offline", Offline);
                    yield return new WaitForSecondsRealtime(retryInterval);
                    continue;
                }
                if (result.Status == 401)
                {
                    SetStatus("Offline", Offline);
                    dialog.Show("Profile not found",
                        "Your player profile no longer exists on the server. Create a new character to continue.",
                        "Create character", () => Leave(() =>
                        {
                            PlayerProfile.Clear();
                            SceneLoader.Load("CharacterCreation");
                        }), cancel: null);
                    yield break;
                }
                if (result.Status == 200 && result.Data != null)
                {
                    SetStatus("Online", Online);
                    // Server - asosiy manba: avatar boshqa kompyuterda o'zgartirilgan bo'lishi mumkin
                    if (!string.IsNullOrEmpty(result.Data.avatarId) && result.Data.avatarId != PlayerProfile.AvatarId)
                    {
                        PlayerProfile.SetAvatar(result.Data.avatarId);
                        ShowAvatar(result.Data.avatarId);
                    }
                    yield break;
                }
                SetStatus("Server error", Offline);
                yield return new WaitForSecondsRealtime(retryInterval);
            }
        }

        void Play()
        {
            // O'yin dunyosi hali yo'q
            toastText.text = "The world is under construction. Coming soon!";
            Anim.SetAlpha(toastText, 1f);
            toastUntil = time + 3f;
        }

        void AskQuit()
        {
            if (leavingAt >= 0f)
                return;
            dialog.Show("Quit game?", "Are you sure you want to quit CraDev?", "Quit", ConfirmDialog.QuitGame);
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

        void SetStatus(string text, Color color)
        {
            statusText.text = text;
            statusDot.color = color;
        }
    }
}

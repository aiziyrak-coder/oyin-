using System;
using System.Collections;
using CraDev.CharacterCreation;
using CraDev.Face;
using CraDev.Online;
using CraDev.Wardrobe;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    /// <summary>
    /// Lobby (NewWorld virtual dunyosiga kirish joyi) - foydalanuvchi konsept rasmlari bo'yicha. Hamma sahifalar
    /// (Bosh sahifa, Dunyo xaritasi, Do'stlar, Top-lar, Sozlamalar, Garderob, zonalar) bitta sahnada: yuqoridagi
    /// navigatsiya ularni almashtiradi, fon rasmi silliq almashadi, 3D qahramon kerakli sahifalarda (Bosh sahifa,
    /// Garderob) o'sha rasmdagi joyida turadi.
    ///
    /// Ochilganda profil serverda tekshiriladi (server tanimasa - yangi qahramon yaratish taklif qilinadi), profil
    /// ma'lumotlari (avatar, kiyim) serverdan yangilanadi, "onlayn" signali boshlanadi. Qo'ng'iroq - kelgan do'stlik
    /// so'rovlari. Esc: ochiq menyu yopiladi, sahifadan Bosh sahifaga qaytiladi, Bosh sahifada - chiqish so'raladi.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        [Header("Server")]
        [SerializeField] string serverUrl = GameApi.DefaultServerUrl;
        [SerializeField] float retryInterval = 10f;
        [SerializeField] float notificationsInterval = 60f;

        [Header("Qahramon")]
        [SerializeField] AvatarOption[] avatars;
        [SerializeField] AvatarViewer viewer;
        [SerializeField] LobbyStage stage;

        [Header("Sahifalar")]
        [SerializeField] LobbyPage[] pages;
        [SerializeField] string homePage = "home";
        [Tooltip("Navigatsiya yorliqlari tartibida: har biri qaysi sahifani ochadi.")]
        [SerializeField] string[] navPages = { "home", "world", "friends", "top", "settings" };
        [SerializeField] SelectList navTabs;
        [SerializeField] bool singleWindow;
        [SerializeField] LobbyFriendsPanel friendsPanel;
        [SerializeField] string gameplayScene;
        LobbyPage settingsOverlay;
        bool settingsOpen;
        public bool SingleWindow => singleWindow;
        public bool SettingsOpen => settingsOpen;
        public LobbyFriendsPanel FriendsPanel => friendsPanel;

        [Header("Fon")]
        [SerializeField] RawImage backgroundA;
        [SerializeField] RawImage backgroundB;
        [SerializeField] Texture defaultBackground;

        [Header("Yuqori o'ng")]
        [SerializeField] Button bellButton;
        [SerializeField] Image bellDot;
        [SerializeField] Button profileButton;
        [SerializeField] Text profileName;
        [SerializeField] RawImage profileThumb;
        [SerializeField] Image profileIcon;
        [SerializeField] CanvasGroup profileMenu;
        [SerializeField] Button profileMenuBlocker;
        [Tooltip("Garderob, Personajni sozlash, Sozlamalar, Chiqish.")]
        [SerializeField] Button[] profileMenuButtons;

        [Header("Umumiy")]
        [SerializeField] CanvasGroup toastGroup;
        [SerializeField] Text toastText;
        [SerializeField] ConfirmDialog dialog;
        [SerializeField] Image fader;

        GameApi api;
        LobbyPage current;
        LobbyPage pending;
        float time;
        float toastUntil = -1f;
        float leavingAt = -1f;
        float switchAt = -1f;
        float pageShown = 1f;
        float backgroundBlend = 1f;
        bool menuOpen;
        float menuShown;

        public GameApi Api => api;
        public string ServerUrl => serverUrl;
        public AvatarViewer Viewer => viewer;
        public LobbyStage Stage => stage;
        public ConfirmDialog Dialog => dialog;
        public LobbyPage Current => current;

        /// <summary>Kelgan (javob kutayotgan) do'stlik so'rovlari soni.</summary>
        public int IncomingRequests { get; private set; }

        /// <summary>Profil (kiyim, avatar, maxfiylik) o'zgarganda: sahifalar o'z matnlarini yangilaydi.</summary>
        public event Action ProfileChanged;

        void Awake()
        {
            api = new GameApi(serverUrl);
            foreach (var page in pages)
            {
                page.Bind(this);
                page.Group.alpha = 0f;
                page.Group.blocksRaycasts = false;
                page.gameObject.SetActive(false);
            }
        }

        void Start()
        {
            Cursor.visible = true;
            if(navTabs!=null)navTabs.Changed += index => Show(navPages[index]);
            if(bellButton!=null)bellButton.onClick.AddListener(OnBell);
            if(profileButton!=null)profileButton.onClick.AddListener(() => {if(singleWindow)Show("settings");else SetMenu(!menuOpen);});
            if(profileMenu!=null)
            {
                profileMenuBlocker.onClick.AddListener(() => SetMenu(false));
                profileMenuButtons[0].onClick.AddListener(() => { SetMenu(false); Show("wardrobe"); });
                profileMenuButtons[1].onClick.AddListener(() => { SetMenu(false); Customize(); });
                profileMenuButtons[2].onClick.AddListener(() => { SetMenu(false); Show("settings"); });
                profileMenuButtons[3].onClick.AddListener(() => { SetMenu(false); AskQuit(); });
                profileMenu.alpha = 0f;
                profileMenu.blocksRaycasts = false;
                profileMenuBlocker.gameObject.SetActive(false);
            }
            toastGroup.alpha = 0f;
            if(bellDot!=null)bellDot.enabled = false;

            profileName.text = PlayerProfile.Nickname;
            ShowAvatar(PlayerProfile.AvatarId);
            var face = FaceStore.Load();
            viewer.SetFace(face);
            viewer.SetOutfit(Outfit.FromJson(PlayerProfile.Outfit));
            ShowProfileThumb(face);

            Show(homePage, immediate: true);
            if(friendsPanel!=null)friendsPanel.Begin(this);
            Presence.Begin(serverUrl);
            StartCoroutine(CheckProfile());
            if(friendsPanel==null)StartCoroutine(PollNotifications());
        }

        // ------------------------------------------------------------------ Sahifalar

        public void Show(string id) => Show(id, false);

        void Show(string id, bool immediate)
        {
            if(singleWindow)
            {
                if(id=="settings"){SetSettings(true);return;}
                if(id!=homePage)return; // Archived pages remain in the scene, but have no live route.
                SetSettings(false);
            }
            var page = Array.Find(pages, p => p.Id == id);
            if (page == null || leavingAt >= 0f)
                return;
            if (page == current)
            {
                // A fast click back to the visible page cancels a queued transition.
                pending = null;
                switchAt = -1f;
                current.Group.blocksRaycasts = true;
                SyncNav(page);
                return;
            }
            // Sahifa chiqishni to'xtatishi mumkin (masalan, saqlanmagan kiyim): u o'zi so'rab, keyin qayta chaqiradi
            if (current != null && !immediate && !current.CanLeave(() => Switch(page, false)))
            {
                SyncNav(current);
                return;
            }
            Switch(page, immediate);
        }

        void Switch(LobbyPage page, bool immediate)
        {
            SetMenu(false);
            pending = page;
            SyncNav(page);
            if (immediate || current == null)
            {
                Activate();
                pageShown = immediate ? 1f : 0f;
                ApplyPageVisibility();
                return;
            }
            // Eski sahifa tez so'nadi, keyin yangisi ochiladi (fon va kamera o'rtada almashadi)
            switchAt = time + 0.14f;
            current.Group.blocksRaycasts = false;
        }

        void Activate()
        {
            if (current != null)
            {
                current.OnHide();
                current.Group.alpha = 0f;
                current.gameObject.SetActive(false);
            }
            current = pending;
            pending = null;
            current.gameObject.SetActive(true);
            current.Group.blocksRaycasts = true;
            SetBackground(current.Background != null ? current.Background : defaultBackground);
            stage.Apply(current.StagePose);
            viewer.enabled = current.StagePose >= 0;
            current.OnShow();
        }

        void SyncNav(LobbyPage page)
        {
            if(navTabs==null)return;
            if (page.NavTab >= 0)
                navTabs.Select(page.NavTab);
            else
                navTabs.Select(-1);
        }

        void SetBackground(Texture texture)
        {
            if (backgroundA.texture == texture)
                return;
            // B - yangi fon, A ustida paydo bo'ladi; tugagach A ga o'tkaziladi
            backgroundB.texture = texture;
            Fit(backgroundB, texture);
            backgroundBlend = 0f;
        }

        static void Fit(RawImage image, Texture texture)
        {
            var fitter = image.GetComponent<AspectRatioFitter>();
            if (fitter != null && texture != null)
                fitter.aspectRatio = (float)texture.width / Mathf.Max(1, texture.height);
        }

        void ApplyPageVisibility()
        {
            if (current == null)
                return;
            float t = Ease.OutCubic(pageShown);
            current.Group.alpha = t;
            current.transform.localPosition = new Vector3(0f, (1f - t) * -14f, 0f);
        }

        // ------------------------------------------------------------------ Kadrlar

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            time += dt;

            if (switchAt >= 0f)
            {
                pageShown = Mathf.MoveTowards(pageShown, 0f, dt / 0.14f);
                if (time >= switchAt)
                {
                    switchAt = -1f;
                    Activate();
                    pageShown = 0f;
                }
            }
            else
                pageShown = Mathf.MoveTowards(pageShown, 1f, dt / 0.28f);
            ApplyPageVisibility();

            if (backgroundBlend < 1f)
            {
                backgroundBlend = Mathf.MoveTowards(backgroundBlend, 1f, dt / 0.35f);
                Anim.SetAlpha(backgroundB, Ease.InOutSine(backgroundBlend));
                if (backgroundBlend >= 1f)
                {
                    backgroundA.texture = backgroundB.texture;
                    Fit(backgroundA, backgroundA.texture);
                    Anim.SetAlpha(backgroundB, 0f);
                }
            }

            float fadeIn = 1f - Ease.OutCubic(Anim.Progress(time, 0f, 0.8f));
            float fadeOut = leavingAt >= 0f ? Ease.InOutSine(Anim.Progress(time, leavingAt, 0.4f)) : 0f;
            Anim.SetAlpha(fader, Mathf.Max(fadeIn, fadeOut));

            menuShown = Mathf.MoveTowards(menuShown, menuOpen ? 1f : 0f, dt / 0.12f);
            if(profileMenu!=null)
            {
                profileMenu.alpha = Ease.OutCubic(menuShown);
                profileMenu.transform.localScale = Vector3.one * Mathf.Lerp(0.97f, 1f, Ease.OutCubic(menuShown));
            }

            if (toastUntil >= 0f)
                toastGroup.alpha = Mathf.Clamp01((toastUntil - time) / 0.35f) * Mathf.Clamp01((time - (toastUntil - 3.2f)) / 0.2f);

            if (!ModalWindow.AnyOpen && leavingAt < 0f && Anim.BackPressed())
            {
                if(settingsOpen)SetSettings(false);
                else if (menuOpen)
                    SetMenu(false);
                else if (current != null && current.OnBack())
                {
                }
                else if (current != null && current.Id != homePage)
                    Show(homePage);
                else
                    AskQuit();
            }
        }

        // ------------------------------------------------------------------ Yuqori o'ng: profil menyusi, qo'ng'iroq

        void SetMenu(bool open)
        {
            if(profileMenu==null)return;
            menuOpen = open;
            profileMenu.blocksRaycasts = open;
            profileMenu.interactable = open;
            profileMenuBlocker.gameObject.SetActive(open);
        }

        void OnBell()
        {
            if(singleWindow){friendsPanel?.Choose("requests");return;}
            if (IncomingRequests > 0)
                Show("friends");
            else
                Toast(Loc.T("menu.no_notifications"));
        }

        IEnumerator PollNotifications()
        {
            while (true)
            {
                yield return api.Friends(PlayerProfile.Token, result =>
                {
                    if (!result.Ok)
                        return;
                    SetIncoming(result.Data.incoming?.Length ?? 0);
                });
                yield return new WaitForSecondsRealtime(notificationsInterval);
            }
        }

        public void SetIncoming(int count)
        {
            IncomingRequests = count;
            if(bellDot!=null)bellDot.enabled = count > 0 && LobbyPrefs.NotifyFriendRequests;
        }

        void ShowProfileThumb(FaceData face)
        {
            bool has = face != null;
            profileThumb.enabled = has;
            profileIcon.enabled = !has;
            if (!has)
            {
                var option = FindAvatar(PlayerProfile.AvatarId);
                if (option?.card != null)
                {
                    profileIcon.sprite = option.card;
                    profileIcon.color = Color.white;
                    profileIcon.preserveAspect = true;
                }
                return;
            }
            profileThumb.texture = face.Photo;
            profileThumb.uvRect = FaceThumbRect(face);
        }

        /// <summary>Rasmdagi yuz joylashgan kvadrat (profil rasmchalari uchun).</summary>
        public static Rect FaceThumbRect(FaceData face)
        {
            Vector2 min = face.Landmarks[0], max = face.Landmarks[0];
            for (int i = 1; i < FaceTracker.MeshLandmarkCount; i++)
            {
                min = Vector2.Min(min, face.Landmarks[i]);
                max = Vector2.Max(max, face.Landmarks[i]);
            }
            float w = face.Photo.width, h = face.Photo.height;
            float side = Mathf.Max((max.x - min.x) * w, (max.y - min.y) * h) * 1.35f;
            Vector2 c = (min + max) / 2f;
            return new Rect(c.x - side / 2f / w, c.y - side / 2f / h, side / w, side / h);
        }

        // ------------------------------------------------------------------ Qahramon

        public AvatarOption FindAvatar(string id)
        {
            foreach (var a in avatars)
                if (a.id == id)
                    return a;
            return null;
        }

        void ShowAvatar(string avatarId)
        {
            var option = FindAvatar(avatarId);
            if (option == null)
                foreach (var a in avatars)
                    if (a.gender == PlayerProfile.Gender) { option = a; break; }
            if (option != null)
                viewer.SetAvatar(option);
        }

        /// <summary>Kiyim saqlandi (garderob): qahramon, server va boshqa sahifalar yangilanadi.</summary>
        public void SaveOutfit(Outfit outfit, Action<bool> done = null)
        {
            string json = WardrobeCatalog.Sanitize(outfit).ToJson();
            StartCoroutine(api.UpdateOutfit(PlayerProfile.Token, json, result =>
            {
                if (result.Ok)
                {
                    PlayerProfile.SetOutfit(json);
                    viewer.SetOutfit(outfit);
                    ProfileChanged?.Invoke();
                }
                done?.Invoke(result.Ok);
            }));
        }

        public void NotifyProfileChanged() => ProfileChanged?.Invoke();

        // ------------------------------------------------------------------ Server

        IEnumerator CheckProfile()
        {
            while (true)
            {
                ApiResult<PlayerResponse> result = default;
                yield return api.GetMe(PlayerProfile.Token, r => result = r);

                if (result.NetworkError)
                {
                    yield return new WaitForSecondsRealtime(retryInterval);
                    continue;
                }
                if (result.Status == 401)
                {
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
                    // Server - asosiy manba: avatar yoki kiyim boshqa kompyuterda o'zgartirilgan bo'lishi mumkin
                    string oldAvatar = PlayerProfile.AvatarId, oldOutfit = PlayerProfile.Outfit;
                    PlayerProfile.Refresh(result.Data);
                    if (PlayerProfile.AvatarId != oldAvatar)
                        ShowAvatar(PlayerProfile.AvatarId);
                    if (PlayerProfile.Outfit != oldOutfit)
                        viewer.SetOutfit(Outfit.FromJson(PlayerProfile.Outfit));
                    ProfileChanged?.Invoke();
                    yield break;
                }
                yield return new WaitForSecondsRealtime(retryInterval);
            }
        }

        // ------------------------------------------------------------------ Amallar

        public void SetSettings(bool open)
        {
            if(!singleWindow || settingsOpen==open)return;
            settingsOverlay ??= Array.Find(pages,p=>p.Id=="settings");
            if(settingsOverlay==null)return;
            settingsOpen=open;
            if(open)
            {
                settingsOverlay.gameObject.SetActive(true);
                settingsOverlay.Group.alpha=1;
                settingsOverlay.Group.blocksRaycasts=true;
                settingsOverlay.OnShow();
            }
            else
            {
                settingsOverlay.OnHide();
                settingsOverlay.Group.blocksRaycasts=false;
                settingsOverlay.gameObject.SetActive(false);
            }
            if(current!=null){current.Group.interactable=!open;current.Group.blocksRaycasts=!open;}
        }

        public void ChooseSection(string value)
        {
            var target=settingsOpen?settingsOverlay:current;
            if(target is LobbyContent content)content.Choose(value);
        }

        public void EnterWorld()
        {
            if(!string.IsNullOrWhiteSpace(gameplayScene) && Application.CanStreamedLevelBeLoaded(gameplayScene))
                Leave(()=>SceneLoader.Switch(gameplayScene));
            else Toast(Loc.T("lobby.world_pending"));
        }

        public void Customize() => Leave(() =>
        {
            CharacterCreationScreen.EditRequested = true;
            SceneLoader.Switch("CharacterCreation");
        });

        public void Toast(string text)
        {
            toastText.text = text;
            toastUntil = time + 3.2f;
        }

        /// <summary>Hali qurilmagan joy yoki imkoniyat: "{nom}: tez orada".</summary>
        public void Soon(string name) => Toast(Loc.F("menu.zone_soon", name));

        public void AskQuit()
        {
            if (leavingAt >= 0f)
                return;
            dialog.Show(Loc.T("menu.quit_title"), Loc.T("menu.quit_message"), Loc.T("common.quit"), ConfirmDialog.QuitGame, Loc.T("common.cancel"));
        }

        void Leave(Action then)
        {
            if (leavingAt >= 0f)
                return;
            leavingAt = time;
            if (current != null)
                current.Group.interactable = false;
            StartCoroutine(After(0.45f, then));
        }

        static IEnumerator After(float seconds, Action action)
        {
            yield return new WaitForSecondsRealtime(seconds);
            action();
        }
    }
}

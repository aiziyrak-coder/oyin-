using System.Collections.Generic;
using System.IO;
using System.Linq;
using CraDev.CharacterCreation;
using CraDev.MainMenu;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static CraDev.EditorTools.UiBuild;

namespace CraDev.EditorTools
{
    /// <summary>
    /// Bosh menyu - virtual dunyoga kirish joyi, foydalanuvchi konsept rasmi bo'yicha (aynan shunday):
    /// orqa fon - konsept rasmning o'zi (yozuvlar va qahramon o'chirilgan, tiniq kattalashtirilgan),
    /// uning ustida rasmdagi platformada o'yinchining 3D qahramoni, hamma interfeys elementlari rasmdagi joyida
    /// (LobbyLayout - rasm o'lchovlari).
    ///
    /// Qatlamlar: BackgroundCamera (fon rasmi, Screen Space - Camera) -> Main Camera (faqat 3D qahramon va uning
    /// soyasi) -> Overlay Canvas (interfeys). Fon ham, interfeysning "sahna" qismi ham ekranni 16:9 nisbatda
    /// qoplaydi (AspectRatioFitter.EnvelopeParent), kamera LobbyCamera bilan xuddi shunday kesiladi.
    /// </summary>
    static partial class CraDevSceneBuilder
    {
        const string LobbyArt = "Assets/CraDev/MainMenu/Background/";
        const string LobbyZones = "Assets/CraDev/MainMenu/Zones/";
        const float RefScale = 1.5f; // konsept rasm 1280x720 -> UI 1920x1080

        static readonly Color GlassFill = new Color(0.07f, 0.09f, 0.13f, 0.52f);
        static readonly Color GlassBorder = new Color(1f, 1f, 1f, 0.16f);
        static readonly Color SoftWhite = new Color(1f, 1f, 1f, 0.86f);

        /// <summary>Zona: kalit, ikonka va rasmdagi binosi (belgi shu nuqta ustida chiqadi, rasm pikselida).</summary>
        static readonly (string id, string icon, Vector2 building)[] LobbyZonesList =
        {
            ("shops", "Bag", new Vector2(430f, 150f)),
            ("education", "Cap", new Vector2(526f, 305f)),
            ("business", "Chart", new Vector2(1142f, 186f)),
            ("entertainment", "Gamepad", new Vector2(980f, 262f)),
            ("community", "People", new Vector2(1160f, 286f)),
        };

        static void BuildMainMenu()
        {
            var kit = Kit();
            var maleIdle = IdleController("m_idle_neutral_01", "Idle_Male");
            var femaleIdle = IdleController("f_idle_neutral_01", "Idle_Female");

            var root = NewUiScene(out var scene);
            root.gameObject.AddComponent<GraphicRaycaster>();
            root.GetComponent<Canvas>().sortingOrder = 10;
            CreateEventSystem();

            // ---------- 1. Fon: konsept rasm (orqadagi kamera) ----------
            var backgroundCamera = new GameObject("BackgroundCamera").AddComponent<Camera>();
            backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
            backgroundCamera.backgroundColor = Color.black;
            backgroundCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
            backgroundCamera.depth = -1;
            var backgroundCanvasGo = new GameObject("BackgroundCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            backgroundCanvasGo.layer = LayerMask.NameToLayer("UI");
            var backgroundCanvas = backgroundCanvasGo.GetComponent<Canvas>();
            backgroundCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            backgroundCanvas.worldCamera = backgroundCamera;
            backgroundCanvas.planeDistance = 10f;
            var bgScaler = backgroundCanvasGo.GetComponent<CanvasScaler>();
            bgScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            bgScaler.referenceResolution = ReferenceResolution;
            bgScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var backgroundStage = CreateStage(backgroundCanvasGo.transform, "BackgroundStage");
            var picture = new GameObject("Picture", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            picture.gameObject.layer = LayerMask.NameToLayer("UI");
            picture.rectTransform.SetParent(backgroundStage, false);
            Stretch(picture.rectTransform);
            picture.texture = LobbyBackground();
            picture.raycastTarget = false;

            // ---------- 2. 3D qahramon: rasmdagi platformada ----------
            var camera = Object.FindFirstObjectByType<Camera>();
            if (camera == backgroundCamera)
                camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c => c != backgroundCamera);
            var (turntable, fov) = BuildLobbyStage(camera);

            // Qahramonni sichqoncha bilan aylantirish uchun (tugmalar ostida)
            var stageInput = CreateFullscreen("StageInput", root, new Color(0f, 0f, 0f, 0f));
            stageInput.raycastTarget = true;

            // Rasmga bog'langan interfeys (nom yorlig'i, minora yozuvi, zona belgilari)
            var stage = CreateStage(root, "Stage");

            // Chap tomon elementlari bitta guruhda (kirish animatsiyasi uchun)
            var menu = CreateRect("Menu", root);
            Stretch(menu);
            var menuGroup = menu.gameObject.AddComponent<CanvasGroup>();

            // ---------- 3. Chap tomon ----------
            CreateBrandAt(root, kit, LobbyLayout.Get("brand_icon"), LobbyLayout.Get("brand_text"));

            var welcome = Localized(CreateLabel("Welcome", menu, kit.Medium, "", 16, Color.white, TextAnchor.MiddleLeft), "menu.welcome", spaced: true);
            PlaceText(welcome, "welcome_label", Vector2.up);
            var nickname = CreateLabel("Nickname", menu, kit.Display, "Lynxos", 80, Color.white, TextAnchor.MiddleLeft);
            nickname.supportRichText = true;
            PlaceText(nickname, "title_name", Vector2.up, reference: "Lynxos");
            var tagline1 = Localized(CreateLabel("Tagline1", menu, kit.Medium, "", 18, Color.white, TextAnchor.MiddleLeft), "menu.tagline1", spaced: true);
            PlaceText(tagline1, "tagline1", Vector2.up);
            var tagline2 = Localized(CreateLabel("Tagline2", menu, kit.Medium, "", 18, Color.white, TextAnchor.MiddleLeft), "menu.tagline2", spaced: true);
            PlaceText(tagline2, "tagline2", Vector2.up);
            var description = Localized(CreateLabel("Description", menu, kit.Medium, "", 15, SoftWhite, TextAnchor.UpperLeft), "menu.description");
            description.lineSpacing = 1.05f;
            PlaceText(description, "description", Vector2.up, grow: 60f);

            var enter = CreateLobbyButton("Enter", menu, "btn_enter", "menu.enter", DrawnIcon("Play"), kit, primary: true);
            var customize = CreateLobbyButton("Customize", menu, "btn_customize", "menu.customize", kit.Icon("User"), kit);
            var settingsButton = CreateLobbyButton("Settings", menu, "btn_settings", "menu.settings", DrawnIcon("Gear"), kit);
            var help = CreateLobbyButton("Help", menu, "btn_help", "menu.help", DrawnIcon("Help"), kit);
            var quit = CreateLobbyButton("Quit", menu, "btn_quit", "menu.quit", DrawnIcon("Exit"), kit);

            // Pastki chap: holat va versiya
            var statusDot = CreateImage("StatusDot", root, kit.PillFill, Vector2.zero, Vector2.zero, new Color32(34, 197, 94, 255));
            PlaceRef(statusDot.rectTransform, LobbyLayout.Get("status_dot"), Vector2.zero);
            var statusText = CreateLabel("StatusText", root, kit.Medium, "", 13, SoftWhite, TextAnchor.MiddleLeft);
            PlaceText(statusText, "status_text", Vector2.zero, reference: "Online");
            var version = CreateLabel("Version", root, kit.Medium, "v0.1.0", 13, SoftWhite, TextAnchor.MiddleLeft);
            PlaceText(version, "version_text", Vector2.zero, reference: "v0.1.0");

            // ---------- 4. Yuqori o'ng: til, bildirishnoma, profil ----------
            // Rasmdagidek: til va qo'ng'iroq fonsiz (faqat ikonka va matn)
            var language = CreatePill("Language", root, "lang_pill", kit, glass: false);
            TintButton(language);
            PlaceIcon(language.transform, "lang_globe", "lang_pill", DrawnIcon("Globe"));
            var languageLabel = CreateLabel("Code", language.transform, kit.SemiBold, "UZ", 14, Color.white, TextAnchor.MiddleLeft);
            FitFont(languageLabel, LobbyLayout.Get("lang_text").Width * RefScale, "UZ");
            PlaceInside(languageLabel.rectTransform, "lang_text", "lang_pill", grow: 16f);
            PlaceIcon(language.transform, "lang_chevron", "lang_pill", DrawnIcon("Chevron"));
            Set(language.gameObject.AddComponent<LanguageToggle>(), "label", languageLabel);

            var bell = CreatePill("Bell", root, "bell_btn", kit, glass: false);
            var bellButton = TintButton(bell);
            var bellIcon = CreateImage("Icon", bell.transform, DrawnIcon("Bell"), Vector2.zero, Vector2.zero, Color.white);
            bellIcon.rectTransform.sizeDelta = Vector2.one * Mathf.Max(bell.rectTransform.sizeDelta.x, bell.rectTransform.sizeDelta.y);

            var profile = CreatePill("Profile", root, "profile_pill", kit);
            var profileButton = TintButton(profile);
            var avatarBox = LobbyLayout.Get("profile_avatar");
            var avatarMask = CreateSliced("AvatarMask", profile.transform, kit.PillFill, Mathf.Min(avatarBox.Width, avatarBox.Height) * RefScale / 2f, 32f, new Color(0.2f, 0.25f, 0.35f));
            PlaceInside(avatarMask.rectTransform, "profile_avatar", "profile_pill");
            float avatarSide = Mathf.Min(avatarMask.rectTransform.sizeDelta.x, avatarMask.rectTransform.sizeDelta.y);
            avatarMask.rectTransform.sizeDelta = new Vector2(avatarSide, avatarSide);
            avatarMask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var profileThumb = new GameObject("Photo", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            profileThumb.rectTransform.SetParent(avatarMask.transform, false);
            Stretch(profileThumb.rectTransform);
            profileThumb.raycastTarget = false;
            var profileIcon = CreateImage("Icon", avatarMask.transform, kit.Icon("User"), Vector2.one * avatarSide * 0.55f, Vector2.zero, Color.white);
            var profileName = CreateLabel("Name", profile.transform, kit.SemiBold, "Player", 14, Color.white, TextAnchor.MiddleLeft);
            FitFont(profileName, LobbyLayout.Get("profile_name").Width * RefScale, "Lynxos_user");
            PlaceInside(profileName.rectTransform, "profile_name", "profile_pill", grow: 40f);
            PlaceIcon(profile.transform, "profile_chevron", "profile_pill", DrawnIcon("Chevron"));

            // ---------- 5. Nom yorlig'i (qahramon boshi ustida, MainMenuScreen joylaydi), minora yozuvi, zona belgilari ----------
            // Fon to'qroq: yorliq och osmon ustida turadi, shaffof shishada yozuv o'qilmay qoladi
            var plate = CreateSliced("Nameplate", root, kit.RoundFill, 10f, 24f, new Color(0.05f, 0.07f, 0.11f, 0.78f));
            Place(plate.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(110f, 42f));
            GlassBorderFor(plate.transform, kit, 10f, false);
            var plateDot = CreateImage("Dot", plate.transform, kit.PillFill, Vector2.zero, Vector2.zero, LobbyLayout.Get("nameplate_dot").Color);
            Place(plateDot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(15f, 7f), new Vector2(8f, 8f));
            var plateName = CreateLabel("Name", plate.transform, kit.SemiBold, "Player", 15, LobbyLayout.Get("nameplate_name").Color, TextAnchor.MiddleLeft);
            Place(plateName.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, 7f), new Vector2(320f, 20f));
            var plateStatus = CreateLabel("Status", plate.transform, kit.Medium, "", 11, new Color(0.76f, 0.8f, 0.88f), TextAnchor.MiddleLeft);
            Place(plateStatus.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, -9f), new Vector2(320f, 16f));

            // Minoradagi yozuv: rasmdagisi o'chirilgan, o'rniga dunyo nomi
            var towerBox = LobbyLayout.Get("tower_sign");
            var tower = CreateRect("TowerSign", stage);
            PlaceStage(tower, towerBox);
            var towerName = CreateLabel("Name", tower, kit.Display, "CRADEV", 30, Color.white, TextAnchor.MiddleCenter);
            towerName.rectTransform.anchorMin = new Vector2(0f, 0.35f);
            towerName.rectTransform.anchorMax = Vector2.one;
            towerName.rectTransform.offsetMin = towerName.rectTransform.offsetMax = Vector2.zero;
            FitFont(towerName, towerBox.Width * RefScale * 0.95f, "CRADEV");
            var towerSub = CreateLabel("Sub", tower, kit.Medium, Loc.Spaced("VIRTUAL WORLD"), 9, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter);
            towerSub.rectTransform.anchorMin = Vector2.zero;
            towerSub.rectTransform.anchorMax = new Vector2(1f, 0.35f);
            towerSub.rectTransform.offsetMin = towerSub.rectTransform.offsetMax = Vector2.zero;
            FitFont(towerSub, towerBox.Width * RefScale * 0.75f, Loc.Spaced("VIRTUAL WORLD"));
            towerName.horizontalOverflow = towerSub.horizontalOverflow = HorizontalWrapMode.Overflow;
            foreach (var t in new[] { towerName, towerSub })
                t.gameObject.AddComponent<Shadow>().effectColor = new Color(0.25f, 0.55f, 1f, 0.55f);

            var markers = new List<CanvasGroup>();
            foreach (var (id, icon, building) in LobbyZonesList)
                markers.Add(CreateZoneMarker(stage, id, icon, building, kit));

            // ---------- 6. Pastda: zona kartalari ----------
            var cardsRoot = CreateRect("Zones", root);
            Stretch(cardsRoot);
            var cardsGroup = cardsRoot.gameObject.AddComponent<CanvasGroup>();
            var zoneButtons = new List<Button>();
            var zoneBorders = new List<Image>();
            for (int i = 0; i < LobbyZonesList.Length; i++)
            {
                var (id, icon, _) = LobbyZonesList[i];
                var (button, border) = CreateLobbyCard(cardsRoot, i + 1, id, DrawnIcon(icon), ZonePicture(i + 1, id), kit);
                zoneButtons.Add(button);
                zoneBorders.Add(border);
            }
            var next = CreatePill("Next", cardsRoot, "cards_next_btn", kit);
            var nextButton = TintButton(next);
            var nextIcon = CreateImage("Icon", next.transform, DrawnIcon("ArrowRight"), Vector2.zero, Vector2.zero, Color.white);
            nextIcon.rectTransform.sizeDelta = Vector2.one * next.rectTransform.sizeDelta.y * 0.46f;

            // Qisqa xabar (kartalar ustida)
            var toast = CreateSliced("Toast", root, kit.PillFill, 20f, 32f, new Color(0.07f, 0.09f, 0.13f, 0.8f));
            var card1 = LobbyLayout.Get("card_1");
            var card5 = LobbyLayout.Get("card_5");
            Place(toast.rectTransform, Vector2.zero, new Vector2(0.5f, 0f), new Vector2((card1.X0 + card5.X1) / 2f * RefScale, (LobbyLayout.Height - card1.Y0) * RefScale + 14f), new Vector2(640f, 40f));
            GlassBorderFor(toast.transform, kit, 20f, true);
            var toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;
            var toastText = CreateLabel("Text", toast.transform, kit.SemiBold, "", 15, Color.white, TextAnchor.MiddleCenter);
            Stretch(toastText.rectTransform);

            // ---------- 7. Qahramon, oynalar, boshqaruvchi ----------
            var viewer = stageInput.gameObject.AddComponent<AvatarViewer>();
            Set(viewer, "stageCamera", camera);
            Set(viewer, "turntable", turntable);
            Set(viewer, "maleIdle", maleIdle);
            Set(viewer, "femaleIdle", femaleIdle);
            Set(viewer, "facePaint", FacePaintMaterial());
            Set(viewer, "driveCamera", false);
            SetArray(viewer, "viewButtons", new Object[0]);
            SetArray(viewer, "viewFills", new Object[0]);
            SetArray(viewer, "viewLabels", new Object[0]);

            var settings = BuildSettingsPanel(root, kit);
            var dialog = BuildConfirmDialog(root, kit);
            AddUiSounds();
            var fader = CreateFullscreen("Fader", root, Color.black);

            var director = new GameObject("MainMenuDirector");
            var screen = director.AddComponent<MainMenuScreen>();
            SetAvatars(screen, new Dictionary<string, Sprite>(), LoadFaceMaps());
            Set(screen, "viewer", viewer);
            Set(screen, "stageCamera", camera);
            Set(screen, "nameplate", plate.rectTransform);
            Set(screen, "nicknameText", nickname);
            Set(screen, "profileNameText", profileName);
            Set(screen, "profileThumb", profileThumb);
            Set(screen, "profileIcon", profileIcon);
            Set(screen, "nameplateName", plateName);
            Set(screen, "nameplateStatus", plateStatus);
            Set(screen, "nameplateDot", plateDot);
            Set(screen, "statusDot", statusDot);
            Set(screen, "statusText", statusText);
            Set(screen, "versionText", version);
            Set(screen, "toastGroup", toastGroup);
            Set(screen, "toastText", toastText);
            Set(screen, "menu", menu);
            Set(screen, "menuGroup", menuGroup);
            Set(screen, "cardsGroup", cardsGroup);
            Set(screen, "enterButton", enter);
            Set(screen, "customizeButton", customize);
            Set(screen, "settingsButton", settingsButton);
            Set(screen, "helpButton", help);
            Set(screen, "quitButton", quit);
            Set(screen, "bellButton", bellButton);
            Set(screen, "profileButton", profileButton);
            SetStringArray(screen, "zoneIds", LobbyZonesList.Select(z => z.id).ToArray());
            SetArray(screen, "zoneCards", zoneButtons.ToArray());
            SetArray(screen, "zoneBorders", zoneBorders.ToArray());
            SetArray(screen, "zoneMarkers", markers.ToArray());
            Set(screen, "nextZoneButton", nextButton);
            Set(screen, "dialog", dialog);
            Set(screen, "settings", settings);
            Set(screen, "fader", fader);

            Save(scene, MenuScene);
        }

        // ---------------------------------------------------------------- 3D qism: kamera rasmga moslanadi

        /// <summary>
        /// Qahramon (0,0,0) da turadi. Kamera shunday tanlanadiki: oyog'i va boshi rasmdagi qahramon joyiga,
        /// ostidagi ko'rinmas platforma (radius R) rasmdagi platforma ellipsiga tushadi. Noma'lumlar (masofa,
        /// balandlik, egilish, ko'rish burchagi, yon siljish, R) sonli usulda topiladi.
        /// </summary>
        static (Transform turntable, float fov) BuildLobbyStage(Camera camera)
        {
            const float bodyHeight = 1.85f; // M1 avatari: oyoq tagidan soch tepasigacha
            float[] best = SolveLobbyCamera(bodyHeight);
            float distance = best[0], height = best[1], pitch = best[2], fov = best[3], side = best[4], radius = best[5];

            camera.clearFlags = CameraClearFlags.Depth;
            camera.depth = 0;
            camera.cullingMask = ~(1 << LayerMask.NameToLayer("UI"));
            camera.fieldOfView = fov;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            camera.allowHDR = false;
            camera.transform.SetPositionAndRotation(new Vector3(side, height, -distance), Quaternion.Euler(pitch, 0f, 0f));
            Set(camera.gameObject.AddComponent<LobbyCamera>(), "fieldOfView16x9", fov);

            // Yorug'lik: rasmdagidek kechki iliq quyosh (oldindan-o'ngdan, rasmda quyosh o'ng tomonda) va
            // chap-orqadan sovuq ko'k chiziq yorug'lik
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.56f, 0.64f);
            RenderSettings.ambientEquatorColor = new Color(0.38f, 0.36f, 0.34f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.21f, 0.21f);
            RenderSettings.fog = false;
            RenderSettings.skybox = null;
            var sun = NewLight("Sun", null, LightType.Directional, new Color(1f, 0.86f, 0.68f), 1.5f, Quaternion.Euler(36f, -34f, 0f));
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 1f;
            sun.shadowNormalBias = 0.3f;
            NewLight("Rim", null, LightType.Directional, new Color(0.62f, 0.78f, 1f), 0.75f, Quaternion.Euler(20f, 155f, 0f));

            var turntable = new GameObject("Turntable").transform;
            turntable.position = Vector3.zero;

            // Ko'rinmas pol: qahramon soyasi rasmdagi platformaga tushadi
            var catcher = GameObject.CreatePrimitive(PrimitiveType.Quad);
            catcher.name = "ShadowCatcher";
            Object.DestroyImmediate(catcher.GetComponent<Collider>());
            catcher.transform.SetPositionAndRotation(new Vector3(0f, 0.002f, 0f), Quaternion.Euler(90f, 0f, 0f));
            catcher.transform.localScale = Vector3.one * radius * 2.2f;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/CraDev/MainMenu/Shaders/ShadowCatcher.shader");
            string materialPath = "Assets/CraDev/MainMenu/ShadowCatcher.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            var renderer = catcher.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Debug.Log($"[CraDev] Lobby kamerasi: masofa {distance:0.00} m, balandlik {height:0.00} m, egilish {pitch:0.0}°, FOV {fov:0.0}°, yon {side:0.00}, platforma R {radius:0.00} m");
            return (turntable, fov);
        }

        /// <summary>Kamera parametrlarini rasmdagi qahramon va platformaga moslash (koordinata bo'yicha tushish).</summary>
        static float[] SolveLobbyCamera(float bodyHeight)
        {
            const float aspect = LobbyLayout.Width / LobbyLayout.Height;
            float Error(float[] p)
            {
                var position = new Vector3(p[4], p[1], -p[0]);
                var rotation = Quaternion.Euler(p[2], 0f, 0f);
                float t = Mathf.Tan(p[3] * 0.5f * Mathf.Deg2Rad);
                Vector2 Project(Vector3 world)
                {
                    var local = Quaternion.Inverse(rotation) * (world - position);
                    float x = local.x / (local.z * t * aspect), y = local.y / (local.z * t);
                    return new Vector2((x + 1f) / 2f * LobbyLayout.Width, (1f - (y + 1f) / 2f) * LobbyLayout.Height);
                }
                float r = p[5];
                var feet = Project(Vector3.zero);
                var head = Project(new Vector3(0f, bodyHeight, 0f));
                var left = Project(new Vector3(-r, 0f, 0f));
                var right = Project(new Vector3(r, 0f, 0f));
                var back = Project(new Vector3(0f, 0f, r));
                var front = Project(new Vector3(0f, 0f, -r));
                float e = Sq(feet.y - LobbyLayout.CharacterFeetY) + Sq(head.y - LobbyLayout.CharacterHeadY) + Sq(feet.x - LobbyLayout.CharacterX);
                e += Sq(left.x - (LobbyLayout.PlatformX - LobbyLayout.PlatformOuterRx)) + Sq(right.x - (LobbyLayout.PlatformX + LobbyLayout.PlatformOuterRx));
                e += Sq(back.y - (LobbyLayout.PlatformY - LobbyLayout.PlatformOuterRy)) + Sq(front.y - (LobbyLayout.PlatformY + LobbyLayout.PlatformOuterRy));
                return e;
            }
            // [masofa, balandlik, egilish, fov, yon, R]
            var p0 = new[] { 7f, 1.2f, 4f, 30f, 0f, 1.2f };
            var step = new[] { 2f, 0.5f, 4f, 8f, 0.5f, 0.4f };
            var min = new[] { 2f, 0.1f, -15f, 10f, -3f, 0.3f };
            var max = new[] { 30f, 4f, 30f, 70f, 3f, 4f };
            float best = Error(p0);
            for (int iteration = 0; iteration < 600; iteration++)
            {
                bool improved = false;
                for (int k = 0; k < p0.Length; k++)
                    foreach (float sign in new[] { 1f, -1f })
                    {
                        var candidate = (float[])p0.Clone();
                        candidate[k] = Mathf.Clamp(candidate[k] + sign * step[k], min[k], max[k]);
                        float e = Error(candidate);
                        if (e < best)
                        {
                            best = e;
                            p0 = candidate;
                            improved = true;
                        }
                    }
                if (!improved)
                    for (int k = 0; k < step.Length; k++)
                        step[k] *= 0.6f;
            }
            Debug.Log($"[CraDev] Kamera moslash xatosi: o'rtacha {Mathf.Sqrt(best / 7f):0.0} px");
            return p0;
        }

        static float Sq(float v) => v * v;

        // ---------------------------------------------------------------- Joylashtirish (rasm pikseli -> UI)

        /// <summary>Ekranni 16:9 nisbatda qoplaydigan "sahna" (fon rasmi bilan bir xil kesiladi).</summary>
        static RectTransform CreateStage(Transform parent, string name)
        {
            var stage = CreateRect(name, parent);
            Stretch(stage);
            var fitter = stage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = LobbyLayout.Width / LobbyLayout.Height;
            return stage;
        }

        /// <summary>Ekran chetiga bog'langan element: rasmdagi o'rni 1.5 barobar (1920x1080); anchor (0,1) - chap-yuqori va h.k.</summary>
        static void PlaceRef(RectTransform rect, LobbyLayout.Item item, Vector2 anchor)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            var center = item.Center * RefScale;
            var anchorPoint = new Vector2(anchor.x * 1920f, (1f - anchor.y) * 1080f);
            rect.anchoredPosition = new Vector2(center.x - anchorPoint.x, -(center.y - anchorPoint.y));
            rect.sizeDelta = new Vector2(item.Width, item.Height) * RefScale;
            rect.localRotation = Quaternion.Euler(0f, 0f, item.Rotation); // o'lchovda musbat = soat strelkasiga qarshi (Unity +Z)
        }

        /// <summary>Sahna (16:9) ichidagi element: nisbiy anchorlar, sahna bilan birga kattalashadi.</summary>
        static void PlaceStage(RectTransform rect, LobbyLayout.Item item)
        {
            rect.anchorMin = new Vector2(item.X0 / LobbyLayout.Width, 1f - item.Y1 / LobbyLayout.Height);
            rect.anchorMax = new Vector2(item.X1 / LobbyLayout.Width, 1f - item.Y0 / LobbyLayout.Height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>Ota element (tugma, pill) ichida: ota markaziga nisbatan. grow - matn qirqilmasligi uchun o'ngga zaxira.</summary>
        static void PlaceInside(RectTransform rect, string id, string parentId, float grow = 0f)
        {
            var item = LobbyLayout.Get(id);
            var parent = LobbyLayout.Get(parentId);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            var offset = (item.Center - parent.Center) * RefScale;
            rect.sizeDelta = new Vector2(item.Width, item.Height) * RefScale + new Vector2(grow, grow * 0.4f);
            rect.anchoredPosition = new Vector2(offset.x + grow / 2f, -offset.y);
        }

        static Image PlaceIcon(Transform parent, string id, string parentId, Sprite sprite)
        {
            var icon = CreateImage(id, parent, sprite, Vector2.zero, Vector2.zero, Color.white);
            PlaceInside(icon.rectTransform, id, parentId);
            float side = Mathf.Max(icon.rectTransform.sizeDelta.x, icon.rectTransform.sizeDelta.y);
            icon.rectTransform.sizeDelta = new Vector2(side, side);
            return icon;
        }

        /// <summary>
        /// Matnni rasmdagi joyiga qo'yadi: shrift o'lchami matn (yoki reference) kengligi rasmdagiga teng bo'ladigan
        /// qilib tanlanadi. Matnning chap (o'ng, markaz) cheti rasmdagidek; to'rtburchak qirqilmasligi uchun kattalashtiriladi.
        /// </summary>
        static void PlaceText(Text label, string id, Vector2 anchor, string reference = null, float grow = 40f)
        {
            var item = LobbyLayout.Get(id);
            // Burilgan matn: o'lchovdagi quti burilgan holatniki - burilmagan kenglik/balandlikni topamiz
            float width = item.Width, height = item.Height;
            float angle = Mathf.Abs(item.Rotation) * Mathf.Deg2Rad, cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
            float det = cos * cos - sin * sin;
            if (angle > 0.01f && det > 0.3f)
            {
                width = (item.Width * cos - item.Height * sin) / det;
                height = Mathf.Max(8f, (item.Height * cos - item.Width * sin) / det);
            }
            FitFont(label, width * RefScale, reference ?? label.text);
            PlaceRef(label.rectTransform, item, anchor);
            var rect = label.rectTransform;
            rect.sizeDelta = new Vector2(width, height) * RefScale;
            var a = label.alignment;
            bool left = a == TextAnchor.UpperLeft || a == TextAnchor.MiddleLeft || a == TextAnchor.LowerLeft;
            bool right = a == TextAnchor.UpperRight || a == TextAnchor.MiddleRight || a == TextAnchor.LowerRight;
            bool upper = a == TextAnchor.UpperLeft || a == TextAnchor.UpperCenter || a == TextAnchor.UpperRight;
            rect.sizeDelta += new Vector2(grow * 2f, grow);
            rect.anchoredPosition += new Vector2(left ? grow : right ? -grow : 0f, upper ? -grow / 2f : 0f);
        }

        /// <summary>Shrift o'lchamini sample matnning eng uzun qatori kengligi targetWidth ga teng bo'ladigan qilib tanlaydi.</summary>
        static void FitFont(Text label, float targetWidth, string sample)
        {
            string saved = label.text;
            label.text = string.IsNullOrEmpty(sample) ? saved : sample;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.fontSize = 100;
            float width = label.preferredWidth;
            if (width > 1f && targetWidth > 1f)
                label.fontSize = Mathf.Clamp(Mathf.RoundToInt(100f * targetWidth / width), 8, 300);
            label.text = saved;
        }

        /// <summary>Pill (yumaloq) tugma. glass = false: foni ko'rinmaydi, lekin bosiladi (rasmdagi til va qo'ng'iroq).</summary>
        static Image CreatePill(string name, Transform parent, string id, UiKit kit, bool glass = true)
        {
            var item = LobbyLayout.Get(id);
            float radius = Mathf.Min(item.Width, item.Height) * RefScale / 2f;
            var fill = CreateSliced(name, parent, kit.PillFill, radius, 32f, glass ? GlassFill : new Color(1f, 1f, 1f, 0f));
            PlaceRef(fill.rectTransform, item, Anchor(item));
            fill.raycastTarget = true;
            if (glass)
                GlassBorderFor(fill.transform, kit, radius, true);
            return fill;
        }

        /// <summary>Element qaysi burchakka bog'lanadi: rasmdagi o'rniga qarab (chap/o'rta/o'ng, yuqori/past).</summary>
        static Vector2 Anchor(LobbyLayout.Item item)
        {
            var c = item.Center;
            float x = c.x < LobbyLayout.Width * 0.33f ? 0f : c.x > LobbyLayout.Width * 0.66f ? 1f : 0.5f;
            float y = c.y < LobbyLayout.Height * 0.5f ? 1f : 0f;
            return new Vector2(x, y);
        }

        /// <summary>Chap menyu tugmasi: shisha (yoki ko'k gradient), chapda ikonka, matn; asosiysida o'ngda o'q.</summary>
        static Button CreateLobbyButton(string name, Transform parent, string id, string key, Sprite icon, UiKit kit, bool primary = false)
        {
            var item = LobbyLayout.Get(id);
            float radius = Mathf.Min(10f, item.Height * RefScale / 4f);
            var fill = CreateSliced(name, parent, primary ? GradientSprite() : kit.RoundFill, radius, 24f, primary ? Color.white : GlassFill);
            PlaceRef(fill.rectTransform, item, Vector2.up);
            fill.raycastTarget = true;
            var border = CreateSliced("Border", fill.transform, kit.RoundStroke, radius, 24f, primary ? new Color(1f, 1f, 1f, 0.3f) : GlassBorder);
            Stretch(border.rectTransform);
            PlaceIcon(fill.transform, id + "_icon", id, icon);
            var label = Localized(CreateLabel("Label", fill.transform, primary ? kit.SemiBold : kit.Medium, "", 16, Color.white, TextAnchor.MiddleLeft), key);
            FitFont(label, LobbyLayout.Get(id + "_label").Width * RefScale, label.text);
            PlaceInside(label.rectTransform, id + "_label", id, grow: 160f);
            if (primary)
            {
                PlaceIcon(fill.transform, id + "_arrow", id, DrawnIcon("ArrowRight"));
                var button = fill.gameObject.AddComponent<Button>();
                button.targetGraphic = fill;
                var colors = button.colors;
                colors.highlightedColor = new Color(1.12f, 1.12f, 1.15f, 1f);
                colors.pressedColor = new Color(0.88f, 0.88f, 0.9f, 1f);
                colors.selectedColor = Color.white;
                colors.fadeDuration = 0.1f;
                button.colors = colors;
                return button;
            }
            return TintButton(fill);
        }

        /// <summary>Zona kartasi: rasmdagidek - tepada rasm, pastda ikonka, nomi va izoh.</summary>
        static (Button, Image) CreateLobbyCard(Transform parent, int index, string zoneId, Sprite icon, Sprite picture, UiKit kit)
        {
            string id = "card_" + index;
            var item = LobbyLayout.Get(id);
            var fill = CreateSliced("Card_" + zoneId, parent, kit.RoundFill, 10f, 24f, GlassFill);
            PlaceRef(fill.rectTransform, item, Vector2.zero);
            fill.raycastTarget = true;

            var mask = CreateRect("Picture", fill.transform);
            PlaceInside(mask, id + "_image", id);
            mask.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(4, 4);
            var image = CreateImage("Image", mask, picture, Vector2.zero, Vector2.zero, Color.white);
            Stretch(image.rectTransform);

            // Ikonka, nom va izoh: birinchi kartaning o'lchovi hamma kartaga (kartaga nisbatan joyi bir xil)
            var reference = LobbyLayout.Get("card_1");
            Vector2 Offset(LobbyLayout.Item part) => (part.Center - reference.Center) * RefScale;
            var iconItem = LobbyLayout.Get("card_1_icon");
            float iconSide = Mathf.Max(iconItem.Width, iconItem.Height) * RefScale;
            var iconImage = CreateImage("Icon", fill.transform, icon, new Vector2(iconSide, iconSide), Vector2.zero, Color.white);
            iconImage.rectTransform.anchoredPosition = new Vector2(Offset(iconItem).x, -Offset(iconItem).y);

            var titleItem = LobbyLayout.Get("card_1_title");
            var title = Localized(CreateLabel("Title", fill.transform, kit.SemiBold, "", 14, Color.white, TextAnchor.MiddleLeft), "zone." + zoneId);
            FitFont(title, titleItem.Width * RefScale, "Magazinlar");
            title.rectTransform.sizeDelta = new Vector2(titleItem.Width * RefScale + 140f, titleItem.Height * RefScale + 10f);
            title.rectTransform.anchoredPosition = new Vector2(Offset(titleItem).x + 70f, -Offset(titleItem).y);
            var subItem = LobbyLayout.Get("card_1_subtitle");
            var sub = Localized(CreateLabel("Subtitle", fill.transform, kit.Medium, "", 11, new Color(1f, 1f, 1f, 0.66f), TextAnchor.MiddleLeft), "zone." + zoneId + ".sub");
            FitFont(sub, subItem.Width * RefScale, "Xarid va brendlar");
            sub.rectTransform.sizeDelta = new Vector2(subItem.Width * RefScale + 140f, subItem.Height * RefScale + 10f);
            sub.rectTransform.anchoredPosition = new Vector2(Offset(subItem).x + 70f, -Offset(subItem).y);

            var border = CreateSliced("Border", fill.transform, kit.RoundStroke, 10f, 24f, GlassBorder);
            Stretch(border.rectTransform);
            return (TintButton(fill), border);
        }

        /// <summary>Rasmdagi bino ustidagi belgi: kichik shisha yorliq (ikonka + zona nomi) va pastga ingichka chiziq.</summary>
        static CanvasGroup CreateZoneMarker(RectTransform stage, string zoneId, string icon, Vector2 building, UiKit kit)
        {
            var marker = CreateRect("Marker_" + zoneId, stage);
            marker.anchorMin = marker.anchorMax = new Vector2(building.x / LobbyLayout.Width, 1f - building.y / LobbyLayout.Height);
            marker.pivot = new Vector2(0.5f, 0f);
            marker.sizeDelta = new Vector2(230f, 86f);
            var group = marker.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            var pill = CreateSliced("Pill", marker, kit.PillFill, 20f, 32f, new Color(0.08f, 0.12f, 0.22f, 0.84f));
            Place(pill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(230f, 40f));
            var ring = CreateSliced("Border", pill.transform, kit.PillStroke, 20f, 32f, new Color(0.45f, 0.72f, 1f, 0.9f));
            Stretch(ring.rectTransform);
            var image = CreateImage("Icon", pill.transform, DrawnIcon(icon), new Vector2(18f, 18f), Vector2.zero, Color.white);
            Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(24f, 0f), new Vector2(18f, 18f));
            var label = Localized(CreateLabel("Title", pill.transform, kit.SemiBold, "", 14, Color.white, TextAnchor.MiddleLeft), "zone." + zoneId);
            Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(180f, 22f));
            var line = CreateImage("Line", marker, null, new Vector2(2f, 40f), Vector2.zero, new Color(0.45f, 0.72f, 1f, 0.9f));
            Place(line.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(2f, 40f));
            var dot = CreateImage("Dot", marker, kit.PillFill, new Vector2(12f, 12f), Vector2.zero, new Color(0.55f, 0.8f, 1f, 1f));
            Place(dot.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12f, 12f));
            return group;
        }

        // ---------------------------------------------------------------- Rasmlar

        /// <summary>Fon rasmi (konsept rasmdan tozalangan, 2560x1440): sifatli siqilgan, mipmapsiz.</summary>
        static Texture2D LobbyBackground()
        {
            string path = LobbyArt + "Lobby_Background.png";
            if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                (importer.textureType != TextureImporterType.Default || importer.maxTextureSize != 4096 || importer.mipmapEnabled))
            {
                importer.textureType = TextureImporterType.Default;
                importer.maxTextureSize = 4096;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
                throw new FileNotFoundException("Bosh menyu foni topilmadi: " + path);
            return texture;
        }

        /// <summary>Karta rasmi: konsept rasmdagi karta rasmining o'zi (tiniq kattalashtirilgan).</summary>
        static Sprite ZonePicture(int index, string zoneId)
        {
            string path = LobbyZones + index + "_" + zoneId + ".png";
            return File.Exists(path) ? LoadSprite(path) : null;
        }

        // ---------------------------------------------------------------- Umumiy UI bo'laklari

        static void CreateBrandAt(Transform parent, UiKit kit, LobbyLayout.Item icon, LobbyLayout.Item text)
        {
            float size = Mathf.Max(icon.Width, icon.Height) * RefScale;
            var tile = CreateSliced("BrandTile", parent, kit.RoundFill, size * 0.24f, 24f, kit.Accent);
            PlaceTopLeft(tile.rectTransform, icon.X0 * RefScale, icon.Y0 * RefScale, size, size);
            var glyphSprite = LoadSprite(IntroArt + "CraDev_Glyph.png");
            var glyph = CreateImage("Glyph", tile.transform, glyphSprite, NativeSize(glyphSprite) * (size / 150f), Vector2.zero, Color.white);
            glyph.rectTransform.anchoredPosition = new Vector2(-8f * size / 150f, 0f);
            var label = CreateLabel("BrandName", parent, kit.Display, "Cra<color=#6F8BFF>Dev</color>", 19, Color.white, TextAnchor.MiddleLeft);
            label.supportRichText = true;
            FitFont(label, text.Width * RefScale, "CraDev");
            PlaceTopLeft(label.rectTransform, text.X0 * RefScale, text.Center.y * RefScale - 30f, text.Width * RefScale + 80f, 60f);
        }

        /// <summary>Matnga tarjima kalitini ulaydi (til almashsa o'zi yangilanadi).</summary>
        static Text Localized(Text text, string key, bool spaced = false)
        {
            var localized = text.gameObject.AddComponent<LocalizedText>();
            Set(localized, "key", key);
            Set(localized, "spaced", spaced);
            text.text = spaced ? Loc.Spaced(Loc.T(key)) : Loc.T(key);
            return text;
        }

        static void LocalizeButton(Button button, string key) => Localized(button.transform.Find("Label").GetComponent<Text>(), key);

        /// <summary>Til tugmasi (globus + "UZ"): yuqori o'ng burchakda, bosilganda til almashadi.</summary>
        static void CreateLanguagePill(Transform root, UiKit kit, Vector2 position)
        {
            var fill = CreateSliced("Language", root, kit.PillFill, 22f, 32f, GlassFill);
            Place(fill.rectTransform, Vector2.one, new Vector2(1f, 1f), position, new Vector2(92f, 44f));
            fill.raycastTarget = true;
            GlassBorderFor(fill.transform, kit, 22f, true);
            TintButton(fill);
            var globe = CreateImage("Globe", fill.transform, DrawnIcon("Globe"), new Vector2(18f, 18f), Vector2.zero, Color.white);
            Place(globe.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(18f, 18f));
            var label = CreateLabel("Code", fill.transform, kit.Bold, "UZ", 14, Color.white, TextAnchor.MiddleLeft);
            Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(40f, 20f));
            Set(fill.gameObject.AddComponent<LanguageToggle>(), "label", label);
        }

        static void GlassBorderFor(Transform parent, UiKit kit, float radius, bool pill)
        {
            var border = CreateSliced("Border", parent, pill ? kit.PillStroke : kit.RoundStroke, radius, pill ? 32f : 24f, GlassBorder);
            Stretch(border.rectTransform);
        }

        static Button TintButton(Image target)
        {
            var button = target.gameObject.AddComponent<Button>();
            button.targetGraphic = target;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.7f, 1.7f, 1.9f, 1.3f);
            colors.pressedColor = new Color(1.2f, 1.2f, 1.3f, 1.1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.12f;
            button.colors = colors;
            return button;
        }

        static void SetStringArray(Object target, string field, string[] values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).stringValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Asosiy tugma foni: yumaloq burchakli, chapdan o'ngga ko'kdan moviyga (9-slice, o'rtasi cho'ziladi).</summary>
        static Sprite GradientSprite()
        {
            string path = UiArt + "UI_Round12_Gradient.png";
            if (!File.Exists(path))
            {
                const int w = 256, h = 96, r = 24;
                var from = new Color(0.22f, 0.36f, 1f);
                var to = new Color(0.36f, 0.62f, 1f);
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float cx = Mathf.Clamp(x + 0.5f, r, w - r), cy = Mathf.Clamp(y + 0.5f, r, h - r);
                        float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                        var c = Color.Lerp(from, to, x / (float)(w - 1));
                        c = Color.Lerp(c, Color.white, 0.12f * (y / (float)h));
                        c.a = Mathf.Clamp01(r - d + 0.5f);
                        t.SetPixel(x, y, c);
                    }
                File.WriteAllBytes(path, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path);
            }
            return LoadSlicedSprite(path);
        }
    }
}

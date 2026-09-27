using System.Collections.Generic;
using System.IO;
using System.Linq;
using CraDev.CharacterCreation;
using CraDev.MainMenu;
using CraDev.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static CraDev.EditorTools.UiBuild;

namespace CraDev.EditorTools
{
    /// <summary>
    /// Bosh menyu - virtual dunyoga kirish joyi: 3D shahar (BuildCity) ustida shisha uslubidagi interfeys.
    /// Joylashuv 1920x1080 da: chapda salomlashish va menyu, yuqori o'ngda til/bildirishnoma/profil,
    /// pastda zona kartalari, qahramon yonida nom yorlig'i.
    /// </summary>
    static partial class CraDevSceneBuilder
    {
        static readonly Color GlassFill = new Color(0.05f, 0.07f, 0.11f, 0.55f);
        static readonly Color GlassBorder = new Color(1f, 1f, 1f, 0.14f);
        static readonly Color SoftWhite = new Color(1f, 1f, 1f, 0.82f);

        static void BuildMainMenu()
        {
            var kit = Kit();
            var script = LoadFont("GreatVibes-Regular.ttf");
            var maleIdle = IdleController("m_idle_neutral_01", "Idle_Male");
            var femaleIdle = IdleController("f_idle_neutral_01", "Idle_Female");

            // Zona kartalari uchun rasmlar: shahar alohida sahnada chiziladi
            var sceneForCards = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
            var cardCamera = new GameObject("Tmp").AddComponent<Camera>();
            BuildCity(cardCamera, maleIdle, femaleIdle);
            Object.DestroyImmediate(cardCamera.gameObject);
            Object.DestroyImmediate(Object.FindFirstObjectByType<UnityEngine.Rendering.PostProcessing.PostProcessVolume>()?.gameObject);
            var cardPictures = RenderZoneCards();

            var root = NewUiScene(out var scene);
            root.gameObject.AddComponent<GraphicRaycaster>();
            CreateEventSystem();

            var camera = Object.FindFirstObjectByType<Camera>();
            var city = BuildCity(camera, maleIdle, femaleIdle);
            var menuCamera = camera.gameObject.AddComponent<MenuCamera>();
            Set(menuCamera, "homePosition", city.CameraPosition);
            Set(menuCamera, "homeTarget", city.CameraTarget);
            var lighting = new GameObject("WorldLighting").AddComponent<WorldLighting>();
            Set(lighting, "probe", city.Probe);

            // Qahramonni sichqoncha bilan aylantirish uchun butun ekran qatlami (tugmalar ustida emas)
            var stage = CreateFullscreen("StageInput", root, new Color(0f, 0f, 0f, 0f));
            stage.raycastTarget = true;

            // O'qilishi uchun yumshoq soyalar: chapda va pastda
            var leftShade = CreateImage("LeftShade", root, ShadeSprite(), Vector2.zero, Vector2.zero, new Color(0.02f, 0.03f, 0.06f, 0.78f));
            leftShade.rectTransform.anchorMin = Vector2.zero;
            leftShade.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            leftShade.rectTransform.offsetMin = leftShade.rectTransform.offsetMax = Vector2.zero;
            var bottomShade = CreateImage("BottomShade", root, VerticalShadeSprite(), Vector2.zero, Vector2.zero, new Color(0.02f, 0.03f, 0.06f, 0.7f));
            bottomShade.rectTransform.anchorMin = Vector2.zero;
            bottomShade.rectTransform.anchorMax = new Vector2(1f, 0.32f);
            bottomShade.rectTransform.offsetMin = bottomShade.rectTransform.offsetMax = Vector2.zero;

            // ---------- Yuqori chap: belgi ----------
            CreateBrandAt(root, kit, 72f, 56f);

            // ---------- Chap: salomlashish va menyu ----------
            var menu = CreateRect("Menu", root);
            Place(menu, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110f, -170f), new Vector2(560f, 700f));
            var menuGroup = menu.gameObject.AddComponent<CanvasGroup>();
            Localized(PlaceLabel(menu, kit.Medium, 17, SoftWhite, 0f, 0f, 560f, 24f), "menu.welcome", spaced: true);
            var nickname = PlaceLabel(menu, kit.Display, 86, Color.white, 0f, 26f, 620f, 110f);
            nickname.text = "Player";
            Localized(PlaceLabel(menu, kit.Medium, 19, Color.white, 0f, 146f, 560f, 28f), "menu.tagline1", spaced: true);
            Localized(PlaceLabel(menu, kit.Medium, 19, Color.white, 0f, 176f, 560f, 28f), "menu.tagline2", spaced: true);
            var description = Localized(PlaceLabel(menu, kit.Medium, 16, new Color(1f, 1f, 1f, 0.72f), 0f, 222f, 480f, 52f), "menu.description");
            description.lineSpacing = 1.15f;
            description.horizontalOverflow = HorizontalWrapMode.Wrap;

            var enter = CreateGradientButton("Enter", menu, "menu.enter", DrawnIcon("Play"), 0f, 300f, 390f, 64f, kit);
            var customize = CreateGlassButton("Customize", menu, "menu.customize", kit.Icon("User"), 0f, 380f, 390f, kit);
            var settingsButton = CreateGlassButton("Settings", menu, "menu.settings", DrawnIcon("Gear"), 0f, 442f, 390f, kit);
            var help = CreateGlassButton("Help", menu, "menu.help", DrawnIcon("Help"), 0f, 504f, 390f, kit);
            var quit = CreateGlassButton("Quit", menu, "menu.quit", DrawnIcon("Exit"), 0f, 566f, 390f, kit);

            // Qo'lyozma shior (chap past)
            var motto = Localized(CreateLabel("Motto", root, script, "", 46, new Color(1f, 1f, 1f, 0.92f), TextAnchor.MiddleLeft), "menu.script");
            motto.lineSpacing = 0.85f;
            Place(motto.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(100f, 205f), new Vector2(420f, 110f));
            motto.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 7f);

            // ---------- Pastki chap: server holati va versiya ----------
            var status = CreateRect("Status", root);
            Place(status, Vector2.zero, new Vector2(0f, 0.5f), new Vector2(110f, 44f), new Vector2(360f, 20f));
            var dot = CreateImage("Dot", status, kit.PillFill, new Vector2(9f, 9f), Vector2.zero, Color.green);
            Place(dot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(9f, 9f));
            var statusText = CreateLabel("Text", status, kit.SemiBold, "", 14, SoftWhite, TextAnchor.MiddleLeft);
            Place(statusText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(150f, 20f));
            var version = CreateLabel("Version", status, kit.Medium, "v0.1.0", 13, new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleLeft);
            Place(version.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(150f, 0f), new Vector2(120f, 20f));

            // ---------- Yuqori o'ng: til, bildirishnoma, profil ----------
            var profileFill = CreateSliced("Profile", root, kit.PillFill, 26f, 32f, GlassFill);
            Place(profileFill.rectTransform, Vector2.one, new Vector2(1f, 1f), new Vector2(-64f, -44f), new Vector2(236f, 52f));
            profileFill.raycastTarget = true;
            GlassBorderFor(profileFill.transform, kit, 26f, true);
            var profileButton = TintButton(profileFill);
            var avatarMask = CreateSliced("AvatarMask", profileFill.transform, kit.PillFill, 20f, 32f, new Color(0.2f, 0.25f, 0.35f));
            Place(avatarMask.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(7f, 0f), new Vector2(40f, 40f));
            avatarMask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var profileThumb = new GameObject("Photo", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            profileThumb.rectTransform.SetParent(avatarMask.transform, false);
            Stretch(profileThumb.rectTransform);
            profileThumb.raycastTarget = false;
            var profileIcon = CreateImage("Icon", avatarMask.transform, kit.Icon("User"), new Vector2(20f, 20f), Vector2.zero, Color.white);
            var profileName = CreateLabel("Name", profileFill.transform, kit.SemiBold, "Player", 15, Color.white, TextAnchor.MiddleLeft);
            Place(profileName.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(58f, 0f), new Vector2(140f, 22f));
            var chevron = CreateImage("Chevron", profileFill.transform, DrawnIcon("Chevron"), new Vector2(16f, 16f), Vector2.zero, SoftWhite);
            Place(chevron.rectTransform, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-22f, 0f), new Vector2(16f, 16f));


            var bellFill = CreateSliced("Bell", root, kit.PillFill, 26f, 32f, GlassFill);
            Place(bellFill.rectTransform, Vector2.one, new Vector2(1f, 1f), new Vector2(-64f - 236f - 12f, -44f), new Vector2(52f, 52f));
            bellFill.raycastTarget = true;
            GlassBorderFor(bellFill.transform, kit, 26f, true);
            var bellButton = TintButton(bellFill);
            CreateImage("Icon", bellFill.transform, DrawnIcon("Bell"), new Vector2(22f, 22f), Vector2.zero, Color.white);

            var languageFill = CreateSliced("Language", root, kit.PillFill, 26f, 32f, GlassFill);
            Place(languageFill.rectTransform, Vector2.one, new Vector2(1f, 1f), new Vector2(-64f - 236f - 12f - 52f - 12f, -44f), new Vector2(110f, 52f));
            languageFill.raycastTarget = true;
            GlassBorderFor(languageFill.transform, kit, 26f, true);
            TintButton(languageFill);
            var globe = CreateImage("Globe", languageFill.transform, DrawnIcon("Globe"), new Vector2(20f, 20f), Vector2.zero, Color.white);
            Place(globe.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(20f, 20f));
            var languageLabel = CreateLabel("Code", languageFill.transform, kit.Bold, "UZ", 15, Color.white, TextAnchor.MiddleLeft);
            Place(languageLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(44f, 0f), new Vector2(40f, 22f));
            var languageChevron = CreateImage("Chevron", languageFill.transform, DrawnIcon("Chevron"), new Vector2(14f, 14f), Vector2.zero, SoftWhite);
            Place(languageChevron.rectTransform, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-18f, 0f), new Vector2(14f, 14f));

            var toggle = languageFill.gameObject.AddComponent<LanguageToggle>();
            Set(toggle, "label", languageLabel);

            // Iqtibos (o'ng yuqori, profil ostida): ortida yumshoq soya, orqa fon yorug' bo'lsa ham o'qiladi
            var rightShade = CreateImage("RightShade", root, ShadeSprite(), Vector2.zero, Vector2.zero, new Color(0.02f, 0.03f, 0.06f, 0.8f));
            rightShade.rectTransform.anchorMin = new Vector2(0.66f, 0.6f);
            rightShade.rectTransform.anchorMax = Vector2.one;
            rightShade.rectTransform.offsetMin = rightShade.rectTransform.offsetMax = Vector2.zero;
            rightShade.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            rightShade.transform.SetSiblingIndex(bottomShade.transform.GetSiblingIndex() + 1);
            var quote = Localized(CreateLabel("Quote", root, script, "", 38, new Color(1f, 1f, 1f, 0.9f), TextAnchor.UpperRight), "menu.quote");
            quote.lineSpacing = 0.9f;
            Place(quote.rectTransform, Vector2.one, new Vector2(1f, 1f), new Vector2(-80f, -140f), new Vector2(420f, 110f));
            var quoteBy = CreateLabel("By", root, kit.Medium, Loc.Spaced("CRADEV"), 12, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleRight);
            Place(quoteBy.rectTransform, Vector2.one, new Vector2(1f, 1f), new Vector2(-84f, -256f), new Vector2(300f, 18f));
            var slogan = CreateLabel("Slogan", root, script, "Same You,\nNew World", 44, new Color(1f, 1f, 1f, 0.78f), TextAnchor.MiddleCenter);
            slogan.lineSpacing = 0.85f;
            Place(slogan.rectTransform, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-330f, 330f), new Vector2(360f, 110f));
            slogan.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 8f);

            // ---------- Qahramon yonidagi nom yorlig'i (skript joylashtiradi) ----------
            var plate = CreateSliced("Nameplate", root, kit.RoundFill, 12f, 24f, GlassFill);
            plate.rectTransform.anchorMin = plate.rectTransform.anchorMax = Vector2.zero;
            plate.rectTransform.pivot = new Vector2(0f, 0.5f);
            plate.rectTransform.sizeDelta = new Vector2(176f, 54f);
            GlassBorderFor(plate.transform, kit, 12f, false);
            var plateDot = CreateImage("Dot", plate.transform, kit.PillFill, new Vector2(9f, 9f), Vector2.zero, Color.green);
            Place(plateDot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(18f, 9f), new Vector2(9f, 9f));
            var plateName = CreateLabel("Name", plate.transform, kit.SemiBold, "Player", 15, Color.white, TextAnchor.MiddleLeft);
            Place(plateName.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 9f), new Vector2(140f, 20f));
            var plateStatus = CreateLabel("Status", plate.transform, kit.Medium, "", 12, SoftWhite, TextAnchor.MiddleLeft);
            Place(plateStatus.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, -11f), new Vector2(140f, 18f));

            // ---------- Pastda: zona kartalari ----------
            var cards = CreateRect("Zones", root);
            Place(cards, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(470f, 40f), new Vector2(1380f, 170f));
            var cardsGroup = cards.gameObject.AddComponent<CanvasGroup>();
            var zoneButtons = new List<Button>();
            var zoneBorders = new List<Image>();
            const float cardWidth = 252f, cardGap = 16f;
            for (int i = 0; i < Zones.Length; i++)
            {
                var zone = Zones[i];
                cardPictures.TryGetValue(zone.Id, out var picture);
                var (button, border) = CreateZoneCard(cards, zone, picture, i * (cardWidth + cardGap), cardWidth, kit);
                zoneButtons.Add(button);
                zoneBorders.Add(border);
            }
            var nextFill = CreateSliced("Next", cards, kit.PillFill, 24f, 32f, GlassFill);
            Place(nextFill.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(Zones.Length * (cardWidth + cardGap) + 4f, 0f), new Vector2(48f, 48f));
            nextFill.raycastTarget = true;
            GlassBorderFor(nextFill.transform, kit, 24f, true);
            var next = TintButton(nextFill);
            CreateImage("Icon", nextFill.transform, DrawnIcon("ArrowRight"), new Vector2(20f, 20f), Vector2.zero, Color.white);

            // Qisqa xabar (kartalar ustida)
            var toast = CreateSliced("Toast", root, kit.PillFill, 22f, 32f, GlassFill);
            Place(toast.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(470f + 690f, 250f), new Vector2(620f, 44f));
            GlassBorderFor(toast.transform, kit, 22f, true);
            var toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;
            var toastText = CreateLabel("Text", toast.transform, kit.SemiBold, "", 15, Color.white, TextAnchor.MiddleCenter);
            Stretch(toastText.rectTransform);

            // ---------- Qahramon ----------
            var viewer = stage.gameObject.AddComponent<AvatarViewer>();
            Set(viewer, "stageCamera", camera);
            Set(viewer, "turntable", city.Turntable);
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
            Set(screen, "menuCamera", menuCamera);
            Set(screen, "characterAnchor", city.Turntable);
            Set(screen, "nicknameText", nickname);
            Set(screen, "profileNameText", profileName);
            Set(screen, "profileThumb", profileThumb);
            Set(screen, "profileIcon", profileIcon);
            Set(screen, "nameplate", plate.rectTransform);
            Set(screen, "nameplateName", plateName);
            Set(screen, "nameplateStatus", plateStatus);
            Set(screen, "nameplateDot", plateDot);
            Set(screen, "statusDot", dot);
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
            SetStringArray(screen, "zoneIds", Zones.Select(z => z.Id).ToArray());
            SetVectorArray(screen, "zoneTargets", Zones.Select(z => z.Front).ToArray());
            SetArray(screen, "zoneCards", zoneButtons.ToArray());
            SetArray(screen, "zoneBorders", zoneBorders.ToArray());
            Set(screen, "nextZoneButton", next);
            Set(screen, "dialog", dialog);
            Set(screen, "settings", settings);
            Set(screen, "fader", fader);

            Save(scene, MenuScene);
        }

        // ---------------------------------------------------------------- UI bo'laklari

        static void CreateBrandAt(Transform parent, UiKit kit, float x, float y)
        {
            var tile = CreateSliced("BrandTile", parent, kit.RoundFill, 9f, 24f, kit.Accent);
            PlaceTopLeft(tile.rectTransform, x, y, 38f, 38f);
            var glyphSprite = LoadSprite(IntroArt + "CraDev_Glyph.png");
            var glyph = CreateImage("Glyph", tile.transform, glyphSprite, NativeSize(glyphSprite) * (38f / 150f), Vector2.zero, Color.white);
            glyph.rectTransform.anchoredPosition = new Vector2(-8f * 38f / 150f, 0f);
            var text = CreateLabel("BrandName", parent, kit.Display, "Cra<color=#6F8BFF>Dev</color>", 19, Color.white, TextAnchor.MiddleLeft);
            text.supportRichText = true;
            PlaceTopLeft(text.rectTransform, x + 50f, y, 220f, 38f);
        }

        static Text PlaceLabel(Transform parent, Font font, int size, Color color, float x, float y, float width, float height)
        {
            var label = CreateLabel("Label", parent, font, "", size, color, TextAnchor.MiddleLeft);
            PlaceTopLeft(label.rectTransform, x, y, width, height);
            return label;
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

        /// <summary>Shisha menyu tugmasi: yarim shaffof, ingichka ramka, chapda ikonka va matn.</summary>
        static Button CreateGlassButton(string name, Transform parent, string key, Sprite icon, float x, float y, float width, UiKit kit)
        {
            var fill = CreateSliced(name, parent, kit.RoundFill, 12f, 24f, GlassFill);
            PlaceTopLeft(fill.rectTransform, x, y, width, 52f);
            fill.raycastTarget = true;
            GlassBorderFor(fill.transform, kit, 12f, false);
            var image = CreateImage("Icon", fill.transform, icon, new Vector2(21f, 21f), Vector2.zero, Color.white);
            Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(30f, 0f), new Vector2(21f, 21f));
            var label = Localized(CreateLabel("Label", fill.transform, kit.SemiBold, "", 16, Color.white, TextAnchor.MiddleLeft), key);
            Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(58f, 0f), new Vector2(width - 70f, 24f));
            return TintButton(fill);
        }

        /// <summary>Asosiy tugma: ko'k-moviy gradient, chapda ikonka, o'ngda o'q.</summary>
        static Button CreateGradientButton(string name, Transform parent, string key, Sprite icon, float x, float y, float width, float height, UiKit kit)
        {
            var fill = CreateSliced(name, parent, GradientSprite(), 12f, 24f, Color.white);
            PlaceTopLeft(fill.rectTransform, x, y, width, height);
            fill.raycastTarget = true;
            var shine = CreateSliced("Border", fill.transform, kit.RoundStroke, 12f, 24f, new Color(1f, 1f, 1f, 0.28f));
            Stretch(shine.rectTransform);
            var image = CreateImage("Icon", fill.transform, icon, new Vector2(22f, 22f), Vector2.zero, Color.white);
            Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(34f, 0f), new Vector2(22f, 22f));
            var label = Localized(CreateLabel("Label", fill.transform, kit.Bold, "", 19, Color.white, TextAnchor.MiddleLeft), key);
            Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(66f, 0f), new Vector2(width - 120f, 28f));
            var arrow = CreateImage("Arrow", fill.transform, DrawnIcon("ArrowRight"), new Vector2(22f, 22f), Vector2.zero, Color.white);
            Place(arrow.rectTransform, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-34f, 0f), new Vector2(22f, 22f));
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

        /// <summary>Zona kartasi: yuqorida bino rasmi, pastda ikonka, nomi va qisqa izoh.</summary>
        static (Button, Image) CreateZoneCard(Transform parent, Zone zone, Sprite picture, float x, float width, UiKit kit)
        {
            const float height = 170f, imageHeight = 104f;
            var fill = CreateSliced("Card_" + zone.Id, parent, kit.RoundFill, 14f, 24f, GlassFill);
            Place(fill.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, 0f), new Vector2(width, height));
            fill.raycastTarget = true;
            var mask = CreateRect("Picture", fill.transform);
            Place(mask, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(width - 12f, imageHeight));
            mask.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(3, 3);
            var image = CreateImage("Image", mask, picture, Vector2.zero, Vector2.zero, Color.white);
            Stretch(image.rectTransform);
            image.preserveAspect = false;

            var icon = CreateImage("Icon", fill.transform, DrawnIcon(zone.Icon), new Vector2(22f, 22f), Vector2.zero, Color.white);
            Place(icon.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(26f, 30f), new Vector2(22f, 22f));
            var title = Localized(CreateLabel("Title", fill.transform, kit.SemiBold, "", 15, Color.white, TextAnchor.MiddleLeft), "zone." + zone.Id);
            Place(title.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(50f, 40f), new Vector2(width - 60f, 20f));
            var sub = Localized(CreateLabel("Subtitle", fill.transform, kit.Medium, "", 12, new Color(1f, 1f, 1f, 0.62f), TextAnchor.MiddleLeft), "zone." + zone.Id + ".sub");
            Place(sub.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(50f, 20f), new Vector2(width - 60f, 18f));

            var border = CreateSliced("Border", fill.transform, kit.RoundStroke, 14f, 24f, GlassBorder);
            Stretch(border.rectTransform);
            return (TintButton(fill), border);
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
                        // Yumaloq burchak: eng yaqin burchak markazigacha masofa
                        float cx = Mathf.Clamp(x + 0.5f, r, w - r), cy = Mathf.Clamp(y + 0.5f, r, h - r);
                        float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                        float alpha = Mathf.Clamp01(r - d + 0.5f);
                        var c = Color.Lerp(from, to, x / (float)(w - 1));
                        c = Color.Lerp(c, Color.white, 0.12f * (y / (float)h)); // tepasi biroz yorqinroq
                        c.a = alpha;
                        t.SetPixel(x, y, c);
                    }
                File.WriteAllBytes(path, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path);
            }
            return LoadSlicedSprite(path);
        }

        /// <summary>Pastdan yuqoriga shaffoflashuvchi soya (kartalar ostida).</summary>
        static Sprite VerticalShadeSprite()
        {
            string path = UiArt + "UI_ShadeV.png";
            if (!File.Exists(path))
            {
                var t = new Texture2D(4, 256, TextureFormat.RGBA32, false);
                for (int y = 0; y < 256; y++)
                {
                    float a = 1f - Mathf.SmoothStep(0f, 1f, y / 255f);
                    for (int x = 0; x < 4; x++)
                        t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                File.WriteAllBytes(path, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path);
            }
            return LoadSprite(path);
        }
    }
}

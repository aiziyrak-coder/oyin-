using System.IO;
using System.Linq;
using CraDev.CDCGroup;
using CraDev.CharacterCreation;
using CraDev.Intro;
using CraDev.Loading;
using CraDev.Online;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static CraDev.EditorTools.UiBuild;

namespace CraDev.EditorTools
{
    /// <summary>
    /// "CraDev > Sahnalarni yaratish" menyusi: o'yin boshidagi sahnalarni noldan quradi va
    /// Build Settings'da shu tartibda birinchi o'rinlarga qo'yadi:
    /// Intro (CraDev) -> CDCGroup -> Loading -> CharacterCreation.
    /// Sahnalarni qo'lda yig'ish shart emas: istalgan payt shu buyruq bilan qayta yaratish mumkin.
    /// </summary>
    static class CraDevSceneBuilder
    {
        const string ScenesFolder = "Assets/CraDev/Scenes/";
        const string IntroScene = ScenesFolder + "Intro.unity";
        const string CdcScene = ScenesFolder + "CDCGroup.unity";
        const string LoadingScene = ScenesFolder + "Loading.unity";
        const string CreationScene = ScenesFolder + "CharacterCreation.unity";

        const string IntroArt = "Assets/CraDev/Intro/Art/";
        const string CdcArt = "Assets/CraDev/CDCGroup/Art/";
        const string UiArt = "Assets/CraDev/UI/Art/";
        const string Fonts = "Assets/CraDev/Common/Fonts/";
        const string FontLight = Fonts + "Manrope-ExtraLight.ttf";
        const string FontSemiBold = Fonts + "Manrope-SemiBold.ttf";
        const string MaterialsFolder = "Assets/CraDev/CharacterCreation/Materials/";

        // Barcha sahnalar uchun bitta tekis fon
        static readonly Color Background = new Color32(11, 11, 12, 255);
        static readonly Color Muted = new Color32(142, 147, 154, 255);

        [MenuItem("CraDev/Sahnalarni yaratish", priority = 1)]
        static void BuildAll()
        {
            string[] scenes = { IntroScene, CdcScene, LoadingScene, CreationScene };
            if (scenes.Any(File.Exists) &&
                !EditorUtility.DisplayDialog("CraDev sahnalari",
                    "Intro, CDCGroup, Loading va CharacterCreation sahnalari noldan qayta yaratiladi. Ularda qo'lda qilingan o'zgarishlar yo'qoladi. Davom etamizmi?",
                    "Ha, yaratish", "Bekor qilish"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            try
            {
                BuildIntro();
                BuildCdcGroup();
                BuildLoading();
                BuildCharacterCreation();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("CraDev sahnalari", "Sahnalarni yaratishda xato: " + e.Message, "OK");
                return;
            }

            // O'yin shu tartibda boshlanadi; boshqa sahnalar ro'yxatda ulardan keyin qoladi
            var buildScenes = EditorBuildSettings.scenes.Where(s => !scenes.Contains(s.path)).ToList();
            buildScenes.InsertRange(0, scenes.Select(p => new EditorBuildSettingsScene(p, true)));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            ConfigurePlayer();

            EditorSceneManager.OpenScene(IntroScene);
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(IntroScene));
            Debug.Log("[CraDev] Sahnalar yaratildi: Intro -> CDCGroup -> Loading -> CharacterCreation. Ko'rish uchun Intro sahnasida Play tugmasini bosing.");
        }

        [MenuItem("CraDev/O'yinni alohida oynada ishga tushirish (Build and Run)", priority = 2)]
        static void BuildAndRun()
        {
            if (!File.Exists(IntroScene))
            {
                EditorUtility.DisplayDialog("CraDev", "Avval \"CraDev > Sahnalarni yaratish\" buyrug'ini bajaring.", "OK");
                return;
            }
            ConfigurePlayer();

            // Tahrirlovchi qaysi tizimda ishlayotgan bo'lsa, o'sha tizim uchun o'yin yig'iladi
            BuildTarget target;
            string file;
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor: target = BuildTarget.StandaloneOSX; file = "CraDev.app"; break;
                case RuntimePlatform.LinuxEditor: target = BuildTarget.StandaloneLinux64; file = "CraDev.x86_64"; break;
                default: target = BuildTarget.StandaloneWindows64; file = "CraDev.exe"; break;
            }

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = Path.Combine("Builds", target.ToString(), file),
                target = target,
                options = BuildOptions.AutoRunPlayer,
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                EditorUtility.DisplayDialog("CraDev", "O'yinni yig'ib bo'lmadi. Tafsilotlar Console oynasida.", "OK");
        }

        /// <summary>
        /// O'yin haqiqiy o'yindek ochilishi uchun Player sozlamalari: to'liq ekranli alohida oyna,
        /// "Made with Unity" ekranisiz, darhol CraDev intro'si bilan boshlanadi.
        /// </summary>
        static void ConfigurePlayer()
        {
            if (PlayerSettings.companyName == "DefaultCompany")
                PlayerSettings.companyName = "CraDev";
            // Standart nom loyiha papkasining nomi bo'ladi; o'yin nomi tanlanguncha oyna sarlavhasi "CraDev"
            if (PlayerSettings.productName == new DirectoryInfo(Application.dataPath).Parent.Name)
                PlayerSettings.productName = "CraDev";

            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.SplashScreen.show = false;
            // Unity 6 da bepul litsenziyada ham ishlaydi; eski versiyalarning Personal litsenziyasida Unity logotipi baribir chiqadi
#if UNITY_2022_1_OR_NEWER
            // Hozircha server http://localhost da ishlaydi. Haqiqiy serverga HTTPS bilan o'tganda buni o'chirish kerak.
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
#endif
        }

        [MenuItem("CraDev/Test: saqlangan profilni o'chirish", priority = 20)]
        static void ClearProfile()
        {
            PlayerProfile.Clear();
            EditorUtility.DisplayDialog("CraDev", "Shu kompyuterdagi profil o'chirildi. Keyingi ishga tushirishda avatar yaratish ekrani yana chiqadi. (Server'dagi nickname band bo'lib qoladi.)", "OK");
        }

        // ---------------------------------------------------------------- CraDev intro

        static void BuildIntro()
        {
            // Yakuniy joylashuvlar Design/CraDev/layout.json dan olingan (ekran markaziga nisbatan)
            var markPos = new Vector2(-273.5f, 30f);
            var glyphOffset = new Vector2(-8f, 0f);
            var wordmarkPos = new Vector2(96f, 33f);
            var taglinePos = new Vector2(0f, -125f);
            // Yozuv niqobi belgi va yozuv orasidagi bo'shliqdan boshlanadi
            var maskPos = new Vector2(102f, 33f);
            var maskSize = new Vector2(560f, 150f);

            var root = NewUiScene(out var scene);
            CreateFullscreen("Background", root, Background);

            var logo = CreateRect("Logo", root);
            var mask = CreateRect("WordmarkMask", logo);
            mask.anchoredPosition = maskPos;
            mask.sizeDelta = maskSize;
            mask.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(6, 0);
            var wordmark = CreateSprite("Wordmark", mask, LoadSprite(IntroArt + "CraDev_Wordmark.png"), wordmarkPos - maskPos);

            // Belgi yozuvdan keyin yaratiladi, shunda harakat paytida yozuv uning ostidan chiqadi
            var mark = CreateRect("Mark", logo);
            mark.anchoredPosition = markPos;
            var tile = CreateSprite("Tile", mark, LoadSprite(IntroArt + "CraDev_Tile.png"), Vector2.zero);
            var glyph = CreateSprite("Glyph", mark, LoadSprite(IntroArt + "CraDev_Glyph.png"), glyphOffset);

            var tagline = CreateSprite("Tagline", logo, LoadSprite(IntroArt + "CraDev_Tagline.png"), taglinePos);
            var fader = CreateFullscreen("Fader", root, new Color(0f, 0f, 0f, 0f));

            var director = new GameObject("IntroDirector");
            var sound = AddSound(director, "Assets/CraDev/Intro/Audio/CraDev_Sound.wav");
            var sequence = director.AddComponent<IntroSequence>();
            Set(sequence, "nextScene", "CDCGroup");
            Set(sequence, "fader", fader);
            Set(sequence, "sound", sound);
            Set(sequence, "fadeInDuration", 0.3f);
            Set(sequence, "soundStart", 0.2f);
            Set(sequence, "fadeOutStart", 3.6f);
            Set(sequence, "fadeOutDuration", 0.6f);
            Set(sequence, "mark", mark);
            Set(sequence, "tile", tile);
            Set(sequence, "glyph", glyph);
            Set(sequence, "wordmark", wordmark.rectTransform);
            Set(sequence, "tagline", tagline);

            Save(scene, IntroScene);
        }

        // ---------------------------------------------------------------- CDCGroup

        static void BuildCdcGroup()
        {
            // Joylashuvlar Design/CDCGroup/layout.json dan olingan
            var lettersPos = new Vector2(-0.5f, 70f);
            var linePos = new Vector2(0f, -24.5f);
            var groupPos = new Vector2(0.5f, -65.5f);
            // "CDC" chiziqdan yuqorida, "GROUP" pastda niqoblanadi: ular chiziq ortidan chiqib keladi
            var upperMaskPos = new Vector2(0f, 58.25f);
            var upperMaskSize = new Vector2(480f, 162.5f);
            var lowerMaskPos = new Vector2(0f, -57f);
            var lowerMaskSize = new Vector2(480f, 62f);

            var root = NewUiScene(out var scene);
            CreateFullscreen("Background", root, Background);

            var logo = CreateRect("Logo", root);
            var upper = CreateRect("LettersMask", logo);
            upper.anchoredPosition = upperMaskPos;
            upper.sizeDelta = upperMaskSize;
            upper.gameObject.AddComponent<RectMask2D>();
            var letters = CreateSprite("Letters", upper, LoadSprite(CdcArt + "CDC_Letters.png"), lettersPos - upperMaskPos);

            var lower = CreateRect("GroupMask", logo);
            lower.anchoredPosition = lowerMaskPos;
            lower.sizeDelta = lowerMaskSize;
            lower.gameObject.AddComponent<RectMask2D>();
            var group = CreateSprite("Group", lower, LoadSprite(CdcArt + "CDC_Group.png"), groupPos - lowerMaskPos);

            var line = CreateSprite("Line", logo, LoadSprite(CdcArt + "CDC_Line.png"), linePos);
            var fader = CreateFullscreen("Fader", root, new Color(0f, 0f, 0f, 0f));

            var director = new GameObject("CDCGroupDirector");
            var sound = AddSound(director, "Assets/CraDev/CDCGroup/Audio/CDC_Sound.wav");
            var splash = director.AddComponent<CdcGroupSplash>();
            Set(splash, "nextScene", "Loading");
            Set(splash, "fader", fader);
            Set(splash, "sound", sound);
            Set(splash, "fadeInDuration", 0.3f);
            Set(splash, "soundStart", 0.2f);
            Set(splash, "fadeOutStart", 2.9f);
            Set(splash, "fadeOutDuration", 0.6f);
            Set(splash, "line", line);
            Set(splash, "letters", letters.rectTransform);
            Set(splash, "group", group.rectTransform);

            Save(scene, CdcScene);
        }

        // ---------------------------------------------------------------- Loading

        static void BuildLoading()
        {
            var light = AssetDatabase.LoadAssetAtPath<Font>(FontLight);
            var semiBold = AssetDatabase.LoadAssetAtPath<Font>(FontSemiBold);
            if (light == null || semiBold == null)
                throw new FileNotFoundException("Manrope shriftlari topilmadi: Assets/CraDev/Common/Fonts");

            var root = NewUiScene(out var scene);
            CreateFullscreen("Background", root, Background);

            var status = CreateText("Status", root, semiBold, "L O A D I N G", 16, new Vector2(0f, 96f), Muted);
            var percent = CreateText("Percent", root, light, "65<size=48>%</size>", 128, new Vector2(0f, 8f), Color.white);
            percent.supportRichText = true;

            var bar = CreateImage("Bar", root, null, new Vector2(420f, 2f), new Vector2(0f, -90f), new Color(1f, 1f, 1f, 0.12f));
            var fill = CreateImage("Fill", bar.transform, null, Vector2.zero, Vector2.zero, Color.white);
            Stretch(fill.rectTransform);
            fill.rectTransform.anchorMax = new Vector2(0.65f, 1f); // tahrirlashda ko'rinishi uchun; o'yinda skript boshqaradi

            var fader = CreateFullscreen("Fader", root, new Color(0f, 0f, 0f, 0f));

            var director = new GameObject("LoadingDirector");
            var loading = director.AddComponent<LoadingScreen>();
            Set(loading, "newPlayerScene", "CharacterCreation");
            Set(loading, "returningPlayerScene", "MainMenu");
            Set(loading, "fader", fader);
            Set(loading, "percentText", percent);
            Set(loading, "statusText", status);
            Set(loading, "barFill", fill.rectTransform);

            Save(scene, LoadingScene);
        }

        // ---------------------------------------------------------------- Avatar yaratish

        static void BuildCharacterCreation()
        {
            var accent = (Color)new Color32(61, 90, 254, 255);
            var field = (Color)new Color32(22, 23, 26, 255);
            var line = (Color)new Color32(38, 39, 44, 255);
            var circle = (Color)new Color32(32, 33, 38, 255);
            var faint = (Color)new Color32(90, 94, 102, 255);
            var bad = (Color)new Color32(240, 82, 82, 255);

            var bold = LoadFont("Manrope-Bold.ttf");
            var semiBold = LoadFont("Manrope-SemiBold.ttf");
            var medium = LoadFont("Manrope-Medium.ttf");
            var display = LoadFont("Unbounded-Bold.ttf");

            System.Func<string, Sprite> Icon = name => LoadSprite(UiArt + "Icon_" + name + ".png");
            var roundFill = LoadSlicedSprite(UiArt + "UI_Round12_Fill.png");
            var roundStroke = LoadSlicedSprite(UiArt + "UI_Round12_Stroke.png");
            var pillFill = LoadSlicedSprite(UiArt + "UI_Pill_Fill.png");
            var pillStroke = LoadSlicedSprite(UiArt + "UI_Pill_Stroke.png");

            var root = NewUiScene(out var scene);
            root.gameObject.AddComponent<GraphicRaycaster>();

            // ---------- 3D: kamera faqat o'ng yarmini chizadi ----------
            var camera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
            camera.rect = new Rect(0.5f, 0f, 0.5f, 1f);
            camera.backgroundColor = new Color32(19, 20, 23, 255);
            camera.cullingMask = ~0;
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            camera.transform.position = new Vector3(0f, 1.0f, -5.4f);
            camera.transform.LookAt(new Vector3(0f, 0.9f, 0f));

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.36f, 0.37f, 0.40f);
            CreateLight("Key Light", new Vector3(2.2f, 3.2f, -3.2f), new Color(1f, 0.965f, 0.925f), 1.15f, LightShadows.Soft);
            CreateLight("Rim Light", new Vector3(-1.5f, 2.6f, 3f), new Color(0.78f, 0.83f, 1f), 0.9f, LightShadows.None);
            CreateLight("Fill Light", new Vector3(-3f, 1.2f, -2f), Color.white, 0.3f, LightShadows.None);

            var skin = CreateLitMaterial("Mannequin", new Color32(201, 205, 211, 255), 0.35f);
            var hair = CreateLitMaterial("Hair", new Color32(43, 45, 51, 255), 0.2f);
            var floorMat = CreateLitMaterial("Floor", new Color32(27, 28, 32, 255), 0.05f);
            var rimMat = CreateLitMaterial("FloorRim", new Color32(42, 43, 49, 255), 0.05f);
            CreateDisc("Floor", 1.0f, 0.02f, -0.01f, floorMat);
            CreateDisc("FloorRim", 1.012f, 0.012f, -0.014f, rimMat);

            var avatarGo = new GameObject("Avatar");
            var avatar = avatarGo.AddComponent<AvatarPreview>();
            Set(avatar, "skinMaterial", skin);
            Set(avatar, "hairMaterial", hair);

            CreateEventSystem();

            // ---------- Chap panel ----------
            var left = CreateImage("LeftPanel", root, null, Vector2.zero, Vector2.zero, new Color32(11, 11, 12, 255));
            left.rectTransform.anchorMin = Vector2.zero;
            left.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            left.rectTransform.offsetMin = left.rectTransform.offsetMax = Vector2.zero;
            var leftT = left.transform;

            // Brend: kichik belgi va "CraDev"
            var brandTile = CreateSliced("BrandTile", leftT, roundFill, 8f, 24f, accent);
            PlaceTopLeft(brandTile.rectTransform, 120f, 72f, 32f, 32f);
            var glyphSprite = LoadSprite(IntroArt + "CraDev_Glyph.png");
            var brandGlyph = CreateImage("Glyph", brandTile.transform, glyphSprite, NativeSize(glyphSprite) * (32f / 150f), Vector2.zero, Color.white);
            brandGlyph.rectTransform.anchoredPosition = new Vector2(-8f * 32f / 150f, 0f);
            var brandText = CreateLabel("BrandName", leftT, display, "Cra<color=#3D5AFE>Dev</color>", 16, Color.white, TextAnchor.MiddleLeft);
            brandText.supportRichText = true;
            PlaceTopLeft(brandText.rectTransform, 166f, 72f, 200f, 32f);

            // Forma: 480 kenglikda, panel bo'yicha vertikal markazda
            var form = CreateRect("Form", leftT);
            Place(form, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(120f, 0f), new Vector2(480f, 700f));

            var eyebrow = CreateLabel("Eyebrow", form, bold, "NEW PLAYER", 13, accent, TextAnchor.UpperLeft);
            PlaceTopLeft(eyebrow.rectTransform, 0f, 0f, 480f, 20f);
            var title = CreateLabel("Title", form, display, "Create your\ncharacter", 44, Color.white, TextAnchor.UpperLeft);
            title.lineSpacing = 0.92f;
            PlaceTopLeft(title.rectTransform, 0f, 34f, 480f, 110f);
            var sub = CreateLabel("Subtitle", form, medium,
                "Pick a nickname and gender. Your nickname is unique across the server, and other players will see it in game.",
                17, UiMuted, TextAnchor.UpperLeft);
            sub.horizontalOverflow = HorizontalWrapMode.Wrap;
            sub.lineSpacing = 1.1f;
            PlaceTopLeft(sub.rectTransform, 0f, 152f, 440f, 84f);

            PlaceTopLeft(CreateLabel("NicknameLabel", form, bold, "NICKNAME", 12, UiMuted, TextAnchor.UpperLeft).rectTransform, 0f, 276f, 480f, 18f);

            // Nickname maydoni
            var glow = CreateSliced("FieldGlow", form, roundFill, 16f, 24f, new Color(accent.r, accent.g, accent.b, 0.16f));
            PlaceTopLeft(glow.rectTransform, -4f, 301f, 488f, 68f);
            glow.enabled = false;
            var fieldBg = CreateSliced("NicknameField", form, roundFill, 12f, 24f, field);
            PlaceTopLeft(fieldBg.rectTransform, 0f, 305f, 480f, 60f);
            fieldBg.raycastTarget = true; // maydon bosilganda yozish boshlanadi
            var fieldBorder = CreateSliced("Border", fieldBg.transform, roundStroke, 12f, 24f, line);
            Stretch(fieldBorder.rectTransform);
            var lead = CreateImage("UserIcon", fieldBg.transform, Icon("User"), new Vector2(22f, 22f), Vector2.zero, UiMuted);
            Place(lead.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(31f, 0f), new Vector2(22f, 22f));
            var status = CreateImage("StatusIcon", fieldBg.transform, Icon("Check"), new Vector2(22f, 22f), Vector2.zero, UiMuted);
            Place(status.rectTransform, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-29f, 0f), new Vector2(22f, 22f));
            var inputText = CreateLabel("Text", fieldBg.transform, semiBold, "", 19, Color.white, TextAnchor.MiddleLeft);
            inputText.supportRichText = false;
            SetInsets(inputText.rectTransform, 56f, 52f);
            var placeholder = CreateLabel("Placeholder", fieldBg.transform, medium, "Enter a nickname", 19, faint, TextAnchor.MiddleLeft);
            SetInsets(placeholder.rectTransform, 56f, 52f);
            var input = fieldBg.gameObject.AddComponent<InputField>();
            input.textComponent = inputText;
            input.placeholder = placeholder;
            input.targetGraphic = fieldBg;
            input.transition = Selectable.Transition.None;
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = NicknameRules.MaxLength;
            input.customCaretColor = true;
            input.caretColor = accent;
            input.caretWidth = 2;
            input.selectionColor = new Color(accent.r, accent.g, accent.b, 0.35f);

            // Maydon ostidagi yordam matni
            var helpIcon = CreateImage("HelpIcon", form, Icon("Check"), new Vector2(16f, 16f), Vector2.zero, UiMuted);
            Place(helpIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(8f, -386f), new Vector2(16f, 16f));
            var helpText = CreateLabel("HelpText", form, medium, "", 14, UiMuted, TextAnchor.MiddleLeft);
            Place(helpText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, -386f), new Vector2(470f, 22f));

            PlaceTopLeft(CreateLabel("GenderLabel", form, bold, "GENDER", 12, UiMuted, TextAnchor.UpperLeft).rectTransform, 0f, 419f, 480f, 18f);
            var maleCard = CreateGenderCard("MaleCard", form, 0f, "Male", Icon("Male"), roundFill, roundStroke, pillFill, Icon("Check"), bold, accent, field, line, circle);
            var femaleCard = CreateGenderCard("FemaleCard", form, 247f, "Female", Icon("Female"), roundFill, roundStroke, pillFill, Icon("Check"), bold, accent, field, line, circle);

            // Asosiy tugma
            var ctaFill = CreateSliced("CreateButton", form, roundFill, 12f, 24f, accent);
            PlaceTopLeft(ctaFill.rectTransform, 0f, 604f, 480f, 60f);
            ctaFill.raycastTarget = true;
            var cta = ctaFill.gameObject.AddComponent<Button>();
            cta.targetGraphic = ctaFill;
            var colors = cta.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0.1f;
            cta.colors = colors;
            var ctaLabel = CreateLabel("Label", ctaFill.transform, bold, "Create character", 17, Color.white, TextAnchor.MiddleCenter);
            Place(ctaLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 30f));
            var ctaIcon = CreateImage("Icon", ctaFill.transform, Icon("Arrow"), new Vector2(22f, 22f), Vector2.zero, Color.white);

            var errorIcon = CreateImage("ErrorIcon", form, Icon("Alert"), new Vector2(16f, 16f), Vector2.zero, bad);
            Place(errorIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(8f, -689f), new Vector2(16f, 16f));
            var errorText = CreateLabel("ErrorText", form, medium, "", 14, bad, TextAnchor.MiddleLeft);
            Place(errorText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(24f, -689f), new Vector2(456f, 22f));

            // ---------- O'ng panel (3D ustida) ----------
            var right = CreateRect("RightPanel", root);
            right.anchorMin = new Vector2(0.5f, 0f);
            right.anchorMax = Vector2.one;
            right.offsetMin = right.offsetMax = Vector2.zero;
            var drag = CreateImage("DragArea", right, null, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            Stretch(drag.rectTransform);
            drag.raycastTarget = true;
            Set(drag.gameObject.AddComponent<AvatarDragArea>(), "avatar", avatar);

            var plate = CreateSliced("Nameplate", right, pillFill, 22f, 32f, new Color(1f, 1f, 1f, 0.06f));
            Place(plate.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -172f), new Vector2(200f, 44f));
            var plateBorder = CreateSliced("Border", plate.transform, pillStroke, 22f, 32f, new Color(1f, 1f, 1f, 0.10f));
            Stretch(plateBorder.rectTransform);
            var plateIcon = CreateImage("Icon", plate.transform, Icon("Male"), new Vector2(18f, 18f), Vector2.zero, accent);
            var plateText = CreateLabel("Text", plate.transform, bold, "Your nickname", 17, faint, TextAnchor.MiddleCenter);
            Place(plateText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 30f));

            var hint = CreateRect("Hint", right);
            Place(hint, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 73f), new Vector2(300f, 24f));
            var hintIcon = CreateImage("Icon", hint, Icon("Rotate"), new Vector2(18f, 18f), Vector2.zero, UiMuted);
            var hintText = CreateLabel("Text", hint, semiBold, "Drag to rotate", 14, UiMuted, TextAnchor.MiddleCenter);
            Place(hintText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 24f));

            var fader = CreateFullscreen("Fader", root, new Color(0f, 0f, 0f, 0f));

            // ---------- Boshqaruvchi ----------
            var director = new GameObject("CharacterCreationDirector");
            var screen = director.AddComponent<CharacterCreationScreen>();
            Set(screen, "nextScene", "MainMenu");
            Set(screen, "nicknameInput", input);
            Set(screen, "fieldBorder", fieldBorder);
            Set(screen, "fieldGlow", glow);
            Set(screen, "statusIcon", status);
            Set(screen, "helpIcon", helpIcon);
            Set(screen, "helpText", helpText);
            Set(screen, "maleCard", maleCard);
            Set(screen, "femaleCard", femaleCard);
            Set(screen, "createButton", cta);
            Set(screen, "createFill", ctaFill);
            Set(screen, "createLabel", ctaLabel);
            Set(screen, "createIcon", ctaIcon);
            Set(screen, "errorIcon", errorIcon);
            Set(screen, "errorText", errorText);
            Set(screen, "avatar", avatar);
            Set(screen, "nameplate", plate.rectTransform);
            Set(screen, "nameplateIcon", plateIcon);
            Set(screen, "nameplateText", plateText);
            Set(screen, "hintIcon", hintIcon);
            Set(screen, "hintText", hintText);
            Set(screen, "fader", fader);
            Set(screen, "checkSprite", Icon("Check"));
            Set(screen, "closeSprite", Icon("Close"));
            Set(screen, "spinnerSprite", Icon("Spinner"));
            Set(screen, "alertSprite", Icon("Alert"));
            Set(screen, "arrowSprite", Icon("Arrow"));
            Set(screen, "maleSprite", Icon("Male"));
            Set(screen, "femaleSprite", Icon("Female"));

            Save(scene, CreationScene);
        }

        static readonly Color UiMuted = new Color32(142, 147, 154, 255);

        static GenderCard CreateGenderCard(string name, Transform parent, float x, string label, Sprite icon,
            Sprite roundFill, Sprite roundStroke, Sprite pill, Sprite check, Font font,
            Color accent, Color field, Color line, Color circle)
        {
            var fill = CreateSliced(name, parent, roundFill, 14f, 24f, field);
            PlaceTopLeft(fill.rectTransform, x, 448f, 233f, 116f);
            fill.raycastTarget = true;
            var border = CreateSliced("Border", fill.transform, roundStroke, 14f, 24f, line);
            Stretch(border.rectTransform);

            var iconCircle = CreateImage("IconCircle", fill.transform, pill, new Vector2(48f, 48f), Vector2.zero, circle);
            Place(iconCircle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40.5f), new Vector2(48f, 48f));
            var iconImage = CreateImage("Icon", iconCircle.transform, icon, new Vector2(24f, 24f), Vector2.zero, UiMuted);
            var text = CreateLabel("Label", fill.transform, font, label, 17, Color.white, TextAnchor.MiddleCenter);
            Place(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -88f), new Vector2(220f, 26f));

            var tick = CreateImage("Tick", fill.transform, pill, new Vector2(22f, 22f), Vector2.zero, accent);
            Place(tick.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-23f, -23f), new Vector2(22f, 22f));
            CreateImage("Check", tick.transform, check, new Vector2(14f, 14f), Vector2.zero, Color.white);

            // Sichqoncha ustiga kelganda ramka biroz yorishadi
            var button = fill.gameObject.AddComponent<Button>();
            button.targetGraphic = border;
            var colors = button.colors;
            colors.normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.selectedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            var card = fill.gameObject.AddComponent<GenderCard>();
            Set(card, "fill", fill);
            Set(card, "border", border);
            Set(card, "iconCircle", iconCircle);
            Set(card, "icon", iconImage);
            Set(card, "tick", tick.gameObject);
            return card;
        }

        static void SetInsets(RectTransform rect, float left, float right)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, 0f);
            rect.offsetMax = new Vector2(-right, 0f);
        }

        static Font LoadFont(string file)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(Fonts + file);
            if (font == null)
                throw new FileNotFoundException("Shrift topilmadi: " + Fonts + file);
            return font;
        }

        static void CreateLight(string name, Vector3 from, Color color, float intensity, LightShadows shadows)
        {
            var go = new GameObject(name);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = shadows;
            light.shadowStrength = 0.6f;
            go.transform.rotation = Quaternion.LookRotation(-from.normalized);
        }

        static void CreateDisc(string name, float diameter, float height, float y, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = new Vector3(0f, y, 0f);
            go.transform.localScale = new Vector3(diameter, height / 2f, diameter);
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        /// <summary>Joriy render pipeline'ga mos yoritiladigan material (Built-in: Standard, URP/HDRP: Lit).</summary>
        static Material CreateLitMaterial(string name, Color color, float smoothness)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            var shader = pipeline != null ? pipeline.defaultShader : Shader.Find("Standard");

            // Mavjud material bo'lsa yangilanadi (havolalar buzilmasligi uchun), bo'lmasa yaratiladi
            string path = MaterialsFolder + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = material == null;
            if (isNew)
                material = new Material(shader) { name = name };
            else
                material.shader = shader;

            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);

            if (isNew)
            {
                EnsureFolder(MaterialsFolder.TrimEnd('/'));
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <summary>UI bosishlari uchun EventSystem: yangi Input System yoqilgan bo'lsa o'shaning moduli bilan.</summary>
        static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            var module = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (module != null)
            {
                go.AddComponent(module);
                return;
            }
#endif
            go.AddComponent<StandaloneInputModule>();
        }
    }
}

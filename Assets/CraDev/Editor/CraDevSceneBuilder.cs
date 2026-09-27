using System.IO;
using System.Linq;
using CraDev.CDCGroup;
using CraDev.CharacterCreation;
using CraDev.Face;
using CraDev.Intro;
using CraDev.Loading;
using CraDev.Online;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.InferenceEngine;
using static CraDev.EditorTools.UiBuild;

namespace CraDev.EditorTools
{
    /// <summary>
    /// "CraDev > Sahnalarni yaratish" menyusi: o'yin boshidagi sahnalarni noldan quradi va
    /// Build Settings'da shu tartibda birinchi o'rinlarga qo'yadi:
    /// Intro (CraDev) -> CDCGroup -> Loading -> CharacterCreation.
    /// Sahnalarni qo'lda yig'ish shart emas: istalgan payt shu buyruq bilan qayta yaratish mumkin.
    /// </summary>
    static partial class CraDevSceneBuilder
    {
        const string ScenesFolder = "Assets/CraDev/Scenes/";
        const string IntroScene = ScenesFolder + "Intro.unity";
        const string CdcScene = ScenesFolder + "CDCGroup.unity";
        const string LoadingScene = ScenesFolder + "Loading.unity";
        const string CreationScene = ScenesFolder + "CharacterCreation.unity";
        static readonly string[] AllScenes = { IntroScene, CdcScene, LoadingScene, CreationScene };

        const string IntroArt = "Assets/CraDev/Intro/Art/";
        const string CdcArt = "Assets/CraDev/CDCGroup/Art/";
        const string UiArt = "Assets/CraDev/UI/Art/";
        const string Fonts = "Assets/CraDev/Common/Fonts/";
        const string FontLight = Fonts + "Manrope-ExtraLight.ttf";
        const string FontSemiBold = Fonts + "Manrope-SemiBold.ttf";

        // Barcha sahnalar uchun bitta tekis fon
        static readonly Color Background = new Color32(11, 11, 12, 255);
        static readonly Color Muted = new Color32(142, 147, 154, 255);

        [MenuItem("CraDev/Sahnalarni yaratish", priority = 1)]
        static void BuildAll()
        {
            if (AllScenes.Any(File.Exists) &&
                !EditorUtility.DisplayDialog("CraDev sahnalari",
                    "Intro, CDCGroup, Loading va CharacterCreation sahnalari noldan qayta yaratiladi. Ularda qo'lda qilingan o'zgarishlar yo'qoladi. Davom etamizmi?",
                    "Ha, yaratish", "Bekor qilish"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            try
            {
                CreateScenes();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("CraDev sahnalari", "Sahnalarni yaratishda xato: " + e.Message, "OK");
                return;
            }

            EditorSceneManager.OpenScene(IntroScene);
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(IntroScene));
            Debug.Log("[CraDev] Sahnalar yaratildi: Intro -> CDCGroup -> Loading -> CharacterCreation. Ko'rish uchun Intro sahnasida Play tugmasini bosing.");
        }

        /// <summary>Barcha sahnalarni yaratadi, Build Settings va Player sozlamalarini o'rnatadi. Xatoda istisno tashlaydi.</summary>
        internal static void CreateScenes()
        {
            BuildIntro();
            BuildCdcGroup();
            BuildLoading();
            BuildCharacterCreation();

            // O'yin shu tartibda boshlanadi; boshqa sahnalar ro'yxatda ulardan keyin qoladi
            var buildScenes = EditorBuildSettings.scenes.Where(s => !AllScenes.Contains(s.path)).ToList();
            buildScenes.InsertRange(0, AllScenes.Select(p => new EditorBuildSettingsScene(p, true)));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            ConfigurePlayer();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("CraDev/O'yinni alohida oynada ishga tushirish (Build and Run)", priority = 2)]
        static void BuildAndRun()
        {
            if (!File.Exists(IntroScene))
            {
                EditorUtility.DisplayDialog("CraDev", "Avval \"CraDev > Sahnalarni yaratish\" buyrug'ini bajaring.", "OK");
                return;
            }
            var report = BuildGame(run: true);
            if (report.summary.result != BuildResult.Succeeded)
                EditorUtility.DisplayDialog("CraDev", "O'yinni yig'ib bo'lmadi. Tafsilotlar Console oynasida.", "OK");
        }

        /// <summary>Tahrirlovchi ishlayotgan tizim uchun o'yin: Windows'da StandaloneWindows64 va hokazo.</summary>
        internal static BuildTarget EditorPlatformTarget()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor: return BuildTarget.StandaloneOSX;
                case RuntimePlatform.LinuxEditor: return BuildTarget.StandaloneLinux64;
                default: return BuildTarget.StandaloneWindows64;
            }
        }

        /// <summary>Standart joy: Builds/&lt;target&gt;/CraDev(.exe|.app|.x86_64).</summary>
        internal static string DefaultBuildPath(BuildTarget target)
        {
            string file;
            switch (target)
            {
                case BuildTarget.StandaloneOSX: file = "CraDev.app"; break;
                case BuildTarget.StandaloneLinux64: file = "CraDev.x86_64"; break;
                default: file = "CraDev.exe"; break;
            }
            return Path.Combine("Builds", target.ToString(), file);
        }

        /// <summary>O'yinni tahrirlovchi ishlayotgan tizim uchun Builds/ papkasiga yig'adi.</summary>
        internal static BuildReport BuildGame(bool run)
        {
            var target = EditorPlatformTarget();
            return BuildGame(target, DefaultBuildPath(target), run);
        }

        /// <summary>Build Settings'dagi yoqilgan sahnalardan o'yinni yig'adi.</summary>
        internal static BuildReport BuildGame(BuildTarget target, string path, bool run)
        {
            ConfigurePlayer();
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = path,
                target = target,
                options = run ? BuildOptions.AutoRunPlayer : BuildOptions.None,
            };
            return BuildPipeline.BuildPlayer(options);
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
            // Multiplayer o'yin: boshqa oynaga o'tilganda ham to'xtamaydi (server bilan aloqa uzilmaydi)
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // 3D sahnalar uchun: silliq qirralar va yaqin masofada tiniq soyalar (barcha sifat darajalarida)
            int level = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.antiAliasing = 4;
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                QualitySettings.shadowDistance = 25f;
                QualitySettings.shadowCascades = 2;
                QualitySettings.vSyncCount = 1;
            }
            QualitySettings.SetQualityLevel(level, false);
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

        const string AvatarModels = "Assets/CraDev/Avatars/Models/";
        const string AvatarCards = "Assets/CraDev/Avatars/Cards/";
        const string AvatarAnimations = "Assets/CraDev/Avatars/Animations/";
        const string StageMaterials = "Assets/CraDev/CharacterCreation/Materials/";

        struct AvatarInfo
        {
            public string Id, Gender, Title, Model;
            public int Height;
            public AvatarInfo(string id, string gender, string title, int height, string model) { Id = id; Gender = gender; Title = title; Height = height; Model = model; }
            public string ModelPath => AvatarModels + Id + "/" + Model + ".fbx";
        }

        // Avatarlar (erkaklar 4 ta: M4 kerak emas, kodlar o'zgarmasligi uchun M5 qoldi) va ularning Microsoft Rocketbox
        // modellari. Modeli yo'qlari o'tkazib yuboriladi. Ro'yxat Server/src/nickname.js dagi AVATARS bilan bir xil bo'lishi kerak.
        static readonly AvatarInfo[] AvatarList =
        {
            new AvatarInfo("M1", "male", "Athletic", 183, "Male_Adult_10"), new AvatarInfo("M2", "male", "Slim", 177, "Male_Adult_09"),
            new AvatarInfo("M3", "male", "Strong", 180, "Male_Adult_17"), new AvatarInfo("M5", "male", "Classic", 176, "Male_Adult_07"),
            new AvatarInfo("F1", "female", "Sporty", 170, "Female_Adult_12"), new AvatarInfo("F2", "female", "Petite", 164, "Female_Adult_17"),
            new AvatarInfo("F3", "female", "Casual", 168, "Female_Adult_08"), new AvatarInfo("F4", "female", "Tall", 178, "Female_Adult_04"),
            new AvatarInfo("F5", "female", "Elegant", 167, "Female_Adult_15"),
        };

        static void BuildCharacterCreation()
        {
            // Sahnadan oldin: idle animatsiya boshqaruvchilari va kartalar uchun 3D modellardan olingan rasmlar
            var maleIdle = IdleController("m_idle_neutral_01", "Idle_Male");
            var femaleIdle = IdleController("f_idle_neutral_01", "Idle_Female");
            var cardPictures = RenderAvatarCards(maleIdle, femaleIdle);
            var faceMaps = BakeFaceMaps(maleIdle, femaleIdle);

            var accent = (Color)new Color32(61, 90, 254, 255);
            var field = (Color)new Color32(22, 23, 26, 255);
            var line = (Color)new Color32(38, 39, 44, 255);
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
            CreateEventSystem();

            // ---------- 3D sahna: studiya, taglik va chiroqlar ----------
            var stageCamera = Object.FindFirstObjectByType<Camera>();
            var turntable = BuildStage(stageCamera);

            // Butun ekran bo'ylab shaffof qatlam: qahramonni sichqoncha bilan aylantirish va yaqinlashtirish.
            // Formadan oldin yaratiladi, shunda forma elementlari bosilganda u xalaqit bermaydi.
            var stage = CreateFullscreen("StageInput", root, new Color(0f, 0f, 0f, 0f));
            stage.raycastTarget = true;
            var stageT = stage.transform;

            // ---------- Chap panel: 3D sahna ustida qorayib boruvchi fon, matn o'qilishi uchun ----------
            var left = CreateImage("LeftShade", root, ShadeSprite(), Vector2.zero, Vector2.zero, new Color(Background.r, Background.g, Background.b, 0.94f));
            left.rectTransform.anchorMin = Vector2.zero;
            left.rectTransform.anchorMax = new Vector2(0.6f, 1f);
            left.rectTransform.offsetMin = left.rectTransform.offsetMax = Vector2.zero;
            var leftT = left.transform;

            var brandTile = CreateSliced("BrandTile", leftT, roundFill, 8f, 24f, accent);
            PlaceTopLeft(brandTile.rectTransform, 120f, 72f, 32f, 32f);
            var glyphSprite = LoadSprite(IntroArt + "CraDev_Glyph.png");
            var brandGlyph = CreateImage("Glyph", brandTile.transform, glyphSprite, NativeSize(glyphSprite) * (32f / 150f), Vector2.zero, Color.white);
            brandGlyph.rectTransform.anchoredPosition = new Vector2(-8f * 32f / 150f, 0f);
            var brandText = CreateLabel("BrandName", leftT, display, "Cra<color=#3D5AFE>Dev</color>", 16, Color.white, TextAnchor.MiddleLeft);
            brandText.supportRichText = true;
            PlaceTopLeft(brandText.rectTransform, 166f, 72f, 200f, 32f);

            var form = CreateRect("Form", leftT);
            Place(form, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(120f, 0f), new Vector2(480f, 850f));

            PlaceTopLeft(CreateLabel("Eyebrow", form, bold, "NEW PLAYER", 13, accent, TextAnchor.UpperLeft).rectTransform, 0f, 0f, 480f, 20f);
            var title = CreateLabel("Title", form, display, "Create your\ncharacter", 44, Color.white, TextAnchor.UpperLeft);
            title.lineSpacing = 0.92f;
            PlaceTopLeft(title.rectTransform, 0f, 34f, 480f, 110f);
            var sub = CreateLabel("Subtitle", form, medium,
                "Pick a nickname and an avatar, then add your face from a photo.",
                17, UiMuted, TextAnchor.UpperLeft);
            sub.horizontalOverflow = HorizontalWrapMode.Wrap;
            sub.lineSpacing = 1.1f;
            PlaceTopLeft(sub.rectTransform, 0f, 146f, 440f, 60f);

            // Pasportdan olingan jins: faqat ko'rsatiladi, o'zgartirilmaydi
            var chip = CreateSliced("GenderChip", form, pillFill, 16f, 32f, new Color(1f, 1f, 1f, 0.06f));
            PlaceTopLeft(chip.rectTransform, 0f, 222f, 200f, 32f);
            chip.rectTransform.pivot = new Vector2(0f, 0.5f);
            chip.rectTransform.anchoredPosition = new Vector2(0f, -238f);
            var chipBorder = CreateSliced("Border", chip.transform, pillStroke, 16f, 32f, new Color(1f, 1f, 1f, 0.10f));
            Stretch(chipBorder.rectTransform);
            var chipIcon = CreateImage("Icon", chip.transform, Icon("Male"), new Vector2(16f, 16f), Vector2.zero, accent);
            Place(chipIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(22f, 0f), new Vector2(16f, 16f));
            var chipText = CreateLabel("Text", chip.transform, semiBold, "Male · from passport", 13, Color.white, TextAnchor.MiddleCenter);
            Place(chipText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 0f), new Vector2(300f, 24f));

            PlaceTopLeft(CreateLabel("NicknameLabel", form, bold, "NICKNAME", 12, UiMuted, TextAnchor.UpperLeft).rectTransform, 0f, 272f, 480f, 18f);

            var glow = CreateSliced("FieldGlow", form, roundFill, 16f, 24f, new Color(accent.r, accent.g, accent.b, 0.16f));
            PlaceTopLeft(glow.rectTransform, -4f, 297f, 488f, 68f);
            glow.enabled = false;
            var fieldBg = CreateSliced("NicknameField", form, roundFill, 12f, 24f, field);
            PlaceTopLeft(fieldBg.rectTransform, 0f, 301f, 480f, 60f);
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

            var helpIcon = CreateImage("HelpIcon", form, Icon("Check"), new Vector2(16f, 16f), Vector2.zero, UiMuted);
            Place(helpIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(8f, -382f), new Vector2(16f, 16f));
            var helpText = CreateLabel("HelpText", form, medium, "", 14, UiMuted, TextAnchor.MiddleLeft);
            Place(helpText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, -382f), new Vector2(470f, 22f));

            // Avatar kartalari: shu jinsdagi avatarlar (5 tagacha, ortiqcha kartalar yashiriladi)
            PlaceTopLeft(CreateLabel("AvatarLabel", form, bold, "AVATAR", 12, UiMuted, TextAnchor.UpperLeft).rectTransform, 0f, 412f, 480f, 18f);
            var cards = new AvatarCard[5];
            for (int i = 0; i < cards.Length; i++)
                cards[i] = CreateAvatarCard("AvatarCard" + (i + 1), form, i * 98f, 438f, roundFill, roundStroke, pillFill, Icon("Check"), accent, field, line);
            var avatarInfo = CreateLabel("AvatarInfo", form, medium, "Athletic · 183 cm", 15, UiMuted, TextAnchor.MiddleLeft);
            Place(avatarInfo.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, -606f), new Vector2(480f, 22f));

            // ---------- Yuz: kamera bilan suratga tushish yoki rasm yuklash ----------
            PlaceTopLeft(CreateLabel("FaceLabel", form, bold, "FACE", 12, UiMuted, TextAnchor.UpperLeft).rectTransform, 0f, 632f, 480f, 18f);
            var thumbFill = CreateSliced("FaceThumb", form, roundFill, 12f, 24f, field);
            PlaceTopLeft(thumbFill.rectTransform, 0f, 656f, 52f, 52f);
            var thumbIcon = CreateImage("Icon", thumbFill.transform, Icon("User"), new Vector2(22f, 22f), Vector2.zero, faint);
            var thumbMask = CreateRect("Mask", thumbFill.transform);
            Stretch(thumbMask);
            thumbMask.offsetMin = new Vector2(3f, 3f);
            thumbMask.offsetMax = new Vector2(-3f, -3f);
            thumbMask.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(2, 2);
            var thumb = new GameObject("Photo", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            thumb.rectTransform.SetParent(thumbMask, false);
            Stretch(thumb.rectTransform);
            thumb.raycastTarget = false;
            var thumbBorder = CreateSliced("Border", thumbFill.transform, roundStroke, 12f, 24f, line);
            Stretch(thumbBorder.rectTransform);
            // Yuzni olib tashlash: rasm burchagidagi kichik tugma
            var removeFill = CreateSliced("Remove", thumbFill.transform, pillFill, 9f, 32f, new Color32(48, 49, 56, 255));
            Place(removeFill.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-2f, -2f), new Vector2(18f, 18f));
            removeFill.raycastTarget = true;
            var removeButton = removeFill.gameObject.AddComponent<Button>();
            removeButton.targetGraphic = removeFill;
            CreateImage("Icon", removeFill.transform, Icon("Close"), new Vector2(10f, 10f), Vector2.zero, Color.white);

            var takeButton = CreateSecondaryButton("TakePhoto", form, "Take photo", DrawnIcon("Camera"), 64f, 660f, 200f, roundFill, roundStroke, semiBold, field, line);
            var uploadButton = CreateSecondaryButton("UploadPhoto", form, "Upload photo", DrawnIcon("Upload"), 276f, 660f, 204f, roundFill, roundStroke, semiBold, field, line);

            var faceStatusIcon = CreateImage("FaceStatusIcon", form, Icon("Check"), new Vector2(16f, 16f), Vector2.zero, UiMuted);
            Place(faceStatusIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(8f, -728f), new Vector2(16f, 16f));
            var faceStatus = CreateLabel("FaceStatus", form, medium, "", 14, UiMuted, TextAnchor.MiddleLeft);
            Place(faceStatus.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, -728f), new Vector2(470f, 22f));

            var ctaFill = CreateSliced("CreateButton", form, roundFill, 12f, 24f, accent);
            PlaceTopLeft(ctaFill.rectTransform, 0f, 752f, 480f, 60f);
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
            Place(errorIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(8f, -836f), new Vector2(16f, 16f));
            var errorText = CreateLabel("ErrorText", form, medium, "", 14, bad, TextAnchor.MiddleLeft);
            Place(errorText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(24f, -836f), new Vector2(456f, 22f));

            // ---------- O'ng tomon: qahramon ustidagi nom va pastdagi boshqaruv ----------
            // Qahramon ekran markazidan StageOffset qadar o'ngda turadi (AvatarViewer.screenOffset)
            float stageX = 0.5f + StageOffset;
            var plate = CreateSliced("Nameplate", stageT, pillFill, 22f, 32f, new Color(1f, 1f, 1f, 0.06f));
            Place(plate.rectTransform, new Vector2(stageX, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(200f, 44f));
            var plateBorder = CreateSliced("Border", plate.transform, pillStroke, 22f, 32f, new Color(1f, 1f, 1f, 0.10f));
            Stretch(plateBorder.rectTransform);
            var plateIcon = CreateImage("Icon", plate.transform, Icon("Male"), new Vector2(18f, 18f), Vector2.zero, accent);
            var plateText = CreateLabel("Text", plate.transform, bold, "Your nickname", 17, faint, TextAnchor.MiddleCenter);
            Place(plateText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 30f));

            // Old / Yon / Orqa tugmalari
            var switcher = CreateSliced("ViewSwitch", stageT, pillFill, 22f, 32f, new Color32(28, 29, 33, 255));
            Place(switcher.rectTransform, new Vector2(stageX, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 96f), new Vector2(284f, 44f));
            string[] viewNames = { "Front", "Side", "Back" };
            var viewButtons = new Button[3];
            var viewFills = new Image[3];
            var viewLabels = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                var fill = CreateSliced(viewNames[i] + "Button", switcher.transform, pillFill, 18f, 32f, new Color(0f, 0f, 0f, 0f));
                Place(fill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 92f, 0f), new Vector2(88f, 36f));
                fill.raycastTarget = true;
                var button = fill.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                var label = CreateLabel("Label", fill.transform, semiBold, viewNames[i], 14, UiMuted, TextAnchor.MiddleCenter);
                Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88f, 24f));
                viewButtons[i] = button;
                viewFills[i] = fill;
                viewLabels[i] = label;
            }

            // Boshqaruv bo'yicha qisqa ko'rsatma
            var hint = CreateRect("Hint", stageT);
            Place(hint, new Vector2(stageX, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 48f), new Vector2(300f, 20f));
            var hintIcon = CreateImage("Icon", hint, Icon("Rotate"), new Vector2(16f, 16f), Vector2.zero, faint);
            var hintText = CreateLabel("Text", hint, medium, "Drag to rotate · Scroll to zoom", 13, faint, TextAnchor.MiddleLeft);
            float hintWidth = 16f + 8f + hintText.preferredWidth;
            hintIcon.rectTransform.anchoredPosition = new Vector2(-hintWidth / 2f + 8f, 0f);
            Place(hintText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-hintWidth / 2f + 24f, 0f), new Vector2(hintText.preferredWidth + 4f, 20f));

            var viewer = stage.gameObject.AddComponent<AvatarViewer>();
            Set(viewer, "stageCamera", stageCamera);
            Set(viewer, "turntable", turntable);
            Set(viewer, "maleIdle", maleIdle);
            Set(viewer, "femaleIdle", femaleIdle);
            Set(viewer, "screenOffset", StageOffset);
            SetArray(viewer, "viewButtons", viewButtons);
            SetArray(viewer, "viewFills", viewFills);
            SetArray(viewer, "viewLabels", viewLabels);
            Set(viewer, "facePaint", FacePaintMaterial());

            // ---------- Kamera oynasi: suratga tushish ----------
            var modal = CreateFullscreen("FaceModal", root, new Color(0f, 0f, 0f, 0.82f));
            modal.raycastTarget = true; // orqadagi forma bosilmaydi
            var panel = CreateSliced("Panel", modal.transform, roundFill, 16f, 24f, new Color32(19, 20, 23, 255));
            Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 690f));
            var panelBorder = CreateSliced("Border", panel.transform, roundStroke, 16f, 24f, line);
            Stretch(panelBorder.rectTransform);
            PlaceTopLeft(CreateLabel("Title", panel.transform, display, "Take a photo", 24, Color.white, TextAnchor.MiddleLeft).rectTransform, 40f, 32f, 640f, 36f);

            var frame = CreateSliced("Frame", panel.transform, roundFill, 12f, 24f, Color.black);
            PlaceTopLeft(frame.rectTransform, 40f, 92f, 640f, 480f);
            var frameMask = CreateRect("Mask", frame.transform);
            Stretch(frameMask);
            frameMask.gameObject.AddComponent<RectMask2D>();
            var preview = new GameObject("Preview", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            preview.transform.SetParent(frameMask, false);
            var previewImage = preview.GetComponent<RawImage>();
            previewImage.raycastTarget = false;
            var fitter = preview.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            var oval = CreateImage("Oval", frameMask, OvalSprite(), new Vector2(280f, 360f), new Vector2(0f, 6f), new Color(1f, 1f, 1f, 0.75f));
            oval.raycastTarget = false;

            var modalStatus = CreateLabel("Status", panel.transform, medium, "", 15, UiMuted, TextAnchor.MiddleCenter);
            PlaceTopLeft(modalStatus.rectTransform, 40f, 580f, 640f, 24f);
            var cancelButton = CreateSecondaryButton("Cancel", panel.transform, "Cancel", null, 40f, 614f, 300f, roundFill, roundStroke, semiBold, field, line);
            var captureFill = CreateSliced("Capture", panel.transform, roundFill, 12f, 24f, accent);
            PlaceTopLeft(captureFill.rectTransform, 380f, 614f, 300f, 44f);
            captureFill.raycastTarget = true;
            var captureButton = captureFill.gameObject.AddComponent<Button>();
            captureButton.targetGraphic = captureFill;
            var captureLabel = CreateLabel("Label", captureFill.transform, bold, "Capture", 16, Color.white, TextAnchor.MiddleCenter);
            Place(captureLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 30f));

            var faceGo = new GameObject("FaceCapture");
            var faceCapture = faceGo.AddComponent<FaceCapture>();
            Set(faceCapture, "detectorModel", AssetDatabase.LoadAssetAtPath<ModelAsset>(FaceModels + "face_detector.onnx"));
            Set(faceCapture, "landmarkModel", AssetDatabase.LoadAssetAtPath<ModelAsset>(FaceModels + "face_landmarks_detector.onnx"));
            Set(faceCapture, "viewer", viewer);
            Set(faceCapture, "takeButton", takeButton);
            Set(faceCapture, "uploadButton", uploadButton);
            Set(faceCapture, "thumb", thumb);
            Set(faceCapture, "thumbIcon", thumbIcon);
            Set(faceCapture, "removeButton", removeButton);
            Set(faceCapture, "statusIcon", faceStatusIcon);
            Set(faceCapture, "statusText", faceStatus);
            Set(faceCapture, "modal", modal.gameObject);
            Set(faceCapture, "preview", previewImage);
            Set(faceCapture, "previewFitter", fitter);
            Set(faceCapture, "captureButton", captureButton);
            Set(faceCapture, "cancelButton", cancelButton);
            Set(faceCapture, "modalStatus", modalStatus);
            Set(faceCapture, "checkSprite", Icon("Check"));
            Set(faceCapture, "alertSprite", Icon("Alert"));
            Set(faceCapture, "spinnerSprite", Icon("Spinner"));

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
            Set(screen, "genderChip", chip.rectTransform);
            Set(screen, "genderIcon", chipIcon);
            Set(screen, "genderText", chipText);
            SetAvatars(screen, cardPictures, faceMaps);
            SetArray(screen, "avatarCards", cards);
            Set(screen, "avatarInfo", avatarInfo);
            Set(screen, "viewer", viewer);
            Set(screen, "faceCapture", faceCapture);
            Set(screen, "createButton", cta);
            Set(screen, "createFill", ctaFill);
            Set(screen, "createLabel", ctaLabel);
            Set(screen, "createIcon", ctaIcon);
            Set(screen, "errorIcon", errorIcon);
            Set(screen, "errorText", errorText);
            Set(screen, "nameplate", plate.rectTransform);
            Set(screen, "nameplateIcon", plateIcon);
            Set(screen, "nameplateText", plateText);
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

        /// <summary>Qahramon ekran markazidan qancha o'ngda turadi (ekran kengligiga nisbatan).</summary>
        const float StageOffset = 0.17f;

        /// <summary>3D modeli bor avatarlarni ekranning "avatars" ro'yxatiga yozadi.</summary>
        static void SetAvatars(CharacterCreationScreen screen, System.Collections.Generic.Dictionary<string, Sprite> cards,
            System.Collections.Generic.Dictionary<string, FaceMap> faceMaps)
        {
            var so = new SerializedObject(screen);
            var list = so.FindProperty("avatars");
            list.arraySize = 0;
            foreach (var info in AvatarList)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(info.ModelPath);
                if (model == null)
                {
                    Debug.Log($"[CraDev] {info.Id} avatarining 3D modeli yo'q ({info.ModelPath}), ro'yxatga qo'shilmadi.");
                    continue;
                }
                int index = list.arraySize;
                list.arraySize++;
                var item = list.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("id").stringValue = info.Id;
                item.FindPropertyRelative("gender").stringValue = info.Gender;
                item.FindPropertyRelative("title").stringValue = info.Title;
                item.FindPropertyRelative("heightCm").intValue = info.Height;
                item.FindPropertyRelative("model").objectReferenceValue = model;
                cards.TryGetValue(info.Id, out var card);
                item.FindPropertyRelative("card").objectReferenceValue = card;

                var uvProp = item.FindPropertyRelative("faceUv");
                var triProp = item.FindPropertyRelative("faceTriangles");
                faceMaps.TryGetValue(info.Id, out var map);
                uvProp.arraySize = map?.Uv.Length ?? 0;
                for (int i = 0; i < uvProp.arraySize; i++)
                    uvProp.GetArrayElementAtIndex(i).vector2Value = map.Uv[i];
                triProp.arraySize = map?.Triangles.Length ?? 0;
                for (int i = 0; i < triProp.arraySize; i++)
                    triProp.GetArrayElementAtIndex(i).intValue = map.Triangles[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- 3D sahna

        /// <summary>
        /// Qahramon turadigan studiya: to'q pol (uzoqda tuman bilan fonga qo'shilib ketadi), past dumaloq taglik,
        /// asosiy yorug'lik (soya bilan) va orqadan chiziq yorug'lik. Qaytaradi: qahramon qo'yiladigan aylanma nuqta.
        /// </summary>
        static Transform BuildStage(Camera camera)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.cullingMask = ~(1 << LayerMask.NameToLayer("UI"));
            camera.fieldOfView = 26f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 60f;
            camera.allowMSAA = true;
            camera.transform.SetPositionAndRotation(new Vector3(-1.6f, 0.95f, -4.8f), Quaternion.identity);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Background;
            RenderSettings.fogStartDistance = 6f;
            RenderSettings.fogEndDistance = 16f;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.22f, 0.24f, 0.28f);
            RenderSettings.ambientEquatorColor = new Color(0.15f, 0.16f, 0.18f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.05f, 0.06f);

            var stage = new GameObject("Stage").transform;

            var floor = Primitive(PrimitiveType.Plane, "Floor", stage, StageMaterial("Floor", new Color32(26, 27, 31, 255), 0.05f));
            floor.localScale = new Vector3(4f, 1f, 4f);

            // Taglik: 3 sm balandlikdagi silliq disk
            const float plinthTop = 0.03f;
            var plinth = Primitive(PrimitiveType.Cylinder, "Plinth", stage, StageMaterial("Plinth", new Color32(36, 37, 42, 255), 0.15f));
            plinth.GetComponent<MeshFilter>().sharedMesh = DiscMesh(0.55f, plinthTop, 128);

            // Studiya yorug'ligi: yuqoridan qahramonga qaratilgan projektorlar. Pol faqat qahramon atrofida
            // yoritiladi va chetlarga qarab qorong'ilashadi.
            var key = Spot("KeyLight", stage, new Vector3(1.4f, 3.4f, -2.4f), new Vector3(0f, 1.1f, 0f), new Color(1f, 0.95f, 0.88f), 2.4f, 42f);
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.8f;
            key.shadowBias = 0.02f;
            key.shadowNormalBias = 0.3f;
            Spot("RimLight", stage, new Vector3(-1.2f, 3f, 2.4f), new Vector3(0f, 1.3f, 0f), new Color(0.7f, 0.78f, 1f), 3f, 40f);
            Spot("FillLight", stage, new Vector3(-2.6f, 1.8f, -2f), new Vector3(0f, 1.2f, 0f), new Color(0.85f, 0.88f, 1f), 0.9f, 45f);

            var turntable = new GameObject("Turntable").transform;
            turntable.SetParent(stage, false);
            turntable.localPosition = new Vector3(0f, plinthTop, 0f);
            return turntable;
        }

        /// <summary>Ko'p qirrali (silliq ko'rinadigan) disk: ust tomoni, yon devori. Mesh fayl sifatida saqlanadi.</summary>
        static Mesh DiscMesh(float radius, float height, int segments)
        {
            string path = StageMaterials + "Plinth.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = "Plinth" };
                Directory.CreateDirectory(StageMaterials);
                AssetDatabase.CreateAsset(mesh, path);
            }
            var vertices = new System.Collections.Generic.List<Vector3>();
            var normals = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();
            // Ust tomoni: markaz + aylana
            vertices.Add(new Vector3(0f, height, 0f));
            normals.Add(Vector3.up);
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                vertices.Add(new Vector3(Mathf.Cos(a) * radius, height, Mathf.Sin(a) * radius));
                normals.Add(Vector3.up);
                if (i > 0)
                    triangles.AddRange(new[] { 0, i + 1, i });
            }
            // Yon devor
            int side = vertices.Count;
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                var n = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                vertices.Add(n * radius + Vector3.up * height);
                vertices.Add(n * radius);
                normals.Add(n);
                normals.Add(n);
                if (i > 0)
                {
                    int v = side + i * 2;
                    triangles.AddRange(new[] { v - 2, v, v - 1, v, v + 1, v - 1 });
                }
            }
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static Transform Primitive(PrimitiveType type, string name, Transform parent, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        static Light NewLight(string name, Transform parent, LightType type, Color color, float intensity, Quaternion rotation)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localRotation = rotation;
            var light = go.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            return light;
        }

        static Light Spot(string name, Transform parent, Vector3 position, Vector3 target, Color color, float intensity, float angle)
        {
            var light = NewLight(name, parent, LightType.Spot, color, intensity, Quaternion.LookRotation(target - position));
            light.transform.localPosition = position;
            light.range = 12f;
            light.spotAngle = angle;
            light.innerSpotAngle = angle * 0.4f;
            light.renderMode = LightRenderMode.ForcePixel;
            return light;
        }

        /// <summary>Sahna materiali (Standard shader): yo'q bo'lsa yaratadi, bor bo'lsa rangini yangilaydi.</summary>
        static Material StageMaterial(string name, Color color, float smoothness)
        {
            string path = StageMaterials + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Directory.CreateDirectory(StageMaterials);
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Bitta idle animatsiyali Animator Controller (yo'q bo'lsa yaratadi).</summary>
        static RuntimeAnimatorController IdleController(string clipFile, string name)
        {
            string path = AvatarAnimations + name + ".controller";
            var existing = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
            if (existing != null)
                return existing;
            var clip = AssetDatabase.LoadAllAssetsAtPath(AvatarAnimations + clipFile + ".fbx")
                .OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null)
                throw new FileNotFoundException("Idle animatsiyasi topilmadi: " + AvatarAnimations + clipFile + ".fbx");
            return UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPathWithClip(path, clip);
        }

        static AnimationClip ControllerClip(RuntimeAnimatorController controller) => controller.animationClips.FirstOrDefault();

        /// <summary>
        /// Har bir avatar uchun kartadagi rasmni 3D modeldan chizadi (shaffof fonda, idle holatida)
        /// va Assets/CraDev/Avatars/Cards/&lt;ID&gt;.png ga yozadi.
        /// </summary>
        static System.Collections.Generic.Dictionary<string, Sprite> RenderAvatarCards(RuntimeAnimatorController maleIdle, RuntimeAnimatorController femaleIdle)
        {
            const int width = 176, height = 300; // karta rasmi 2x o'lchamda
            var result = new System.Collections.Generic.Dictionary<string, Sprite>();
            if (!CanRender)
            {
                // Grafikasiz muhitda (CI) oldin chizilgan rasmlar ishlatiladi
                foreach (var info in AvatarList)
                    if (File.Exists(AvatarCards + info.Id + ".png"))
                        result[info.Id] = LoadSprite(AvatarCards + info.Id + ".png");
                return result;
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory(AvatarCards);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.38f, 0.40f, 0.45f);
            RenderSettings.ambientEquatorColor = new Color(0.2f, 0.21f, 0.23f);
            RenderSettings.ambientGroundColor = new Color(0.06f, 0.06f, 0.07f);
            NewLight("Key", null, LightType.Directional, new Color(1f, 0.96f, 0.9f), 1.2f, Quaternion.Euler(30f, 25f, 0f));
            NewLight("Rim", null, LightType.Directional, new Color(0.72f, 0.8f, 1f), 0.8f, Quaternion.Euler(18f, 200f, 0f));

            var camera = new GameObject("CardCamera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.fieldOfView = 18f;
            camera.aspect = (float)width / height;
            var rt = new RenderTexture(width * 2, height * 2, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            camera.targetTexture = rt;

            foreach (var info in AvatarList)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(info.ModelPath);
                if (model == null)
                    continue;
                var go = (GameObject)Object.Instantiate(model);
                go.transform.rotation = Quaternion.Euler(0f, ModelFacing, 0f);
                var clip = ControllerClip(info.Gender == "female" ? femaleIdle : maleIdle);
                if (clip != null)
                    clip.SampleAnimation(go, 0.5f);

                var bounds = new Bounds(go.transform.position, Vector3.zero);
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                    bounds.Encapsulate(r.bounds);
                float half = bounds.size.y * 0.54f;
                float distance = half / Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z - distance);
                camera.transform.rotation = Quaternion.identity;
                camera.Render();

                // 2x chizib, yarmiga kichraytiramiz: qirralar silliqroq bo'ladi
                var small = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(rt, small);
                var previous = RenderTexture.active;
                RenderTexture.active = small;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(small);

                string path = AvatarCards + info.Id + ".png";
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(go);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                result[info.Id] = LoadSprite(path);
            }

            camera.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            return result;
        }

        /// <summary>Rocketbox modellarini kameraga (-Z tomonga) qaratish uchun burilish.</summary>
        const float ModelFacing = 180f;

        /// <summary>Chapdan o'ngga shaffoflashib boruvchi soya: 3D sahna ustida forma o'qilishi uchun.</summary>
        static Sprite ShadeSprite()
        {
            string path = UiArt + "UI_Shade.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(256, 4, TextureFormat.RGBA32, false);
                for (int x = 0; x < 256; x++)
                {
                    float t = x / 255f;
                    float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, t));
                    for (int y = 0; y < 4; y++)
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }
            return LoadSprite(path);
        }

        static AvatarCard CreateAvatarCard(string name, Transform parent, float x, float y, Sprite roundFill, Sprite roundStroke,
            Sprite pill, Sprite check, Color accent, Color field, Color line)
        {
            var fill = CreateSliced(name, parent, roundFill, 12f, 24f, field);
            PlaceTopLeft(fill.rectTransform, x, y, 88f, 150f);
            fill.raycastTarget = true;
            var picture = CreateImage("Picture", fill.transform, null, Vector2.zero, Vector2.zero, Color.white);
            picture.rectTransform.anchorMin = Vector2.zero;
            picture.rectTransform.anchorMax = Vector2.one;
            picture.rectTransform.offsetMin = new Vector2(6f, 6f);
            picture.rectTransform.offsetMax = new Vector2(-6f, -6f);
            picture.preserveAspect = true;
            var border = CreateSliced("Border", fill.transform, roundStroke, 12f, 24f, line);
            Stretch(border.rectTransform);

            var tick = CreateImage("Tick", fill.transform, pill, new Vector2(18f, 18f), Vector2.zero, accent);
            Place(tick.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-14f, -14f), new Vector2(18f, 18f));
            CreateImage("Check", tick.transform, check, new Vector2(12f, 12f), Vector2.zero, Color.white);

            var button = fill.gameObject.AddComponent<Button>();
            button.targetGraphic = border;
            var colors = button.colors;
            colors.normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.selectedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            var card = fill.gameObject.AddComponent<AvatarCard>();
            Set(card, "fill", fill);
            Set(card, "border", border);
            Set(card, "picture", picture);
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

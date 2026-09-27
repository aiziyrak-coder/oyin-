using System.IO;
using System.Linq;
using CraDev.CDCGroup;
using CraDev.Intro;
using CraDev.Loading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static CraDev.EditorTools.UiBuild;

namespace CraDev.EditorTools
{
    /// <summary>
    /// "CraDev > Sahnalarni yaratish" menyusi: o'yin boshidagi sahnalarni noldan quradi va
    /// Build Settings'da shu tartibda birinchi o'rinlarga qo'yadi:
    /// Intro (CraDev) -> CDCGroup -> Loading.
    /// Sahnalarni qo'lda yig'ish shart emas: istalgan payt shu buyruq bilan qayta yaratish mumkin.
    /// </summary>
    static class CraDevSceneBuilder
    {
        const string ScenesFolder = "Assets/CraDev/Scenes/";
        const string IntroScene = ScenesFolder + "Intro.unity";
        const string CdcScene = ScenesFolder + "CDCGroup.unity";
        const string LoadingScene = ScenesFolder + "Loading.unity";

        const string IntroArt = "Assets/CraDev/Intro/Art/";
        const string CdcArt = "Assets/CraDev/CDCGroup/Art/";
        const string LoadingArt = "Assets/CraDev/Loading/Art/";
        const string FontPath = "Assets/CraDev/Common/Fonts/Rajdhani-SemiBold.ttf";

        [MenuItem("CraDev/Sahnalarni yaratish (Intro, CDCGroup, Loading)", priority = 1)]
        static void BuildAll()
        {
            string[] scenes = { IntroScene, CdcScene, LoadingScene };
            if (scenes.Any(File.Exists) &&
                !EditorUtility.DisplayDialog("CraDev sahnalari",
                    "Intro, CDCGroup va Loading sahnalari noldan qayta yaratiladi. Ularda qo'lda qilingan o'zgarishlar yo'qoladi. Davom etamizmi?",
                    "Ha, yaratish", "Bekor qilish"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            try
            {
                BuildIntro();
                BuildCdcGroup();
                BuildLoading();
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

            if (PlayerSettings.companyName == "DefaultCompany")
                PlayerSettings.companyName = "CraDev";

            EditorSceneManager.OpenScene(IntroScene);
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(IntroScene));
            Debug.Log("[CraDev] Sahnalar yaratildi: Intro -> CDCGroup -> Loading. Ko'rish uchun Intro sahnasida Play tugmasini bosing.");
        }

        // ---------------------------------------------------------------- CraDev intro

        static void BuildIntro()
        {
            // Joylashuvlar Design/CraDev/layout.json dan olingan (ekran markaziga nisbatan)
            var emblemPos = new Vector2(0f, 140f);
            var wordmarkPos = new Vector2(2.5f, -70.5f);
            var wordmarkGlowPos = new Vector2(-2.5f, -71f);
            var dividerPos = new Vector2(0f, -167.5f);
            var taglinePos = new Vector2(-0.5f, -205f);

            var root = NewUiScene(out var scene);
            CreateFullscreen("Background", root, new Color32(5, 7, 13, 255));
            var particles = CreateParticles(root, LoadSprite(IntroArt + "CraDev_Particle.png"), 70,
                new Color(0.25f, 0.85f, 1f), new Color(0.62f, 0.40f, 1f), new Vector2(4f, 12f));

            var logo = CreateRect("Logo", root);
            var glow = CreateImage("Glow", logo, LoadSprite(IntroArt + "CraDev_Glow.png"), new Vector2(1100f, 1100f), emblemPos, Color.white);
            var emblem = CreateSprite("Emblem", logo, LoadSprite(IntroArt + "CraDev_Emblem.png"), emblemPos);

            // Yozuvni markazdan ochish uchun yumshoq chetli RectMask2D
            var reveal = CreateRect("WordmarkReveal", logo);
            reveal.anchoredPosition = wordmarkPos;
            reveal.sizeDelta = new Vector2(820f, 260f);
            reveal.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(90, 0);
            var wordmarkGlow = CreateSprite("WordmarkGlow", reveal, LoadSprite(IntroArt + "CraDev_WordmarkGlow.png"), wordmarkGlowPos - wordmarkPos);
            var wordmark = CreateSprite("Wordmark", reveal, LoadSprite(IntroArt + "CraDev_Wordmark.png"), Vector2.zero);

            // Nur faqat harflar ustida ko'rinishi uchun yozuvning o'zi niqob (Mask) vazifasini bajaradi
            wordmark.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var shine = CreateImage("Shine", wordmark.transform, LoadSprite(IntroArt + "CraDev_Shine.png"),
                new Vector2(80f, 260f), Vector2.zero, new Color(1f, 1f, 1f, 0f));
            shine.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -18f);

            var divider = CreateSprite("Divider", logo, LoadSprite(IntroArt + "CraDev_Divider.png"), dividerPos);
            var tagline = CreateSprite("Tagline", logo, LoadSprite(IntroArt + "CraDev_Tagline.png"), taglinePos);
            var fader = CreateFullscreen("Fader", root, new Color(0f, 0f, 0f, 0f));

            var director = new GameObject("IntroDirector");
            var sound = AddSound(director, "Assets/CraDev/Intro/Audio/CraDev_IntroSound.wav");
            var sequence = director.AddComponent<IntroSequence>();
            Set(sequence, "nextScene", "CDCGroup");
            Set(sequence, "fader", fader);
            Set(sequence, "sound", sound);
            Set(sequence, "fadeInDuration", 0.8f);
            Set(sequence, "soundStart", 0.6f);
            Set(sequence, "fadeOutStart", 5.6f);
            Set(sequence, "fadeOutDuration", 1.0f);
            Set(sequence, "logoRoot", logo);
            Set(sequence, "glow", glow);
            Set(sequence, "emblem", emblem);
            Set(sequence, "wordmarkReveal", reveal);
            Set(sequence, "wordmarkGlow", wordmarkGlow);
            Set(sequence, "shine", shine);
            Set(sequence, "divider", divider);
            Set(sequence, "tagline", tagline);
            Set(sequence, "particles", particles);

            Save(scene, IntroScene);
        }

        // ---------------------------------------------------------------- CDCGroup

        static void BuildCdcGroup()
        {
            // Joylashuvlar Design/CDCGroup/layout.json dan olingan
            var emblemPos = new Vector2(-211.5f, 0f);
            var wordmarkPos = new Vector2(118.5f, -10f);
            var highlightPos = new Vector2(3f, 0f);
            var linePos = new Vector2(0f, -161f);

            var root = NewUiScene(out var scene);
            CreateFullscreen("Background", root, new Color32(6, 6, 7, 255));
            var particles = CreateParticles(root, LoadSprite(IntroArt + "CraDev_Particle.png"), 45,
                new Color(0.95f, 0.96f, 1f), new Color(0.62f, 0.66f, 0.72f), new Vector2(3f, 9f));

            var lockup = CreateRect("Lockup", root);
            var glow = CreateImage("Glow", lockup, LoadSprite(CdcArt + "CDC_Glow.png"), new Vector2(1000f, 1000f), Vector2.zero, Color.white);
            var emblem = CreateSprite("Emblem", lockup, LoadSprite(CdcArt + "CDC_Emblem.png"), emblemPos);

            // Yozuv chapdan o'ngga ochiladi: niqobning chap cheti qimirlamaydi, faqat kengligi o'sadi
            var wordmarkSprite = LoadSprite(CdcArt + "CDC_Wordmark.png");
            float wordmarkLeft = wordmarkPos.x - NativeSize(wordmarkSprite).x / 2f;
            const float revealMargin = 70f; // chap yumshoq chet (60) harflarga tegmasligi uchun
            var reveal = CreateRect("WordmarkReveal", lockup);
            reveal.pivot = new Vector2(0f, 0.5f);
            reveal.anchoredPosition = new Vector2(wordmarkLeft - revealMargin, wordmarkPos.y);
            reveal.sizeDelta = new Vector2(520f, 220f);
            reveal.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(60, 0);
            var wordmark = CreateSprite("Wordmark", reveal, wordmarkSprite, Vector2.zero);
            wordmark.rectTransform.anchorMin = wordmark.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            wordmark.rectTransform.anchoredPosition = new Vector2(revealMargin + NativeSize(wordmarkSprite).x / 2f, 0f);

            // Yaltirash: tor, yumshoq chetli oyna ichida logoning oq nusxasi
            var glint = CreateRect("GlintWindow", lockup);
            glint.sizeDelta = new Vector2(220f, 260f);
            glint.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(80, 0);
            var highlight = CreateImage("Highlight", glint, LoadSprite(CdcArt + "CDC_Highlight.png"),
                NativeSize(LoadSprite(CdcArt + "CDC_Highlight.png")), highlightPos, new Color(1f, 1f, 1f, 0f));

            var line = CreateSprite("Line", lockup, LoadSprite(CdcArt + "CDC_Line.png"), linePos);
            var fader = CreateFullscreen("Fader", root, new Color(0f, 0f, 0f, 0f));

            var director = new GameObject("CDCGroupDirector");
            var sound = AddSound(director, "Assets/CraDev/CDCGroup/Audio/CDC_Sound.wav");
            var splash = director.AddComponent<CdcGroupSplash>();
            Set(splash, "nextScene", "Loading");
            Set(splash, "fader", fader);
            Set(splash, "sound", sound);
            Set(splash, "fadeInDuration", 0.6f);
            Set(splash, "soundStart", 0.35f);
            Set(splash, "fadeOutStart", 4.0f);
            Set(splash, "fadeOutDuration", 0.8f);
            Set(splash, "lockup", lockup);
            Set(splash, "glow", glow);
            Set(splash, "emblem", emblem);
            Set(splash, "wordmarkReveal", reveal);
            Set(splash, "wordmark", wordmark.rectTransform);
            Set(splash, "glintWindow", glint);
            Set(splash, "glintHighlight", highlight);
            Set(splash, "line", line);
            Set(splash, "particles", particles);

            Save(scene, CdcScene);
        }

        // ---------------------------------------------------------------- Loading

        static void BuildLoading()
        {
            var center = new Vector2(0f, 40f);
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
                throw new FileNotFoundException("Shrift topilmadi: " + FontPath);

            var root = NewUiScene(out var scene);
            CreateFullscreen("Background", root, new Color32(5, 7, 13, 255));
            CreateImage("Glow", root, LoadSprite(IntroArt + "CraDev_Glow.png"), new Vector2(900f, 900f), center, new Color(1f, 1f, 1f, 0.16f));
            var particles = CreateParticles(root, LoadSprite(IntroArt + "CraDev_Particle.png"), 50,
                new Color(0.25f, 0.85f, 1f), new Color(0.62f, 0.40f, 1f), new Vector2(3f, 10f));

            var spinner = CreateRect("Spinner", root);
            spinner.anchoredPosition = center;
            var hex = CreateSprite("Hex", spinner, LoadSprite(LoadingArt + "Loading_Hex.png"), Vector2.zero);
            hex.color = new Color(0.55f, 0.85f, 1f, 0.22f);
            var arcSprite = LoadSprite(LoadingArt + "Loading_Arc.png");
            var outer = CreateImage("ArcOuter", spinner, arcSprite, new Vector2(300f, 300f), Vector2.zero, new Color(0.18f, 0.9f, 1f, 1f));
            var inner = CreateImage("ArcInner", spinner, arcSprite, new Vector2(210f, 210f), Vector2.zero, new Color(0.62f, 0.36f, 1f, 0.7f));
            var percent = CreateText("Percent", spinner, font, "0%", 60, Vector2.zero, new Color32(223, 231, 245, 255));

            var status = CreateText("Status", root, font, "L O A D I N G", 22, new Vector2(0f, -150f), new Color32(131, 145, 173, 255));

            var bar = CreateImage("Bar", root, null, new Vector2(560f, 4f), new Vector2(0f, -195f), new Color(1f, 1f, 1f, 0.08f));
            var fill = CreateImage("Fill", bar.transform, LoadSprite(LoadingArt + "Loading_BarFill.png"), Vector2.zero, Vector2.zero, Color.white);
            Stretch(fill.rectTransform);
            fill.rectTransform.anchorMax = new Vector2(0.65f, 1f); // tahrirlashda ko'rinishi uchun; o'yinda skript boshqaradi
            var head = CreateImage("Head", fill.transform, LoadSprite(IntroArt + "CraDev_Particle.png"), new Vector2(44f, 44f), Vector2.zero,
                new Color(0.45f, 0.9f, 1f, 0.9f));
            head.rectTransform.anchorMin = head.rectTransform.anchorMax = new Vector2(1f, 0.5f);

            var fader = CreateFullscreen("Fader", root, new Color(0f, 0f, 0f, 0f));

            var director = new GameObject("LoadingDirector");
            var loading = director.AddComponent<LoadingScreen>();
            Set(loading, "defaultScene", "MainMenu");
            Set(loading, "fader", fader);
            Set(loading, "spinnerOuter", outer.rectTransform);
            Set(loading, "spinnerInner", inner.rectTransform);
            Set(loading, "hex", hex);
            Set(loading, "barFill", fill.rectTransform);
            Set(loading, "barHead", head);
            Set(loading, "percentText", percent);
            Set(loading, "statusText", status);
            Set(loading, "particles", particles);

            Save(scene, LoadingScene);
        }
    }
}

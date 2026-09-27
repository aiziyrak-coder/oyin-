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
        const string FontLight = "Assets/CraDev/Common/Fonts/Manrope-ExtraLight.ttf";
        const string FontSemiBold = "Assets/CraDev/Common/Fonts/Manrope-SemiBold.ttf";

        // Barcha sahnalar uchun bitta tekis fon
        static readonly Color Background = new Color32(11, 11, 12, 255);
        static readonly Color Muted = new Color32(142, 147, 154, 255);

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
            Set(loading, "defaultScene", "MainMenu");
            Set(loading, "fader", fader);
            Set(loading, "percentText", percent);
            Set(loading, "statusText", status);
            Set(loading, "barFill", fill.rectTransform);

            Save(scene, LoadingScene);
        }
    }
}

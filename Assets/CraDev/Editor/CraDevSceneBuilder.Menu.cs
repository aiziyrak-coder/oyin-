using System.Collections.Generic;
using System.IO;
using CraDev.CharacterCreation;
using CraDev.MainMenu;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UI;
using static CraDev.EditorTools.UiBuild;

namespace CraDev.EditorTools
{
    /// <summary>
    /// Bosh menyu sahnasi va ekranlar uchun umumiy oynalar: tasdiqlash (Quit?), sozlamalar, tugma ovozlari,
    /// o'yin ikonkasi.
    /// </summary>
    static partial class CraDevSceneBuilder
    {
        const string UiAudio = "Assets/CraDev/UI/Audio/";

        /// <summary>UI qurish uchun shriftlar, rasmlar va ranglar.</summary>
        class UiKit
        {
            public Font Bold, SemiBold, Medium, Display;
            public Sprite RoundFill, RoundStroke, PillFill, PillStroke;
            public readonly Color Accent = new Color32(61, 90, 254, 255);
            public readonly Color Field = new Color32(22, 23, 26, 255);
            public readonly Color Line = new Color32(38, 39, 44, 255);
            public readonly Color Faint = new Color32(90, 94, 102, 255);
            public readonly Color Panel = new Color32(19, 20, 23, 255);
            public Sprite Icon(string name) => LoadSprite(UiArt + "Icon_" + name + ".png");
        }

        static UiKit Kit() => new UiKit
        {
            Bold = LoadFont("Manrope-Bold.ttf"),
            SemiBold = LoadFont("Manrope-SemiBold.ttf"),
            Medium = LoadFont("Manrope-Medium.ttf"),
            Display = LoadFont("Unbounded-Bold.ttf"),
            RoundFill = LoadSlicedSprite(UiArt + "UI_Round12_Fill.png"),
            RoundStroke = LoadSlicedSprite(UiArt + "UI_Round12_Stroke.png"),
            PillFill = LoadSlicedSprite(UiArt + "UI_Pill_Fill.png"),
            PillStroke = LoadSlicedSprite(UiArt + "UI_Pill_Stroke.png"),
        };

        // ---------------------------------------------------------------- Bosh menyu

        static void BuildMainMenu()
        {
            var kit = Kit();
            var maleIdle = IdleController("m_idle_neutral_01", "Idle_Male");
            var femaleIdle = IdleController("f_idle_neutral_01", "Idle_Female");

            var root = NewUiScene(out var scene);
            root.gameObject.AddComponent<GraphicRaycaster>();
            CreateEventSystem();

            var stageCamera = Object.FindFirstObjectByType<Camera>();
            var turntable = BuildStage(stageCamera);
            var stage = CreateFullscreen("StageInput", root, new Color(0f, 0f, 0f, 0f));
            stage.raycastTarget = true;

            var left = CreateImage("LeftShade", root, ShadeSprite(), Vector2.zero, Vector2.zero, new Color(Background.r, Background.g, Background.b, 0.94f));
            left.rectTransform.anchorMin = Vector2.zero;
            left.rectTransform.anchorMax = new Vector2(0.6f, 1f);
            left.rectTransform.offsetMin = left.rectTransform.offsetMax = Vector2.zero;
            CreateBrand(left.transform, kit);

            // ---------- Menyu ----------
            var menu = CreateRect("Menu", left.transform);
            Place(menu, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(120f, 0f), new Vector2(480f, 480f));
            var menuGroup = menu.gameObject.AddComponent<CanvasGroup>();

            PlaceTopLeft(CreateLabel("Eyebrow", menu, kit.Bold, "WELCOME BACK", 13, kit.Accent, TextAnchor.UpperLeft).rectTransform, 0f, 0f, 480f, 20f);
            var nickname = CreateLabel("Nickname", menu, kit.Display, "Player", 52, Color.white, TextAnchor.MiddleLeft);
            PlaceTopLeft(nickname.rectTransform, 0f, 26f, 480f, 72f);
            var avatarInfo = CreateLabel("AvatarInfo", menu, kit.Medium, "", 17, UiMuted, TextAnchor.MiddleLeft);
            PlaceTopLeft(avatarInfo.rectTransform, 0f, 100f, 480f, 26f);

            var (play, _) = CreateAccentButton("Play", menu, "Play", kit.Icon("Arrow"), 0f, 166f, 360f, 64f, kit, 19);
            var customize = CreateSecondaryButton("Customize", menu, "Customize", null, 0f, 246f, 360f, kit.RoundFill, kit.RoundStroke, kit.SemiBold, kit.Field, kit.Line);
            var settingsButton = CreateSecondaryButton("Settings", menu, "Settings", null, 0f, 310f, 360f, kit.RoundFill, kit.RoundStroke, kit.SemiBold, kit.Field, kit.Line);
            var quit = CreateSecondaryButton("Quit", menu, "Quit", null, 0f, 374f, 360f, kit.RoundFill, kit.RoundStroke, kit.SemiBold, kit.Field, kit.Line);
            foreach (var b in new[] { customize, settingsButton, quit })
                ((RectTransform)b.transform).sizeDelta = new Vector2(360f, 52f);
            var toast = CreateLabel("Toast", menu, kit.Medium, "", 15, Color.white, TextAnchor.MiddleLeft);
            PlaceTopLeft(toast.rectTransform, 0f, 446f, 480f, 24f);

            // ---------- Pastki chap: server holati va versiya ----------
            var status = CreateRect("Status", root);
            Place(status, Vector2.zero, new Vector2(0f, 0.5f), new Vector2(120f, 56f), new Vector2(400f, 20f));
            var dot = CreateImage("Dot", status, kit.PillFill, new Vector2(8f, 8f), Vector2.zero, UiMuted);
            Place(dot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(8f, 8f));
            var statusText = CreateLabel("Text", status, kit.SemiBold, "Connecting…", 13, UiMuted, TextAnchor.MiddleLeft);
            Place(statusText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(160f, 20f));
            var version = CreateLabel("Version", status, kit.Medium, "v0.1.0", 13, kit.Faint, TextAnchor.MiddleLeft);
            Place(version.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(92f, 0f), new Vector2(160f, 20f));

            // Qahramon ostidagi ko'rsatma
            float stageX = 0.5f + StageOffset;
            var hint = CreateRect("Hint", stage.transform);
            Place(hint, new Vector2(stageX, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 48f), new Vector2(300f, 20f));
            var hintIcon = CreateImage("Icon", hint, kit.Icon("Rotate"), new Vector2(16f, 16f), Vector2.zero, kit.Faint);
            var hintText = CreateLabel("Text", hint, kit.Medium, "Drag to rotate · Scroll to zoom", 13, kit.Faint, TextAnchor.MiddleLeft);
            float hintWidth = 16f + 8f + hintText.preferredWidth;
            hintIcon.rectTransform.anchoredPosition = new Vector2(-hintWidth / 2f + 8f, 0f);
            Place(hintText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-hintWidth / 2f + 24f, 0f), new Vector2(hintText.preferredWidth + 4f, 20f));

            var viewer = stage.gameObject.AddComponent<AvatarViewer>();
            Set(viewer, "stageCamera", stageCamera);
            Set(viewer, "turntable", turntable);
            Set(viewer, "maleIdle", maleIdle);
            Set(viewer, "femaleIdle", femaleIdle);
            Set(viewer, "screenOffset", StageOffset);
            Set(viewer, "facePaint", FacePaintMaterial());
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
            Set(screen, "nicknameText", nickname);
            Set(screen, "avatarInfoText", avatarInfo);
            Set(screen, "toastText", toast);
            Set(screen, "statusDot", dot);
            Set(screen, "statusText", statusText);
            Set(screen, "versionText", version);
            Set(screen, "menu", menu);
            Set(screen, "menuGroup", menuGroup);
            Set(screen, "playButton", play);
            Set(screen, "customizeButton", customize);
            Set(screen, "settingsButton", settingsButton);
            Set(screen, "quitButton", quit);
            Set(screen, "dialog", dialog);
            Set(screen, "settings", settings);
            Set(screen, "fader", fader);

            Save(scene, MenuScene);
        }

        /// <summary>Chap yuqoridagi CraDev belgisi va nomi.</summary>
        static void CreateBrand(Transform parent, UiKit kit)
        {
            var tile = CreateSliced("BrandTile", parent, kit.RoundFill, 8f, 24f, kit.Accent);
            PlaceTopLeft(tile.rectTransform, 120f, 72f, 32f, 32f);
            var glyphSprite = LoadSprite(IntroArt + "CraDev_Glyph.png");
            var glyph = CreateImage("Glyph", tile.transform, glyphSprite, NativeSize(glyphSprite) * (32f / 150f), Vector2.zero, Color.white);
            glyph.rectTransform.anchoredPosition = new Vector2(-8f * 32f / 150f, 0f);
            var text = CreateLabel("BrandName", parent, kit.Display, "Cra<color=#3D5AFE>Dev</color>", 16, Color.white, TextAnchor.MiddleLeft);
            text.supportRichText = true;
            PlaceTopLeft(text.rectTransform, 166f, 72f, 200f, 32f);
        }

        /// <summary>Asosiy (ko'k) tugma: matn va ixtiyoriy ikonka (matndan keyin) markazda.</summary>
        static (Button, Text) CreateAccentButton(string name, Transform parent, string text, Sprite icon, float x, float y, float width, float height,
            UiKit kit, int fontSize = 17)
        {
            var fill = CreateSliced(name, parent, kit.RoundFill, 12f, 24f, kit.Accent);
            PlaceTopLeft(fill.rectTransform, x, y, width, height);
            fill.raycastTarget = true;
            var button = fill.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            var label = CreateLabel("Label", fill.transform, kit.Bold, text, fontSize, Color.white, TextAnchor.MiddleCenter);
            if (icon == null)
            {
                Stretch(label.rectTransform);
                return (button, label);
            }
            float textWidth = label.preferredWidth, total = textWidth + 12f + 22f;
            Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-total / 2f + textWidth / 2f, 0f), new Vector2(textWidth + 4f, 30f));
            CreateImage("Icon", fill.transform, icon, new Vector2(22f, 22f), new Vector2(total / 2f - 11f, 0f), Color.white);
            return (button, label);
        }

        // ---------------------------------------------------------------- Oynalar

        /// <summary>Ekran o'rtasidagi oyna uchun asos: qorong'i fon (orqani bosib bo'lmaydi) va panel.</summary>
        static (Image dim, CanvasGroup group, Image panel) CreateModalBase(string name, Transform root, Vector2 size, UiKit kit)
        {
            var dim = CreateFullscreen(name, root, new Color(0f, 0f, 0f, 0.72f));
            dim.raycastTarget = true;
            var group = dim.gameObject.AddComponent<CanvasGroup>();
            var panel = CreateSliced("Panel", dim.transform, kit.RoundFill, 16f, 24f, kit.Panel);
            Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var border = CreateSliced("Border", panel.transform, kit.RoundStroke, 16f, 24f, kit.Line);
            Stretch(border.rectTransform);
            return (dim, group, panel);
        }

        static ConfirmDialog BuildConfirmDialog(Transform root, UiKit kit)
        {
            var (dim, group, panel) = CreateModalBase("ConfirmDialog", root, new Vector2(520f, 252f), kit);
            var title = CreateLabel("Title", panel.transform, kit.Display, "Quit game?", 22, Color.white, TextAnchor.MiddleLeft);
            PlaceTopLeft(title.rectTransform, 36f, 32f, 448f, 32f);
            var message = CreateLabel("Message", panel.transform, kit.Medium, "", 16, UiMuted, TextAnchor.UpperLeft);
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            message.lineSpacing = 1.1f;
            PlaceTopLeft(message.rectTransform, 36f, 80f, 448f, 70f);
            var cancel = CreateSecondaryButton("Cancel", panel.transform, "Cancel", null, 36f, 168f, 214f, kit.RoundFill, kit.RoundStroke, kit.SemiBold, kit.Field, kit.Line);
            ((RectTransform)cancel.transform).sizeDelta = new Vector2(214f, 48f);
            var (confirm, confirmLabel) = CreateAccentButton("Confirm", panel.transform, "Quit", null, 270f, 168f, 214f, 48f, kit, 16);

            var dialog = dim.gameObject.AddComponent<ConfirmDialog>();
            Set(dialog, "group", group);
            Set(dialog, "panel", panel.rectTransform);
            Set(dialog, "titleText", title);
            Set(dialog, "messageText", message);
            Set(dialog, "confirmButton", confirm);
            Set(dialog, "confirmLabel", confirmLabel);
            Set(dialog, "cancelButton", cancel);
            Set(dialog, "cancelLabel", cancel.transform.Find("Label").GetComponent<Text>());
            return dialog;
        }

        static SettingsPanel BuildSettingsPanel(Transform root, UiKit kit)
        {
            string[] names = { "Display mode", "Window size", "Graphics quality", "V-Sync", "Volume" };
            const float rowHeight = 60f, rowGap = 10f, top = 96f, width = 560f;
            float doneY = top + names.Length * (rowHeight + rowGap) + 14f;
            var (dim, group, panel) = CreateModalBase("Settings", root, new Vector2(width + 72f, doneY + 52f + 36f), kit);
            PlaceTopLeft(CreateLabel("Title", panel.transform, kit.Display, "Settings", 24, Color.white, TextAnchor.MiddleLeft).rectTransform, 36f, 34f, 400f, 36f);

            var previous = new Button[names.Length];
            var next = new Button[names.Length];
            var values = new Text[names.Length];
            var rows = new CanvasGroup[names.Length];
            var arrow = kit.Icon("Arrow");
            for (int i = 0; i < names.Length; i++)
            {
                var row = CreateSliced("Row" + i, panel.transform, kit.RoundFill, 12f, 24f, kit.Field);
                PlaceTopLeft(row.rectTransform, 36f, top + i * (rowHeight + rowGap), width, rowHeight);
                rows[i] = row.gameObject.AddComponent<CanvasGroup>();
                var label = CreateLabel("Label", row.transform, kit.SemiBold, names[i], 16, Color.white, TextAnchor.MiddleLeft);
                Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(240f, 24f));

                next[i] = CreateArrowButton("Next", row.transform, arrow, 0f, width - 12f - 40f, kit);
                values[i] = CreateLabel("Value", row.transform, kit.SemiBold, "", 16, Color.white, TextAnchor.MiddleCenter);
                Place(values[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(width - 12f - 40f - 180f, 0f), new Vector2(180f, 24f));
                previous[i] = CreateArrowButton("Previous", row.transform, arrow, 180f, width - 12f - 40f - 180f - 40f, kit);
            }
            var (done, _) = CreateAccentButton("Done", panel.transform, "Done", null, 36f, doneY, width, 52f, kit, 16);

            var settings = dim.gameObject.AddComponent<SettingsPanel>();
            Set(settings, "group", group);
            Set(settings, "panel", panel.rectTransform);
            SetArray(settings, "previousButtons", previous);
            SetArray(settings, "nextButtons", next);
            SetArray(settings, "values", values);
            SetArray(settings, "rows", rows);
            Set(settings, "closeButton", done);
            return settings;
        }

        static Button CreateArrowButton(string name, Transform parent, Sprite arrow, float angle, float x, UiKit kit)
        {
            var fill = CreateSliced(name, parent, kit.RoundFill, 10f, 24f, new Color32(32, 33, 38, 255));
            Place(fill.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(40f, 40f));
            fill.raycastTarget = true;
            var icon = CreateImage("Icon", fill.transform, arrow, new Vector2(18f, 18f), Vector2.zero, Color.white);
            icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            var button = fill.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.5f, 1.5f, 1.6f, 1f);
            colors.pressedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            return button;
        }

        // ---------------------------------------------------------------- Ovozlar

        static void AddUiSounds()
        {
            var go = new GameObject("UiSounds", typeof(AudioSource));
            var sounds = go.AddComponent<UiSounds>();
            Set(sounds, "hover", SoundClip("UI_Hover", 0.035f, t =>
                Mathf.Sin(2f * Mathf.PI * 2400f * t) * Mathf.Exp(-t * 150f) * 0.16f));
            Set(sounds, "click", SoundClip("UI_Click", 0.08f, t =>
            {
                // Pastga tushuvchi qisqa "tik" va yengil shovqin: yumshoq, zamonaviy bosish ovozi
                float phase = 2f * Mathf.PI * (700f * t + 900f * (1f - Mathf.Exp(-t * 60f)) / 60f);
                float tone = Mathf.Sin(phase) * Mathf.Exp(-t * 55f);
                float noise = (Mathf.PerlinNoise(t * 9000f, 0.3f) * 2f - 1f) * Mathf.Exp(-t * 400f) * 0.3f;
                float attack = Mathf.Clamp01(t / 0.002f);
                return (tone + noise) * attack * 0.42f;
            }));
        }

        /// <summary>16-bit mono WAV (44.1 kHz). Fayl bor bo'lsa qayta yaratilmaydi.</summary>
        static AudioClip SoundClip(string name, float duration, System.Func<float, float> wave)
        {
            string path = UiAudio + name + ".wav";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(UiAudio);
                const int rate = 44100;
                int samples = Mathf.CeilToInt(duration * rate);
                using (var stream = new FileStream(path, FileMode.Create))
                using (var w = new BinaryWriter(stream))
                {
                    w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                    w.Write(36 + samples * 2);
                    w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                    w.Write(16); w.Write((short)1); w.Write((short)1);
                    w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
                    w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                    w.Write(samples * 2);
                    for (int i = 0; i < samples; i++)
                    {
                        float t = (float)i / rate;
                        // Oxirida qisqa so'nish: "chirt" etmasin
                        float fade = Mathf.Clamp01((duration - t) / 0.004f);
                        w.Write((short)Mathf.Clamp(wave(t) * fade * short.MaxValue, short.MinValue, short.MaxValue));
                    }
                }
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // ---------------------------------------------------------------- Ikonka

        /// <summary>O'yin ikonkasi (.exe, vazifalar paneli): intro'dagi CraDev belgisi - ko'k plitka va oq "C".</summary>
        static void ApplyGameIcon()
        {
            string path = UiArt + "App_Icon.png";
            if (!File.Exists(path))
            {
                var tile = new Texture2D(2, 2);
                tile.LoadImage(File.ReadAllBytes(IntroArt + "CraDev_Tile.png"));
                var glyph = new Texture2D(2, 2);
                glyph.LoadImage(File.ReadAllBytes(IntroArt + "CraDev_Glyph.png"));

                const int size = 1024;
                float scale = size / (float)Mathf.Max(tile.width, tile.height);
                var icon = new Texture2D(size, size, TextureFormat.RGBA32, false);
                // Belgi plitka markazidan 8 birlik (2x rasmda 16 px) chapda (Intro sahnasidagi joylashuv)
                Vector2 glyphSize = new Vector2(glyph.width, glyph.height) * scale;
                Vector2 glyphMin = new Vector2(size, size) / 2f - glyphSize / 2f + new Vector2(-16f * scale, 0f);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        Color c = tile.GetPixelBilinear((x + 0.5f) / size, (y + 0.5f) / size);
                        float gu = (x + 0.5f - glyphMin.x) / glyphSize.x, gv = (y + 0.5f - glyphMin.y) / glyphSize.y;
                        if (gu >= 0f && gu <= 1f && gv >= 0f && gv <= 1f)
                        {
                            Color g = glyph.GetPixelBilinear(gu, gv);
                            c = new Color(Mathf.Lerp(c.r, g.r, g.a), Mathf.Lerp(c.g, g.g, g.a), Mathf.Lerp(c.b, g.b, g.a), Mathf.Max(c.a, g.a));
                        }
                        icon.SetPixel(x, y, c);
                    }
                File.WriteAllBytes(path, icon.EncodeToPNG());
                Object.DestroyImmediate(tile);
                Object.DestroyImmediate(glyph);
                Object.DestroyImmediate(icon);
                AssetDatabase.ImportAsset(path);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { texture }, IconKind.Any);
        }
    }
}

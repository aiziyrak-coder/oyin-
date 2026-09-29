using CraDev.MainMenu;
using UnityEngine;
using UnityEngine.UI;
using static CraDev.EditorTools.UiBuild;

namespace CraDev.EditorTools
{
    /// <summary>
    /// Lobby ichidagi avatar studiyasi (LobbyAvatarStudio): o'ngda tekis panel (qahramon kartalari, yuz bo'limi,
    /// Bekor qilish / Saqlash), panel ichida jonli yuz skaneri sahifasi, qahramon ostida "Butun bo'y / Yuz"
    /// tugmalari va fon rasmini qoraytiruvchi qatlam. O'lchamlar lobbyning 65% ixcham canvas'iga mos.
    /// </summary>
    static partial class CraDevSceneBuilder
    {
        const float StudioWidth = 700f;
        static readonly Color StudioMuted = new Color32(178, 174, 169, 255);

        static void V2AvatarStudio(Transform root, MainMenuScreen screen)
        {
            var host = CreateRect("AvatarStudio", root);
            Stretch(host);
            var studio = host.gameObject.AddComponent<LobbyAvatarStudio>();
            Set(studio, "lobby", screen);
            Set(screen, "avatarStudio", studio);

            // Fon rasmi ustidagi qoraytirish: fon kamerasi canvas'ida, shuning uchun 3D qahramon undan oldinda qoladi
            var background = GameObject.Find("BackgroundCanvas");
            if (background != null)
            {
                var backdrop = CreateFullscreen("StudioBackdrop", background.transform, new Color(0.035f, 0.04f, 0.05f, 0f));
                backdrop.transform.SetAsLastSibling();
                backdrop.enabled = false;
                Set(studio, "backdrop", backdrop);
            }

            // ---------- O'ng panel ----------
            var panel = V2Panel(host, "StudioPanel", 0, 0, StudioWidth, 1000, V2Glass);
            panel.raycastTarget = true; // panel ostidagi qahramonni aylantirish qatlami bosilmaydi
            var rect = panel.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-48f, 0f);
            rect.sizeDelta = new Vector2(StudioWidth, -72f);
            ReferenceSurface.Apply(panel, 0, 27);
            Set(studio, "panel", rect);
            Set(studio, "panelGroup", panel.gameObject.AddComponent<CanvasGroup>());

            var editor = CreateRect("EditorPage", panel.transform);
            Stretch(editor);
            Set(studio, "editorPage", editor.gameObject);
            const float pad = 36f, inner = StudioWidth - 2 * pad;

            var title = V2Text(editor, "studio.title", pad, 26, inner - 70, 52, 38);
            title.font = v2.SemiBold;
            V2Text(editor, "studio.subtitle", pad, 80, inner - 40, 34, 22).color = StudioMuted;
            Set(studio, "closeButton", StudioButton(editor, "StudioClose", null, LineIcon("Close"), StudioWidth - pad - 56, 28, 56, 52, 5));

            // Qahramon: pasportdagi jinsga mos kartalar (skript to'ldiradi, ortiqchasi yashiriladi)
            SectionLabel(editor, "studio.model", 142);
            const int cardCount = 5;
            float cardGap = 12f, cardWidth = (inner - cardGap * (cardCount - 1)) / cardCount, cardHeight = 200f;
            var cards = new Object[cardCount];
            var pictures = new Object[cardCount];
            var ticks = new Object[cardCount];
            for (int i = 0; i < cardCount; i++)
            {
                var card = StudioButton(editor, "StudioAvatar_" + i, null, null, pad + i * (cardWidth + cardGap), 180, cardWidth, cardHeight, 5);
                var picture = CreateImage("Picture", card.transform, null, Vector2.zero, Vector2.zero, Color.white);
                PlaceTopLeft(picture.rectTransform, 6, 8, cardWidth - 12, cardHeight - 16);
                picture.preserveAspect = true;
                var tick = V2Panel(card.transform, "Tick", cardWidth - 36, 8, 28, 28, LobbyPalette.Accent);
                V2Icon(tick.transform, "Check", 5, 5, 18);
                tick.gameObject.SetActive(false);
                cards[i] = card;
                pictures[i] = picture;
                ticks[i] = tick.gameObject;
            }
            SetArray(studio, "cards", cards);
            SetArray(studio, "cardPictures", pictures);
            SetArray(studio, "cardTicks", ticks);
            var info = V2Text(editor, "", pad, 388, inner, 32, 22, false);
            info.color = StudioMuted;
            Set(studio, "avatarInfo", info);

            // Yuz: rasmcha, holat, maxfiylik izohi, jonli skaner / yuklash / olib tashlash
            SectionLabel(editor, "studio.face", 440);
            var thumb = V2Panel(editor, "FaceThumb", pad, 478, 128, 128, V2Glass);
            ReferenceSurface.Apply(thumb, 5, 14);
            var placeholder = V2Icon(thumb.transform, "User", 40, 40, 48);
            placeholder.color = StudioMuted;
            Set(studio, "faceIcon", placeholder);
            Set(studio, "faceThumb", V2Picture(thumb.transform, "FacePhoto", null, 4, 4, 120, 120));
            var status = V2Text(editor, "", 184, 480, inner - 148, 64, 22, false);
            status.alignment = TextAnchor.UpperLeft;
            Set(studio, "faceStatus", status);
            var privacy = V2Text(editor, "studio.privacy", 184, 548, inner - 148, 58, 18);
            privacy.alignment = TextAnchor.UpperLeft;
            privacy.color = StudioMuted;
            Set(studio, "scanButton", StudioButton(editor, "StudioScan", "studio.scan", LineIcon("Camera"), pad, 634, inner, 62, 4));
            float half = (inner - 12f) / 2f;
            Set(studio, "uploadButton", StudioButton(editor, "StudioUpload", "face.upload", LineIcon("Upload"), pad, 708, half, 58, 5));
            Set(studio, "removeButton", StudioButton(editor, "StudioRemoveFace", "studio.remove", LineIcon("Close"), pad + half + 12f, 708, half, 58, 5));

            // Pastki qism (panel pastiga yopishgan): xato matni, Bekor qilish / Saqlash
            var error = V2Text(editor, "", pad, 0, inner, 60, 20, false);
            error.alignment = TextAnchor.LowerLeft;
            Bottom(error.rectTransform, pad, 124);
            Set(studio, "errorText", error);
            var cancel = StudioButton(editor, "StudioCancel", "common.cancel", null, pad, 0, half, 66, 5);
            Bottom((RectTransform)cancel.transform, pad, 40);
            Set(studio, "cancelButton", cancel);
            var save = StudioButton(editor, "StudioSave", null, LineIcon("Check"), pad + half + 12f, 0, half, 66, 2);
            Bottom((RectTransform)save.transform, pad + half + 12f, 40);
            Set(studio, "saveButton", save);
            Set(studio, "saveLabel", save.transform.Find("Label").GetComponent<Text>());

            // ---------- Jonli yuz skaneri: panel ichidagi alohida sahifa ----------
            var scanPage = CreateRect("ScanPage", panel.transform);
            Stretch(scanPage);
            var style = new ScanStyle
            {
                Title = v2.SemiBold, Body = v2.Medium, Strong = v2.Bold, S = 1.45f, Round = v2.RoundFill,
                Text = new Color32(229, 235, 242, 255), Muted = StudioMuted,
                Oval = OvalSprite(), Spinner = LoadSprite(UiArt + "Icon_Spinner.png"), Alert = LoadSprite(UiArt + "Icon_Alert.png"),
                Close = LineIcon("Close"), Camera = LineIcon("Camera"),
                Button = (parent, name, icon, x, y, w, h, primary) => StudioButton(parent, name, null, icon, x, y, w, h, primary ? 2 : 5),
            };
            var scanner = BuildFaceScanView(host.gameObject, scanPage, scanPage.gameObject, StudioWidth, style, out _);
            Set(studio, "scanner", scanner);

            // ---------- Qahramon ostida: Butun bo'y / Yuz va boshqaruv izohi ----------
            var dock = CreateRect("StudioViewDock", host);
            dock.anchorMin = dock.anchorMax = new Vector2(0.38f, 0f);
            dock.pivot = new Vector2(0.5f, 0f);
            dock.anchoredPosition = new Vector2(0f, 44f);
            dock.sizeDelta = new Vector2(440f, 108f);
            Set(studio, "viewDock", dock);
            Set(studio, "viewGroup", dock.gameObject.AddComponent<CanvasGroup>());
            SetArray(studio, "viewButtons", new Object[]
            {
                StudioButton(dock, "StudioViewBody", "studio.view_body", null, 0, 0, 212, 56, 4),
                StudioButton(dock, "StudioViewFace", "studio.view_face", null, 228, 0, 212, 56, 5),
            });
            var drag = V2Text(dock, "create.drag_hint", 0, 66, 440, 36, 20);
            drag.alignment = TextAnchor.MiddleCenter;
            drag.horizontalOverflow = HorizontalWrapMode.Overflow;
            drag.color = new Color32(214, 210, 204, 255);
            var shadow = drag.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }

        static void SectionLabel(Transform parent, string key, float y)
        {
            var label = V2Text(parent, key, 36, y, 400, 30, 20);
            label.font = v2.SemiBold;
            label.color = StudioMuted;
        }

        /// <summary>Panel pastidan o'lchanadigan joy (pastki chap burchak).</summary>
        static void Bottom(RectTransform rect, float x, float y)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>
        /// Studiya tugmasi: lobby uslubidagi yuza (ReferenceSurface: 2 - asosiy yashil, 4 - tanlangan, 5 - oddiy),
        /// ixtiyoriy ikonka va "Label" matni (key null bo'lsa matnni skript yozadi).
        /// </summary>
        static Button StudioButton(Transform parent, string name, string key, Sprite icon, float x, float y, float w, float h, int style)
        {
            var fill = V2Panel(parent, name, x, y, w, h, V2Glass);
            fill.raycastTarget = true;
            var button = fill.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.85f, 0.88f, 0.92f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.7f, 0.7f, 0.72f, 0.45f);
            colors.fadeDuration = 0.14f;
            button.colors = colors;
            bool iconOnly = icon != null && key == null && w <= h * 1.5f;
            if (icon != null)
            {
                var image = CreateImage("Icon", fill.transform, icon, Vector2.zero, Vector2.zero, Color.white);
                if (iconOnly)
                    Place(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f));
                else
                    PlaceTopLeft(image.rectTransform, 20, (h - 26) / 2, 26, 26);
            }
            var label = CreateLabel("Label", fill.transform, v2.Medium, "", 21, new Color32(229, 235, 242, 255),
                icon != null && !iconOnly ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
            if (icon != null && !iconOnly)
                PlaceTopLeft(label.rectTransform, 58, 0, w - 72, h);
            else
                PlaceTopLeft(label.rectTransform, 12, 0, w - 24, h);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            if (key != null)
                Localized(label, key);
            ReferenceSurface.Apply(fill, style, 14);
            return button;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    // Sozlamalar sahifasining boshqaruv elementlari (ish vaqtida chiziladi): slayder, ‹ › tanlagich, segmentlar,
    // switch, faqat o'qiladigan qatorlar va tugmalar ro'yxati. Qiymat o'zgarganda sahifa qayta qurilmaydi:
    // har element o'z yangilash funksiyasini settingsRefresh ga yozadi, RefreshSettings() hammasini yangilaydi.
    public partial class LobbyContent
    {
        [SerializeField] Sprite chevronLeft, chevronRight, pencil, resetIcon;

        /// <summary>Sozlamalar oynasi ranglari (builder ham shularni ishlatadi): lobbining ko'mir rangidagi tekis panellar.</summary>
        public static readonly Color SettingsSheet = new Color32(26, 25, 27, 252);
        public static readonly Color SettingsSidebar = new Color32(34, 33, 35, 245);
        static readonly Color RowFill = new Color32(44, 43, 46, 240);
        static readonly Color ControlFill = new Color32(64, 63, 67, 255);
        static readonly Color SwitchOff = new Color32(82, 81, 86, 255);
        static readonly Color Soft = new Color32(178, 174, 169, 255);
        static readonly Color Bright = new Color32(236, 233, 229, 255);

        readonly List<Action> settingsRefresh = new List<Action>();
        float settingsY;
        float RowWidth => width - 8;
        float ControlWidth => Mathf.Min(600f, RowWidth * .47f);
        float ControlX => RowWidth - 24 - ControlWidth;

        sealed class SettingRow { public RectTransform rect; public Text label, hint; public float height; }

        /// <summary>Hamma elementlar qiymatini joyida yangilaydi (sahifa qayta qurilmaydi, aylantirish joyi saqlanadi).</summary>
        void RefreshSettings()
        {
            if (this == null) return;
            foreach (var refresh in settingsRefresh.ToArray()) refresh();
        }

        void SectionTitle(string text)
        {
            var title = Text(text, 4, settingsY, RowWidth - 8, 56, 36); title.font = bold; title.name = "SectionTitle";
            settingsY += 68;
        }

        void Subheading(string text)
        {
            settingsY += 8;
            var title = Text(text, 4, settingsY, RowWidth - 8, 44, 29); title.font = bold; title.name = "Subheading";
            settingsY += 52;
        }

        Text Paragraph(string text, Color color, int size = 25)
        {
            var label = Text(text, 4, settingsY, RowWidth - 8, 40, size);
            label.color = color; label.alignment = TextAnchor.UpperLeft; label.verticalOverflow = VerticalWrapMode.Overflow;
            float h = Mathf.Max(30, Mathf.Ceil(label.preferredHeight) + 4);
            label.rectTransform.sizeDelta = new Vector2(RowWidth - 8, h);
            settingsY += h + 16;
            return label;
        }

        // Qator asosi: chapda nomi (va ixtiyoriy izoh), o'ngda boshqaruv ustuni. button=true: butun qator bosiladi.
        SettingRow SettingPanel(string name, string label, string hint, bool button, Action click = null)
        {
            float h = hint == null ? 68 : 88;
            Image panel;
            if (button)
            {
                var b = Button("", 0, settingsY, RowWidth, h, click);
                b.name = name; panel = b.GetComponent<Image>(); panel.color = RowFill;
            }
            else panel = Panel(name, 0, settingsY, RowWidth, h, RowFill);
            var row = new SettingRow { rect = panel.rectTransform, height = h };
            float labelWidth = ControlX - 40;
            row.label = Text(label, 24, hint == null ? 0 : 12, labelWidth, hint == null ? h : 36, 27, panel.transform);
            row.label.name = "RowLabel"; row.label.color = Bright;
            if (hint != null)
            {
                row.hint = Text(hint, 24, 48, labelWidth, 30, 21, panel.transform);
                row.hint.name = "RowHint"; row.hint.color = Soft;
            }
            settingsY += h + 10;
            return row;
        }

        static void SetDimmed(Text text, bool dimmed)
        {
            if (text == null) return;
            var c = text.color; c.a = dimmed ? .42f : 1f; text.color = c;
        }

        /// <summary>Faqat o'qiladigan qator (tugma emas): bosilmaydi, sichqoncha ostida yonmaydi.</summary>
        Text InfoRow(string name, string label, string value, string hint = null)
        {
            var row = SettingPanel("Info_" + name, label, hint, false);
            var text = Text(value, ControlX, 0, ControlWidth, row.height, 27, row.rect);
            text.alignment = TextAnchor.MiddleRight; text.color = Soft; text.name = name + "_Value";
            return text;
        }

        /// <summary>Boshqa ko'rinishga o'tadigan qator: qiymat va o'ng tomonda strelka.</summary>
        Button NavRow(string name, string label, string value, Action click)
        {
            var row = SettingPanel("Nav_" + name, label, null, true, click);
            var text = Text(value, ControlX, 0, ControlWidth - 44, row.height, 27, row.rect);
            text.alignment = TextAnchor.MiddleRight; text.color = Soft; text.name = name + "_Value";
            Glyph(row.rect, chevronRight, "›", RowWidth - 24 - 26, (row.height - 26) / 2, 26, Soft);
            return row.rect.GetComponent<Button>();
        }

        /// <summary>Qator o'ngida asosiy (ko'k) tugma: masalan, "Avatar va yuz" → Tahrirlash.</summary>
        Button ActionRow(string name, string label, string hint, string buttonText, Action click, string value = null)
        {
            var row = SettingPanel("Action_" + name, label, hint, false);
            float w = 250, h = 52;
            var button = Button(buttonText, RowWidth - 24 - w, (row.height - h) / 2, w, h, click, true, row.rect);
            button.name = name;
            if (pencil != null)
            {
                Glyph(button.transform, pencil, null, 18, (h - 24) / 2, 24, Color.white);
                var text = button.GetComponentInChildren<Text>();
                text.rectTransform.anchoredPosition = new Vector2(52, 0); text.rectTransform.sizeDelta = new Vector2(w - 64, h);
            }
            if (!string.IsNullOrEmpty(value))
            {
                var text = Text(value, ControlX, 0, ControlWidth - w - 24, row.height, 26, row.rect);
                text.alignment = TextAnchor.MiddleRight; text.color = Soft; text.name = name + "_Value";
            }
            return button;
        }

        /// <summary>Switch qatori: butun qator bosiladi, tugmacha joyida silliq suriladi (sahifa qayta qurilmaydi).</summary>
        SwitchToggle SwitchRow(string name, string label, string hint, Func<bool> read, Action<bool> changed)
        {
            var row = SettingPanel("Switch_" + name, label, hint, true);
            var track = Panel("Switch", RowWidth - 24 - 64, (row.height - 34) / 2, 64, 34, SwitchOff, row.rect);
            track.pixelsPerUnitMultiplier = 1.4f;
            var knob = Panel("SwitchKnob", 0, 0, 26, 26, new Color(.95f, .95f, .96f), track.transform);
            knob.pixelsPerUnitMultiplier = 1.8f;
            var knobRect = knob.rectTransform;
            knobRect.anchorMin = knobRect.anchorMax = knobRect.pivot = new Vector2(.5f, .5f);
            var toggle = row.rect.gameObject.AddComponent<SwitchToggle>();
            toggle.Init(track, knobRect, LobbyPalette.Accent, SwitchOff, 15, read());
            toggle.Changed += value => changed(value);
            settingsRefresh.Add(() => { if (toggle != null && toggle.Value != read()) toggle.Value = read(); });
            return toggle;
        }

        /// <summary>
        /// Gorizontal slayder: sudrash, bosish va strelka tugmalari bilan; o'ngda qiymat. step - yaxlitlash qadami.
        /// </summary>
        Slider SliderRow(string name, string label, string hint, float min, float max, float step, Func<float> read,
            Func<float, string> format, Action<float> changed)
        {
            var row = SettingPanel("Slider_" + name, label, hint, false);
            float valueWidth = 100;
            var valueText = Text(format(read()), RowWidth - 24 - valueWidth, 0, valueWidth, row.height, 27, row.rect);
            valueText.alignment = TextAnchor.MiddleRight; valueText.color = Bright; valueText.name = name + "_Value";
            var area = Rect(name, row.rect, ControlX, (row.height - 40) / 2, ControlWidth - valueWidth - 20, 40);
            var hit = area.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
            var track = Panel("Track", 0, 0, 0, 0, SwitchOff, area); Band(track.rectTransform, 6, 12);
            track.pixelsPerUnitMultiplier = 8;
            var fillArea = Rect("FillArea", area, 0, 0, 0, 0); Band(fillArea, 6, 12);
            var fill = Panel("Fill", 0, 0, 0, 0, LobbyPalette.Accent, fillArea); fill.pixelsPerUnitMultiplier = 8;
            var fillRect = fill.rectTransform; fillRect.pivot = new Vector2(0, .5f);
            fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one; fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            var handleArea = Rect("HandleArea", area, 0, 0, 0, 0); Band(handleArea, 24, 12);
            var handle = Panel("Handle", 0, 0, 0, 0, Bright, handleArea); handle.raycastTarget = true; handle.pixelsPerUnitMultiplier = 2;
            var handleRect = handle.rectTransform; handleRect.pivot = new Vector2(.5f, .5f);
            handleRect.anchoredPosition = Vector2.zero; handleRect.sizeDelta = new Vector2(24, 0);
            var slider = area.gameObject.AddComponent<Slider>();
            slider.fillRect = fillRect; slider.handleRect = handleRect; slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min; slider.maxValue = max; slider.wholeNumbers = step >= 1 && Mathf.Approximately(step, Mathf.Round(step));
            var colors = slider.colors; colors.highlightedColor = new Color(1, 1, 1); colors.normalColor = new Color(.9f, .9f, .9f);
            colors.pressedColor = new Color(.8f, .8f, .8f); colors.selectedColor = Color.white; colors.fadeDuration = .1f; slider.colors = colors;
            slider.SetValueWithoutNotify(read());
            slider.onValueChanged.AddListener(raw =>
            {
                float value = Mathf.Clamp(step > 0 ? min + Mathf.Round((raw - min) / step) * step : raw, min, max);
                if (Mathf.Abs(slider.value - value) > 1e-4f) slider.SetValueWithoutNotify(value);
                valueText.text = format(value);
                changed(value);
            });
            UiSounds.Hook(slider);
            settingsRefresh.Add(() =>
            {
                if (slider == null) return;
                slider.SetValueWithoutNotify(read());
                valueText.text = format(read());
            });
            return slider;
        }

        /// <summary>‹ qiymat › tanlagich: ikki tomonga, oxirida aylanmaydi (chekka strelka o'chadi).</summary>
        SettingRow StepperRow(string name, string label, string hint, Func<string> text, Func<bool> canPrev, Func<bool> canNext,
            Action prev, Action next, Func<bool> enabled = null)
        {
            var row = SettingPanel("Stepper_" + name, label, hint, false);
            float size = 48, y = (row.height - size) / 2;
            var back = ArrowButton(name + "_Prev", chevronLeft, "‹", ControlX, y, size, row.rect, prev);
            var forward = ArrowButton(name + "_Next", chevronRight, "›", RowWidth - 24 - size, y, size, row.rect, next);
            var value = Text("", ControlX + size + 8, 0, ControlWidth - 2 * size - 16, row.height, 27, row.rect);
            value.alignment = TextAnchor.MiddleCenter; value.color = Bright; value.name = name + "_Value";
            Action refresh = () =>
            {
                if (value == null) return;
                bool on = enabled == null || enabled();
                value.text = text();
                SetArrow(back, on && canPrev());
                SetArrow(forward, on && canNext());
                SetDimmed(value, !on); SetDimmed(row.label, !on);
            };
            settingsRefresh.Add(refresh); refresh();
            return row;
        }

        Button ArrowButton(string name, Sprite icon, string glyph, float x, float y, float size, Transform parent, Action click)
        {
            var button = Button("", x, y, size, size, () => { click(); RefreshSettings(); }, false, parent);
            button.name = name; button.GetComponent<Image>().color = ControlFill;
            var colors = button.colors; colors.disabledColor = Color.white; button.colors = colors;
            button.gameObject.AddComponent<CanvasGroup>();
            Glyph(button.transform, icon, glyph, (size - 24) / 2, (size - 24) / 2, 24, Bright);
            return button;
        }

        static void SetArrow(Button button, bool on)
        {
            if (button == null) return;
            button.interactable = on;
            button.GetComponent<CanvasGroup>().alpha = on ? 1f : .3f;
        }

        /// <summary>Segmentli tanlov: hamma variant ko'rinadi, tanlangani ko'k. Tugmalar nomi: name_0, name_1, ...</summary>
        Button[] SegmentedRow(string name, string label, string hint, string[] options, Func<int> current, Action<int> choose, float width = 0)
        {
            var row = SettingPanel("Segmented_" + name, label, hint, false);
            float cw = width > 0 ? Mathf.Min(width, RowWidth - 300) : ControlWidth;
            float gap = 6, h = 48, w = (cw - gap * (options.Length - 1)) / options.Length, x0 = RowWidth - 24 - cw;
            var buttons = new Button[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                var button = Button(options[i], x0 + i * (w + gap), (row.height - h) / 2, w, h, () =>
                {
                    if (current() != index) choose(index);
                    RefreshSettings();
                }, false, row.rect);
                button.name = name + "_" + i;
                var text = button.GetComponentInChildren<Text>();
                text.alignment = TextAnchor.MiddleCenter; text.rectTransform.anchoredPosition = new Vector2(6, 0);
                text.rectTransform.sizeDelta = new Vector2(w - 12, h);
                text.resizeTextForBestFit = true; text.resizeTextMinSize = 14; text.resizeTextMaxSize = 20;
                buttons[i] = button;
            }
            settingsRefresh.Add(() =>
            {
                int selected = current();
                for (int i = 0; i < buttons.Length; i++)
                    if (buttons[i] != null) buttons[i].GetComponent<Image>().color = i == selected ? LobbyPalette.Accent : ControlFill;
            });
            settingsRefresh[settingsRefresh.Count - 1]();
            return buttons;
        }

        /// <summary>Tugmalar ro'yxati qatori (faqat o'qiladi): o'ngda klaviatura tugmachalari, guruhlar orasida "yoki".</summary>
        void KeyRow(string name, string label, params string[][] groups)
        {
            var row = SettingPanel("Keys_" + name, label, null, false);
            float x = RowWidth - 24, h = 42, y = (row.height - h) / 2;
            for (int g = groups.Length - 1; g >= 0; g--)
            {
                for (int k = groups[g].Length - 1; k >= 0; k--)
                {
                    string cap = groups[g][k];
                    float w = Mathf.Max(46, cap.Length * 12 + 28);
                    x -= w;
                    var key = Panel("Key", x, y, w, h, ControlFill, row.rect);
                    var text = Text(cap, 0, 0, w, h, 23, key.transform);
                    text.alignment = TextAnchor.MiddleCenter; text.color = Bright; text.rectTransform.anchoredPosition = Vector2.zero;
                    x -= 6;
                }
                if (g > 0)
                {
                    float w = 56; x -= w;
                    var or = Text(Loc.T("settings.key.or"), x, 0, w, row.height, 22, row.rect);
                    or.alignment = TextAnchor.MiddleCenter; or.color = Soft;
                    x -= 6;
                }
            }
        }

        /// <summary>Bo'lim oxiri: "Asliga qaytarish" (tasdiq bilan) va "avtomatik saqlanadi" izohi.</summary>
        void SettingsFooter(Action reset, string note = null)
        {
            settingsY += 6;
            float left = 4;
            if (reset != null)
            {
                var button = Button(Loc.T("settings.reset"), 0, settingsY, 290, 52, () => AskReset(reset));
                button.name = "SettingsReset";
                if (resetIcon != null)
                {
                    Glyph(button.transform, resetIcon, null, 16, 14, 24, Bright);
                    var text = button.GetComponentInChildren<Text>();
                    text.rectTransform.anchoredPosition = new Vector2(50, 0); text.rectTransform.sizeDelta = new Vector2(228, 52);
                }
                left = 310;
            }
            var label = Text(note ?? Loc.T("world.settings.saved"), left, settingsY, RowWidth - left - 4, 52, 22);
            label.color = Soft; label.alignment = reset != null ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft; label.name = "AutosaveNote";
            settingsY += 70;
        }

        void AskReset(Action reset)
        {
            string title = Loc.T("settings.section." + SettingsSections[Mathf.Clamp(section, 0, SettingsSections.Length - 1)]);
            Lobby.Dialog.Show(Loc.T("settings.reset_title"), Loc.F("settings.reset_message", title), Loc.T("settings.reset"), () =>
            {
                reset();
                RefreshSettings();
                Lobby.Toast(Loc.T("settings.reset_done"));
            }, Loc.T("common.cancel"));
        }

        // Ikonka (sprite bo'lsa) yoki matnli belgi (sahna hali qayta yaratilmagan bo'lsa).
        Graphic Glyph(Transform parent, Sprite icon, string fallback, float x, float y, float size, Color color)
        {
            if (icon != null)
            {
                var image = Rect("Icon", parent, x, y, size, size).gameObject.AddComponent<Image>();
                image.sprite = icon; image.preserveAspect = true; image.raycastTarget = false; image.color = color;
                return image;
            }
            if (fallback == null) return null;
            var text = Text(fallback, x - 8, y - 8, size + 16, size + 16, 40, parent);
            text.alignment = TextAnchor.MiddleCenter; text.color = color; text.name = "Icon";
            text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        // Ota element bo'ylab gorizontal cho'zilgan, balandligi height, chetlardan inset ichkarida.
        static void Band(RectTransform rect, float height, float inset)
        {
            rect.anchorMin = new Vector2(0, .5f); rect.anchorMax = new Vector2(1, .5f); rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = new Vector2(inset, -height / 2); rect.offsetMax = new Vector2(-inset, height / 2);
        }
    }
}

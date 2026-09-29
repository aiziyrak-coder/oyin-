using System;
using System.Collections.Generic;
using System.Globalization;
using CraDev.MainMenu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CraDev.World
{
    /// <summary>
    /// Dunyoning ixcham HUD'i va Esc menyusi: chapda olam boshqaruvi (<see cref="WorldPreferences"/>),
    /// o'ngda butun o'yinga tegishli grafika va ovoz (<see cref="GameSettings"/>).
    /// </summary>
    // HUD o'yinchidan oldin yangilanadi: Esc bosilgan kadrda menyuni birinchi bo'lib HUD ochadi.
    [DefaultExecutionOrder(-100)]
    public sealed class WorldHud : MonoBehaviour
    {
        [SerializeField] WorldPlayerController player;
        [SerializeField] Font font;
        [SerializeField] Font boldFont;
        [SerializeField] Sprite rounded;

        const float WindowWidth = 1180f, WindowHeight = 676f, Pad = 36f, ColumnWidth = 520f;
        const float RowHeight = 44f, SliderRowHeight = 76f, RowGap = 8f;
        static readonly Color TextColor = new Color(.93f, .95f, .97f);
        static readonly Color Muted = new Color(.62f, .67f, .74f);
        static readonly Color MapArrow = new Color(.38f, .83f, .65f);
        // Qator fonlari: oddiy qator 5% oq; bosiladigan qator RowColors bilan 5% -> 10% (hover/tanlangan).
        static readonly Color RowFill = new Color(1f, 1f, 1f, .05f);
        static readonly Color InteractiveFill = new Color(1f, 1f, 1f, .10f);
        static readonly Color SecondaryFill = new Color(1f, 1f, 1f, .16f);
        static readonly Color SwitchOff = new Color(.25f, .28f, .32f);
        static Color Accent => LobbyPalette.Accent;

        readonly List<(Text label, string key)> localized = new List<(Text, string)>();
        readonly List<(Toggle toggle, Image track, RectTransform knob)> switches = new List<(Toggle, Image, RectTransform)>();
        GameObject settings, reticle, hints;
        Text movementState, sensitivityValue, fovValue, volumeValue, displayValue, qualityValue;
        Slider sensitivitySlider, fovSlider, volumeSlider;
        Toggle invertToggle, bobToggle, crouchToggle, vsyncToggle, aoToggle;
        Button resumeButton;
        WorldMinimap minimap;
        bool settingsOpen, dirty, gameSettingsDirty, leaving;
        float saveAt;
        int menuChangedFrame = -1;
        string stateKey;

        public bool SettingsOpen => settingsOpen;
        public RenderTexture MapTexture => minimap ? minimap.Texture : null;
        public Camera MapCamera => minimap ? minimap.MapCamera : null;

        void Start()
        {
            if (!player) player = FindFirstObjectByType<WorldPlayerController>();
            if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            WorldPreferences.Load();
            BuildHud();
            BuildSettings();
            Loc.Changed += RefreshText;
            WorldPreferences.Changed += RefreshPreferences;
            if (player) player.PauseChanged += OnPauseChanged;
            RefreshText();
            SetSettings(player && player.Paused);
        }

        void Update()
        {
            if (leaving) return;
            if (MenuPressed()) ToggleMenuFromInput();
            if ((dirty || gameSettingsDirty) && Time.unscaledTime >= saveAt) SavePending();
            if (!player || !movementState) return;
            string next = !player.IsGrounded ? "world.hud.airborne" :
                player.IsCrouching ? "world.hud.crouching" :
                player.IsSprinting && player.Speed > .15f ? "world.hud.running" :
                player.Speed > .15f ? "world.hud.walking" : "world.hud.standing";
            if (next != stateKey)
            {
                stateKey = next;
                movementState.text = Loc.T(next);
            }
        }

        /// <summary>
        /// Esc / geympad Start: menyuni ochadi yoki yopadi. Shu kadrda pauza allaqachon almashgan bo'lsa
        /// (masalan, Editor Esc'da kursorni qo'yib yuborib o'yinchi o'zi pauza qilgan) ikkinchi marta almashtirmaydi.
        /// </summary>
        public bool ToggleMenuFromInput()
        {
            if (leaving || Time.frameCount == menuChangedFrame) return false;
            SetSettings(!settingsOpen);
            return true;
        }

        public void SetSettings(bool open)
        {
            if (leaving) return;
            if (player && player.Paused != open) player.SetPaused(open);
            ApplySettingsVisibility(open);
        }

        void OnPauseChanged(bool paused) => ApplySettingsVisibility(paused);

        void ApplySettingsVisibility(bool open)
        {
            if (settingsOpen != open) menuChangedFrame = Time.frameCount;
            settingsOpen = open;
            if (!settings) return;
            settings.SetActive(open);
            if (reticle) reticle.SetActive(!open);
            if (hints) hints.SetActive(!open);
            if (open)
            {
                RefreshPreferences();
                if (EventSystem.current && resumeButton)
                    EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
            }
            else
            {
                SavePending();
                if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
            }
        }

        void BuildHud()
        {
            var map = Panel("WorldMinimapFrame", transform, new Color(.045f, .065f, .055f, .92f));
            TopLeft(map, 24f, 24f, 206f, 206f);
            var viewport = Rect("MapViewport", map);
            TopLeft(viewport, 8f, 8f, 190f, 190f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var raw = Rect("MapView", viewport).gameObject.AddComponent<RawImage>();
            Stretch(raw.rectTransform);
            raw.color = Color.white;
            raw.raycastTarget = false;
            var grid = Panel("MapGridHorizontal", viewport, new Color(1f, 1f, 1f, .12f));
            Center(grid, Vector2.zero, new Vector2(170f, 1f));
            grid = Panel("MapGridVertical", viewport, new Color(1f, 1f, 1f, .12f));
            Center(grid, Vector2.zero, new Vector2(1f, 170f));
            var dot = Panel("MapPlayerHalo", viewport, new Color(.03f, .09f, .065f, .75f));
            Center(dot, Vector2.zero, new Vector2(24f, 24f));
            var arrowRect = Rect("MapPlayerHeading", viewport);
            Center(arrowRect, Vector2.zero, new Vector2(13f, 18f));
            var arrow = arrowRect.gameObject.AddComponent<WorldMapArrow>();
            arrow.color = MapArrow;
            arrow.raycastTarget = false;
            var north = Label("MapNorth", viewport, "world.hud.north", 13, TextColor, TextAnchor.MiddleCenter);
            TopLeft(north.rectTransform, 72f, 4f, 46f, 24f);
            var northBack = Panel("MapNorthBacking", viewport, new Color(.025f, .05f, .035f, .65f));
            TopLeft(northBack, 73f, 5f, 44f, 22f);
            northBack.SetSiblingIndex(north.transform.GetSiblingIndex());
            movementState = Label("WorldMovementState", transform, null, 15, TextColor, TextAnchor.MiddleLeft);
            TopLeft(movementState.rectTransform, 28f, 234f, 210f, 28f);
            var stateShadow = movementState.gameObject.AddComponent<Shadow>();
            stateShadow.effectColor = new Color(0f, 0f, 0f, .7f);
            stateShadow.effectDistance = new Vector2(0f, -1f);
            minimap = gameObject.AddComponent<WorldMinimap>();
            if (player) minimap.Configure(player.transform, raw, arrowRect);

            var dotRect = Panel("WorldReticle", transform, new Color(1f, 1f, 1f, .85f));
            Center(dotRect, Vector2.zero, new Vector2(3f, 3f));
            reticle = dotRect.gameObject;
            var hintPanel = Panel("WorldControlsHint", transform, new Color(.03f, .05f, .04f, .64f));
            hintPanel.anchorMin = hintPanel.anchorMax = new Vector2(.5f, 0f);
            hintPanel.pivot = new Vector2(.5f, 0f);
            hintPanel.anchoredPosition = new Vector2(0f, 22f);
            hintPanel.sizeDelta = new Vector2(1110f, 40f);
            var text = Label("WorldControls", hintPanel, "world.hud.controls", 16, TextColor, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            hints = hintPanel.gameObject;
        }

        void BuildSettings()
        {
            var veil = Panel("WorldSettingsOverlay", transform, new Color(.01f, .015f, .02f, .55f));
            Stretch(veil);
            veil.GetComponent<Image>().raycastTarget = true;
            settings = veil.gameObject;
            var panel = Panel("WorldSettingsWindow", veil, LobbyPalette.Surface);
            Center(panel, Vector2.zero, new Vector2(WindowWidth, WindowHeight));
            var line = Panel("WorldSettingsAccent", panel, Accent);
            TopLeft(line, Pad, 28f, 32f, 3f);
            var title = Label("WorldSettingsTitle", panel, "world.settings.title", 27, TextColor, TextAnchor.MiddleLeft, true);
            TopLeft(title.rectTransform, Pad, 40f, WindowWidth - Pad * 2f, 40f);
            var subtitle = Label("WorldSettingsSubtitle", panel, "world.settings.note", 16, Muted, TextAnchor.MiddleLeft);
            TopLeft(subtitle.rectTransform, Pad, 82f, WindowWidth - Pad * 2f, 28f);
            TopLeft(Panel("WorldSettingsDivider", panel, new Color(1f, 1f, 1f, .09f)), Pad, 122f, WindowWidth - Pad * 2f, 1f);

            // Chap ustun: faqat olam boshqaruvi (cradev.world.*), o'zgarishlar 0.4 s dan keyin saqlanadi.
            var controls = Column(panel, "WorldControlsColumn", Pad, "world.settings.section.controls");
            float y = 32f;
            sensitivitySlider = SettingSlider(controls, "Sensitivity", "world.settings.sensitivity", y,
                WorldPreferences.MinSensitivity, WorldPreferences.MaxSensitivity, out sensitivityValue);
            sensitivitySlider.onValueChanged.AddListener(value =>
            {
                WorldPreferences.Sensitivity = Mathf.Round(value * 20f) / 20f;
                RefreshValues();
                MarkDirty();
            });
            y += SliderRowHeight + RowGap;
            fovSlider = SettingSlider(controls, "FieldOfView", "world.settings.fov", y,
                WorldPreferences.MinFieldOfView, WorldPreferences.MaxFieldOfView, out fovValue);
            fovSlider.wholeNumbers = true;
            fovSlider.onValueChanged.AddListener(value =>
            {
                WorldPreferences.FieldOfView = value;
                if (player && player.ViewCamera) player.ViewCamera.fieldOfView = WorldPreferences.FieldOfView;
                RefreshValues();
                MarkDirty();
            });
            y += SliderRowHeight + RowGap;
            invertToggle = SettingSwitch(controls, "InvertY", "world.settings.invert", y, value => { WorldPreferences.InvertY = value; MarkDirty(); });
            y += RowHeight + RowGap;
            bobToggle = SettingSwitch(controls, "HeadBob", "world.settings.headbob", y, value => { WorldPreferences.HeadBob = value; MarkDirty(); });
            y += RowHeight + RowGap;
            crouchToggle = SettingSwitch(controls, "ToggleCrouch", "world.settings.crouch", y, value => { WorldPreferences.ToggleCrouch = value; MarkDirty(); });

            // O'ng ustun: butun o'yin sozlamalari, lobbydagi bilan bir xil GameSettings API.
            var graphics = Column(panel, "WorldGraphicsColumn", WindowWidth - Pad - ColumnWidth, "world.settings.section.graphics");
            y = 32f;
            displayValue = SettingSelector(graphics, "DisplayMode", "world.settings.display", y,
                _ => ChangeGameSettings(() => GameSettings.Fullscreen = !GameSettings.Fullscreen));
            y += RowHeight + RowGap;
            qualityValue = SettingSelector(graphics, "Quality", "world.settings.quality", y, StepQuality);
            y += RowHeight + RowGap;
            vsyncToggle = SettingSwitch(graphics, "VSync", "world.settings.vsync", y,
                value => ChangeGameSettings(() => GameSettings.VSync = value));
            y += RowHeight + RowGap;
            aoToggle = SettingSwitch(graphics, "AmbientOcclusion", "world.settings.ao", y,
                value => { WorldPreferences.AmbientOcclusion = value; MarkDirty(); });
            y += RowHeight + RowGap;
            volumeSlider = SettingSlider(graphics, "Volume", "world.settings.volume", y, 0f, 1f, out volumeValue);
            volumeSlider.onValueChanged.AddListener(value =>
            {
                // Tortish paytida faqat eshitiladigan balandlik o'zgaradi; to'liq GameSettings.Apply/Save
                // slayder 0.4 s tinch turgach (SavePending) - har kadrda ekran/sifatni qayta qo'llamaslik uchun.
                GameSettings.Volume = Mathf.Clamp01(Mathf.Round(value * 100f) / 100f);
                AudioListener.volume = GameSettings.Volume;
                RefreshValues();
                gameSettingsDirty = true;
                saveAt = Time.unscaledTime + .4f;
            });

            TopLeft(Panel("WorldSettingsFooterDivider", panel, new Color(1f, 1f, 1f, .09f)), Pad, 504f, WindowWidth - Pad * 2f, 1f);
            var keys = Label("WorldSettingsControls", panel, "world.hud.controls", 15, Muted, TextAnchor.MiddleLeft);
            TopLeft(keys.rectTransform, Pad, 516f, WindowWidth - Pad * 2f, 30f);
            keys.horizontalOverflow = HorizontalWrapMode.Wrap;
            keys.resizeTextForBestFit = true;
            keys.resizeTextMinSize = 11;
            keys.resizeTextMaxSize = 15;

            resumeButton = ActionButton(panel, "WorldResume", "world.settings.resume", Pad, 562f, 300f, 52f, true, () => SetSettings(false));
            ActionButton(panel, "WorldReturnLobby", "world.settings.lobby", Pad + 312f, 562f, 300f, 52f, false, ReturnToLobby);
            ActionButton(panel, "WorldResetSettings", "world.settings.reset_controls", WindowWidth - Pad - 240f, 568f, 240f, 40f, false, () =>
            {
                WorldPreferences.Reset();
                dirty = false;
                if (player && player.ViewCamera) player.ViewCamera.fieldOfView = WorldPreferences.FieldOfView;
                RefreshPreferences();
            });
            var saved = Label("WorldAutoSave", panel, "world.settings.saved", 13, Muted, TextAnchor.MiddleLeft);
            TopLeft(saved.rectTransform, Pad, 626f, 600f, 24f);
            settings.SetActive(false);
        }

        RectTransform Column(Transform parent, string name, float x, string headerKey)
        {
            var column = Rect(name, parent);
            TopLeft(column, x, 140f, ColumnWidth, 356f);
            var header = Label(name + "Header", column, headerKey, 13, Muted, TextAnchor.MiddleLeft, true);
            TopLeft(header.rectTransform, 2f, 0f, ColumnWidth - 4f, 22f);
            return column;
        }

        Slider SettingSlider(Transform parent, string name, string key, float y, float min, float max, out Text value)
        {
            var row = Panel(name + "Row", parent, RowFill);
            TopLeft(row, 0f, y, ColumnWidth, SliderRowHeight);
            var label = Label(name + "Label", row, key, 17, TextColor, TextAnchor.MiddleLeft);
            TopLeft(label.rectTransform, 14f, 6f, ColumnWidth - 138f, 30f);
            value = Label(name + "Value", row, null, 17, TextColor, TextAnchor.MiddleRight);
            TopLeft(value.rectTransform, ColumnWidth - 124f, 6f, 110f, 30f);
            var rect = Rect(name + "Slider", row);
            TopLeft(rect, 14f, 38f, ColumnWidth - 28f, 30f);
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            var slider = rect.gameObject.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            var track = Panel("Track", rect, new Color(1f, 1f, 1f, .14f));
            HorizontalLine(track, 4f);
            track.GetComponent<Image>().pixelsPerUnitMultiplier = 12f;
            var fillArea = Rect("FillArea", rect);
            HorizontalLine(fillArea, 4f);
            var fill = Panel("Fill", fillArea, Accent);
            Stretch(fill);
            fill.GetComponent<Image>().pixelsPerUnitMultiplier = 12f;
            slider.fillRect = fill;
            var handleArea = Rect("HandleArea", rect);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(9f, 0f);
            handleArea.offsetMax = new Vector2(-9f, 0f);
            var handle = Panel("Handle", handleArea, TextColor);
            Center(handle, Vector2.zero, new Vector2(18f, 18f));
            var handleImage = handle.GetComponent<Image>();
            handleImage.pixelsPerUnitMultiplier = 24f / 9f;
            handleImage.raycastTarget = true;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            return slider;
        }

        Toggle SettingSwitch(Transform parent, string name, string key, float y, Action<bool> apply)
        {
            var row = Panel(name + "Toggle", parent, InteractiveFill);
            TopLeft(row, 0f, y, ColumnWidth, RowHeight);
            var image = row.GetComponent<Image>();
            image.raycastTarget = true;
            var toggle = row.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = image;
            toggle.colors = RowColors();
            var label = Label(name + "Label", row, key, 17, TextColor, TextAnchor.MiddleLeft);
            TopLeft(label.rectTransform, 14f, 0f, ColumnWidth - 96f, RowHeight);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            var track = Panel("Switch", row, SwitchOff);
            TopLeft(track, ColumnWidth - 66f, 8f, 52f, 28f);
            var trackImage = track.GetComponent<Image>();
            trackImage.pixelsPerUnitMultiplier = 24f / 14f;
            var knob = Panel("SwitchKnob", track, new Color(.93f, .95f, .97f));
            TopLeft(knob, 3f, 3f, 22f, 22f);
            knob.GetComponent<Image>().pixelsPerUnitMultiplier = 24f / 11f;
            switches.Add((toggle, trackImage, knob));
            toggle.onValueChanged.AddListener(value => { apply(value); PaintSwitches(); });
            return toggle;
        }

        Text SettingSelector(Transform parent, string name, string key, float y, Action<int> step)
        {
            var row = Panel(name + "Selector", parent, RowFill);
            TopLeft(row, 0f, y, ColumnWidth, RowHeight);
            var label = Label(name + "Label", row, key, 17, TextColor, TextAnchor.MiddleLeft);
            TopLeft(label.rectTransform, 14f, 0f, ColumnWidth - 250f, RowHeight);
            ArrowButton(row, name + "Previous", "‹", ColumnWidth - 222f, () => step(-1));
            var value = Label(name + "Value", row, null, 16, TextColor, TextAnchor.MiddleCenter);
            TopLeft(value.rectTransform, ColumnWidth - 184f, 0f, 138f, RowHeight);
            ArrowButton(row, name + "Next", "›", ColumnWidth - 42f, () => step(1));
            return value;
        }

        void ArrowButton(Transform parent, string name, string glyph, float x, Action action)
        {
            var rect = Panel(name, parent, SecondaryFill);
            TopLeft(rect, x, 6f, 32f, 32f);
            var image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = RowColors();
            button.onClick.AddListener(() => action());
            var label = Label(name + "Text", rect, null, 22, TextColor, TextAnchor.MiddleCenter);
            label.text = glyph;
            Stretch(label.rectTransform);
        }

        Button ActionButton(Transform parent, string name, string key, float x, float y, float w, float h, bool primary, Action action)
        {
            var rect = Panel(name, parent, primary ? Accent : SecondaryFill);
            TopLeft(rect, x, y, w, h);
            var image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (primary)
            {
                var colors = button.colors;
                colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = new Color(.85f, .88f, .92f);
                colors.fadeDuration = .10f;
                button.colors = colors;
            }
            else button.colors = RowColors();
            button.onClick.AddListener(() => action());
            var label = Label(name + "Text", rect, key, h < 44f ? 15 : 18, TextColor, TextAnchor.MiddleCenter, primary);
            Stretch(label.rectTransform);
            return button;
        }

        /// <summary>Shaffof oq fonli qator: oddiy holatda xira, sichqoncha/klaviatura tanlaganda yorqinroq.</summary>
        static ColorBlock RowColors()
        {
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = new Color(1f, 1f, 1f, .5f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(1f, 1f, 1f, .8f);
            colors.disabledColor = new Color(1f, 1f, 1f, .25f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = .10f;
            return colors;
        }

        void PaintSwitches()
        {
            foreach (var item in switches)
            {
                if (!item.toggle) continue;
                bool on = item.toggle.isOn;
                item.track.color = on ? Accent : SwitchOff;
                item.knob.anchoredPosition = new Vector2(on ? 27f : 3f, -3f);
            }
        }

        void StepQuality(int direction)
        {
            int count = QualitySettings.names.Length;
            if (count < 2) return;
            ChangeGameSettings(() => GameSettings.Quality = ((GameSettings.Quality + direction) % count + count) % count);
        }

        /// <summary>Lobby sozlamalaridagi kabi: o'zgartirish, darhol qo'llash va saqlash.</summary>
        void ChangeGameSettings(Action change)
        {
            change();
            gameSettingsDirty = false;
            GameSettings.Apply();
            GameSettings.Save();
            RefreshPreferences();
        }

        static string QualityName(int level)
        {
            string key = "quality." + level;
            if (Loc.Has(key)) return Loc.T(key);
            var names = QualitySettings.names;
            return level >= 0 && level < names.Length ? names[level] : level.ToString(CultureInfo.InvariantCulture);
        }

        void RefreshText()
        {
            foreach (var item in localized)
                if (item.label) item.label.text = Loc.T(item.key);
            stateKey = null;
            RefreshPreferences();
        }

        void RefreshPreferences()
        {
            if (!sensitivitySlider) return;
            sensitivitySlider.SetValueWithoutNotify(WorldPreferences.Sensitivity);
            fovSlider.SetValueWithoutNotify(WorldPreferences.FieldOfView);
            invertToggle.SetIsOnWithoutNotify(WorldPreferences.InvertY);
            bobToggle.SetIsOnWithoutNotify(WorldPreferences.HeadBob);
            crouchToggle.SetIsOnWithoutNotify(WorldPreferences.ToggleCrouch);
            aoToggle.SetIsOnWithoutNotify(WorldPreferences.AmbientOcclusion);
            vsyncToggle.SetIsOnWithoutNotify(GameSettings.VSync);
            volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
            PaintSwitches();
            RefreshValues();
        }

        void RefreshValues()
        {
            sensitivityValue.text = WorldPreferences.Sensitivity.ToString("0.00", CultureInfo.InvariantCulture);
            fovValue.text = Mathf.RoundToInt(WorldPreferences.FieldOfView) + "°";
            volumeValue.text = Mathf.RoundToInt(GameSettings.Volume * 100f) + "%";
            displayValue.text = Loc.T(GameSettings.Fullscreen ? "world.settings.display.fullscreen" : "world.settings.display.windowed");
            qualityValue.text = QualityName(GameSettings.Quality);
        }

        void MarkDirty()
        {
            dirty = true;
            saveAt = Time.unscaledTime + .4f;
        }

        void SavePending()
        {
            if (dirty)
            {
                dirty = false;
                WorldPreferences.Save();
            }
            if (gameSettingsDirty)
            {
                gameSettingsDirty = false;
                GameSettings.Apply();
                GameSettings.Save();
            }
        }

        public void ReturnToLobby()
        {
            if (leaving) return;
            leaving = true;
            SavePending();
            if (player) player.SetPaused(true);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneLoader.Load("MainMenu");
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) { SavePending(); SetSettings(true); }
        }

        void OnDestroy()
        {
            SavePending();
            Loc.Changed -= RefreshText;
            WorldPreferences.Changed -= RefreshPreferences;
            if (player) player.PauseChanged -= OnPauseChanged;
        }

        Text Label(string name, Transform parent, string key, int size, Color color, TextAnchor alignment, bool bold = false)
        {
            var label = Rect(name, parent).gameObject.AddComponent<Text>();
            label.font = bold && boldFont ? boldFont : font;
            if (bold && !boldFont) label.fontStyle = FontStyle.Bold;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            if (key != null) localized.Add((label, key));
            return label;
        }

        /// <summary>8 px radiusli tekis panel (lobby bilan bir xil).</summary>
        RectTransform Panel(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = rounded;
            image.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = 3f;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        static void TopLeft(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
        }

        static void Center(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static void HorizontalLine(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, .5f);
            rect.anchorMax = new Vector2(1f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
        }

        static bool MenuPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                   (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7);
#else
            return false;
#endif
        }
    }
}

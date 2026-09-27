using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CraDev.World
{
    /// <summary>Dunyoning ixcham HUD'i va lobbydan alohida boshqaruv sozlamalari.</summary>
    public sealed class WorldHud : MonoBehaviour
    {
        [SerializeField] WorldPlayerController player;
        [SerializeField] Font font;
        [SerializeField] Sprite rounded;
        [SerializeField] Camera mapCamera;

        static readonly Color TextColor = new Color(.93f, .96f, .94f);
        static readonly Color Muted = new Color(.64f, .71f, .68f);
        static readonly Color Accent = new Color(.38f, .83f, .65f);
        static readonly Color Surface = new Color(.075f, .10f, .093f, .98f);
        readonly List<(Text label, string key)> localized = new List<(Text, string)>();
        GameObject settings, reticle, hints;
        Text movementState, sensitivityValue, fovValue;
        Slider sensitivitySlider, fovSlider;
        Toggle invertToggle, bobToggle, crouchToggle;
        Button resumeButton;
        WorldMinimap minimap;
        bool settingsOpen, dirty, leaving;
        float saveAt;
        string stateKey;

        public bool SettingsOpen => settingsOpen;
        public RenderTexture MapTexture => minimap ? minimap.Texture : null;
        public Camera MapCamera => minimap ? minimap.MapCamera : mapCamera;

        void Start()
        {
            if (!player) player = FindFirstObjectByType<WorldPlayerController>();
            if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            WorldPreferences.Load();
            BuildHud();
            BuildSettings();
            Loc.Changed += RefreshText;
            if (player) player.PauseChanged += OnPauseChanged;
            RefreshText();
            RefreshPreferences();
            SetSettings(player && player.Paused);
        }

        void Update()
        {
            if (leaving) return;
            if (MenuPressed()) SetSettings(!settingsOpen);
            if (dirty && Time.unscaledTime >= saveAt) SavePending();
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

        public void SetSettings(bool open)
        {
            if (leaving) return;
            if (player && player.Paused != open) player.SetPaused(open);
            ApplySettingsVisibility(open);
        }

        void OnPauseChanged(bool paused) => ApplySettingsVisibility(paused);

        void ApplySettingsVisibility(bool open)
        {
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
            arrow.color = Accent;
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
            if (player) minimap.Configure(player.transform, mapCamera, raw, arrowRect);

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
            var veil = Panel("WorldSettingsOverlay", transform, new Color(.015f, .025f, .025f, .55f));
            Stretch(veil);
            veil.GetComponent<Image>().raycastTarget = true;
            settings = veil.gameObject;
            var panel = Panel("WorldSettingsWindow", veil, Surface);
            Center(panel, Vector2.zero, new Vector2(660f, 670f));
            var line = Panel("WorldSettingsAccent", panel, Accent);
            TopLeft(line, 32f, 25f, 32f, 3f);
            var title = Label("WorldSettingsTitle", panel, "world.settings.title", 27, TextColor, TextAnchor.MiddleLeft);
            TopLeft(title.rectTransform, 32f, 38f, 576f, 40f);
            title.fontStyle = FontStyle.Bold;
            var subtitle = Label("WorldSettingsSubtitle", panel, "world.settings.subtitle", 16, Muted, TextAnchor.MiddleLeft);
            TopLeft(subtitle.rectTransform, 32f, 81f, 596f, 30f);
            line = Panel("WorldSettingsDivider", panel, new Color(1f, 1f, 1f, .09f));
            TopLeft(line, 32f, 120f, 596f, 1f);

            sensitivitySlider = SettingSlider(panel, "Sensitivity", "world.settings.sensitivity", 144f, .1f, 3f, out sensitivityValue);
            sensitivitySlider.onValueChanged.AddListener(value =>
            {
                WorldPreferences.Sensitivity = value;
                RefreshValues();
                MarkDirty();
            });
            fovSlider = SettingSlider(panel, "FieldOfView", "world.settings.fov", 225f, 65f, 100f, out fovValue);
            fovSlider.wholeNumbers = true;
            fovSlider.onValueChanged.AddListener(value =>
            {
                WorldPreferences.FieldOfView = value;
                if (player && player.ViewCamera) player.ViewCamera.fieldOfView = value;
                RefreshValues();
                MarkDirty();
            });
            invertToggle = SettingToggle(panel, "InvertY", "world.settings.invert", 311f, value => WorldPreferences.InvertY = value);
            bobToggle = SettingToggle(panel, "HeadBob", "world.settings.headbob", 364f, value => WorldPreferences.HeadBob = value);
            crouchToggle = SettingToggle(panel, "ToggleCrouch", "world.settings.crouch", 417f, value => WorldPreferences.ToggleCrouch = value);
            var controls = Label("WorldSettingsControls", panel, "world.hud.controls", 15, Muted, TextAnchor.MiddleLeft);
            TopLeft(controls.rectTransform, 32f, 481f, 596f, 54f);
            controls.horizontalOverflow = HorizontalWrapMode.Wrap;

            resumeButton = ActionButton(panel, "WorldResume", "world.settings.resume", 32f, 554f, 292f, 48f,
                new Color(.19f, .42f, .33f), () => SetSettings(false));
            ActionButton(panel, "WorldReturnLobby", "world.settings.lobby", 336f, 554f, 292f, 48f,
                new Color(.14f, .18f, .16f), ReturnToLobby);
            ActionButton(panel, "WorldResetSettings", "world.settings.reset", 32f, 616f, 210f, 32f,
                new Color(.105f, .14f, .12f), () =>
                {
                    WorldPreferences.Reset();
                    dirty = false;
                    RefreshPreferences();
                    if (player && player.ViewCamera) player.ViewCamera.fieldOfView = WorldPreferences.FieldOfView;
                });
            var saved = Label("WorldAutoSave", panel, "world.settings.saved", 13, Muted, TextAnchor.MiddleRight);
            TopLeft(saved.rectTransform, 255f, 616f, 373f, 32f);
            settings.SetActive(false);
        }

        Slider SettingSlider(Transform parent, string name, string key, float y, float min, float max, out Text value)
        {
            var label = Label(name + "Label", parent, key, 18, TextColor, TextAnchor.MiddleLeft);
            TopLeft(label.rectTransform, 32f, y, 490f, 30f);
            value = Label(name + "Value", parent, null, 17, Accent, TextAnchor.MiddleRight);
            TopLeft(value.rectTransform, 526f, y, 102f, 30f);
            var rect = Rect(name + "Slider", parent);
            TopLeft(rect, 32f, y + 32f, 596f, 34f);
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            var slider = rect.gameObject.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            var track = Panel("Track", rect, new Color(.20f, .26f, .23f));
            HorizontalLine(track, 4f);
            var fillArea = Rect("FillArea", rect);
            HorizontalLine(fillArea, 4f);
            var fill = Panel("Fill", fillArea, Accent);
            Stretch(fill);
            slider.fillRect = fill;
            var handleArea = Rect("HandleArea", rect);
            Stretch(handleArea);
            var handle = Panel("Handle", handleArea, TextColor);
            Center(handle, Vector2.zero, new Vector2(16f, 16f));
            handle.GetComponent<Image>().raycastTarget = true;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            return slider;
        }

        Toggle SettingToggle(Transform parent, string name, string key, float y, Action<bool> apply)
        {
            var row = Rect(name + "Toggle", parent);
            TopLeft(row, 32f, y, 596f, 43f);
            var hit = row.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            var toggle = row.gameObject.AddComponent<Toggle>();
            var label = Label(name + "Label", row, key, 17, TextColor, TextAnchor.MiddleLeft);
            TopLeft(label.rectTransform, 0f, 0f, 530f, 43f);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            var box = Panel("Checkbox", row, new Color(.18f, .25f, .21f));
            TopLeft(box, 564f, 7f, 28f, 28f);
            var check = Panel("Checked", box, Accent);
            TopLeft(check, 6f, 6f, 16f, 16f);
            toggle.targetGraphic = box.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            toggle.onValueChanged.AddListener(value => { apply(value); MarkDirty(); });
            return toggle;
        }

        Button ActionButton(Transform parent, string name, string key, float x, float y, float w, float h, Color color, Action action)
        {
            var rect = Panel(name, parent, color);
            TopLeft(rect, x, y, w, h);
            var image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(.80f, .88f, .84f);
            colors.fadeDuration = .10f;
            button.colors = colors;
            button.onClick.AddListener(() => action());
            var label = Label(name + "Text", rect, key, h < 40f ? 14 : 18, TextColor, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            return button;
        }

        void RefreshText()
        {
            foreach (var item in localized)
                if (item.label) item.label.text = Loc.T(item.key);
            stateKey = null;
        }

        void RefreshPreferences()
        {
            if (!sensitivitySlider) return;
            sensitivitySlider.SetValueWithoutNotify(WorldPreferences.Sensitivity);
            fovSlider.SetValueWithoutNotify(WorldPreferences.FieldOfView);
            invertToggle.SetIsOnWithoutNotify(WorldPreferences.InvertY);
            bobToggle.SetIsOnWithoutNotify(WorldPreferences.HeadBob);
            crouchToggle.SetIsOnWithoutNotify(WorldPreferences.ToggleCrouch);
            RefreshValues();
        }

        void RefreshValues()
        {
            sensitivityValue.text = WorldPreferences.Sensitivity.ToString("0.00", CultureInfo.InvariantCulture);
            fovValue.text = Mathf.RoundToInt(WorldPreferences.FieldOfView) + "°";
        }

        void MarkDirty()
        {
            dirty = true;
            saveAt = Time.unscaledTime + .4f;
        }

        void SavePending()
        {
            if (!dirty) return;
            WorldPreferences.Save();
            dirty = false;
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
            if (player) player.PauseChanged -= OnPauseChanged;
        }

        Text Label(string name, Transform parent, string key, int size, Color color, TextAnchor alignment)
        {
            var label = Rect(name, parent).gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            if (key != null) localized.Add((label, key));
            return label;
        }

        RectTransform Panel(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = rounded;
            image.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = 4f;
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

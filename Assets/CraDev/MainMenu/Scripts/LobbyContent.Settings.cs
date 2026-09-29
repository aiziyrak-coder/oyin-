using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using CraDev.Online;
using CraDev.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    // Sozlamalar (lobby ustidagi oyna). Har o'zgarish darhol qo'llanadi va saqlanadi; ekran rejimi/o'lchami
    // o'zgarsa 10 soniyalik "Saqlansinmi?" so'raladi. Boshqaruv sozlamalari olam bilan bitta: WorldPreferences.
    public partial class LobbyContent
    {
        /// <summary>Yon menyu bo'limlari tartibi (builder ham shu ro'yxatdan quradi): settings.section.* kalitlari.</summary>
        public static readonly string[] SettingsSections = { "profile", "graphics", "audio", "controls", "lobby", "privacy", "language", "help" };

        bool countryView, countryBusy, keepSettingsScroll;
        bool saveGame, saveWorld;
        float saveAt, lastPreview;
        Coroutine saveRoutine;
        // Maxfiylik: foydalanuvchi xohlagan holat (javob kelguncha switch shu holatda), bir vaqtda bitta so'rov
        bool? wantOnline, wantRequests;
        bool privacyBusy;
        // "Saqlansinmi?" oynasi
        RectTransform keepDialog;
        Text keepMessage;
        GameSettings.DisplayState keepPrevious;
        float keepUntil;
        bool profileSubscribed;

        bool DesiredOnline => wantOnline ?? PlayerProfile.ShowOnline;
        bool DesiredRequests => wantRequests ?? PlayerProfile.AllowRequests;

        void Settings()
        {
            settingsRefresh.Clear();
            settingsY = 0;
            // Qatorlar orasidagi bo'sh joy ham g'ildirak/sudrash bilan aylantiradi (qatorlarning o'zi bosilmaydi)
            var scrollHit = surface.gameObject.AddComponent<Image>(); scrollHit.color = Color.clear; scrollHit.raycastTarget = true;
            SyncSettingsSidebar();
            section = Mathf.Clamp(section, 0, SettingsSections.Length - 1);
            if (section == 0 && countryView) CountryPicker();
            else
            {
                SectionTitle(Loc.T("settings.section." + SettingsSections[section]));
                switch (section)
                {
                    case 0: ProfileSection(); break;
                    case 1: GraphicsSection(); break;
                    case 2: AudioSection(); break;
                    case 3: ControlsSection(); break;
                    case 4: LobbySection(); break;
                    case 5: PrivacySection(); break;
                    case 6: LanguageSection(); break;
                    case 7: HelpSection(); break;
                }
            }
            ContentHeight(Mathf.Max(settingsY + 16, 64));
        }

        // Eski sahnada yon menyu boshqa bo'limlar nomi bilan qurilgan bo'lishi mumkin: yozuvlar runtime ro'yxatiga moslanadi.
        void SyncSettingsSidebar()
        {
            if (categories == null) return;
            for (int i = 0; i < categories.Length && i < SettingsSections.Length; i++)
            {
                var label = categories[i] != null ? categories[i].GetComponentInChildren<LocalizedText>() : null;
                string key = "settings.section." + SettingsSections[i];
                if (label != null && label.Key != key) label.Key = key;
            }
        }

        void Rerender(bool keepScroll)
        {
            keepSettingsScroll = keepScroll;
            Render();
        }

        // ------------------------------------------------------------------ Bo'limlar

        void ProfileSection()
        {
            var card = Panel("ProfileCard", 0, settingsY, RowWidth, 128, RowFill);
            Portrait(card.transform, PlayerProfile.AvatarId, 20, 16, 96);
            var nickname = Text(PlayerProfile.Nickname, 136, 20, RowWidth - 160, 50, 34, card.transform);
            nickname.font = bold; nickname.color = Bright;
            Text(Loc.F("settings.id", PlayerProfile.PublicId.ToString("D6")), 136, 72, RowWidth - 160, 34, 23, card.transform).color = Soft;
            settingsY += 140;
            string avatar = Lobby.FindAvatar(PlayerProfile.AvatarId)?.title;
            ActionRow("EditAvatar", Loc.T("settings.avatar"), Loc.T("settings.avatar_hint"), Loc.T("settings.edit"), EditAvatar, avatar);
            InfoRow("Nickname", Loc.T("settings.row.nickname"), PlayerProfile.Nickname, Loc.T("create.nickname_locked"));
            NavRow("Country", Loc.T("settings.row.country"), CountryName(PlayerProfile.Country), () => { countryView = true; Rerender(false); });
            InfoRow("Account", Loc.T("settings.security.account"), Loc.T("settings.account_value"));
        }

        void EditAvatar()
        {
            Lobby.SetSettings(false);
            Lobby.Customize();
        }

        static string CountryName(string code) =>
            string.IsNullOrEmpty(code) ? Loc.T("settings.not_set") : Loc.Has("country." + code) ? Loc.T("country." + code) : code;

        void CountryPicker()
        {
            var back = Button(Loc.T("settings.back"), 0, 0, 200, 52, () => { countryView = false; Rerender(false); });
            back.name = "CountryBack";
            if (chevronLeft != null)
            {
                Glyph(back.transform, chevronLeft, null, 14, 14, 24, Bright);
                var text = back.GetComponentInChildren<Text>();
                text.rectTransform.anchoredPosition = new Vector2(46, 0); text.rectTransform.sizeDelta = new Vector2(140, 52);
            }
            var title = Text(Loc.T("settings.country_title"), 224, 0, RowWidth - 228, 52, 34);
            title.font = bold; title.name = "SectionTitle";
            settingsY = 76;
            string[] countries = { "UZ", "KZ", "KG", "TJ", "TM", "RU", "TR", "AE", "KR", "DE", "GB", "US" };
            const int columns = 3;
            float gap = 10, w = (RowWidth - gap * (columns - 1)) / columns;
            for (int i = 0; i < countries.Length; i++)
            {
                string code = countries[i];
                var button = Button(Loc.T("country." + code), i % columns * (w + gap), settingsY + i / columns * 74, w, 64,
                    () => PickCountry(code), code == PlayerProfile.Country);
                button.name = "Country_" + code;
            }
            settingsY += (countries.Length + columns - 1) / columns * 74;
        }

        void PickCountry(string code)
        {
            if (countryBusy) return;
            if (code == PlayerProfile.Country) { countryView = false; Rerender(false); return; }
            countryBusy = true;
            Lobby.StartCoroutine(Lobby.Api.UpdateCountry(PlayerProfile.Token, code, result =>
            {
                countryBusy = false;
                if (result.Ok) PlayerProfile.SetCountry(string.IsNullOrEmpty(result.Data.country) ? code : result.Data.country);
                Lobby.Toast(Loc.T(result.Ok ? "settings.saved" : "common.server_error"));
                if (this == null || !result.Ok) return;
                countryView = false;
                if (isActiveAndEnabled) Rerender(false);
            }));
        }

        void GraphicsSection()
        {
            var modes = GameSettings.DisplayModes;
            SegmentedRow("DisplayMode", Loc.T("settings.display"), null, modes.Select(ModeName).ToArray(),
                () => Array.IndexOf(modes, GameSettings.DisplayMode), i => ChangeDisplay(() => GameSettings.DisplayMode = modes[i]));
            var resolution = StepperRow("Resolution", ResolutionLabel(), ResolutionHint(), ResolutionText,
                () => ResolutionIndex() < ResolutionList().Count - 1, () => ResolutionIndex() > 0,
                () => StepResolution(1), () => StepResolution(-1), () => GameSettings.DisplayMode != FullScreenMode.FullScreenWindow);
            settingsRefresh.Add(() => { if (resolution.label != null) { resolution.label.text = ResolutionLabel(); resolution.hint.text = ResolutionHint(); } });
            StepperRow("Quality", Loc.T("settings.quality"), Loc.T("settings.quality_hint"), () => GameSettings.QualityName(GameSettings.Quality),
                () => GameSettings.Quality > 0, () => GameSettings.Quality < QualitySettings.names.Length - 1,
                () => SetQuality(GameSettings.Quality - 1), () => SetQuality(GameSettings.Quality + 1));
            SwitchRow("VSync", Loc.T("settings.vsync"), Loc.T("settings.vsync_hint"), () => GameSettings.VSync,
                value => { GameSettings.VSync = value; ApplySettings(); RefreshSettings(); });
            var limits = GameSettings.FrameLimits;
            var fps = StepperRow("FrameLimit", Loc.T("settings.fps"), FpsHint(), () => FpsName(GameSettings.FrameLimit),
                () => FpsIndex() > 0, () => FpsIndex() < limits.Length - 1,
                () => SetFrameLimit(limits[Mathf.Max(0, FpsIndex() - 1)]), () => SetFrameLimit(limits[Mathf.Min(limits.Length - 1, FpsIndex() + 1)]),
                () => !GameSettings.VSync);
            settingsRefresh.Add(() => { if (fps.hint != null) fps.hint.text = FpsHint(); });
            SettingsFooter(GameSettings.ResetDisplay);
        }

        static string ModeName(FullScreenMode mode) => Loc.T(mode == FullScreenMode.ExclusiveFullScreen ? "settings.mode.exclusive"
            : mode == FullScreenMode.Windowed ? "settings.windowed" : "settings.mode.borderless");

        static string ResolutionLabel() => Loc.T(GameSettings.DisplayMode == FullScreenMode.Windowed ? "settings.size" : "settings.resolution");

        static string ResolutionHint() => Loc.T(GameSettings.DisplayMode == FullScreenMode.Windowed ? "settings.resolution_hint.windowed"
            : GameSettings.DisplayMode == FullScreenMode.ExclusiveFullScreen ? "settings.resolution_hint.exclusive" : "settings.resolution_hint.borderless");

        static string ResolutionText()
        {
            var size = GameSettings.TargetSize(GameSettings.DisplayMode);
            return GameSettings.DisplayMode == FullScreenMode.FullScreenWindow
                ? Loc.F("settings.resolution_native", size.x, size.y) : size.x + " × " + size.y;
        }

        static System.Collections.Generic.List<Vector2Int> ResolutionList() => GameSettings.ResolutionsFor(GameSettings.DisplayMode);

        // Ro'yxat kattasidan kichigiga: joriy o'lcham yo'q bo'lsa (oyna qo'lda o'zgargan) eng yaqini olinadi.
        static int ResolutionIndex()
        {
            var list = ResolutionList();
            var size = GameSettings.TargetSize(GameSettings.DisplayMode);
            int index = list.IndexOf(size);
            if (index >= 0) return index;
            int best = 0;
            for (int i = 1; i < list.Count; i++)
                if (Mathf.Abs(list[i].x * list[i].y - size.x * size.y) < Mathf.Abs(list[best].x * list[best].y - size.x * size.y)) best = i;
            return best;
        }

        // step > 0: kichikroq o'lcham (‹), step < 0: kattaroq (›)
        void StepResolution(int step)
        {
            var list = ResolutionList();
            if (list.Count == 0 || GameSettings.DisplayMode == FullScreenMode.FullScreenWindow) return;
            var target = list[Mathf.Clamp(ResolutionIndex() + step, 0, list.Count - 1)];
            ChangeDisplay(() => GameSettings.Resolution = target);
        }

        void SetQuality(int level)
        {
            GameSettings.Quality = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
            ApplySettings();
        }

        static int FpsIndex() => Mathf.Max(0, Array.IndexOf(GameSettings.FrameLimits, GameSettings.FrameLimit));
        static string FpsName(int limit) => limit <= 0 ? Loc.T("settings.fps_unlimited") : Loc.F("settings.fps_value", limit);
        static string FpsHint() => Loc.T(GameSettings.VSync ? "settings.fps_hint_vsync" : "settings.fps_hint");

        void SetFrameLimit(int limit)
        {
            GameSettings.FrameLimit = limit;
            ApplySettings();
        }

        void AudioSection()
        {
            Func<float, string> percent = value => Mathf.RoundToInt(value * 100) + "%";
            SliderRow("MasterVolume", Loc.T("settings.volume_master"), null, 0, 1, .01f, () => GameSettings.Volume, percent, value =>
            {
                GameSettings.Volume = value; GameSettings.Apply(); QueueSave(game: true); PreviewSound();
            });
            SliderRow("InterfaceVolume", Loc.T("settings.volume_ui"), Loc.T("settings.volume_ui_hint"), 0, 1, .01f, () => GameSettings.SfxVolume, percent, value =>
            {
                GameSettings.SfxVolume = value; GameSettings.Apply(); QueueSave(game: true); PreviewSound();
            });
            SettingsFooter(GameSettings.ResetAudio);
        }

        // Ovoz sozlanayotganda yangi balandlikni eshitish uchun qisqa "tik" (tez-tez emas)
        void PreviewSound()
        {
            if (Time.unscaledTime - lastPreview < .12f) return;
            lastPreview = Time.unscaledTime;
            UiSounds.PlayClick();
        }

        void ControlsSection()
        {
            SliderRow("Sensitivity", Loc.T("world.settings.sensitivity"), null, .1f, 3f, .05f, () => WorldPreferences.Sensitivity,
                value => value.ToString("0.00", CultureInfo.InvariantCulture), value => { WorldPreferences.Sensitivity = value; QueueSave(world: true); });
            SwitchRow("InvertY", Loc.T("world.settings.invert"), null, () => WorldPreferences.InvertY,
                value => { WorldPreferences.InvertY = value; WorldPreferences.Save(); });
            SliderRow("FieldOfView", Loc.T("world.settings.fov"), null, 65, 100, 1, () => WorldPreferences.FieldOfView,
                value => Mathf.RoundToInt(value) + "°", value => { WorldPreferences.FieldOfView = value; QueueSave(world: true); });
            SwitchRow("HeadBob", Loc.T("world.settings.headbob"), null, () => WorldPreferences.HeadBob,
                value => { WorldPreferences.HeadBob = value; WorldPreferences.Save(); });
            SegmentedRow("CrouchMode", Loc.T("settings.crouch_mode"), null, new[] { Loc.T("settings.crouch_hold"), Loc.T("settings.crouch_toggle") },
                () => WorldPreferences.ToggleCrouch ? 1 : 0, i => { WorldPreferences.ToggleCrouch = i == 1; WorldPreferences.Save(); }, 420);
            Paragraph(Loc.T("settings.controls_note"), Soft, 23);
            Subheading(Loc.T("settings.keys_title"));
            // Olam boshqaruvi (WorldPlayerController.ReadInput) bilan bir xil; qayta tayinlash hozircha yo'q
            KeyRow("Move", Loc.T("settings.key.move"), new[] { "W", "A", "S", "D" }, new[] { Loc.T("settings.key.arrows") });
            KeyRow("Sprint", Loc.T("settings.key.sprint"), new[] { "Shift" });
            KeyRow("Crouch", Loc.T("settings.key.crouch"), new[] { "Ctrl" }, new[] { "C" });
            KeyRow("Jump", Loc.T("settings.key.jump"), new[] { Loc.T("settings.key.space") });
            KeyRow("Look", Loc.T("settings.key.look"), new[] { Loc.T("settings.key.mouse_cap") });
            KeyRow("Menu", Loc.T("settings.key.esc"), new[] { "Esc" });
            SettingsFooter(WorldPreferences.Reset);
        }

        void LobbySection()
        {
            string[] weather = Enumerable.Range(0, 4).Select(i => Loc.T("weather." + i)).ToArray();
            SegmentedRow("LobbyWeather", Loc.T("settings.weather"), Loc.T("settings.weather_hint"), weather,
                () => LobbyEnvironment.Weather, i => LobbyEnvironment.Weather = i, 660);
            SwitchRow("Atmosphere", Loc.T("settings.atmosphere"), null, () => LobbyEnvironment.Motion, value => LobbyEnvironment.Motion = value);
            SwitchRow("NotifyRequests", Loc.T("settings.notify.requests"), Loc.T("settings.notify.requests_hint"),
                () => LobbyPrefs.NotifyFriendRequests, value => LobbyPrefs.NotifyFriendRequests = value);
            SettingsFooter(() => { LobbyEnvironment.Weather = 0; LobbyEnvironment.Motion = true; LobbyPrefs.Reset(); });
        }

        void PrivacySection()
        {
            SwitchRow("ShowOnline", Loc.T("settings.privacy.online"), null, () => DesiredOnline, value => SetPrivacy(value, null));
            SwitchRow("AllowRequests", Loc.T("settings.privacy.requests"), null, () => DesiredRequests, value => SetPrivacy(null, value));
            settingsY += 6;
            Paragraph(Loc.T("settings.privacy.info"), Soft, 24);
        }

        void LanguageSection()
        {
            float w = (RowWidth - 12) / 2;
            var uz = Button("O'zbekcha", 0, settingsY, w, 88, () => Loc.Current = Language.Uz, Loc.Current == Language.Uz);
            var en = Button("English", w + 12, settingsY, w, 88, () => Loc.Current = Language.En, Loc.Current == Language.En);
            uz.name = "Language_uz"; en.name = "Language_en";
            foreach (var text in new[] { uz.GetComponentInChildren<Text>(), en.GetComponentInChildren<Text>() })
            {
                text.alignment = TextAnchor.MiddleCenter; text.fontSize = 26; text.rectTransform.anchoredPosition = Vector2.zero;
                text.rectTransform.sizeDelta = new Vector2(w, 88);
            }
            settingsY += 106;
            Paragraph(Loc.T("settings.language_hint"), Soft, 23);
        }

        void HelpSection()
        {
            Paragraph(Loc.T("settings.help.lobby"), Bright, 25);
            Subheading(Loc.T("settings.help.world_title"));
            Paragraph(Loc.T("settings.help.world"), Bright, 25);
            Paragraph(Loc.F("settings.version", Application.version), Soft, 22);
        }

        // ------------------------------------------------------------------ Saqlash

        void ApplySettings() { GameSettings.Apply(); GameSettings.Save(); }

        // Slayder sudralayotganda har kadrda diskka yozilmaydi: to'xtagandan 0.4 s keyin saqlanadi
        void QueueSave(bool game = false, bool world = false)
        {
            saveGame |= game; saveWorld |= world;
            saveAt = Time.unscaledTime + .4f;
            if (saveRoutine == null && isActiveAndEnabled) saveRoutine = StartCoroutine(SaveLater());
        }

        IEnumerator SaveLater()
        {
            while (Time.unscaledTime < saveAt) yield return null;
            saveRoutine = null;
            FlushSettingsSaves();
        }

        void FlushSettingsSaves()
        {
            if (saveRoutine != null) { StopCoroutine(saveRoutine); saveRoutine = null; }
            if (saveGame) GameSettings.Save();
            if (saveWorld) WorldPreferences.Save();
            saveGame = saveWorld = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            FlushSettingsSaves();
        }

        // ------------------------------------------------------------------ Ochish, yopish, Esc

        void SettingsShown()
        {
            countryView = false;
            if (!profileSubscribed && Lobby != null) { Lobby.ProfileChanged += OnSettingsProfileChanged; profileSubscribed = true; }
        }

        void SettingsHidden()
        {
            if (keepDialog != null) RevertDisplay();
            FlushSettingsSaves();
            countryView = false;
            if (profileSubscribed && Lobby != null) { Lobby.ProfileChanged -= OnSettingsProfileChanged; profileSubscribed = false; }
        }

        // Serverdan profil yangilandi (nickname, mamlakat, maxfiylik): ko'rinib turgan bo'lim joyida yangilanadi
        void OnSettingsProfileChanged()
        {
            if (this == null || !isActiveAndEnabled) return;
            if (section == 0 && !countryView) Rerender(true);
            else RefreshSettings();
        }

        bool SettingsBack()
        {
            if (keepDialog != null) { RevertDisplay(); return true; }
            if (countryView) { countryView = false; Rerender(false); return true; }
            return false;
        }

        // ------------------------------------------------------------------ Maxfiylik: optimistik, ketma-ket so'rovlar

        void SetPrivacy(bool? online, bool? requests)
        {
            if (online.HasValue) wantOnline = online;
            if (requests.HasValue) wantRequests = requests;
            if (!privacyBusy) Lobby.StartCoroutine(SendPrivacy());
        }

        // Bir vaqtda bitta so'rov: javob kelguncha yana bosilsa, oxirgi xohlangan holat keyingi so'rovda yuboriladi.
        // Har so'rov ikkala qiymatni ham hozirgi xohlangan holatdan oladi, shuning uchun biri ikkinchisini qaytarmaydi.
        IEnumerator SendPrivacy()
        {
            privacyBusy = true;
            bool failed = false;
            while (true)
            {
                bool online = DesiredOnline, requests = DesiredRequests;
                if (online == PlayerProfile.ShowOnline && requests == PlayerProfile.AllowRequests) break;
                ApiResult<PlayerResponse> result = default;
                yield return Lobby.Api.UpdatePrivacy(PlayerProfile.Token, online, requests, r => result = r);
                if (!result.Ok) { failed = true; break; }
                PlayerProfile.SetPrivacy(result.Data.showOnline, result.Data.allowRequests);
                // Server boshqa qiymat qaytarsa, qayta-qayta yubormaymiz: serverdagi holat ko'rsatiladi
                if (result.Data.showOnline != online || result.Data.allowRequests != requests) break;
            }
            wantOnline = wantRequests = null; // switchlar endi serverdagi (saqlangan) holatni ko'rsatadi
            privacyBusy = false;
            // Muvaffaqiyatda switch allaqachon to'g'ri turibdi; xatoda u saqlangan holatga qaytadi va xabar chiqadi
            if (failed) Lobby.Toast(Loc.T("common.server_error"));
            RefreshSettings();
        }

        // ------------------------------------------------------------------ Ekran: "Saqlansinmi?" (10 s)

        void ChangeDisplay(Action change)
        {
            var before = GameSettings.CaptureDisplay();
            change();
            if (GameSettings.SameDisplay(before, GameSettings.CaptureDisplay())) { GameSettings.Save(); RefreshSettings(); return; }
            GameSettings.Apply(); // tasdiqlanmaguncha saqlanmaydi
            RefreshSettings();
            AskKeepDisplay(keepDialog != null ? keepPrevious : before);
        }

        void AskKeepDisplay(GameSettings.DisplayState previous)
        {
            keepPrevious = previous;
            keepUntil = Time.unscaledTime + 10f;
            if (keepDialog != null) return;
            var dim = new GameObject("KeepDisplayDialog", typeof(RectTransform)) { layer = 5 }.GetComponent<RectTransform>();
            dim.SetParent(transform, false);
            dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one;
            dim.offsetMin = new Vector2(-3000, -3000); dim.offsetMax = new Vector2(3000, 3000);
            var shade = dim.gameObject.AddComponent<Image>(); shade.color = new Color(0, 0, 0, .55f); shade.raycastTarget = true;
            var sheet = Panel("Sheet", 0, 0, 700, 340, SettingsSheet, dim);
            sheet.raycastTarget = true;
            var sheetRect = sheet.rectTransform;
            sheetRect.anchorMin = sheetRect.anchorMax = sheetRect.pivot = new Vector2(.5f, .5f); sheetRect.anchoredPosition = Vector2.zero;
            var title = Text(Loc.T("settings.keep_title"), 40, 30, 620, 54, 32, sheet.transform); title.font = bold; title.color = Bright;
            keepMessage = Text("", 40, 96, 620, 120, 25, sheet.transform);
            keepMessage.alignment = TextAnchor.UpperLeft; keepMessage.color = Soft;
            var revert = Button(Loc.T("settings.revert"), 32, 256, 308, 56, RevertDisplay, false, sheet.transform);
            var keep = Button(Loc.T("settings.keep"), 360, 256, 308, 56, KeepDisplay, true, sheet.transform);
            revert.name = "RevertDisplay"; keep.name = "KeepDisplay";
            foreach (var button in new[] { revert, keep })
            {
                var text = button.GetComponentInChildren<Text>();
                text.alignment = TextAnchor.MiddleCenter; text.rectTransform.anchoredPosition = Vector2.zero; text.rectTransform.sizeDelta = new Vector2(308, 56);
            }
            keepDialog = dim;
            // Orqadagi tanlangan tugma Enter bilan ishlab ketmasin
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            StartCoroutine(KeepCountdown(dim));
        }

        IEnumerator KeepCountdown(RectTransform dialog)
        {
            while (keepDialog == dialog && dialog != null)
            {
                int left = Mathf.CeilToInt(keepUntil - Time.unscaledTime);
                if (left <= 0) { RevertDisplay(); yield break; }
                keepMessage.text = Loc.F("settings.keep_message", left);
                yield return new WaitForSecondsRealtime(.2f);
            }
        }

        void KeepDisplay()
        {
            GameSettings.Save();
            CloseKeepDialog();
        }

        void RevertDisplay()
        {
            if (keepDialog == null) return;
            GameSettings.RestoreDisplay(keepPrevious);
            GameSettings.Apply();
            GameSettings.Save();
            CloseKeepDialog();
            RefreshSettings();
            if (Lobby != null) Lobby.Toast(Loc.T("settings.reverted"));
        }

        void CloseKeepDialog()
        {
            if (keepDialog != null) Destroy(keepDialog.gameObject);
            keepDialog = null; keepMessage = null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CraDev
{
    /// <summary>
    /// O'yin sozlamalari: ekran rejimi va o'lchami, grafika sifati, V-Sync, kadr chegarasi, ovoz balandliklari.
    /// PlayerPrefs'da saqlanadi va o'yin ochilishi bilan (birinchi sahnadan oldin) qo'llanadi.
    /// Birinchi ochilishda: chegarasiz to'liq ekran (monitor o'lchami), loyihadagi standart sifat, V-Sync yoqilgan.
    /// </summary>
    public static class GameSettings
    {
        const string Prefix = "cradev.settings.";
        const int DefaultFrameLimit = 240;

        /// <summary>Kadr chegarasi variantlari (0 - cheklanmagan). V-Sync yoqilganda ishlatilmaydi.</summary>
        public static readonly int[] FrameLimits = { 30, 60, 120, 144, 240, 0 };

        /// <summary>Ekran rejimi: to'liq ekran (eksklyuziv, faqat Windows), chegarasiz oyna yoki oynali.</summary>
        public static FullScreenMode DisplayMode { get; set; } = FullScreenMode.FullScreenWindow;

        /// <summary>Oynali bo'lmagan har qanday rejim. Eski kod uchun: true qilinsa chegarasiz oyna yoqiladi.</summary>
        public static bool Fullscreen
        {
            get => DisplayMode != FullScreenMode.Windowed;
            set { if (value != Fullscreen) DisplayMode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed; }
        }

        static Vector2Int windowedSize, exclusiveSize;

        /// <summary>
        /// Joriy rejim o'lchami: oynali - oyna o'lchami, to'liq ekran - ekran o'lchami, chegarasiz - monitorning o'zi
        /// (chegarasiz rejimda o'rnatib bo'lmaydi). Har rejim o'z o'lchamini alohida eslab qoladi.
        /// </summary>
        public static Vector2Int Resolution
        {
            get => TargetSize(DisplayMode);
            set
            {
                if (DisplayMode == FullScreenMode.Windowed) windowedSize = value;
                else if (DisplayMode == FullScreenMode.ExclusiveFullScreen) exclusiveSize = value;
            }
        }

        public static int Quality { get; set; }
        public static bool VSync { get; set; } = true;
        /// <summary>Kadr chegarasi (FPS), 0 - cheklanmagan. Faqat V-Sync o'chiq bo'lganda ishlaydi.</summary>
        public static int FrameLimit { get; set; } = DefaultFrameLimit;
        /// <summary>Umumiy ovoz, 0..1 (AudioListener.volume).</summary>
        public static float Volume { get; set; } = 1f;
        /// <summary>Interfeys ovozlari (tugma bosilishi, ustiga kelish), 0..1: umumiy ovozga ko'paytiriladi.</summary>
        public static float SfxVolume { get; set; } = 1f;

        /// <summary>Apply() dan keyin: sozlamani ishlatadigan tizimlar (masalan, olam effektlari) yangilanadi.</summary>
        public static event Action Changed;

        /// <summary>Eksklyuziv to'liq ekran faqat Windows'da ishlaydi; boshqa tizimlarda chegarasiz oyna bo'ladi.</summary>
        public static bool ExclusiveSupported =>
            Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;

        /// <summary>Sozlamalarda ko'rsatiladigan ekran rejimlari tartibi.</summary>
        public static FullScreenMode[] DisplayModes => ExclusiveSupported
            ? new[] { FullScreenMode.ExclusiveFullScreen, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed }
            : new[] { FullScreenMode.FullScreenWindow, FullScreenMode.Windowed };

        static int defaultQuality = -1;

        /// <summary>Loyiha (QualitySettings.asset) standart sifat darajasi: "Asliga qaytarish" shunga qaytaradi.</summary>
        public static int DefaultQuality
        {
            get
            {
                if (defaultQuality < 0) defaultQuality = QualitySettings.GetQualityLevel();
                return defaultQuality;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            _ = DefaultQuality; // hali hech narsa o'zgarmagan: loyiha standartini eslab qolamiz
            Load();
            Apply();
        }

        // ------------------------------------------------------------------ O'lchamlar

        /// <summary>Monitorning asl o'lchami (ish stoli).</summary>
        public static Vector2Int NativeResolution()
        {
            var display = Display.main;
            if (display != null && display.systemWidth > 0 && display.systemHeight > 0)
                return new Vector2Int(display.systemWidth, display.systemHeight);
            var r = Screen.currentResolution;
            return new Vector2Int(r.width, r.height);
        }

        /// <summary>Monitor qo'llaydigan o'lchamlar (takrorlanmasdan, kattasidan kichigiga). Eski kod uchun.</summary>
        public static List<Vector2Int> Resolutions() => ResolutionsFor(FullScreenMode.ExclusiveFullScreen);

        /// <summary>
        /// Rejim uchun tanlanadigan o'lchamlar, kattasidan kichigiga. To'liq ekran: monitor rejimlari.
        /// Oynali: sarlavha va vazifalar paneli bilan ekranga sig'adigan o'lchamlar. Chegarasiz: faqat monitor o'lchami.
        /// </summary>
        public static List<Vector2Int> ResolutionsFor(FullScreenMode mode)
        {
            var native = NativeResolution();
            if (mode == FullScreenMode.FullScreenWindow || mode == FullScreenMode.MaximizedWindow)
                return new List<Vector2Int> { native };
            var monitor = Screen.resolutions.Select(r => new Vector2Int(r.width, r.height));
            IEnumerable<Vector2Int> list;
            if (mode == FullScreenMode.Windowed)
            {
                Vector2Int[] common = { new Vector2Int(1280, 720), new Vector2Int(1366, 768), new Vector2Int(1600, 900),
                    new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(3200, 1800), new Vector2Int(3840, 2160) };
                list = monitor.Concat(common).Where(r => r.x >= 960 && r.y >= 540 && FitsWindow(r, native));
            }
            else
            {
                list = monitor.Where(r => r.x >= 1024 && r.y >= 720);
            }
            var result = list.Distinct().OrderByDescending(r => r.x * r.y).ThenByDescending(r => r.x).ToList();
            if (mode == FullScreenMode.ExclusiveFullScreen && !result.Contains(native))
                result.Insert(0, native);
            if (mode == FullScreenMode.Windowed && result.Count == 0)
                result.Add(DefaultWindowSize());
            return result;
        }

        /// <summary>Oyna sarlavhasi va vazifalar paneli bilan ekranga sig'adimi (ish stoli o'lchamiga teng oyna sig'maydi).</summary>
        static bool FitsWindow(Vector2Int size, Vector2Int native) =>
            size.x > 0 && size.y > 0 && size.x <= native.x - 64 && size.y <= native.y - 120;

        /// <summary>Birinchi marta oynali rejim: ish stolining ~85 foiziga sig'adigan eng katta o'lcham (masalan, 1920x1080 da 1600x900).</summary>
        public static Vector2Int DefaultWindowSize()
        {
            var native = NativeResolution();
            Vector2Int[] common = { new Vector2Int(3200, 1800), new Vector2Int(2560, 1440), new Vector2Int(1920, 1080),
                new Vector2Int(1600, 900), new Vector2Int(1366, 768), new Vector2Int(1280, 720) };
            foreach (var size in common)
                if (size.x <= native.x * .85f && size.y <= native.y * .85f)
                    return size;
            return new Vector2Int(Mathf.Max(640, Mathf.RoundToInt(native.x * .8f)), Mathf.Max(360, Mathf.RoundToInt(native.y * .8f)));
        }

        /// <summary>Rejimda haqiqatda qo'llanadigan o'lcham.</summary>
        public static Vector2Int TargetSize(FullScreenMode mode) =>
            TargetSizeOf(new DisplayState { Mode = mode, Windowed = windowedSize, Exclusive = exclusiveSize });

        static Vector2Int TargetSizeOf(DisplayState state)
        {
            var native = NativeResolution();
            switch (Valid(state.Mode))
            {
                case FullScreenMode.ExclusiveFullScreen:
                    return state.Exclusive.x >= 640 && state.Exclusive.y >= 360 ? state.Exclusive : native;
                case FullScreenMode.Windowed:
                    return FitsWindow(state.Windowed, native) ? state.Windowed : DefaultWindowSize();
                default:
                    return native;
            }
        }

        static FullScreenMode Valid(FullScreenMode mode)
        {
            if (mode == FullScreenMode.ExclusiveFullScreen && !ExclusiveSupported) return FullScreenMode.FullScreenWindow;
            if (mode == FullScreenMode.MaximizedWindow) return FullScreenMode.Windowed;
            return Enum.IsDefined(typeof(FullScreenMode), mode) ? mode : FullScreenMode.FullScreenWindow;
        }

        // ------------------------------------------------------------------ Ekran holatini saqlash/qaytarish

        /// <summary>Ekran rejimi va o'lchamlari: o'zgarishdan oldin olinadi, "Saqlansinmi?" rad etilsa qaytariladi.</summary>
        public struct DisplayState
        {
            public FullScreenMode Mode;
            public Vector2Int Windowed, Exclusive;
        }

        public static DisplayState CaptureDisplay() =>
            new DisplayState { Mode = DisplayMode, Windowed = windowedSize, Exclusive = exclusiveSize };

        public static void RestoreDisplay(DisplayState state)
        {
            DisplayMode = Valid(state.Mode);
            windowedSize = state.Windowed;
            exclusiveSize = state.Exclusive;
        }

        /// <summary>Ikki holat ekranda bir xil natija beradimi (rejim va haqiqiy o'lcham).</summary>
        public static bool SameDisplay(DisplayState a, DisplayState b) =>
            Valid(a.Mode) == Valid(b.Mode) && TargetSizeOf(a) == TargetSizeOf(b);

        // ------------------------------------------------------------------ Grafika sifati

        /// <summary>
        /// Har daraja uchun og'ir sozlamalar: soyalar, silliqlash (MSAA), anizotrop filtr, LOD, yumshoq zarrachalar,
        /// real vaqtli aks ettirish. Ish vaqtida SetQualityLevel dan keyin qo'llanadi, shuning uchun builder asset'dagi
        /// qiymatlarni bir xil qilib yozsa ham darajalar baribir farq qiladi. Tekstura o'lchami (Juda past/Past da
        /// yarim) QualitySettings.asset da. Ultra (standart) avvalgi ko'rinishni saqlaydi.
        /// </summary>
        static readonly (int lights, ShadowQuality shadows, ShadowResolution resolution, float distance, int cascades,
            int msaa, AnisotropicFiltering aniso, float lodBias, bool softParticles, bool probes)[] Presets =
        {
            (0, ShadowQuality.HardOnly, ShadowResolution.Low, 30f, 1, 0, AnisotropicFiltering.Disable, .3f, false, false),
            (1, ShadowQuality.HardOnly, ShadowResolution.Medium, 45f, 2, 0, AnisotropicFiltering.Disable, .5f, false, false),
            (2, ShadowQuality.All, ShadowResolution.Medium, 60f, 2, 2, AnisotropicFiltering.Enable, .75f, false, false),
            (2, ShadowQuality.All, ShadowResolution.High, 80f, 4, 2, AnisotropicFiltering.Enable, 1f, true, true),
            (3, ShadowQuality.All, ShadowResolution.VeryHigh, 100f, 4, 4, AnisotropicFiltering.ForceEnable, 1.5f, true, true),
            (4, ShadowQuality.All, ShadowResolution.VeryHigh, 110f, 4, 4, AnisotropicFiltering.ForceEnable, 2f, true, true),
        };

        /// <summary>Joriy (SetQualityLevel qilingan) darajaga shu darajaning sozlamalarini yozadi. Builder ham chaqirishi mumkin.</summary>
        public static void ApplyQualityPreset(int level)
        {
            int count = Mathf.Max(1, QualitySettings.names.Length);
            int index = count == Presets.Length ? level : Mathf.RoundToInt(level * (Presets.Length - 1f) / Mathf.Max(1, count - 1));
            var p = Presets[Mathf.Clamp(index, 0, Presets.Length - 1)];
            QualitySettings.pixelLightCount = p.lights;
            QualitySettings.shadows = p.shadows;
            QualitySettings.shadowResolution = p.resolution;
            QualitySettings.shadowDistance = p.distance;
            QualitySettings.shadowCascades = p.cascades;
            QualitySettings.antiAliasing = p.msaa;
            QualitySettings.anisotropicFiltering = p.aniso;
            QualitySettings.lodBias = p.lodBias;
            QualitySettings.softParticles = p.softParticles;
            QualitySettings.realtimeReflectionProbes = p.probes;
        }

        /// <summary>Sifat darajasi nomi joriy tilda (Juda past … Ultra).</summary>
        public static string QualityName(int level)
        {
            var names = QualitySettings.names;
            if (level < 0 || level >= names.Length) return "";
            string key = "quality." + level;
            return names.Length == 6 && Loc.Has(key) ? Loc.T(key) : names[level];
        }

        // ------------------------------------------------------------------ Saqlash va qo'llash

        public static void Load()
        {
            int mode = PlayerPrefs.GetInt(Prefix + "displaymode", -1);
            if (mode < 0) // eski saqlangan qiymat: faqat "fullscreen" (chegarasiz) yoki oynali
                mode = PlayerPrefs.GetInt(Prefix + "fullscreen", 1) == 1 ? (int)FullScreenMode.FullScreenWindow : (int)FullScreenMode.Windowed;
            DisplayMode = Valid((FullScreenMode)mode);
            var native = NativeResolution();
            windowedSize = new Vector2Int(PlayerPrefs.GetInt(Prefix + "width", 0), PlayerPrefs.GetInt(Prefix + "height", 0));
            // Avvalgi versiyada oyna o'lchami standart bo'yicha butun ish stoli edi: sig'maydigan o'lcham qabul qilinmaydi
            if (!FitsWindow(windowedSize, native)) windowedSize = DefaultWindowSize();
            exclusiveSize = new Vector2Int(PlayerPrefs.GetInt(Prefix + "fs_width", native.x), PlayerPrefs.GetInt(Prefix + "fs_height", native.y));
            if (exclusiveSize.x < 640 || exclusiveSize.y < 360) exclusiveSize = native;
            Quality = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "quality", DefaultQuality), 0, QualitySettings.names.Length - 1);
            VSync = PlayerPrefs.GetInt(Prefix + "vsync", 1) == 1;
            FrameLimit = PlayerPrefs.GetInt(Prefix + "fpslimit", DefaultFrameLimit);
            if (Array.IndexOf(FrameLimits, FrameLimit) < 0) FrameLimit = DefaultFrameLimit;
            Volume = Clamp01(PlayerPrefs.GetFloat(Prefix + "volume", 1f));
            SfxVolume = Clamp01(PlayerPrefs.GetFloat(Prefix + "sfxvolume", 1f));
        }

        public static void Save()
        {
            PlayerPrefs.SetInt(Prefix + "displaymode", (int)DisplayMode);
            PlayerPrefs.SetInt(Prefix + "fullscreen", Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "width", windowedSize.x);
            PlayerPrefs.SetInt(Prefix + "height", windowedSize.y);
            PlayerPrefs.SetInt(Prefix + "fs_width", exclusiveSize.x);
            PlayerPrefs.SetInt(Prefix + "fs_height", exclusiveSize.y);
            PlayerPrefs.SetInt(Prefix + "quality", Quality);
            PlayerPrefs.SetInt(Prefix + "vsync", VSync ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "fpslimit", FrameLimit);
            PlayerPrefs.SetFloat(Prefix + "volume", Volume);
            PlayerPrefs.SetFloat(Prefix + "sfxvolume", SfxVolume);
            PlayerPrefs.Save();
        }

        static int appliedQuality = -1;
        static (FullScreenMode mode, Vector2Int size)? appliedDisplay;

        public static void Apply()
        {
            DisplayMode = Valid(DisplayMode);
#if !UNITY_EDITOR
            // Faqat ekran sozlamasi o'zgarganda: ovoz kabi boshqa sozlama o'zgarganda ekran qayta o'rnatilmaydi (miltillamaydi)
            var display = (DisplayMode, TargetSize(DisplayMode));
            if (appliedDisplay != display)
            {
                Screen.SetResolution(display.Item2.x, display.Item2.y, display.Item1);
                appliedDisplay = display;
            }
#endif
            Quality = Mathf.Clamp(Quality, 0, QualitySettings.names.Length - 1);
            if (Quality != appliedQuality || QualitySettings.GetQualityLevel() != Quality)
            {
                QualitySettings.SetQualityLevel(Quality, true);
                ApplyQualityPreset(Quality);
                appliedQuality = Quality;
            }
            // Sifat darajasi o'z V-Sync qiymatini o'rnatadi: biznikini undan keyin qo'yamiz
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = VSync || FrameLimit <= 0 ? -1 : FrameLimit;
            Volume = Clamp01(Volume);
            SfxVolume = Clamp01(SfxVolume);
            AudioListener.volume = Volume;
            Changed?.Invoke();
        }

        /// <summary>Ekran va grafika standart holatga: chegarasiz to'liq ekran, standart sifat, V-Sync, 240 FPS chegarasi.</summary>
        public static void ResetDisplay()
        {
            DisplayMode = FullScreenMode.FullScreenWindow;
            windowedSize = DefaultWindowSize();
            exclusiveSize = NativeResolution();
            Quality = Mathf.Clamp(DefaultQuality, 0, QualitySettings.names.Length - 1);
            VSync = true;
            FrameLimit = DefaultFrameLimit;
            Apply();
            Save();
        }

        /// <summary>Ovoz balandliklari standart holatga (100%).</summary>
        public static void ResetAudio()
        {
            Volume = 1f;
            SfxVolume = 1f;
            Apply();
            Save();
        }

        static float Clamp01(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp01(value);
    }
}

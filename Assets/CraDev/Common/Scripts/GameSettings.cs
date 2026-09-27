using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CraDev
{
    /// <summary>
    /// O'yin sozlamalari: ekran rejimi, o'lchami, grafika sifati, V-Sync va ovoz balandligi.
    /// PlayerPrefs'da saqlanadi va o'yin ochilishi bilan (birinchi sahnadan oldin) qo'llanadi.
    /// Birinchi ochilishda: to'liq ekran, monitor o'lchami, loyihadagi standart sifat.
    /// </summary>
    public static class GameSettings
    {
        const string Prefix = "cradev.settings.";

        public static bool Fullscreen { get; set; } = true;
        public static Vector2Int Resolution { get; set; }
        public static int Quality { get; set; }
        public static bool VSync { get; set; } = true;
        /// <summary>0..1</summary>
        public static float Volume { get; set; } = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            Load();
            Apply();
        }

        /// <summary>Monitor qo'llaydigan o'lchamlar (takrorlanmasdan, kattasidan kichigiga).</summary>
        public static List<Vector2Int> Resolutions()
        {
            var list = Screen.resolutions
                .Select(r => new Vector2Int(r.width, r.height))
                .Where(r => r.x >= 1024 && r.y >= 720)
                .Distinct()
                .OrderByDescending(r => r.x * r.y)
                .ToList();
            var native = NativeResolution();
            if (!list.Contains(native))
                list.Insert(0, native);
            return list;
        }

        static Vector2Int NativeResolution()
        {
            var r = Screen.currentResolution;
            return new Vector2Int(r.width, r.height);
        }

        public static void Load()
        {
            Fullscreen = PlayerPrefs.GetInt(Prefix + "fullscreen", 1) == 1;
            var native = NativeResolution();
            Resolution = new Vector2Int(PlayerPrefs.GetInt(Prefix + "width", native.x), PlayerPrefs.GetInt(Prefix + "height", native.y));
            Quality = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "quality", QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
            VSync = PlayerPrefs.GetInt(Prefix + "vsync", 1) == 1;
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "volume", 1f));
        }

        public static void Save()
        {
            PlayerPrefs.SetInt(Prefix + "fullscreen", Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "width", Resolution.x);
            PlayerPrefs.SetInt(Prefix + "height", Resolution.y);
            PlayerPrefs.SetInt(Prefix + "quality", Quality);
            PlayerPrefs.SetInt(Prefix + "vsync", VSync ? 1 : 0);
            PlayerPrefs.SetFloat(Prefix + "volume", Volume);
            PlayerPrefs.Save();
        }

        public static void Apply()
        {
#if !UNITY_EDITOR
            // To'liq ekranda monitorning o'z o'lchami (chegarasiz oyna); oynali rejimda tanlangan o'lcham
            var size = Fullscreen ? NativeResolution() : Resolution;
            Screen.SetResolution(size.x, size.y, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
#endif
            QualitySettings.SetQualityLevel(Quality, true);
            // Sifat darajasi o'z V-Sync qiymatini o'rnatadi: biznikini undan keyin qo'yamiz
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = VSync ? -1 : 240;
            AudioListener.volume = Volume;
        }
    }
}

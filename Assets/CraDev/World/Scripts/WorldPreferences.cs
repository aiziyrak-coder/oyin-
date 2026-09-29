using System;
using UnityEngine;

namespace CraDev.World
{
    /// <summary>
    /// Dunyo boshqaruvi sozlamalarining yagona manbai (cradev.world.*): sezgirlik, FOV, Y teskari, kamera tebranishi,
    /// cho'kkalash rejimi va olam AO grafikasi. Lobby sozlamalari ham shu API orqali o'qiydi/yozadi.
    /// Setter faqat xotirani o'zgartiradi; diskka <see cref="Save"/> yozadi.
    /// </summary>
    public static class WorldPreferences
    {
        const string Prefix = "cradev.world.";
        public const float MinSensitivity = .1f, MaxSensitivity = 3f, DefaultSensitivity = 1f;
        public const float MinFieldOfView = 65f, MaxFieldOfView = 100f, DefaultFieldOfView = 80f;

        static bool loaded;
        static float sensitivity = DefaultSensitivity, fieldOfView = DefaultFieldOfView;
        static bool invertY, headBob, toggleCrouch, ambientOcclusion = true;

        /// <summary>Biror qiymat haqiqatan o'zgarganda (setter, Load, Reset) chaqiriladi: ochiq UI'lar o'zini yangilaydi.</summary>
        public static event Action Changed;

        /// <summary>1.00 = <see cref="WorldPlayerController.MouseDegreesPerPixel"/> daraja har piksel siljishga.</summary>
        public static float Sensitivity
        {
            get { EnsureLoaded(); return sensitivity; }
            set { EnsureLoaded(); Assign(ref sensitivity, FiniteClamp(value, MinSensitivity, MaxSensitivity, DefaultSensitivity)); }
        }

        public static float FieldOfView
        {
            get { EnsureLoaded(); return fieldOfView; }
            set { EnsureLoaded(); Assign(ref fieldOfView, FiniteClamp(value, MinFieldOfView, MaxFieldOfView, DefaultFieldOfView)); }
        }

        public static bool InvertY
        {
            get { EnsureLoaded(); return invertY; }
            set { EnsureLoaded(); Assign(ref invertY, value); }
        }

        public static bool HeadBob
        {
            get { EnsureLoaded(); return headBob; }
            set { EnsureLoaded(); Assign(ref headBob, value); }
        }

        public static bool ToggleCrouch
        {
            get { EnsureLoaded(); return toggleCrouch; }
            set { EnsureLoaded(); Assign(ref toggleCrouch, value); }
        }

        /// <summary>Olamdagi yumshoq kontakt soyalari (WorldImageEffects AO). Grafika sozlamasi: <see cref="Reset"/> unga tegmaydi.</summary>
        public static bool AmbientOcclusion
        {
            get { EnsureLoaded(); return ambientOcclusion; }
            set { EnsureLoaded(); Assign(ref ambientOcclusion, value); }
        }

        public static void Load()
        {
            loaded = true;
            sensitivity = FiniteClamp(PlayerPrefs.GetFloat(Prefix + "sensitivity", DefaultSensitivity), MinSensitivity, MaxSensitivity, DefaultSensitivity);
            fieldOfView = FiniteClamp(PlayerPrefs.GetFloat(Prefix + "fov", DefaultFieldOfView), MinFieldOfView, MaxFieldOfView, DefaultFieldOfView);
            invertY = PlayerPrefs.GetInt(Prefix + "invertY", 0) == 1;
            headBob = PlayerPrefs.GetInt(Prefix + "headBob", 0) == 1;
            toggleCrouch = PlayerPrefs.GetInt(Prefix + "toggleCrouch", 0) == 1;
            ambientOcclusion = PlayerPrefs.GetInt(Prefix + "ambientOcclusion", 1) == 1;
            Changed?.Invoke();
        }

        public static void Save()
        {
            EnsureLoaded();
            PlayerPrefs.SetFloat(Prefix + "sensitivity", sensitivity);
            PlayerPrefs.SetFloat(Prefix + "fov", fieldOfView);
            PlayerPrefs.SetInt(Prefix + "invertY", invertY ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "headBob", headBob ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "toggleCrouch", toggleCrouch ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "ambientOcclusion", ambientOcclusion ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Boshqaruvni (sezgirlik, FOV, Y, tebranish, cho'kkalash) asliga qaytaradi va saqlaydi.</summary>
        public static void Reset()
        {
            EnsureLoaded();
            sensitivity = DefaultSensitivity;
            fieldOfView = DefaultFieldOfView;
            invertY = headBob = toggleCrouch = false;
            Save();
            Changed?.Invoke();
        }

        static void EnsureLoaded()
        {
            if (!loaded) Load();
        }

        static void Assign<T>(ref T field, T value)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            Changed?.Invoke();
        }

        static float FiniteClamp(float value, float min, float max, float fallback) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
    }
}

using UnityEngine;

namespace CraDev.World
{
    /// <summary>Dunyo boshqaruvi sozlamalari lobby sozlamalaridan alohida saqlanadi.</summary>
    public static class WorldPreferences
    {
        const string Prefix = "cradev.world.";
        static bool loaded;
        static float sensitivity = 1f, fieldOfView = 80f;
        static bool invertY, headBob, toggleCrouch;

        public static float Sensitivity
        {
            get { EnsureLoaded(); return sensitivity; }
            set { EnsureLoaded(); sensitivity = FiniteClamp(value, .1f, 3f, 1f); }
        }

        public static float FieldOfView
        {
            get { EnsureLoaded(); return fieldOfView; }
            set { EnsureLoaded(); fieldOfView = FiniteClamp(value, 65f, 100f, 80f); }
        }

        public static bool InvertY
        {
            get { EnsureLoaded(); return invertY; }
            set { EnsureLoaded(); invertY = value; }
        }

        public static bool HeadBob
        {
            get { EnsureLoaded(); return headBob; }
            set { EnsureLoaded(); headBob = value; }
        }

        public static bool ToggleCrouch
        {
            get { EnsureLoaded(); return toggleCrouch; }
            set { EnsureLoaded(); toggleCrouch = value; }
        }

        public static void Load()
        {
            loaded = true;
            sensitivity = FiniteClamp(PlayerPrefs.GetFloat(Prefix + "sensitivity", 1f), .1f, 3f, 1f);
            fieldOfView = FiniteClamp(PlayerPrefs.GetFloat(Prefix + "fov", 80f), 65f, 100f, 80f);
            invertY = PlayerPrefs.GetInt(Prefix + "invertY", 0) == 1;
            headBob = PlayerPrefs.GetInt(Prefix + "headBob", 0) == 1;
            toggleCrouch = PlayerPrefs.GetInt(Prefix + "toggleCrouch", 0) == 1;
        }

        public static void Save()
        {
            EnsureLoaded();
            PlayerPrefs.SetFloat(Prefix + "sensitivity", sensitivity);
            PlayerPrefs.SetFloat(Prefix + "fov", fieldOfView);
            PlayerPrefs.SetInt(Prefix + "invertY", invertY ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "headBob", headBob ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "toggleCrouch", toggleCrouch ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            loaded = true;
            sensitivity = 1f;
            fieldOfView = 80f;
            invertY = headBob = toggleCrouch = false;
            Save();
        }

        static void EnsureLoaded()
        {
            if (!loaded) Load();
        }

        static float FiniteClamp(float value, float min, float max, float fallback) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
    }
}

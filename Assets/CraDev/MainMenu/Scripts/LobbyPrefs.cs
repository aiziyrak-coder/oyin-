using UnityEngine;

namespace CraDev.MainMenu
{
    /// <summary>
    /// Faqat shu kompyuterdagi sozlamalar (serverga yuborilmaydi): bildirishnomalar, UI ovozlari va boshqaruv.
    /// Sozlamalar sahifasida o'zgartiriladi.
    /// </summary>
    public static class LobbyPrefs
    {
        const string Prefix = "cradev.lobby.";

        public static bool NotifyFriendRequests
        {
            get => PlayerPrefs.GetInt(Prefix + "notify_friends", 1) != 0;
            set => Set("notify_friends", value);
        }

        public static bool NotifyEvents
        {
            get => PlayerPrefs.GetInt(Prefix + "notify_events", 1) != 0;
            set => Set("notify_events", value);
        }

        public static bool NotifySystem
        {
            get => PlayerPrefs.GetInt(Prefix + "notify_system", 1) != 0;
            set => Set("notify_system", value);
        }

        /// <summary>Sichqoncha sezgirligi (dunyo ichida, birinchi shaxs kamerasi): 1..10.</summary>
        public static int MouseSensitivity
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "mouse", 5), 1, 10);
            set { PlayerPrefs.SetInt(Prefix + "mouse", Mathf.Clamp(value, 1, 10)); PlayerPrefs.Save(); }
        }

        public static bool InvertY
        {
            get => PlayerPrefs.GetInt(Prefix + "invert_y", 0) != 0;
            set => Set("invert_y", value);
        }

        static void Set(string key, bool value)
        {
            PlayerPrefs.SetInt(Prefix + key, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}

using UnityEngine;

namespace CraDev.MainMenu
{
    /// <summary>
    /// Lobbining faqat shu kompyuterdagi sozlamalari (serverga yuborilmaydi): bildirishnomalar.
    /// Sozlamalar sahifasining "Lobbi" bo'limida o'zgartiriladi. Sichqoncha va kamera sozlamalari lobby bilan
    /// olam uchun bitta joyda: <c>CraDev.World.WorldPreferences</c>.
    /// </summary>
    public static class LobbyPrefs
    {
        const string Prefix = "cradev.lobby.";

        /// <summary>Kelgan do'stlik so'rovi haqida xabar (toast, qo'ng'iroqcha belgisi) ko'rsatilsinmi.</summary>
        public static bool NotifyFriendRequests
        {
            get => PlayerPrefs.GetInt(Prefix + "notify_friends", 1) != 0;
            set => Set("notify_friends", value);
        }

        /// <summary>Lobbi bildirishnomalarini standart holatga qaytaradi.</summary>
        public static void Reset() => NotifyFriendRequests = true;

        static void Set(string key, bool value)
        {
            PlayerPrefs.SetInt(Prefix + key, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}

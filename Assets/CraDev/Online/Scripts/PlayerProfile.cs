using UnityEngine;

namespace CraDev.Online
{
    /// <summary>
    /// Server'da yaratilgan o'yinchi profili shu kompyuterda saqlanadi.
    /// Profil bo'lsa, o'yin avatar yaratish ekranini o'tkazib yuboradi.
    /// </summary>
    public static class PlayerProfile
    {
        const string IdKey = "cradev.player.id";
        const string NicknameKey = "cradev.player.nickname";
        const string GenderKey = "cradev.player.gender";
        const string TokenKey = "cradev.player.token";

        public static bool Exists => !string.IsNullOrEmpty(PlayerPrefs.GetString(IdKey, ""));
        public static string Id => PlayerPrefs.GetString(IdKey, "");
        public static string Nickname => PlayerPrefs.GetString(NicknameKey, "");
        public static string Gender => PlayerPrefs.GetString(GenderKey, "");
        /// <summary>Server bergan maxfiy kalit: keyinchalik o'yinchini tanish uchun.</summary>
        public static string Token => PlayerPrefs.GetString(TokenKey, "");

        public static void Save(PlayerResponse player)
        {
            PlayerPrefs.SetString(IdKey, player.id);
            PlayerPrefs.SetString(NicknameKey, player.nickname);
            PlayerPrefs.SetString(GenderKey, player.gender);
            PlayerPrefs.SetString(TokenKey, player.token);
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(IdKey);
            PlayerPrefs.DeleteKey(NicknameKey);
            PlayerPrefs.DeleteKey(GenderKey);
            PlayerPrefs.DeleteKey(TokenKey);
            PlayerPrefs.Save();
        }
    }
}

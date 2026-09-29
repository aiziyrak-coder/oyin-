using UnityEngine;

namespace CraDev.Online
{
    /// <summary>
    /// Server'da yaratilgan o'yinchi profili shu kompyuterda saqlanadi.
    /// Profil bo'lsa, o'yin avatar yaratish ekranini o'tkazib yuboradi. Server - asosiy manba: bosh menyu ochilganda
    /// profil serverdan yangilanadi (avatar, kiyim, mamlakat, maxfiylik). Server o'yinchini tanimasa (yangi baza yoki
    /// boshqa server), <see cref="GameApi.GetMe"/> profilni shu yerdagi ma'lumotlardan serverda qayta yaratadi.
    /// </summary>
    public static class PlayerProfile
    {
        const string IdKey = "cradev.player.id";
        const string NicknameKey = "cradev.player.nickname";
        const string GenderKey = "cradev.player.gender";
        const string AvatarKey = "cradev.player.avatar";
        const string TokenKey = "cradev.player.token";
        const string PublicIdKey = "cradev.player.public_id";
        const string OutfitKey = "cradev.player.outfit";
        const string CountryKey = "cradev.player.country";
        const string ShowOnlineKey = "cradev.player.show_online";
        const string AllowRequestsKey = "cradev.player.allow_requests";
        const string SavedLooksKey = "cradev.player.saved_looks";

        public static bool Exists => !string.IsNullOrEmpty(PlayerPrefs.GetString(IdKey, ""));
        public static string Id => PlayerPrefs.GetString(IdKey, "");
        public static string Nickname => PlayerPrefs.GetString(NicknameKey, "");
        public static string Gender => PlayerPrefs.GetString(GenderKey, "");
        /// <summary>Tanlangan avatar kodi (M1…F5).</summary>
        public static string AvatarId => PlayerPrefs.GetString(AvatarKey, "");
        /// <summary>Server bergan maxfiy kalit: keyinchalik o'yinchini tanish uchun.</summary>
        public static string Token => PlayerPrefs.GetString(TokenKey, "");
        /// <summary>Boshqalarga ko'rinadigan qisqa raqam (ID: #842193). 0 - hali serverdan olinmagan.</summary>
        public static int PublicId => PlayerPrefs.GetInt(PublicIdKey, 0);
        /// <summary>Garderobdagi kiyim (JSON, bo'sh - avatarning asl kiyimi).</summary>
        public static string Outfit => PlayerPrefs.GetString(OutfitKey, "");
        public static string Country => PlayerPrefs.GetString(CountryKey, "UZ");
        public static bool ShowOnline => PlayerPrefs.GetInt(ShowOnlineKey, 1) != 0;
        public static bool AllowRequests => PlayerPrefs.GetInt(AllowRequestsKey, 1) != 0;
        /// <summary>O'yinchi saqlagan obrazlar (garderob, "Saqlanganlar"): JSON ro'yxat.</summary>
        public static string SavedLooks => PlayerPrefs.GetString(SavedLooksKey, "");

        public static void Save(PlayerResponse player)
        {
            PlayerPrefs.SetString(IdKey, player.id);
            PlayerPrefs.SetString(NicknameKey, player.nickname);
            PlayerPrefs.SetString(GenderKey, player.gender);
            PlayerPrefs.SetString(AvatarKey, player.avatarId);
            PlayerPrefs.SetString(TokenKey, player.token);
            Refresh(player);
        }

        /// <summary>Serverdagi profil ma'lumotlari (token o'zgarmaydi).</summary>
        public static void Refresh(PlayerResponse player)
        {
            if (!string.IsNullOrEmpty(player.avatarId))
                PlayerPrefs.SetString(AvatarKey, player.avatarId);
            if (player.publicId > 0)
                PlayerPrefs.SetInt(PublicIdKey, player.publicId);
            if (player.outfit != null)
                PlayerPrefs.SetString(OutfitKey, player.outfit);
            if (!string.IsNullOrEmpty(player.country))
                PlayerPrefs.SetString(CountryKey, player.country);
            PlayerPrefs.SetInt(ShowOnlineKey, player.showOnline ? 1 : 0);
            PlayerPrefs.SetInt(AllowRequestsKey, player.allowRequests ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetAvatar(string avatarId)
        {
            PlayerPrefs.SetString(AvatarKey, avatarId);
            PlayerPrefs.Save();
        }

        public static void SetOutfit(string json)
        {
            PlayerPrefs.SetString(OutfitKey, json ?? "");
            PlayerPrefs.Save();
        }

        public static void SetCountry(string code)
        {
            PlayerPrefs.SetString(CountryKey, code);
            PlayerPrefs.Save();
        }

        public static void SetPrivacy(bool showOnline, bool allowRequests)
        {
            PlayerPrefs.SetInt(ShowOnlineKey, showOnline ? 1 : 0);
            PlayerPrefs.SetInt(AllowRequestsKey, allowRequests ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Bitta maxfiylik bayrog'i (ikkinchisiga tegmaydi). Sozlamalar kaliti uni so'rov yuborilishi bilan yozadi (kalit
        /// darhol almashadi), server rad etsa eski qiymatni qaytaradi: <see cref="GameApi.UpdateShowOnline"/>.
        /// </summary>
        public static void SetShowOnline(bool value)
        {
            PlayerPrefs.SetInt(ShowOnlineKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <inheritdoc cref="SetShowOnline"/>
        public static void SetAllowRequests(bool value)
        {
            PlayerPrefs.SetInt(AllowRequestsKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetSavedLooks(string json)
        {
            PlayerPrefs.SetString(SavedLooksKey, json ?? "");
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            foreach (var key in new[] { IdKey, NicknameKey, GenderKey, AvatarKey, TokenKey, PublicIdKey, OutfitKey, CountryKey, ShowOnlineKey, AllowRequestsKey, SavedLooksKey })
                PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}

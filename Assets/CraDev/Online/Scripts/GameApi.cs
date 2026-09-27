using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace CraDev.Online
{
    /// <summary>
    /// O'yin serveri bilan ishlash (Server/ papkasidagi Node.js server).
    /// Metodlar korutina: <c>StartCoroutine(api.CheckNickname(...))</c>.
    /// </summary>
    public class GameApi
    {
        /// <summary>O'yin serveri (Server/ papkasi). Haqiqiy serverga o'tganda shu yerda o'zgartiriladi.</summary>
        public const string DefaultServerUrl = "http://localhost:8080";

        const int TimeoutSeconds = 8;
        readonly string baseUrl;

        public GameApi(string baseUrl) => this.baseUrl = baseUrl.TrimEnd('/');

        /// <summary>Nickname bo'shmi? Tarmoq xatosida result.NetworkError = true.</summary>
        public IEnumerator CheckNickname(string nickname, Action<ApiResult<AvailabilityResponse>> done) =>
            Send("GET", "/api/nicknames/availability?name=" + UnityWebRequest.EscapeURL(nickname), null, null, done);

        /// <summary>O'yinchini yaratadi va nickname'ni band qiladi. 409 = nickname allaqachon olingan.</summary>
        public IEnumerator CreatePlayer(string nickname, string gender, string avatarId, Action<ApiResult<PlayerResponse>> done) =>
            Send("POST", "/api/players", null, JsonUtility.ToJson(new CreatePlayerRequest { nickname = nickname, gender = gender, avatarId = avatarId }), done);

        /// <summary>Saqlangan token bo'yicha o'yinchi profili. 401 = serverda bunday o'yinchi yo'q.</summary>
        public IEnumerator GetMe(string token, Action<ApiResult<PlayerResponse>> done) => Send("GET", "/api/players/me", token, null, done);

        /// <summary>O'yinchi avatarini o'zgartiradi (jins o'zgarmaydi).</summary>
        public IEnumerator UpdateAvatar(string token, string avatarId, Action<ApiResult<PlayerResponse>> done) =>
            Send("PATCH", "/api/players/me", token, JsonUtility.ToJson(new UpdateAvatarRequest { avatarId = avatarId }), done);

        /// <summary>Kiyimni saqlaydi (JSON, bo'sh - asl kiyim). Boshqa o'yinchilar ham shu kiyimda ko'radi.</summary>
        public IEnumerator UpdateOutfit(string token, string outfitJson, Action<ApiResult<PlayerResponse>> done) =>
            Send("PATCH", "/api/players/me", token, JsonUtility.ToJson(new UpdateOutfitRequest { outfit = outfitJson ?? "" }), done);

        /// <summary>Mamlakat (ISO kodi, masalan "UZ").</summary>
        public IEnumerator UpdateCountry(string token, string country, Action<ApiResult<PlayerResponse>> done) =>
            Send("PATCH", "/api/players/me", token, JsonUtility.ToJson(new UpdateCountryRequest { country = country }), done);

        /// <summary>Maxfiylik: onlayn holatini ko'rsatish va do'stlik so'rovlarini qabul qilish.</summary>
        public IEnumerator UpdatePrivacy(string token, bool showOnline, bool allowRequests, Action<ApiResult<PlayerResponse>> done) =>
            Send("PATCH", "/api/players/me", token, JsonUtility.ToJson(new UpdatePrivacyRequest { showOnline = showOnline, allowRequests = allowRequests }), done);

        /// <summary>"Men o'yindaman" (har 30 soniyada): onlayn vaqt hisoblanadi, javobda hozir onlayn o'yinchilar soni.</summary>
        public IEnumerator Presence(string token, Action<ApiResult<StatsResponse>> done) => Send("POST", "/api/presence", token, "{}", done);

        /// <summary>Hozir onlayn va jami o'yinchilar.</summary>
        public IEnumerator Stats(Action<ApiResult<StatsResponse>> done) => Send("GET", "/api/stats", null, null, done);

        public IEnumerator SearchPlayers(string token, string query, Action<ApiResult<PlayerList>> done) =>
            SendList("/api/players/search?q=" + UnityWebRequest.EscapeURL(query), token, done);

        public IEnumerator SuggestedPlayers(string token, Action<ApiResult<PlayerList>> done) => SendList("/api/players/suggested", token, done);

        public IEnumerator Friends(string token, Action<ApiResult<FriendsResponse>> done) => Send("GET", "/api/friends", token, null, done);

        public IEnumerator RequestFriend(string token, string nickname, Action<ApiResult<FriendshipResponse>> done) =>
            Send("POST", "/api/friends/request", token, JsonUtility.ToJson(new NicknameRequest { nickname = nickname }), done);

        public IEnumerator AcceptFriend(string token, string nickname, Action<ApiResult<FriendshipResponse>> done) =>
            Send("POST", "/api/friends/accept", token, JsonUtility.ToJson(new NicknameRequest { nickname = nickname }), done);

        public IEnumerator RemoveFriend(string token, string nickname, Action<ApiResult<FriendshipResponse>> done) =>
            Send("POST", "/api/friends/remove", token, JsonUtility.ToJson(new NicknameRequest { nickname = nickname }), done);

        public IEnumerator Leaderboard(int limit, Action<ApiResult<LeaderboardList>> done) => SendList("/api/leaderboard?limit=" + limit, null, done);

        public IEnumerator Events(Action<ApiResult<EventList>> done) => SendList("/api/events", null, done);

        public IEnumerator Party(string token, string action, PartyRequest body, Action<ApiResult<PartyState>> done) =>
            Send("POST", "/api/party/" + action, token, JsonUtility.ToJson(body), done);

        /// <summary>
        /// Server ro'yxatni JSON massiv qilib qaytaradi, JsonUtility esa faqat obyektni o'qiydi: javob {"items":[...]}
        /// ga o'raladi.
        /// </summary>
        IEnumerator SendList<T>(string path, string token, Action<ApiResult<T>> done) where T : class
        {
            yield return Send<T>("GET", path, token, null, done, wrapArray: true);
        }

        IEnumerator Send<T>(string method, string path, string token, string json, Action<ApiResult<T>> done, bool wrapArray = false) where T : class
        {
            using (var request = new UnityWebRequest(baseUrl + path, method))
            {
                if (json != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                request.downloadHandler = new DownloadHandlerBuffer();
                if (!string.IsNullOrEmpty(token))
                    request.SetRequestHeader("Authorization", "Bearer " + token);
                request.timeout = TimeoutSeconds;
                yield return request.SendWebRequest();
                done(ApiResult<T>.From(request, wrapArray));
            }
        }
    }

    public struct ApiResult<T> where T : class
    {
        /// <summary>Server bilan umuman aloqa bo'lmadi (server o'chiq, internet yo'q, vaqt tugadi).</summary>
        public bool NetworkError;
        public long Status;
        public T Data;

        public bool Ok => !NetworkError && Status >= 200 && Status < 300 && Data != null;

        public static ApiResult<T> From(UnityWebRequest request, bool wrapArray = false)
        {
            var result = new ApiResult<T> { Status = request.responseCode };
            if (request.result == UnityWebRequest.Result.ConnectionError || request.responseCode == 0)
            {
                result.NetworkError = true;
                return result;
            }
            string text = request.downloadHandler != null ? request.downloadHandler.text : null;
            if (!string.IsNullOrEmpty(text))
            {
                if (wrapArray && text.TrimStart().StartsWith("["))
                    text = "{\"items\":" + text + "}";
                try { result.Data = JsonUtility.FromJson<T>(text); }
                catch (ArgumentException) { result.Data = null; }
            }
            return result;
        }
    }

    [Serializable] public class PartyRequest { public string nickname, invitationId; public int pingMs = -1; }
    [Serializable] public class PartyMember { public string nickname, avatarId, gender, outfit; public int seat, pingMs; public bool online; }
    [Serializable] public class PartyInvitation { public string id, nickname; }
    [Serializable] public class PartyState { public string roomId, error; public bool host; public PartyMember[] members; public PartyInvitation[] invitations; }

    [Serializable]
    public class AvailabilityResponse
    {
        public string name;
        public bool available;
        public string reason;
        public string message;
    }

    [Serializable]
    public class PlayerResponse
    {
        public string id;
        public int publicId;
        public string nickname;
        public string gender;
        public string avatarId;
        public string outfit;
        public string country;
        public bool showOnline = true;
        public bool allowRequests = true;
        public int onlineSeconds;
        public string token;
        public string createdAt;
        public string error;
        public string reason;
        public string message;
    }

    [Serializable]
    public class StatsResponse
    {
        public int online;
        public int players;
    }

    /// <summary>Boshqa o'yinchi (ijtimoiy ro'yxatlarda): shaxsiy ma'lumotsiz.</summary>
    [Serializable]
    public class PlayerSummary
    {
        public string nickname;
        public string avatarId;
        public string gender;
        public bool online;
        /// <summary>"none", "friends", "outgoing" (men so'rov yuborganman), "incoming" (menga so'rov kelgan).</summary>
        public string friendship;
    }

    [Serializable]
    public class PlayerList
    {
        public PlayerSummary[] items;
    }

    [Serializable]
    public class FriendsResponse
    {
        public PlayerSummary[] friends;
        public PlayerSummary[] incoming;
        public PlayerSummary[] outgoing;
    }

    [Serializable]
    public class FriendshipResponse
    {
        public string friendship;
        public string error;
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string nickname;
        public string avatarId;
        public string gender;
        public int minutes;
    }

    [Serializable]
    public class LeaderboardList
    {
        public LeaderboardEntry[] items;
    }

    [Serializable]
    public class LocalizedString
    {
        public string uz;
        public string en;

        public string Value => Loc.Current == Language.En ? en : uz;
    }

    [Serializable]
    public class GameEvent
    {
        public string id;
        public string zone;
        public LocalizedString title;
        public LocalizedString place;
        public string startsAt;
        public string endsAt;

        /// <summary>Boshlanish vaqti (kompyuterning mahalliy vaqtida).</summary>
        public DateTime StartsLocal => DateTime.TryParse(startsAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var t) ? t.ToLocalTime() : DateTime.MinValue;
    }

    [Serializable]
    public class EventList
    {
        public GameEvent[] items;
    }

    [Serializable]
    class UpdateAvatarRequest
    {
        public string avatarId;
    }

    [Serializable]
    class UpdateOutfitRequest
    {
        public string outfit;
    }

    [Serializable]
    class UpdateCountryRequest
    {
        public string country;
    }

    [Serializable]
    class UpdatePrivacyRequest
    {
        public bool showOnline;
        public bool allowRequests;
    }

    [Serializable]
    class NicknameRequest
    {
        public string nickname;
    }

    [Serializable]
    class CreatePlayerRequest
    {
        public string nickname;
        public string gender;
        public string avatarId;
    }
}

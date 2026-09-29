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
        /// <summary>
        /// Sahnalardagi standart manzil: o'yin yonidagi server (Server/ papkasi). Umumiy serverga o'yinni qayta yig'masdan
        /// ulanish mumkin: buyruq qatori, CRADEV_SERVER yoki server.txt (<see cref="ServerAddress"/>).
        /// </summary>
        public const string DefaultServerUrl = "http://localhost:8080";

        /// <summary>Reyting: server ko'pi bilan shuncha qator qaytaradi (Server/src/app.js LEADERBOARD_MAX).</summary>
        public const int LeaderboardMax = 50;

        const int TimeoutSeconds = 8;
        readonly string baseUrl;

        /// <param name="configuredUrl">Sahnadagi manzil (builder yozadi); tashqi sozlama bo'lsa u ishlatiladi.</param>
        public GameApi(string configuredUrl) => baseUrl = ServerAddress.Resolve(configuredUrl);

        /// <summary>Haqiqatda ishlatilayotgan server manzili.</summary>
        public string BaseUrl => baseUrl;

        /// <summary>
        /// Nickname bo'shmi? Javob tanasi faqat 200 da o'qiladi: 429 (so'rovlar juda ko'p) yoki 5xx da result.Data = null,
        /// result.Status esa saqlanadi. Shunda ular "band" yoki "noto'g'ri" deb emas, qayta urinish kerak bo'lgan holat
        /// sifatida ko'rinadi (<see cref="RegistrationSession.Classify"/>). Tarmoq xatosida result.NetworkError = true.
        /// </summary>
        public IEnumerator CheckNickname(string nickname, Action<ApiResult<AvailabilityResponse>> done) =>
            Send<AvailabilityResponse>("GET", "/api/nicknames/availability?name=" + UnityWebRequest.EscapeURL(nickname), null, null, result =>
            {
                if (result.Status != 200)
                    result.Data = null;
                done(result);
            });

        /// <summary>O'yinchini yaratadi va nickname'ni band qiladi. 409 = nickname allaqachon olingan.</summary>
        public IEnumerator CreatePlayer(string nickname, string gender, string avatarId, Action<ApiResult<PlayerResponse>> done) =>
            Send("POST", "/api/players", null, JsonUtility.ToJson(new CreatePlayerRequest { nickname = nickname, gender = gender, avatarId = avatarId }), done);

        /// <summary>
        /// Saqlangan token bo'yicha o'yinchi profili. Server bu o'yinchini tanimasa (baza yangidan boshlangan, boshqa
        /// server), shu kompyuterdagi profil serverda qayta yaratiladi (POST /api/players/restore) va profil qaytadi:
        /// o'yinchi qahramonini yo'qotmaydi. 401 faqat tiklab bo'lmaganda (masalan nickname bu serverda boshqa o'yinchida).
        /// Tiklash paytida tarmoq xatosi, 429 yoki 5xx bo'lsa o'sha natija qaytadi: chaqiruvchi keyinroq qayta urinadi,
        /// profil o'chirilmaydi.
        /// </summary>
        public IEnumerator GetMe(string token, Action<ApiResult<PlayerResponse>> done)
        {
            ApiResult<PlayerResponse> result = default;
            yield return Send<PlayerResponse>("GET", "/api/players/me", token, null, r => result = r);
            string restore = !result.NetworkError && result.Status == 401 ? RestoreJson(token) : null;
            if (restore != null)
            {
                ApiResult<PlayerResponse> restored = default;
                yield return Send<PlayerResponse>("POST", "/api/players/restore", null, restore, r => restored = r);
                if (!restored.NetworkError && (restored.Status == 200 || restored.Status == 201))
                {
                    Debug.Log("[CraDev] Profil serverda yo'q edi: shu kompyuterdagi nusxadan tiklandi (" + baseUrl + ")");
                    yield return Send<PlayerResponse>("GET", "/api/players/me", token, null, r => result = r);
                }
                else if (restored.NetworkError || restored.Status == 429 || restored.Status >= 500)
                    result = restored;
                else
                    Debug.LogWarning($"[CraDev] Profilni serverda tiklab bo'lmadi: {restored.Status} {restored.Data?.error}");
            }
            done(result);
        }

        /// <summary>Tiklash so'rovi: faqat token shu kompyuterdagi profilniki va profil to'liq bo'lsa (aks holda null).</summary>
        static string RestoreJson(string token)
        {
            if (string.IsNullOrEmpty(token) || token != PlayerProfile.Token || !PlayerProfile.Exists
                || string.IsNullOrEmpty(PlayerProfile.Nickname) || string.IsNullOrEmpty(PlayerProfile.Gender))
                return null;
            return JsonUtility.ToJson(new RestorePlayerRequest
            {
                id = PlayerProfile.Id,
                token = token,
                nickname = PlayerProfile.Nickname,
                gender = PlayerProfile.Gender,
                avatarId = PlayerProfile.AvatarId,
                outfit = PlayerProfile.Outfit,
                country = PlayerProfile.Country,
                showOnline = PlayerProfile.ShowOnline,
                allowRequests = PlayerProfile.AllowRequests,
            });
        }

        /// <summary>O'yinchi avatarini o'zgartiradi (jins o'zgarmaydi).</summary>
        public IEnumerator UpdateAvatar(string token, string avatarId, Action<ApiResult<PlayerResponse>> done) =>
            Send("PATCH", "/api/players/me", token, JsonUtility.ToJson(new UpdateAvatarRequest { avatarId = avatarId }), done);

        /// <summary>Kiyimni saqlaydi (JSON, bo'sh - asl kiyim). Boshqa o'yinchilar ham shu kiyimda ko'radi.</summary>
        public IEnumerator UpdateOutfit(string token, string outfitJson, Action<ApiResult<PlayerResponse>> done) =>
            Send("PATCH", "/api/players/me", token, JsonUtility.ToJson(new UpdateOutfitRequest { outfit = outfitJson ?? "" }), done);

        /// <summary>Mamlakat (ISO kodi, masalan "UZ").</summary>
        public IEnumerator UpdateCountry(string token, string country, Action<ApiResult<PlayerResponse>> done) =>
            Send("PATCH", "/api/players/me", token, JsonUtility.ToJson(new UpdateCountryRequest { country = country }), done);

        /// <summary>
        /// Maxfiylik: ikkala bayroq birga. Bittasi o'zgarganda <see cref="UpdateShowOnline"/> yoki
        /// <see cref="UpdateAllowRequests"/> ishlating: ikki kalit ketma-ket bosilsa, eski qiymat ikkinchisini bekor qilmaydi.
        /// </summary>
        public IEnumerator UpdatePrivacy(string token, bool showOnline, bool allowRequests, Action<ApiResult<PlayerResponse>> done) =>
            Send("PATCH", "/api/players/me", token, JsonUtility.ToJson(new UpdatePrivacyRequest { showOnline = showOnline, allowRequests = allowRequests }), done);

        /// <summary>Faqat "onlayn holatimni ko'rsatish" (allowRequests serverda o'zgarmaydi).</summary>
        public IEnumerator UpdateShowOnline(string token, bool showOnline, Action<ApiResult<PlayerResponse>> done) =>
            Send("PATCH", "/api/players/me", token, BoolJson("showOnline", showOnline), done);

        /// <summary>Faqat "do'stlik so'rovlarini qabul qilish" (showOnline serverda o'zgarmaydi).</summary>
        public IEnumerator UpdateAllowRequests(string token, bool allowRequests, Action<ApiResult<PlayerResponse>> done) =>
            Send("PATCH", "/api/players/me", token, BoolJson("allowRequests", allowRequests), done);

        // JsonUtility bool maydonni tashlab keta olmaydi: bitta maydonli tana qo'lda yoziladi
        static string BoolJson(string field, bool value) => "{\"" + field + "\":" + (value ? "true" : "false") + "}";

        /// <summary>"Men o'yindaman" (har 30 soniyada): onlayn vaqt hisoblanadi, javobda hozir onlayn o'yinchilar soni.</summary>
        public IEnumerator Presence(string token, Action<ApiResult<StatsResponse>> done) => Send("POST", "/api/presence", token, "{}", done);

        /// <summary>Hozir onlayn va jami o'yinchilar.</summary>
        public IEnumerator Stats(Action<ApiResult<StatsResponse>> done) => Send("GET", "/api/stats", null, null, done);

        public IEnumerator SearchPlayers(string token, string query, Action<ApiResult<PlayerList>> done) =>
            SendList("/api/players/search?q=" + UnityWebRequest.EscapeURL(query), token, done);

        /// <summary>
        /// Ping: bazaga tegmaydigan GET /api/ping ning to'liq aylanish vaqti (ms). Boshqa so'rovlar bilan bitta navbatda
        /// emas: chaqiruvchi uni alohida korutinada yuboradi. done(ok, ms): tarmoq xatosida ok = false.
        /// Aniqlik kadr chastotasiga bog'liq (javob keyingi kadrda o'qiladi): 60 FPS da +0..16 ms.
        /// </summary>
        public IEnumerator Ping(Action<bool, int> done)
        {
            using (var request = UnityWebRequest.Get(baseUrl + "/api/ping?_=" + DateTime.UtcNow.Ticks))
            {
                request.timeout = 5;
                request.SetRequestHeader("Cache-Control", "no-cache");
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var operation = request.SendWebRequest();
                // completed kadr oxirini kutmasdan chaqiriladi (asosiy oqimda, so'rov tugagani aniqlangan zahoti)
                long elapsed = -1;
                operation.completed += _ => elapsed = watch.ElapsedMilliseconds;
                yield return operation;
                if (elapsed < 0) elapsed = watch.ElapsedMilliseconds;
                bool ok = request.result == UnityWebRequest.Result.Success && request.responseCode == 200;
                done(ok, (int)Math.Min(elapsed, 10000));
            }
        }

        // ---------------- Guruhlar (Server/src/groups.js)

        public IEnumerator GroupsMine(string token, Action<ApiResult<GroupList>> done) => Send("GET", "/api/groups/mine", token, null, done);

        public IEnumerator GroupSearch(string token, string query, Action<ApiResult<GroupList>> done) =>
            SendList("/api/groups/search?q=" + UnityWebRequest.EscapeURL(query), token, done);

        public IEnumerator GroupDetail(string token, string code, Action<ApiResult<GroupInfo>> done) =>
            Send("GET", "/api/groups/detail?code=" + UnityWebRequest.EscapeURL(code ?? ""), token, null, done);

        public IEnumerator GroupCreate(string token, GroupCreateRequest body, Action<ApiResult<GroupInfo>> done) =>
            Send("POST", "/api/groups/create", token, JsonUtility.ToJson(body), done);

        public IEnumerator GroupJoin(string token, string code, Action<ApiResult<GroupJoinResponse>> done) =>
            Send("POST", "/api/groups/join", token, JsonUtility.ToJson(new GroupActionRequest { code = code }), done);

        /// <summary>cancel, leave, approve, reject, remove, role, delete: javob - yangilangan guruh (leave/delete da { ok }).</summary>
        public IEnumerator GroupAction(string token, string action, string code, string nickname, string role, Action<ApiResult<GroupInfo>> done) =>
            Send("POST", "/api/groups/" + action, token, JsonUtility.ToJson(new GroupActionRequest { code = code, nickname = nickname, role = role }), done);

        public IEnumerator Friends(string token, Action<ApiResult<FriendsResponse>> done) => Send("GET", "/api/friends", token, null, done);

        public IEnumerator RequestFriend(string token, string nickname, Action<ApiResult<FriendshipResponse>> done) =>
            Send("POST", "/api/friends/request", token, JsonUtility.ToJson(new NicknameRequest { nickname = nickname }), done);

        public IEnumerator AcceptFriend(string token, string nickname, Action<ApiResult<FriendshipResponse>> done) =>
            Send("POST", "/api/friends/accept", token, JsonUtility.ToJson(new NicknameRequest { nickname = nickname }), done);

        public IEnumerator RemoveFriend(string token, string nickname, Action<ApiResult<FriendshipResponse>> done) =>
            Send("POST", "/api/friends/remove", token, JsonUtility.ToJson(new NicknameRequest { nickname = nickname }), done);

        public IEnumerator Leaderboard(int limit, Action<ApiResult<LeaderboardList>> done) =>
            SendList("/api/leaderboard?limit=" + Mathf.Clamp(limit, 1, LeaderboardMax), null, done);

        public IEnumerator Events(Action<ApiResult<EventList>> done) => SendList("/api/events", null, done);

        public IEnumerator Party(string token, string action, PartyRequest body, Action<ApiResult<PartyState>> done) =>
            Send("POST", "/api/party/" + action, token, JsonUtility.ToJson(body), done);

        /// <summary>Ovoz bo'lagini guruhga yuborish (8-bit mu-law, base64).</summary>
        public IEnumerator VoiceSend(string token, VoiceChunk chunk, Action<ApiResult<VoiceCursor>> done) =>
            Send("POST", "/api/party/voice", token, JsonUtility.ToJson(chunk), done);

        /// <summary>Guruhdagi boshqalarning <paramref name="since"/> dan keyingi ovoz bo'laklari.</summary>
        public IEnumerator VoicePoll(string token, int since, Action<ApiResult<VoiceBatch>> done) =>
            Send("GET", "/api/party/voice?since=" + since, token, null, done);

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

    [Serializable] public class PartyRequest { public string nickname, invitationId; public int pingMs = -1; public bool micOn, speakerOn; }
    [Serializable] public class PartyMember { public string nickname, avatarId, gender, outfit; public int seat, pingMs; public bool online, micOn, speakerOn; }
    [Serializable] public class VoiceChunk { public int seq, rate; public string nickname, data; }
    [Serializable] public class VoiceCursor { public int cursor; public string error; }
    [Serializable] public class VoiceBatch { public int cursor; public VoiceChunk[] chunks; }
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
        public int publicId;
        public string avatarId;
        public string gender;
        public bool online;
        /// <summary>"none", "friends", "outgoing" (men so'rov yuborganman), "incoming" (menga so'rov kelgan).</summary>
        public string friendship;
    }

    [Serializable]
    public class GroupMember
    {
        public string nickname, avatarId, gender, role, expiresAt;
        /// <summary>"none", "friends", "outgoing", "incoming" yoki "self" (o'zim).</summary>
        public string friendship;
        public int publicId;
        public bool online;
    }

    /// <summary>Guruh: ro'yxatda qisqa, batafsil ko'rinishda a'zolar (faqat a'zoga) va so'rovlar (faqat admin/egaga) bilan.</summary>
    [Serializable]
    public class GroupInfo
    {
        public string code, name, description, owner, createdAt, error;
        /// <summary>"free", "private", "paid".</summary>
        public string kind;
        public int price;
        public string currency, period;
        /// <summary>"owner", "admin", "member" yoki "" (a'zo emas).</summary>
        public string role;
        public string expiresAt;
        public int memberCount, onlineCount, pending;
        public bool requested, ok;
        public GroupMember[] members, requests;
        public bool IsMember => !string.IsNullOrEmpty(role);
        public bool IsAdmin => role == "owner" || role == "admin";
    }

    [Serializable]
    public class GroupList
    {
        public GroupInfo[] groups, requests, items;
        public string error;
    }

    [Serializable]
    public class GroupJoinResponse
    {
        public string status, error;
        public GroupInfo group;
    }

    [Serializable]
    public class GroupCreateRequest
    {
        public string name, description, kind, currency = "UZS", period = "month";
        public int price;
    }

    [Serializable]
    class GroupActionRequest
    {
        public string code, nickname, role;
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
        public DateTime StartsLocal => Local(startsAt);

        /// <summary>Tugash vaqti (mahalliy). Server bermasa - boshlanish vaqti.</summary>
        public DateTime EndsLocal { get { var end = Local(endsAt); return end == DateTime.MinValue ? StartsLocal : end; } }

        static DateTime Local(string iso) =>
            DateTime.TryParse(iso, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var t) ? t.ToLocalTime() : DateTime.MinValue;
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
    class RestorePlayerRequest
    {
        public string id, token, nickname, gender, avatarId, outfit, country;
        public bool showOnline, allowRequests;
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

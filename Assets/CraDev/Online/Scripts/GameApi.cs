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
        public IEnumerator CheckNickname(string nickname, Action<ApiResult<AvailabilityResponse>> done)
        {
            string url = $"{baseUrl}/api/nicknames/availability?name={UnityWebRequest.EscapeURL(nickname)}";
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = TimeoutSeconds;
                yield return request.SendWebRequest();
                done(ApiResult<AvailabilityResponse>.From(request));
            }
        }

        /// <summary>O'yinchini yaratadi va nickname'ni band qiladi. 409 = nickname allaqachon olingan.</summary>
        public IEnumerator CreatePlayer(string nickname, string gender, string avatarId, Action<ApiResult<PlayerResponse>> done)
        {
            string json = JsonUtility.ToJson(new CreatePlayerRequest { nickname = nickname, gender = gender, avatarId = avatarId });
            using (var request = new UnityWebRequest($"{baseUrl}/api/players", UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = TimeoutSeconds;
                yield return request.SendWebRequest();
                done(ApiResult<PlayerResponse>.From(request));
            }
        }

        /// <summary>Saqlangan token bo'yicha o'yinchi profili. 401 = serverda bunday o'yinchi yo'q.</summary>
        public IEnumerator GetMe(string token, Action<ApiResult<PlayerResponse>> done)
        {
            using (var request = UnityWebRequest.Get($"{baseUrl}/api/players/me"))
            {
                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.timeout = TimeoutSeconds;
                yield return request.SendWebRequest();
                done(ApiResult<PlayerResponse>.From(request));
            }
        }

        /// <summary>O'yinchi avatarini o'zgartiradi (jins o'zgarmaydi).</summary>
        public IEnumerator UpdateAvatar(string token, string avatarId, Action<ApiResult<PlayerResponse>> done)
        {
            string json = JsonUtility.ToJson(new UpdateAvatarRequest { avatarId = avatarId });
            using (var request = new UnityWebRequest($"{baseUrl}/api/players/me", "PATCH"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.timeout = TimeoutSeconds;
                yield return request.SendWebRequest();
                done(ApiResult<PlayerResponse>.From(request));
            }
        }
    }

    public struct ApiResult<T> where T : class
    {
        /// <summary>Server bilan umuman aloqa bo'lmadi (server o'chiq, internet yo'q, vaqt tugadi).</summary>
        public bool NetworkError;
        public long Status;
        public T Data;

        public static ApiResult<T> From(UnityWebRequest request)
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
                try { result.Data = JsonUtility.FromJson<T>(text); }
                catch (ArgumentException) { result.Data = null; }
            }
            return result;
        }
    }

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
        public string nickname;
        public string gender;
        public string avatarId;
        public string token;
        public string createdAt;
        public string error;
        public string reason;
        public string message;
    }

    [Serializable]
    class UpdateAvatarRequest
    {
        public string avatarId;
    }

    [Serializable]
    class CreatePlayerRequest
    {
        public string nickname;
        public string gender;
        public string avatarId;
    }
}

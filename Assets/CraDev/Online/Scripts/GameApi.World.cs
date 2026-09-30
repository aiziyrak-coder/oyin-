using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace CraDev.Online
{
    public partial class GameApi
    {
        public IEnumerator WorldJoin(string token, Action<ApiResult<WorldSnapshot>> done) =>
            Send("POST", "/api/world/join", token, "{}", done);
        public IEnumerator WorldState(string token, WorldStateRequest state, Action<ApiResult<WorldSnapshot>> done) =>
            Send("POST", "/api/world/state", token, JsonUtility.ToJson(state), done);
        public IEnumerator WorldLeave(string token, string sessionId, Action<ApiResult<WorldReply>> done) =>
            Send("POST", "/api/world/leave", token, JsonUtility.ToJson(new WorldSession { sessionId = sessionId }), done);
        public IEnumerator WorldSendVoice(string token, WorldAudio body, Action<ApiResult<WorldReply>> done) =>
            Send("POST", "/api/world/voice", token, JsonUtility.ToJson(body), done);
        public IEnumerator WorldReadVoice(string token, string sessionId, long since, Action<ApiResult<WorldAudioBatch>> done) =>
            Send("GET", "/api/world/voice?sessionId=" + UnityWebRequest.EscapeURL(sessionId ?? "") + "&since=" + since, token, null, done);
    }
    [Serializable] public class WorldSession { public string sessionId; }
    [Serializable] public sealed class WorldStateRequest : WorldSession
    {
        public float x, y, z, yaw;
        public bool crouching, micOn, speakerOn;
    }
    [Serializable] public sealed class WorldPeer
    {
        public int publicId;
        public string nickname, avatarId, gender, outfit;
        public float x, y, z, yaw;
        public bool crouching, micOn, speakerOn;
        public Vector3 Position => new Vector3(x, y, z);
    }
    [Serializable] public sealed class WorldSnapshot
    {
        public string sessionId, error;
        public WorldPeer self;
        public WorldPeer[] players;
        public long serverTime;
        public bool corrected;
    }
    [Serializable] public sealed class WorldReply { public bool ok; public string error; public long cursor; }
    [Serializable] public sealed class WorldAudio : WorldSession
    {
        public long seq;
        public int rate, publicId;
        public string data, nickname;
        public float x, y, z;
    }
    [Serializable] public sealed class WorldAudioBatch
    {
        public string error;
        public long cursor;
        public WorldAudio[] chunks;
    }
}

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace CraDev.Online
{
    public partial class GameApi
    {
        public IEnumerator PenaltyState(string token, string sessionId, Action<ApiResult<PenaltySnapshot>> done) =>
            Send<PenaltySnapshot>("GET", "/api/penalty/state?sessionId=" + UnityWebRequest.EscapeURL(sessionId ?? ""), token, null,
                result => { result.Data?.Normalize(); done(result); });
        public IEnumerator PenaltyAction(string token, string action, PenaltyRequest body, Action<ApiResult<PenaltySnapshot>> done) =>
            Send<PenaltySnapshot>("POST", "/api/penalty/" + action, token, JsonUtility.ToJson(body),
                result => { result.Data?.Normalize(); done(result); });
    }
    [Serializable] public sealed class PenaltyRequest
    {
        public string sessionId;
        public int version, turn, direction;
        public float aimX, aimY, power;
    }
    [Serializable] public sealed class PenaltyShot
    {
        public int shotId, durationMs, dive;
        public long startedAt;
        public float targetX, targetY, targetZ, power;
        public string outcome;
    }
    [Serializable] public sealed class PenaltyAttempt { public int publicId; public string result; }
    [Serializable] public sealed class PenaltySnapshot
    {
        public string phase, result, error;
        public int version, turn, winner, shooterId, keeperId, score1, score2, attempts1, attempts2;
        public long deadline, serverTime;
        public bool shooterReady, keeperReady;
        public WorldPeer player1, player2;
        public PenaltyShot shot;
        public PenaltyAttempt[] history;
        public int[] resetVotes;
        public void Normalize()
        {
            if (player1 != null && player1.publicId <= 0) player1 = null;
            if (player2 != null && player2.publicId <= 0) player2 = null;
            if (shot != null && shot.shotId <= 0) shot = null;
        }
    }
}

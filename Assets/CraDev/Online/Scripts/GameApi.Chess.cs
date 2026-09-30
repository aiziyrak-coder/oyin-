using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace CraDev.Online
{
    public partial class GameApi
    {
        public IEnumerator ChessTable(string token, string sessionId, int tableId, Action<ApiResult<ChessSnapshot>> done) =>
            Send<ChessSnapshot>("GET", "/api/chess/table?sessionId=" + UnityWebRequest.EscapeURL(sessionId ?? "") + "&tableId=" + tableId, token, null,
                result => { result.Data?.NormalizeSeats(); done(result); });
        public IEnumerator ChessAction(string token, string action, ChessRequest body, Action<ApiResult<ChessSnapshot>> done) =>
            Send<ChessSnapshot>("POST", "/api/chess/" + action, token, JsonUtility.ToJson(body),
                result => { result.Data?.NormalizeSeats(); done(result); });
    }

    [Serializable] public sealed class ChessRequest
    {
        public string sessionId, color, from, to, promotion;
        public int tableId, version;
    }
    [Serializable] public sealed class ChessSeat { public int publicId; public string nickname; }
    [Serializable] public sealed class ChessMove { public string from, to, promotion; }
    [Serializable] public sealed class ChessHistoryMove
    { public int ply, number; public string color, san, from, to, piece, captured, promotion; }
    [Serializable] public sealed class ChessSnapshot
    {
        public int tableId, version;
        public string fen, turn, status, winner, lastFrom, lastTo, error, checkSquare, timeControl, drawOffer;
        public string[] board;
        public string[] whiteCaptures, blackCaptures;
        public ChessHistoryMove[] history;
        public ChessSeat white, black;
        public ChessMove[] legalMoves;
        public int[] resetVotes;

        /// <summary>
        /// JsonUtility can materialize JSON null inline classes as empty objects. Public IDs are positive;
        /// a zero-ID seat is therefore vacant, not an occupied chair. Apply at the transport boundary.
        /// </summary>
        public void NormalizeSeats()
        {
            if (white != null && white.publicId <= 0) white = null;
            if (black != null && black.publicId <= 0) black = null;
        }
    }
}

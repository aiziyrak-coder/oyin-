using UnityEngine;

namespace CraDev.World
{
    /// <summary>Stable server table number and board coordinates. White sits south.</summary>
    public sealed class CityChessTable : MonoBehaviour
    {
        public int TableId;
        public Vector3 BoardCenter => transform.position + Vector3.up * .81f;
        public const float BoardSize = .9f;

        public Vector3 SquareCenter(int file, int rank)
        {
            return BoardCenter + new Vector3((file - 3.5f) * BoardSize / 8f, .008f,
                (rank - 3.5f) * BoardSize / 8f);
        }
    }
}

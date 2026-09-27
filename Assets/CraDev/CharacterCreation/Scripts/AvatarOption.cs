using System;
using UnityEngine;

namespace CraDev.CharacterCreation
{
    /// <summary>Tanlash mumkin bo'lgan avatar: kodi (M1…F5), nomi va uch tomondan rasmlari.</summary>
    [Serializable]
    public class AvatarOption
    {
        public string id;
        [Tooltip("\"male\" yoki \"female\"")]
        public string gender;
        public string title;
        public int heightCm;
        public Sprite front;
        public Sprite side;
        public Sprite back;
        [Tooltip("Ixtiyoriy: 45° burchakdan old ko'rinish. Bo'lsa, aylanish silliqroq bo'ladi.")]
        public Sprite frontQuarter;
        [Tooltip("Ixtiyoriy: 135° burchakdan orqa ko'rinish.")]
        public Sprite backQuarter;
    }
}

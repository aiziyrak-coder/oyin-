using System;
using UnityEngine;

namespace CraDev.CharacterCreation
{
    /// <summary>Tanlash mumkin bo'lgan avatar: kodi (M1…F5), nomi, 3D modeli va kartadagi rasmi.</summary>
    [Serializable]
    public class AvatarOption
    {
        public string id;
        [Tooltip("\"male\" yoki \"female\"")]
        public string gender;
        public string title;
        public int heightCm;
        [Tooltip("3D qahramon (Assets/CraDev/Avatars/Models/<ID>/*.fbx).")]
        public GameObject model;
        [Tooltip("Kartadagi kichik rasm: builder 3D modeldan chizib oladi.")]
        public Sprite card;
    }
}

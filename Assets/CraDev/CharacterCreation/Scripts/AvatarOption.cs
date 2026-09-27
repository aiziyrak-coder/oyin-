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
        [Tooltip("Yuz nuqtalarining bosh teksturasidagi o'rni (builder topadi; topilmagani -1).")]
        public Vector2[] faceUv;
        [Tooltip("faceUv nuqtalari orasidagi uchburchaklar (uchtadan indeks).")]
        public int[] faceTriangles;

        public bool SupportsFace => faceUv != null && faceUv.Length > 0 && faceTriangles != null && faceTriangles.Length > 0;
    }
}

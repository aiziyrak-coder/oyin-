using UnityEngine;
using UnityEngine.EventSystems;

namespace CraDev.CharacterCreation
{
    /// <summary>O'ng yarim ekrandagi ko'rinmas maydon: sichqoncha bilan tortilsa avatar buriladi.</summary>
    public class AvatarDragArea : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] AvatarPreview avatar;
        Canvas canvas;

        void Awake() => canvas = GetComponentInParent<Canvas>();

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (avatar != null) avatar.BeginDrag();
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Ekran piksellarini 1920x1080 birliklariga o'tkazamiz, shunda tezlik o'lchamga bog'liq bo'lmaydi
            float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
            if (avatar != null) avatar.Drag(eventData.delta.x / scale);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (avatar != null) avatar.EndDrag();
        }
    }
}

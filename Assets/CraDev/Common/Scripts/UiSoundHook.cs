using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraDev
{
    /// <summary>
    /// UiSounds qo'shadi: tugma ustiga sichqoncha kelganda (tugma faol bo'lsa) ovoz. hover=false: faqat bosilish
    /// ovozi (masalan, oyna tashqarisidagi ko'rinmas qorong'i fon).
    /// </summary>
    public class UiSoundHook : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] bool hover = true;

        Selectable selectable;

        /// <summary>Tugmaning bosilish ovozi allaqachon ulanganmi (UiSounds.Hook ikki marta ulamasligi uchun).</summary>
        public bool ClickHooked { get; set; }

        void Awake() => selectable = GetComponent<Selectable>();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hover && (selectable == null || selectable.IsInteractable()))
                UiSounds.PlayHover();
        }
    }
}

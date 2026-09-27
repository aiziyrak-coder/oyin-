using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraDev
{
    /// <summary>UiSounds qo'shadi: tugma ustiga sichqoncha kelganda (tugma faol bo'lsa) ovoz.</summary>
    public class UiSoundHook : MonoBehaviour, IPointerEnterHandler
    {
        Selectable selectable;

        void Awake() => selectable = GetComponent<Selectable>();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (selectable == null || selectable.IsInteractable())
                UiSounds.PlayHover();
        }
    }
}

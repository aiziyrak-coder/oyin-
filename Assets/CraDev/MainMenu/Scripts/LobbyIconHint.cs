using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    public class LobbyIconHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] Text label;
        [SerializeField] string key, fallback;
        void Show(bool active){if(label!=null)label.text=active?Loc.T(key):string.IsNullOrEmpty(fallback)?"":Loc.T(fallback);}
        public void OnPointerEnter(PointerEventData e)=>Show(true);
        public void OnPointerExit(PointerEventData e)=>Show(false);
        public void OnSelect(BaseEventData e)=>Show(true);
        public void OnDeselect(BaseEventData e)=>Show(false);
    }
}

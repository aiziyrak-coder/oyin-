using UnityEngine;
namespace CraDev.MainMenu
{
    public class MapMarker : MonoBehaviour
    {
        Vector2 original;
        void Awake() => original = ((RectTransform)transform).anchoredPosition;
        public void Zoom(float zoom)
        {
            ((RectTransform)transform).anchoredPosition = new Vector2(960,-540) + (original-new Vector2(960,-540))*zoom;
        }
    }
}

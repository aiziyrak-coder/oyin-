using UnityEngine;
using UnityEngine.UI;

namespace CraDev.CharacterCreation
{
    /// <summary>Jins tanlash kartasi (Male / Female): tanlangan va tanlanmagan ko'rinishlar.</summary>
    public class GenderCard : MonoBehaviour
    {
        [SerializeField] Image fill;
        [SerializeField] Image border;
        [SerializeField] Image iconCircle;
        [SerializeField] Image icon;
        [SerializeField] GameObject tick;

        static readonly Color Accent = new Color32(61, 90, 254, 255);
        static readonly Color Field = new Color32(22, 23, 26, 255);
        static readonly Color SelectedFill = new Color32(22, 27, 58, 255);
        static readonly Color Line = new Color32(38, 39, 44, 255);
        static readonly Color CircleIdle = new Color32(32, 33, 38, 255);
        static readonly Color Muted = new Color32(142, 147, 154, 255);

        public void SetSelected(bool selected)
        {
            fill.color = selected ? SelectedFill : Field;
            border.color = selected ? Accent : Line;
            iconCircle.color = selected ? Accent : CircleIdle;
            icon.color = selected ? Color.white : Muted;
            tick.SetActive(selected);
        }
    }
}

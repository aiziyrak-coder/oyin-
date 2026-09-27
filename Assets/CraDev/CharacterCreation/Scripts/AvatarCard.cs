using UnityEngine;
using UnityEngine.UI;

namespace CraDev.CharacterCreation
{
    /// <summary>Chap paneldagi kichik avatar kartasi: rasm, ramka va tanlangan belgisi.</summary>
    public class AvatarCard : MonoBehaviour
    {
        [SerializeField] Image fill;
        [SerializeField] Image border;
        [SerializeField] Image picture;
        [SerializeField] GameObject tick;

        static readonly Color Accent = new Color32(61, 90, 254, 255);
        static readonly Color Field = new Color32(22, 23, 26, 255);
        static readonly Color SelectedFill = new Color32(22, 27, 58, 255);
        static readonly Color Line = new Color32(38, 39, 44, 255);

        public Button Button => GetComponent<Button>();

        public void Show(AvatarOption avatar)
        {
            picture.sprite = avatar.card;
            picture.preserveAspect = true;
            gameObject.SetActive(true);
        }

        public void SetSelected(bool selected)
        {
            fill.color = selected ? SelectedFill : Field;
            border.color = selected ? Accent : Line;
            tick.SetActive(selected);
        }
    }
}

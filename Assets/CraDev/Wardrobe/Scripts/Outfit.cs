using System;
using UnityEngine;

namespace CraDev.Wardrobe
{
    /// <summary>Kiyim qismlari (qatlamlar): avatar teksturasidagi niqob kanallari bilan bir xil tartibda.</summary>
    public enum OutfitSlot { Top = 0, Bottom = 1, Shoes = 2, Hair = 3 }

    /// <summary>
    /// O'yinchining kiyimi: har bir qism uchun garderob buyumi (mato naqshi) va rang. Bo'sh - avatarning asl kiyimi.
    /// Serverga va PlayerPrefs'ga JSON ko'rinishida saqlanadi (boshqa o'yinchilar ham shu kiyimda ko'radi).
    /// </summary>
    [Serializable]
    public class Outfit
    {
        public string top = "", topColor = "";
        public string bottom = "", bottomColor = "";
        public string shoes = "", shoesColor = "";
        public string hair = "", hairColor = "";

        public string Item(OutfitSlot slot) => slot switch
        {
            OutfitSlot.Top => top, OutfitSlot.Bottom => bottom, OutfitSlot.Shoes => shoes, _ => hair,
        };

        public string ColorHex(OutfitSlot slot) => slot switch
        {
            OutfitSlot.Top => topColor, OutfitSlot.Bottom => bottomColor, OutfitSlot.Shoes => shoesColor, _ => hairColor,
        };

        public void Set(OutfitSlot slot, string item, string color)
        {
            item ??= "";
            color ??= "";
            switch (slot)
            {
                case OutfitSlot.Top: top = item; topColor = color; break;
                case OutfitSlot.Bottom: bottom = item; bottomColor = color; break;
                case OutfitSlot.Shoes: shoes = item; shoesColor = color; break;
                default: hair = item; hairColor = color; break;
            }
        }

        /// <summary>Qism o'zgartirilganmi (buyum tanlangan) va uning rangi (chiziqli emas, sRGB).</summary>
        public bool TryGetColor(OutfitSlot slot, out Color color)
        {
            color = Color.white;
            return !string.IsNullOrEmpty(Item(slot)) && ColorUtility.TryParseHtmlString("#" + ColorHex(slot), out color);
        }

        public bool IsEmpty => string.IsNullOrEmpty(top) && string.IsNullOrEmpty(bottom) && string.IsNullOrEmpty(shoes) && string.IsNullOrEmpty(hair);

        public Outfit Clone() => (Outfit)MemberwiseClone();

        public bool SameAs(Outfit other) => other != null && ToJson() == other.ToJson();

        public string ToJson() => IsEmpty ? "" : JsonUtility.ToJson(this);

        public static Outfit FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new Outfit();
            try
            {
                return JsonUtility.FromJson<Outfit>(json) ?? new Outfit();
            }
            catch (ArgumentException)
            {
                return new Outfit();
            }
        }
    }
}

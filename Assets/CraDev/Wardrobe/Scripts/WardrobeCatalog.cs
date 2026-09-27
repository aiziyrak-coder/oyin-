using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CraDev.Wardrobe
{
    /// <summary>Mato naqshlari (OutfitRecolor shaderidagi raqamlar bilan bir xil).</summary>
    public enum FabricPattern { Plain = 0, Denim = 1, Leather = 2, Knit = 3, Stripes = 4, Plaid = 5, Camo = 6 }

    /// <summary>Garderob bo'limlaridagi uslublar (yuqoridagi yorliqlar).</summary>
    [Flags]
    public enum OutfitStyle { None = 0, Suit = 1, Sport = 2, Casual = 4, Special = 8 }

    /// <summary>Garderob buyumi: qaysi qism, qanday mato, qaysi rangda (o'yinchi rangini keyin o'zgartira oladi).</summary>
    public sealed class WardrobeItem
    {
        public readonly string Id;
        public readonly OutfitSlot Slot;
        public readonly FabricPattern Pattern;
        public readonly string Color;   // "RRGGBB"
        public readonly OutfitStyle Styles;

        public WardrobeItem(string id, OutfitSlot slot, FabricPattern pattern, string color, OutfitStyle styles)
        {
            Id = id; Slot = slot; Pattern = pattern; Color = color; Styles = styles;
        }

        public string Name => Loc.T("wardrobe.item." + Id);
    }

    /// <summary>Tayyor obraz: to'liq kiyim to'plami.</summary>
    public sealed class WardrobeLook
    {
        public readonly string Id;
        public readonly OutfitStyle Styles;
        public readonly Outfit Outfit;

        public WardrobeLook(string id, OutfitStyle styles, Outfit outfit) { Id = id; Styles = styles; Outfit = outfit; }

        public string Name => Loc.T("wardrobe.look." + Id);
    }

    /// <summary>
    /// Garderobdagi hamma narsa: buyumlar, ranglar palitrasi va tayyor obrazlar. Rocketbox kiyimlari yaxlit bo'lgani uchun
    /// "buyum" - avatarning o'z kiyimi yangi mato va rangda (niqob bo'yicha bo'yaladi).
    /// </summary>
    public static class WardrobeCatalog
    {
        const OutfitStyle Suit = OutfitStyle.Suit, Sport = OutfitStyle.Sport, Casual = OutfitStyle.Casual, Special = OutfitStyle.Special;

        public static readonly WardrobeItem[] Items =
        {
            // Ustki kiyim
            new WardrobeItem("top_bomber_black", OutfitSlot.Top, FabricPattern.Plain, "1E1E21", Casual | Sport),
            new WardrobeItem("top_leather_black", OutfitSlot.Top, FabricPattern.Leather, "18181B", Special | Casual),
            new WardrobeItem("top_leather_brown", OutfitSlot.Top, FabricPattern.Leather, "6B4630", Special),
            new WardrobeItem("top_knit_white", OutfitSlot.Top, FabricPattern.Knit, "E6E3DC", Casual),
            new WardrobeItem("top_suede_tan", OutfitSlot.Top, FabricPattern.Leather, "B98A55", Casual | Special),
            new WardrobeItem("top_denim", OutfitSlot.Top, FabricPattern.Denim, "3E5F8F", Casual),
            new WardrobeItem("top_blazer_navy", OutfitSlot.Top, FabricPattern.Plain, "1F2A44", Suit),
            new WardrobeItem("top_knit_grey", OutfitSlot.Top, FabricPattern.Knit, "7D8087", Suit | Casual),
            new WardrobeItem("top_stripes", OutfitSlot.Top, FabricPattern.Stripes, "1F2A44", Casual),
            new WardrobeItem("top_plaid_red", OutfitSlot.Top, FabricPattern.Plaid, "8E2A2A", Casual | Special),
            new WardrobeItem("top_camo", OutfitSlot.Top, FabricPattern.Camo, "5B6236", Sport | Special),
            new WardrobeItem("top_sport_red", OutfitSlot.Top, FabricPattern.Plain, "B3262E", Sport),
            new WardrobeItem("top_sport_blue", OutfitSlot.Top, FabricPattern.Plain, "2F5BD8", Sport),
            new WardrobeItem("top_olive", OutfitSlot.Top, FabricPattern.Plain, "4E5A3A", Casual),
            // Shim
            new WardrobeItem("bottom_jeans_blue", OutfitSlot.Bottom, FabricPattern.Denim, "2F4A78", Casual),
            new WardrobeItem("bottom_jeans_black", OutfitSlot.Bottom, FabricPattern.Denim, "25282E", Casual | Special),
            new WardrobeItem("bottom_chino_beige", OutfitSlot.Bottom, FabricPattern.Plain, "C8B28E", Suit | Casual),
            new WardrobeItem("bottom_suit_grey", OutfitSlot.Bottom, FabricPattern.Plain, "5E6168", Suit),
            new WardrobeItem("bottom_suit_navy", OutfitSlot.Bottom, FabricPattern.Plain, "1F2A44", Suit),
            new WardrobeItem("bottom_cargo_black", OutfitSlot.Bottom, FabricPattern.Plain, "1E1F21", Sport | Casual),
            new WardrobeItem("bottom_camo", OutfitSlot.Bottom, FabricPattern.Camo, "5B6236", Sport | Special),
            new WardrobeItem("bottom_leather", OutfitSlot.Bottom, FabricPattern.Leather, "141416", Special),
            new WardrobeItem("bottom_sport_grey", OutfitSlot.Bottom, FabricPattern.Knit, "8A8D93", Sport),
            // Oyoq kiyim
            new WardrobeItem("shoes_white", OutfitSlot.Shoes, FabricPattern.Plain, "EDEDEA", Sport | Casual),
            new WardrobeItem("shoes_black", OutfitSlot.Shoes, FabricPattern.Plain, "1B1B1D", Suit | Casual),
            new WardrobeItem("shoes_leather_brown", OutfitSlot.Shoes, FabricPattern.Leather, "5A3A26", Suit),
            new WardrobeItem("shoes_leather_black", OutfitSlot.Shoes, FabricPattern.Leather, "111113", Suit | Special),
            new WardrobeItem("shoes_red", OutfitSlot.Shoes, FabricPattern.Plain, "B3262E", Sport),
            new WardrobeItem("shoes_blue", OutfitSlot.Shoes, FabricPattern.Plain, "2F5BD8", Sport),
            new WardrobeItem("shoes_beige", OutfitSlot.Shoes, FabricPattern.Plain, "CDB896", Casual),
            new WardrobeItem("shoes_camo", OutfitSlot.Shoes, FabricPattern.Camo, "5B6236", Special),
            // Soch rangi
            new WardrobeItem("hair_black", OutfitSlot.Hair, FabricPattern.Plain, "141214", Casual | Suit),
            new WardrobeItem("hair_dark_brown", OutfitSlot.Hair, FabricPattern.Plain, "33221A", Casual | Suit),
            new WardrobeItem("hair_brown", OutfitSlot.Hair, FabricPattern.Plain, "5E3E26", Casual),
            new WardrobeItem("hair_auburn", OutfitSlot.Hair, FabricPattern.Plain, "6E2E1A", Casual),
            new WardrobeItem("hair_copper", OutfitSlot.Hair, FabricPattern.Plain, "A4502A", Special),
            new WardrobeItem("hair_blond", OutfitSlot.Hair, FabricPattern.Plain, "C9A46A", Casual),
            new WardrobeItem("hair_platinum", OutfitSlot.Hair, FabricPattern.Plain, "DCD2BE", Special),
            new WardrobeItem("hair_grey", OutfitSlot.Hair, FabricPattern.Plain, "9A9A9A", Suit),
            new WardrobeItem("hair_blue", OutfitSlot.Hair, FabricPattern.Plain, "2E4FA8", Special),
            new WardrobeItem("hair_pink", OutfitSlot.Hair, FabricPattern.Plain, "D96C9C", Special),
            new WardrobeItem("hair_purple", OutfitSlot.Hair, FabricPattern.Plain, "6D3FA0", Special),
        };

        /// <summary>Rang tanlash qatori (tanlangan buyum rangi).</summary>
        public static readonly string[] Palette =
        {
            "1C1C1E", "F2F2F0", "8A8D93", "1F2A44", "2F5BD8", "7FB2E5", "B3262E", "6B1E2E",
            "2E6B45", "5B6236", "C8B28E", "6B4630", "E07A2E", "E3C23A", "6D4BA8", "E58FB0",
        };

        public static readonly string[] HairPalette =
        {
            "141214", "33221A", "5E3E26", "6E2E1A", "A4502A", "C9A46A", "DCD2BE", "9A9A9A", "2E4FA8", "D96C9C", "6D3FA0",
        };

        public static readonly WardrobeLook[] Looks =
        {
            new WardrobeLook("original", Casual, new Outfit()),
            new WardrobeLook("casual", Casual, Make("top_denim", "bottom_chino_beige", "shoes_white")),
            new WardrobeLook("sport", Sport, Make("top_sport_blue", "bottom_sport_grey", "shoes_white")),
            new WardrobeLook("formal", Suit, Make("top_blazer_navy", "bottom_suit_navy", "shoes_leather_black")),
            new WardrobeLook("city", Casual | Special, Make("top_leather_black", "bottom_jeans_black", "shoes_black")),
            new WardrobeLook("military", Special | Sport, Make("top_camo", "bottom_cargo_black", "shoes_leather_black")),
            new WardrobeLook("summer", Casual, Make("top_knit_white", "bottom_chino_beige", "shoes_beige")),
            new WardrobeLook("autumn", Casual | Special, Make("top_plaid_red", "bottom_jeans_blue", "shoes_leather_brown")),
        };

        static readonly Dictionary<string, WardrobeItem> byId = Items.ToDictionary(i => i.Id);

        public static WardrobeItem Find(string id) => id != null && byId.TryGetValue(id, out var item) ? item : null;

        public static IEnumerable<WardrobeItem> For(OutfitSlot slot) => Items.Where(i => i.Slot == slot);

        static Outfit Make(string top, string bottom, string shoes)
        {
            var outfit = new Outfit();
            foreach (var id in new[] { top, bottom, shoes })
            {
                var item = FindEarly(id);
                outfit.Set(item.Slot, item.Id, item.Color);
            }
            return outfit;
        }

        // Looks statik maydoni byId dan oldin yaratiladi: shu sabab bu yerda ro'yxatdan qidiriladi
        static WardrobeItem FindEarly(string id) => Items.First(i => i.Id == id);

        /// <summary>Kiyimdagi noma'lum buyum yoki noto'g'ri rangni tozalaydi (eski versiya yoki buzilgan ma'lumot).</summary>
        public static Outfit Sanitize(Outfit outfit)
        {
            var clean = new Outfit();
            if (outfit == null)
                return clean;
            foreach (OutfitSlot slot in Enum.GetValues(typeof(OutfitSlot)))
            {
                var item = Find(outfit.Item(slot));
                if (item == null || item.Slot != slot)
                    continue;
                string color = outfit.ColorHex(slot);
                clean.Set(slot, item.Id, ColorUtility.TryParseHtmlString("#" + color, out _) && color.Length == 6 ? color : item.Color);
            }
            return clean;
        }
    }
}

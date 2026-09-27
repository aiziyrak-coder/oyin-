// AVTOMATIK YARATILGAN: foydalanuvchi konsept rasmining (1280x720) o'lchovlari.
// Qayta yaratish: scratchpad/ref/gen_layout.py spec.json. Qo'lda o'zgartirish mumkin (masalan, joyini surish).
using System.Collections.Generic;
using UnityEngine;

namespace CraDev.EditorTools
{
    /// <summary>
    /// Bosh menyu joylashuvi: har bir elementning konsept rasmdagi o'rni (piksel, 1280x720, y pastga), shrift
    /// o'lchami, rangi va burilishi. Builder bularni 1920x1080 ga (x1.5) o'tkazadi.
    /// </summary>
    static class LobbyLayout
    {
        public const float Width = 1280f, Height = 720f;

        public readonly struct Item
        {
            public readonly float X0, Y0, X1, Y1, FontSize, Rotation;
            public readonly Color Color;
            public Item(float x0, float y0, float x1, float y1, float fontSize, string hex, float rotation)
            {
                X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; FontSize = fontSize; Rotation = rotation;
                Color = ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.white;
            }
            public float Width => X1 - X0;
            public float Height => Y1 - Y0;
            public Vector2 Center => new Vector2((X0 + X1) / 2f, (Y0 + Y1) / 2f);
        }

        public static readonly Dictionary<string, Item> Items = new Dictionary<string, Item>
        {
            ["bell_btn"] = new Item(1069.0f, 29.0f, 1088.0f, 51.0f, 0.0f, "070A10", 0.0f),
            ["brand_icon"] = new Item(45.0f, 24.0f, 75.0f, 56.0f, 0.0f, "4476F2", 0.0f),
            ["brand_text"] = new Item(85.0f, 34.0f, 155.0f, 48.0f, 19.0f, "FFFFFF", 0.0f),
            ["btn_customize"] = new Item(52.0f, 358.0f, 304.0f, 399.0f, 0.0f, "1A1E24", 0.0f),
            ["btn_customize_icon"] = new Item(73.0f, 368.0f, 92.0f, 388.0f, 0.0f, "DDE1E8", 0.0f),
            ["btn_customize_label"] = new Item(112.0f, 374.0f, 211.0f, 385.0f, 12.5f, "E4E8EC", 0.0f),
            ["btn_enter"] = new Item(52.0f, 302.0f, 304.0f, 347.0f, 0.0f, "3F5CFC", 0.0f),
            ["btn_enter_arrow"] = new Item(257.0f, 320.0f, 272.0f, 331.0f, 0.0f, "DCF2FF", 0.0f),
            ["btn_enter_icon"] = new Item(77.0f, 316.0f, 94.0f, 335.0f, 0.0f, "E2EEFF", 0.0f),
            ["btn_enter_label"] = new Item(115.0f, 319.0f, 151.0f, 330.0f, 15.0f, "EEF5FF", 0.0f),
            ["btn_help"] = new Item(52.0f, 448.0f, 304.0f, 489.0f, 0.0f, "1A1E24", 0.0f),
            ["btn_help_icon"] = new Item(72.0f, 458.0f, 93.0f, 479.0f, 0.0f, "DDE1E8", 0.0f),
            ["btn_help_label"] = new Item(112.0f, 464.0f, 148.0f, 473.0f, 12.5f, "E4E8EC", 0.0f),
            ["btn_quit"] = new Item(52.0f, 493.0f, 304.0f, 534.0f, 0.0f, "1A1E24", 0.0f),
            ["btn_quit_icon"] = new Item(75.0f, 504.0f, 93.0f, 524.0f, 0.0f, "DDE1E8", 0.0f),
            ["btn_quit_label"] = new Item(112.0f, 509.0f, 148.0f, 520.0f, 12.5f, "E4E8EC", 0.0f),
            ["btn_settings"] = new Item(52.0f, 403.0f, 304.0f, 444.0f, 0.0f, "1A1E24", 0.0f),
            ["btn_settings_icon"] = new Item(73.0f, 413.0f, 93.0f, 434.0f, 0.0f, "DDE1E8", 0.0f),
            ["btn_settings_label"] = new Item(112.0f, 419.0f, 167.0f, 428.0f, 12.5f, "E4E8EC", 0.0f),
            ["card_1"] = new Item(249.0f, 572.0f, 441.0f, 685.0f, 0.0f, "FFFFFF", 0.0f),
            ["card_1_icon"] = new Item(272.0f, 648.0f, 293.0f, 672.0f, 0.0f, "E8ECF4", 0.0f),
            ["card_1_image"] = new Item(255.0f, 577.0f, 436.0f, 640.0f, 0.0f, "FFFFFF", 0.0f),
            ["card_1_subtitle"] = new Item(312.0f, 664.0f, 378.0f, 671.0f, 10.0f, "8C95A8", 0.0f),
            ["card_1_title"] = new Item(311.0f, 650.0f, 361.0f, 660.0f, 12.0f, "F0F3FA", 0.0f),
            ["card_2"] = new Item(453.0f, 574.0f, 632.0f, 684.0f, 0.0f, "FFFFFF", 0.0f),
            ["card_2_icon"] = new Item(470.0f, 651.0f, 492.0f, 669.0f, 0.0f, "E6EAEC", 0.0f),
            ["card_2_image"] = new Item(457.0f, 578.0f, 628.0f, 640.0f, 0.0f, "FFFFFF", 0.0f),
            ["card_2_subtitle"] = new Item(511.0f, 664.0f, 583.0f, 671.0f, 10.0f, "8C95A8", 0.0f),
            ["card_2_title"] = new Item(511.0f, 650.0f, 585.0f, 659.0f, 12.0f, "F0F3FA", 0.0f),
            ["card_3"] = new Item(645.0f, 574.0f, 823.0f, 684.0f, 0.0f, "FFFFFF", 0.0f),
            ["card_3_icon"] = new Item(662.0f, 649.0f, 681.0f, 670.0f, 0.0f, "EDEFFA", 0.0f),
            ["card_3_image"] = new Item(649.0f, 578.0f, 819.0f, 640.0f, 0.0f, "FFFFFF", 0.0f),
            ["card_3_subtitle"] = new Item(702.0f, 665.0f, 764.0f, 671.0f, 10.0f, "8C95A8", 0.0f),
            ["card_3_title"] = new Item(702.0f, 650.0f, 767.0f, 658.0f, 12.0f, "F0F3FA", 0.0f),
            ["card_4"] = new Item(836.0f, 574.0f, 1014.0f, 684.0f, 0.0f, "FFFFFF", 0.0f),
            ["card_4_icon"] = new Item(852.0f, 650.0f, 879.0f, 669.0f, 0.0f, "E0E4F2", 0.0f),
            ["card_4_image"] = new Item(840.0f, 578.0f, 1009.0f, 640.0f, 0.0f, "FFFFFF", 0.0f),
            ["card_4_subtitle"] = new Item(898.0f, 665.0f, 963.0f, 671.0f, 10.0f, "8C95A8", 0.0f),
            ["card_4_title"] = new Item(898.0f, 650.0f, 977.0f, 659.0f, 12.0f, "F0F3FA", 0.0f),
            ["card_5"] = new Item(1026.0f, 574.0f, 1204.0f, 684.0f, 0.0f, "FFFFFF", 0.0f),
            ["card_5_icon"] = new Item(1047.0f, 651.0f, 1067.0f, 668.0f, 0.0f, "F4F6F8", 0.0f),
            ["card_5_image"] = new Item(1031.0f, 578.0f, 1200.0f, 640.0f, 0.0f, "FFFFFF", 0.0f),
            ["card_5_subtitle"] = new Item(1087.0f, 664.0f, 1162.0f, 671.0f, 10.0f, "8C95A8", 0.0f),
            ["card_5_title"] = new Item(1087.0f, 650.0f, 1136.0f, 658.0f, 12.0f, "F0F3FA", 0.0f),
            ["cards_next_btn"] = new Item(1223.0f, 611.0f, 1258.0f, 647.0f, 0.0f, "FFFFFF", 0.0f),
            ["character"] = new Item(609.0f, 110.0f, 748.0f, 529.0f, 0.0f, "FFFFFF", 0.0f),
            ["description"] = new Item(53.0f, 253.0f, 291.0f, 283.0f, 12.5f, "B8BBC4", 0.0f),
            ["lang_chevron"] = new Item(1027.0f, 38.0f, 1036.0f, 43.0f, 0.0f, "10203A", 0.0f),
            ["lang_globe"] = new Item(968.0f, 29.0f, 990.0f, 51.0f, 0.0f, "0C1624", 0.0f),
            ["lang_pill"] = new Item(968.0f, 29.0f, 1036.0f, 51.0f, 0.0f, "FFFFFF", 0.0f),
            ["lang_text"] = new Item(1000.0f, 35.0f, 1018.0f, 45.0f, 14.0f, "0E1A30", 0.0f),
            ["nameplate"] = new Item(728.0f, 453.0f, 823.0f, 491.0f, 0.0f, "FFFFFF", 0.0f),
            ["nameplate_dot"] = new Item(737.0f, 462.0f, 746.0f, 471.0f, 0.0f, "2BC482", 0.0f),
            ["nameplate_name"] = new Item(750.0f, 463.0f, 807.0f, 472.0f, 12.0f, "DFE3F0", 0.0f),
            ["nameplate_status"] = new Item(750.0f, 477.0f, 773.0f, 483.0f, 9.5f, "99A1B3", 0.0f),
            ["profile_avatar"] = new Item(1121.0f, 26.0f, 1149.0f, 55.0f, 0.0f, "FFFFFF", 0.0f),
            ["profile_chevron"] = new Item(1227.0f, 38.0f, 1236.0f, 43.0f, 0.0f, "E6EAF4", 0.0f),
            ["profile_name"] = new Item(1157.0f, 36.0f, 1218.0f, 47.0f, 13.0f, "EEF0F8", 0.0f),
            ["profile_pill"] = new Item(1112.0f, 20.0f, 1249.0f, 60.0f, 0.0f, "FFFFFF", 0.0f),
            ["quote_sign"] = new Item(1142.0f, 151.0f, 1225.0f, 166.0f, 9.0f, "9A8F98", 8.0f),
            ["quote_text"] = new Item(1090.0f, 103.0f, 1227.0f, 150.0f, 17.0f, "736A85", 8.0f),
            ["script_left"] = new Item(51.0f, 563.0f, 213.0f, 627.0f, 30.0f, "C8D9EA", 10.0f),
            ["script_right"] = new Item(946.0f, 491.0f, 1086.0f, 556.0f, 28.0f, "E8EEF8", 10.5f),
            ["status_dot"] = new Item(47.0f, 671.0f, 56.0f, 680.0f, 0.0f, "2BD17E", 0.0f),
            ["status_text"] = new Item(61.0f, 671.0f, 91.0f, 679.0f, 11.0f, "8D909A", 0.0f),
            ["tagline1"] = new Item(53.0f, 198.0f, 203.0f, 209.0f, 14.5f, "C4C6CE", 0.0f),
            ["tagline2"] = new Item(53.0f, 220.0f, 245.0f, 230.0f, 14.5f, "C4C6CE", 0.0f),
            ["title_name"] = new Item(53.0f, 130.0f, 323.0f, 188.0f, 64.0f, "FFFFFF", 0.0f),
            ["tower_sign"] = new Item(805.0f, 156.0f, 887.0f, 184.0f, 22.0f, "EDF5FD", 0.0f),
            ["version_text"] = new Item(125.0f, 672.0f, 152.0f, 680.0f, 11.0f, "6A6F79", 0.0f),
            ["welcome_label"] = new Item(51.0f, 105.0f, 207.0f, 115.0f, 14.0f, "A8ACB4", 0.0f),
        };

        // Qahramon ostidagi yonib turuvchi platforma (ellips) va qahramon
        public const float PlatformX = 671.5f, PlatformY = 521.7f;
        public const float PlatformOuterRx = 211.0f, PlatformOuterRy = 24.5f;
        public const float PlatformInnerRx = 179.0f, PlatformInnerRy = 20.0f;
        public const float CharacterX = 678.0f, CharacterHeadY = 110.0f, CharacterFeetY = 529.0f;

        public static Item Get(string id) => Items.TryGetValue(id, out var item) ? item : throw new KeyNotFoundException("LobbyLayout: " + id);
    }
}

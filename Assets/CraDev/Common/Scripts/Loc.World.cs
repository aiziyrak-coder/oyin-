namespace CraDev
{
    // Dunyo (WorldSandbox) Esc menyusining qo'shimcha matnlari.
    public static partial class Loc
    {
        static readonly System.Collections.Generic.Dictionary<string, (string uz, string en)> WorldTable =
            Register(new System.Collections.Generic.Dictionary<string, (string uz, string en)>
            {
                ["world.settings.note"] = ("Boshqaruv faqat olam uchun; grafika va ovoz butun o'yinga tegishli",
                    "Controls apply to the world; graphics and sound apply to the whole game"),
                ["world.settings.section.controls"] = ("BOSHQARUV", "CONTROLS"),
                ["world.settings.section.graphics"] = ("GRAFIKA VA OVOZ", "GRAPHICS & SOUND"),
                ["world.settings.display"] = ("Ekran rejimi", "Display mode"),
                ["world.settings.display.fullscreen"] = ("To'liq ekran", "Fullscreen"),
                ["world.settings.display.windowed"] = ("Oynada", "Windowed"),
                ["world.settings.quality"] = ("Grafika sifati", "Graphics quality"),
                ["world.settings.vsync"] = ("V-Sync (kadrlarni monitorga moslash)", "V-Sync (match monitor refresh)"),
                ["world.settings.ao"] = ("Yumshoq kontakt soyalari (AO)", "Soft contact shadows (AO)"),
                ["world.settings.volume"] = ("Umumiy ovoz", "Master volume"),
                ["world.settings.reset_controls"] = ("Boshqaruvni tiklash", "Reset controls"),
            });
    }
}

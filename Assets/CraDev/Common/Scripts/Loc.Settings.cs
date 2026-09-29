using System.Collections.Generic;

namespace CraDev
{
    // Sozlamalar sahifasining matnlari (Loc.cs dagi asosiy jadvalga Register orqali qo'shiladi).
    public static partial class Loc
    {
        static readonly Dictionary<string, (string uz, string en)> SettingsTable = Register(new Dictionary<string, (string uz, string en)>
        {
            ["settings.section.graphics"] = ("Ekran va grafika", "Display & graphics"),
            ["settings.section.audio"] = ("Ovoz", "Audio"),
            ["settings.section.lobby"] = ("Lobbi va xabarlar", "Lobby & notifications"),

            ["settings.avatar"] = ("Avatar va yuz", "Avatar & face"),
            ["settings.avatar_hint"] = ("Qahramonni va yuz rasmini almashtiring", "Change your character and face photo"),
            ["settings.row.nickname"] = ("Nickname", "Nickname"),
            ["settings.account_value"] = ("Shu kompyuterda saqlanadi", "Saved on this computer"),
            ["settings.not_set"] = ("Tanlanmagan", "Not set"),
            ["settings.country_title"] = ("Mamlakatni tanlang", "Choose your country"),
            ["settings.back"] = ("Orqaga", "Back"),

            ["settings.mode.exclusive"] = ("To'liq ekran", "Fullscreen"),
            ["settings.mode.borderless"] = ("Chegarasiz oyna", "Borderless"),
            ["settings.resolution"] = ("Ekran o'lchami", "Resolution"),
            ["settings.resolution_native"] = ("{0} × {1} (monitor)", "{0} × {1} (monitor)"),
            ["settings.resolution_hint.exclusive"] = ("Kichikroq o'lcham tezroq ishlaydi", "Lower resolutions run faster"),
            ["settings.resolution_hint.windowed"] = ("Ekranga sig'adigan oyna o'lchamlari", "Window sizes that fit your screen"),
            ["settings.resolution_hint.borderless"] = ("Chegarasiz oyna doim monitor o'lchamida", "Borderless always uses your monitor resolution"),
            ["settings.quality_hint"] = ("Soyalar, silliqlash va teksturalar sifati", "Shadows, anti-aliasing and texture detail"),
            ["settings.vsync_hint"] = ("Kadrlar monitor chastotasiga tenglashadi, tasvir yirtilmaydi", "Matches your monitor's refresh rate, no tearing"),
            ["settings.fps"] = ("Kadr chegarasi", "Frame rate limit"),
            ["settings.fps_value"] = ("{0} FPS", "{0} FPS"),
            ["settings.fps_unlimited"] = ("Cheklanmagan", "Unlimited"),
            ["settings.fps_hint"] = ("Noutbukda 60 FPS batareya va issiqlikni tejaydi", "60 FPS saves battery and heat on laptops"),
            ["settings.fps_hint_vsync"] = ("V-Sync yoqilgan: monitor chastotasi ishlatiladi", "V-Sync is on: your monitor's refresh rate is used"),
            ["settings.keep_title"] = ("Ekran sozlamalari saqlansinmi?", "Keep these display settings?"),
            ["settings.keep_message"] = ("Tasvir to'g'ri ko'rinayotgan bo'lsa, \"Saqlash\"ni bosing. {0} soniyadan keyin avvalgi sozlamalar qaytariladi.",
                "If everything looks right, choose Keep. Previous settings return in {0} s."),
            ["settings.keep"] = ("Saqlash", "Keep"),
            ["settings.revert"] = ("Qaytarish", "Revert"),
            ["settings.reverted"] = ("Avvalgi ekran sozlamalari qaytarildi", "Previous display settings restored"),

            ["settings.volume_master"] = ("Umumiy ovoz", "Master volume"),
            ["settings.volume_ui"] = ("Interfeys ovozlari", "Interface sounds"),
            ["settings.volume_ui_hint"] = ("Tugma bosilishi va ustiga kelish ovozlari", "Button click and hover sounds"),

            ["settings.crouch_mode"] = ("Cho'kkalash", "Crouch"),
            ["settings.crouch_hold"] = ("Ushlab turish", "Hold"),
            ["settings.crouch_toggle"] = ("Bosib almashtirish", "Toggle"),
            ["settings.controls_note"] = ("Bu sozlamalar olamdagi Esc menyusi bilan bir xil va darhol ishlaydi.",
                "These are the same settings as the in-world Esc menu and apply immediately."),
            ["settings.keys_title"] = ("Tugmalar", "Keys"),
            ["settings.key.sprint"] = ("Yugurish", "Sprint"),
            ["settings.key.crouch"] = ("Cho'kkalash", "Crouch"),
            ["settings.key.look"] = ("Atrofga qarash", "Look around"),
            ["settings.key.esc"] = ("Menyu va sozlamalar", "Menu & settings"),
            ["settings.key.or"] = ("yoki", "or"),
            ["settings.key.arrows"] = ("Strelkalar", "Arrow keys"),
            ["settings.key.mouse_cap"] = ("Sichqoncha", "Mouse"),
            ["settings.key.space"] = ("Probel", "Space"),

            ["settings.weather_hint"] = ("Soat bo'yicha: kun vaqti kompyuter soatidan olinadi", "Local clock follows your computer's time of day"),
            ["settings.notify.requests"] = ("Do'stlik so'rovlari haqida xabar", "Friend request notifications"),
            ["settings.notify.requests_hint"] = ("Yangi so'rov kelganda lobbida xabar chiqadi", "Show a notice in the lobby when a request arrives"),

            ["settings.privacy.info"] = ("Onlayn holat o'chirilsa, boshqalar sizni oflayn ko'radi. So'rovlar o'chirilsa, hech kim sizga yangi do'stlik so'rovi yubora olmaydi. O'zgarishlar serverda saqlanadi.",
                "When online status is off, others see you as offline. When requests are off, nobody can send you a new friend request. Changes are saved on the server."),
            ["settings.language_hint"] = ("Tilni lobbining yuqori o'ng burchagidagi UZ/EN tugmasi bilan ham almashtirish mumkin.",
                "You can also switch the language with the UZ/EN button at the top right of the lobby."),

            ["settings.reset"] = ("Asliga qaytarish", "Restore defaults"),
            ["settings.reset_title"] = ("Asliga qaytarilsinmi?", "Restore defaults?"),
            ["settings.reset_message"] = ("\"{0}\" bo'limidagi sozlamalar standart qiymatlarga qaytadi.", "Settings in \"{0}\" will return to their default values."),
            ["settings.reset_done"] = ("Standart sozlamalar tiklandi", "Defaults restored"),

            ["settings.help.lobby"] = ("Lobbi - NewWorldga kirish joyi, hammasi bitta oynada. Chapdagi panelda do'stlaringiz: qidiruv, yangi do'st qo'shish (+), so'rovlar va faqat onlayn do'stlar. Onlayn do'stni lobbingizga taklif qilsangiz, u yoningizda paydo bo'ladi (5 kishigacha). O'ng yuqorida til (UZ/EN), qo'ng'iroqcha (do'stlik so'rovlari) va profilingiz: u shu sozlamalarni ochadi. Avatar va yuzni \"Profil\" bo'limida o'zgartirasiz. O'ng pastdagi \"NewWorldga kirish\" tugmasi olamni ochadi. Esc oynani yopadi yoki o'yindan chiqishni so'raydi.",
                "The lobby is your entry point to NewWorld, all in one window. The panel on the left holds your friends: search, add a friend (+), requests and an online-only view. Invite an online friend to your lobby and they appear next to you (up to 5 people). At the top right are the language (UZ/EN), the bell (friend requests) and your profile, which opens these settings. Change your avatar and face in the Profile section. The Enter NewWorld button at the bottom right opens the world. Esc closes a window or asks to quit."),
            ["settings.help.world_title"] = ("Olamda", "In the world"),
            ["settings.help.world"] = ("W A S D yoki strelkalar - yurish, Shift - yugurish, Ctrl yoki C - cho'kkalash, Space - sakrash, sichqoncha - atrofga qarash. Esc olam menyusini ochadi: u yerda sezgirlik va ko'rish maydonini sozlash yoki lobbiga qaytish mumkin.",
                "W A S D or arrow keys - move, Shift - sprint, Ctrl or C - crouch, Space - jump, mouse - look around. Esc opens the world menu, where you can adjust sensitivity and field of view or return to the lobby."),
        });
    }
}

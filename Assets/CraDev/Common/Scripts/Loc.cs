using System;
using System.Collections.Generic;
using UnityEngine;

namespace CraDev
{
    public enum Language { Uz, En }

    /// <summary>
    /// O'yin matnlari ikki tilda: o'zbekcha (standart) va inglizcha. Til PlayerPrefs'da saqlanadi.
    /// Matn kalit bo'yicha olinadi: <c>Loc.T("menu.enter")</c>. Til almashsa <see cref="Changed"/> chaqiriladi,
    /// <see cref="LocalizedText"/> komponentlari o'zi yangilanadi.
    /// </summary>
    public static class Loc
    {
        const string PrefKey = "cradev.language";
        static Language? current;

        public static event Action Changed;

        public static Language Current
        {
            get
            {
                current ??= PlayerPrefs.GetString(PrefKey, "uz") == "en" ? Language.En : Language.Uz;
                return current.Value;
            }
            set
            {
                if (Current == value)
                    return;
                current = value;
                PlayerPrefs.SetString(PrefKey, value == Language.En ? "en" : "uz");
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        public static string Code => Current == Language.En ? "EN" : "UZ";

        public static string T(string key)
        {
            if (!Table.TryGetValue(key, out var pair))
            {
                Debug.LogWarning("[CraDev] Tarjima yo'q: " + key);
                return key;
            }
            return Current == Language.En ? pair.en : pair.uz;
        }

        /// <summary>Formatlangan matn: <c>Loc.F("create.welcome", nickname)</c>.</summary>
        public static string F(string key, params object[] args) => string.Format(T(key), args);

        /// <summary>Harflar orasini ochadi ("XUSH" -> "X U S H"): Text'da harf oralig'i sozlamasi yo'q.</summary>
        public static string Spaced(string text) => string.Join(" ", text.ToCharArray()).Replace("   ", "     ");

        static readonly Dictionary<string, (string uz, string en)> Table = new Dictionary<string, (string, string)>
        {
            // ---------------- Umumiy
            ["common.cancel"] = ("Bekor qilish", "Cancel"),
            ["common.quit"] = ("Chiqish", "Quit"),
            ["common.ok"] = ("Tushunarli", "Got it"),
            ["common.soon"] = ("Tez orada", "Coming soon"),
            ["common.online"] = ("Onlayn", "Online"),
            ["common.offline"] = ("Oflayn", "Offline"),
            ["common.connecting"] = ("Ulanmoqda…", "Connecting…"),
            ["common.server_error"] = ("Server xatosi", "Server error"),
            ["loading.loading"] = ("YUKLANMOQDA", "LOADING"),
            ["loading.ready"] = ("TAYYOR", "READY"),

            // ---------------- Bosh menyu
            ["menu.welcome"] = ("XUSH KELIBSIZ", "WELCOME"),
            ["menu.tagline1"] = ("VIRTUAL DUNYO", "VIRTUAL WORLD"),
            ["menu.tagline2"] = ("REAL IMKONIYATLAR", "REAL OPPORTUNITIES"),
            ["menu.description"] = ("O'qing. Xarid qiling. Ishlang. Muloqot qiling.\nBularning barchasi — bitta virtual olamda.",
                "Learn. Shop. Work. Connect.\nAll of it in one virtual world."),
            ["menu.enter"] = ("Kirish", "Enter"),
            ["menu.customize"] = ("Personajni sozlash", "Customize character"),
            ["menu.settings"] = ("Sozlamalar", "Settings"),
            ["menu.help"] = ("Yordam", "Help"),
            ["menu.quit"] = ("Chiqish", "Quit"),
            ["menu.enter_soon"] = ("Virtual dunyo qurilmoqda. Tez orada eshiklar ochiladi!", "The virtual world is being built. The doors open soon!"),
            ["menu.zone_soon"] = ("{0}: tez orada ochiladi", "{0}: opening soon"),
            ["menu.no_notifications"] = ("Yangi bildirishnomalar yo'q", "No new notifications"),
            ["menu.quit_title"] = ("Chiqasizmi?", "Quit game?"),
            ["menu.quit_message"] = ("Virtual dunyodan chiqmoqchimisiz?", "Are you sure you want to leave the virtual world?"),
            ["menu.help_title"] = ("Yordam", "Help"),
            ["menu.help_message"] = (
                "Qahramonni sichqoncha bilan aylantiring. Pastdagi kartalar orqali shahar zonalarini ko'ring. Esc — orqaga yoki chiqish. Til — yuqori o'ng burchakda.",
                "Drag the character to rotate it. Use the cards below to look at the city zones. Esc goes back or quits. Change the language in the top right corner."),
            ["menu.profile_missing_title"] = ("Profil topilmadi", "Profile not found"),
            ["menu.profile_missing_message"] = ("Profilingiz serverda yo'q. Davom etish uchun yangi qahramon yarating.",
                "Your player profile no longer exists on the server. Create a new character to continue."),
            ["menu.create_character"] = ("Qahramon yaratish", "Create character"),

            // ---------------- Zonalar
            ["zone.shops"] = ("Magazinlar", "Shops"),
            ["zone.shops.sub"] = ("Xarid va brendlar", "Shopping and brands"),
            ["zone.education"] = ("O'quv markazlari", "Education centers"),
            ["zone.education.sub"] = ("Bilim va rivojlanish", "Learning and growth"),
            ["zone.business"] = ("Biznes markazi", "Business center"),
            ["zone.business.sub"] = ("Ish va hamkorlik", "Work and partnership"),
            ["zone.entertainment"] = ("Ko'ngilochar zona", "Entertainment zone"),
            ["zone.entertainment.sub"] = ("O'yin va tadbirlar", "Games and events"),
            ["zone.community"] = ("Hamjamiyat", "Community"),
            ["zone.community.sub"] = ("Do'stlar va muloqot", "Friends and chat"),

            // ---------------- Sozlamalar
            ["settings.title"] = ("Sozlamalar", "Settings"),
            ["settings.display"] = ("Ekran rejimi", "Display mode"),
            ["settings.size"] = ("Oyna o'lchami", "Window size"),
            ["settings.quality"] = ("Grafika sifati", "Graphics quality"),
            ["settings.vsync"] = ("V-Sync", "V-Sync"),
            ["settings.volume"] = ("Ovoz", "Volume"),
            ["settings.language"] = ("Til", "Language"),
            ["settings.fullscreen"] = ("To'liq ekran", "Fullscreen"),
            ["settings.windowed"] = ("Oynali", "Windowed"),
            ["settings.on"] = ("Yoqilgan", "On"),
            ["settings.off"] = ("O'chiq", "Off"),
            ["settings.done"] = ("Tayyor", "Done"),
            ["quality.0"] = ("Juda past", "Very low"),
            ["quality.1"] = ("Past", "Low"),
            ["quality.2"] = ("O'rta", "Medium"),
            ["quality.3"] = ("Yuqori", "High"),
            ["quality.4"] = ("Juda yuqori", "Very high"),
            ["quality.5"] = ("Ultra", "Ultra"),

            // ---------------- Qahramon yaratish
            ["create.eyebrow"] = ("YANGI O'YINCHI", "NEW PLAYER"),
            ["create.title"] = ("Qahramoningizni\nyarating", "Create your\ncharacter"),
            ["create.subtitle"] = ("Nickname va avatar tanlang, keyin rasmdan yuzingizni qo'shing.", "Pick a nickname and an avatar, then add your face from a photo."),
            ["edit.eyebrow"] = ("SOZLASH", "CUSTOMIZE"),
            ["edit.title"] = ("Qahramonni\nsozlash", "Edit your\ncharacter"),
            ["edit.subtitle"] = ("Boshqa avatar tanlang yoki yuzingizni yangilang.", "Choose another avatar or update your face."),
            ["create.gender_male"] = ("Erkak · pasportdan", "Male · from passport"),
            ["create.gender_female"] = ("Ayol · pasportdan", "Female · from passport"),
            ["create.nickname"] = ("NICKNAME", "NICKNAME"),
            ["create.nickname_placeholder"] = ("Nickname kiriting", "Enter a nickname"),
            ["create.your_nickname"] = ("Sizning nickname", "Your nickname"),
            ["create.avatar"] = ("AVATAR", "AVATAR"),
            ["create.face"] = ("YUZ", "FACE"),
            ["create.help_default"] = ("3–16 belgi: harflar, raqamlar va _. Harf bilan boshlanadi.", "3–16 characters: letters, numbers and _. Starts with a letter."),
            ["create.checking"] = ("Tekshirilmoqda…", "Checking availability…"),
            ["create.available"] = ("Nickname bo'sh", "Nickname is available"),
            ["create.taken"] = ("Bu nickname band.", "This nickname is already taken."),
            ["create.taken_race"] = ("Kimdir hozirgina bu nickname'ni oldi. Boshqasini tanlang.", "Someone just took this nickname. Try another one."),
            ["create.invalid"] = ("Bu nickname'ni ishlatib bo'lmaydi.", "This nickname can't be used."),
            ["create.length"] = ("3–16 ta belgi bo'lishi kerak.", "Use 3–16 characters."),
            ["create.pattern"] = ("Harf bilan boshlang. Faqat lotin harflari, raqamlar va _.", "Start with a letter. Use only letters, numbers and _."),
            ["create.reserved"] = ("Bu nom band qilingan.", "This name is reserved."),
            ["create.offline"] = ("Serverga ulanib bo'lmadi. Qayta urinilmoqda…", "Can't reach the server. Retrying…"),
            ["create.create"] = ("Qahramon yaratish", "Create character"),
            ["create.creating"] = ("Yaratilmoqda…", "Creating…"),
            ["create.welcome"] = ("Xush kelibsiz, {0}", "Welcome, {0}"),
            ["create.save"] = ("Saqlash", "Save changes"),
            ["create.saving"] = ("Saqlanmoqda…", "Saving…"),
            ["create.saved"] = ("Saqlandi", "Saved"),
            ["create.nickname_locked"] = ("Nickname'ni o'zgartirib bo'lmaydi.", "Your nickname can't be changed."),
            ["create.error_network"] = ("Serverga ulanib bo'lmadi. Internetni tekshirib, qayta urining.", "Can't reach the server. Check your connection and try again."),
            ["create.error_avatar"] = ("Bu avatar pasport ma'lumotlaringizga mos emas.", "This avatar doesn't match your passport data."),
            ["create.error_limit"] = ("Juda ko'p urinish. Bir daqiqa kutib, qayta urining.", "Too many attempts. Wait a minute and try again."),
            ["create.error_server"] = ("Serverda xato yuz berdi. Qayta urining.", "Something went wrong on the server. Try again."),
            ["create.error_profile"] = ("Profilingiz serverda topilmadi.", "Your profile was not found on the server."),
            ["create.drag_hint"] = ("Aylantirish uchun torting · Yaqinlashtirish uchun g'ildirak", "Drag to rotate · Scroll to zoom"),
            ["create.view_front"] = ("Old", "Front"),
            ["create.view_side"] = ("Yon", "Side"),
            ["create.view_back"] = ("Orqa", "Back"),
            ["create.quit_message"] = ("Qahramon hali yaratilmadi. Chiqmoqchimisiz?", "Your character is not created yet. Are you sure you want to quit?"),

            // ---------------- Avatarlar
            ["avatar.info"] = ("{0} · {1} sm", "{0} · {1} cm"),
            ["avatar.athletic"] = ("Atletik", "Athletic"),
            ["avatar.slim"] = ("Ozg'in", "Slim"),
            ["avatar.strong"] = ("Baquvvat", "Strong"),
            ["avatar.classic"] = ("Klassik", "Classic"),
            ["avatar.sporty"] = ("Sportcha", "Sporty"),
            ["avatar.petite"] = ("Nozik", "Petite"),
            ["avatar.casual"] = ("Oddiy", "Casual"),
            ["avatar.tall"] = ("Baland bo'yli", "Tall"),
            ["avatar.elegant"] = ("Nafis", "Elegant"),

            // ---------------- Yuz
            ["face.take"] = ("Suratga olish", "Take photo"),
            ["face.upload"] = ("Rasm yuklash", "Upload photo"),
            ["face.hint"] = ("Kameraga to'g'ri qarang, yorug' joyda, ko'zoynaksiz.", "Look straight at the camera, good light, no glasses."),
            ["face.on_avatar"] = ("Yuzingiz qahramonga qo'yilgan.", "Your face is on the avatar."),
            ["face.added"] = ("Yuz qo'shildi. G'ildirak bilan yaqinlashtiring.", "Face added. Scroll to zoom in or out."),
            ["face.finding"] = ("Yuz qidirilmoqda…", "Finding your face…"),
            ["face.no_camera"] = ("Kamera topilmadi. Rasm yuklang.", "No camera found. Upload a photo instead."),
            ["face.bad_file"] = ("Faylni ochib bo'lmadi. JPG yoki PNG rasm tanlang.", "Can't open this file. Use a JPG or PNG photo."),
            ["face.not_found"] = ("Yuz topilmadi. Yuz aniq ko'rinadigan, yorug' rasm tanlang.", "No face found. Use a clear, front-facing photo with good light."),
            ["face.not_clear"] = ("Yuz yetarlicha aniq emas. Kameraga to'g'ri qarang.", "The face is not clear enough. Look straight at the camera."),
            ["face.error"] = ("Nimadir xato ketdi. Boshqa rasm bilan urining.", "Something went wrong. Try another photo."),
            ["face.modal_title"] = ("Suratga olish", "Take a photo"),
            ["face.modal_hint"] = ("Yuzingizni oval ichiga joylang va to'g'ri qarang.", "Fit your face inside the oval and look straight."),
            ["face.camera_starting"] = ("Kamera yoqilmoqda…", "The camera is starting…"),
            ["face.capture"] = ("Suratga olish", "Capture"),
            ["face.dialog_title"] = ("Yuzingiz rasmini tanlang", "Choose a photo of your face"),
        };
    }
}

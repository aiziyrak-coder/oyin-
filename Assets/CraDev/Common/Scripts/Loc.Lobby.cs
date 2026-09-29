using System.Collections.Generic;

namespace CraDev
{
    // Lobby bosh ekrani, do'stlar paneli, tadbir kartasi va lobby guruhi (party) matnlari.
    // Loc.cs dagi asosiy jadvalga Register orqali qo'shiladi. Asosiy jadvalda bor kalit bu yerda qayta
    // yozilsa ham asosiy jadval ustun turadi (Loc.T avval Table'ni o'qiydi): shuning uchun faqat yangi kalitlar.
    public static partial class Loc
    {
        static readonly Dictionary<string, (string uz, string en)> LobbyTable = Register(new Dictionary<string, (string uz, string en)>
        {
            // ---------------- Bosh ekran (o'ng ustun): "siz" murojaati, lobby.caption bilan bir xil
            ["lobby.welcome_to"] = ("XUSH KELIBSIZ", "WELCOME TO"),
            ["lobby.home.sign"] = ("—   O'yin boshlansin   —", "—   Game On   —"),
            ["lobby.home.promo"] = ("Yangi sarguzasht\nsizni kutmoqda", "Your next adventure\nawaits"),
            ["lobby.home.promo_hint"] = ("Do'stlaringiz bilan birga\nkashf eting", "Explore together\nwith your friends"),
            ["lobby.home.your_world"] = ("O'z dunyongiz", "Your world"),

            // ---------------- Keyingi tadbir kartasi va batafsil oyna
            ["lobby.event.today"] = ("Bugun", "Today"),
            ["lobby.event.tomorrow"] = ("Ertaga", "Tomorrow"),
            ["lobby.weekday.0"] = ("Yakshanba", "Sunday"),
            ["lobby.weekday.1"] = ("Dushanba", "Monday"),
            ["lobby.weekday.2"] = ("Seshanba", "Tuesday"),
            ["lobby.weekday.3"] = ("Chorshanba", "Wednesday"),
            ["lobby.weekday.4"] = ("Payshanba", "Thursday"),
            ["lobby.weekday.5"] = ("Juma", "Friday"),
            ["lobby.weekday.6"] = ("Shanba", "Saturday"),
            ["lobby.event.live"] = ("Hozir · {0} gacha", "Live · until {0}"),
            ["lobby.event.live_note"] = ("Tadbir hozir davom etmoqda.", "The event is happening right now."),
            ["lobby.event.starts_in"] = ("Boshlanishiga {0} qoldi.", "Starts in {0}."),
            ["lobby.event.minutes"] = ("{0} daqiqa", "{0} min"),
            ["lobby.event.hours"] = ("{0} soat", "{0} h"),
            ["lobby.event.hours_minutes"] = ("{0} soat {1} daqiqa", "{0} h {1} min"),
            ["lobby.event.days"] = ("{0} kun", "{0} days"),
            ["lobby.event.none"] = ("Hozircha rejada yo'q", "None scheduled"),
            ["lobby.event.offline"] = ("Server bilan aloqa yo'q", "No connection"),

            // ---------------- Do'stlik so'rovlari: bildirishnoma
            ["lobby.requests.new_one"] = ("{0} sizga do'stlik so'rovi yubordi", "{0} sent you a friend request"),
            ["lobby.requests.new_many"] = ("{0} ta yangi do'stlik so'rovi keldi", "{0} new friend requests"),
            ["lobby.requests.pending_many"] = ("Sizda {0} ta javob kutayotgan do'stlik so'rovi bor", "You have {0} pending friend requests"),

            // ---------------- Do'stlar paneli: tugma izohlari, tasdiqlash va natijalar
            ["lobby.tool.drawer"] = ("Do'stlar panelini yashirish yoki ochish", "Hide or show the friends panel"),
            ["lobby.tool.quit"] = ("O'yindan chiqish", "Quit game"),
            ["lobby.friend.add_hint"] = ("Do'stlik so'rovi yuborish", "Send a friend request"),
            ["lobby.friend.accept_hint"] = ("So'rovni qabul qilish", "Accept the request"),
            ["lobby.friend.decline_hint"] = ("So'rovni rad etish", "Decline the request"),
            ["lobby.friend.cancel_hint"] = ("So'rovni qaytarib olish", "Withdraw the request"),
            ["lobby.friend.remove_hint"] = ("Do'stlar ro'yxatidan o'chirish", "Remove from friends"),
            ["lobby.friend.decline_confirm"] = ("{0} yuborgan do'stlik so'rovini rad etasizmi?", "Decline the friend request from {0}?"),
            ["lobby.friend.cancel_confirm"] = ("{0} uchun yuborilgan do'stlik so'rovi qaytarib olinsinmi?", "Withdraw your friend request to {0}?"),
            ["lobby.friend.withdraw"] = ("Qaytarib olish", "Withdraw"),
            ["lobby.friend.remove_confirm"] = ("{0} do'stlaringiz ro'yxatidan o'chirilsinmi?", "Remove {0} from your friends?"),
            ["lobby.friend.declined"] = ("{0} so'rovi rad etildi", "Declined the request from {0}"),
            ["lobby.friend.cancelled"] = ("{0} uchun so'rov qaytarib olindi", "Request to {0} withdrawn"),
            ["lobby.friend.not_found"] = ("Bunday o'yinchi topilmadi", "Player not found"),
            ["lobby.friend.self"] = ("O'zingizni do'st qilib qo'sha olmaysiz", "You can't add yourself"),
            ["lobby.friend.no_request"] = ("Bu so'rov endi mavjud emas", "This request is no longer available"),
            ["lobby.friend.invalid_query"] = ("Qidiruv 1–24 belgidan iborat bo'lsin", "Search must be 1–24 characters"),
            ["lobby.error.rate_limited"] = ("Juda ko'p urinish. Bir oz kutib, qayta urinib ko'ring.", "Too many attempts. Please wait a moment and try again."),
            ["lobby.error.server"] = ("Serverda xatolik. Birozdan keyin qayta urinib ko'ring.", "Server error. Please try again shortly."),

            // ---------------- Lobby guruhi (party): foydalanuvchi uni "lobby" deb ataydi (USTA.md)
            ["lobby.party.invite_hint"] = ("Lobbyga taklif qilish", "Invite to party"),
            ["lobby.party.member"] = ("Lobbyingizda", "In your party"),
            ["lobby.party.leave_confirm"] = ("Lobbyni tark etasizmi? Mezbon chiqsa, lobby yopiladi.", "Leave this party? If the host leaves, the party closes."),
            // Server qaytarishi mumkin bo'lgan, asosiy jadvalda yo'q xato kodlari (xom kalit ko'rinmasin)
            ["party.error.server_error"] = ("Serverda xatolik. Qayta urinib ko'ring.", "Server error. Please try again."),
            ["party.error.bad_json"] = ("So'rov bajarilmadi. Qayta urinib ko'ring.", "The request failed. Please try again."),
            ["party.error.bad_request"] = ("So'rov bajarilmadi. Qayta urinib ko'ring.", "The request failed. Please try again."),
            ["party.error.invalid_ping"] = ("So'rov bajarilmadi. Qayta urinib ko'ring.", "The request failed. Please try again."),
            ["party.error.not_found"] = ("So'rov bajarilmadi. Qayta urinib ko'ring.", "The request failed. Please try again."),
        });
    }
}

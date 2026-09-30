using System.Collections.Generic;

namespace CraDev
{
    public static partial class Loc
    {
        static readonly Dictionary<string, (string uz, string en)> PenaltyTable = Register(new Dictionary<string, (string uz, string en)>
        {
            ["penalty.title"] = ("PENALTI ARENASI", "PENALTY ARENA"),
            ["penalty.prompt"] = ("E  ·  Ikki kishilik penalti", "E  ·  Two-player penalty shootout"),
            ["penalty.join"] = ("Maydonga chiqish", "Enter the pitch"),
            ["penalty.close"] = ("Yopish · Esc", "Close · Esc"),
            ["penalty.leave"] = ("Maydonni tark etish", "Leave match"),
            ["penalty.confirm"] = ("Ha, o'yindan chiqish", "Yes, leave match"),
            ["penalty.rules"] = ("Ikki o'yinchi navbat bilan to'p tepadi va darvozani qo'riqlaydi. Har biriga 5 zarba. Durangda qo'shimcha juft zarbalar. Pul yoki coin tikilmaydi.", "Two players alternate shooting and goalkeeping. Five shots each, then paired sudden death. No money or coin wagering."),
            ["penalty.fair"] = ("Tanlovlar raqibga ko'rinmaydi. Ikkala o'yinchi tanlagach zarba boshlanadi. Har navbatga 30 soniya.", "Choices stay hidden. The shot starts when both players commit. Each turn allows 30 seconds."),
            ["penalty.waiting"] = ("Ikkinchi o'yinchi kutilmoqda…", "Waiting for another player…"),
            ["penalty.loading"] = ("Arena bilan ulanmoqda…", "Connecting to the arena…"),
            ["penalty.empty"] = ("Bo'sh o'rin", "Open place"),
            ["penalty.shooter"] = ("SIZ TO'P TEPASIZ", "YOU ARE SHOOTING"),
            ["penalty.keeper"] = ("SIZ DARVOZABONSIZ", "YOU ARE GOALKEEPING"),
            ["penalty.spectator"] = ("KUZATUVCHI", "SPECTATOR"),
            ["penalty.aim"] = ("Darvozada nishon tanlang", "Choose a target on the goal"),
            ["penalty.power"] = ("Zarba kuchi", "Shot power"),
            ["penalty.shoot"] = ("ZARBA · Space", "SHOOT · Space"),
            ["penalty.committed"] = ("Tanlov qabul qilindi. Raqib kutilmoqda…", "Choice locked. Waiting for opponent…"),
            ["penalty.left"] = ("Chap · A", "Left · A"),
            ["penalty.center"] = ("Markaz · Space", "Centre · Space"),
            ["penalty.right"] = ("O'ng · D", "Right · D"),
            ["penalty.keeper_hint"] = ("Darvozabon nigohida sakrash tomonini tanlang. Tanlovni o'zgartirib bo'lmaydi.", "Choose a dive from the goalkeeper's viewpoint. A committed choice cannot be changed."),
            ["penalty.flight"] = ("ZARBA!", "SHOT!"),
            ["penalty.goal"] = ("GOL!", "GOAL!"),
            ["penalty.save"] = ("DARVOZABON QAYTARDI", "SAVED"),
            ["penalty.miss"] = ("DARVOZADAN TASHQARI", "OFF TARGET"),
            ["penalty.timeout"] = ("ZARBA VAQTI TUGADI", "SHOT TIMED OUT"),
            ["penalty.finished"] = ("Uchrashuv yakunlandi", "Match finished"),
            ["penalty.abandoned"] = ("Raqib maydonni tark etdi", "A player left the match"),
            ["penalty.won"] = ("G'olib: {0}", "Winner: {0}"),
            ["penalty.draw"] = ("Durang", "Draw"),
            ["penalty.round"] = ("Zarbalar: {0}/5  —  {1}/5", "Attempts: {0}/5  —  {1}/5"),
            ["penalty.sudden"] = ("Qo'shimcha zarbalar · {0} — {1}", "Sudden death · {0} — {1}"),
            ["penalty.rematch"] = ("Qayta o'ynash", "Rematch"),
            ["penalty.voted"] = ("Sherik roziligi kutilmoqda", "Waiting for opponent's vote"),
            ["penalty.error"] = ("Ulanish uzildi. Qayta urinilmoqda…", "Connection lost. Retrying…"),
            ["penalty.changed"] = ("Holat yangilandi. Qayta tanlang.", "State changed. Please choose again."),
            ["penalty.full"] = ("Ikkala o'rin band. Uchrashuvni kuzatishingiz mumkin.", "Both places are occupied. You can watch the match."),
            ["penalty.busy"] = ("Avval boshqa o'yindagi o'rningizni tark eting.", "Leave your place in the other activity first."),
            ["penalty.far"] = ("Maydonga kirish uchun kirish belgisiga yaqinlashing.", "Move closer to the entry marker to join."),
            ["penalty.wait"] = ("Biroz kuting, so'ng qayta urining.", "Please wait and try again."),
            ["penalty.warning"] = ("Yuqori kuch va chekka nishon zarbani darvozadan chiqarishi mumkin.", "High power and edge targets can send the shot wide or over."),
            ["penalty.closed_note"] = ("Oynani yopish o'yinni tark etmaydi. E bilan qayta oching.", "Closing this view keeps your place. Press E to reopen."),
        });
    }
}

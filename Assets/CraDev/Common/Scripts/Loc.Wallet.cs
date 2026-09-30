using System.Collections.Generic;
namespace CraDev
{
    public static partial class Loc
    {
        static readonly Dictionary<string, (string uz, string en)> WalletTable = Register(new Dictionary<string, (string uz, string en)>
        {
            ["wallet.title"] = ("CDCoin hamyoni", "CDCoin wallet"),
            ["wallet.balance"] = ("Hisobingiz: {0} CDCoin", "Your balance: {0} CDCoin"),
            ["wallet.rate"] = ("1 CDCoin = 100 so'm", "1 CDCoin = 100 UZS"),
            ["wallet.pending"] = ("Click / Payme — hozircha ulanmagan. To'lov va coin xaridi hali yopiq.", "Click / Payme are not connected yet. Payments and coin purchases are unavailable."),
            ["wallet.unavailable"] = ("Hisobni yuklab bo'lmadi. Server ulanishini tekshiring.", "Could not load wallet. Check your server connection."),
            ["wallet.history"] = ("Oxirgi amallar", "Recent transactions"),
            ["wallet.empty"] = ("Hali amallar yo'q", "No transactions yet"),
            ["wallet.kind.topup"] = ("Hisob to'ldirish", "Top-up"),
            ["wallet.kind.group_payment"] = ("Guruh obunasi", "Group subscription"),
            ["wallet.kind.group_income"] = ("Guruhdan tushum", "Group income"),
            ["wallet.subscribe"] = ("Obunani tasdiqlash", "Confirm subscription"),
            ["wallet.already_processed"] = ("Bu to'lov avval bajarilgan. Yangi muddat uchun qayta obuna bo'ling.", "This payment was already processed. Subscribe again for a new period."),
            ["wallet.confirm_subscription"] = ("{1} guruhiga obuna uchun {0} CDCoin yechiladi. To'liq guruh egasiga o'tadi; komissiya 0%. Avtomatik yangilanmaydi. Tasdiqlaysizmi?", "Subscribe to {1} for {0} CDCoin? The owner receives the full amount; 0% commission. No automatic renewal."),
            ["groups.error.insufficient_coins"] = ("CDCoin yetarli emas.", "Insufficient CDCoin."),
            ["groups.error.balance_limit"] = ("Hisob chegarasiga yetildi. To'lov bajarilmadi.", "Balance limit reached. Payment was not made."),
            ["groups.error.legacy_currency"] = ("Bu guruh eski pul birligida. Egasi tahrirlash orqali CDCoin narxini belgilashi kerak.", "This group uses a legacy currency. Its owner must edit the group and set a CDCoin price."),
            ["groups.error.price_changed"] = ("Narx o'zgargan. Guruhni qayta oching.", "The price changed. Reopen the group."),
            ["groups.error.payment_required"] = ("A'zolik uchun CDCoin to'lovi kerak.", "Membership requires a CDCoin payment."),
            ["groups.error.idempotency_conflict"] = ("To'lov ma'lumotlari mos kelmadi. Guruhni qayta oching.", "Payment details conflict. Reopen the group."),
        });
    }
}

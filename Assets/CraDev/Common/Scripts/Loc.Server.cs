using System.Collections.Generic;

namespace CraDev
{
    /// <summary>Server bilan ishlash matnlari (nickname tekshiruvida server javob bera olmagan holatlar).</summary>
    public static partial class Loc
    {
        static readonly Dictionary<string, (string uz, string en)> ServerTable = Register(new Dictionary<string, (string uz, string en)>
        {
            ["create.check_busy"] = ("Tekshiruvlar juda ko'p. Bir oz kutib, qayta tekshiriladi…", "Too many checks right now. Checking again shortly…"),
            ["create.check_server_error"] = ("Serverda xato. Nickname qayta tekshirilmoqda…", "Server error. Checking the nickname again…"),
        });
    }
}

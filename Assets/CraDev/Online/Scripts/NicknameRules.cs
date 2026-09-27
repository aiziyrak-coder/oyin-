using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CraDev.Online
{
    /// <summary>
    /// Nickname qoidalari. Server'dagi Server/src/nickname.js bilan bir xil bo'lishi kerak:
    /// o'yinchi darhol javob oladi, lekin yakuniy tekshiruv baribir server'da bo'ladi.
    /// </summary>
    public static class NicknameRules
    {
        public const int MinLength = 3;
        public const int MaxLength = 16;

        static readonly Regex Pattern = new Regex("^[A-Za-z][A-Za-z0-9_]*$");

        static readonly HashSet<string> Reserved = new HashSet<string>
        {
            "admin", "administrator", "moderator", "support", "system", "server",
            "cradev", "cdcgroup", "gamemaster", "gm", "null", "undefined",
        };

        /// <summary>Kiritishda ruxsat etilgan belgi: lotin harfi, raqam yoki "_".</summary>
        public static bool IsAllowedChar(char c) =>
            (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';

        /// <summary>Nickname'ni tekshiradi. Xato bo'lsa, o'yinchiga ko'rsatiladigan xabarni qaytaradi.</summary>
        public static bool Validate(string raw, out string nickname, out string message)
        {
            nickname = (raw ?? "").Trim();
            message = null;
            if (nickname.Length < MinLength || nickname.Length > MaxLength)
                message = Loc.T("create.length");
            else if (!Pattern.IsMatch(nickname))
                message = Loc.T("create.pattern");
            else if (Reserved.Contains(nickname.ToLowerInvariant()))
                message = Loc.T("create.reserved");
            return message == null;
        }
    }
}

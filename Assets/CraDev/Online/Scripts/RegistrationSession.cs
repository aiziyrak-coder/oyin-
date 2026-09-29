namespace CraDev.Online
{
    /// <summary>Server nickname tekshiruvining natijasi (<see cref="RegistrationSession.Classify"/>).</summary>
    public enum NicknameCheck
    {
        Available,
        Taken,
        Reserved,
        Invalid,
        /// <summary>Server bilan aloqa yo'q (o'chiq, internet yo'q, vaqt tugadi).</summary>
        Offline,
        /// <summary>429: bu manzildan tekshiruvlar juda ko'p (klub, NAT) - kutib qayta uriniladi.</summary>
        Busy,
        /// <summary>5xx yoki tushunarsiz javob: nickname haqida hech narsa ma'lum emas, qayta uriniladi.</summary>
        ServerError,
    }

    /// <summary>
    /// Ro'yxatdan o'tish davomida bosqichlar orasida uzatiladigan ma'lumotlar.
    /// Jins pasport bosqichida aniqlanadi va o'yinchi uni o'zgartira olmaydi.
    /// </summary>
    public static class RegistrationSession
    {
        /// <summary>"male" yoki "female". Bo'sh bo'lsa, pasport bosqichi hali o'tilmagan.</summary>
        public static string Gender { get; set; }

        public static bool HasGender => Gender == "male" || Gender == "female";

        /// <summary>
        /// Nickname tekshiruvi javobini holatga aylantiradi. Faqat 200 javobi nickname haqida: 429 va 5xx "band" yoki
        /// "noto'g'ri" emas, qayta urinish kerak bo'lgan holat (<see cref="IsRetryable"/>).
        /// </summary>
        public static NicknameCheck Classify(ApiResult<AvailabilityResponse> result)
        {
            if (result.NetworkError)
                return NicknameCheck.Offline;
            if (result.Status == 429)
                return NicknameCheck.Busy;
            if (result.Status != 200 || result.Data == null)
                return NicknameCheck.ServerError;
            if (result.Data.available)
                return NicknameCheck.Available;
            return result.Data.reason == "taken" ? NicknameCheck.Taken
                : result.Data.reason == "reserved" ? NicknameCheck.Reserved
                : NicknameCheck.Invalid;
        }

        /// <summary>Nickname haqida javob yo'q: bir ozdan keyin qayta tekshirish kerak.</summary>
        public static bool IsRetryable(NicknameCheck check) =>
            check == NicknameCheck.Offline || check == NicknameCheck.Busy || check == NicknameCheck.ServerError;

        /// <summary>Qayta tekshirishgacha kutish (soniya): cheklovga tushganda uzoqroq, aks holda cheklov uzilmaydi.</summary>
        public static float RetryDelay(NicknameCheck check) => check == NicknameCheck.Busy ? 12f : 4f;

        /// <summary>O'yinchiga ko'rsatiladigan matn kaliti (<c>Loc.T</c>).</summary>
        public static string MessageKey(NicknameCheck check) => check switch
        {
            NicknameCheck.Available => "create.available",
            NicknameCheck.Taken => "create.taken",
            NicknameCheck.Reserved => "create.reserved",
            NicknameCheck.Invalid => "create.invalid",
            NicknameCheck.Busy => "create.check_busy",
            NicknameCheck.ServerError => "create.check_server_error",
            _ => "create.offline",
        };
    }
}

namespace CraDev.Online
{
    /// <summary>
    /// Ro'yxatdan o'tish davomida bosqichlar orasida uzatiladigan ma'lumotlar.
    /// Jins pasport bosqichida aniqlanadi va o'yinchi uni o'zgartira olmaydi.
    /// </summary>
    public static class RegistrationSession
    {
        /// <summary>"male" yoki "female". Bo'sh bo'lsa, pasport bosqichi hali o'tilmagan.</summary>
        public static string Gender { get; set; }

        public static bool HasGender => Gender == "male" || Gender == "female";
    }
}

using System.Collections.Generic;

namespace CraDev
{
    // Lobby ovozli chati (mikrofon/karnay), "Lobbydan chiqish" tugmasi va salom pufagi matnlari.
    public static partial class Loc
    {
        static readonly Dictionary<string, (string uz, string en)> VoiceTable = Register(new Dictionary<string, (string uz, string en)>
        {
            ["voice.mic"] = ("Mikrofon (M)", "Microphone (M)"),
            ["voice.speaker"] = ("Karnay: boshqalarni eshitish", "Speaker: hear others"),
            ["voice.mic_on"] = ("Mikrofon yoqildi", "Microphone on"),
            ["voice.mic_off"] = ("Mikrofon o'chirildi", "Microphone off"),
            ["voice.speaker_on"] = ("Karnay va mikrofon yoqildi (quloqchin tavsiya etiladi)", "Speaker and microphone on (headphones recommended)"),
            ["voice.speaker_on_nomic"] = ("Karnay yoqildi", "Speaker on"),
            ["voice.speaker_off"] = ("Karnay o'chirildi: mikrofon ham o'chdi", "Speaker off: microphone is off too"),
            ["voice.need_speaker"] = ("Mikrofon uchun avval karnayni yoqing", "Turn on the speaker first to use the microphone"),
            ["voice.no_mic"] = ("Mikrofon topilmadi", "No microphone found"),
            ["voice.mic_error"] = ("Mikrofonni ochib bo'lmadi: ulanish va ruxsatlarni tekshiring", "Could not open the microphone: check the connection and permissions"),
            ["voice.leave_lobby"] = ("Lobbydan chiqish", "Leave lobby"),
            ["greet.hello"] = ("Assalomu alaykum!", "Hello!"),
        });
    }
}

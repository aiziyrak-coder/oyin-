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
            ["voice.speaker_on"] = ("Karnay yoqildi (quloqchin tavsiya etiladi)", "Speaker on (headphones recommended)"),
            ["voice.volume"] = ("Ovozli chat balandligi", "Voice chat volume"),
            ["voice.device"] = ("Mikrofon qurilmasi", "Microphone device"),
            ["voice.device_default"] = ("Avtomatik", "Automatic"),
            ["voice.device_hint"] = ("Tanlangan qurilma uzilsa mikrofon o'chadi", "Microphone stops if the selected device disconnects"),
            ["voice.ptt"] = ("Bosib gapirish", "Push to talk"),
            ["voice.ptt_hint"] = ("V ni ushlab gapiring. M — mikrofonni yoqish/o'chirish", "Hold V to talk. M toggles the microphone"),
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

namespace CraDev
{
    public static partial class Loc
    {
        static readonly System.Collections.Generic.Dictionary<string,(string uz,string en)> CityTable =
            Register(new System.Collections.Generic.Dictionary<string,(string uz,string en)>
            {
                ["city.connecting"]=("Olamga ulanmoqda…","Connecting to the world…"),
                ["city.login"]=("Avval profilingizga kiring","Sign in to your profile first"),
                ["city.refresh"]=("Profilni yangilash uchun lobbyga qayting","Return to the lobby to refresh your profile"),
                ["city.full"]=("Olam band. Qayta urinmoqda…","World is full. Retrying…"),
                ["city.unavailable"]=("Serverga ulanib bo‘lmadi. Qayta urinmoqda…","Server unavailable. Retrying…"),
                ["city.reconnecting"]=("Aloqa uzildi. Qayta ulanmoqda…","Connection lost. Reconnecting…"),
                ["city.shared"]=("Umumiy olam","Shared world"),
                ["city.presence"]=("{0} o‘yinchi  ·  {1} ms\nShaxmat markazi — shimolda","{0} players  ·  {1} ms\nChess pavilion — north"),
                ["city.voice"]=("M  Mikrofon: {0}   N  Ovoz: {1}\nYaqin ovoz: 18 m  ·  {2}\nQuloqchin tavsiya etiladi","M  Microphone: {0}   N  Audio: {1}\nProximity: 18 m  ·  {2}\nHeadphones recommended"),
                ["city.on"]=("yoqilgan","on"), ["city.off"]=("o‘chiq","off"),
                ["city.ptt"]=("V: bosib gapirish","V: push to talk"),
                ["city.open_mic"]=("ochiq mikrofon rejimi","open microphone mode"),
                ["city.voice.connect"]=("Avval olamga ulaning","Connect to the world first"),
                ["city.voice.speaker"]=("Avval karnayni yoqing","Turn on audio first"),
                ["city.voice.missing"]=("Mikrofon topilmadi","Microphone not found"),
                ["city.voice.test"]=("Sinovda mikrofon yozilmaydi","Microphone capture is disabled in tests"),
                ["city.voice.error"]=("Mikrofon ochilmadi. Sozlamalarni tekshiring","Microphone unavailable. Check your settings"),
                ["city.voice.signal"]=("Mikrofondan signal kelmadi","No signal from microphone"),
            });
    }
}

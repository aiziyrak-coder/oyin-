using System.Collections.Generic;
namespace CraDev
{
    public static partial class Loc
    {
        static readonly Dictionary<string,(string uz,string en)> ChatTable=Register(new Dictionary<string,(string uz,string en)>{
            ["chat.title"]=("Yozishmalar","Messages"),["chat.choose"]=("Do'st yoki guruhni tanlang","Choose a friend or group"),
            ["chat.empty_contacts"]=("Avval do'st qo'shing yoki guruhga kiring","Add a friend or join a group first"),
            ["chat.placeholder"]=("Xabar yozing...","Write a message..."),["chat.send"]=("Yuborish","Send"),
            ["chat.older"]=("Oldingi xabarlar","Older messages"),
            ["chat.latest"]=("So'nggi xabarlarga qaytish","Back to latest messages"),
            ["chat.connection_error"]=("Ulanishni tekshiring. Yuborilmagan matn saqlandi.","Check your connection. Unsent text was kept."),
            ["chat.forbidden"]=("Yozishma uchun do'stlik yoki faol guruh a'zoligi kerak","Chat requires friendship or active group membership"),
            ["chat.slow_down"]=("Juda tez yuboryapsiz. Biroz kuting.","Sending too quickly. Please wait."),
        });
    }
}

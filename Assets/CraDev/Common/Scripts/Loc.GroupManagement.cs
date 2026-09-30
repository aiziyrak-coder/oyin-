using System.Collections.Generic;
namespace CraDev
{
    public static partial class Loc
    {
        static readonly Dictionary<string,(string uz,string en)> GroupManagementTable=Register(new Dictionary<string,(string uz,string en)>{
            ["groups.edit"]=("Guruhni tahrirlash","Edit group"),["groups.save"]=("Saqlash","Save"),["groups.saved"]=("Guruh saqlandi","Group saved"),
            ["groups.transfer"]=("Egalikni topshirish","Transfer ownership"),
            ["groups.transfer_hint"]=("Yangi egani tanlang. Siz admin bo'lib qolasiz. Keyingi CDCoin tushumlari yangi egaga o'tadi.","Choose the new owner. You will remain an admin. Future CDCoin income goes to the new owner."),
            ["groups.transfer_confirm"]=("Egalikni {0} ga topshirasizmi? Bu amalni faqat yangi ega qaytara oladi.","Transfer ownership to {0}? Only the new owner can transfer it back."),
            ["groups.done.transfer"]=("Egalik {0} ga topshirildi","Ownership transferred to {0}"),
        });
    }
}

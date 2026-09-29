using System.Collections.Generic;

namespace CraDev
{
    /// <summary>Avatar studiyasi va jonli yuz skaneri matnlari (ikki tilda).</summary>
    public static partial class Loc
    {
        static readonly Dictionary<string, (string uz, string en)> AvatarTable = Register(new Dictionary<string, (string uz, string en)>
        {
            // ---------------- Lobby: avatar studiyasi
            ["lobby.avatar"] = ("Avatar", "Avatar"),
            ["studio.title"] = ("Avatar studiyasi", "Avatar studio"),
            ["studio.subtitle"] = ("Qahramoningizni va yuzingizni tanlang", "Choose your character and your face"),
            ["studio.model"] = ("QAHRAMON", "CHARACTER"),
            ["studio.face"] = ("YUZ", "FACE"),
            ["studio.scan"] = ("Jonli skaner", "Live face scan"),
            ["studio.remove"] = ("Olib tashlash", "Remove"),
            ["studio.save"] = ("Saqlash", "Save"),
            ["studio.saved"] = ("Avatar saqlandi", "Avatar saved"),
            ["studio.view_body"] = ("Butun bo'y", "Full body"),
            ["studio.view_face"] = ("Yuz", "Face"),
            ["studio.face_none"] = ("Yuz qo'shilmagan. Jonli skanerlang yoki rasm yuklang.", "No face yet. Scan it live or upload a photo."),
            ["studio.face_new"] = ("Yangi yuz qahramonda. Saqlashni unutmang.", "New face on your character. Don't forget to save."),
            ["studio.face_removed"] = ("Saqlasangiz, yuz olib tashlanadi.", "The face will be removed when you save."),
            ["studio.privacy"] = ("Yuz rasmi faqat shu kompyuterda saqlanadi, serverga yuborilmaydi.", "Your face photo stays on this computer and is never uploaded."),
            ["studio.discard_title"] = ("O'zgarishlar saqlanmagan", "Unsaved changes"),
            ["studio.discard_message"] = ("Qahramon va yuzdagi o'zgarishlar bekor qilinsinmi?", "Discard the changes to your character and face?"),
            ["studio.discard"] = ("Bekor qilish", "Discard"),
            ["studio.keep_editing"] = ("Tahrirda qolish", "Keep editing"),

            // ---------------- Jonli yuz skaneri
            ["face.scan_title"] = ("Jonli yuz skaneri", "Live face scan"),
            ["face.hint_find"] = ("Yuzingizni oval ichiga joylang", "Place your face inside the oval"),
            ["face.hint_closer"] = ("Kameraga biroz yaqinroq keling", "Move a little closer to the camera"),
            ["face.hint_back"] = ("Kameradan biroz uzoqlashing", "Move back a little"),
            ["face.hint_center"] = ("Yuzingizni oval markaziga keltiring", "Center your face in the oval"),
            ["face.hint_straight"] = ("Kameraga to'g'ri qarang, boshingizni qiyshaytirmang", "Look straight at the camera and keep your head level"),
            ["face.hint_light"] = ("Yorug'lik yetarli emas: yuzingiz yaxshi yoritilsin", "Not enough light: make sure your face is well lit"),
            ["face.hint_still"] = ("Qimirlamay turing", "Hold still"),
            ["face.hint_good"] = ("Zo'r! Suratga olish mumkin", "Great! Ready to capture"),
            ["face.hint_countdown"] = ("Qimirlamang, suratga olinmoqda…", "Hold still, taking the photo…"),
            ["face.preparing"] = ("Skaner tayyorlanmoqda…", "Getting the scanner ready…"),
            ["face.review"] = ("Natijani 3D qahramonda ko'ring. Yoqsa, Ishlatish tugmasini bosing.", "Check the result on your 3D character. If you like it, press Use."),
            ["face.use"] = ("Ishlatish", "Use"),
            ["face.retake"] = ("Qayta olish", "Retake"),
            ["face.choose_other"] = ("Boshqa rasm", "Another photo"),
            ["face.retry"] = ("Qayta urinish", "Try again"),
            ["face.camera_failed"] = ("Kamera ochilmadi. U boshqa dasturda (Zoom, Teams) band bo'lishi yoki Windows maxfiylik sozlamalarida o'chirilgan bo'lishi mumkin. Qayta urining yoki rasm yuklang.",
                "Couldn't start the camera. Another app (Zoom, Teams) may be using it, or camera access is turned off in Windows privacy settings. Try again or upload a photo."),
            ["face.camera_lost"] = ("Kamera javob bermay qoldi. Ulanganini tekshirib, qayta urining.", "The camera stopped responding. Check that it is connected and try again."),
            ["face.switch_camera"] = ("Kamera: {0}", "Camera: {0}"),
            ["face.privacy"] = ("Rasm faqat shu kompyuterda saqlanadi.", "The photo stays on this computer."),
            ["face.save_failed"] = ("Yuzni saqlab bo'lmadi. Diskda bo'sh joy borligini tekshiring.", "Couldn't save your face. Check that there is free disk space."),
            ["face.draft"] = ("Yuz qahramonga qo'yildi. Qahramon yaratilganda yoki saqlanganda yoziladi.", "Face applied. It is saved when you create or save your character."),
        });
    }
}

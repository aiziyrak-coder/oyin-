using System.Collections.Generic;

namespace CraDev
{
    public static partial class Loc
    {
        static readonly Dictionary<string, (string uz, string en)> ChessTable = Register(new Dictionary<string, (string uz, string en)>
        {
            ["chess.title"] = ("Shaxmatchilar maydonchasi", "Chess pavilion"),
            ["chess.table"] = ("Doska {0:00}", "Table {0:00}"),
            ["chess.prompt"] = ("E  ·  {0} — o'ynash yoki kuzatish", "E  ·  {0} — play or watch"),
            ["chess.connecting"] = ("Olam serveriga ulanmoqda…", "Connecting to the world…"),
            ["chess.loading"] = ("Doska yuklanmoqda…", "Loading board…"),
            ["chess.white"] = ("OQ DONALAR", "WHITE PIECES"),
            ["chess.black"] = ("QORA DONALAR", "BLACK PIECES"),
            ["chess.empty"] = ("Bo'sh o'rin", "Available seat"),
            ["chess.join_white"] = ("Oq bilan o'ynash", "Play as white"),
            ["chess.join_black"] = ("Qora bilan o'ynash", "Play as black"),
            ["chess.waiting"] = ("Ikkinchi shaxmatchi kutilmoqda", "Waiting for another player"),
            ["chess.your_turn"] = ("Sizning yurishingiz", "Your turn"),
            ["chess.white_turn"] = ("Oqlar yuradi", "White to move"),
            ["chess.black_turn"] = ("Qoralar yuradi", "Black to move"),
            ["chess.check"] = ("Shoh xavf ostida!", "Check!"),
            ["chess.checkmate"] = ("Mot", "Checkmate"),
            ["chess.stalemate"] = ("Pat — durang", "Stalemate — draw"),
            ["chess.draw"] = ("Durang", "Draw"),
            ["chess.abandoned"] = ("Partiya yakunlandi", "Game ended"),
            ["chess.won_white"] = ("Oqlar g'alaba qozondi", "White wins"),
            ["chess.won_black"] = ("Qoralar g'alaba qozondi", "Black wins"),
            ["chess.watching"] = ("Kuzatuvchi", "Spectating"),
            ["chess.hint"] = ("Avval donani, keyin yoritilgan katakni bosing. Yurishlar serverda tekshiriladi.", "Select a piece, then a highlighted square. Moves are verified by the server."),
            ["chess.leave_note"] = ("Oynani yopish o'rinni bo'shatmaydi. Stoldan uzoqlashish yoki o'rinni tark etish partiyani tugatadi.", "Closing this view keeps your seat. Leaving the seat or walking away ends your game."),
            ["chess.leave"] = ("O'rinni tark etish", "Leave seat"),
            ["chess.confirm_leave"] = ("Ha, partiyani tugatish", "Yes, end my game"),
            ["chess.reset"] = ("Yangi partiya", "New game"),
            ["chess.voted"] = ("Sherik tasdig'i kutilmoqda", "Waiting for opponent's vote"),
            ["chess.close"] = ("Yopish · Esc", "Close · Esc"),
            ["chess.promote"] = ("Piyoda qaysi donaga aylansin?", "Choose a promotion piece"),
            ["chess.queen"] = ("Farzin", "Queen"),
            ["chess.rook"] = ("Rux", "Rook"),
            ["chess.bishop"] = ("Fil", "Bishop"),
            ["chess.knight"] = ("Ot", "Knight"),
            ["chess.error"] = ("Ulanish uzildi. Qayta urinilmoqda…", "Connection lost. Retrying…"),
            ["chess.changed"] = ("Doska yangilandi. Yurishni qayta tanlang.", "The board changed. Select your move again."),
            ["chess.taken"] = ("Bu o'rin band. Boshqa rangni tanlang.", "That seat is taken. Choose the other side."),
            ["chess.leave_first"] = ("Avval oldingi doskadagi o'rinni tark eting.", "Leave your previous table first."),
            ["chess.too_far"] = ("O'ynash uchun stolga yaqinlashing.", "Move closer to the table to play."),
            ["chess.cannot_move"] = ("Bu yurishga ruxsat yo'q.", "That move is not allowed."),
            ["chess.slow"] = ("Biroz kuting, keyin qayta urining.", "Please wait a moment and retry."),
        });
    }
}

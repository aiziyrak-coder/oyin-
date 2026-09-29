using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using CraDev.Online;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    /// <summary>
    /// Chap ijtimoiy paneldagi "Guruhlar" bo'limi (Telegram'dagidek guruhlar, Server/src/groups.js).
    /// Do'stlar paneli (LobbyFriendsPanel) bilan bitta ro'yxat maydonini (rows) va qidiruv maydonini bo'lishadi:
    /// "groups" rejimida qidiruv maydoni guruh nomi yoki kodini qidiradi. Ko'rinishlar: ro'yxat (mening guruhlarim,
    /// yuborgan so'rovlarim yoki qidiruv natijalari), guruh sahifasi (a'zolar, so'rovlar, amallar), yangi guruh formasi.
    /// Pullik guruhda to'lov tizimi yo'q: "Obuna bo'lish" so'rov yuboradi, egasi to'lovni o'zi tekshirib tasdiqlaydi.
    /// </summary>
    public class LobbyGroupsPanel : MonoBehaviour
    {
        [SerializeField] Font font;
        [SerializeField] Font boldFont;
        [SerializeField] Sprite rounded, addIcon, removeIcon, acceptIcon, inviteIcon, adminIcon;
        [Tooltip("LobbyFriendsPanel bilan umumiy ro'yxat konteyneri (ScrollRect content).")]
        [SerializeField] RectTransform rows;
        [Tooltip("Guruh asboblari qatori: yangi guruh, kod bilan qo'shilish, yangilash (builder).")]
        [SerializeField] Button createButton, joinButton, refreshButton;
        [SerializeField] Text tooltip;

        public const string CodePrefix = "NW-";
        public const string LinkPrefix = "newworld://group/";
        const float RowHeight = 68, Gap = 6;
        static readonly Color RowFill = new Color32(41, 40, 40, 248);
        static readonly Color Muted = new Color32(160, 162, 172, 255);
        static readonly Color Soft = new Color32(206, 210, 218, 255);
        static readonly Color Green = new Color32(53, 211, 139, 255);
        static readonly Color Gold = new Color32(255, 215, 143, 255);
        static readonly Color Danger = new Color32(150, 42, 56, 235);
        static readonly Color InviteFill = new Color32(66, 59, 43, 255);
        static readonly Color FieldFill = new Color32(24, 24, 26, 255);

        MainMenuScreen lobby;
        LobbyFriendsPanel host;
        string view = "list", query = "", openCode, hint;
        GroupInfo[] mine = Array.Empty<GroupInfo>(), sent = Array.Empty<GroupInfo>(), results;
        GroupInfo detail;
        bool loading, busy, mineLoaded;
        Func<string> failure;
        int revision;
        // Yangi guruh formasi (tur almashtirilganda qayta chiziladi, yozilgan matn saqlanadi)
        string formName = "", formDescription = "", formPrice = "", formKind = "free", formCurrency = "UZS", formPeriod = "month";

        /// <summary>Guruhlar bo'limi hozir ko'rinib turibdi (panel "groups" rejimida).</summary>
        public bool Active { get; private set; }
        public string View => view;
        /// <summary>Men a'zo bo'lgan guruhlar soni (tab yonidagi son).</summary>
        public int MineCount => mine.Length;
        /// <summary>Men admin/ega bo'lgan guruhlardagi javob kutayotgan so'rovlar.</summary>
        public int PendingCount => mine.Sum(g => g.pending);
        public event Action CountsChanged;

        public void Begin(MainMenuScreen screen, LobbyFriendsPanel owner)
        {
            lobby = screen; host = owner;
            if (createButton != null) createButton.onClick.AddListener(OpenCreate);
            if (joinButton != null) joinButton.onClick.AddListener(() => host.FocusSearch(Loc.T("groups.join_hint")));
            if (refreshButton != null) refreshButton.onClick.AddListener(Refresh);
            StartCoroutine(LoadMineOnly());
        }

        IEnumerator LoadMineOnly()
        {
            ApiResult<GroupList> result = default;
            yield return lobby.Api.GroupsMine(PlayerProfile.Token, r => result = r);
            if (result.Ok) SetMine(result.Data);
        }

        void SetMine(GroupList list)
        {
            mine = list.groups ?? Array.Empty<GroupInfo>();
            sent = list.requests ?? Array.Empty<GroupInfo>();
            mineLoaded = true;
            CountsChanged?.Invoke();
        }

        /// <summary>Bo'lim ochildi yoki yopildi (LobbyFriendsPanel.SetMode).</summary>
        public void SetActive(bool on, string text)
        {
            Active = on;
            if (!on) { revision++; return; }
            query = text ?? "";
            // Oxirgi ma'lum ro'yxat darhol ko'rinadi (do'stlar qatorlari qolib ketmaydi), keyin yangilanadi
            if (mineLoaded || view == "create") Draw();
            if (view != "create") Load();
        }

        /// <summary>Qidiruv maydoni o'zgardi: ro'yxat ko'rinishiga qaytib qidiradi (bo'sh - mening guruhlarim).</summary>
        public void Search(string text)
        {
            query = (text ?? "").Trim();
            view = "list";
            Load();
        }

        /// <summary>Yangilash (asbob tugmasi, 30 s lik so'rov): forma ochiq bo'lsa unga tegilmaydi.</summary>
        public void Refresh()
        {
            if (!Active || view == "create") return;
            Load();
        }

        /// <summary>Esc: guruh sahifasi yoki formadan ro'yxatga qaytadi. true - Esc shu yerda ishlatildi.</summary>
        public bool Back()
        {
            if (!Active || view == "list") return false;
            view = "list"; detail = null; openCode = null;
            Load();
            return true;
        }

        /// <summary>Ro'yxatni qayta chizadi (masalan party tarkibi o'zgarganda taklif tugmalari uchun).</summary>
        public void Redraw() { if (Active && !loading) Draw(); }

        void Open(string code)
        {
            view = "detail"; openCode = code; detail = null;
            Load();
        }

        void OpenCreate()
        {
            if (!Active) return;
            view = "create";
            Draw();
        }

        void Load()
        {
            int request = ++revision;
            loading = true; failure = null;
            if (view == "detail" && detail == null || view == "list" && !mineLoaded) ShowMessage(Loc.T("common.connecting"));
            StartCoroutine(LoadRoutine(request));
        }

        IEnumerator LoadRoutine(int request)
        {
            if (view == "detail" && !string.IsNullOrEmpty(openCode))
            {
                ApiResult<GroupInfo> result = default;
                yield return lobby.Api.GroupDetail(PlayerProfile.Token, openCode, r => result = r);
                if (request != revision) yield break;
                if (result.Ok) { detail = result.Data; loading = false; Draw(); yield break; }
                if (result.Status == 404) { lobby.Toast(Loc.T("groups.error.group_not_found")); view = "list"; detail = null; }
                else { failure = Failure(result.NetworkError, result.Status, result.Data?.error); loading = false; Draw(); yield break; }
            }
            ApiResult<GroupList> own = default;
            yield return lobby.Api.GroupsMine(PlayerProfile.Token, r => own = r);
            if (request != revision) yield break;
            if (own.Ok) SetMine(own.Data);
            else if (!mineLoaded) failure = Failure(own.NetworkError, own.Status, own.Data?.error);
            results = null; hint = null;
            if (query.Length > 0)
            {
                string code = ParseCode(query);
                if (code == null && query.Length < 2) hint = Loc.T("groups.search_short");
                else
                {
                    ApiResult<GroupList> found = default;
                    yield return lobby.Api.GroupSearch(PlayerProfile.Token, code != null ? code : query, r => found = r);
                    if (request != revision) yield break;
                    if (found.Ok) results = found.Data.items ?? Array.Empty<GroupInfo>();
                    else failure = Failure(found.NetworkError, found.Status, found.Data?.error);
                }
            }
            loading = false;
            Draw();
        }

        /// <summary>Kod yoki havola: "NW-AB23CD", "ab23cd", "newworld://group/AB23CD" -> "AB23CD" (server bilan bir xil).</summary>
        public static string ParseCode(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            string text = input.Trim();
            int slash = text.LastIndexOf("group/", StringComparison.OrdinalIgnoreCase);
            if (slash >= 0) text = text.Substring(slash + 6).TrimEnd('/');
            if (text.StartsWith("NW-", StringComparison.OrdinalIgnoreCase) || text.StartsWith("NW ", StringComparison.OrdinalIgnoreCase)) text = text.Substring(3);
            else if (text.Length == 8 && text.StartsWith("NW", StringComparison.OrdinalIgnoreCase)) text = text.Substring(2);
            text = text.ToUpperInvariant();
            if (text.Length != 6) return null;
            foreach (char c in text) if ("ABCDEFGHJKMNPQRSTUVWXYZ23456789".IndexOf(c) < 0) return null;
            return text;
        }

        // ------------------------------------------------------------------ Chizish

        void Draw()
        {
            if (!Active || rows == null) return;
            Clear();
            float y;
            if (failure != null && !(view == "detail" && detail != null)) { ShowMessage(failure()); return; }
            if (view == "create") y = DrawCreate();
            else if (view == "detail" && detail != null) y = DrawDetail();
            else y = DrawList();
            Finish(y);
        }

        float DrawList()
        {
            float w = rows.rect.width, y = 0;
            if (hint != null) return Message(hint, y);
            if (results != null)
            {
                y = Header(Loc.F("groups.results", results.Length), y);
                if (results.Length == 0) return Message(Loc.T("groups.no_results"), y);
                foreach (var group in results) y = GroupRow(group, y, w);
                return y;
            }
            y = Header(Loc.F("groups.mine", mine.Length), y);
            if (mine.Length == 0) y = Message(Loc.T("groups.empty"), y);
            foreach (var group in mine) y = GroupRow(group, y, w);
            if (sent.Length > 0)
            {
                y = Header(Loc.F("groups.sent", sent.Length), y + 8);
                foreach (var group in sent) y = GroupRow(group, y, w);
            }
            return y;
        }

        float GroupRow(GroupInfo group, float y, float w)
        {
            var row = Panel("Group_" + group.code, rows, 0, y, w, RowHeight, RowFill);
            ReferenceSurface.Apply(row, 5, 14);
            row.raycastTarget = true;
            var button = row.gameObject.AddComponent<Button>(); button.targetGraphic = row;
            string code = group.code;
            button.onClick.AddListener(() => Open(code));
            LobbyFriendsPanel.Sounds(button);
            Monogram(row.transform, group.name, 10, 8, 52);
            var name = Label(row.transform, group.name, 74, 6, w - 210, 30, 21);
            name.font = boldFont != null ? boldFont : font;
            name.resizeTextForBestFit = true; name.resizeTextMinSize = 16; name.resizeTextMaxSize = 21;
            string line = KindLabel(group) + " · " + Loc.F("groups.members_count", group.memberCount);
            if (group.onlineCount > 0) line += " · " + Loc.F("groups.online_count", group.onlineCount);
            Label(row.transform, line, 74, 36, w - 150, 24, 16).color = group.kind == "paid" ? Gold : Muted;
            string badge = group.requested ? Loc.T("groups.requested") : RoleLabel(group.role);
            if (!string.IsNullOrEmpty(badge))
            {
                var tag = Label(row.transform, badge, w - 128, 6, 92, 26, 15);
                tag.alignment = TextAnchor.MiddleRight; tag.color = group.role == "owner" ? Gold : Soft;
            }
            if (group.pending > 0)
            {
                var count = Panel("Pending", row.transform, w - 66, 34, 30, 24, new Color32(243, 65, 95, 255));
                var text = Label(count.transform, group.pending > 9 ? "9+" : group.pending.ToString(), 0, 0, 30, 24, 15);
                text.alignment = TextAnchor.MiddleCenter; text.color = Color.white;
                if (tooltip != null) { count.raycastTarget = true; LobbyIconHint.Attach(count.gameObject, tooltip, "groups.pending_hint"); }
            }
            var arrow = Label(row.transform, "›", w - 30, 12, 24, 44, 30);
            arrow.alignment = TextAnchor.MiddleCenter; arrow.color = Muted;
            return y + RowHeight + Gap;
        }

        float DrawDetail()
        {
            var g = detail;
            float w = rows.rect.width, y = 0;
            TextButton(rows, 0, y, 150, 38, Loc.T("groups.back"), new Color32(52, 52, 56, 255), () => Back());
            y += 48;
            Monogram(rows, g.name, 0, y, 60);
            var title = Label(rows, g.name, 72, y - 2, w - 72, 34, 25);
            title.font = boldFont != null ? boldFont : font;
            title.resizeTextForBestFit = true; title.resizeTextMinSize = 17; title.resizeTextMaxSize = 25;
            Label(rows, KindLabel(g) + (g.IsMember ? " · " + RoleLabel(g.role) : ""), 72, y + 32, w - 72, 26, 17).color = g.kind == "paid" ? Gold : Soft;
            y += 70;
            // ID, nusxa olish va havola
            Label(rows, "ID: " + CodePrefix + g.code, 0, y, 190, 34, 19).color = Soft;
            TextButton(rows, 196, y, 122, 34, Loc.T("groups.copy_code"), new Color32(52, 52, 56, 255), () => Copy(CodePrefix + g.code, "groups.code_copied"));
            TextButton(rows, 324, y, w - 324, 34, Loc.T("groups.copy_link"), new Color32(52, 52, 56, 255), () => Copy(LinkPrefix + g.code, "groups.link_copied"));
            y += 42;
            string stats = Loc.F("groups.owner", g.owner) + " · " + Loc.F("groups.members_count", g.memberCount) + " · " + Loc.F("groups.online_count", g.onlineCount);
            var statsLabel = Label(rows, stats, 0, y, w, 26, 17); statsLabel.color = Muted;
            y += 30;
            if (!string.IsNullOrEmpty(g.description)) y = Paragraph(g.description, y, w, Soft, 18) + 6;
            if (g.kind == "paid")
            {
                y = Paragraph(Loc.F("groups.price_line", Price(g)), y, w, Gold, 19);
                if (g.role == "member" && !string.IsNullOrEmpty(g.expiresAt)) y = Paragraph(Loc.F("groups.expires", Date(g.expiresAt)), y, w, Green, 17);
                if (!g.IsMember) y = Paragraph(Loc.T("groups.paid_note"), y, w, Muted, 16);
                y += 4;
            }
            else if (g.kind == "private" && !g.IsMember) y = Paragraph(Loc.T("groups.private_note"), y, w, Muted, 16) + 4;

            // Asosiy amal
            string code = g.code;
            if (g.role == "owner")
                TextButton(rows, 0, y, w, 44, Loc.T("groups.delete"), Danger, () => Confirm("groups.delete", Loc.F("groups.delete_confirm", g.name), () => Act("delete", null, null)));
            else if (g.IsMember)
                TextButton(rows, 0, y, w, 44, Loc.T("groups.leave"), Danger, () => Confirm("groups.leave", Loc.F("groups.leave_confirm", g.name), () => Act("leave", null, null)));
            else if (g.requested)
                TextButton(rows, 0, y, w, 44, Loc.T("groups.cancel_request"), new Color32(66, 66, 72, 255), () => Act("cancel", null, null));
            else
                TextButton(rows, 0, y, w, 44, Loc.T(g.kind == "paid" ? "groups.subscribe" : g.kind == "private" ? "groups.request_join" : "groups.join"), LobbyPalette.Accent, Join);
            y += 54;

            if (g.IsAdmin && g.requests != null && g.requests.Length > 0)
            {
                y = Header(Loc.F(g.kind == "paid" ? "groups.requests_paid" : "groups.requests", g.requests.Length), y);
                foreach (var r in g.requests) y = MemberRow(r, y, w, true);
                y += 6;
            }
            if (!g.IsMember) return Message(Loc.T("groups.members_hidden"), y);
            y = Header(Loc.F("groups.members", g.members?.Length ?? 0), y);
            if (g.members != null) foreach (var m in g.members) y = MemberRow(m, y, w, false);
            return y;
        }

        float MemberRow(GroupMember m, float y, float w, bool request)
        {
            var g = detail;
            var party = lobby.GetComponent<LobbyParty>();
            bool self = m.friendship == "self";
            var row = Panel((request ? "Request_" : "Member_") + m.nickname, rows, 0, y, w, 64, RowFill);
            ReferenceSurface.Apply(row, m.online ? 4 : 5, 14);
            var portrait = Panel("Portrait", row.transform, 8, 7, 50, 50, new Color32(56, 72, 90, 255));
            var option = lobby.FindAvatar(m.avatarId);
            if (option?.card != null)
            {
                var photo = Rect("Avatar", portrait.transform, 0, 0, 50, 50).gameObject.AddComponent<RawImage>();
                photo.texture = option.card.texture; photo.uvRect = new Rect(.32f, .76f, .36f, .2112f); photo.raycastTarget = false;
            }
            int icons = 0;
            float Slot() => w - 48 - 46 * icons++;
            bool idle = !busy;
            if (request)
            {
                IconButton(row.transform, Slot(), removeIcon, "groups.reject_hint", () => Act("reject", m.nickname, null), idle);
                IconButton(row.transform, Slot(), acceptIcon, "groups.approve_hint", () => Act("approve", m.nickname, null), idle);
            }
            else if (!self)
            {
                bool canRemove = g.IsAdmin && m.role != "owner" && (m.role != "admin" || g.role == "owner");
                if (canRemove)
                    IconButton(row.transform, Slot(), removeIcon, "groups.remove_hint", () => Confirm("groups.remove_hint", Loc.F("groups.remove_confirm", m.nickname, g.name), () => Act("remove", m.nickname, null)), idle);
                if (g.role == "owner" && m.role != "owner")
                {
                    bool admin = m.role == "admin";
                    IconButton(row.transform, Slot(), adminIcon != null ? adminIcon : acceptIcon, admin ? "groups.unadmin_hint" : "groups.admin_hint",
                        () => Act("role", m.nickname, admin ? "member" : "admin"), idle, admin ? InviteFill : (Color?)null, admin ? Gold : (Color?)null);
                }
                if (m.friendship == "none")
                    IconButton(row.transform, Slot(), addIcon, "lobby.friend.add_hint", () => AddFriend(m), idle);
                if (party != null && m.online && !party.InParty(m.nickname))
                    IconButton(row.transform, Slot(), inviteIcon != null ? inviteIcon : addIcon, "lobby.party.invite_hint", () => party.Invite(m.nickname),
                        !party.IsInviting(m.nickname), InviteFill, Gold);
            }
            float nameWidth = Mathf.Max(80, w - 70 - 46 * icons - 6);
            var name = Label(row.transform, self ? m.nickname + " " + Loc.T("groups.you") : m.nickname, 68, 4, nameWidth, 30, 20);
            name.resizeTextForBestFit = true; name.resizeTextMinSize = 14; name.resizeTextMaxSize = 20;
            string state = m.online ? Loc.T("common.online") : Loc.T("common.offline");
            string role = request ? "" : RoleLabel(m.role);
            if (!string.IsNullOrEmpty(role) && m.role != "member") state = role + " · " + state;
            if (!request && !string.IsNullOrEmpty(m.expiresAt)) state += " · " + Loc.F("groups.until", Date(m.expiresAt));
            else if (m.publicId > 0) state += " · ID " + m.publicId.ToString("D6");
            var status = Label(row.transform, state, 68, 34, nameWidth, 24, 15);
            status.color = m.online ? Green : Muted;
            status.resizeTextForBestFit = true; status.resizeTextMinSize = 12; status.resizeTextMaxSize = 15;
            return y + 64 + Gap;
        }

        float DrawCreate()
        {
            float w = rows.rect.width, y = 0;
            TextButton(rows, 0, y, 150, 38, Loc.T("groups.back"), new Color32(52, 52, 56, 255), () => Back());
            var title = Label(rows, Loc.T("groups.create_title"), 162, 0, w - 162, 38, 23);
            title.font = boldFont != null ? boldFont : font;
            y += 50;
            y = FieldLabel("groups.field.name", y);
            Field(0, y, w, 46, formName, 40, false, "groups.field.name_hint", v => formName = v);
            y += 54;
            y = FieldLabel("groups.field.description", y);
            Field(0, y, w, 86, formDescription, 300, true, "groups.field.description_hint", v => formDescription = v);
            y += 94;
            y = FieldLabel("groups.field.kind", y);
            y = Segments(new[] { "free", "private", "paid" }, k => Loc.T("groups.kind." + k), formKind, k => { formKind = k; Draw(); }, y, w);
            y = Paragraph(Loc.T("groups.kind_hint." + formKind), y, w, Muted, 16) + 4;
            if (formKind == "paid")
            {
                y = FieldLabel("groups.field.price", y);
                var price = Field(0, y, 200, 46, formPrice, 10, false, "groups.field.price_hint", v => formPrice = v);
                price.contentType = InputField.ContentType.IntegerNumber;
                float x = 210, cw = (w - 210 - 6) / 2;
                foreach (var currency in new[] { "UZS", "USD" })
                {
                    string c = currency;
                    TextButton(rows, x, y, cw, 46, c, formCurrency == c ? LobbyPalette.Accent : new Color32(52, 52, 56, 255), () => { formCurrency = c; Draw(); });
                    x += cw + 6;
                }
                y += 54;
                y = FieldLabel("groups.field.period", y);
                y = Segments(new[] { "week", "month", "year" }, p => Loc.T("groups.period." + p), formPeriod, p => { formPeriod = p; Draw(); }, y, w);
                y = Paragraph(Loc.T("groups.paid_owner_note"), y, w, Gold, 16) + 4;
            }
            TextButton(rows, 0, y + 4, w, 48, Loc.T(busy ? "common.connecting" : "groups.create"), LobbyPalette.Accent, Create).interactable = !busy;
            return y + 60;
        }

        float Segments(string[] values, Func<string, string> label, string selected, Action<string> pick, float y, float w)
        {
            float cw = (w - 6 * (values.Length - 1)) / values.Length, x = 0;
            foreach (var value in values)
            {
                string v = value;
                TextButton(rows, x, y, cw, 42, label(v), v == selected ? LobbyPalette.Accent : new Color32(52, 52, 56, 255), () => pick(v));
                x += cw + 6;
            }
            return y + 50;
        }

        float FieldLabel(string key, float y)
        {
            Label(rows, Loc.T(key), 2, y, rows.rect.width, 24, 16).color = Soft;
            return y + 26;
        }

        // ------------------------------------------------------------------ Amallar

        void Create()
        {
            if (busy) return;
            string name = System.Text.RegularExpressions.Regex.Replace(formName ?? "", @"\s+", " ").Trim();
            if (name.Length < 3 || name.Length > 40) { lobby.Toast(Loc.T("groups.error.invalid_name")); return; }
            var body = new GroupCreateRequest { name = name, description = (formDescription ?? "").Trim(), kind = formKind, currency = formCurrency, period = formPeriod };
            if (formKind == "paid")
            {
                if (!int.TryParse(formPrice, NumberStyles.Integer, CultureInfo.InvariantCulture, out int price) || price < 1 || price > 1000000000)
                { lobby.Toast(Loc.T("groups.error.invalid_price")); return; }
                body.price = price;
            }
            busy = true; Draw();
            lobby.StartCoroutine(lobby.Api.GroupCreate(PlayerProfile.Token, body, result =>
            {
                if (this == null) return;
                busy = false;
                if (!result.Ok) { lobby.Toast(ErrorText(result.NetworkError, result.Status, result.Data?.error)); Draw(); return; }
                formName = formDescription = formPrice = ""; formKind = "free";
                lobby.Toast(Loc.F("groups.created", CodePrefix + result.Data.code));
                view = "detail"; openCode = result.Data.code; detail = result.Data;
                host.ClearSearch();
                Draw();
                StartCoroutine(LoadMineOnly());
            }));
        }

        void Join()
        {
            if (busy || detail == null) return;
            busy = true; Draw();
            string kind = detail.kind;
            lobby.StartCoroutine(lobby.Api.GroupJoin(PlayerProfile.Token, detail.code, result =>
            {
                if (this == null) return;
                busy = false;
                if (!result.Ok) { lobby.Toast(ErrorText(result.NetworkError, result.Status, result.Data?.error)); Draw(); return; }
                lobby.Toast(Loc.T(result.Data.status == "requested" ? kind == "paid" ? "groups.subscribe_sent" : "groups.request_sent" : "groups.joined"));
                if (result.Data.group != null) detail = result.Data.group;
                Draw();
                StartCoroutine(LoadMineOnly());
            }));
        }

        void Act(string action, string nickname, string role)
        {
            if (busy || detail == null) return;
            busy = true; Draw();
            string name = detail.name;
            lobby.StartCoroutine(lobby.Api.GroupAction(PlayerProfile.Token, action, detail.code, nickname, role, result =>
            {
                if (this == null) return;
                busy = false;
                if (!result.Ok) { lobby.Toast(ErrorText(result.NetworkError, result.Status, result.Data?.error)); Draw(); return; }
                string done = "groups.done." + action + (action == "role" ? "_" + role : "");
                lobby.Toast(Loc.F(done, nickname ?? name));
                if (action == "leave" || action == "delete") { view = "list"; detail = null; openCode = null; Load(); return; }
                if (result.Data.code != null) detail = result.Data;
                Draw();
                StartCoroutine(LoadMineOnly());
            }));
        }

        void AddFriend(GroupMember member)
        {
            if (busy) return;
            busy = true; Draw();
            lobby.StartCoroutine(lobby.Api.RequestFriend(PlayerProfile.Token, member.nickname, result =>
            {
                if (this == null) return;
                busy = false;
                lobby.Toast(LobbyFriendsPanel.FriendMessage("none", member.nickname, result));
                if (result.Ok && detail?.members != null)
                    foreach (var m in detail.members) if (m.nickname == member.nickname) m.friendship = result.Data.friendship;
                Draw();
                host.Refresh();
            }));
        }

        void Confirm(string titleKey, string message, Action action)
        {
            lobby.Dialog.Show(Loc.T(titleKey), message, Loc.T("groups.confirm"), action, Loc.T("common.cancel"));
        }

        void Copy(string text, string toastKey)
        {
            GUIUtility.systemCopyBuffer = text;
            lobby.Toast(Loc.F(toastKey, text));
        }

        static Func<string> Failure(bool network, long status, string code) => () => ErrorText(network, status, code);

        /// <summary>Server xato kodi (groups.js) bo'yicha matn; noma'lumlari umumiy xato matni.</summary>
        public static string ErrorText(bool network, long status, string code)
        {
            if (!network && !string.IsNullOrEmpty(code) && Loc.Has("groups.error." + code)) return Loc.T("groups.error." + code);
            return LobbyFriendsPanel.ErrorMessage(network, status, code);
        }

        // ------------------------------------------------------------------ Matnlar

        static string KindLabel(GroupInfo g) => g.kind == "paid" ? Loc.F("groups.kind_paid_short", Price(g)) : Loc.T("groups.kind." + (g.kind ?? "free"));
        static string RoleLabel(string role) => string.IsNullOrEmpty(role) ? "" : Loc.T("groups.role." + role);
        static string Price(GroupInfo g)
        {
            string amount = g.price.ToString("N0", CultureInfo.InvariantCulture).Replace(",", " ");
            return amount + " " + g.currency + " / " + Loc.T("groups.per." + (string.IsNullOrEmpty(g.period) ? "month" : g.period));
        }
        static string Date(string iso) =>
            DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var t) ? t.ToLocalTime().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : iso;

        // ------------------------------------------------------------------ UI yordamchilari

        void Clear()
        {
            if (rows == null) return;
            // Maydon fokusda bo'lsa (forma) o'chirilayotgan obyekt tanlangan bo'lib qolmasin
            var events = EventSystem.current;
            if (events != null && events.currentSelectedGameObject != null && events.currentSelectedGameObject.transform.IsChildOf(rows))
                events.SetSelectedGameObject(null);
            foreach (Transform child in rows) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        }

        void ShowMessage(string text)
        {
            if (!Active || rows == null) return;
            Clear();
            Finish(Message(text, 0));
        }

        void Finish(float height)
        {
            var viewport = rows.parent as RectTransform;
            rows.sizeDelta = new Vector2(rows.sizeDelta.x, Mathf.Max(viewport != null ? viewport.rect.height : 0, height + 8));
        }

        float Header(string text, float y)
        {
            var label = Label(rows, text, 4, y, rows.rect.width - 8, 30, 17);
            label.color = Muted; label.font = boldFont != null ? boldFont : font;
            return y + 32;
        }

        float Message(string text, float y) => Paragraph(text, y + 6, rows.rect.width - 20, new Color32(169, 188, 207, 255), 19, 10) + 6;

        float Paragraph(string text, float y, float w, Color color, int size, float x = 0)
        {
            var label = Label(rows, text, x, y, w, 30, size);
            label.color = color; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Overflow;
            label.alignment = TextAnchor.UpperLeft;
            float height = Mathf.Max(size + 8, label.preferredHeight + 4);
            label.rectTransform.sizeDelta = new Vector2(w, height);
            return y + height + 4;
        }

        // Guruh belgisi: nomining birinchi harfi, nomdan olingan barqaror rang
        void Monogram(Transform parent, string name, float x, float y, float size)
        {
            uint hash = 2166136261; foreach (char c in name ?? "") hash = unchecked((hash ^ c) * 16777619);
            var color = Color.HSVToRGB((hash % 360) / 360f, .45f, .55f);
            var mark = Panel("Monogram", parent, x, y, size, size, color);
            string letter = string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1).ToUpperInvariant();
            var text = Label(mark.transform, letter, 0, 0, size, size, Mathf.RoundToInt(size * .45f));
            text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.font = boldFont != null ? boldFont : font;
        }

        static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.gameObject.layer = 5; rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); return rect;
        }

        Image Panel(string name, Transform parent, float x, float y, float w, float h, Color color)
        {
            var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>(); image.sprite = rounded; image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 3; image.color = color; image.raycastTarget = false; return image;
        }

        Text Label(Transform parent, string value, float x, float y, float w, float h, int size)
        {
            var text = Rect("Label", parent, x, y, w, h).gameObject.AddComponent<Text>(); text.font = font; text.fontSize = size; text.text = value;
            text.supportRichText = false;
            text.color = new Color32(232, 238, 245, 255); text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        Button TextButton(Transform parent, float x, float y, float w, float h, string text, Color fill, Action action)
        {
            var image = Panel("Button", parent, x, y, w, h, fill); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.onClick.AddListener(() => action());
            var label = Label(image.transform, text, 6, 0, w - 12, h, 17);
            label.alignment = TextAnchor.MiddleCenter; label.color = Color.white;
            label.resizeTextForBestFit = true; label.resizeTextMinSize = 12; label.resizeTextMaxSize = 17;
            LobbyFriendsPanel.Sounds(button);
            return button;
        }

        void IconButton(Transform parent, float x, Sprite icon, string hint, Action action, bool enabled, Color? fill = null, Color? glyph = null)
        {
            var image = Panel("Action", parent, x, 12, 38, 38, fill ?? LobbyPalette.Accent); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.interactable = enabled;
            button.onClick.AddListener(() => action());
            var mark = Panel("Icon", image.transform, 9, 9, 20, 20, glyph ?? Color.white); mark.sprite = icon; mark.type = Image.Type.Simple;
            LobbyFriendsPanel.Sounds(button);
            if (tooltip != null) LobbyIconHint.Attach(image.gameObject, tooltip, hint);
        }

        /// <summary>
        /// Ish vaqtida yaratiladigan matn maydoni. InputField obyekt faol bo'lmaganda qo'shiladi va textComponent
        /// yoqilishdan oldin beriladi: aks holda karetka va matn bog'lanmay qoladi.
        /// </summary>
        InputField Field(float x, float y, float w, float h, string value, int limit, bool multiline, string placeholderKey, Action<string> changed)
        {
            var fill = Panel("Field", rows, x, y, w, h, FieldFill); fill.raycastTarget = true;
            var go = fill.gameObject; go.SetActive(false);
            var text = Label(fill.transform, "", 12, multiline ? 6 : 0, w - 24, multiline ? h - 12 : h, 18);
            text.alignment = multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            var placeholder = Label(fill.transform, Loc.T(placeholderKey), 12, multiline ? 6 : 0, w - 24, multiline ? h - 12 : h, 17);
            placeholder.alignment = text.alignment; placeholder.color = new Color(1, 1, 1, .4f); placeholder.fontStyle = FontStyle.Italic;
            var field = go.AddComponent<InputField>();
            field.textComponent = text; field.placeholder = placeholder; field.targetGraphic = fill;
            field.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
            field.characterLimit = limit;
            field.customCaretColor = true; field.caretColor = Color.white; field.caretWidth = 2;
            field.selectionColor = new Color(.45f, .62f, 1f, .45f);
            field.text = value ?? "";
            field.onValueChanged.AddListener(v => changed(v));
            go.SetActive(true);
            return field;
        }
    }
}

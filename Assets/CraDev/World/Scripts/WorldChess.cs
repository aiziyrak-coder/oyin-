using System;
using System.Collections;
using System.Collections.Generic;
using CraDev.Online;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CraDev.World
{
    /// <summary>One in-world overlay, with server-authoritative boards shared by nearby players.</summary>
    public sealed class WorldChess : MonoBehaviour
    {
        [SerializeField] Font font;
        [SerializeField] Font boldFont;
        [SerializeField] Sprite rounded;
        static readonly Color Ink = new Color(.93f, .94f, .91f);
        static readonly Color Muted = new Color(.60f, .67f, .65f);
        static readonly Color Accent = new Color(.63f, .83f, .67f);
        static readonly Color LightSquare = new Color(.81f, .80f, .70f);
        static readonly Color DarkSquare = new Color(.23f, .36f, .31f);
        readonly Dictionary<int, ChessSnapshot> states = new Dictionary<int, ChessSnapshot>();
        readonly Dictionary<int, WorldChessPieces.BoardView> boards = new Dictionary<int, WorldChessPieces.BoardView>();
        readonly List<(Text label, string key)> localized = new List<(Text, string)>();
        readonly Image[] squares = new Image[64];
        readonly ChessPieceGraphic[] icons = new ChessPieceGraphic[64];
        readonly Button[] cells = new Button[64];
        readonly Text[] fileLabels = new Text[8], rankLabels = new Text[8];
        WorldNetwork network;
        WorldPlayerController player;
        WorldHud hud;
        CityChessTable[] tables;
        WorldChessPieces pieces;
        RectTransform overlay, promotionPanel;
        Text prompt, heading, status, whiteName, blackName, role, note, footer;
        Button whiteButton, blackButton, leaveButton, resetButton, closeButton;
        Text whiteCaption, blackCaption, leaveCaption, resetCaption, closeCaption;
        ChessSnapshot state;
        int currentTable = -1, selected = -1, pollIndex, viewRevision;
        string session = "", errorKey, promotionFrom, promotionTo;
        float nextPoll, errorUntil, confirmUntil;
        bool requestBusy, actionBusy;
        public bool IsOpen => overlay && overlay.gameObject.activeSelf;
        public int CurrentTable => currentTable;
        public int VisiblePieceCount => pieces != null ? pieces.VisibleCount : 0;
        public ChessSnapshot CurrentState => state;
        public bool ActionPending => actionBusy;

        void Start()
        {
            network = FindFirstObjectByType<WorldNetwork>();
            player = FindFirstObjectByType<WorldPlayerController>();
            hud = GetComponent<WorldHud>();
            if (!hud) hud = FindFirstObjectByType<WorldHud>();
            if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tables = FindObjectsByType<CityChessTable>(FindObjectsSortMode.None);
            Array.Sort(tables, (a, b) => a.TableId.CompareTo(b.TableId));
            pieces = new WorldChessPieces();
            foreach (var table in tables) boards[table.TableId] = pieces.Create(table);
            Build();
            Loc.Changed += Refresh;
            Refresh();
        }

        void Update()
        {
            if (!player || tables == null) return;
            pieces?.Draw(player.transform.position);
            if (!network) network = FindFirstObjectByType<WorldNetwork>();
            string nowSession = network && network.Connected ? network.SessionId : "";
            if (session != nowSession)
            {
                session = nowSession;
                states.Clear(); state = null; selected = -1; viewRevision++;
                HidePromotion(); nextPoll = 0f;
                // A restarted world server may reuse table versions; do not keep stale boards.
                foreach (var board in boards.Values) board.SetBoard(WorldChessPieces.InitialBoard);
                Refresh();
            }
            var nearby = NearestTable(3f);
            bool free = !IsOpen && !player.Paused;
            prompt.gameObject.SetActive(free && nearby);
            if (free && nearby)
            {
                prompt.text = network && network.Connected
                    ? string.Format(Loc.T("chess.prompt"), string.Format(Loc.T("chess.table"), nearby.TableId + 1))
                    : Loc.T("chess.connecting");
                if (UsePressed() && network && network.Connected) OpenTable(nearby.TableId);
            }
            if (IsOpen)
            {
                var table = Table(currentTable);
                if (!table || Distance(table) > 4.8f) Close();
                if (!string.IsNullOrEmpty(errorKey) && Time.unscaledTime > errorUntil) { errorKey = null; Refresh(); }
                if (confirmUntil > 0f && Time.unscaledTime > confirmUntil) { confirmUntil = 0f; Refresh(); }
            }
            if (!network || !network.Connected || string.IsNullOrEmpty(session) || requestBusy || actionBusy || Time.unscaledTime < nextPoll) return;
            if (IsOpen) StartCoroutine(Poll(currentTable));
            else if (tables.Length > 0)
            {
                for (int i = 0; i < tables.Length; i++)
                {
                    var table = tables[pollIndex++ % tables.Length];
                    if (Distance(table) < 25f) { StartCoroutine(Poll(table.TableId)); break; }
                }
                nextPoll = Time.unscaledTime + .4f;
            }
        }

        float Distance(CityChessTable table)
        {
            Vector3 delta = player.transform.position - table.transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }
        CityChessTable NearestTable(float range)
        {
            CityChessTable best = null;
            foreach (var table in tables)
            {
                float distance = Distance(table);
                if (distance < range) { best = table; range = distance; }
            }
            return best;
        }
        CityChessTable Table(int id) => Array.Find(tables, table => table.TableId == id);

        public void OpenTable(int tableId)
        {
            if (!Table(tableId) || Distance(Table(tableId)) > 3f || !network || !network.Connected) return;
            currentTable = tableId; selected = -1; viewRevision++; confirmUntil = 0f; errorKey = null;
            states.TryGetValue(tableId, out state);
            overlay.gameObject.SetActive(true);
            overlay.SetAsLastSibling();
            if (hud) hud.SetInteraction(true); else player.SetPaused(true);
            nextPoll = 0f;
            HidePromotion(); Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            overlay.gameObject.SetActive(false); selected = -1; viewRevision++; confirmUntil = 0f;
            HidePromotion();
            if (hud) hud.SetInteraction(false); else if (player) player.SetPaused(false);
        }

        IEnumerator Poll(int tableId)
        {
            requestBusy = true;
            string expected = session;
            ApiResult<ChessSnapshot> result = default;
            yield return network.Api.ChessTable(PlayerProfile.Token, expected, tableId, value => result = value);
            requestBusy = false;
            nextPoll = Time.unscaledTime + (IsOpen ? .65f : .4f);
            if (session != expected) yield break;
            if (result.Ok) Accept(result.Data);
            else if (IsOpen && currentTable == tableId) Error(result);
        }

        void Accept(ChessSnapshot snapshot)
        {
            if (snapshot == null || snapshot.board == null || snapshot.board.Length != 64) return;
            snapshot.NormalizeSeats();
            if (states.TryGetValue(snapshot.tableId, out var previous) && previous.version > snapshot.version) return;
            states[snapshot.tableId] = snapshot;
            if (boards.TryGetValue(snapshot.tableId, out var board)) board.SetBoard(snapshot.board);
            if (currentTable != snapshot.tableId) return;
            bool changed = state == null || state.version != snapshot.version;
            state = snapshot;
            if (changed) { selected = -1; HidePromotion(); confirmUntil = 0f; }
            Refresh();
        }

        string OwnColor()
        {
            int id = PlayerProfile.PublicId;
            if (id <= 0 || state == null) return "";
            if (state.white != null && state.white.publicId == id) return "white";
            if (state.black != null && state.black.publicId == id) return "black";
            return "";
        }
        bool ActiveGame => state != null && (state.status == "playing" || state.status == "check");
        bool CanMove => !actionBusy && ActiveGame && OwnColor() == state.turn;
        bool Flipped => OwnColor() == "black";

        public void SelectSquare(int square)
        {
            if (!IsOpen || !CanMove || square < 0 || square >= 64 || promotionPanel.gameObject.activeSelf) return;
            if (selected >= 0)
            {
                string from = SquareName(selected), to = SquareName(square);
                var moves = state.legalMoves ?? Array.Empty<ChessMove>();
                ChessMove move = Array.Find(moves, candidate => candidate.from == from && candidate.to == to);
                if (move != null)
                {
                    if (!string.IsNullOrEmpty(move.promotion))
                    {
                        promotionFrom = from; promotionTo = to;
                        promotionPanel.gameObject.SetActive(true);
                    }
                    else Move(from, to, "");
                    return;
                }
            }
            string piece = state.board[square];
            bool mine = !string.IsNullOrEmpty(piece) && (char.IsUpper(piece[0]) == (OwnColor() == "white"));
            selected = mine && selected != square ? square : -1;
            PaintBoard();
        }

        void Move(string from, string to, string promote)
        {
            HidePromotion();
            Send("move", new ChessRequest { from = from, to = to, promotion = promote });
        }
        void Send(string action, ChessRequest request = null)
        {
            if (actionBusy || !IsOpen || !network || !network.Connected || state == null) return;
            request = request ?? new ChessRequest();
            request.sessionId = session; request.tableId = currentTable; request.version = state.version;
            StartCoroutine(Act(action, request));
        }
        IEnumerator Act(string action, ChessRequest request)
        {
            actionBusy = true; selected = -1; Refresh();
            int revision = viewRevision;
            ApiResult<ChessSnapshot> result = default;
            yield return network.Api.ChessAction(PlayerProfile.Token, action, request, value => result = value);
            actionBusy = false; nextPoll = 0f;
            if (session != request.sessionId) yield break;
            if (result.Ok) { errorKey = null; Accept(result.Data); }
            else if (IsOpen && revision == viewRevision) Error(result);
            if (IsOpen && revision == viewRevision) Refresh();
        }
        void Error(ApiResult<ChessSnapshot> result)
        {
            string code = result.Data != null ? result.Data.error : "";
            errorKey = result.Status == 429 ? "chess.slow" : result.NetworkError || result.Status >= 500 ? "chess.error" :
                code == "leave_first" ? "chess.leave_first" : code == "too_far" ? "chess.too_far" :
                code == "seat_taken" ? "chess.taken" : result.Status == 409 ? "chess.changed" : "chess.cannot_move";
            errorUntil = Time.unscaledTime + 4f; Refresh();
        }

        void Leave()
        {
            if (ActiveGame && Time.unscaledTime >= confirmUntil)
            { confirmUntil = Time.unscaledTime + 5f; Refresh(); return; }
            confirmUntil = 0f; Send("leave");
        }
        void HidePromotion()
        {
            if (promotionPanel) promotionPanel.gameObject.SetActive(false);
        }

        void Refresh()
        {
            if (!heading) return;
            heading.text = Loc.T("chess.title") + "  /  " + string.Format(Loc.T("chess.table"), currentTable + 1);
            whiteCaption.text = Loc.T("chess.join_white"); blackCaption.text = Loc.T("chess.join_black");
            closeCaption.text = Loc.T("chess.close");
            leaveCaption.text = Loc.T(confirmUntil > Time.unscaledTime ? "chess.confirm_leave" : "chess.leave");
            bool voted = state != null && state.resetVotes != null && Array.IndexOf(state.resetVotes, PlayerProfile.PublicId) >= 0;
            resetCaption.text = Loc.T(voted ? "chess.voted" : "chess.reset");
            whiteName.text = Loc.T("chess.white") + "\n" + (state?.white?.nickname ?? Loc.T("chess.empty"));
            blackName.text = Loc.T("chess.black") + "\n" + (state?.black?.nickname ?? Loc.T("chess.empty"));
            string own = OwnColor();
            role.text = Loc.T(string.IsNullOrEmpty(own) ? "chess.watching" : own == "white" ? "chess.white" : "chess.black");
            note.text = Loc.T(string.IsNullOrEmpty(own) ? "chess.hint" : "chess.leave_note");
            footer.text = !string.IsNullOrEmpty(errorKey) ? Loc.T(errorKey) : Loc.T("chess.hint");
            footer.color = !string.IsNullOrEmpty(errorKey) ? new Color(1f, .68f, .48f) : Muted;
            whiteButton.gameObject.SetActive(string.IsNullOrEmpty(own)); blackButton.gameObject.SetActive(string.IsNullOrEmpty(own));
            whiteButton.interactable = state != null && state.white == null && !actionBusy && network && network.Connected;
            blackButton.interactable = state != null && state.black == null && !actionBusy && network && network.Connected;
            leaveButton.gameObject.SetActive(!string.IsNullOrEmpty(own));
            resetButton.gameObject.SetActive(!string.IsNullOrEmpty(own) && !ActiveGame);
            leaveButton.interactable = !actionBusy;
            resetButton.interactable = !actionBusy && !voted;
            status.text = Status();
            foreach (var item in localized) item.label.text = Loc.T(item.key);
            PaintBoard();
        }

        string Status()
        {
            if (!network || !network.Connected) return Loc.T("chess.connecting");
            if (state == null) return Loc.T("chess.loading");
            if (ActiveGame)
            {
                string turn = Loc.T(OwnColor() == state.turn ? "chess.your_turn" : state.turn == "white" ? "chess.white_turn" : "chess.black_turn");
                return (state.status == "check" ? Loc.T("chess.check") + "\n" : "") + turn;
            }
            string result = Loc.T("chess." + state.status);
            if (!string.IsNullOrEmpty(state.winner)) result += "\n" + Loc.T("chess.won_" + state.winner);
            return result;
        }

        void PaintBoard()
        {
            bool flip = Flipped;
            for (int visual = 0; visual < 64; visual++)
            {
                int file = visual % 8, row = visual / 8;
                int square = flip ? (7 - file) + row * 8 : file + (7 - row) * 8;
                string coordinate = SquareName(square);
                Color fill = ((square % 8 + square / 8) & 1) == 0 ? DarkSquare : LightSquare;
                if (state != null && (state.lastFrom == coordinate || state.lastTo == coordinate)) fill = Color.Lerp(fill, new Color(.75f, .67f, .32f), .5f);
                if (selected >= 0 && state?.legalMoves != null && Array.Exists(state.legalMoves, m => m.from == SquareName(selected) && m.to == coordinate)) fill = Color.Lerp(fill, new Color(.5f, .85f, .5f), .65f);
                if (square == selected) fill = new Color(.78f, .79f, .37f);
                squares[visual].color = fill;
                icons[visual].Piece = state != null && state.board != null ? state.board[square] : "";
                cells[visual].interactable = CanMove;
            }
            for (int i = 0; i < 8; i++)
            {
                fileLabels[i].text = ((char)('a' + (flip ? 7 - i : i))).ToString();
                rankLabels[i].text = (flip ? i + 1 : 8 - i).ToString();
            }
        }

        public static string SquareName(int square) => square < 0 || square > 63 ? "" : ((char)('a' + square % 8)).ToString() + (square / 8 + 1);
        public static int SquareIndex(string coordinate) => coordinate != null && coordinate.Length == 2 && coordinate[0] >= 'a' && coordinate[0] <= 'h' && coordinate[1] >= '1' && coordinate[1] <= '8' ? coordinate[0] - 'a' + (coordinate[1] - '1') * 8 : -1;

        void Build()
        {
            prompt = Text("ChessInteractionHint", transform, "", 21, Ink);
            var hint = prompt.rectTransform;
            hint.anchorMin = hint.anchorMax = new Vector2(.5f, 0f); hint.pivot = new Vector2(.5f, 0f);
            hint.anchoredPosition = new Vector2(0f, 82f); hint.sizeDelta = new Vector2(660f, 50f);
            prompt.alignment = TextAnchor.MiddleCenter;
            prompt.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1f, -1f);
            overlay = Panel("ChessOverlay", transform, new Color(.015f, .025f, .03f, .72f)); Stretch(overlay);
            overlay.GetComponent<Image>().raycastTarget = true;
            var window = Panel("ChessWindow", overlay, new Color(.065f, .088f, .08f, .99f));
            Center(window, Vector2.zero, new Vector2(1040f, 760f));
            heading = Text("ChessTitle", window, "", 23, Ink, true); Place(heading.rectTransform, 32f, 24f, 750f, 34f);
            closeButton = Button("ChessClose", window, 840f, 22f, 168f, 38f, "", Close, out closeCaption);
            var board = Panel("ChessBoardFrame", window, new Color(.035f, .045f, .04f)); Place(board, 28f, 80f, 624f, 624f);
            for (int visual = 0; visual < 64; visual++)
            {
                int file = visual % 8, row = visual / 8, captured = visual;
                var cell = Panel("Square" + visual, board, Color.white); Place(cell, 24f + file * 72f, 24f + row * 72f, 72f, 72f);
                squares[visual] = cell.GetComponent<Image>(); squares[visual].sprite = null; squares[visual].raycastTarget = true;
                var button = cell.gameObject.AddComponent<Button>(); button.targetGraphic = squares[visual]; button.transition = Selectable.Transition.None;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                button.onClick.AddListener(() => { int f = captured % 8, r = captured / 8; SelectSquare(Flipped ? 7 - f + r * 8 : f + (7 - r) * 8); });
                cells[visual] = button;
                var icon = Rect("Piece", cell).gameObject.AddComponent<ChessPieceGraphic>(); Stretch(icon.rectTransform);
                icon.raycastTarget = false; icons[visual] = icon;
            }
            for (int i = 0; i < 8; i++)
            {
                fileLabels[i] = Text("File" + i, board, "", 13, Muted); Place(fileLabels[i].rectTransform, 24f + i * 72f, 600f, 72f, 22f); fileLabels[i].alignment = TextAnchor.MiddleCenter;
                rankLabels[i] = Text("Rank" + i, board, "", 13, Muted); Place(rankLabels[i].rectTransform, 0f, 24f + i * 72f, 24f, 72f); rankLabels[i].alignment = TextAnchor.MiddleCenter;
            }
            role = Text("ChessRole", window, "", 13, Accent, true); Place(role.rectTransform, 680f, 88f, 322f, 25f);
            status = Text("ChessStatus", window, "", 24, Ink, true); Place(status.rectTransform, 680f, 122f, 322f, 78f);
            whiteName = Text("ChessWhitePlayer", window, "", 19, Ink); Place(whiteName.rectTransform, 680f, 224f, 322f, 70f);
            blackName = Text("ChessBlackPlayer", window, "", 19, Ink); Place(blackName.rectTransform, 680f, 310f, 322f, 70f);
            whiteButton = Button("ChessJoinWhite", window, 680f, 404f, 328f, 48f, "", () => Send("join", new ChessRequest { color = "white" }), out whiteCaption);
            blackButton = Button("ChessJoinBlack", window, 680f, 462f, 328f, 48f, "", () => Send("join", new ChessRequest { color = "black" }), out blackCaption);
            leaveButton = Button("ChessLeave", window, 680f, 404f, 328f, 48f, "", Leave, out leaveCaption);
            resetButton = Button("ChessReset", window, 680f, 462f, 328f, 48f, "", () => Send("reset"), out resetCaption);
            note = Text("ChessNote", window, "", 16, Muted); Place(note.rectTransform, 680f, 548f, 320f, 145f);
            footer = Text("ChessFooter", window, "", 15, Muted); Place(footer.rectTransform, 32f, 711f, 976f, 36f);
            promotionPanel = Panel("ChessPromotion", window, new Color(.04f, .065f, .05f, .99f)); Place(promotionPanel, 134f, 252f, 728f, 244f);
            promotionPanel.GetComponent<Image>().raycastTarget = true;
            var promoteTitle = Text("PromotionTitle", promotionPanel, "", 23, Ink, true); Place(promoteTitle.rectTransform, 24f, 24f, 680f, 36f);
            localized.Add((promoteTitle, "chess.promote"));
            string[] names = { "queen", "rook", "bishop", "knight" }, types = { "q", "r", "b", "n" };
            for (int i = 0; i < types.Length; i++)
            {
                string type = types[i];
                var choice = Button("Promotion" + type, promotionPanel, 24f + i * 172f, 82f, 164f, 128f, "", () => Move(promotionFrom, promotionTo, type), out var caption);
                Place(caption.rectTransform, 4f, 94f, 156f, 28f); localized.Add((caption, "chess." + names[i]));
                var icon = Rect("PromotionPiece", choice.transform).gameObject.AddComponent<ChessPieceGraphic>(); Place(icon.rectTransform, 42f, 8f, 80f, 80f); icon.Piece = type.ToUpperInvariant(); icon.raycastTarget = false;
            }
            promotionPanel.gameObject.SetActive(false); overlay.gameObject.SetActive(false);
        }

        Text Text(string name, Transform parent, string value, int size, Color color, bool bold = false)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>(); text.font = bold && boldFont ? boldFont : font;
            text.fontSize = size; text.color = color; text.text = value; text.supportRichText = false; text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleLeft; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        Button Button(string name, Transform parent, float x, float y, float w, float h, string caption, Action action, out Text label)
        {
            var rect = Panel(name, parent, new Color(.16f, .23f, .19f)); Place(rect, x, y, w, h);
            var image = rect.GetComponent<Image>(); image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f); colors.disabledColor = new Color(.65f, .65f, .65f, .65f); button.colors = colors;
            button.onClick.AddListener(() => action()); button.navigation = new Navigation { mode = Navigation.Mode.None };
            label = Text(name + "Label", rect, caption, 17, Ink); Stretch(label.rectTransform); label.alignment = TextAnchor.MiddleCenter;
            return button;
        }
        RectTransform Panel(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent); var image = rect.gameObject.AddComponent<Image>(); image.color = color;
            image.sprite = rounded; image.type = rounded ? Image.Type.Sliced : Image.Type.Simple; image.pixelsPerUnitMultiplier = 5f; image.raycastTarget = false;
            return rect;
        }
        static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            var rect = (RectTransform)go.transform; rect.SetParent(parent, false); return rect;
        }
        static void Place(RectTransform rect, float x, float y, float w, float h)
        { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); }
        static void Center(RectTransform rect, Vector2 position, Vector2 size)
        { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size; }
        static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        static bool UsePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.E);
#else
            return false;
#endif
        }
        void OnDestroy()
        {
            Loc.Changed -= Refresh;
            StopAllCoroutines();
            pieces?.Dispose();
        }
    }

}

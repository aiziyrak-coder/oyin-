using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CraDev.CharacterCreation;
using CraDev.Online;
using CraDev.World;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CraDev
{
    /// <summary>Only used by -cradevCitySmoke. No account creation, chess moves, microphone, or saved preference writes.</summary>
    public static class DevCitySmoke
    {
        public static int Failures { get; private set; }
        static int checks;
        static bool navigationPassed;
        static void Check(bool ok, string description)
        {
            checks++; if (!ok) Failures++;
            Debug.Log($"[CityTest] {(ok ? "PASS" : "FAIL")}: {description}");
        }
        static void Complete() => Debug.Log($"[CityTest] COMPLETE: {checks} checks, {Failures} failures");

        public static IEnumerator Run()
        {
            checks = Failures = 0;
            var player = Object.FindFirstObjectByType<WorldPlayerController>();
            var hud = Object.FindFirstObjectByType<WorldHud>();
            var network = Object.FindFirstObjectByType<WorldNetwork>();
            var voice = Object.FindFirstObjectByType<WorldVoice>();
            var chess = Object.FindFirstObjectByType<WorldChess>();
            Check(player && player.Capsule && player.ViewCamera, "world controller, capsule and camera exist");
            Check(hud && network && voice && chess, "world HUD, network, proximity voice and chess exist");
            if (!player || !hud || !network || !voice || !chess) { Complete(); yield break; }
            bool previousTest = player.TestMode, previousPaused = player.Paused, previousMenu = hud.SettingsOpen;
            player.TestMode = true;
            try
            {
                hud.SetSettings(false);
                yield return null;
                Geometry();
                Check(player.ViewCamera.allowHDR, "camera uses HDR lighting");
                Check(QualitySettings.activeColorSpace == ColorSpace.Linear, "materials use linear colour space");
                var effects = player.ViewCamera.GetComponent<WorldImageEffects>();
                Check(effects && effects.IsSupported && effects.isActiveAndEnabled, "world tone mapping shader is supported and active");
                Check(hud.MapCamera && hud.MapTexture && hud.MapTexture.IsCreated(), "minimap camera renders a live texture");
                Check(RenderSettings.sun && RenderSettings.sun.shadows != LightShadows.None, "daylight casts geometry shadows");
                Check(!voice.MicOn, "public-world microphone starts muted");
                Check(WorldChess.SquareName(0) == "a1" && WorldChess.SquareName(63) == "h8", "chess board coordinates use a1 through h8");
                Check(WorldChess.SquareIndex("e4") == 28 && WorldChess.SquareIndex("z9") == -1 && WorldChess.SquareIndex(null) == -1, "chess coordinate parsing rejects invalid input");
                // Every temporary position is restored in this same frame: the live network never sees a test teleport.
                PhysicsChecks(player);
                float deadline = Time.realtimeSinceStartup + 22f;
                while (!network.Connected && Time.realtimeSinceStartup < deadline) yield return null;
                Check(network.Connected && !string.IsNullOrEmpty(network.SessionId), "authenticated public-world session is connected");
                if (!network.Connected) yield break;
                Check(network.PlayerCount >= 1, "world presence includes the current player");
                Check(Mathf.Abs(player.transform.position.x) < 145f && Mathf.Abs(player.transform.position.z) < 145f && player.transform.position.y > -.2f, "random spawn is inside the district and above ground");
                yield return Capture("city-street.png");
                // Realtime controller movement, not teleports: normal server speed validation stays enabled.
                bool cityVisuals = Array.IndexOf(Environment.GetCommandLineArgs(), "-cradevCityVisuals") >= 0;
                var route = new List<Vector3> { new Vector3(0,0,0) };
                if (cityVisuals) route.Add(new Vector3(-18,0,34));
                route.AddRange(new[] { new Vector3(0,0,46), new Vector3(0,0,54), new Vector3(-14,0,54), new Vector3(-14,0,56.4f) });
                foreach (var waypoint in route)
                {
                    yield return WalkTo(player, waypoint);
                    if (!navigationPassed) yield break;
                    if (cityVisuals && Mathf.Abs(waypoint.x + 18f) < .1f && Mathf.Abs(waypoint.z - 34f) < .1f)
                    {
                        float oldYaw = player.Yaw, oldPitch = player.Pitch;
                        try
                        {
                            LookTowards(player, new Vector3(-18, 3.3f, 40));
                            yield return Capture("city-advertising.png");
                        }
                        finally { LookAt(player, oldYaw, oldPitch); }
                    }
                    if (Mathf.Abs(waypoint.z - 46f) < .1f) yield return Capture("city-pavilion.png");
                }
                float settle = Time.realtimeSinceStartup + 1.4f;
                while (Time.realtimeSinceStartup < settle) { player.Simulate(Vector2.zero, Vector2.zero, false, false, false, Mathf.Min(Time.unscaledDeltaTime,.1f)); yield return null; }
                Check(chess.VisiblePieceCount == 320, $"ten fresh tables render 320 instanced chess pieces: {chess.VisiblePieceCount}");
                yield return Capture("city-interior.png");
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-cradevCityPeerTest") >= 0)
                    yield return PeerChecks(player, network);
                chess.OpenTable(0);
                Check(chess.IsOpen && chess.CurrentTable == 0, "nearby table opens in-world chess without changing scene");
                Check(player.Paused && !hud.SettingsOpen, "chess pauses walking without opening world settings");
                deadline = Time.realtimeSinceStartup + 12f;
                while (chess.IsOpen && chess.CurrentState == null && Time.realtimeSinceStartup < deadline) yield return null;
                var state = chess.CurrentState;
                Check(state != null && state.tableId == 0 && state.board != null && state.board.Length == 64, "live server returns the authoritative 64-square chess board");
                Check(chess.transform.Find("ChessOverlay/ChessWindow/ChessBoardFrame")?.GetComponentsInChildren<Button>().Length == 64, "all 64 board squares have clickable controls");
                Check(chess.transform.Find("ChessOverlay/ChessWindow/ChessJoinWhite") && chess.transform.Find("ChessOverlay/ChessWindow/ChessJoinBlack"), "white and black seat choices are present");
                Check(chess.transform.Find("ChessOverlay/ChessWindow/ChessPromotion")?.GetComponentsInChildren<Button>(true).Length == 4, "promotion offers queen, rook, bishop and knight");
                yield return Capture("city-chess.png");
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-cradevCityChessPeerTest") >= 0)
                    yield return ChessPeerChecks(chess, network);
                Check(hud.ToggleMenuFromInput(), "Esc handler accepts closing chess");
                Check(!chess.IsOpen && !player.Paused && !hud.SettingsOpen, "Esc closes chess and resumes walking without opening settings");
                yield return null;
                Check(hud.ToggleMenuFromInput() && hud.SettingsOpen && player.Paused, "next Esc opens the independent world settings");
                yield return Capture("city-settings.png");
                hud.SetSettings(false);
                Check(!voice.MicOn, "test never enables microphone capture");
            }
            finally
            {
                if (chess && chess.IsOpen) chess.Close();
                // Keep the final live position: restoring the old spawn here would send a forbidden teleport.
                if (hud) hud.SetSettings(previousMenu);
                if (player) { player.TestMode = previousTest; player.SetPaused(previousPaused); }
                Complete();
            }
        }

        static void Geometry()
        {
            var tables = Object.FindObjectsByType<CityChessTable>(FindObjectsSortMode.None);
            Check(tables.Length == 10, $"exactly ten chess tables exist: {tables.Length}");
            var ids = new HashSet<int>();
            foreach (var table in tables)
            {
                bool id = table.TableId >= 0 && table.TableId < 10 && ids.Add(table.TableId);
                float x = -14 + table.TableId % 5 * 7, z = table.TableId < 5 ? 59 : 70;
                Check(id && Mathf.Abs(table.BoardCenter.x-x) < .01f && Mathf.Abs(table.BoardCenter.z-z) < .01f && Mathf.Abs(table.BoardCenter.y-.81f) < .01f,
                    $"table {table.TableId} has a unique server id and matching board coordinates");
            }
            int chairs=0, roads=0, signs=0, markings=0, plots=0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if(t.name=="ChessChair")chairs++;
                if(t.name.StartsWith("Asphalt",StringComparison.Ordinal))roads++;
                if(t.name.StartsWith("RoadSign_",StringComparison.Ordinal))signs++;
                if(t.name=="ZebraCrossing")markings++;
                if(t.name=="PlotSurveyMarker")plots++;
            }
            Check(chairs==20,$"twenty chairs exist for ten boards: {chairs}");
            Check(Object.FindObjectsByType<CityBillboard>(FindObjectsSortMode.None).Length==4,"four animated advertising screens exist");
            Check(roads>=8 && signs>=16 && markings>=64 && plots>0,"roads, signs, zebra crossings and reserved building plots exist");
            Check(GameObject.Find("ChessPavilion_OpenEntrance"),"walk-in chess pavilion exists");
            int mask=~((1<<8)|(1<<5));
            for(int z=46;z<=54;z++)
            {
                bool blocked=Physics.CheckCapsule(new Vector3(0,.36f,z),new Vector3(0,1.5f,z),.28f,mask,QueryTriggerInteraction.Ignore);
                Check(!blocked,$"entrance walking clearance at x0 z{z}");
            }
        }

        static void PhysicsChecks(WorldPlayerController player)
        {
            Vector3 original=player.transform.position;float yaw=player.Yaw;
            bool crouch=WorldPreferences.ToggleCrouch;
            try
            {
                WorldPreferences.ToggleCrouch=false;
                Reset(player); float y=player.transform.position.y, height=player.Capsule.height;
                Check(player.IsGrounded && Mathf.Abs(y)<.2f,"flat district ground supports the capsule");
                Vector3 before=player.transform.position;Sim(player,1.5f,Vector2.up);
                float walk=Flat(before,player.transform.position);
                Check(walk>3 && walk<7 && player.IsGrounded,$"flat walking is smooth: {walk:F2}m");
                Reset(player);before=player.transform.position;Sim(player,1.5f,Vector2.up,true);
                Check(Flat(before,player.transform.position)>walk*1.4f,"sprint is faster than walking");
                Reset(player);Sim(player,.6f,default,false,true);
                Check(player.IsCrouching && player.Capsule.height<height-.3f,"crouch lowers the physical capsule");
                Sim(player,.7f);
                Check(!player.IsCrouching && Mathf.Abs(player.Capsule.height-height)<.03f,"standing restores capsule height");
                Reset(player); y=player.transform.position.y;float peak=y;
                player.Simulate(Vector2.zero,Vector2.zero,false,false,true,1f/60);
                for(int i=0;i<180;i++){player.Simulate(Vector2.zero,Vector2.zero,false,false,false,1f/60);peak=Mathf.Max(peak,player.transform.position.y);}
                Check(peak-y>.65f && peak-y<1.5f && player.IsGrounded,"jump rises naturally and lands on flat ground");
            }
            finally { WorldPreferences.ToggleCrouch=crouch;player.Teleport(original,yaw);Physics.SyncTransforms(); }
        }
        static void Reset(WorldPlayerController player){player.Teleport(new Vector3(0,.08f,0));Physics.SyncTransforms();Sim(player,.5f);}
        static void Sim(WorldPlayerController player,float seconds,Vector2 move=default,bool sprint=false,bool crouch=false)
        {for(int i=0;i<Mathf.RoundToInt(seconds*60);i++)player.Simulate(move,Vector2.zero,sprint,crouch,false,1f/60);}
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}

        static IEnumerator WalkTo(WorldPlayerController player,Vector3 target)
        {
            float deadline=Time.realtimeSinceStartup+Mathf.Clamp(Flat(player.transform.position,target)/3f+15f,18f,60f);
            navigationPassed=false;
            while(Time.realtimeSinceStartup<deadline)
            {
                Vector3 delta=target-player.transform.position;delta.y=0;float distance=delta.magnitude;
                if(distance<.3f){navigationPassed=true;break;}
                float desired=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;
                float dt=Mathf.Clamp(Time.unscaledDeltaTime,.0001f,.1f);
                // Avoid tall street furniture; kerbs stay traversable by the CharacterController step offset.
                Vector3 direction=delta.normalized;
                if(Physics.CapsuleCast(player.transform.position+Vector3.up*.65f,player.transform.position+Vector3.up*1.4f,.33f,direction,out _,1f,~((1<<8)|(1<<5)),QueryTriggerInteraction.Ignore))
                {
                    foreach(float angle in new[]{45f,-45f,80f,-80f})
                    {
                        Vector3 alternative=Quaternion.Euler(0,angle,0)*direction;
                        if(Physics.CapsuleCast(player.transform.position+Vector3.up*.65f,player.transform.position+Vector3.up*1.4f,.33f,alternative,out _,1.2f,~((1<<8)|(1<<5)),QueryTriggerInteraction.Ignore))continue;
                        desired=Mathf.Atan2(alternative.x,alternative.z)*Mathf.Rad2Deg;break;
                    }
                }
                float angleDelta=Mathf.DeltaAngle(player.Yaw,desired);
                float turn=Mathf.Clamp(angleDelta,-220f*dt,220f*dt);
                float sensitivity=Mathf.Clamp(WorldPreferences.Sensitivity,WorldPreferences.MinSensitivity,WorldPreferences.MaxSensitivity);
                float pixels=turn/(sensitivity*WorldPlayerController.MouseDegreesPerPixel);
                Vector2 movement=Mathf.Abs(angleDelta)<65f?Vector2.up*Mathf.Clamp01(distance/2f):Vector2.zero;
                player.Simulate(movement,new Vector2(pixels,0),true,false,false,dt);
                yield return null;
            }
            Check(navigationPassed,$"real-time walking reaches ({target.x:F1},{target.z:F1}); current ({player.transform.position.x:F1},{player.transform.position.z:F1})");
        }

        static IEnumerator Capture(string name)
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-cradevShot");
            if(index<0||index+1>=args.Length)yield break;
            string folder=Path.GetDirectoryName(args[index+1]);if(string.IsNullOrEmpty(folder))yield break;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,name));
            yield return new WaitForSecondsRealtime(.25f);
        }

        // Requires a real second client on an isolated test server. No synthetic peer or local avatar injection.
        static IEnumerator PeerChecks(WorldPlayerController player, WorldNetwork network)
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            Transform host = null;
            WorldPeer peer = null;
            AvatarViewer viewer = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                var root = GameObject.Find("WorldPlayers");
                if (network.Connected && network.VisibleCount > 0 && root)
                {
                    float nearest = float.MaxValue;
                    foreach (Transform child in root.transform)
                    {
                        if (!child.name.StartsWith("Player_", StringComparison.Ordinal) || !int.TryParse(child.name.Substring(7), out int id)) continue;
                        var actual = network.Peer(id);
                        var avatar = child.GetComponentInChildren<AvatarViewer>();
                        if (actual == null || !avatar || !avatar.CurrentModel || !avatar.CurrentModel.activeInHierarchy) continue;
                        float distance = (child.position - player.transform.position).sqrMagnitude;
                        if (distance >= nearest) continue;
                        nearest = distance; host = child; peer = actual; viewer = avatar;
                    }
                }
                if (host && viewer) break;
                yield return null;
            }
            Check(network.Connected && network.VisibleCount >= 1 && host && peer != null,
                "real second client appears through live world snapshots within twenty seconds");
            if (!host || peer == null || !viewer) yield break;
            // Deferred collider destruction and the first nickname update complete by the next rendered frame.
            yield return null;
            Check(viewer.PaintTextureLimit == 512, "remote avatar replica has a 512-pixel paint limit");
            var model = viewer.CurrentModel;
            var renderers = model ? model.GetComponentsInChildren<Renderer>() : Array.Empty<Renderer>();
            bool activeRenderer = Array.Exists(renderers, renderer => renderer.enabled && renderer.gameObject.activeInHierarchy);
            Check(model && model.activeInHierarchy && activeRenderer,
                "remote player uses an active rendered 3D avatar model");
            Check(host.GetComponentsInChildren<Collider>(true).Length == 0,
                "remote player has no colliders that can block local movement");
            var nickname = host.Find("Nickname")?.GetComponent<TextMesh>();
            Check(!string.IsNullOrWhiteSpace(peer.nickname) && nickname && !string.IsNullOrWhiteSpace(nickname.text) &&
                nickname.text.StartsWith(peer.nickname, StringComparison.Ordinal),
                "remote nameplate displays the real server nickname");
            float height = viewer.BodyHeight * Mathf.Abs(viewer.transform.lossyScale.y);
            Check(height >= 1.5f && height <= 2.2f, $"remote avatar is normalized to human scale: {height:F3}m");
            Check(viewer.GetComponent<WorldAvatarMotion>(), "remote avatar has the world movement animation driver");
            float originalYaw = player.Yaw, originalPitch = player.Pitch;
            try
            {
                Vector3 direction = host.position + Vector3.up * .95f - player.ViewCamera.transform.position;
                float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float pitch = -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
                LookAt(player, yaw, pitch);
                yield return Capture("city-multiplayer.png");
                yield return PerformanceSample();
            }
            finally { if (player) LookAt(player, originalYaw, originalPitch); }
        }

        static void LookAt(WorldPlayerController player, float yaw, float pitch)
        {
            float sensitivity = Mathf.Clamp(WorldPreferences.Sensitivity, WorldPreferences.MinSensitivity, WorldPreferences.MaxSensitivity);
            float degrees = sensitivity * WorldPlayerController.MouseDegreesPerPixel;
            float x = Mathf.DeltaAngle(player.Yaw, yaw) / degrees;
            float y = -(Mathf.Clamp(pitch, -85f, 85f) - player.Pitch) / (degrees * (WorldPreferences.InvertY ? -1f : 1f));
            player.Simulate(Vector2.zero, new Vector2(x, y), false, false, false, 1f / 60f);
        }

        static void LookTowards(WorldPlayerController player, Vector3 point)
        {
            Vector3 direction = point - player.ViewCamera.transform.position;
            LookAt(player, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg,
                -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg);
        }

        static IEnumerator ChessPeerChecks(WorldChess chess, WorldNetwork network)
        {
            bool allowed = Array.IndexOf(Environment.GetCommandLineArgs(), "-cradevCityPeerTest") >= 0 &&
                network.Api != null && string.Equals(network.Api.BaseUrl.TrimEnd('/'), "http://127.0.0.1:8088", StringComparison.Ordinal);
            Check(allowed, "chess peer actions are restricted to the explicit isolated 127.0.0.1:8088 peer-test server");
            if (!allowed) yield break;
            var nullSeats = JsonUtility.FromJson<ChessSnapshot>("{\"white\":null,\"black\":null}");
            Debug.Log($"[CityTest] JSON NULL SEATS BEFORE NORMALIZATION: white={SeatDescription(nullSeats.white)}; black={SeatDescription(nullSeats.black)}");
            nullSeats.NormalizeSeats();
            var occupiedSeat = JsonUtility.FromJson<ChessSnapshot>("{\"white\":{\"publicId\":123456,\"nickname\":\"Peer\"},\"black\":null}");
            occupiedSeat.NormalizeSeats();
            Check(nullSeats.white == null && nullSeats.black == null && occupiedSeat.white != null &&
                occupiedSeat.white.publicId == 123456 && occupiedSeat.black == null,
                "serialized null chess seats normalize to vacant while a real occupied seat remains intact");
            var join = chess.transform.Find("ChessOverlay/ChessWindow/ChessJoinWhite")?.GetComponent<Button>();
            Debug.Log($"[CityTest] CHESS JOIN READINESS: connected={network.Connected}; open={chess.IsOpen}; table={chess.CurrentTable}; pending={chess.ActionPending}; white={SeatDescription(chess.CurrentState?.white)}; black={SeatDescription(chess.CurrentState?.black)}; buttonActive={(join && join.gameObject.activeInHierarchy)}; interactable={(join && join.interactable)}");
            Check(chess.IsOpen && chess.CurrentTable == 0 && join && join.gameObject.activeInHierarchy && join.interactable,
                "live table zero exposes the enabled white-seat GUI button");
            if (!chess.IsOpen || !join || !join.gameObject.activeInHierarchy || !join.interactable) yield break;
            join.onClick.Invoke();
            float deadline = Time.realtimeSinceStartup + 20f;
            while (Time.realtimeSinceStartup < deadline)
            {
                var s = chess.CurrentState;
                if (s?.white != null && s.white.publicId == PlayerProfile.PublicId && s.black != null &&
                    s.black.nickname != null && s.black.nickname.StartsWith("CityTestGuest", StringComparison.Ordinal) && s.status == "playing") break;
                yield return null;
            }
            var state = chess.CurrentState;
            bool seated = state?.white != null && state.white.publicId == PlayerProfile.PublicId && state.black != null &&
                state.black.nickname != null && state.black.nickname.StartsWith("CityTestGuest", StringComparison.Ordinal) && state.status == "playing";
            Check(seated, "white GUI seat and the real black test-client seat start the same server game");
            if (!seated) yield break;
            var player = Object.FindFirstObjectByType<WorldPlayerController>();
            deadline = Time.realtimeSinceStartup + 5f;
            while (Time.realtimeSinceStartup < deadline && network.Self?.pose != "sit") yield return null;
            Check(network.Self != null && network.Self.activity == "chess" && network.Self.activitySlot == 0 &&
                network.Self.activityRole == "white" && network.Self.pose == "sit" && player.ActivityLocked,
                "the server seats the white player and the controller receives the locked sitting pose");
            Check(network.Self != null && Mathf.Abs(network.Self.x + 14f) < .05f && Mathf.Abs(network.Self.z - 57.78f) < .05f,
                "white sits at the real south chair rather than inside the table");
            chess.Close();
            Vector3 seatedPosition = player.transform.position;
            player.Simulate(Vector2.up, Vector2.zero, true, false, true, .05f);
            Check(!chess.IsOpen && !player.Paused && player.ActivityLocked && (player.transform.position - seatedPosition).sqrMagnitude < .001f,
                "closing the chess view allows looking around but cannot walk or jump out of the chair");
            yield return new WaitForSecondsRealtime(.3f);
            yield return Capture("city-chess-seated.png");
            chess.OpenTable(0);
            Check(chess.IsOpen && player.Paused, "seated player can reopen the same table without changing the scene");
            chess.SelectSquare(WorldChess.SquareIndex("e2"));
            var board = chess.transform.Find("ChessOverlay/ChessWindow/ChessBoardFrame");
            var source = board.Find("Square52") as RectTransform; // e2 from white's view
            var target = board.Find("Square36") as RectTransform; // e4
            Check(target && target.GetComponentInChildren<ChessLegalMarker>().Mode == 1,
                "selecting e2 shows a legal-move dot on e4");
            var canvas = chess.GetComponentInParent<Canvas>();
            var eventCamera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 startPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, source.TransformPoint(source.rect.center));
            Vector2 endPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, target.TransformPoint(target.rect.center));
            chess.BeginPieceDrag(52, startPoint, eventCamera); chess.DragPiece(endPoint, eventCamera); chess.EndPieceDrag(endPoint, eventCamera);
            deadline = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < deadline && !BoardHas(chess.CurrentState, "e4", "P", "e2")) yield return null;
            bool whiteMoved = BoardHas(chess.CurrentState, "e4", "P", "e2");
            Check(whiteMoved, "dragging GUI e2-e4 is acknowledged by the authoritative server board");
            if (!whiteMoved) yield break;
            deadline = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < deadline && !BoardHas(chess.CurrentState, "e5", "p", "e7")) yield return null;
            bool blackMoved = BoardHas(chess.CurrentState, "e5", "p", "e7");
            Check(blackMoved, "the second real client replies e7-e5 and the GUI receives it");
            Check(blackMoved && chess.CurrentState.turn == "white" && chess.CurrentState.status == "playing",
                "the shared game returns the turn to white after both legal moves");
            Check(chess.CurrentState.history != null && chess.CurrentState.history.Length == 2 &&
                chess.CurrentState.history[0].san == "e4" && chess.CurrentState.history[1].san == "e5",
                "the server and chess UI retain the SAN move history for both players");
            Check(chess.transform.Find("ChessOverlay/ChessWindow/ChessMoveHistory") &&
                chess.transform.Find("ChessOverlay/ChessWindow/ChessDraw").gameObject.activeInHierarchy &&
                chess.transform.Find("ChessOverlay/ChessWindow/ChessResign").gameObject.activeInHierarchy,
                "move-history, mutual-draw and confirmed-resignation controls exist in a live game");
            var flip = chess.transform.Find("ChessOverlay/ChessWindow/ChessFlip")?.GetComponent<Button>();
            flip.onClick.Invoke();
            Check(board.Find("File0").GetComponent<Text>().text == "h" && board.Find("Rank0").GetComponent<Text>().text == "1",
                "flipping the board changes file/rank orientation together");
            flip.onClick.Invoke();
            yield return Capture("city-chess-playing.png");
            // Release only this test seat via the same confirmation path used by the player.
            var leave = chess.transform.Find("ChessOverlay/ChessWindow/ChessLeave")?.GetComponent<Button>();
            if (leave && leave.interactable && leave.gameObject.activeInHierarchy)
            {
                leave.onClick.Invoke(); yield return null;
                if (leave.interactable && leave.gameObject.activeInHierarchy) leave.onClick.Invoke();
                deadline = Time.realtimeSinceStartup + 8f;
                while (Time.realtimeSinceStartup < deadline && chess.CurrentState?.white?.publicId == PlayerProfile.PublicId) yield return null;
                Check(chess.CurrentState != null && (chess.CurrentState.white == null || chess.CurrentState.white.publicId != PlayerProfile.PublicId),
                    "test seat is released through the confirmed GUI leave action");
                deadline = Time.realtimeSinceStartup + 5f;
                while (Time.realtimeSinceStartup < deadline && player.ActivityLocked) yield return null;
                Check(!player.ActivityLocked && network.Self != null && string.IsNullOrEmpty(network.Self.activity),
                    "confirmed leave releases the authoritative chair and restores standing movement");
            }
        }

        static bool BoardHas(ChessSnapshot state, string occupied, string piece, string empty)
        {
            return state?.board != null && state.board.Length == 64 && state.board[WorldChess.SquareIndex(occupied)] == piece &&
                string.IsNullOrEmpty(state.board[WorldChess.SquareIndex(empty)]);
        }

        static string SeatDescription(ChessSeat seat) => seat == null ? "null" : "publicId=" + seat.publicId;

        static IEnumerator PerformanceSample()
        {
            // A bounded observation of this machine/run, never a pass/fail FPS promise for other hardware.
            yield return new WaitForEndOfFrame();
            if (!ReadableFrame(out string firstFrame))
            {
                Debug.Log("[CityTest] PERFORMANCE SKIPPED: screenshot is blank or lacks scene variation; hidden/unrendered windows are not rendering benchmarks. " + firstFrame);
                yield break;
            }
            for (int frame = 0; frame < 20; frame++) yield return null;
            float total = 0f, worst = 0f;
            int slowFrames = 0;
            for (int frame = 0; frame < 160; frame++)
            {
                yield return null;
                float dt = Time.unscaledDeltaTime;
                total += dt; worst = Mathf.Max(worst, dt); if (dt > .033333f) slowFrames++;
            }
            yield return new WaitForEndOfFrame();
            if (!ReadableFrame(out string lastFrame))
            {
                Debug.Log("[CityTest] PERFORMANCE SKIPPED: rendering became blank during the sample. " + lastFrame);
                yield break;
            }
            Debug.Log($"[CityTest] PERFORMANCE: 160 rendered frames with a live remote avatar; average {(total > 0 ? 160f / total : 0):F1} FPS; worst {worst * 1000f:F2} ms; {slowFrames} frames over 33.33ms; {Screen.width}x{Screen.height}. Local sample only, not a hardware-wide benchmark.");
        }

        static bool ReadableFrame(out string evidence)
        {
            Texture2D screenshot = null;
            try
            {
                screenshot = ScreenCapture.CaptureScreenshotAsTexture();
                if (!screenshot || screenshot.width < 32 || screenshot.height < 32) { evidence = "No readable frame texture."; return false; }
                Color32[] pixels = screenshot.GetPixels32();
                double sum = 0, sumSquared = 0;
                float min = 1f, max = 0f;
                const int grid = 24;
                // Sample the central scene, not the corner HUD labels or monitor borders.
                for (int y = 0; y < grid; y++)
                for (int x = 0; x < grid; x++)
                {
                    int px = Mathf.Clamp(Mathf.RoundToInt(screenshot.width * (.2f + .6f * x / (grid - 1))), 0, screenshot.width - 1);
                    int py = Mathf.Clamp(Mathf.RoundToInt(screenshot.height * (.2f + .6f * y / (grid - 1))), 0, screenshot.height - 1);
                    Color32 color = pixels[py * screenshot.width + px];
                    float value = (color.r + color.g + color.b) / (3f * 255f);
                    sum += value; sumSquared += value * value; min = Mathf.Min(min, value); max = Mathf.Max(max, value);
                }
                double mean = sum / (grid * grid), variance = Math.Max(0, sumSquared / (grid * grid) - mean * mean);
                evidence = $"Central-frame mean={mean:F4}, range={max-min:F4}, variance={variance:F6}.";
                return max > .015f && max - min > .035f && variance > .000015;
            }
            catch (Exception error) { evidence = "Frame read failed: " + error.GetType().Name; return false; }
            finally { if (screenshot) Object.Destroy(screenshot); }
        }
    }
}

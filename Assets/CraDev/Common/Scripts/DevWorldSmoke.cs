using System.Collections;
using CraDev.Online;
using CraDev.World;
using UnityEngine;

namespace CraDev
{
    // Faqat -cradevWorldSmoke bilan ishlaydi; profil va saqlangan sozlamalarni o'zgartirmaydi.
    public static class DevWorldSmoke
    {
        public static int Failures { get; private set; }
        static int checks;
        static readonly Vector3 Origin = new Vector3(0, .06f, 0);

        static void Check(bool ok, string description)
        {
            checks++;
            if (!ok) Failures++;
            Debug.Log($"[WorldTest] {(ok ? "PASS" : "FAIL")}: {description}");
        }

        static void Simulate(WorldPlayerController player, float seconds, int hz = 60,
            Vector2 move = default, bool sprint = false, bool crouch = false)
        {
            int frames = Mathf.RoundToInt(seconds * hz);
            for (int i = 0; i < frames; i++)
                player.Simulate(move, Vector2.zero, sprint, crouch, false, 1f / hz);
        }

        static void Reset(WorldPlayerController player, Vector3 position, float yaw = 0)
        {
            player.SetPaused(false);
            player.Teleport(position, yaw);
            Physics.SyncTransforms();
            Simulate(player, .5f);
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0;
            return Vector3.Distance(a, b);
        }

        static void Complete() => Debug.Log($"[WorldTest] COMPLETE: {checks} checks, {Failures} failures");

        public static IEnumerator Run()
        {
            checks = Failures = 0;
            var player = Object.FindFirstObjectByType<WorldPlayerController>();
            var hud = Object.FindFirstObjectByType<WorldHud>();
            Check(player != null, "world movement controller exists");
            Check(hud != null, "separate world HUD exists");
            if (player == null || hud == null) { Complete(); yield break; }

            bool originalTestMode = player.TestMode;
            bool originalPaused = player.Paused;
            bool originalSettingsOpen = hud.SettingsOpen;
            bool toggleCrouch = WorldPreferences.ToggleCrouch;
            float sensitivity = WorldPreferences.Sensitivity;
            float fieldOfView = WorldPreferences.FieldOfView;
            Vector3 originalPosition = player.transform.position;
            float originalYaw = player.Yaw;
            string nickname = PlayerProfile.Nickname;
            string avatar = PlayerProfile.AvatarId;
            string outfit = PlayerProfile.Outfit;
            player.TestMode = true;
            WorldPreferences.ToggleCrouch = false;

            try
            {
                hud.SetSettings(false);
                yield return null;
                yield return null;
                Check(player.Capsule != null && player.Capsule.enabled, "physical CharacterController is enabled");
                Check(player.ViewCamera != null && player.ViewCamera.enabled, "first-person camera is active");
                if (player.Capsule == null || player.ViewCamera == null) yield break;

                Reset(player, Origin);
                Check(player.IsGrounded && Mathf.Abs(player.transform.position.y) < .2f, "capsule settles on ground without sinking");
                float standingHeight = player.Capsule.height;
                float standingEye = player.ViewCamera.transform.localPosition.y;

                Vector3 start = player.transform.position;
                Simulate(player, 1.5f, move: Vector2.up);
                float walk = FlatDistance(start, player.transform.position);
                float walkSpeed = player.Speed;
                Check(walk > 1.5f && walk < 8f && player.IsGrounded, $"walking advances on ground: {walk:F3} m");
                Check(!player.IsSprinting && !player.IsCrouching, "walking has no sprint or crouch state");

                Reset(player, Origin);
                start = player.transform.position;
                Simulate(player, 1.5f, move: Vector2.up, sprint: true);
                float sprint = FlatDistance(start, player.transform.position);
                Check(sprint > walk * 1.35f && player.Speed > walkSpeed * 1.35f, $"sprint is faster than walk: {sprint:F3} m");
                Check(player.IsSprinting && player.IsGrounded, "sprint state is grounded");

                Reset(player, Origin);
                start = player.transform.position;
                Simulate(player, 1.5f, move: Vector2.one);
                float diagonal = FlatDistance(start, player.transform.position);
                Check(Mathf.Abs(diagonal - walk) < Mathf.Max(.12f, walk * .035f), $"diagonal input is normalized: {diagonal:F3} vs {walk:F3} m");

                float minimum = float.MaxValue, maximum = 0;
                foreach (int hz in new[] { 30, 60, 120 })
                {
                    Reset(player, Origin);
                    start = player.transform.position;
                    Simulate(player, 1.5f, hz, Vector2.up);
                    float distance = FlatDistance(start, player.transform.position);
                    minimum = Mathf.Min(minimum, distance);
                    maximum = Mathf.Max(maximum, distance);
                    Check(distance > 1.5f && player.IsGrounded, $"movement at {hz} FPS: {distance:F4} m");
                }
                Check(maximum - minimum < Mathf.Max(.12f, minimum * .035f), $"frame-rate independence: spread {maximum - minimum:F4} m");

                Reset(player, Origin);
                Simulate(player, 1, move: Vector2.up);
                start = player.transform.position;
                Simulate(player, 1);
                float stopDistance = FlatDistance(start, player.transform.position);
                Check(player.Speed < .03f && stopDistance < .8f, $"smooth deceleration stops: {stopDistance:F3} m drift, {player.Speed:F3} m/s");
                start = player.transform.position;
                Simulate(player, 3);
                Check(FlatDistance(start, player.transform.position) < .005f && player.IsGrounded, "idle remains still and supported by ground");

                Reset(player, Origin);
                float floor = player.transform.position.y;
                player.Simulate(Vector2.zero, Vector2.zero, false, false, true, 1f / 60);
                float peak = player.transform.position.y;
                bool airborne = !player.IsGrounded;
                for (int frame = 0; frame < 180; frame++)
                {
                    player.Simulate(Vector2.zero, Vector2.zero, false, false, false, 1f / 60);
                    peak = Mathf.Max(peak, player.transform.position.y);
                    airborne |= !player.IsGrounded;
                }
                Check(airborne && peak - floor > .5f && peak - floor < 4, $"jump leaves ground with bounded height: {peak - floor:F3} m");
                Check(player.IsGrounded && Mathf.Abs(player.transform.position.y - floor) < .12f, "gravity lands capsule back on ground");

                Reset(player, new Vector3(0, .06f, 8));
                Simulate(player, 3, move: Vector2.up, sprint: true);
                Check(player.transform.position.z > 9 && player.Capsule.bounds.max.z < 11.80f, $"solid wall blocks sprint: front z={player.Capsule.bounds.max.z:F3}");
                Check(player.IsGrounded && Mathf.Abs(player.transform.position.y) < .2f, "wall contact does not lift or drop player");

                Reset(player, new Vector3(10, .06f, 1));
                float stepPeak = player.transform.position.y;
                for (int frame = 0; frame < 180 && player.transform.position.z < 7.3f; frame++)
                {
                    player.Simulate(Vector2.up, Vector2.zero, false, false, false, 1f / 60);
                    stepPeak = Mathf.Max(stepPeak, player.transform.position.y);
                }
                Check(stepPeak > .35f && player.transform.position.z > 6.8f, $"step lane is traversable: top={stepPeak:F3}, z={player.transform.position.z:F3}");
                Simulate(player, 1);
                Check(player.IsGrounded, "player is grounded after step lane");

                Reset(player, new Vector3(12, .06f, 14));
                for (int frame = 0; frame < 360 && player.transform.position.z < 22.4f; frame++)
                    player.Simulate(Vector2.up, Vector2.zero, false, false, false, 1f / 60);
                Check(player.transform.position.z > 22 && player.transform.position.y > 1.6f && player.transform.position.y < 2.35f,
                    $"gentle ramp can be walked up: y={player.transform.position.y:F3}, z={player.transform.position.z:F3}");
                Simulate(player, .5f);
                Check(player.IsGrounded && player.transform.position.y > 1.6f, "gentle slope supports player when movement stops");

                Reset(player, new Vector3(-12, .06f, 14));
                float steepPeak = player.transform.position.y;
                for (int frame = 0; frame < 240; frame++)
                {
                    player.Simulate(Vector2.up, Vector2.zero, true, false, false, 1f / 60);
                    steepPeak = Mathf.Max(steepPeak, player.transform.position.y);
                }
                Check(steepPeak < 1.25f && player.transform.position.z < 17.1f,
                    $"slope limit prevents climbing steep ramp: peak={steepPeak:F3}, z={player.transform.position.z:F3}");

                Reset(player, Origin);
                Simulate(player, 1, crouch: true);
                Check(player.IsCrouching && player.Capsule.height < standingHeight - .25f, "crouch lowers collision capsule");
                Check(player.ViewCamera.transform.localPosition.y < standingEye - .2f, "crouch lowers first-person viewpoint");
                start = player.transform.position;
                Simulate(player, 1.5f, move: Vector2.up, sprint: true, crouch: true);
                Check(!player.IsSprinting && FlatDistance(start, player.transform.position) < walk * .85f, "crouching stays slow even with sprint held");
                Simulate(player, 1);
                Check(!player.IsCrouching && Mathf.Abs(player.Capsule.height - standingHeight) < .02f, "releasing crouch restores standing in clear space");

                Reset(player, new Vector3(-10, .06f, -4));
                Simulate(player, .5f, crouch: true);
                for (int frame = 0; frame < 360 && player.transform.position.z < -.4f; frame++)
                    player.Simulate(Vector2.up, Vector2.zero, false, true, false, 1f / 60);
                Check(Mathf.Abs(player.transform.position.z) < 1.4f && player.IsCrouching, "crouching enters low-ceiling passage");
                Check(!player.CanStand, "overhead obstruction is detected");
                Simulate(player, 1);
                Check(player.IsCrouching && player.Capsule.height < standingHeight - .25f, "cannot stand through low ceiling when crouch released");
                Check(player.Capsule.bounds.max.y < 1.36f, "crouched capsule remains below ceiling underside");
                Simulate(player, 4, move: Vector2.down);
                Simulate(player, .5f);
                Check(player.CanStand && !player.IsCrouching, "leaving ceiling automatically permits standing");

                Reset(player, Origin);
                WorldPreferences.ToggleCrouch = true;
                player.Simulate(Vector2.zero, Vector2.zero, false, true, false, 1f / 60);
                Simulate(player, .5f);
                Check(player.IsCrouching, "toggle crouch stays active after release");
                player.Simulate(Vector2.zero, Vector2.zero, false, true, false, 1f / 60);
                Simulate(player, .5f);
                Check(!player.IsCrouching, "second crouch press exits toggle stance");
                WorldPreferences.ToggleCrouch = false;

                Reset(player, Origin, 90);
                Simulate(player, 1, move: Vector2.up);
                Check(player.transform.position.x > 1 && Mathf.Abs(player.transform.position.z) < .15f, "movement follows heading rather than global axis");
                float previousYaw = player.Yaw;
                player.Simulate(Vector2.zero, new Vector2(25, 10000), false, false, false, 1f / 60);
                float firstPitch = player.Pitch;
                Check(Mathf.Abs(Mathf.DeltaAngle(previousYaw, player.Yaw)) > .1f, "mouse look changes yaw");
                Check(Mathf.Abs(firstPitch) < 89.9f && Mathf.Abs(firstPitch) > 30, "upward pitch is clamped before camera flips");
                player.Simulate(Vector2.zero, new Vector2(0, -20000), false, false, false, 1f / 60);
                Check(Mathf.Abs(player.Pitch) < 89.9f && firstPitch * player.Pitch < 0, "opposite pitch limit is also clamped");

                float[] mouseTurns = new float[2];
                int index = 0;
                foreach (int hz in new[] { 30, 120 })
                {
                    Reset(player, Origin);
                    for (int frame = 0; frame < hz; frame++)
                        player.Simulate(Vector2.zero, new Vector2(120f / hz, 0), false, false, false, 1f / hz);
                    mouseTurns[index++] = player.Yaw;
                }
                Check(Mathf.Abs(Mathf.DeltaAngle(mouseTurns[0], mouseTurns[1])) < .02f,
                    $"same mouse displacement gives same turn at 30/120 FPS: {mouseTurns[0]:F4}/{mouseTurns[1]:F4} degrees");

                // Faqat xotiradagi qiymatlar: diskka Save/Reset chaqirilmaydi.
                WorldPreferences.Sensitivity = -20;
                Check(Mathf.Abs(WorldPreferences.Sensitivity - .1f) < .001f, "sensitivity minimum is bounded");
                WorldPreferences.Sensitivity = 200;
                Check(Mathf.Abs(WorldPreferences.Sensitivity - 3f) < .001f, "sensitivity maximum is bounded");
                WorldPreferences.Sensitivity = float.NaN;
                Check(Mathf.Abs(WorldPreferences.Sensitivity - 1f) < .001f, "invalid sensitivity uses finite safe default");
                WorldPreferences.Sensitivity = sensitivity;
                WorldPreferences.FieldOfView = -20;
                Check(Mathf.Abs(WorldPreferences.FieldOfView - 65) < .001f, "field of view minimum is bounded");
                WorldPreferences.FieldOfView = 200;
                Check(Mathf.Abs(WorldPreferences.FieldOfView - 100) < .001f, "field of view maximum is bounded");
                WorldPreferences.FieldOfView = float.PositiveInfinity;
                Check(Mathf.Abs(WorldPreferences.FieldOfView - 80) < .001f, "invalid field of view uses finite safe default");
                WorldPreferences.FieldOfView = 95;
                Simulate(player, 1);
                Check(Mathf.Abs(player.ViewCamera.fieldOfView - 95) < .05f, "world field-of-view preference reaches live camera");
                WorldPreferences.FieldOfView = fieldOfView;

                Reset(player, Origin);
                Simulate(player, .5f, move: Vector2.up);
                player.SetPaused(true);
                start = player.transform.position;
                previousYaw = player.Yaw;
                for (int i = 0; i < 60; i++)
                    player.Simulate(Vector2.up, Vector2.one * 10, true, true, true, 1f / 60);
                Check(player.Paused && Vector3.Distance(start, player.transform.position) < .0001f, "pause blocks movement and jumping");
                Check(Mathf.Abs(Mathf.DeltaAngle(previousYaw, player.Yaw)) < .0001f, "pause blocks mouse look");
                player.SetPaused(false);

                Reset(player, Origin);
                player.Capsule.enabled = false;
                player.transform.position = new Vector3(0, -60, 0);
                player.Capsule.enabled = true;
                Physics.SyncTransforms();
                Simulate(player, .5f);
                Check(player.transform.position.y > -.2f && FlatDistance(player.transform.position, Origin) < .1f, "out-of-world fall safely respawns at known position");
                Check(player.IsGrounded, "respawn returns to valid ground");

                // Menyu ochilganda haqiqiy kursor yo'lini ham tekshiramiz, lekin jonli inputni o'qitmaymiz.
                player.TestMode = false;
                hud.SetSettings(true);
                player.TestMode = true;
                yield return null;
                Check(hud.SettingsOpen && player.Paused, "world settings pause controller");
                Check(Cursor.visible && Cursor.lockState == CursorLockMode.None, "settings release cursor for controls");
                player.TestMode = false;
                hud.SetSettings(false);
                player.TestMode = true;
                yield return null;
                Check(!hud.SettingsOpen && !player.Paused, "closing world settings resumes controller");
                Check(hud.MapTexture != null && hud.MapTexture.IsCreated() && hud.MapTexture.width >= 128 && hud.MapTexture.width <= 512, "small minimap has a valid bounded render texture");
                Check(hud.MapCamera != null && hud.MapCamera.orthographic && hud.MapCamera.targetTexture == hud.MapTexture, "minimap uses dedicated orthographic camera");
                Check(hud.MapCamera != null && (hud.MapCamera.cullingMask & ((1 << 5) | (1 << 8))) == 0,
                    "minimap excludes UI and player collider layers");
                Reset(player, new Vector3(18, .06f, -9), 90);
                yield return null;
                yield return null;
                Check(hud.MapCamera != null && FlatDistance(hud.MapCamera.transform.position, player.transform.position) < .25f, "minimap tracks actual player position");
                Check(hud.MapCamera != null && Quaternion.Angle(hud.MapCamera.transform.rotation, Quaternion.Euler(90, 0, 0)) < .1f,
                    "minimap camera remains north-up while player turns");
                var marker = hud.transform.Find("WorldMinimapFrame/MapViewport/MapPlayerHeading");
                Check(marker != null && Mathf.Abs(Mathf.DeltaAngle(marker.localEulerAngles.z, -90)) < .1f,
                    "minimap arrow follows player heading independently of map");
                var mapFrame = hud.transform.Find("WorldMinimapFrame") as RectTransform;
                Check(mapFrame != null && mapFrame.anchorMin == new Vector2(0, 1) && mapFrame.anchorMax == new Vector2(0, 1)
                    && mapFrame.anchoredPosition.x > 0 && mapFrame.anchoredPosition.y < 0 && mapFrame.rect.width <= 230,
                    "small minimap is anchored in top-left corner");
                Check(PlayerProfile.Nickname == nickname && PlayerProfile.AvatarId == avatar && PlayerProfile.Outfit == outfit, "saved player profile is unchanged");

                // Bir kadrda o'tgan deterministik sinovlarni FPS o'lchovidan chiqarib tashlaymiz.
                for (int frame = 0; frame < 4; frame++) yield return null;
                float performanceUntil = Time.realtimeSinceStartup + 2.2f;
                float elapsed = 0, worstFrame = 0;
                int sampledFrames = 0, slowFrames = 0;
                while (Time.realtimeSinceStartup < performanceUntil)
                {
                    yield return null;
                    float dt = Time.unscaledDeltaTime;
                    elapsed += dt;
                    worstFrame = Mathf.Max(worstFrame, dt);
                    sampledFrames++;
                    if (dt > 1f / 30) slowFrames++;
                }
                Debug.Log($"[WorldTest] PERFORMANCE: frames={sampledFrames}, average={sampledFrames / Mathf.Max(.001f, elapsed):F1} FPS, " +
                    $"worst={worstFrame * 1000:F2} ms, framesOver33ms={slowFrames}, focused={Application.isFocused}");
            }
            finally
            {
                WorldPreferences.ToggleCrouch = toggleCrouch;
                WorldPreferences.Sensitivity = sensitivity;
                WorldPreferences.FieldOfView = fieldOfView;
                player.Teleport(originalPosition, originalYaw);
                hud.SetSettings(originalSettingsOpen);
                player.TestMode = originalTestMode;
                player.SetPaused(originalPaused);
                Complete();
            }
        }
    }
}

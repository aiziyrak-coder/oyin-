using System;
using System.Collections.Generic;
using System.Reflection;
using CraDev.World;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CraDev
{
    /// <summary>
    /// Synchronous, opt-in regression check. Runs and restores all controller state before yielding,
    /// so neither a live server packet nor a rendered frame can observe the temporary test platform.
    /// No account, microphone, saved preference, network or scene asset is modified.
    /// </summary>
    public static class DevLocomotionSmoke
    {
        static int checks, failures;
        static readonly Vector3 Origin = new Vector3(0f, 1000.08f, 0f);
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        public static int Run(WorldPlayerController player)
        {
            checks = failures = 0;
            Check(player && player.Capsule && player.ViewCamera, "controller, capsule and camera exist");
            if (!player || !player.Capsule || !player.ViewCamera) return Complete();
            var capsule = player.Capsule;
            var camera = player.ViewCamera;
            Vector3 originalPosition = player.transform.position, cameraPosition = camera.transform.localPosition;
            Quaternion originalRotation = player.transform.rotation, cameraRotation = camera.transform.localRotation;
            float oldHeight = capsule.height, oldStep = capsule.stepOffset, oldFov = camera.fieldOfView;
            Vector3 oldCenter = capsule.center;
            bool oldCapsuleEnabled = capsule.enabled, oldPaused = player.Paused;
            bool oldToggle = WorldPreferences.ToggleCrouch, oldBob = WorldPreferences.HeadBob, oldInvert = WorldPreferences.InvertY;
            float oldSensitivity = WorldPreferences.Sensitivity;
            // Preserve private transient physics/spawn/camera state too, not only visible position.
            var fields = new Dictionary<FieldInfo, object>();
            foreach (var field in typeof(WorldPlayerController).GetFields(PrivateInstance | BindingFlags.Public))
                if (!field.IsInitOnly && !field.IsLiteral) fields[field] = field.GetValue(player);
            var temporary = new GameObject("_LocomotionSmokeOnly");
            try
            {
                player.TestMode = true; player.SetPaused(false); player.ClearActivityPose(); capsule.enabled = true;
                WorldPreferences.ToggleCrouch = false; WorldPreferences.HeadBob = false;
                WorldPreferences.InvertY = false; WorldPreferences.Sensitivity = 1f;
                Box(temporary.transform, "Floor", new Vector3(0, 999.5f, 0), new Vector3(100, 1, 100));
                Physics.SyncTransforms();
                GroundAndSpeed(player);
                WallAndCeiling(player, temporary.transform);
                ActivitiesAndCorrection(player);
                PoseDoesNotAccumulate(temporary.transform);
            }
            catch (Exception exception)
            {
                Check(false, "unexpected regression exception: " + exception);
            }
            finally
            {
                // Disable test colliders immediately; destruction itself may be deferred until end-of-frame.
                temporary.SetActive(false);
                capsule.enabled = false;
                player.ClearActivityPose(); player.SetPaused(oldPaused);
                foreach (var pair in fields) pair.Key.SetValue(player, pair.Value);
                player.transform.SetPositionAndRotation(originalPosition, originalRotation);
                capsule.height = oldHeight; capsule.center = oldCenter; capsule.stepOffset = oldStep;
                capsule.enabled = oldCapsuleEnabled;
                camera.transform.localPosition = cameraPosition; camera.transform.localRotation = cameraRotation; camera.fieldOfView = oldFov;
                WorldPreferences.ToggleCrouch = oldToggle; WorldPreferences.HeadBob = oldBob;
                WorldPreferences.InvertY = oldInvert; WorldPreferences.Sensitivity = oldSensitivity;
                Physics.SyncTransforms(); Object.Destroy(temporary);
            }
            Check((player.transform.position - originalPosition).sqrMagnitude < .000001f &&
                Quaternion.Angle(player.transform.rotation, originalRotation) < .001f &&
                player.Paused == oldPaused && capsule.enabled == oldCapsuleEnabled,
                "controller transform, pause and collision state are restored in the same frame");
            return Complete();
        }

        static void GroundAndSpeed(WorldPlayerController player)
        {
            Reset(player);
            Check(player.IsGrounded && Mathf.Abs(player.transform.position.y - 1000f) < .15f,
                "capsule settles on the isolated solid floor");
            float minimum = float.MaxValue, maximum = 0f;
            foreach (int hz in new[] { 30, 60, 144 })
            {
                Reset(player); Vector3 start = player.transform.position;
                Step(player, 2f, hz, Vector2.up);
                float distance = FlatDistance(start, player.transform.position);
                minimum = Mathf.Min(minimum, distance); maximum = Mathf.Max(maximum, distance);
                Check(distance > 6.2f && distance < 7.2f && player.IsGrounded,
                    $"walk at {hz} Hz advances a stable physical distance: {distance:F4} m");
            }
            Check(maximum - minimum < .08f, $"30/60/144 Hz walking spread stays below eight centimetres: {maximum - minimum:F4} m");
            Reset(player); Vector3 straightStart = player.transform.position; Step(player, 2f, 60, Vector2.up);
            float straight = FlatDistance(straightStart, player.transform.position);
            Reset(player); Vector3 diagonalStart = player.transform.position; Step(player, 2f, 60, Vector2.one);
            float diagonal = FlatDistance(diagonalStart, player.transform.position);
            Check(Mathf.Abs(straight - diagonal) < .04f, $"diagonal input cannot move faster: straight {straight:F4}, diagonal {diagonal:F4} m");
            Reset(player);
            player.Simulate(Vector2.up, Vector2.zero, false, false, false, 1f / 60f);
            float firstSpeed = player.Speed;
            Step(player, .5f, 60, Vector2.up);
            Check(firstSpeed > 0f && firstSpeed < 1f && player.Speed > 3.3f && player.Speed < 3.65f,
                $"acceleration ramps rather than snapping to full speed: {firstSpeed:F3} to {player.Speed:F3} m/s");
            Vector3 stopStart = player.transform.position;
            Step(player, .5f, 60);
            float drift = FlatDistance(stopStart, player.transform.position);
            Check(player.Speed < .02f && drift > .04f && drift < .35f,
                $"deceleration comes to a controlled stop: {drift:F3} m, {player.Speed:F3} m/s");
            Vector3 idleStart = player.transform.position; Step(player, 2f, 60);
            Check(FlatDistance(idleStart, player.transform.position) < .002f, "stationary input has no sliding drift");
            Reset(player); Step(player, 1f, 60, Vector2.up, true);
            Check(player.Speed > 5.8f && player.Speed < 6.2f && player.IsSprinting, "sprint reaches the bounded six-metres-per-second speed");
            float lowestApex = float.MaxValue, highestApex = 0f;
            foreach (int hz in new[] { 30, 60, 144 })
            {
                Reset(player); float floor = player.transform.position.y;
                player.Simulate(Vector2.zero, Vector2.zero, false, false, true, 1f / hz);
                float apex = player.transform.position.y;
                for (int i = 0; i < hz * 2; i++)
                { player.Simulate(Vector2.zero, Vector2.zero, false, false, false, 1f / hz); apex = Mathf.Max(apex, player.transform.position.y); }
                float height = apex - floor; lowestApex = Mathf.Min(lowestApex, height); highestApex = Mathf.Max(highestApex, height);
                Check(height > .85f && height < 1.1f && player.IsGrounded && Mathf.Abs(player.transform.position.y - floor) < .06f,
                    $"jump at {hz} Hz returns to ground with a bounded one-metre apex: {height:F4} m");
            }
            Check(highestApex - lowestApex < .05f, $"jump height remains consistent across frame rates: {highestApex - lowestApex:F4} m spread");
        }

        static void WallAndCeiling(WorldPlayerController player, Transform parent)
        {
            var wall = Box(parent, "Wall", new Vector3(0, 1001.5f, 2), new Vector3(30, 3, .2f));
            Reset(player); Step(player, 2f, 60, Vector2.one);
            Check(player.transform.position.x > 3f && player.Capsule.bounds.max.z < 1.94f && player.IsGrounded,
                $"diagonal wall contact slides tangentially without penetration: x {player.transform.position.x:F3}, front z {player.Capsule.bounds.max.z:F3}");
            Reset(player); Step(player, 2f, 60, Vector2.up, true);
            Check(player.Capsule.bounds.max.z < 1.94f && player.Speed < .08f,
                "sprint into a wall reports actual stopped movement rather than stored input speed");
            Vector3 wallPosition = player.transform.position; Step(player, .5f, 60, Vector2.down);
            Check(player.transform.position.z < wallPosition.z - .7f, "reversing from a wall responds promptly without a velocity backlog");
            wall.SetActive(false); Reset(player);
            Step(player, .5f, 60, crouch: true);
            Check(player.IsCrouching && player.Capsule.height < 1.12f && player.ViewCamera.transform.localPosition.y < 1.05f,
                "crouch smoothly lowers both collision capsule and camera");
            var ceiling = Box(parent, "Ceiling", new Vector3(0, 1001.5f, 0), new Vector3(3, .2f, 3));
            Physics.SyncTransforms(); Step(player, 1f, 60);
            Check(!player.CanStand && player.IsCrouching && player.Capsule.bounds.max.y < 1001.4f,
                "releasing crouch under a low ceiling cannot force the capsule through it");
            Step(player, 2f, 60, Vector2.up); Step(player, .5f, 60);
            Check(player.CanStand && !player.IsCrouching && Mathf.Abs(player.Capsule.height - 1.8f) < .02f,
                "walking clear of the ceiling restores full standing height automatically");
            ceiling.SetActive(false); Physics.SyncTransforms();
        }

        static void ActivitiesAndCorrection(WorldPlayerController player)
        {
            Reset(player);
            Vector3 chair = player.transform.position;
            player.ApplyActivityPose(chair, 37f, "sit");
            player.Simulate(Vector2.one, new Vector2(100f, 50f), true, true, true, .05f);
            Check(player.ActivityLocked && player.ActivityPose == "sit" && (player.transform.position - chair).sqrMagnitude < .000001f && player.Speed == 0f,
                "sitting blocks walking, crouching and jumping even with every movement input held");
            Check(Mathf.Abs(Mathf.DeltaAngle(player.Yaw, 45.5f)) < .02f && Mathf.Abs(player.Pitch + 4.25f) < .02f,
                "sitting still permits exact mouse look without unlocking movement");
            float yaw = player.Yaw, pitch = player.Pitch;
            for (int i = 0; i < 300; i++)
            {
                player.ApplyActivityPose(chair, 37f, "sit");
                player.Simulate(Vector2.one, Vector2.zero, true, true, true, 1f / 60f);
            }
            Check((player.transform.position - chair).sqrMagnitude < .000001f && Mathf.Abs(Mathf.DeltaAngle(player.Yaw, yaw)) < .001f && Mathf.Abs(player.Pitch - pitch) < .001f,
                "three hundred repeated seat snapshots cause neither position nor free-look drift");
            Check(Mathf.Abs(player.ViewCamera.transform.localPosition.y - 1.12f) < .02f,
                "seated camera converges to the chair eye height");
            player.ClearActivityPose(); Step(player, .5f, 60, Vector2.up);
            Check(!player.ActivityLocked && FlatDistance(chair, player.transform.position) > .9f,
                "clearing the activity restores physical walking");
            Vector3 spawn = Origin + new Vector3(-3, 0, -4);
            player.SetWorldSpawn(spawn, 17f);
            player.Simulate(Vector2.zero, new Vector2(200f, -150f), false, false, false, 1f / 60f);
            yaw = player.Yaw; pitch = player.Pitch;
            Vector3 corrected = Origin + new Vector3(5, 0, 5);
            player.CorrectPosition(corrected);
            Check((player.transform.position - corrected).sqrMagnitude < .000001f && Mathf.Abs(Mathf.DeltaAngle(player.Yaw, yaw)) < .001f && Mathf.Abs(player.Pitch - pitch) < .001f,
                "server position correction preserves the mouse yaw and pitch");
            player.Teleport(new Vector3(0f, -45f, 0f), 0f);
            player.Simulate(Vector2.zero, Vector2.zero, false, false, false, 1f / 60f);
            Check((player.transform.position - spawn).sqrMagnitude < .000001f && Mathf.Abs(Mathf.DeltaAngle(player.Yaw, 17f)) < .001f,
                "correction never overwrites the saved safe respawn point or heading");
        }

        static void PoseDoesNotAccumulate(Transform parent)
        {
            var model = new GameObject("SyntheticRig"); model.transform.SetParent(parent, false);
            var hip = Bone("Bip01 Pelvis", model.transform, new Vector3(0, .95f, 0));
            var spine = Bone("Bip01 Spine", hip, new Vector3(0, .2f, 0));
            foreach (string side in new[] { "L", "R" })
            {
                float x = side == "L" ? -.1f : .1f;
                var thigh = Bone("Bip01 " + side + " Thigh", hip, new Vector3(x, 0, 0));
                var calf = Bone("Bip01 " + side + " Calf", thigh, new Vector3(0, -.42f, 0));
                Bone("Bip01 " + side + " Foot", calf, new Vector3(0, -.4f, .07f));
                var arm = Bone("Bip01 " + side + " UpperArm", spine, new Vector3(x * 2f, .18f, 0));
                Bone("Bip01 " + side + " Forearm", arm, new Vector3(x * 2f, -.1f, 0));
            }
            var motion = model.AddComponent<WorldAvatarMotion>(); motion.enabled = false; motion.Configure(model);
            var transforms = model.GetComponentsInChildren<Transform>();
            var positions = new Vector3[transforms.Length]; var rotations = new Quaternion[transforms.Length];
            for (int i = 0; i < transforms.Length; i++) { positions[i] = transforms[i].localPosition; rotations[i] = transforms[i].localRotation; }
            var restore = typeof(WorldAvatarMotion).GetMethod("Update", PrivateInstance);
            var animate = typeof(WorldAvatarMotion).GetMethod("LateUpdate", PrivateInstance);
            Check(restore != null && animate != null, "additive avatar pose provides paired restore/apply passes");
            if (restore == null || animate == null) return;
            motion.SetActivity("chess", "sit", "", 0);
            for (int i = 0; i < 1000; i++) { restore.Invoke(motion, null); animate.Invoke(motion, null); }
            restore.Invoke(motion, null);
            bool stable = true;
            for (int i = 0; i < transforms.Length; i++) stable &= (transforms[i].localPosition - positions[i]).sqrMagnitude < .000001f && Quaternion.Angle(transforms[i].localRotation, rotations[i]) < .02f;
            Check(stable, "one thousand additive seated-pose cycles restore every joint with no cumulative bone drift");
            motion.SetActivity("penalty", "keeper", "dive_left", .8f);
            for (int i = 0; i < 200; i++) { restore.Invoke(motion, null); animate.Invoke(motion, null); }
            restore.Invoke(motion, null);
            stable = true;
            for (int i = 0; i < transforms.Length; i++) stable &= (transforms[i].localPosition - positions[i]).sqrMagnitude < .000001f && Quaternion.Angle(transforms[i].localRotation, rotations[i]) < .02f;
            Check(stable, "goalkeeper dives also restore translated hips and rotated joints without drift");
        }

        static Transform Bone(string name, Transform parent, Vector3 position)
        { var value = new GameObject(name).transform; value.SetParent(parent, false); value.localPosition = position; return value; }
        static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var value = new GameObject(name); value.transform.SetParent(parent, false); value.transform.position = position;
            value.AddComponent<BoxCollider>().size = size; return value;
        }
        static void Reset(WorldPlayerController player)
        { player.ClearActivityPose(); player.SetPaused(false); player.Teleport(Origin); Physics.SyncTransforms(); Step(player, .5f, 60); }
        static void Step(WorldPlayerController player, float seconds, int hz, Vector2 move = default, bool sprint = false, bool crouch = false)
        { for (int i = 0; i < Mathf.RoundToInt(seconds * hz); i++) player.Simulate(move, Vector2.zero, sprint, crouch, false, 1f / hz); }
        static float FlatDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        static void Check(bool ok, string description)
        { checks++; if (!ok) failures++; Debug.Log($"[LocomotionTest] {(ok ? "PASS" : "FAIL")}: {description}"); }
        static int Complete() { Debug.Log($"[LocomotionTest] COMPLETE: {checks} checks, {failures} failures"); return failures; }
    }
}

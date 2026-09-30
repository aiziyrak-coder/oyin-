using System;
using System.Collections.Generic;
using System.IO;
using CraDev.World;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CraDev
{
    /// <summary>Read-only scene assertions for the solid chess club and compact penalty stadium.</summary>
    public static class DevSportsEnvironmentSmoke
    {
        public static int Failures { get; private set; }
        public static int Checks { get; private set; }
        static Transform infrastructure;
        static readonly Dictionary<string, List<Transform>> objects = new Dictionary<string, List<Transform>>();

        public static int Run()
        {
            Failures = Checks = 0; objects.Clear();
            infrastructure = GameObject.Find("NewWorldCityInfrastructure")?.transform;
            Check(infrastructure, "city infrastructure exists for sports geometry");
            if (!infrastructure) return Complete();
            Physics.SyncTransforms();
            foreach (var t in infrastructure.GetComponentsInChildren<Transform>(true))
            {
                if (!objects.TryGetValue(t.name, out var list)) { list = new List<Transform>(); objects[t.name] = list; }
                list.Add(t);
            }

            Check(Count("PenaltyStadium_TwoPlayer") == 1, "exactly one two-player penalty stadium exists");
            Check(Count("ChessSolidSideWall") == 2 && Count("ChessSolidFrontWall") == 2 && Count("ChessSolidNorthWall") == 1,
                "chess club has five solid wall sections around its open south doorway");
            bool solid = true;
            foreach (string name in new[] { "ChessSolidSideWall", "ChessSolidFrontWall", "ChessSolidNorthWall" })
            {
                foreach (var t in Named(name))
                {
                    var renderer = t.GetComponent<Renderer>(); var collider = t.GetComponent<Collider>();
                    solid &= renderer && renderer.sharedMaterial && renderer.sharedMaterial.renderQueue <= 2500 &&
                        (!renderer.sharedMaterial.HasProperty("_Color") || renderer.sharedMaterial.color.a > .99f) && collider && !collider.isTrigger;
                }
            }
            Check(solid && Count("ClearArchitecturalGlass") == 0, "club walls are opaque physical masonry, not transparent glass");
            bool lit = Count("ChessTableLight") == 6;
            foreach (var t in Named("ChessTableLight"))
            {
                var light = t.GetComponent<Light>(); lit &= light && light.enabled && light.type == LightType.Point && light.intensity >= 2 && light.range >= 10 && light.shadows == LightShadows.None;
            }
            Check(lit, "six bright shadowless table lights illuminate the enclosed club without expensive realtime shadows");
            foreach (float x in new[] { -2.6f, 0f, 2.6f })
            {
                bool clear = true; for (float z = 48; z <= 54; z += 1) clear &= !InfrastructureBlocked(new Vector3(x, 0, z));
                Check(clear, "six-metre chess entrance remains walkable at x=" + x);
            }
            Check(Count("ChessChair") == 20 && infrastructure.GetComponentsInChildren<CityChessTable>().Length == 10,
                "ten original chess stations retain twenty chairs");
            Check(Count("ChessTrophyCup") == 8 && Count("GalleryCubePedestal") == 2 && Count("ChessLibraryBookcase") == 1 && Count("ChessTrophyDisplay") == 1,
                "club has eight trophy cups, two gallery plinths, a bookcase and trophy cabinet");
            foreach (string id in new[] { "Capablanca", "Polgar", "Carlsen", "Abdusattorov" })
            {
                var t = First("LicensedChessPortrait_" + id); var renderer = t ? t.GetComponent<Renderer>() : null;
                var texture = renderer && renderer.sharedMaterial ? renderer.sharedMaterial.mainTexture : null;
                Check(Count("LicensedChessPortrait_" + id) == 1 && texture && texture.width >= 512 && texture.height >= 512,
                    "licensed photographic portrait is imported: " + id);
                var credit = First("PortraitCredit_" + id)?.GetComponent<TextMesh>();
                Check(credit && !string.IsNullOrWhiteSpace(credit.text), "portrait has an in-world attribution: " + id);
            }
            var nodir = First("LicensedChessPortrait_Abdusattorov");
            bool upright = false;
            if (nodir)
            {
                var texture = nodir.GetComponent<Renderer>().sharedMaterial.mainTexture;
                upright = nodir.localScale.y > nodir.localScale.x && (texture.width <= texture.height || nodir.Find("PortraitExifClockwise90"));
            }
            Check(upright, "Abdusattorov portrait preserves portrait aspect and handles the source EXIF rotation");

            var goal = First("PenaltyGoal_RegulationMouth");
            Check(goal && Vector3.Distance(goal.position, new Vector3(64,0,79)) < .01f, "penalty goal matches the authoritative server coordinates");
            var posts = Named("PenaltyGoalPost"); var crossbar = First("PenaltyGoalCrossbar");
            bool dimensions = posts.Count == 2 && crossbar;
            if (dimensions)
            {
                var left = posts[0].GetComponent<Renderer>().bounds; var right = posts[1].GetComponent<Renderer>().bounds;
                if (left.center.x > right.center.x) { var swap = left; left = right; right = swap; }
                var top = crossbar.GetComponent<Renderer>().bounds;
                dimensions = Mathf.Abs(right.min.x - left.max.x - 7.32f) < .025f && Mathf.Abs(top.min.y - 2.44f) < .025f;
            }
            Check(dimensions, "goal has a real 7.32 x 2.44 metre clear mouth");
            bool physical = posts.Count == 2 && crossbar;
            foreach (var p in posts) physical &= p.GetComponent<Collider>() && !p.GetComponent<Collider>().isTrigger;
            physical &= crossbar && crossbar.GetComponent<Collider>();
            Check(physical, "goalposts and crossbar have physical colliders");
            var net = First("PenaltyGoalNet_VisualOnly");
            Check(net && net.GetComponent<MeshFilter>()?.sharedMesh && net.GetComponentsInChildren<Collider>().Length == 0,
                "visible goal net is a single mesh with no collider interfering with the authoritative ball");
            var spot = First("PenaltySpot_11Metres");
            Check(spot && Mathf.Abs(spot.position.x - 64) < .001f && Mathf.Abs(spot.position.z - 68) < .001f && goal && Mathf.Abs(goal.position.z - spot.position.z - 11) < .001f,
                "penalty spot is exactly eleven metres from the goal line");
            var marker = First("PenaltyInteractionPoint");
            Check(marker && Vector3.Distance(marker.position, new Vector3(64,0,43)) < .001f, "stadium interaction point matches the shared join coordinates");
            foreach (float x in new[] { 61.5f, 64f, 66.5f })
            {
                bool clear = true; for (float z = 38; z <= 50; z += 1) clear &= !InfrastructureBlocked(new Vector3(x, 0, z));
                Check(clear, "stadium entrance is walkable from the south road at x=" + x);
            }
            bool pitch = true; for (float z = 50; z <= 77; z += 1) pitch &= !InfrastructureBlocked(new Vector3(64, 0, z));
            Check(pitch, "centre of pitch is unobstructed from entry to keeper area");
            var pitchSurface = First("PenaltyPitch"); var pitchMaterial = pitchSurface ? pitchSurface.GetComponent<Renderer>().sharedMaterial : null;
            Check(pitchMaterial && pitchMaterial.shader && pitchMaterial.shader.isSupported && pitchMaterial.shader.name == "CraDev/WorldTurf",
                "striped turf uses its supported physical material shader");
            Check(Count("PenaltySpectatorStand") == 2 && Count("StadiumFloodlightMast") == 4 && Count("PenaltyScoreboardScore") == 1,
                "stadium has two stands, four floodlights and one scoreboard");
            bool plotClear = true;
            foreach (var t in Named("PlotSurveyMarker")) if (t.position.x >= 42 && t.position.x <= 84 && t.position.z >= 41 && t.position.z <= 83) plotClear = false;
            Check(plotClear, "old vacant-plot survey markers do not remain inside the stadium");
            string creditsPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ASSET-CREDITS.txt"));
            bool credits = File.Exists(creditsPath);
            if (credits) { string content = File.ReadAllText(creditsPath); credits &= content.Contains("Miroslav.vajdic") && content.Contains("creativecommons.org/licenses/by/4.0/") && content.Contains("Ofb"); }
            Check(credits, "distributable asset credits accompany the executable");
            return Complete();
        }

        static bool InfrastructureBlocked(Vector3 p)
        {
            foreach (var collider in Physics.OverlapCapsule(p + Vector3.up * .38f, p + Vector3.up * 1.5f, .28f, ~0, QueryTriggerInteraction.Ignore))
                if (collider && collider.transform.IsChildOf(infrastructure)) return true;
            return false;
        }
        static List<Transform> Named(string name) => objects.TryGetValue(name, out var list) ? list : new List<Transform>();
        static int Count(string name) => objects.TryGetValue(name, out var list) ? list.Count : 0;
        static Transform First(string name) => objects.TryGetValue(name, out var list) && list.Count > 0 ? list[0] : null;
        static void Check(bool ok, string text) { Checks++; if (!ok) Failures++; Debug.Log($"[SportsEnvironmentTest] {(ok ? "PASS" : "FAIL")}: {text}"); }
        static int Complete() { Debug.Log($"[SportsEnvironmentTest] COMPLETE: {Checks} checks, {Failures} failures"); return Failures; }
    }
}

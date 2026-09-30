using System.Collections.Generic;
using System.Globalization;
using CraDev.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CraDev.EditorTools
{
    static partial class CraDevSceneBuilder
    {
        static readonly float[] CityRoads = { -90, -30, 30, 90 };
        static readonly Dictionary<string, Mesh> CityMeshes = new Dictionary<string, Mesh>();
        static Transform cityRoot;
        static Material cityStone, cityMetal, cityPaint, cityGold, cityGlow;

        // The physical ground is continuous. Road paint and paving overlays have no collider seams.
        static void BuildCityInfrastructure()
        {
            CityMeshes.Clear();
            cityRoot = new GameObject("NewWorldCityInfrastructure").transform;
            var grass = WorldPhotoMaterial("CityGround", "Ground", 2.51f, new Color(.71f, .77f, .63f), .8f);
            grass.SetFloat("_MacroStrength", .25f);
            var asphalt = WorldPhotoMaterial("CityAsphalt", "Asphalt", 3f, new Color(.92f, .94f, .96f), .85f);
            asphalt.SetFloat("_Smoothness", .3f);
            cityStone = WorldPhotoMaterial("CityLimestone", "Concrete", 2f, new Color(.72f, .73f, .7f), .5f);
            cityMetal = WorldMaterial("CityAnthracite", new Color(.07f, .087f, .1f));
            cityMetal.SetFloat("_Metallic", .72f); cityMetal.SetFloat("_Glossiness", .48f);
            // Retain the plain Standard instancing variant used by the runtime chess-piece renderer.
            cityMetal.enableInstancing = true; EditorUtility.SetDirty(cityMetal);
            cityPaint = WorldMaterial("CityRoadPaint", new Color(.85f, .84f, .75f));
            cityGold = WorldMaterial("CityBronze", new Color(.48f, .32f, .16f));
            cityGold.SetFloat("_Metallic", .8f); cityGold.SetFloat("_Glossiness", .42f);
            cityGlow = CityEmissive("CityWarmLight", new Color(1f, .81f, .49f), 1.35f);
            WorldBox("Ground", new Vector3(0, -.5f, 0), new Vector3(300, 1, 300), grass).transform.SetParent(cityRoot);

            // Non-overlapping asphalt strips prevent coplanar flicker at every junction.
            foreach (float z in CityRoads)
                CityBox("AsphaltEastWest", new Vector3(0, .006f, z), new Vector3(300, .012f, 12), asphalt);
            float[] edges = { -150, -96, -84, -36, -24, 24, 36, 84, 96, 150 };
            foreach (float x in CityRoads)
                for (int i = 0; i < edges.Length; i += 2)
                    CityBox("AsphaltNorthSouth", new Vector3(x, .006f, (edges[i] + edges[i + 1]) * .5f),
                        new Vector3(12, .012f, edges[i + 1] - edges[i]), asphalt);

            for (int zi = 0; zi < edges.Length; zi += 2)
            for (int xi = 0; xi < edges.Length; xi += 2)
                CityBlock(edges[xi], edges[xi + 1], edges[zi], edges[zi + 1], xi / 2, zi / 2);
            CityRoadMarkings();
            CityStreetFurniture();
            CityChessPavilion();
            CityMonitor(new Vector3(-18, 0, 40), 0);
            CityMonitor(new Vector3(18, 0, 40), 0);
            CityMonitor(new Vector3(-65, 0, -20), 180);
            CityMonitor(new Vector3(65, 0, -20), 180);

            CityDistrictBoundary();
            Debug.Log("[CraDev] City: 300x300m, 8 roads, 16 junctions, vacant plots, open chess pavilion and 10 tables.");
        }

        static GameObject CityBox(string name, Vector3 position, Vector3 size, Material material, bool collider = false, bool bevel = false, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent ? parent : cityRoot, false); go.transform.position = position;
            go.GetComponent<Renderer>().sharedMaterial = material; go.isStatic = true;
            if (name == "LaneDash" || name == "ZebraCrossing" || name == "StopLine" || name == "PavingJoint" ||
                name == "StormDrain" || name == "DrainBar" || name == "PlotCornerX" || name == "PlotCornerZ")
            {
                // Millimetre-thin decals must never cast shadows on the very surface they decorate.
                go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                go.GetComponent<Renderer>().receiveShadows = false;
            }
            if (bevel)
            {
                string key = "CityBox_" + size.x.ToString("0.####", CultureInfo.InvariantCulture) + "_" +
                    size.y.ToString("0.####", CultureInfo.InvariantCulture) + "_" + size.z.ToString("0.####", CultureInfo.InvariantCulture);
                if (!CityMeshes.TryGetValue(key, out var mesh)) { mesh = WorldBevelMesh(key, size); CityMeshes.Add(key, mesh); }
                go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<BoxCollider>().size = size;
            }
            else go.transform.localScale = size;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static Material CityEmissive(string name, Color color, float strength)
        {
            var material = WorldMaterial(name, color);
            material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * strength);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            EditorUtility.SetDirty(material); return material;
        }

        static void CityDistrictBoundary()
        {
            // Development-site hoarding stays INSIDE the server's ±145m limit. It is visible and physical,
            // so players meet an understandable boundary instead of repeated network corrections.
            var panel = WorldPhotoMaterial("CityBoundaryPanels", "Concrete", 3f, new Color(.19f, .23f, .23f), .24f);
            foreach (float edge in new[] { -144f, 144f })
            {
                CityBox("DistrictBoundaryWallX", new Vector3(edge, 1.2f, 0), new Vector3(.22f, 2.4f, 288), panel, true);
                CityBox("DistrictBoundaryWallZ", new Vector3(0, 1.2f, edge), new Vector3(288, 2.4f, .22f), panel, true);
                CityBox("BoundaryStonePlinth", new Vector3(edge, .16f, 0), new Vector3(.46f, .32f, 288), cityStone, true);
                CityBox("BoundaryStonePlinth", new Vector3(0, .16f, edge), new Vector3(288, .32f, .46f), cityStone, true);
                CityBox("BoundaryBronzeCoping", new Vector3(edge, 2.4f, 0), new Vector3(.25f, .08f, 288), cityGold);
                CityBox("BoundaryBronzeCoping", new Vector3(0, 2.4f, edge), new Vector3(288, .08f, .25f), cityGold);
                for (float p = -144; p <= 144; p += 6)
                {
                    CityBox("BoundaryFrame", new Vector3(edge, 1.2f, p), new Vector3(.3f, 2.4f, .075f), cityMetal);
                    CityBox("BoundaryFrame", new Vector3(p, 1.2f, edge), new Vector3(.075f, 2.4f, .3f), cityMetal);
                }
            }
        }

        static void CityBlock(float minX, float maxX, float minZ, float maxZ, int column, int row)
        {
            float width = maxX - minX, depth = maxZ - minZ, x = (minX + maxX) / 2, z = (minZ + maxZ) / 2;
            foreach (float side in new[] { minX + 1.75f, maxX - 1.75f })
                CityBox("Footway", new Vector3(side, .04f, z), new Vector3(3.5f, .08f, depth), cityStone, true);
            foreach (float side in new[] { minZ + 1.75f, maxZ - 1.75f })
                CityBox("Footway", new Vector3(x, .04f, side), new Vector3(width - 7, .08f, 3.5f), cityStone, true);
            foreach (float side in new[] { minX + .12f, maxX - .12f })
                CityBox("Kerb", new Vector3(side, .075f, z), new Vector3(.24f, .15f, depth), cityStone, true);
            foreach (float side in new[] { minZ + .12f, maxZ - .12f })
                CityBox("Kerb", new Vector3(x, .075f, side), new Vector3(width - .5f, .15f, .24f), cityStone, true);
            // Open plots are intentionally not buildings: just flat landscaped land and survey markers.
            if (column == 2 && row == 3) return;
            foreach (float px in new[] { minX + 5, maxX - 5 })
            foreach (float pz in new[] { minZ + 5, maxZ - 5 })
            {
                CityBox("PlotSurveyMarker", new Vector3(px, .15f, pz), new Vector3(.09f, .3f, .09f), cityGold);
                CityBox("PlotCornerX", new Vector3(px + (px < x ? .6f : -.6f), .012f, pz), new Vector3(1.2f, .02f, .065f), cityStone);
                CityBox("PlotCornerZ", new Vector3(px, .012f, pz + (pz < z ? .6f : -.6f)), new Vector3(.065f, .02f, 1.2f), cityStone);
            }
            // Inset pavement joints and drainage are visual-only to keep walking perfectly smooth.
            for (float p = minX + 4; p < maxX - 2; p += 2)
                foreach (float side in new[] { minZ + 1.8f, maxZ - 1.8f })
                    CityBox("PavingJoint", new Vector3(p, .092f, side), new Vector3(.015f, .002f, 3), cityMetal);
            for (float p = minZ + 4; p < maxZ - 2; p += 4)
                foreach (float side in new[] { minX + 1.8f, maxX - 1.8f })
                    CityBox("PavingJoint", new Vector3(side, .092f, p), new Vector3(3, .002f, .015f), cityMetal);
        }

        static bool CityNearJunction(float value, float distance)
        {
            foreach (float road in CityRoads) if (Mathf.Abs(value - road) < distance) return true;
            return false;
        }

        static void CityRoadMarkings()
        {
            foreach (float road in CityRoads)
            for (float p = -144; p <= 144; p += 8)
            {
                if (CityNearJunction(p, 12)) continue;
                CityBox("LaneDash", new Vector3(p, .03f, road), new Vector3(3.5f, .004f, .12f), cityPaint);
                CityBox("LaneDash", new Vector3(road, .03f, p), new Vector3(.12f, .004f, 3.5f), cityPaint);
            }
            foreach (float x in CityRoads)
            foreach (float z in CityRoads)
            {
                for (int stripe = 0; stripe < 9; stripe++)
                {
                    float delta = -4.4f + stripe * 1.1f;
                    foreach (float side in new[] { -8.3f, 8.3f })
                    {
                        CityBox("ZebraCrossing", new Vector3(x + delta, .03f, z + side), new Vector3(.55f, .004f, 2.6f), cityPaint);
                        CityBox("ZebraCrossing", new Vector3(x + side, .03f, z + delta), new Vector3(2.6f, .004f, .55f), cityPaint);
                    }
                }
                foreach (float side in new[] { -10.5f, 10.5f })
                {
                    CityBox("StopLine", new Vector3(x + 3 * Mathf.Sign(side), .03f, z + side), new Vector3(5.3f, .004f, .25f), cityPaint);
                    CityBox("StopLine", new Vector3(x + side, .03f, z - 3 * Mathf.Sign(side)), new Vector3(.25f, .004f, 5.3f), cityPaint);
                }
            }
        }

        static void CityStreetFurniture()
        {
            var signBlue = WorldMaterial("CityDirectionSign", new Color(.055f, .2f, .27f));
            var signRed = WorldMaterial("CityStopSign", new Color(.62f, .04f, .03f));
            foreach (float x in CityRoads)
            foreach (float z in CityRoads)
            {
                CityStreetLamp(new Vector3(x - 7.4f, 0, z - 7.4f), 45);
                CityStreetLamp(new Vector3(x + 7.4f, 0, z + 7.4f), 225);
                CityRoadSign(new Vector3(x - 7.1f, 0, z + 10.4f), "30", signRed, 180);
                CityRoadSign(new Vector3(x + 7.1f, 0, z - 10.4f), "PIYODA", signBlue, 0);
            }
            CityWayfinding(new Vector3(-9, 0, 22), "SHAXMAT KLUBI  ↑", 0);
            CityWayfinding(new Vector3(21.5f, 0, 39), "SHAXMAT KLUBI  ←", 90);
            CityWayfinding(new Vector3(-21.5f, 0, 39), "SHAXMAT KLUBI  →", -90);
            // Recessed storm drains sit beside the kerb, not across the pedestrian route.
            foreach (float road in CityRoads)
            for (float p = -130; p <= 130; p += 40)
            {
                if (CityNearJunction(p, 13)) continue;
                CityBox("StormDrain", new Vector3(p, .017f, road + 5.65f), new Vector3(.8f, .012f, .45f), cityMetal);
                for (int i = 0; i < 7; i++)
                    CityBox("DrainBar", new Vector3(p - .32f + i * .105f, .04f, road + 5.65f), new Vector3(.028f, .012f, .43f), cityStone);
            }
        }

        static void CityStreetLamp(Vector3 p, float yaw)
        {
            var lamp = new GameObject("StreetLamp").transform; lamp.SetParent(cityRoot); lamp.position = p;
            CityBox("LampBase", p + Vector3.up * .15f, new Vector3(.44f, .3f, .44f), cityStone, true, true, lamp);
            CityBox("LampMast", p + Vector3.up * 3.2f, new Vector3(.12f, 6.4f, .12f), cityMetal, true, true, lamp);
            CityBox("LampHood", p + new Vector3(.45f, 6.35f, 0), new Vector3(1.2f, .16f, .4f), cityMetal, false, true, lamp);
            CityBox("LampLED", p + new Vector3(.45f, 6.265f, 0), new Vector3(.92f, .018f, .25f), cityGlow, false, false, lamp);
            lamp.rotation = Quaternion.Euler(0, yaw, 0);
        }

        static void CityRoadSign(Vector3 p, string label, Material face, float yaw)
        {
            var sign = new GameObject("RoadSign_" + label).transform; sign.SetParent(cityRoot); sign.position = p;
            CityBox("SignPost", p + Vector3.up * 1.35f, new Vector3(.06f, 2.7f, .06f), cityMetal, true, false, sign);
            if (label == "30")
            {
                CitySignDisc("SpeedLimitRedRim", p + Vector3.up * 2.55f, .8f, .025f, face, sign);
                CitySignDisc("SpeedLimitWhiteFace", p + new Vector3(0, 2.55f, -.03f), .64f, .008f, cityPaint, sign);
                CityText("SpeedLimit30", label, p + new Vector3(0, 2.55f, -.09f), .35f, new Color(.08f, .09f, .1f), sign);
            }
            else
            {
                CityBox("ReflectiveBorder", p + Vector3.up * 2.55f, new Vector3(.86f, .7f, .05f), cityPaint, false, true, sign);
                CityBox("SignFace", p + new Vector3(0, 2.55f, -.03f), new Vector3(.8f, .64f, .014f), face, false, true, sign);
                CityPedestrianSymbol(p + new Vector3(0, 2.55f, -.09f), sign);
            }
            sign.rotation = Quaternion.Euler(0, yaw, 0);
        }

        static void CitySignDisc(string name, Vector3 p, float diameter, float halfDepth, Material material, Transform parent)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder); disc.name = name;
            disc.transform.SetParent(parent); disc.transform.position = p; disc.transform.rotation = Quaternion.Euler(90, 0, 0);
            disc.transform.localScale = new Vector3(diameter, halfDepth, diameter); disc.isStatic = true;
            disc.GetComponent<Renderer>().sharedMaterial = material; Object.DestroyImmediate(disc.GetComponent<Collider>());
        }

        static void CityPedestrianSymbol(Vector3 p, Transform parent)
        {
            if (!CityMeshes.TryGetValue("PedestrianTriangle", out var mesh))
            {
                mesh = new Mesh { name = "CityPedestrianTriangle" };
                mesh.vertices = new[] { new Vector3(-.34f, -.27f, 0), new Vector3(.34f, -.27f, 0), new Vector3(0, .28f, 0) };
                mesh.triangles = new[] { 0, 2, 1 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                mesh = SaveWorldGeometry(mesh, "CityPedestrianTriangle"); CityMeshes.Add("PedestrianTriangle", mesh);
            }
            var triangle = new GameObject("PedestrianSignTriangle", typeof(MeshFilter), typeof(MeshRenderer));
            triangle.transform.SetParent(parent); triangle.transform.position = p;
            triangle.GetComponent<MeshFilter>().sharedMesh = mesh; triangle.GetComponent<Renderer>().sharedMaterial = cityPaint;
            triangle.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            triangle.GetComponent<Renderer>().receiveShadows = false;
            p.z -= .035f;
            CitySignDisc("PedestrianHead", p + new Vector3(.025f, .135f, 0), .085f, .004f, cityMetal, parent);
            CitySignLine(p + new Vector3(.015f, .075f, 0), p + new Vector3(-.03f, -.045f, 0), .034f, parent);
            CitySignLine(p + new Vector3(-.012f, .03f, 0), p + new Vector3(-.13f, -.02f, 0), .028f, parent);
            CitySignLine(p + new Vector3(0, .035f, 0), p + new Vector3(.1f, -.035f, 0), .028f, parent);
            CitySignLine(p + new Vector3(-.03f, -.04f, 0), p + new Vector3(-.15f, -.185f, 0), .034f, parent);
            CitySignLine(p + new Vector3(-.03f, -.04f, 0), p + new Vector3(.095f, -.185f, 0), .034f, parent);
            for (int i = 0; i < 5; i++)
            {
                var stripe = CityBox("PedestrianSignStripe", p + new Vector3(-.22f + i * .11f, -.222f, 0),
                    new Vector3(.065f, .027f, .008f), cityMetal, false, false, parent);
                stripe.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        static void CitySignLine(Vector3 start, Vector3 end, float width, Transform parent)
        {
            var line = CityBox("PedestrianSymbolStroke", (start + end) * .5f, new Vector3(width, Vector3.Distance(start, end), .008f), cityMetal, false, false, parent);
            line.transform.rotation = Quaternion.FromToRotation(Vector3.up, end - start);
            line.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            line.GetComponent<Renderer>().receiveShadows = false;
        }

        static void CityWayfinding(Vector3 p, string label, float yaw)
        {
            var sign = new GameObject("ChessWayfinding").transform; sign.SetParent(cityRoot); sign.position = p;
            CityBox("WayfindingPost", p + Vector3.up * 1.25f, new Vector3(.1f, 2.5f, .1f), cityMetal, true, false, sign);
            CityBox("WayfindingPanel", p + Vector3.up * 2.4f, new Vector3(3.8f, .68f, .09f), cityMetal, false, true, sign);
            CityText("WayfindingText", label, p + new Vector3(0, 2.4f, -.11f), .2f, new Color(.98f, .94f, .82f), sign);
            sign.rotation = Quaternion.Euler(0, yaw, 0);
        }

        static TextMesh CityText(string name, string text, Vector3 p, float height, Color color, Transform parent = null)
        {
            var go = new GameObject(name); go.transform.SetParent(parent ? parent : cityRoot, false); go.transform.position = p;
            var label = go.AddComponent<TextMesh>(); label.text = text; label.font = Kit().SemiBold;
            label.fontSize = 64; label.characterSize = height / 6.4f; label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center; label.color = color; label.richText = false;
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = label.font.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            // Dynamic fonts rebuild their atlas at runtime; their UVs must not be frozen by static batching.
            go.isStatic = false; return label;
        }

        static void CityChessPavilion()
        {
            var pavilion = new GameObject("ChessPavilion_OpenEntrance").transform; pavilion.SetParent(cityRoot);
            var pale = WorldPhotoMaterial("ChessTravertine", "Concrete", 3.1f, new Color(.94f, .88f, .75f), .32f);
            pale.SetFloat("_Smoothness", .6f);
            var walnut = WorldPhotoMaterial("ChessWarmInterior", "CityWood", 1f, new Color(.62f, .53f, .45f), .35f);
            walnut.SetFloat("_Smoothness", .75f);
            var floor = WorldPhotoMaterial("ChessHonedFloor", "CityFloor", 1.5f, new Color(.93f, .95f, .96f), .3f);
            floor.SetFloat("_Smoothness", .9f);
            var glass = WorldMaterial("ChessArchitecturalGlass", new Color(.55f, .73f, .76f, .13f));
            glass.SetFloat("_Mode", 3); glass.SetInt("_SrcBlend", (int)BlendMode.One);
            glass.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha); glass.SetInt("_ZWrite", 0);
            glass.DisableKeyword("_ALPHATEST_ON"); glass.DisableKeyword("_ALPHABLEND_ON"); glass.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            glass.renderQueue = 3000; glass.SetFloat("_Glossiness", .9f); glass.SetFloat("_Metallic", .2f);
            EditorUtility.SetDirty(glass);

            CityBox("ChessForecourt", new Vector3(0, .015f, 58.5f), new Vector3(47, .01f, 45), pale, false, false, pavilion);
            CityBox("PavilionFloor", new Vector3(0, .03f, 64), new Vector3(44, .006f, 28), floor, false, false, pavilion);
            // No threshold or invisible door collider: six-metre south opening is walk-through.
            CityBox("WestStonePlinth", new Vector3(-21.8f, .3f, 64), new Vector3(.4f, .6f, 28), pale, true, true, pavilion);
            CityBox("EastStonePlinth", new Vector3(21.8f, .3f, 64), new Vector3(.4f, .6f, 28), pale, true, true, pavilion);
            CityBox("NorthStonePlinth", new Vector3(0, .3f, 77.8f), new Vector3(43.2f, .6f, .4f), pale, true, true, pavilion);
            foreach (float side in new[] { -12.4f, 12.4f })
                CityBox("FrontStonePlinth", new Vector3(side, .3f, 50.2f), new Vector3(18.8f, .6f, .4f), pale, true, true, pavilion);
            foreach (float x in new[] { -21.8f, 21.8f })
                CityGlassPanel(new Vector3(x, 2.75f, 64), new Vector3(.06f, 4.3f, 27.4f), glass, pavilion);
            CityGlassPanel(new Vector3(0, 2.75f, 77.8f), new Vector3(43.2f, 4.3f, .06f), glass, pavilion);
            foreach (float side in new[] { -12.4f, 12.4f })
                CityGlassPanel(new Vector3(side, 2.75f, 50.2f), new Vector3(18.8f, 4.3f, .06f), glass, pavilion);

            // Fine mullions, deep stone reveals and an overhanging roof give the pavilion real scale.
            foreach (float x in new[] { -21.8f, -14f, -7f, 7f, 14f, 21.8f })
            {
                foreach (float z in new[] { 50.15f, 77.85f })
                    CityBox("BronzeMullion", new Vector3(x, 2.5f, z), new Vector3(.12f, 5, .16f), cityGold, true, true, pavilion);
            }
            foreach (float x in new[] { -3.15f, 3.15f })
                CityBox("EntryStonePier", new Vector3(x, 2.65f, 50.1f), new Vector3(.3f, 5.3f, .6f), pale, true, true, pavilion);
            foreach (float x in new[] { -21.8f, 21.8f })
            foreach (float z in new[] { 57, 64, 71 })
                CityBox("SideMullion", new Vector3(x, 2.5f, z), new Vector3(.16f, 5, .12f), cityGold, true, true, pavilion);
            CityBox("FloatingStoneRoof", new Vector3(0, 5.35f, 64), new Vector3(46, .65f, 30), pale, true, true, pavilion);
            CityBox("RoofShadowGap", new Vector3(0, 4.975f, 64), new Vector3(44.7f, .12f, 28.7f), cityMetal, false, true, pavilion);
            CityBox("EntryCanopy", new Vector3(0, 4.7f, 48.3f), new Vector3(10, .22f, 4.2f), cityMetal, false, true, pavilion);
            CityText("PavilionName", "SHAXMAT KLUBI", new Vector3(0, 5.35f, 48.66f), .52f, new Color(.29f, .23f, .17f), pavilion);
            CityText("PavilionInvitation", "10 TA DOSKA  ·  BIRGA O‘YNAYMIZ", new Vector3(0, 4.64f, 46.17f), .18f, new Color(.88f, .8f, .61f), pavilion);
            CityText("InteriorWelcome", "SHAXMATCHILAR MAYDONCHASI", new Vector3(0, 3.9f, 77.64f), .48f, new Color(.95f, .89f, .73f), pavilion);

            for (float x = -21; x <= 21; x += 1.2f)
                CityBox("AcousticCeilingSlat", new Vector3(x, 4.82f, 64), new Vector3(.09f, .22f, 26.8f), walnut, false, true, pavilion);
            foreach (float z in new[] { 55f, 64.5f, 74.5f })
            {
                CityBox("CeilingLightRecess", new Vector3(0, 4.68f, z), new Vector3(40, .1f, .18f), cityMetal, false, false, pavilion);
                CityBox("ContinuousWarmLED", new Vector3(0, 4.619f, z), new Vector3(39.6f, .018f, .11f), cityGlow, false, false, pavilion);
            }
            for (int row = 0; row < 2; row++)
            for (int column = 0; column < 5; column++)
                CityChessStation(row * 5 + column, new Vector3(-14 + column * 7, 0, row == 0 ? 59 : 70), walnut, pavilion);
            foreach (float x in new[] { -14f, 0f, 14f })
            foreach (float z in new[] { 59f, 70f })
            {
                var light = NewLight("ChessTableLight", pavilion, LightType.Point, new Color(1f, .9f, .72f), 1.5f, Quaternion.identity);
                light.transform.position = new Vector3(x, 3.6f, z); light.range = 11; light.shadows = LightShadows.None;
                light.renderMode = LightRenderMode.Auto;
            }
            foreach (float x in new[] { -20.4f, 20.4f })
                foreach (float z in new[] { 52f, 75.5f }) CityPlanter(new Vector3(x, 0, z), pavilion);
        }

        static void CityGlassPanel(Vector3 p, Vector3 size, Material glass, Transform parent)
        {
            var pane = CityBox("ClearArchitecturalGlass", p, size, glass, true, false, parent);
            pane.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        static void CityChessStation(int number, Vector3 p, Material wood, Transform parent)
        {
            var station = new GameObject("ChessTable_" + number).transform; station.SetParent(parent); station.position = p;
            station.gameObject.AddComponent<CityChessTable>().TableId = number;
            CityBox("TableTop", p + Vector3.up * .745f, new Vector3(1.5f, .07f, 1.2f), wood, true, true, station);
            CityBox("BoardFrame", p + Vector3.up * .793f, new Vector3(.98f, .026f, .98f), cityGold, false, true, station);
            foreach (float x in new[] { -.48f, .48f })
            {
                CityBox("TablePedestal", p + new Vector3(x, .36f, 0), new Vector3(.08f, .72f, .56f), cityMetal, true, true, station);
                CityBox("TableFoot", p + new Vector3(x, .035f, 0), new Vector3(.4f, .07f, .72f), cityMetal, false, true, station);
            }
            var board = new GameObject("MarbleChessBoard"); board.transform.SetParent(station, false); board.transform.localPosition = Vector3.up * .81f;
            board.isStatic = true;
            board.AddComponent<MeshFilter>().sharedMesh = CityChessBoardMesh();
            var ivory = WorldMaterial("ChessIvorySquares", new Color(.81f, .77f, .65f)); ivory.SetFloat("_Glossiness", .36f);
            var ebony = WorldMaterial("ChessEbonySquares", new Color(.105f, .14f, .13f)); ebony.SetFloat("_Glossiness", .38f);
            board.AddComponent<MeshRenderer>().sharedMaterials = new[] { ebony, ivory };
            CityText("TableNumber", (number + 1).ToString("00"), p + new Vector3(.59f, .791f, -.38f), .085f,
                new Color(.88f, .83f, .63f), station).transform.rotation = Quaternion.Euler(90, 0, 0);
            foreach (float side in new[] { -1.22f, 1.22f }) CityChessChair(p + new Vector3(0, 0, side), Mathf.Sign(side), wood, station);
            // Letters/numbers sit outside the playable .9 m board and cannot receive clicks instead of pieces.
            for (int i = 0; i < 8; i++)
            {
                float offset = (i - 3.5f) * CityChessTable.BoardSize / 8;
                CityText("BoardFile", ((char)('a' + i)).ToString(), p + new Vector3(offset, .808f, -.474f), .032f,
                    new Color(.94f, .9f, .77f), station).transform.rotation = Quaternion.Euler(90, 0, 0);
                CityText("BoardRank", (i + 1).ToString(), p + new Vector3(-.475f, .808f, offset), .032f,
                    new Color(.94f, .9f, .77f), station).transform.rotation = Quaternion.Euler(90, 0, 0);
            }
        }

        static Mesh CityChessBoardMesh()
        {
            if (CityMeshes.TryGetValue("ChessBoard090", out var cached)) return cached;
            var vertices = new List<Vector3>(); var triangles = new[] { new List<int>(), new List<int>() };
            float size = CityChessTable.BoardSize / 8;
            for (int rank = 0; rank < 8; rank++)
            for (int file = 0; file < 8; file++)
            {
                float x = (file - 4) * size, z = (rank - 4) * size; int start = vertices.Count;
                vertices.Add(new Vector3(x, 0, z)); vertices.Add(new Vector3(x, 0, z + size));
                vertices.Add(new Vector3(x + size, 0, z + size)); vertices.Add(new Vector3(x + size, 0, z));
                triangles[(rank + file) % 2].AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
            var mesh = new Mesh { name = "CityChessBoard090", subMeshCount = 2 };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles[0], 0); mesh.SetTriangles(triangles[1], 1);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            cached = SaveWorldGeometry(mesh, "CityChessBoard090"); CityMeshes.Add("ChessBoard090", cached); return cached;
        }

        static void CityChessChair(Vector3 p, float direction, Material wood, Transform parent)
        {
            var chair = new GameObject("ChessChair").transform; chair.SetParent(parent); chair.position = p;
            var fabric = WorldPhotoMaterial("ChessChairFabric", "Concrete", .18f, new Color(.22f, .27f, .25f), .16f);
            CityBox("ChairSeatFrame", p + Vector3.up * .42f, new Vector3(.58f, .07f, .56f), wood, true, true, chair);
            CityBox("ChairUpholstery", p + Vector3.up * .475f, new Vector3(.53f, .055f, .51f), fabric, true, true, chair);
            CityBox("ChairBack", p + new Vector3(0, .75f, direction * .25f), new Vector3(.57f, .52f, .06f), wood, true, true, chair);
            CityBox("BackCushion", p + new Vector3(0, .76f, direction * .212f), new Vector3(.5f, .39f, .027f), fabric, false, true, chair);
            foreach (float x in new[] { -.22f, .22f })
                foreach (float z in new[] { -.2f, .2f })
                    CityBox("ChairLeg", p + new Vector3(x, .21f, z), new Vector3(.04f, .42f, .04f), cityMetal, true, true, chair);
        }

        static void CityPlanter(Vector3 p, Transform parent)
        {
            CityBox("StonePlanter", p + Vector3.up * .38f, new Vector3(.9f, .76f, .9f), cityStone, true, true, parent);
            CityBox("PlanterSoil", p + Vector3.up * .765f, new Vector3(.78f, .03f, .78f), cityMetal, false, false, parent);
            var green = WorldPhotoMaterial("CityPlantLeaves", "Ground", .45f, new Color(.25f, .43f, .22f), .3f);
            for (int i = 0; i < 9; i++)
            {
                float angle = i * Mathf.PI * 2 / 9; var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaf.name = "PlanterLeafCluster"; leaf.transform.SetParent(parent); leaf.isStatic = true;
                leaf.transform.position = p + new Vector3(Mathf.Cos(angle) * .25f, 1 + (i % 3) * .11f, Mathf.Sin(angle) * .25f);
                leaf.transform.localScale = new Vector3(.25f, .82f, .25f);
                leaf.transform.rotation = Quaternion.Euler(Mathf.Sin(angle) * 32, i * 40, Mathf.Cos(angle) * 32);
                leaf.GetComponent<Renderer>().sharedMaterial = green; Object.DestroyImmediate(leaf.GetComponent<Collider>());
            }
        }

        static void CityMonitor(Vector3 p, float yaw)
        {
            var stand = new GameObject("AdvertisementMonitor").transform; stand.SetParent(cityRoot); stand.position = p;
            foreach (float x in new[] { -1.7f, 1.7f })
            {
                CityBox("MonitorFooting", p + new Vector3(x, .13f, 0), new Vector3(.65f, .26f, .7f), cityStone, true, true, stand);
                CityBox("MonitorSupport", p + new Vector3(x, 1.65f, 0), new Vector3(.13f, 3.3f, .16f), cityMetal, true, true, stand);
            }
            CityBox("MonitorEnclosure", p + Vector3.up * 3.3f, new Vector3(5.4f, 2.25f, .22f), cityMetal, true, true, stand);
            CityBox("MonitorAccent", p + new Vector3(0, 2.19f, -.13f), new Vector3(4.9f, .018f, .012f), cityGlow, false, false, stand);
            var display = new GameObject("EmissiveMonitorDisplay", typeof(RectTransform), typeof(Canvas));
            display.transform.SetParent(stand, false); display.transform.localPosition = new Vector3(0, 3.3f, -.2f);
            display.transform.localScale = Vector3.one * .004f;
            var canvas = display.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main; canvas.pixelPerfect = false;
            var rect = display.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(1280, 500);
            var backdrop = display.AddComponent<Image>(); backdrop.color = new Color(.12f, .27f, .26f, 1); backdrop.raycastTarget = false;
            var clip = new GameObject("MarqueeViewport", typeof(RectTransform), typeof(RectMask2D)); clip.transform.SetParent(rect, false);
            var viewport = clip.GetComponent<RectTransform>(); viewport.sizeDelta = new Vector2(1210, 220);
            var first = CityMonitorText(viewport, "Sizning reklamangiz uchun joy");
            var second = CityMonitorText(viewport, "Sizning reklamangiz uchun joy");
            // Large type is readable as a marquee; the complete sentence does not have to fit into a distant95pxsign.
            display.AddComponent<CityBillboard>().Configure(first, second, 2180);
            var upper = CityMonitorText(rect, "REKLAMA MAYDONI"); upper.sizeDelta = new Vector2(1100, 80);
            upper.anchorMin = upper.anchorMax = new Vector2(.5f, .5f); upper.pivot = new Vector2(.5f, .5f);
            upper.anchoredPosition = new Vector2(0, 164); upper.GetComponent<Text>().fontSize = 34;
            upper.GetComponent<Text>().alignment = TextAnchor.MiddleCenter; upper.GetComponent<Text>().color = new Color(.55f, .74f, .68f);
            var lower = CityMonitorText(rect, "•  •  •"); lower.sizeDelta = new Vector2(400, 80);
            lower.anchorMin = lower.anchorMax = new Vector2(.5f, .5f); lower.pivot = new Vector2(.5f, .5f);
            lower.anchoredPosition = new Vector2(0, -171); lower.GetComponent<Text>().fontSize = 27;
            lower.GetComponent<Text>().alignment = TextAnchor.MiddleCenter; lower.GetComponent<Text>().color = new Color(.76f, .65f, .4f);
            // World cameras exclude Unity's overlay UI layer. These physical screens intentionally stay layer 0.
            foreach (Transform child in display.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 0;
            stand.rotation = Quaternion.Euler(0, yaw, 0);
        }

        static RectTransform CityMonitorText(RectTransform parent, string value)
        {
            var text = new GameObject("MonitorText", typeof(RectTransform), typeof(Text)); text.transform.SetParent(parent, false);
            var rect = text.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, .5f);
            rect.pivot = new Vector2(0, .5f); rect.sizeDelta = new Vector2(2050, 220);
            // Separate text from the background image in world depth as well as canvas ordering.
            rect.localPosition = new Vector3(0, 0, -8);
            var label = text.GetComponent<Text>(); label.font = Kit().SemiBold; label.fontSize = 110;
            label.text = value; label.color = new Color(.92f, .94f, .87f); label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Overflow; label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false; label.supportRichText = false; return rect;
        }
    }
}

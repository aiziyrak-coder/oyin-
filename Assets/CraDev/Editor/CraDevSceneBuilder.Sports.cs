using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CraDev.EditorTools
{
    static partial class CraDevSceneBuilder
    {
        static void BuildPenaltyStadium()
        {
            var stadium = new GameObject("PenaltyStadium_TwoPlayer").transform; stadium.SetParent(cityRoot);
            var stone = WorldPhotoMaterial("StadiumArchitecturalStone", "Concrete", 2.2f, new Color(.75f, .77f, .73f), .35f);
            var turf = WorldPhotoMaterial("StadiumMownTurf", "Ground", .9f, new Color(.24f, .53f, .19f), .2f, "CraDev/WorldTurf");
            turf.SetFloat("_Smoothness", .16f);
            var white = WorldMaterial("StadiumGoalEnamel", new Color(.94f, .96f, .91f)); white.SetFloat("_Glossiness", .63f);
            var darkGreen = WorldMaterial("StadiumSeatGreen", new Color(.04f, .2f, .16f)); darkGreen.SetFloat("_Glossiness", .35f);
            var sage = WorldMaterial("StadiumSeatSage", new Color(.33f, .43f, .32f)); sage.SetFloat("_Glossiness", .3f);
            var net = WorldMaterial("StadiumGoalNet", new Color(.84f, .88f, .81f)); net.SetFloat("_Glossiness", .25f);
            var led = CityEmissive("StadiumCoolLED", new Color(.78f, .9f, 1f), 2f);

            // Flush overlays share the continuous city ground collider: no pitch seams or entry steps.
            CityBox("PenaltyStadiumApron", new Vector3(63, .021f, 60), new Vector3(41, .012f, 46), stone, false, false, stadium);
            CityBox("PenaltyPitch", new Vector3(64, .03f, 62.6f), new Vector3(36, .012f, 36.8f), turf, false, false, stadium);
            // Compact training pitch, full-size 7.32 x 2.44 m goal and exact 11 m penalty spot.
            SportsFieldLine(new Vector3(47, .045f, 46), new Vector3(81, .045f, 46), .09f, white, stadium);
            SportsFieldLine(new Vector3(47, .045f, 79), new Vector3(81, .045f, 79), .09f, white, stadium);
            foreach (float x in new[] { 47f, 81f }) SportsFieldLine(new Vector3(x, .045f, 46), new Vector3(x, .045f, 79), .09f, white, stadium);
            SportsFieldLine(new Vector3(49, .046f, 62.5f), new Vector3(79, .046f, 62.5f), .085f, white, stadium);
            foreach (float x in new[] { 49f, 79f }) SportsFieldLine(new Vector3(x, .046f, 62.5f), new Vector3(x, .046f, 79), .085f, white, stadium);
            SportsFieldLine(new Vector3(54.84f, .047f, 73.5f), new Vector3(73.16f, .047f, 73.5f), .08f, white, stadium);
            foreach (float x in new[] { 54.84f, 73.16f }) SportsFieldLine(new Vector3(x, .047f, 73.5f), new Vector3(x, .047f, 79), .08f, white, stadium);
            var spot = GameObject.CreatePrimitive(PrimitiveType.Cylinder); spot.name = "PenaltySpot_11Metres";
            spot.transform.SetParent(stadium); spot.transform.position = new Vector3(64, .044f, 68);
            spot.transform.localScale = new Vector3(.22f, .002f, .22f); spot.GetComponent<Renderer>().sharedMaterial = white;
            spot.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off; Object.DestroyImmediate(spot.GetComponent<Collider>()); spot.isStatic = true;
            // Only the part of the 9.15 m arc outside the compact penalty area is painted.
            for (int i = 0; i < 28; i++)
            {
                float a = Mathf.Lerp(217, 323, i / 28f) * Mathf.Deg2Rad, b = Mathf.Lerp(217, 323, (i + 1) / 28f) * Mathf.Deg2Rad;
                SportsFieldLine(new Vector3(64 + Mathf.Cos(a) * 9.15f, .047f, 68 + Mathf.Sin(a) * 9.15f),
                    new Vector3(64 + Mathf.Cos(b) * 9.15f, .047f, 68 + Mathf.Sin(b) * 9.15f), .08f, white, stadium);
            }

            var goal = new GameObject("PenaltyGoal_RegulationMouth").transform; goal.SetParent(stadium); goal.position = new Vector3(64, 0, 79);
            foreach (float x in new[] { 60.28f, 67.72f })
            {
                SportsCylinder("PenaltyGoalPost", new Vector3(x, .04f, 79), new Vector3(x, 2.5f, 79), .06f, white, goal, true);
                SportsCylinder("PenaltyGoalRearSupport", new Vector3(x, .05f, 81.3f), new Vector3(x, 2.5f, 80.7f), .033f, white, goal);
                SportsCylinder("PenaltyGoalTopSupport", new Vector3(x, 2.5f, 79), new Vector3(x, 2.5f, 80.7f), .033f, white, goal);
                SportsCylinder("PenaltyGoalGroundSupport", new Vector3(x, .05f, 79), new Vector3(x, .05f, 81.3f), .033f, white, goal);
            }
            SportsCylinder("PenaltyGoalCrossbar", new Vector3(60.28f, 2.5f, 79), new Vector3(67.72f, 2.5f, 79), .06f, white, goal, true);
            SportsCylinder("PenaltyGoalRearBar", new Vector3(60.28f, .05f, 81.3f), new Vector3(67.72f, .05f, 81.3f), .033f, white, goal);
            BuildPenaltyNet(goal, net);

            foreach (float x in new[] { 42.7f, 83.3f })
            {
                CityBox("StadiumPerimeterWall", new Vector3(x, .5f, 64), new Vector3(.2f, 1, 38), stone, true, false, stadium);
                SportsCylinder("StadiumSafetyRail", new Vector3(x, 1.25f, 45), new Vector3(x, 1.25f, 83), .035f, cityMetal, stadium);
                for (float z = 45; z <= 83; z += 3.8f) SportsCylinder("StadiumFencePost", new Vector3(x, .9f, z), new Vector3(x, 1.28f, z), .03f, cityMetal, stadium);
            }
            CityBox("StadiumNorthWall", new Vector3(63, .55f, 83), new Vector3(40.6f, 1.1f, .22f), stone, true, false, stadium);
            CityBox("StadiumEntranceWing", new Vector3(51.7f, .55f, 43), new Vector3(18, 1.1f, .25f), stone, true, false, stadium);
            CityBox("StadiumEntranceWing", new Vector3(75.3f, .55f, 43), new Vector3(16, 1.1f, .25f), stone, true, false, stadium);
            foreach (float x in new[] { 60.7f, 67.3f })
                CityBox("StadiumEntryPillar", new Vector3(x, 1.9f, 43), new Vector3(.4f, 3.8f, .65f), cityMetal, true, true, stadium);
            CityBox("StadiumEntranceHeader", new Vector3(64, 3.85f, 43), new Vector3(7.4f, .65f, .7f), cityMetal, false, true, stadium);
            CityText("PenaltyArenaName", "PENALTI ARENA", new Vector3(64, 3.87f, 42.63f), .37f, new Color(.9f, .94f, .8f), stadium);
            CityText("PenaltyArenaInvitation", "2 O‘YINCHI  ·  1 TO‘P  ·  1 G‘ALABA", new Vector3(64, 3.24f, 42.63f), .16f, new Color(.79f, .86f, .73f), stadium);
            var marker = new GameObject("PenaltyInteractionPoint"); marker.transform.SetParent(stadium); marker.transform.position = new Vector3(64, 0, 43);
            CityBox("PenaltyInformationKiosk", new Vector3(68.4f, .95f, 42.5f), new Vector3(.8f, 1.9f, .25f), cityMetal, true, true, stadium);
            CityText("PenaltyInformationText", "PENALTI\n\nE — O‘YNASH\n\nIkki kishilik bahs", new Vector3(68.4f, 1.14f, 42.35f), .1f, Color.white, stadium);

            BuildStadiumStand(new Vector3(44.6f, 0, 63.5f), -90, stone, darkGreen, sage, stadium);
            BuildStadiumStand(new Vector3(82.3f, 0, 63.5f), 90, stone, darkGreen, sage, stadium);
            foreach (float x in new[] { 43.4f, 82.7f })
            foreach (float z in new[] { 46f, 80.8f }) BuildStadiumFloodlight(new Vector3(x, 0, z), led, stadium);
            foreach (float x in new[] { 48.5f, 79.5f })
            {
                CityBox("StadiumAdPanel", new Vector3(x, .8f, 81.9f), new Vector3(8f, 1.1f, .16f), darkGreen, true, true, stadium);
                CityText("StadiumAdCaption", "Sizning reklamangiz uchun joy", new Vector3(x, .86f, 81.79f), .15f, new Color(.84f, .92f, .76f), stadium);
            }
            BuildStadiumScoreboard(stadium);
            CityWayfinding(new Vector3(21.5f, 0, 22), "PENALTI ARENA  →", 0);
            CityWayfinding(new Vector3(40, 0, 40), "PENALTI ARENA  →", 0);
            Debug.Log("[CraDev] Sports: one penalty arena, 7.32x2.44m goal, 11m spot, 2 stands, 4 floodlights; chess solid walls, 4 licensed portraits, trophy gallery.");
        }

        static void SportsFieldLine(Vector3 a, Vector3 b, float width, Material material, Transform parent)
        {
            var line = CityBox("PenaltyPitchMarking", (a + b) * .5f, new Vector3(width, .003f, Vector3.Distance(a, b)), material, false, false, parent);
            line.transform.rotation = Quaternion.LookRotation(b - a, Vector3.up);
            line.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        static void BuildStadiumStand(Vector3 p, float yaw, Material stone, Material seat, Material alternate, Transform parent)
        {
            var stand = new GameObject("PenaltySpectatorStand").transform; stand.SetParent(parent); stand.position = p;
            // Compact two-row terraces face the pitch; colliders stop players clipping through seating.
            for (int row = 0; row < 2; row++)
            {
                float z = row * .72f, y = .22f + row * .35f;
                CityBox("StadiumTerraceStep", p + new Vector3(0, y * .5f, z), new Vector3(23.5f, y, .76f), stone, true, false, stand);
                for (int i = 0; i < 24; i++)
                {
                    if (i == 7 || i == 16) continue; // two clear visual gangways
                    float x = -11.1f + i * .96f;
                    CityBox("StadiumSeatCushion", p + new Vector3(x, y + .28f, z), new Vector3(.69f, .09f, .5f), i % 4 == 0 ? alternate : seat, true, true, stand);
                    CityBox("StadiumSeatBack", p + new Vector3(x, y + .56f, z + .2f), new Vector3(.69f, .57f, .075f), i % 4 == 0 ? alternate : seat, true, true, stand);
                    CityBox("StadiumSeatSupport", p + new Vector3(x, y + .13f, z), new Vector3(.06f, .26f, .32f), cityMetal, false, false, stand);
                }
            }
            CityBox("StadiumStandCanopy", p + new Vector3(0, 3.05f, .15f), new Vector3(25, .16f, 2.35f), cityMetal, false, true, stand);
            CityBox("StadiumCanopyTrim", p + new Vector3(0, 3.03f, -.97f), new Vector3(25, .22f, .05f), cityGold, false, false, stand);
            foreach (float x in new[] { -11.7f, 0f, 11.7f })
                SportsCylinder("StadiumCanopyColumn", p + new Vector3(x, 0, 1.05f), p + new Vector3(x, 3.05f, 1.05f), .055f, cityMetal, stand, true);
            stand.rotation = Quaternion.Euler(0, yaw, 0);
        }

        static void BuildStadiumFloodlight(Vector3 p, Material led, Transform parent)
        {
            CityBox("StadiumFloodlightFoot", p + Vector3.up * .12f, new Vector3(.58f, .24f, .58f), cityStone, true, true, parent);
            SportsCylinder("StadiumFloodlightMast", p, p + Vector3.up * 9, .08f, cityMetal, parent, true);
            var head = CityBox("StadiumFloodlightHead", p + Vector3.up * 8.95f, new Vector3(1.5f, .85f, .16f), cityMetal, false, true, parent);
            float yaw = p.x < 64 ? -60 : 60; head.transform.rotation = Quaternion.Euler(22, yaw, 0);
            for (int i = 0; i < 3; i++)
            for (int j = 0; j < 2; j++)
            {
                var panel = CityBox("StadiumFloodlightLED", Vector3.zero, new Vector3(.39f, .28f, .025f), led, false, true, head.transform);
                panel.transform.localPosition = new Vector3(-.46f + i * .46f, -.18f + j * .36f, -.1f);
                panel.transform.localRotation = Quaternion.identity;
            }
            var light = NewLight("StadiumPitchLight", parent, LightType.Spot, new Color(.81f, .9f, 1), 2.3f, Quaternion.identity);
            light.transform.position = p + Vector3.up * 8.7f; light.transform.LookAt(new Vector3(64, 0, 65));
            light.spotAngle = 85; light.range = 42; light.shadows = LightShadows.None; light.renderMode = LightRenderMode.Auto;
        }

        static void BuildStadiumScoreboard(Transform parent)
        {
            foreach (float x in new[] { 60f, 68f })
                CityBox("PenaltyScoreboardSupport", new Vector3(x, 3.2f, 82.5f), new Vector3(.16f, 6.4f, .16f), cityMetal, true, true, parent);
            CityBox("PenaltyScoreboardCase", new Vector3(64, 5.7f, 82.4f), new Vector3(10.2f, 3f, .3f), cityMetal, true, true, parent);
            var panel = CityEmissive("PenaltyScoreboardScreen", new Color(.025f, .07f, .065f), .7f);
            CityBox("PenaltyScoreboardScreen", new Vector3(64, 5.7f, 82.235f), new Vector3(9.9f, 2.72f, .015f), panel, false, false, parent);
            CityText("PenaltyScoreboardTitle", "PENALTI ARENA", new Vector3(64, 6.58f, 82.21f), .28f, new Color(.68f, .82f, .71f), parent);
            CityText("PenaltyScoreboardScore", "0  :  0", new Vector3(64, 5.65f, 82.2f), .85f, Color.white, parent);
            CityText("PenaltyScoreboardStatus", "IKKI O‘YINCHI KUTILMOQDA", new Vector3(64, 4.72f, 82.2f), .19f, new Color(.74f, .85f, .74f), parent);
        }

        static void BuildPenaltyNet(Transform parent, Material material)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            // Fine round rope is one static mesh, not hundreds of colliders or renderers.
            for (int i = 0; i <= 37; i++)
            {
                float x = Mathf.Lerp(60.34f, 67.66f, i / 37f);
                SportsNetRope(vertices, triangles, new Vector3(x, .08f, 81.24f), new Vector3(x, 2.44f, 80.64f));
                SportsNetRope(vertices, triangles, new Vector3(x, 2.44f, 79.05f), new Vector3(x, 2.44f, 80.64f));
            }
            for (int i = 0; i <= 12; i++)
            {
                float t = i / 12f, y = Mathf.Lerp(.08f, 2.44f, t), rear = Mathf.Lerp(81.24f, 80.64f, t);
                SportsNetRope(vertices, triangles, new Vector3(60.34f, y, rear), new Vector3(67.66f, y, rear));
                foreach (float x in new[] { 60.34f, 67.66f }) SportsNetRope(vertices, triangles, new Vector3(x, y, 79.05f), new Vector3(x, y, rear));
            }
            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f;
                foreach (float x in new[] { 60.34f, 67.66f })
                    SportsNetRope(vertices, triangles, new Vector3(x, .08f, Mathf.Lerp(79.05f, 81.24f, t)), new Vector3(x, 2.44f, Mathf.Lerp(79.05f, 80.64f, t)));
                SportsNetRope(vertices, triangles, new Vector3(60.34f, 2.44f, Mathf.Lerp(79.05f, 80.64f, t)), new Vector3(67.66f, 2.44f, Mathf.Lerp(79.05f, 80.64f, t)));
            }
            var mesh = new Mesh { name = "PenaltyGoalNet" }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh = SaveWorldGeometry(mesh, "PenaltyGoalNet");
            var net = new GameObject("PenaltyGoalNet_VisualOnly", typeof(MeshFilter), typeof(MeshRenderer)); net.transform.SetParent(parent, false);
            net.transform.position = Vector3.zero; net.GetComponent<MeshFilter>().sharedMesh = mesh; net.GetComponent<MeshRenderer>().sharedMaterial = material;
            net.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off; net.isStatic = true;
        }

        static void SportsNetRope(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b)
        {
            var forward = (b - a).normalized; var right = Vector3.Cross(forward, Mathf.Abs(forward.y) < .9f ? Vector3.up : Vector3.forward).normalized * .008f;
            var up = Vector3.Cross(forward, right).normalized * .008f; int n = vertices.Count;
            for (int end = 0; end < 2; end++)
            for (int side = 0; side < 4; side++)
            {
                float angle = side * Mathf.PI * .5f; vertices.Add((end == 0 ? a : b) + right * Mathf.Cos(angle) + up * Mathf.Sin(angle));
            }
            for (int side = 0; side < 4; side++) { int next = (side + 1) % 4; triangles.AddRange(new[] { n + side, n + next, n + side + 4, n + next, n + next + 4, n + side + 4 }); }
        }

        static void BuildChessGallery(Transform pavilion, Material walnut, Material plaster)
        {
            var green = WorldMaterial("ChessHeritageGreen", new Color(.1f, .19f, .16f));
            green.SetFloat("_Glossiness", .2f);
            var ivory = WorldMaterial("ChessGalleryIvory", new Color(.89f, .87f, .8f));
            foreach (float x in new[] { -21.56f, 21.56f })
            {
                CityBox("ChessWallWainscot", new Vector3(x, .68f, 64), new Vector3(.07f, 1.32f, 27.2f), walnut, false, false, pavilion);
                CityBox("ChessBrassDado", new Vector3(x, 1.37f, 64), new Vector3(.085f, .035f, 27.2f), cityGold, false, false, pavilion);
            }
            CityBox("GalleryWainscot", new Vector3(0, .68f, 77.53f), new Vector3(43.1f, 1.32f, .08f), walnut, false, false, pavilion);
            CityBox("GalleryBrassDado", new Vector3(0, 1.37f, 77.48f), new Vector3(43.1f, .035f, .04f), cityGold, false, false, pavilion);
            CityBox("ChessEntryRunner", new Vector3(0, .041f, 54), new Vector3(3.8f, .006f, 7.5f), green, false, false, pavilion);
            CityBox("ChessGalleryRunner", new Vector3(0, .041f, 74.7f), new Vector3(38, .006f, 1.6f), green, false, false, pavilion);

            ChessPortrait("Capablanca", "JOSE RAUL CAPABLANCA", "Agence Rol / BnF · Public domain", new Vector3(-15, 2.9f, 77.38f), 5227f / 7290f, ivory, pavilion);
            ChessPortrait("Polgar", "JUDIT POLGAR", "Noord-Hollands Archief / Fotoburo de Boer · CC0", new Vector3(-5, 2.9f, 77.38f), 2304f / 3443f, ivory, pavilion);
            ChessPortrait("Carlsen", "MAGNUS CARLSEN", "Miroslav.vajdic · CC BY 4.0 · crop: SpyroeBM", new Vector3(5, 2.9f, 77.38f), 1558f / 1906f, ivory, pavilion);
            ChessPortrait("Abdusattorov", "NODIRBEK ABDUSATTOROV", "Ofb · Wikimedia Commons · CC0", new Vector3(15, 2.9f, 77.38f), 1603f / 2390f, ivory, pavilion);
            CityText("PortraitLicenseNotice", "SURATLAR: WIKIMEDIA COMMONS · creativecommons.org/licenses/by/4.0\nMualliflar va litsenziyalar: o‘yin bilan birga berilgan ASSET-CREDITS.txt", new Vector3(0, .72f, 77.38f), .105f, new Color(.85f, .79f, .63f), pavilion);

            foreach (float x in new[] { -10f, 10f })
            {
                CityBox("GalleryCubePedestal", new Vector3(x, .48f, 76.1f), new Vector3(.9f, .96f, .9f), plaster, true, true, pavilion);
                CityBox("PedestalBronzeCap", new Vector3(x, .98f, 76.1f), new Vector3(.92f, .04f, .92f), cityGold, false, true, pavilion);
                ChessTrophy(new Vector3(x, 1.0f, 76.1f), x < 0 ? .9f : .74f, pavilion);
            }
            // Storage is against the side walls, well outside the ten table/chair interaction paths.
            ChessBookcase(new Vector3(-20.9f, 0, 64.5f), 90, walnut, green, pavilion);
            ChessTrophyCabinet(new Vector3(20.9f, 0, 64.5f), -90, walnut, green, pavilion);
            foreach (float x in new[] { -18f, 18f })
            {
                CityBox("ClubLoungeBench", new Vector3(x, .26f, 52), new Vector3(3.2f, .52f, .72f), walnut, true, true, pavilion);
                CityBox("ClubLoungeCushion", new Vector3(x, .55f, 52), new Vector3(3.1f, .12f, .65f), green, false, true, pavilion);
            }
        }

        static void ChessPortrait(string id, string caption, string credit, Vector3 p, float ratio, Material mat, Transform parent)
        {
            string path = "Assets/CraDev/World/Art/ChessGallery/" + id + ".jpg";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.maxTextureSize = 2048; importer.sRGBTexture = true; importer.mipmapEnabled = true;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.wrapMode = TextureWrapMode.Clamp; importer.anisoLevel = 4; importer.SaveAndReimport();
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (!texture) throw new System.InvalidOperationException("Licensed gallery portrait missing: " + path);
            float h = 2.42f, w = h * ratio;
            CityBox("PortraitFrame_" + id, p + new Vector3(0, 0, .07f), new Vector3(w + .22f, h + .22f, .14f), cityGold, false, true, parent);
            CityBox("PortraitMount_" + id, p + new Vector3(0, 0, -.011f), new Vector3(w + .11f, h + .11f, .023f), mat, false, false, parent);
            var art = WorldMaterial("Portrait_" + id, Color.white);
            art.mainTexture = texture; art.SetFloat("_Glossiness", .08f);
            art.EnableKeyword("_EMISSION"); art.SetTexture("_EmissionMap", texture); art.SetColor("_EmissionColor", Color.white * .21f);
            EditorUtility.SetDirty(art);
            var photo = GameObject.CreatePrimitive(PrimitiveType.Quad); photo.name = "LicensedChessPortrait_" + id;
            photo.transform.SetParent(parent); photo.transform.position = p + new Vector3(0, 0, -.03f);
            photo.transform.localScale = new Vector3(w, h, 1); photo.isStatic = true;
            photo.GetComponent<Renderer>().sharedMaterial = art; Object.DestroyImmediate(photo.GetComponent<Collider>());
            // This CC0 original is encoded landscape with EXIF rotation=6. Keep the source bytes intact;
            // Unity importers that ignore EXIF need a clockwise UV rotation (not a stretched portrait).
            if (id == "Abdusattorov" && texture.width > texture.height)
            {
                var mesh = Object.Instantiate(photo.GetComponent<MeshFilter>().sharedMesh); mesh.name = "AbdusattorovExifPortrait";
                var uv = mesh.uv; for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(1 - uv[i].y, uv[i].x);
                mesh.uv = uv; photo.GetComponent<MeshFilter>().sharedMesh = SaveWorldGeometry(mesh, "AbdusattorovExifPortrait");
                new GameObject("PortraitExifClockwise90").transform.SetParent(photo.transform, false);
            }
            CityText("PortraitCaption_" + id, caption, p + new Vector3(0, -1.52f, -.015f), .19f, new Color(.22f, .2f, .16f), parent);
            CityText("PortraitCredit_" + id, credit, p + new Vector3(0, -1.76f, -.015f), .075f, new Color(.87f, .82f, .68f), parent);
            CityBox("GalleryPictureLight", p + new Vector3(0, 1.49f, -.17f), new Vector3(w * .85f, .045f, .13f), cityGold, false, true, parent);
            CityBox("GalleryPictureLED", p + new Vector3(0, 1.459f, -.17f), new Vector3(w * .77f, .013f, .09f), cityGlow, false, false, parent);
        }

        static void ChessBookcase(Vector3 p, float yaw, Material wood, Material green, Transform parent)
        {
            var furniture = new GameObject("ChessLibraryBookcase").transform; furniture.SetParent(parent); furniture.position = p;
            CityBox("BookcaseBack", p + Vector3.up * 1.35f, new Vector3(5.4f, 2.7f, .16f), green, true, true, furniture);
            foreach (float x in new[] { -2.74f, 2.74f }) CityBox("BookcaseSide", p + new Vector3(x, 1.35f, -.2f), new Vector3(.1f, 2.7f, .6f), wood, true, true, furniture);
            var covers = new[] { green, cityGold, cityMetal, WorldMaterial("ChessBooksTerracotta", new Color(.38f, .15f, .09f)) };
            for (int shelf = 0; shelf < 4; shelf++)
            {
                float y = .15f + shelf * .64f;
                CityBox("LibraryShelf", p + new Vector3(0, y, -.2f), new Vector3(5.5f, .065f, .62f), wood, true, true, furniture);
                for (int i = 0; i < 22; i++)
                {
                    float height = .36f + (i * 7 + shelf * 3) % 5 * .031f;
                    CityBox("ChessBook", p + new Vector3(-2.48f + i * .231f, y + .038f + height * .5f, -.24f), new Vector3(.15f, height, .35f), covers[(i + shelf) % 4], false, true, furniture);
                    CityBox("BookGoldSpine", p + new Vector3(-2.48f + i * .231f, y + .13f, -.423f), new Vector3(.106f, .012f, .006f), cityGold, false, false, furniture);
                }
            }
            CityText("LibraryTitle", "SHAXMAT KUTUBXONASI", p + new Vector3(0, 3, -.08f), .2f, new Color(.3f, .25f, .17f), furniture);
            furniture.rotation = Quaternion.Euler(0, yaw, 0);
        }

        static void ChessTrophyCabinet(Vector3 p, float yaw, Material wood, Material green, Transform parent)
        {
            var cabinet = new GameObject("ChessTrophyDisplay").transform; cabinet.SetParent(parent); cabinet.position = p;
            CityBox("TrophyDisplayBack", p + Vector3.up * 1.4f, new Vector3(5.4f, 2.8f, .16f), green, true, true, cabinet);
            foreach (float x in new[] { -2.73f, 2.73f }) CityBox("TrophyDisplaySide", p + new Vector3(x, 1.4f, -.3f), new Vector3(.13f, 2.8f, .75f), wood, true, true, cabinet);
            foreach (float y in new[] { .3f, 1.6f, 2.82f })
                CityBox("TrophyDisplayShelf", p + new Vector3(0, y, -.3f), new Vector3(5.5f, .1f, .75f), wood, true, true, cabinet);
            for (int i = 0; i < 3; i++)
            {
                ChessTrophy(p + new Vector3(-1.7f + i * 1.7f, .36f, -.3f), .75f + i * .07f, cabinet);
                ChessTrophy(p + new Vector3(-1.7f + i * 1.7f, 1.66f, -.3f), .65f + (2 - i) * .06f, cabinet);
            }
            CityText("TrophyDisplayTitle", "G‘ALABALAR ZALI", p + new Vector3(0, 3.1f, -.08f), .22f, new Color(.3f, .25f, .17f), cabinet);
            cabinet.rotation = Quaternion.Euler(0, yaw, 0);
        }

        static void ChessTrophy(Vector3 p, float scale, Transform parent)
        {
            var trophy = new GameObject("ChessTrophyCup").transform; trophy.SetParent(parent); trophy.position = p;
            CityBox("TrophyMarbleBase", p + new Vector3(0, .075f, 0), new Vector3(.42f, .15f, .42f), cityMetal, false, true, trophy);
            SportsCylinder("TrophyStem", p + Vector3.up * .17f, p + Vector3.up * .49f, .05f, cityGold, trophy);
            var cup = SportsLathe("ChessTrophyBowl", new[] { new Vector2(.07f,.44f), new Vector2(.13f,.5f), new Vector2(.24f,.66f), new Vector2(.29f,.87f), new Vector2(.28f,.9f), new Vector2(.25f,.87f), new Vector2(.21f,.66f), new Vector2(.09f,.5f) }, 32);
            var bowl = new GameObject("TrophyCupBowl", typeof(MeshFilter), typeof(MeshRenderer)); bowl.transform.SetParent(trophy, false);
            bowl.GetComponent<MeshFilter>().sharedMesh = cup; bowl.GetComponent<Renderer>().sharedMaterial = cityGold; bowl.isStatic = true;
            foreach (float side in new[] { -1f, 1f })
            {
                SportsCylinder("TrophyHandle", p + new Vector3(side * .25f, .83f, 0), p + new Vector3(side * .38f, .76f, 0), .025f, cityGold, trophy);
                SportsCylinder("TrophyHandle", p + new Vector3(side * .38f, .76f, 0), p + new Vector3(side * .31f, .55f, 0), .025f, cityGold, trophy);
                SportsCylinder("TrophyHandle", p + new Vector3(side * .31f, .55f, 0), p + new Vector3(side * .17f, .55f, 0), .025f, cityGold, trophy);
            }
            trophy.localScale = Vector3.one * scale;
        }

        static Mesh SportsLathe(string name, Vector2[] profile, int segments)
        {
            if (CityMeshes.TryGetValue(name, out var mesh)) return mesh;
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
            for (int row = 0; row < profile.Length; row++)
            for (int column = 0; column <= segments; column++)
            {
                float a = column * Mathf.PI * 2 / segments;
                vertices.Add(new Vector3(Mathf.Cos(a) * profile[row].x, profile[row].y, Mathf.Sin(a) * profile[row].x));
                uv.Add(new Vector2((float)column / segments, (float)row / (profile.Length - 1)));
                if (row == 0 || column == segments) continue;
                int n = row * (segments + 1) + column;
                triangles.AddRange(new[] { n, n + 1, n - segments - 1, n + 1, n - segments, n - segments - 1 });
            }
            mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh = SaveWorldGeometry(mesh, name); CityMeshes.Add(name, mesh); return mesh;
        }

        static GameObject SportsCylinder(string name, Vector3 a, Vector3 b, float radius, Material material, Transform parent, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = name; go.transform.SetParent(parent);
            go.transform.position = (a + b) * .5f; go.transform.rotation = Quaternion.FromToRotation(Vector3.up, b - a);
            go.transform.localScale = new Vector3(radius * 2, Vector3.Distance(a, b) * .5f, radius * 2);
            go.GetComponent<Renderer>().sharedMaterial = material; go.isStatic = true;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
    }
}

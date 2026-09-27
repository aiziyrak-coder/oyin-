using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CraDev.EditorTools
{
    static partial class CraDevSceneBuilder
    {
        const int SceneryResolution = 128;
        const float SceneryExtent = 220f;
        const string SceneryFolder = "Assets/CraDev/World/Materials/";

        // Sinov yo'llariga tegmaydi: markazning atrofigagina tabiiy relyef va mayda o't qo'shiladi.
        static void BuildWorldScenery(Material grass, Material rock, Material soil)
        {
            var root = new GameObject("NaturalOutskirts").transform;
            var heights = new float[SceneryResolution + 1, SceneryResolution + 1];
            var vertices = new Vector3[(SceneryResolution + 1) * (SceneryResolution + 1)];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[SceneryResolution * SceneryResolution * 6];
            int triangle = 0;
            for (int z = 0; z <= SceneryResolution; z++)
            for (int x = 0; x <= SceneryResolution; x++)
            {
                float px = -SceneryExtent + 2 * SceneryExtent * x / SceneryResolution;
                float pz = -SceneryExtent + 2 * SceneryExtent * z / SceneryResolution;
                int index = z * (SceneryResolution + 1) + x;
                heights[x, z] = SceneryHeight(px, pz);
                vertices[index] = new Vector3(px, heights[x, z], pz);
                uvs[index] = new Vector2(px, pz);
                if (x == SceneryResolution || z == SceneryResolution) continue;
                int next = index + SceneryResolution + 1;
                triangles[triangle++] = index;
                triangles[triangle++] = next;
                triangles[triangle++] = index + 1;
                triangles[triangle++] = index + 1;
                triangles[triangle++] = next;
                triangles[triangle++] = next + 1;
            }
            var terrain = new Mesh { name = "OutskirtsTerrain" };
            terrain.vertices = vertices;
            terrain.uv = uvs;
            terrain.triangles = triangles;
            terrain.RecalculateNormals();
            terrain.RecalculateTangents();
            terrain.RecalculateBounds();
            SceneryObject("RollingHills", SaveSceneryMesh(terrain), grass, root, true);

            var positions = new[]
            {
                new Vector2(-34,31), new Vector2(-38,33), new Vector2(-35,36),
                new Vector2(34,48), new Vector2(37,52), new Vector2(40,49),
                new Vector2(-53,70), new Vector2(-59,68), new Vector2(-55,78),
                new Vector2(65,91), new Vector2(69,94), new Vector2(73,89),
                new Vector2(34,105), new Vector2(38,108), new Vector2(-75,118)
            };
            var random = new System.Random(90731);
            var scannedRock = LoadScannedSceneryRock();
            var variants = scannedRock != null ? new[] { scannedRock } : new Mesh[4];
            if (scannedRock == null)
                for (int i = 0; i < variants.Length; i++) variants[i] = SceneryRock(i);
            var instances = new CombineInstance[positions.Length];
            var sizes = new float[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                Vector2 position = positions[i];
                float size = i % 3 == 0 ? Range(random, 1.8f, 2.7f) : Range(random, .65f, 1.45f);
                sizes[i] = size;
                float floor = ScenerySurface(heights, position.x, position.y);
                // Nishabda markazga qo'yilgan toshning pastki cheti havoda osilib qolmasin.
                foreach (Vector2 edge in new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down })
                    floor = Mathf.Min(floor, ScenerySurface(heights, position.x + edge.x * size * .55f, position.y + edge.y * size * .55f));
                var point = new Vector3(position.x, floor - .11f, position.y);
                var rotation = Quaternion.Euler(Range(random, -7, 7), Range(random, 0, 360), Range(random, -6, 6));
                var scale = new Vector3(size, size * Range(random, .95f, 1.35f), size * Range(random, .72f, 1.1f));
                instances[i] = new CombineInstance { mesh = variants[i % variants.Length], transform = Matrix4x4.TRS(point, rotation, scale) };
            }
            var combined = new Mesh { name = "OutskirtsRocks", indexFormat = IndexFormat.UInt32 };
            combined.CombineMeshes(instances, true, true);
            combined.RecalculateTangents();
            combined.RecalculateBounds();
            SceneryObject("NaturalRocks", SaveSceneryMesh(combined), rock, root, true);

            if (soil != null) BuildScenerySoil(heights, positions, sizes, soil, root);
            BuildSceneryGrass(heights, positions, sizes, root);
            Debug.Log("[CraDev] Tabiiy chekka: 129x129 relyef, 15 tosh, 52000 tagacha ingichka o't poyasi, 6 renderer; markaziy fizika maydoni saqlandi.");
        }

        static float SceneryHeight(float x, float z)
        {
            float dx = Mathf.Max(Mathf.Abs(x) - 25f, 0);
            float dz = Mathf.Max(Mathf.Max(-20f - z, z - 36f), 0);
            float distance = Mathf.Sqrt(dx * dx + dz * dz);
            float apron = Mathf.SmoothStep(0, 1, Mathf.Clamp01((distance - 8f) / 47f));
            float broad = Mathf.PerlinNoise(x * .0072f + 81.2f, z * .0072f + 31.8f);
            float medium = Mathf.PerlinNoise(x * .021f + 132.4f, z * .021f + 64.7f);
            float fine = Mathf.PerlinNoise(x * .063f + 16.3f, z * .063f + 93.1f);
            float height = Mathf.Max(.25f, (broad - .24f) * 25f + (medium - .5f) * 6f + fine * .65f);
            // Ikki keng, asimmetrik balandlik ufqni bir xil halqa shaklidan chiqaradi.
            height += 7 * Mathf.Exp(-((x - 112) * (x - 112) / 8000f + (z - 104) * (z - 104) / 5900f));
            height += 4 * Mathf.Exp(-((x + 124) * (x + 124) / 6700f + (z - 61) * (z - 61) / 10200f));
            float edge = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) - 192f) / 28f));
            // Tekis qism asl yer ostida: bir-biriga mos keluvchi ikki pol miltillamaydi.
            return height * apron * edge - .045f;
        }

        // Dekor oyoqlari formula bilan emas, chizilgan uchburchaklarning aynan ustiga joylashadi.
        static float ScenerySurface(float[,] heights, float x, float z)
        {
            float gx = Mathf.Clamp((x + SceneryExtent) * SceneryResolution / (2 * SceneryExtent), 0, SceneryResolution - .001f);
            float gz = Mathf.Clamp((z + SceneryExtent) * SceneryResolution / (2 * SceneryExtent), 0, SceneryResolution - .001f);
            int ix = Mathf.FloorToInt(gx), iz = Mathf.FloorToInt(gz);
            float u = gx - ix, v = gz - iz;
            float a = heights[ix, iz], b = heights[ix + 1, iz], c = heights[ix, iz + 1], d = heights[ix + 1, iz + 1];
            float surface = u + v <= 1 ? a + (b - a) * u + (c - a) * v : d + (c - d) * (1 - u) + (b - d) * (1 - v);
            return Mathf.Max(0, surface);
        }

        static Mesh SceneryRock(int variant)
        {
            const int rings = 14, sides = 28;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            vertices.Add(SceneryRockPoint(Vector3.up, variant));
            uvs.Add(new Vector2(.5f, 1));
            for (int ring = 1; ring < rings; ring++)
            {
                float latitude = Mathf.PI * ring / rings;
                for (int side = 0; side <= sides; side++)
                {
                    float longitude = 2 * Mathf.PI * side / sides;
                    var unit = new Vector3(Mathf.Sin(latitude) * Mathf.Cos(longitude), Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(longitude));
                    vertices.Add(SceneryRockPoint(unit, variant));
                    uvs.Add(new Vector2((float)side / sides, 1 - (float)ring / rings));
                }
            }
            int bottom = vertices.Count;
            vertices.Add(Vector3.zero);
            uvs.Add(new Vector2(.5f, 0));
            for (int side = 0; side < sides; side++)
            {
                triangles.Add(0); triangles.Add(side + 2); triangles.Add(side + 1);
                int lower = 1 + (rings - 2) * (sides + 1) + side;
                triangles.Add(bottom); triangles.Add(lower); triangles.Add(lower + 1);
            }
            for (int ring = 0; ring < rings - 2; ring++)
            for (int side = 0; side < sides; side++)
            {
                int a = 1 + ring * (sides + 1) + side, b = a + sides + 1;
                triangles.Add(a); triangles.Add(a + 1); triangles.Add(b);
                triangles.Add(a + 1); triangles.Add(b + 1); triangles.Add(b);
            }
            var mesh = new Mesh { name = "OutskirtsRockVariant" + variant };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            var normals = mesh.normals;
            for (int ring = 0; ring < rings - 1; ring++)
            {
                int first = 1 + ring * (sides + 1), last = first + sides;
                normals[first] = normals[last] = (normals[first] + normals[last]).normalized;
            }
            mesh.normals = normals;
            mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return SaveSceneryMesh(mesh);
        }

        static Mesh LoadScannedSceneryRock()
        {
            const string modelPath = "Assets/CraDev/World/Art/ScannedRock/Boulder.fbx";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab == null) return null;
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            }
            var instance = Object.Instantiate(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            try
            {
                var parts = new List<CombineInstance>();
                foreach (var filter in instance.GetComponentsInChildren<MeshFilter>(true))
                {
                    var source = filter.sharedMesh;
                    if (source == null || source.vertexCount == 0) continue;
                    // FBX to'rtta bir joydagi LOD olib keladi: faqat 16.5k LOD2 olinadi, ular ustma-ust qo'shilmaydi.
                    if (filter.name.IndexOf("LOD2", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                        source.name.IndexOf("LOD2", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    for (int submesh = 0; submesh < source.subMeshCount; submesh++)
                        parts.Add(new CombineInstance { mesh = source, subMeshIndex = submesh, transform = filter.transform.localToWorldMatrix });
                }
                if (parts.Count == 0) throw new System.InvalidOperationException("[CraDev] Boulder.fbx ichida talab qilingan LOD2 mesh topilmadi.");
                var mesh = new Mesh { name = "OutskirtsScannedBoulder", indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(parts.ToArray(), true, true);
                mesh.RecalculateBounds();
                var bounds = mesh.bounds;
                float width = Mathf.Max(bounds.size.x, bounds.size.z);
                if (width < .0001f || mesh.uv.Length != mesh.vertexCount)
                {
                    Object.DestroyImmediate(mesh);
                    Debug.LogWarning("[CraDev] Boulder UV yoki o'lchami yaroqsiz; procedural zaxira ishlatiladi.");
                    return null;
                }
                var vertices = mesh.vertices;
                var pivot = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                for (int i = 0; i < vertices.Length; i++) vertices[i] = (vertices[i] - pivot) * (2 / width);
                mesh.vertices = vertices;
                // Suratga mos original normal/UVlar saqlanadi; faqat tangent va chegaralar yangilanadi.
                if (mesh.normals.Length != vertices.Length) mesh.RecalculateNormals();
                mesh.RecalculateTangents(); mesh.RecalculateBounds();
                long triangles = (long)mesh.GetIndexCount(0) / 3;
                Debug.Log($"[CraDev] Scanned Boulder LOD2 only: {vertices.Length} vertices, {triangles} triangles, width=2m, imported UV preserved.");
                if (triangles > 40000) Debug.LogWarning("[CraDev] Scanned Boulder 40k uchburchakdan katta; uzoq nusxalar uchun LOD tavsiya qilinadi.");
                return SaveSceneryMesh(mesh);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        static Vector3 SceneryRockPoint(Vector3 unit, int variant)
        {
            // Yumaloq shar emas: yemirilgan qirralar va katta sinish tekisliklari tosh siluetini beradi.
            float broad = Mathf.PerlinNoise(unit.x * 1.7f + 13.8f + variant * 7, unit.z * 1.7f + unit.y * .8f + 43.1f);
            float rough = Mathf.PerlinNoise(unit.x * 5.3f + unit.y * 2 + 71 + variant, unit.z * 5.3f + 21);
            float radius = .73f + broad * .4f + rough * .075f;
            float x = Mathf.Sign(unit.x) * Mathf.Pow(Mathf.Abs(unit.x), .78f);
            float z = Mathf.Sign(unit.z) * Mathf.Pow(Mathf.Abs(unit.z), .83f);
            float y = Mathf.Sign(unit.y) * Mathf.Pow(Mathf.Abs(unit.y), .82f);
            var point = new Vector3(x * radius, (y + 1) * .66f * radius, z * radius);
            point.x += unit.y * .12f;
            float roof = 1.13f + (variant % 2 == 0 ? .17f : -.13f) * point.x + .11f * point.z;
            point.y = Mathf.Min(point.y, roof + rough * .045f);
            if (point.y < .075f) point.y = .018f;
            return point;
        }

        static void BuildScenerySoil(float[,] heights, Vector2[] positions, float[] sizes, Material soil, Transform parent)
        {
            const int sides = 28, rings = 3;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int patch = 0; patch < positions.Length; patch++)
            {
                Vector2 center = positions[patch];
                int start = vertices.Count;
                AddSoilPoint(center, heights, vertices, uvs);
                for (int ring = 1; ring <= rings; ring++)
                for (int side = 0; side <= sides; side++)
                {
                    float angle = 2 * Mathf.PI * side / sides;
                    float radius = sizes[patch] * 1.3f * ring / rings * (1 + Mathf.Sin(angle * 3 + patch) * .11f + Mathf.Cos(angle * 5 - patch) * .06f);
                    AddSoilPoint(center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * .84f), heights, vertices, uvs);
                }
                for (int side = 0; side < sides; side++)
                {
                    triangles.Add(start); triangles.Add(start + side + 2); triangles.Add(start + side + 1);
                }
                for (int ring = 0; ring < rings - 1; ring++)
                for (int side = 0; side < sides; side++)
                {
                    int a = start + 1 + ring * (sides + 1) + side, b = a + sides + 1;
                    triangles.Add(a); triangles.Add(a + 1); triangles.Add(b);
                    triangles.Add(a + 1); triangles.Add(b + 1); triangles.Add(b);
                }
            }
            var mesh = new Mesh { name = "OutskirtsSoil" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            SceneryObject("RockSoilPatches", SaveSceneryMesh(mesh), soil, parent, false);
        }

        static void AddSoilPoint(Vector2 point, float[,] heights, List<Vector3> vertices, List<Vector2> uvs)
        {
            vertices.Add(new Vector3(point.x, ScenerySurface(heights, point.x, point.y) + .009f, point.y));
            uvs.Add(point);
        }

        static void BuildSceneryGrass(float[,] heights, Vector2[] rocks, float[] sizes, Transform parent)
        {
            var vertices = new List<Vector3>[] { new List<Vector3>(), new List<Vector3>(), new List<Vector3>() };
            var uvs = new List<Vector2>[] { new List<Vector2>(), new List<Vector2>(), new List<Vector2>() };
            var vertexColors = new List<Color>[] { new List<Color>(), new List<Color>(), new List<Color>() };
            var triangles = new List<int>[] { new List<int>(), new List<int>(), new List<int>() };
            var random = new System.Random(53931);
            int bladeCount = 0;
            for (int cluster = 0; cluster < 650; cluster++)
            {
                float sign = cluster % 2 == 0 ? -1 : 1;
                float x, z;
                if (cluster % 10 < 5)
                {
                    // Oldingi chetda ham mayda poyalar ko'rinadi; himoyalangan z<=36 hududga kirmaydi.
                    x = Range(random, -24, 24);
                    z = 37.3f + Mathf.Pow((float)random.NextDouble(), 1.5f) * 21;
                }
                else if (cluster % 10 < 8)
                {
                    x = sign * Range(random, 26.3f, 40);
                    z = Range(random, -18, 42);
                }
                else
                {
                    x = sign * (27 + Mathf.Pow((float)random.NextDouble(), 1.7f) * 51);
                    z = Range(random, -28, 130);
                }
                bool occupied = false;
                for (int rock = 0; rock < rocks.Length; rock++)
                    if (Vector2.Distance(new Vector2(x, z), rocks[rock]) < sizes[rock] * 1.15f + .3f) { occupied = true; break; }
                if (occupied) continue;
                float patchRadius = Range(random, .35f, 1.05f);
                float patchShape = Range(random, .6f, 1);
                for (int blade = 0; blade < 80; blade++)
                {
                    float spreadAngle = Range(random, 0, Mathf.PI * 2);
                    float spread = Mathf.Pow((float)random.NextDouble(), .7f) * patchRadius * (.84f + .16f * Mathf.Sin(spreadAngle * 3 + cluster));
                    var point = new Vector3(x + Mathf.Cos(spreadAngle) * spread, 0, z + Mathf.Sin(spreadAngle) * spread * patchShape);
                    if (Mathf.Abs(point.x) < 25 && point.z > -20 && point.z < 36) continue;
                    point.y = ScenerySurface(heights, point.x, point.z) + .003f;
                    float angle = Range(random, 0, Mathf.PI * 2);
                    var right = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    float height = Range(random, .09f, .24f), width = Range(random, .003f, .007f) * .5f;
                    var lean = new Vector3(-right.z, 0, right.x) * Range(random, -.035f, .045f);
                    var middle = point + Vector3.up * height * .55f + lean * .2f;
                    var tip = point + Vector3.up * height + lean;
                    int shadeChoice = random.Next(10);
                    int shade = shadeChoice < 6 ? 0 : shadeChoice < 9 ? 1 : 2;
                    int start = vertices[shade].Count;
                    vertices[shade].Add(point - right * width); vertices[shade].Add(point + right * width);
                    vertices[shade].Add(middle - right * width * .57f); vertices[shade].Add(middle + right * width * .57f); vertices[shade].Add(tip);
                    uvs[shade].Add(new Vector2(0, 0)); uvs[shade].Add(new Vector2(1, 0));
                    uvs[shade].Add(new Vector2(0, .55f)); uvs[shade].Add(new Vector2(1, .55f)); uvs[shade].Add(new Vector2(.5f, 1));
                    float variation = Range(random, .76f, 1.05f);
                    var color = new Color(variation, variation, variation, 0);
                    vertexColors[shade].Add(color); vertexColors[shade].Add(color);
                    color.a = .55f; vertexColors[shade].Add(color); vertexColors[shade].Add(color);
                    color.a = 1; vertexColors[shade].Add(color);
                    // WorldGrass ikki tarafni chizadi; takroriy orqa uchburchaklar z-fight yaratmaydi.
                    triangles[shade].Add(start); triangles[shade].Add(start + 2); triangles[shade].Add(start + 1);
                    triangles[shade].Add(start + 1); triangles[shade].Add(start + 2); triangles[shade].Add(start + 3);
                    triangles[shade].Add(start + 2); triangles[shade].Add(start + 4); triangles[shade].Add(start + 3);
                    bladeCount++;
                }
            }
            var colors = new[] { new Color(.36f, .40f, .21f), new Color(.44f, .43f, .27f), new Color(.32f, .38f, .20f) };
            var grassShader = Shader.Find("CraDev/WorldGrass") ?? Shader.Find("Standard");
            for (int shade = 0; shade < 3; shade++)
            {
                string path = SceneryFolder + "SceneryGrassBlade" + shade + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) { material = new Material(grassShader); AssetDatabase.CreateAsset(material, path); }
                else material.shader = grassShader;
                material.color = colors[shade];
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .05f);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0);
                EditorUtility.SetDirty(material);
                var mesh = new Mesh { name = "OutskirtsGrass" + shade, indexFormat = vertices[shade].Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(vertices[shade]); mesh.SetUVs(0, uvs[shade]); mesh.SetColors(vertexColors[shade]); mesh.SetTriangles(triangles[shade], 0);
                mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
                var go = SceneryObject("GrassClumps" + shade, SaveSceneryMesh(mesh), material, parent, false);
                go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            Debug.Log($"[CraDev] Tabiiy o't: {bladeCount} ingichka poya, {bladeCount * 3} uchburchak, 3 batch; 3-7mm kenglik, 9-24cm balandlik.");
        }

        static GameObject SceneryObject(string name, Mesh mesh, Material material, Transform parent, bool collider)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false); go.isStatic = true;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        static Mesh SaveSceneryMesh(Mesh mesh)
        {
            string path = SceneryFolder + mesh.name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        static float Range(System.Random random, float min, float max) => min + (max - min) * (float)random.NextDouble();
    }
}

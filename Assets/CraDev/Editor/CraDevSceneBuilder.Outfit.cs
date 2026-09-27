using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CraDev.EditorTools
{
    /// <summary>
    /// Garderob uchun "kiyim niqoblari": har bir avatarning teksturasida ustki kiyim, shim, oyoq kiyim va soch qayerda
    /// ekanini topadi. Rocketbox modellari yaxlit (kiyim alohida mesh emas), shuning uchun:
    ///  1) tana uchburchaklari qaysi suyaklarga bog'langaniga qarab qismga ajratiladi (umurtqa/qo'l - ustki kiyim,
    ///     son/boldir - shim, oyoq panjasi - oyoq kiyim, kaft - teri); UV orolida ko'pchilik qaysi qism bo'lsa, orol shu qism;
    ///  2) har bir qismning asosiy mato rangi topiladi (k-means, Lab; faqat ko'rinadigan tomonlardan - taglik hisobga
    ///     olinmaydi), niqob shu rangga yaqin teksellarda to'liq bo'ladi: chiziqlar, zamoklar, logotiplar va ichki futbolka
    ///     asl rangida qoladi, teri esa bo'yalmaydi;
    ///  3) soch: bosh teksturasida yuzdan (yuz xaritasi) tashqaridagi, quloq balandligidan yuqoridagi soch rangidagi joylar.
    /// Natija: Textures/&lt;tana&gt;_outfit.png (R - ustki kiyim, G - shim, B - oyoq kiyim), Textures/&lt;bosh&gt;_hair.png (R - soch)
    /// va Avatars/OutfitMaps.json (asosiy ranglar). Grafikasiz muhitda (CI) saqlangan natija ishlatiladi.
    /// Tekshiruv rasmlari: Logs/Outfit/.
    /// </summary>
    static partial class CraDevSceneBuilder
    {
        const int OutfitMaskSize = 1024;
        const string OutfitMapsFile = "Assets/CraDev/Avatars/OutfitMaps.json";

        enum BodyPart { Top, Bottom, Shoes, Skin, Hip, Neck, Count }

        internal class OutfitMap
        {
            public string BodyMask, HairMask;
            public Color[] Base = new Color[4]; // top, bottom, shoes, hair: asosiy rang (sRGB)
        }

        [System.Serializable]
        class OutfitMapRecord
        {
            public string id;
            public string bodyMask, hairMask;
            public Color top, bottom, shoes, hair;
        }

        [System.Serializable]
        class OutfitMapFile
        {
            public List<OutfitMapRecord> avatars = new List<OutfitMapRecord>();
        }

        static Dictionary<string, OutfitMap> BakeOutfitMaps(Dictionary<string, FaceMap> faceMaps)
        {
            if (!CanRender)
            {
                var saved = LoadOutfitMaps();
                Debug.Log($"[CraDev] Grafika yo'q: {saved.Count} ta kiyim niqobi {OutfitMapsFile} dan olindi.");
                return saved;
            }
            var result = new Dictionary<string, OutfitMap>();
            string debugFolder = Path.Combine("Logs", "Outfit");
            Directory.CreateDirectory(debugFolder);
            foreach (var info in AvatarList)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(info.ModelPath);
                if (model == null)
                    continue;
                faceMaps.TryGetValue(info.Id, out var faceMap);
                var map = BakeOutfitMap(info, model, faceMap, debugFolder);
                if (map != null)
                    result[info.Id] = map;
            }
            SaveOutfitMaps(result);
            return result;
        }

        static OutfitMap BakeOutfitMap(AvatarInfo info, GameObject model, FaceMap faceMap, string debugFolder)
        {
            var go = (GameObject)Object.Instantiate(model);
            try
            {
                var headBone = go.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Bip01 Head");
                foreach (var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var materials = skin.sharedMaterials;
                    int body = System.Array.FindIndex(materials, m => m != null && m.name.Contains("_body"));
                    if (body < 0 || !(materials[body].mainTexture is Texture2D bodyTexture))
                        continue;
                    var mesh = skin.sharedMesh;

                    // Dunyo koordinatalaridagi uchlar va normallar (model tik turgan holatda): taglik pastga qaragan bo'ladi
                    var baked = new Mesh();
                    skin.BakeMesh(baked, true);
                    var positions = baked.vertices.Select(v => skin.transform.TransformPoint(v)).ToArray();
                    var normals = baked.normals.Select(n => skin.transform.TransformDirection(n)).ToArray();
                    Object.DestroyImmediate(baked);

                    var labels = LabelBody(mesh, body, skin.bones, normals, out var visible, out var islandMap, out string stats);
                    var bodyColors = ReadPixels(bodyTexture);
                    var weights = GarmentWeights(labels, visible, islandMap, bodyColors, out var garmentBase);
                    var map = new OutfitMap { BodyMask = MaskPathFor(bodyTexture, "_outfit") };
                    System.Array.Copy(garmentBase, map.Base, 3);
                    WriteMask(map.BodyMask, weights[0], weights[1], weights[2]);
                    WriteOutfitDebug(Path.Combine(debugFolder, info.Id + "_body.png"), bodyColors, labels, weights);

                    int head = System.Array.FindIndex(materials, m => m != null && m.name.Contains("_head"));
                    map.Base[3] = new Color(0.2f, 0.15f, 0.1f);
                    if (head >= 0 && materials[head].mainTexture is Texture2D headTexture && headBone != null && faceMap != null)
                    {
                        var headColors = ReadPixels(headTexture);
                        var hair = HairWeights(mesh, head, positions, headBone.position, faceMap, headColors, out var hairBase, out var hairLabels, out float coverage);
                        map.Base[3] = hairBase;
                        WriteOutfitDebug(Path.Combine(debugFolder, info.Id + "_hair.png"), headColors, hairLabels, new[] { hair, new float[hair.Length], new float[hair.Length] });
                        // Niqob sochning kamida 70% ini qoplasagina: aks holda soch dog'-dog' bo'yalardi - bu avatarda soch rangi o'zgarmaydi
                        if (coverage >= 0.7f)
                        {
                            map.HairMask = MaskPathFor(headTexture, "_hair");
                            WriteMask(map.HairMask, hair, null, null);
                        }
                        else
                            Debug.Log($"[CraDev] {info.Id}: soch niqobi sochning {coverage:P0} qismini qoplaydi - soch rangi bu avatarda o'chiq.");
                    }
                    Debug.Log($"[CraDev] {info.Id}: kiyim niqobi tayyor ({stats}); ranglar: ustki #{ColorUtility.ToHtmlStringRGB(map.Base[0])}, " +
                              $"shim #{ColorUtility.ToHtmlStringRGB(map.Base[1])}, oyoq kiyim #{ColorUtility.ToHtmlStringRGB(map.Base[2])}, " +
                              $"soch #{ColorUtility.ToHtmlStringRGB(map.Base[3])}{(map.HairMask == null ? " (soch niqobi yo'q)" : "")}");
                    return map;
                }
                Debug.LogWarning($"[CraDev] {info.Id}: tana materiali topilmadi, garderob bu avatarda ishlamaydi.");
                return null;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        static string MaskPathFor(Texture texture, string suffix)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            return (Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path).Replace("_color", "") + suffix + ".png")).Replace('\\', '/');
        }

        static Color32[] ReadPixels(Texture texture)
        {
            var copy = ReadTexture(texture, OutfitMaskSize);
            var pixels = copy.GetPixels32();
            Object.DestroyImmediate(copy);
            return pixels;
        }

        static BodyPart PartOfBone(string bone)
        {
            if (bone.Contains("Hand") || bone.Contains("Finger")) return BodyPart.Skin;
            if (bone.Contains("Foot") || bone.Contains("Toe")) return BodyPart.Shoes;
            if (bone.Contains("Thigh") || bone.Contains("Calf")) return BodyPart.Bottom;
            if (bone.Contains("Neck") || bone.Contains("Head")) return BodyPart.Neck;
            if (bone.Contains("Spine") || bone.Contains("Clavicle") || bone.Contains("UpperArm") || bone.Contains("Forearm")) return BodyPart.Top;
            return BodyPart.Hip; // Pelvis va ildiz suyagi: kamar, son bo'g'imi - orolga qarab hal qilinadi
        }

        /// <summary>
        /// Tana teksturasining har bir tekseli qaysi qismga tegishli (-1 - hech biri). visible: tekselni pastga qaramagan
        /// (ko'rinadigan) uchburchak qoplaydimi - asosiy rang faqat shulardan olinadi.
        /// </summary>
        static int[] LabelBody(Mesh mesh, int submesh, Transform[] bones, Vector3[] normals, out int[] visible, out int[] islandMap, out string stats)
        {
            var uv = mesh.uv;
            var weights = mesh.boneWeights;
            var triangles = mesh.GetTriangles(submesh);
            var boneParts = bones.Select(b => b != null ? PartOfBone(b.name) : BodyPart.Hip).ToArray();

            // Uchburchak qismi: uchala uchining suyak og'irliklari yig'indisi bo'yicha
            int count = triangles.Length / 3;
            var part = new BodyPart[count];
            var score = new float[(int)BodyPart.Count];
            for (int t = 0; t < count; t++)
            {
                System.Array.Clear(score, 0, score.Length);
                for (int k = 0; k < 3; k++)
                {
                    var w = weights[triangles[t * 3 + k]];
                    score[(int)boneParts[w.boneIndex0]] += w.weight0;
                    score[(int)boneParts[w.boneIndex1]] += w.weight1;
                    score[(int)boneParts[w.boneIndex2]] += w.weight2;
                    score[(int)boneParts[w.boneIndex3]] += w.weight3;
                }
                int best = 0;
                for (int p = 1; p < score.Length; p++)
                    if (score[p] > score[best]) best = p;
                part[t] = (BodyPart)best;
            }

            // UV orollari (umumiy uchlar orqali bog'langan uchburchaklar)
            var parent = Enumerable.Range(0, uv.Length).ToArray();
            int Find(int v) { while (parent[v] != v) v = parent[v] = parent[parent[v]]; return v; }
            for (int t = 0; t < count; t++)
            {
                int a = Find(triangles[t * 3]), b = Find(triangles[t * 3 + 1]), c = Find(triangles[t * 3 + 2]);
                parent[b] = a;
                parent[Find(c)] = a;
            }
            var islands = new Dictionary<int, float[]>();
            for (int t = 0; t < count; t++)
            {
                int root = Find(triangles[t * 3]);
                if (!islands.TryGetValue(root, out var totals))
                    islands[root] = totals = new float[(int)BodyPart.Count];
                totals[(int)part[t]] += UvArea(uv, triangles, t);
            }

            int resolved = 0;
            for (int t = 0; t < count; t++)
            {
                var totals = islands[Find(triangles[t * 3])];
                float garments = totals[(int)BodyPart.Top] + totals[(int)BodyPart.Bottom] + totals[(int)BodyPart.Shoes];
                float all = totals.Sum();
                // Orolning 70% dan ko'prog'i bitta kiyim bo'lsa, yoqa, yeng uchi, kamar ham shu kiyimga qo'shiladi
                var dominant = new[] { BodyPart.Top, BodyPart.Bottom, BodyPart.Shoes }.OrderByDescending(p => totals[(int)p]).First();
                if (all > 0f && totals[(int)dominant] >= 0.7f * all)
                {
                    if (part[t] != dominant) resolved++;
                    part[t] = dominant;
                }
                else if (part[t] == BodyPart.Hip)
                    part[t] = totals[(int)BodyPart.Top] > totals[(int)BodyPart.Bottom] ? BodyPart.Top : BodyPart.Bottom;
                else if (part[t] == BodyPart.Neck)
                    part[t] = garments > 0f && totals[(int)BodyPart.Top] >= 0.3f * all ? BodyPart.Top : BodyPart.Skin;
            }

            // Teksturaga chizish
            var labels = Enumerable.Repeat(-1, OutfitMaskSize * OutfitMaskSize).ToArray();
            visible = new int[labels.Length];
            islandMap = Enumerable.Repeat(-1, labels.Length).ToArray();
            for (int t = 0; t < count; t++)
            {
                int i0 = triangles[t * 3], i1 = triangles[t * 3 + 1], i2 = triangles[t * 3 + 2];
                RasterizeUv(labels, uv[i0], uv[i1], uv[i2], (int)part[t]);
                RasterizeUv(islandMap, uv[i0], uv[i1], uv[i2], Find(i0));
                if ((normals[i0] + normals[i1] + normals[i2]).normalized.y > -0.5f)
                    RasterizeUv(visible, uv[i0], uv[i1], uv[i2], 1);
            }
            stats = $"{count} uchburchak, {islands.Count} orol, {resolved} tasi orol bo'yicha tuzatildi";
            return labels;
        }

        static float UvArea(Vector2[] uv, int[] triangles, int t)
        {
            Vector2 a = uv[triangles[t * 3]], b = uv[triangles[t * 3 + 1]], c = uv[triangles[t * 3 + 2]];
            return Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y)) * 0.5f;
        }

        static void RasterizeUv(int[] target, Vector2 a, Vector2 b, Vector2 c, int value)
        {
            const int n = OutfitMaskSize;
            a = Clamp01(a) * n; b = Clamp01(b) * n; c = Clamp01(c) * n;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), 0, n - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))), 0, n - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), 0, n - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))), 0, n - 1);
            float area = (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);
            if (Mathf.Abs(area) < 1e-6f)
                return;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float w0 = ((b.x - p.x) * (c.y - p.y) - (c.x - p.x) * (b.y - p.y)) / area;
                    float w1 = ((c.x - p.x) * (a.y - p.y) - (a.x - p.x) * (c.y - p.y)) / area;
                    float w2 = 1f - w0 - w1;
                    const float e = -0.02f; // chetga tegib turgan teksel ham olinadi: choklarda bo'shliq qolmasin
                    if (w0 >= e && w1 >= e && w2 >= e)
                        target[y * n + x] = value;
                }
        }

        static Vector2 Clamp01(Vector2 uv) => new Vector2(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y)); // Rocketbox UV lari 0..1 ichida

        /// <summary>
        /// Har bir kiyim uchun asosiy mato rangi (ko'rinadigan joylardagi eng katta rang guruhi) va har bir tekselning shu
        /// rangga yaqinligi (0..1). Teri rangiga yaqin teksellar bo'yalmaydi.
        /// </summary>
        static float[][] GarmentWeights(int[] labels, int[] visible, int[] islandMap, Color32[] colors, out Color[] baseColors)
        {
            int n = labels.Length;
            var lab = new Vector3[n];
            for (int i = 0; i < n; i++)
                lab[i] = ToLab(colors[i]);

            // Teri: kaftlar (va bo'yin) rangining medianasi
            var skinSamples = Enumerable.Range(0, n).Where(i => labels[i] == (int)BodyPart.Skin).Select(i => lab[i]).ToList();
            Vector3? skin = skinSamples.Count > 50 ? Median(skinSamples) : (Vector3?)null;

            baseColors = new Color[3];
            var result = new float[3][];
            for (int g = 0; g < 3; g++)
            {
                var idx = Enumerable.Range(0, n).Where(i => labels[i] == g).ToArray();
                var seen = idx.Where(i => visible[i] != 0).ToArray();
                result[g] = SimilarityMask(idx, seen.Length >= 50 ? seen : idx, lab, skin, out baseColors[g]);
                DropForeignIslands(result[g], idx, islandMap);
                result[g] = Dilate(result[g], labels, 3);
            }
            return result;
        }

        /// <summary>
        /// idx teksellari uchun niqob: namunalardagi asosiy rangga yaqinlik (yorug'lik farqi - burmalar, soya - kechiriladi),
        /// teri rangidagilar chiqarib tashlanadi, yakka-yakka "dog'lar" tozalanadi.
        /// </summary>
        static float[] SimilarityMask(int[] idx, int[] samples, Vector3[] lab, Vector3? skin, out Color baseColor)
        {
            var mask = new float[lab.Length];
            if (samples.Length < 50)
            {
                baseColor = Color.gray;
                return mask;
            }
            var center = DominantColor(samples.Select(i => lab[i]).ToList(), skin);
            baseColor = FromLab(center);
            foreach (int i in idx)
            {
                float d = LabDistance(lab[i], center, 0.3f);
                float w = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(12f, 26f, d));
                // Teri bo'yalmaydi. Mato (soch) teridan ancha farq qilsa - teri rangiga yaqin teksellar chiqariladi; rangi teriga
                // o'xshash bo'lsa (och jigarrang soch) - teksel qaysi biriga yaqinroq bo'lsa, o'shaniki hisoblanadi
                if (skin.HasValue)
                {
                    float toSkin = LabDistance(lab[i], skin.Value, 0.6f);
                    w *= LabDistance(center, skin.Value, 1f) > 18f
                        ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(8f, 16f, toSkin))
                        : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-2f, 4f, toSkin - LabDistance(lab[i], center, 0.6f)));
                }
                mask[i] = w;
            }

            // Dog'lar: atrofidagi (5x5) o'rtacha past bo'lgan yakka teksellar olib tashlanadi, katta sohalar o'zgarmaydi
            var blurred = BoxBlur(mask, 2);
            foreach (int i in idx)
                mask[i] *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.55f, blurred[i]));
            return mask;
        }

        /// <summary>
        /// Bo'lagining (UV oroli) katta qismi mato rangiga o'xshamasa, bo'lak butunlay bo'yalmaydi: masalan, boldirga
        /// bog'langan etik qo'nji yoki uning juni shim deb bo'yalmasin.
        /// </summary>
        static void DropForeignIslands(float[] mask, int[] idx, int[] islandMap)
        {
            var sum = new Dictionary<int, float>();
            var count = new Dictionary<int, int>();
            foreach (int i in idx)
            {
                int island = islandMap[i];
                sum[island] = (sum.TryGetValue(island, out var s) ? s : 0f) + mask[i];
                count[island] = (count.TryGetValue(island, out var c) ? c : 0) + 1;
            }
            foreach (int i in idx)
                if (sum[islandMap[i]] / count[islandMap[i]] < 0.35f)
                    mask[i] = 0f;
        }

        static float[] BoxBlur(float[] values, int radius)
        {
            const int n = OutfitMaskSize;
            var horizontal = new float[values.Length];
            var result = new float[values.Length];
            float inv = 1f / (radius * 2 + 1);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float s = 0f;
                    for (int k = -radius; k <= radius; k++)
                        s += values[y * n + Mathf.Clamp(x + k, 0, n - 1)];
                    horizontal[y * n + x] = s * inv;
                }
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float s = 0f;
                    for (int k = -radius; k <= radius; k++)
                        s += horizontal[Mathf.Clamp(y + k, 0, n - 1) * n + x];
                    result[y * n + x] = s * inv;
                }
            return result;
        }

        /// <summary>
        /// Soch niqobi (bosh teksturasida). Soch rangi bosh suyagidan (taxminan quloq ostidan) yuqoridagi, yuzdan
        /// (yuz xaritasi) tashqaridagi joylardan olinadi; niqob esa yuzdan tashqaridagi hamma soch rangidagi teksellarga
        /// qo'yiladi (uzun sochning yelkaga tushgan qismi ham). Tish, til, ko'z kabi mayda bo'laklar chiqarib tashlanadi.
        /// Teri rangi yuz o'rtasidan olinadi.
        /// </summary>
        static float[] HairWeights(Mesh mesh, int submesh, Vector3[] positions, Vector3 head, FaceMap faceMap, Color32[] colors,
            out Color hairBase, out int[] labels, out float coverage)
        {
            const int n = OutfitMaskSize;
            var uv = mesh.uv;
            var triangles = mesh.GetTriangles(submesh);

            // Bo'laklar (UV orollari) va ularning 3D o'lchami: 5 sm dan kichiklari (ko'z olmasi) soch emas; og'iz ichi
            // (tish, milk, til) - bosh suyagining oldida, og'iz balandligidagi 10 sm dan kichik bo'lak
            var parent = Enumerable.Range(0, uv.Length).ToArray();
            int Find(int v) { while (parent[v] != v) v = parent[v] = parent[parent[v]]; return v; }
            for (int t = 0; t < triangles.Length; t += 3)
            {
                int a = Find(triangles[t]);
                parent[Find(triangles[t + 1])] = a;
                parent[Find(triangles[t + 2])] = a;
            }
            var islandBounds = new Dictionary<int, Bounds>();
            foreach (int v in triangles)
            {
                int root = Find(v);
                if (islandBounds.TryGetValue(root, out var b))
                    b.Encapsulate(positions[v]);
                else
                    b = new Bounds(positions[v], Vector3.zero);
                islandBounds[root] = b;
            }

            labels = Enumerable.Repeat(-1, n * n).ToArray();
            var region = new int[n * n];     // 1 - soch bo'lishi mumkin, 2 - rang namunasi ham olinadi
            for (int t = 0; t < triangles.Length; t += 3)
            {
                int i0 = triangles[t], i1 = triangles[t + 1], i2 = triangles[t + 2];
                RasterizeUv(labels, uv[i0], uv[i1], uv[i2], (int)BodyPart.Skin);
                var bounds = islandBounds[Find(i0)];
                float extent = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                var local = bounds.center - head; // model +Z tomonga qaraydi
                bool mouth = extent < 0.1f && local.y > -0.03f && local.y < 0.09f && local.z > 0.02f && Mathf.Abs(local.x) < 0.05f;
                if (extent < 0.05f || mouth)
                    continue;
                bool above = Mathf.Min(positions[i0].y, Mathf.Min(positions[i1].y, positions[i2].y)) > head.y - 0.02f;
                RasterizeUv(region, uv[i0], uv[i1], uv[i2], above ? 2 : 1);
            }

            // Yuz: yuz nuqtalarining qavariq qobig'i (biroz kengaytirilgan)
            // Og'iz ichiga tushgan nuqtalar (teksturaning boshqa burchagida) chiqarib tashlanadi: ular qobiqni cho'zib yuboradi
            var points = faceMap.Uv.Where(p => p.x >= 0f).Select(p => p * n).ToList();
            var middle = new Vector2(points.Select(p => p.x).OrderBy(v => v).ElementAt(points.Count / 2), points.Select(p => p.y).OrderBy(v => v).ElementAt(points.Count / 2));
            float typical = points.Select(p => (p - middle).magnitude).OrderBy(v => v).ElementAt(points.Count / 2);
            points = points.Where(p => (p - middle).magnitude < typical * 2.2f).ToList();
            var hull = ConvexHull(points);
            var centroid = hull.Aggregate(Vector2.zero, (s, p) => s + p) / Mathf.Max(1, hull.Count);
            hull = hull.Select(p => centroid + (p - centroid) * 1.06f).ToList();

            var lab = new Vector3[n * n];
            for (int i = 0; i < lab.Length; i++)
                lab[i] = ToLab(colors[i]);
            var faceSamples = new List<Vector3>();
            var hairIdx = new List<int>();
            var sampleIdx = new List<int>();
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int i = y * n + x;
                    bool inFace = hull.Count >= 3 && InsidePolygon(hull, new Vector2(x + 0.5f, y + 0.5f));
                    if (inFace && (new Vector2(x, y) - centroid).magnitude < 0.09f * n)
                        faceSamples.Add(lab[i]);
                    if (region[i] == 0 || inFace)
                        continue;
                    hairIdx.Add(i);
                    labels[i] = (int)BodyPart.Top; // tekshiruv rasmida qizil
                    if (region[i] == 2)
                        sampleIdx.Add(i);
                }
            Vector3? skin = faceSamples.Count > 50 ? Median(faceSamples) : (Vector3?)null;
            var mask = SimilarityMask(hairIdx.ToArray(), sampleIdx.Count >= 50 ? sampleIdx.ToArray() : hairIdx.ToArray(), lab, skin, out hairBase);
            // Sifat: bosh tepasida rangi teridan ko'ra sochga yaqin teksellarning qanchasi niqobda (och, teriga o'xshash,
            // jingalak sochda past bo'ladi - niqob dog'-dog' chiqadi)
            var hairLab = ToLab((Color32)hairBase);
            var hairLike = sampleIdx.Where(i => !skin.HasValue || LabDistance(lab[i], skin.Value, 0.6f) > LabDistance(lab[i], hairLab, 0.6f)).ToList();
            coverage = hairLike.Count == 0 ? 0f : hairLike.Count(i => mask[i] > 0.5f) / (float)hairLike.Count;
            return Dilate(mask, labels, 3);
        }

        static List<Vector2> ConvexHull(List<Vector2> points)
        {
            var p = points.OrderBy(v => v.x).ThenBy(v => v.y).ToList();
            if (p.Count < 3)
                return p;
            float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
            var hull = new List<Vector2>();
            foreach (var pass in new[] { p, Enumerable.Reverse(p).ToList() })
            {
                int start = hull.Count;
                foreach (var v in pass)
                {
                    while (hull.Count >= start + 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], v) <= 0f)
                        hull.RemoveAt(hull.Count - 1);
                    hull.Add(v);
                }
                hull.RemoveAt(hull.Count - 1);
            }
            return hull;
        }

        static bool InsidePolygon(List<Vector2> polygon, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
                if ((polygon[i].y > p.y) != (polygon[j].y > p.y) &&
                    p.x < (polygon[j].x - polygon[i].x) * (p.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                    inside = !inside;
            return inside;
        }

        static Vector3 DominantColor(List<Vector3> samples, Vector3? skin)
        {
            // Deterministik k-means (k = 3), boshlang'ich markazlar - yorug'lik bo'yicha kvantillar
            if (samples.Count > 40000)
                samples = samples.Where((_, i) => i % (samples.Count / 40000 + 1) == 0).ToList();
            var sorted = samples.OrderBy(s => s.x).ToList();
            var centers = new[] { sorted[sorted.Count / 6], sorted[sorted.Count / 2], sorted[sorted.Count * 5 / 6] };
            var assign = new int[samples.Count];
            for (int iteration = 0; iteration < 12; iteration++)
            {
                var sum = new Vector3[3];
                var cnt = new int[3];
                for (int i = 0; i < samples.Count; i++)
                {
                    int best = 0;
                    float bestD = float.MaxValue;
                    for (int k = 0; k < 3; k++)
                    {
                        float d = LabDistance(samples[i], centers[k], 0.5f);
                        if (d < bestD) { bestD = d; best = k; }
                    }
                    assign[i] = best;
                    sum[best] += samples[i];
                    cnt[best]++;
                }
                for (int k = 0; k < 3; k++)
                    if (cnt[k] > 0) centers[k] = sum[k] / cnt[k];
            }
            var sizes = new int[3];
            foreach (int a in assign) sizes[a]++;
            // Eng katta guruh; teri rangidagi guruh (masalan, kalta yengdan chiqqan qo'l) hisobga olinmaydi
            return Enumerable.Range(0, 3)
                .Where(k => sizes[k] > 0 && (!skin.HasValue || LabDistance(centers[k], skin.Value, 1f) > 12f))
                .OrderByDescending(k => sizes[k])
                .Select(k => centers[k])
                .DefaultIfEmpty(centers[System.Array.IndexOf(sizes, sizes.Max())])
                .First();
        }

        static float[] Dilate(float[] values, int[] labels, int steps)
        {
            const int n = OutfitMaskSize;
            var current = (float[])values.Clone();
            for (int s = 0; s < steps; s++)
            {
                var next = (float[])current.Clone();
                for (int y = 1; y < n - 1; y++)
                    for (int x = 1; x < n - 1; x++)
                    {
                        int i = y * n + x;
                        if (labels[i] >= 0)
                            continue;
                        float m = Mathf.Max(Mathf.Max(current[i - 1], current[i + 1]), Mathf.Max(current[i - n], current[i + n]));
                        if (m > next[i]) next[i] = m;
                    }
                current = next;
            }
            return current;
        }

        static Vector3 Median(List<Vector3> samples)
        {
            float M(System.Func<Vector3, float> f) { var s = samples.Select(f).OrderBy(v => v).ToList(); return s[s.Count / 2]; }
            return new Vector3(M(v => v.x), M(v => v.y), M(v => v.z));
        }

        static float LabDistance(Vector3 a, Vector3 b, float lightnessWeight)
        {
            float dl = (a.x - b.x) * lightnessWeight;
            return Mathf.Sqrt(dl * dl + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z));
        }

        static Vector3 ToLab(Color32 c)
        {
            float Lin(byte v) { float f = v / 255f; return f <= 0.04045f ? f / 12.92f : Mathf.Pow((f + 0.055f) / 1.055f, 2.4f); }
            float r = Lin(c.r), g = Lin(c.g), b = Lin(c.b);
            float x = (0.4124f * r + 0.3576f * g + 0.1805f * b) / 0.95047f;
            float y = 0.2126f * r + 0.7152f * g + 0.0722f * b;
            float z = (0.0193f * r + 0.1192f * g + 0.9505f * b) / 1.08883f;
            float F(float t) => t > 0.008856f ? Mathf.Pow(t, 1f / 3f) : 7.787f * t + 16f / 116f;
            return new Vector3(116f * F(y) - 16f, 500f * (F(x) - F(y)), 200f * (F(y) - F(z)));
        }

        static Color FromLab(Vector3 lab)
        {
            float fy = (lab.x + 16f) / 116f, fx = fy + lab.y / 500f, fz = fy - lab.z / 200f;
            float Inv(float t) => t * t * t > 0.008856f ? t * t * t : (t - 16f / 116f) / 7.787f;
            float x = Inv(fx) * 0.95047f, y = Inv(fy), z = Inv(fz) * 1.08883f;
            float r = 3.2406f * x - 1.5372f * y - 0.4986f * z;
            float g = -0.9689f * x + 1.8758f * y + 0.0415f * z;
            float b = 0.0557f * x - 0.2040f * y + 1.0570f * z;
            float S(float v) { v = Mathf.Clamp01(v); return v <= 0.0031308f ? 12.92f * v : 1.055f * Mathf.Pow(v, 1f / 2.4f) - 0.055f; }
            return new Color(S(r), S(g), S(b));
        }

        static void WriteMask(string path, float[] r, float[] g, float[] b)
        {
            const int n = OutfitMaskSize;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false, true);
            var pixels = new Color32[n * n];
            byte B(float[] v, int i) => v == null ? (byte)0 : (byte)(Mathf.Clamp01(v[i]) * 255f);
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(B(r, i), B(g, i), B(b, i), 255);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        static void WriteOutfitDebug(string path, Color32[] colors, int[] labels, float[][] weights)
        {
            const int n = OutfitMaskSize;
            var tints = new[] { new Color(1f, 0.2f, 0.2f), new Color(0.2f, 0.4f, 1f), new Color(0.2f, 1f, 0.3f), new Color(1f, 0.9f, 0.2f) };
            var texture = new Texture2D(n * 2, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int i = y * n + x;
                    Color c = colors[i];
                    texture.SetPixel(x, y, labels[i] >= 0 && labels[i] < tints.Length ? Color.Lerp(c, tints[labels[i]], 0.45f) : c * 0.35f);
                    var m = new Color(weights[0][i], weights[1][i], weights[2][i]);
                    texture.SetPixel(n + x, y, Color.Lerp(c * 0.3f, m, Mathf.Max(m.r, Mathf.Max(m.g, m.b))));
                }
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        static void SaveOutfitMaps(Dictionary<string, OutfitMap> maps)
        {
            var file = new OutfitMapFile();
            foreach (var pair in maps.OrderBy(p => p.Key))
                file.avatars.Add(new OutfitMapRecord
                {
                    id = pair.Key, bodyMask = pair.Value.BodyMask, hairMask = pair.Value.HairMask ?? "",
                    top = pair.Value.Base[0], bottom = pair.Value.Base[1], shoes = pair.Value.Base[2], hair = pair.Value.Base[3],
                });
            File.WriteAllText(OutfitMapsFile, JsonUtility.ToJson(file, true));
            AssetDatabase.ImportAsset(OutfitMapsFile);
        }

        static Dictionary<string, OutfitMap> LoadOutfitMaps()
        {
            var result = new Dictionary<string, OutfitMap>();
            if (!File.Exists(OutfitMapsFile))
                return result;
            foreach (var r in JsonUtility.FromJson<OutfitMapFile>(File.ReadAllText(OutfitMapsFile)).avatars)
                result[r.id] = new OutfitMap
                {
                    BodyMask = r.bodyMask, HairMask = string.IsNullOrEmpty(r.hairMask) ? null : r.hairMask,
                    Base = new[] { r.top, r.bottom, r.shoes, r.hair },
                };
            return result;
        }

        const string OutfitPaintPath = "Assets/CraDev/Wardrobe/OutfitRecolor.mat";
        const string GlossVariantPath = "Assets/CraDev/Wardrobe/GlossVariant.mat";

        /// <summary>Kiyimni bo'yovchi material (Hidden/CraDev/OutfitRecolor).</summary>
        static Material OutfitPaintMaterial()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/CraDev/Wardrobe/Shaders/OutfitRecolor.shader");
            var material = AssetDatabase.LoadAssetAtPath<Material>(OutfitPaintPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, OutfitPaintPath);
            }
            material.shader = shader;
            return material;
        }

        /// <summary>
        /// Standard shaderning "normal + silliqlik xaritasi" varianti o'yinga kirishi uchun material: o'yinda bu kalit so'z
        /// garderob tomonidan yoqiladi, lekin build faqat materiallarda ishlatilgan variantlarni qo'shadi.
        /// </summary>
        static Material GlossVariantMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(GlossVariantPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, GlossVariantPath);
            }
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICGLOSSMAP");
            material.SetTexture("_BumpMap", Texture2D.normalTexture);
            material.SetTexture("_MetallicGlossMap", Texture2D.blackTexture);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Faqat kiyim niqoblarini hisoblash (sahnalarsiz) va tekshiruv rasmlari: CraDevBatch.BakeOutfits.</summary>
        internal static void BakeOutfitsOnly()
        {
            BakeOutfitMaps(LoadFaceMaps());
            RenderOutfitTests();
        }

        /// <summary>
        /// Har bir avatarni bir nechta kiyimda chizadi (asl, bir nechta tayyor obraz, soch rangi) - Logs/Outfit/test_&lt;ID&gt;.png.
        /// Garderob bo'yashini o'yinsiz tekshirish uchun.
        /// </summary>
        static void RenderOutfitTests()
        {
            if (!CanRender)
                return;
            var maps = LoadOutfitMaps();
            EditorSceneManagerNewEmpty();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.52f, 0.56f);
            RenderSettings.ambientEquatorColor = new Color(0.3f, 0.3f, 0.32f);
            RenderSettings.ambientGroundColor = new Color(0.1f, 0.1f, 0.11f);
            NewLight("Key", null, LightType.Directional, new Color(1f, 0.96f, 0.9f), 1.25f, Quaternion.Euler(28f, 30f, 0f));
            NewLight("Rim", null, LightType.Directional, new Color(0.72f, 0.8f, 1f), 0.7f, Quaternion.Euler(18f, 200f, 0f));
            var camera = new GameObject("TestCamera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.17f, 0.19f);
            camera.fieldOfView = 18f;
            const int w = 300, h = 560;
            camera.aspect = (float)w / h;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            camera.targetTexture = rt;
            var paint = OutfitPaintMaterial();

            var looks = new List<Wardrobe.Outfit> { new Wardrobe.Outfit() };
            foreach (var id in new[] { "city", "autumn", "military", "formal" })
                looks.Add(Wardrobe.WardrobeCatalog.Looks.First(l => l.Id == id).Outfit.Clone());
            looks[1].Set(Wardrobe.OutfitSlot.Hair, "hair_blond", "C9A46A");
            looks[2].Set(Wardrobe.OutfitSlot.Hair, "hair_copper", "A4502A");
            looks[3].Set(Wardrobe.OutfitSlot.Hair, "hair_blue", "2E4FA8");

            foreach (var info in AvatarList)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(info.ModelPath);
                if (model == null || !maps.TryGetValue(info.Id, out var map))
                    continue;
                var option = new CharacterCreation.AvatarOption
                {
                    id = info.Id,
                    outfitMask = AssetDatabase.LoadAssetAtPath<Texture2D>(map.BodyMask),
                    hairMask = map.HairMask != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(map.HairMask) : null,
                    outfitBase = map.Base,
                };
                var sheet = new Texture2D(w * looks.Count, h, TextureFormat.RGBA32, false);
                for (int k = 0; k < looks.Count; k++)
                {
                    var go = (GameObject)Object.Instantiate(model);
                    go.transform.rotation = Quaternion.Euler(0f, ModelFacing + 20f, 0f);
                    var targets = new List<RenderTexture>();
                    foreach (var renderer in go.GetComponentsInChildren<Renderer>())
                    {
                        var materials = renderer.sharedMaterials.Select(m => m != null ? new Material(m) : null).ToArray();
                        foreach (var material in materials)
                        {
                            if (material == null || material.mainTexture == null)
                                continue;
                            RenderTexture a = null, b = null;
                            if (material.name.Contains("_body") && Wardrobe.OutfitPainter.PaintBody(material.mainTexture, option, looks[k], paint, ref a, ref b))
                            {
                                material.mainTexture = a;
                                material.SetTexture("_MetallicGlossMap", b);
                                material.EnableKeyword("_METALLICGLOSSMAP");
                            }
                            else if (material.name.Contains("_head") && Wardrobe.OutfitPainter.PaintHair(material.mainTexture, option, looks[k], paint, false, ref a))
                                material.mainTexture = a;
                            else if (material.name.Contains("_opacity") && Wardrobe.OutfitPainter.PaintHair(material.mainTexture, option, looks[k], paint, true, ref a))
                                material.mainTexture = a;
                            if (a != null) targets.Add(a);
                            if (b != null) targets.Add(b);
                        }
                        renderer.sharedMaterials = materials;
                    }
                    var bounds = new Bounds(go.transform.position, Vector3.zero);
                    foreach (var r in go.GetComponentsInChildren<Renderer>())
                        bounds.Encapsulate(r.bounds);
                    float distance = bounds.size.y * 0.55f / Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                    camera.transform.SetPositionAndRotation(new Vector3(bounds.center.x, bounds.center.y, bounds.center.z - distance), Quaternion.identity);
                    camera.Render();
                    var shot = ReadRenderTexture(rt);
                    sheet.SetPixels(k * w, 0, w, h, shot.GetPixels());
                    Object.DestroyImmediate(shot);
                    Object.DestroyImmediate(go);
                    foreach (var t in targets)
                        t.Release();
                }
                File.WriteAllBytes(Path.Combine("Logs", "Outfit", "test_" + info.Id + ".png"), sheet.EncodeToPNG());
                Object.DestroyImmediate(sheet);
            }
            camera.targetTexture = null;
            rt.Release();
        }

        static void EditorSceneManagerNewEmpty() =>
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
    }
}

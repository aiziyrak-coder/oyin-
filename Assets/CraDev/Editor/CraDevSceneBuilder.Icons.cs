using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static CraDev.EditorTools.UiBuild;

namespace CraDev.EditorTools
{
    /// <summary>
    /// Lobby ikonkalari: ingichka chiziqli, oq (rangi Image.color bilan). Chizmalar Editor/LobbyIcons.json da oddiy
    /// buyruqlar bilan yozilgan (24x24 to'r, y pastga): L - chiziq, Z - yopiq chiziq, A - yoy, C - aylana, D - to'la doira,
    /// R - yumaloq to'rtburchak, F - to'la ko'pburchak, FR - to'la yumaloq to'rtburchak. Ko'rinishini tekshirish uchun
    /// xuddi shu hisob Python'da ham bor (icon_render.py). PNG lar UI/Art/Lobby/ ga yoziladi; JSON o'zgarsa qayta chiziladi.
    /// </summary>
    static partial class CraDevSceneBuilder
    {
        const string IconsFile = "Assets/CraDev/Editor/LobbyIcons.json";
        const string LobbyArtIcons = "Assets/CraDev/UI/Art/Lobby/";
        const int IconPixels = 72;

        [System.Serializable]
        class IconDefinition
        {
            public string name;
            public float stroke = 1.6f;
            public string[] paths;
        }

        [System.Serializable]
        class IconLibrary
        {
            public IconDefinition[] icons;
        }

        static Dictionary<string, IconDefinition> iconLibrary;

        /// <summary>Lobby ikonkasi (sprite). Nomi LobbyIcons.json dagidek (masalan "Shirt", "ChevronRight").</summary>
        static Sprite LineIcon(string name)
        {
            if (iconLibrary == null)
                iconLibrary = JsonUtility.FromJson<IconLibrary>(File.ReadAllText(IconsFile)).icons.ToDictionary(i => i.name);
            if (!iconLibrary.TryGetValue(name, out var icon))
                throw new KeyNotFoundException("LobbyIcons.json da ikonka yo'q: " + name);

            Directory.CreateDirectory(LobbyArtIcons);
            string path = LobbyArtIcons + "Icon_" + name + ".png";
            // Chizma o'zgargan bo'lsa (yoki birinchi marta) qayta chiziladi: chizma imzosi Library/ da saqlanadi
            string signature = Hash(icon);
            Directory.CreateDirectory("Library/CraDevIcons");
            string signaturePath = "Library/CraDevIcons/" + name + ".txt";
            if (!File.Exists(path) || !File.Exists(signaturePath) || File.ReadAllText(signaturePath) != signature)
            {
                var texture = RenderIcon(icon, IconPixels);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                File.WriteAllText(signaturePath, signature);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            return LoadSprite(path);
        }

        static string Hash(IconDefinition icon) =>
            IconPixels + "|" + icon.stroke.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + string.Join(";", icon.paths);

        static Texture2D RenderIcon(IconDefinition icon, int size)
        {
            var segments = new List<(Vector2 a, Vector2 b)>();
            var circles = new List<Vector3>();   // x, y, r (chiziq)
            var discs = new List<Vector3>();     // x, y, r (to'la)
            var polygons = new List<List<Vector2>>();

            void Polyline(List<Vector2> points, bool closed)
            {
                for (int i = 0; i < points.Count - 1; i++)
                    segments.Add((points[i], points[i + 1]));
                if (closed && points.Count > 2)
                    segments.Add((points[points.Count - 1], points[0]));
            }

            foreach (var path in icon.paths)
            {
                var parts = path.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                var v = parts.Skip(1).Select(p => float.Parse(p, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                switch (parts[0])
                {
                    case "L": Polyline(Pairs(v), false); break;
                    case "Z": Polyline(Pairs(v), true); break;
                    case "A": Polyline(ArcPoints(v[0], v[1], v[2], v[3], v[4], v[5]), false); break;
                    case "C": circles.Add(new Vector3(v[0], v[1], v[2])); break;
                    case "D": discs.Add(new Vector3(v[0], v[1], v[2])); break;
                    case "R": Polyline(RoundedRectPoints(v[0], v[1], v[2], v[3], v[4]), true); break;
                    case "F": polygons.Add(Pairs(v)); break;
                    case "FR": polygons.Add(RoundedRectPoints(v[0], v[1], v[2], v[3], v[4])); break;
                    default: throw new System.FormatException($"Ikonka {icon.name}: noma'lum buyruq {parts[0]}");
                }
            }

            float half = icon.stroke / 2f;
            float unit = size / 24f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int yi = 0; yi < size; yi++)
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2((x + 0.5f) / unit, (yi + 0.5f) / unit); // yi - rasm qatori (yuqoridan)
                    float d = float.MaxValue;
                    foreach (var s in segments)
                        d = Mathf.Min(d, SegmentDistance(p, s.a, s.b));
                    foreach (var c in circles)
                        d = Mathf.Min(d, Mathf.Abs(Vector2.Distance(p, c) - c.z));
                    float alpha = Mathf.Clamp01((half - d) * unit + 0.5f);
                    foreach (var c in discs)
                        alpha = Mathf.Max(alpha, Mathf.Clamp01((c.z - Vector2.Distance(p, c)) * unit + 0.5f));
                    foreach (var polygon in polygons)
                    {
                        float edge = float.MaxValue;
                        for (int i = 0; i < polygon.Count; i++)
                            edge = Mathf.Min(edge, SegmentDistance(p, polygon[i], polygon[(i + 1) % polygon.Count]));
                        bool inside = InsidePolygon(polygon, p);
                        alpha = Mathf.Max(alpha, inside ? Mathf.Clamp01(edge * unit + 0.5f) : Mathf.Clamp01(0.5f - edge * unit));
                    }
                    // Texture2D qatorlari pastdan yuqoriga
                    pixels[(size - 1 - yi) * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        static List<Vector2> Pairs(float[] v)
        {
            var points = new List<Vector2>();
            for (int i = 0; i + 1 < v.Length; i += 2)
                points.Add(new Vector2(v[i], v[i + 1]));
            return points;
        }

        static List<Vector2> ArcPoints(float cx, float cy, float rx, float ry, float from, float to)
        {
            int n = Mathf.Max(8, (int)(Mathf.Abs(to - from) / 6f));
            var points = new List<Vector2>();
            for (int i = 0; i <= n; i++)
            {
                float a = (from + (to - from) * i / n) * Mathf.Deg2Rad;
                points.Add(new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry));
            }
            return points;
        }

        static List<Vector2> RoundedRectPoints(float x, float y, float w, float h, float r)
        {
            r = Mathf.Max(0f, Mathf.Min(r, Mathf.Min(w / 2f, h / 2f)));
            var points = new List<Vector2>();
            foreach (var (cx, cy, start) in new[] { (x + w - r, y + r, -90f), (x + w - r, y + h - r, 0f), (x + r, y + h - r, 90f), (x + r, y + r, 180f) })
                for (int i = 0; i <= 6; i++)
                {
                    float a = (start + 90f * i / 6f) * Mathf.Deg2Rad;
                    points.Add(new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r));
                }
            return points;
        }
    }
}

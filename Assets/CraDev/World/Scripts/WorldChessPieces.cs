using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CraDev.World
{
    /// <summary>Shared lathed meshes, twelve instanced batches and short board-diff animations. No colliders.</summary>
    internal sealed class WorldChessPieces : IDisposable
    {
        static readonly char[] Types = { 'p', 'r', 'n', 'b', 'q', 'k' };
        public static readonly string[] InitialBoard = Initial();
        readonly Mesh[] meshes = new Mesh[6];
        readonly Material[] materials = new Material[2];
        readonly List<BoardView> boards = new List<BoardView>();
        readonly Matrix4x4[][] batches = new Matrix4x4[12][];
        readonly int[] counts = new int[12];
        public int VisibleCount { get; private set; }

        public WorldChessPieces()
        {
            for (int i = 0; i < meshes.Length; i++) meshes[i] = BuildPiece(Types[i]);
            var shader = Shader.Find("Standard");
            materials[0] = new Material(shader) { name = "ChessEbony", color = new Color(.052f, .065f, .061f), enableInstancing = true };
            materials[1] = new Material(shader) { name = "ChessIvory", color = new Color(.86f, .82f, .67f), enableInstancing = true };
            foreach (var material in materials) { material.SetFloat("_Metallic", .18f); material.SetFloat("_Glossiness", .74f); }
            for (int i = 0; i < batches.Length; i++) batches[i] = new Matrix4x4[640];
        }
        public BoardView Create(CityChessTable table)
        {
            var board = new BoardView(table); board.SetBoard(InitialBoard); boards.Add(board); return board;
        }
        public void Draw(Vector3 viewerPosition)
        {
            Array.Clear(counts, 0, counts.Length); VisibleCount = 0;
            foreach (var board in boards)
            {
                if (!board.Table || (board.Table.transform.position - viewerPosition).sqrMagnitude > 85f * 85f) continue;
                board.Animate();
                for (int square = 0; square < 64; square++)
                {
                    int batch = board.Kinds[square]; if (batch < 0) continue;
                    int index = counts[batch]++; batches[batch][index] = board.Matrices[square]; VisibleCount++;
                }
                for (int i = 0; i < board.CaptureCount; i++)
                {
                    int batch = board.CaptureKinds[i];
                    batches[batch][counts[batch]++] = board.CaptureMatrices[i];
                }
            }
            for (int i = 0; i < batches.Length; i++)
            {
                if (counts[i] == 0) continue;
                if (SystemInfo.supportsInstancing)
                    Graphics.DrawMeshInstanced(meshes[i % 6], 0, materials[i / 6], batches[i], counts[i], null, ShadowCastingMode.On, true, 0);
                else
                    for (int p = 0; p < counts[i]; p++) Graphics.DrawMesh(meshes[i % 6], batches[i][p], materials[i / 6], 0);
            }
        }
        public void Dispose()
        {
            foreach (var mesh in meshes) if (mesh) UnityEngine.Object.Destroy(mesh);
            foreach (var material in materials) if (material) UnityEngine.Object.Destroy(material);
            boards.Clear();
        }

        internal sealed class BoardView
        {
            internal readonly CityChessTable Table;
            internal readonly Matrix4x4[] Matrices = new Matrix4x4[64];
            internal readonly int[] Kinds = new int[64];
            internal readonly int[] CaptureKinds = new int[64];
            internal readonly Matrix4x4[] CaptureMatrices = new Matrix4x4[64];
            internal int CaptureCount;
            readonly string[] previous = new string[64];
            readonly Vector3[] starts = new Vector3[64], ends = new Vector3[64], capturedAt = new Vector3[64];
            readonly bool[] moving = new bool[64];
            float animationStart = -1f;
            internal BoardView(CityChessTable table) { Table = table; }
            public void SetBoard(string[] board, string lastFrom = "", string lastTo = "")
            {
                if (board == null || board.Length != 64) return;
                int changes = 0;
                for (int i = 0; i < 64; i++) if ((previous[i] ?? "") != (board[i] ?? "")) changes++;
                if (changes == 0) return;
                bool animate = changes >= 2 && changes <= 4 && animationStart >= 0f;
                var consumed = new bool[64];
                CaptureCount = 0;
                for (int square = 0; square < 64; square++)
                {
                    string piece = board[square]; Kinds[square] = -1; moving[square] = false;
                    if (string.IsNullOrEmpty(piece)) continue;
                    char type = char.ToLowerInvariant(piece[0]); int index = Array.IndexOf(Types, type);
                    if (index < 0) continue;
                    bool white = char.IsUpper(piece[0]); Kinds[square] = index + (white ? 6 : 0);
                    ends[square] = Table.SquareCenter(square % 8, square / 8);
                    starts[square] = ends[square];
                    if (animate && piece != previous[square])
                    {
                        int source = -1, named = WorldChess.SquareIndex(lastFrom);
                        // The explicit last move also matches a pawn promoted into a different piece.
                        if (WorldChess.SquareIndex(lastTo) == square && named >= 0 && !string.IsNullOrEmpty(previous[named])) source = named;
                        if (source < 0) for (int candidate = 0; candidate < 64; candidate++)
                            if (!consumed[candidate] && previous[candidate] == piece && board[candidate] != piece)
                            { source = candidate; break; }
                        if (source >= 0)
                        {
                            consumed[source] = true; moving[square] = true;
                            starts[square] = Table.SquareCenter(source % 8, source / 8);
                        }
                    }
                    Matrices[square] = Matrix4x4.TRS(ends[square], Quaternion.Euler(0f, white ? 0f : 180f, 0f), Vector3.one);
                }
                if (animate) for (int square = 0; square < 64; square++)
                {
                    string old = previous[square];
                    if (string.IsNullOrEmpty(old) || consumed[square] || old == board[square]) continue;
                    int kind = Array.IndexOf(Types, char.ToLowerInvariant(old[0]));
                    if (kind < 0) continue;
                    CaptureKinds[CaptureCount] = kind + (char.IsUpper(old[0]) ? 6 : 0);
                    capturedAt[CaptureCount++] = Table.SquareCenter(square % 8, square / 8);
                }
                Array.Copy(board, previous, 64);
                animationStart = Time.unscaledTime;
                Animate();
            }
            public void Animate()
            {
                float t = Mathf.Clamp01((Time.unscaledTime - animationStart) / .28f);
                float smooth = t * t * (3f - 2f * t);
                for (int i = 0; i < 64; i++)
                {
                    if (!moving[i] || Kinds[i] < 0) continue;
                    Vector3 p = Vector3.Lerp(starts[i], ends[i], smooth) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * .045f);
                    Matrices[i] = Matrix4x4.TRS(p, Quaternion.Euler(0, Kinds[i] >= 6 ? 0 : 180, 0), Vector3.one);
                    if (t >= 1f) moving[i] = false;
                }
                if (t >= 1f) CaptureCount = 0;
                for (int i = 0; i < CaptureCount; i++)
                    CaptureMatrices[i] = Matrix4x4.TRS(capturedAt[i], Quaternion.identity, Vector3.one * (1f - smooth));
            }
        }
        static string[] Initial()
        {
            var board = new string[64]; string line = "RNBQKBNR";
            for (int i = 0; i < 64; i++) board[i] = "";
            for (int i = 0; i < 8; i++) { board[i] = line[i].ToString(); board[i + 8] = "P"; board[i + 48] = "p"; board[i + 56] = char.ToLowerInvariant(line[i]).ToString(); }
            return board;
        }

        static Mesh BuildPiece(char kind)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            float scale = kind == 'p' ? .83f : kind == 'k' ? 1.13f : kind == 'q' ? 1.07f : 1f;
            var profile = new List<Vector2> { new Vector2(0, 0), new Vector2(.039f, 0), new Vector2(.043f, .005f), new Vector2(.043f, .011f), new Vector2(.038f, .017f), new Vector2(.036f, .024f), new Vector2(.029f, .031f), new Vector2(.025f, .038f) };
            if (kind == 'r')
                profile.AddRange(new[] { new Vector2(.023f, .06f), new Vector2(.022f, .095f), new Vector2(.032f, .10f), new Vector2(.036f, .106f), new Vector2(.036f, .126f), new Vector2(0, .126f) });
            else if (kind == 'n')
                profile.AddRange(new[] { new Vector2(.028f, .042f), new Vector2(.03f, .048f), new Vector2(0, .052f) });
            else
                profile.AddRange(new[] { new Vector2(.019f, .055f), new Vector2(.013f, .093f), new Vector2(.020f, .109f), new Vector2(.027f, .111f), new Vector2(.027f, .118f), new Vector2(.019f, .123f), new Vector2(0, .125f) });
            Lathe(vertices, triangles, profile, Vector3.zero, 32);
            if (kind == 'p') Sphere(vertices, triangles, new Vector3(0, .139f, 0), .025f);
            if (kind == 'r')
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * 60f * Mathf.Deg2Rad;
                    Box(vertices, triangles, new Vector3(Mathf.Sin(angle) * .027f, .135f, Mathf.Cos(angle) * .027f), new Vector3(.016f, .019f, .018f), Quaternion.Euler(0, i * 60f, 0));
                }
            if (kind == 'b')
                Lathe(vertices, triangles, new[] { new Vector2(0, .121f), new Vector2(.014f, .124f), new Vector2(.025f, .146f), new Vector2(.020f, .163f), new Vector2(.008f, .181f), new Vector2(0, .193f) }, Vector3.zero, 32);
            if (kind == 'q')
            {
                Lathe(vertices, triangles, new[] { new Vector2(0, .123f), new Vector2(.016f, .123f), new Vector2(.022f, .143f), new Vector2(.030f, .16f), new Vector2(.025f, .169f), new Vector2(0, .17f) }, Vector3.zero, 32);
                for (int i = 0; i < 8; i++) { float angle = i * Mathf.PI / 4f; Sphere(vertices, triangles, new Vector3(Mathf.Sin(angle) * .025f, .175f, Mathf.Cos(angle) * .025f), .007f); }
                Sphere(vertices, triangles, new Vector3(0, .186f, 0), .011f);
            }
            if (kind == 'k')
            {
                Lathe(vertices, triangles, new[] { new Vector2(0, .123f), new Vector2(.015f, .123f), new Vector2(.024f, .15f), new Vector2(.021f, .16f), new Vector2(0, .163f) }, Vector3.zero, 32);
                Box(vertices, triangles, new Vector3(0, .182f, 0), new Vector3(.013f, .043f, .013f), Quaternion.identity);
                Box(vertices, triangles, new Vector3(0, .187f, 0), new Vector3(.038f, .012f, .013f), Quaternion.identity);
            }
            if (kind == 'n')
            {
                var outline = new[] { new Vector2(-.029f,.05f),new Vector2(.028f,.05f),new Vector2(.024f,.075f),new Vector2(.010f,.105f),new Vector2(.006f,.127f),new Vector2(.033f,.127f),new Vector2(.041f,.14f),new Vector2(.021f,.165f),new Vector2(.003f,.177f),new Vector2(-.004f,.195f),new Vector2(-.016f,.181f),new Vector2(-.027f,.18f),new Vector2(-.025f,.166f),new Vector2(-.036f,.147f),new Vector2(-.038f,.12f),new Vector2(-.028f,.088f) };
                Extrude(vertices, triangles, outline, .012f);
            }
            for (int i = 0; i < vertices.Count; i++) vertices[i] *= scale;
            var mesh = new Mesh { name = "Chess_" + kind };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.UploadMeshData(true);
            return mesh;
        }

        static void Lathe(List<Vector3> vertices, List<int> triangles, IList<Vector2> profile, Vector3 center, int segments)
        {
            int start = vertices.Count;
            for (int row = 0; row < profile.Count; row++)
                for (int i = 0; i <= segments; i++)
                { float angle = i * Mathf.PI * 2f / segments; vertices.Add(center + new Vector3(Mathf.Sin(angle) * profile[row].x, profile[row].y, Mathf.Cos(angle) * profile[row].x)); }
            for (int row = 0; row < profile.Count - 1; row++)
                for (int i = 0; i < segments; i++)
                {
                    int a = start + row * (segments + 1) + i, b = a + segments + 1;
                    triangles.Add(a); triangles.Add(a + 1); triangles.Add(b); triangles.Add(a + 1); triangles.Add(b + 1); triangles.Add(b);
                }
        }
        static void Sphere(List<Vector3> vertices, List<int> triangles, Vector3 center, float radius)
        {
            var profile = new Vector2[13];
            for (int i = 0; i < profile.Length; i++) { float a = i * Mathf.PI / 12f; profile[i] = new Vector2(Mathf.Sin(a) * radius, -Mathf.Cos(a) * radius); }
            Lathe(vertices, triangles, profile, center, 20);
        }
        static void Box(List<Vector3> vertices, List<int> triangles, Vector3 center, Vector3 size, Quaternion rotation)
        {
            int start = vertices.Count;
            Vector3[] p = {new Vector3(-1,-1,-1),new Vector3(1,-1,-1),new Vector3(1,1,-1),new Vector3(-1,1,-1),new Vector3(-1,-1,1),new Vector3(1,-1,1),new Vector3(1,1,1),new Vector3(-1,1,1)};
            foreach (var point in p) vertices.Add(center + rotation * Vector3.Scale(point, size * .5f));
            int[] t = {0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5};
            foreach (int index in t) triangles.Add(start + index);
        }
        static void Extrude(List<Vector3> vertices, List<int> triangles, Vector2[] outline, float thickness)
        {
            int start = vertices.Count, n = outline.Length;
            foreach (var p in outline) vertices.Add(new Vector3(-thickness, p.y, p.x));
            foreach (var p in outline) vertices.Add(new Vector3(thickness, p.y, p.x));
            var face = ChessPieceGraphic.Triangulate(outline);
            for (int i = 0; i < face.Count; i += 3)
            {
                triangles.Add(start + face[i]); triangles.Add(start + face[i + 1]); triangles.Add(start + face[i + 2]);
                triangles.Add(start + n + face[i + 2]); triangles.Add(start + n + face[i + 1]); triangles.Add(start + n + face[i]);
            }
            for (int i = 0; i < n; i++) { int j = (i + 1) % n; triangles.Add(start+i);triangles.Add(start+n+i);triangles.Add(start+j);triangles.Add(start+j);triangles.Add(start+n+i);triangles.Add(start+n+j); }
        }
    }

    /// <summary>Font-independent chess silhouettes: works on every supported locale and display.</summary>
    public sealed class ChessPieceGraphic : MaskableGraphic
    {
        string piece = "";
        public string Piece { get => piece; set { value = value ?? ""; if (piece == value) return; piece = value; SetVerticesDirty(); } }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (string.IsNullOrEmpty(piece)) return;
            char type = char.ToLowerInvariant(piece[0]);
            bool white = char.IsUpper(piece[0]);
            Color fill = white ? new Color(.97f,.94f,.82f) : new Color(.075f,.105f,.09f);
            Color edge = white ? new Color(.16f,.23f,.19f) : new Color(.75f,.77f,.66f);
            float s = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .0107f;
            var center = rectTransform.rect.center;
            var shapes = Shapes(type);
            foreach (var shape in shapes)
            {
                Vector2[] offsets = {new Vector2(-1.05f,0),new Vector2(1.05f,0),new Vector2(0,-1.05f),new Vector2(0,1.05f)};
                foreach (var offset in offsets) Polygon(vh,shape,center+offset*s,s,edge);
                Polygon(vh,shape,center,s,fill);
            }
        }
        static List<Vector2[]> Shapes(char kind)
        {
            var list = new List<Vector2[]> { Polygon(-25,-29,25,-29,22,-23,-22,-23), Polygon(-21,-21,21,-21,17,-16,-17,-16) };
            if (kind == 'n') list.Add(Polygon(-16,-14,18,-14,15,-2,5,6,3,14,19,13,25,20,13,31,3,34,-1,41,-8,35,-14,35,-14,29,-22,22,-22,9,-16,-3));
            else if (kind == 'r') { list.Add(Polygon(-15,-14,15,-14,13,18,-13,18)); list.Add(Polygon(-21,18,21,18,21,34,12,34,12,27,5,27,5,34,-5,34,-5,27,-12,27,-12,34,-21,34)); }
            else
            {
                list.Add(Polygon(-15,-14,15,-14,10,-6,7,9,13,16,-13,16,-7,9,-10,-6));
                list.Add(Polygon(-17,15,17,15,17,20,-17,20));
                if (kind == 'p') list.Add(Circle(0,30,11));
                if (kind == 'b') list.Add(Polygon(0,44,12,29,9,21,-9,21,-12,29,-3,39,1,31,4,34));
                if (kind == 'q') { list.Add(Polygon(-12,21,12,21,21,37,10,32,0,41,-10,32,-21,37)); list.Add(Circle(-21,38,3)); list.Add(Circle(0,42,3)); list.Add(Circle(21,38,3)); }
                if (kind == 'k') { list.Add(Polygon(-9,21,9,21,14,29,11,34,-11,34,-14,29)); list.Add(Polygon(-3,32,3,32,3,38,10,38,10,44,3,44,3,50,-3,50,-3,44,-10,44,-10,38,-3,38)); }
            }
            return list;
        }
        static Vector2[] Polygon(params float[] points)
        { var result = new Vector2[points.Length/2]; for(int i=0;i<result.Length;i++) result[i]=new Vector2(points[i*2],points[i*2+1]-5f); return result; }
        static Vector2[] Circle(float x,float y,float r)
        { var result=new Vector2[24];for(int i=0;i<result.Length;i++){float a=i*Mathf.PI*2f/result.Length;result[i]=new Vector2(x+Mathf.Cos(a)*r,y-5f+Mathf.Sin(a)*r);}return result; }
        static void Polygon(VertexHelper vh,Vector2[] shape,Vector2 center,float scale,Color color)
        {
            int start=vh.currentVertCount;foreach(var p in shape) vh.AddVert(center+p*scale,color,Vector2.zero);
            var triangles=Triangulate(shape);for(int i=0;i<triangles.Count;i+=3)vh.AddTriangle(start+triangles[i],start+triangles[i+1],start+triangles[i+2]);
        }
        internal static List<int> Triangulate(Vector2[] polygon)
        {
            var order = new List<int>(); var output = new List<int>();
            float area=0;for(int i=0;i<polygon.Length;i++){var a=polygon[i];var b=polygon[(i+1)%polygon.Length];area+=a.x*b.y-b.x*a.y;}
            for(int i=0;i<polygon.Length;i++)order.Add(area>0?i:polygon.Length-1-i);
            int guard=polygon.Length*polygon.Length;
            while(order.Count>2 && guard-->0)
            {
                bool found=false;
                for(int i=0;i<order.Count;i++)
                {
                    int a=order[(i+order.Count-1)%order.Count],b=order[i],c=order[(i+1)%order.Count];
                    if(Cross(polygon[a],polygon[b],polygon[c])<=.000001f)continue;
                    bool contains=false;foreach(int p in order){if(p==a||p==b||p==c)continue;if(Cross(polygon[a],polygon[b],polygon[p])>=0&&Cross(polygon[b],polygon[c],polygon[p])>=0&&Cross(polygon[c],polygon[a],polygon[p])>=0){contains=true;break;}}
                    if(contains)continue;output.Add(a);output.Add(b);output.Add(c);order.RemoveAt(i);found=true;break;
                }
                if(!found)break;
            }
            return output;
        }
        static float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
    }
}

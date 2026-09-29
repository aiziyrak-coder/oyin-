using UnityEngine;
using UnityEngine.UI;
namespace CraDev.MainMenu
{
    /// <summary>
    /// Ovozli chat belgisi (spritesiz, vektor): 0 - mikrofon, 1 - karnay. O'chiq holatda ustidan qiya chiziq tortiladi.
    /// Tugmalarda ham, party nameplate'larida ham bir xil ishlatiladi.
    /// </summary>
    public class LobbyVoiceIcon : MaskableGraphic
    {
        public int kind;
        [SerializeField] bool off;
        public bool Off { get => off; set { if (off == value) return; off = value; SetVerticesDirty(); } }

        Rect box;
        Vector2 P(float x, float y) => new Vector2(box.xMin + x * box.width, box.yMin + y * box.height);
        void Tri(VertexHelper h, Vector2 a, Vector2 b, Vector2 c)
        {
            int n = h.currentVertCount;
            h.AddVert(P(a.x, a.y), color, Vector2.zero); h.AddVert(P(b.x, b.y), color, Vector2.zero); h.AddVert(P(c.x, c.y), color, Vector2.zero);
            h.AddTriangle(n, n + 1, n + 2);
        }
        void Quad(VertexHelper h, Vector2 a, Vector2 b, Vector2 c, Vector2 d) { Tri(h, a, b, c); Tri(h, a, c, d); }
        void Box(VertexHelper h, float x0, float y0, float x1, float y1) =>
            Quad(h, new Vector2(x0, y0), new Vector2(x0, y1), new Vector2(x1, y1), new Vector2(x1, y0));
        void Disc(VertexHelper h, float x, float y, float r)
        {
            for (int i = 0; i < 20; i++)
            {
                float a = i * Mathf.PI / 10, b = (i + 1) * Mathf.PI / 10;
                Tri(h, new Vector2(x, y), new Vector2(x + Mathf.Cos(a) * r, y + Mathf.Sin(a) * r), new Vector2(x + Mathf.Cos(b) * r, y + Mathf.Sin(b) * r));
            }
        }
        // Yoy chizig'i (burchaklar radianda, qalinlik 0..1 birlikda)
        void Arc(VertexHelper h, float x, float y, float r, float from, float to, float width)
        {
            const int steps = 14;
            for (int i = 0; i < steps; i++)
            {
                float a = Mathf.Lerp(from, to, i / (float)steps), b = Mathf.Lerp(from, to, (i + 1) / (float)steps);
                float ri = r - width / 2, ro = r + width / 2;
                Quad(h, new Vector2(x + Mathf.Cos(a) * ri, y + Mathf.Sin(a) * ri), new Vector2(x + Mathf.Cos(a) * ro, y + Mathf.Sin(a) * ro),
                    new Vector2(x + Mathf.Cos(b) * ro, y + Mathf.Sin(b) * ro), new Vector2(x + Mathf.Cos(b) * ri, y + Mathf.Sin(b) * ri));
            }
        }
        void Line(VertexHelper h, Vector2 a, Vector2 b, float width)
        {
            var d = (b - a).normalized; var n = new Vector2(-d.y, d.x) * width / 2;
            Quad(h, a - n, b - n, b + n, a + n);
        }
        protected override void OnPopulateMesh(VertexHelper h)
        {
            h.Clear();
            box = rectTransform.rect;
            // Kvadrat ichida chizish (cho'zilmasin)
            float side = Mathf.Min(box.width, box.height);
            box = new Rect(box.center.x - side / 2, box.center.y - side / 2, side, side);
            const float w = .085f;
            if (kind == 0)
            {
                // Mikrofon: kapsula, ushlagich yoyi, oyoqcha va taglik
                Box(h, .38f, .47f, .62f, .75f); Disc(h, .5f, .75f, .12f); Disc(h, .5f, .47f, .12f);
                Arc(h, .5f, .5f, .22f, Mathf.PI * 1.02f, Mathf.PI * 1.98f, w);
                Line(h, new Vector2(.28f, .52f), new Vector2(.28f, .5f), w); Line(h, new Vector2(.72f, .52f), new Vector2(.72f, .5f), w);
                Box(h, .5f - w / 2, .14f, .5f + w / 2, .28f); Box(h, .34f, .1f, .66f, .1f + w);
            }
            else
            {
                // Karnay: quti + konus, yoqilganda tovush to'lqinlari
                Box(h, .12f, .38f, .28f, .62f);
                Quad(h, new Vector2(.28f, .38f), new Vector2(.28f, .62f), new Vector2(.5f, .82f), new Vector2(.5f, .18f));
                if (!off)
                {
                    Arc(h, .5f, .5f, .15f, -Mathf.PI * .3f, Mathf.PI * .3f, w);
                    Arc(h, .5f, .5f, .3f, -Mathf.PI * .32f, Mathf.PI * .32f, w);
                }
            }
            if (off)
            {
                // Qiya chiziq: qizil emas, o'z rangida (rangni skript beradi), atrofida ingichka bo'shliq yo'q - sodda
                Line(h, new Vector2(.12f, .9f), new Vector2(.88f, .1f), w * 1.2f);
            }
        }
    }
}

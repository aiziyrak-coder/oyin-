using UnityEngine;
using UnityEngine.UI;

namespace CraDev
{
    /// <summary>
    /// Splash va Loading ekranlari fonida sekin suzib yuradigan, miltillovchi uchqunlar.
    /// UI Image'lardan tuzilgan, shuning uchun istalgan render pipeline'da ishlaydi.
    /// Umumiy yorqinlikni <see cref="Intensity"/> orqali sahna skripti boshqaradi.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UiParticles : MonoBehaviour
    {
        [SerializeField] Sprite sprite;
        [SerializeField, Range(0, 200)] int count = 70;
        [Tooltip("Uchqun o'lchami (min, max), UI birliklarida.")]
        [SerializeField] Vector2 sizeRange = new Vector2(4f, 12f);
        [Tooltip("Yuqoriga ko'tarilish tezligi (min, max), birlik/soniya.")]
        [SerializeField] Vector2 riseSpeed = new Vector2(8f, 30f);
        [SerializeField] Color colorA = new Color(0.25f, 0.85f, 1f);
        [SerializeField] Color colorB = new Color(0.62f, 0.40f, 1f);

        [Range(0f, 1f)] public float Intensity;

        struct Mote
        {
            public RectTransform rect;
            public Image image;
            public Vector2 position;
            public Vector2 velocity;
            public Color color;
            public float phase;
            public float twinkleSpeed;
            public float brightness;
        }

        RectTransform area;
        Mote[] motes;

        void Start()
        {
            area = (RectTransform)transform;
            Rect bounds = area.rect;
            motes = new Mote[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Mote", typeof(RectTransform), typeof(Image));
                go.layer = gameObject.layer;
                var rect = (RectTransform)go.transform;
                rect.SetParent(area, false);
                float size = Random.Range(sizeRange.x, sizeRange.y);
                rect.sizeDelta = new Vector2(size, size);

                var image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.raycastTarget = false;

                motes[i] = new Mote
                {
                    rect = rect,
                    image = image,
                    position = new Vector2(Random.Range(bounds.xMin, bounds.xMax), Random.Range(bounds.yMin, bounds.yMax)),
                    velocity = new Vector2(Random.Range(-6f, 6f), Random.Range(riseSpeed.x, riseSpeed.y)),
                    color = Color.Lerp(colorA, colorB, Random.value),
                    phase = Random.Range(0f, Mathf.PI * 2f),
                    twinkleSpeed = Random.Range(0.8f, 2.6f),
                    brightness = Random.Range(0.25f, 0.9f),
                };
            }
            Update();
        }

        void Update()
        {
            if (motes == null)
                return;

            Rect bounds = area.rect;
            float dt = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;
            const float margin = 16f;

            for (int i = 0; i < motes.Length; i++)
            {
                ref Mote m = ref motes[i];
                m.position += m.velocity * dt;

                // Ekrandan chiqib ketgan uchqunni pastdan qayta chiqaramiz
                if (m.position.y > bounds.yMax + margin)
                {
                    m.position.y = bounds.yMin - margin;
                    m.position.x = Random.Range(bounds.xMin, bounds.xMax);
                }
                if (m.position.x < bounds.xMin - margin) m.position.x = bounds.xMax + margin;
                else if (m.position.x > bounds.xMax + margin) m.position.x = bounds.xMin - margin;

                float twinkle = 0.55f + 0.45f * Mathf.Sin(time * m.twinkleSpeed + m.phase);
                Color c = m.color;
                c.a = m.brightness * twinkle * Intensity;
                m.image.color = c;
                m.rect.anchoredPosition = m.position;
            }
        }
    }
}

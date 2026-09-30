using UnityEngine;

namespace CraDev.World
{
    /// <summary>Physical LED sign: a slow readable marquee, no network or video decoder.</summary>
    public sealed class CityBillboard : MonoBehaviour
    {
        [SerializeField] RectTransform first;
        [SerializeField] RectTransform second;
        [SerializeField] float repeatWidth = 1140f;
        [SerializeField] float pixelsPerSecond = 46f;
        float offset;

        public void Configure(RectTransform a, RectTransform b, float width)
        {
            first = a; second = b; repeatWidth = width;
        }

        void Update()
        {
            if (!first || !second) return;
            offset = Mathf.Repeat(offset + Time.deltaTime * pixelsPerSecond, repeatWidth);
            first.anchoredPosition = new Vector2(-offset, 0);
            second.anchoredPosition = new Vector2(repeatWidth - offset, 0);
        }
    }
}

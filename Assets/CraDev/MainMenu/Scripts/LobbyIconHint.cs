using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    /// <summary>
    /// Ikonkali tugma izohi: sichqoncha ustiga kelganda yoki tugma tanlanganda (klaviatura, geympad) ko'rsatiladi.
    /// floating=false: label - doimiy yozuv, unga izoh (yoki fallback) yoziladi. floating=true: label umumiy suzuvchi
    /// izoh pufagi ichida; pufak shu tugma ostiga (joy bo'lmasa ustiga) qo'yiladi va ekrandan chiqmaydi.
    /// </summary>
    public class LobbyIconHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] Text label;
        [SerializeField] string key, fallback;
        [SerializeField] bool floating;
        const float Gap = 8, Padding = 16, Height = 42, MaxWidth = 460;
        static LobbyIconHint shown;

        /// <summary>Ish vaqtida yaratilgan tugmaga (do'stlar qatori, qo'ng'iroq) suzuvchi izoh ulaydi.</summary>
        public static LobbyIconHint Attach(GameObject target, Text bubbleLabel, string key)
        {
            if (target == null || bubbleLabel == null) return null;
            if (!target.TryGetComponent(out LobbyIconHint hint)) hint = target.AddComponent<LobbyIconHint>();
            hint.label = bubbleLabel; hint.key = key; hint.floating = true;
            return hint;
        }

        void Show(bool active)
        {
            if (label == null) return;
            if (!floating) { label.text = active ? Loc.T(key) : string.IsNullOrEmpty(fallback) ? "" : Loc.T(fallback); return; }
            var bubble = label.transform.parent as RectTransform;
            if (bubble == null) return;
            if (!active) { if (shown == this) { shown = null; bubble.gameObject.SetActive(false); } return; }
            shown = this;
            label.text = Loc.T(key);
            bubble.gameObject.SetActive(true);
            bubble.SetAsLastSibling();
            Place(bubble);
        }

        void Place(RectTransform bubble)
        {
            var area = bubble.parent as RectTransform;
            var target = transform as RectTransform;
            if (area == null || target == null) return;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.pivot = new Vector2(.5f, .5f);
            labelRect.offsetMin = new Vector2(Padding, 0); labelRect.offsetMax = new Vector2(-Padding, 0);
            float width = Mathf.Min(MaxWidth, label.preferredWidth + Padding * 2);
            bubble.anchorMin = bubble.anchorMax = new Vector2(.5f, .5f);
            bubble.sizeDelta = new Vector2(width, Height);
            // Tugma burchaklari izoh joylashgan panel koordinatalarida
            var corners = new Vector3[4]; target.GetWorldCorners(corners);
            for (int i = 0; i < 4; i++) corners[i] = area.InverseTransformPoint(corners[i]);
            var rect = area.rect;
            float x = (corners[0].x + corners[2].x) / 2;
            bool below = corners[0].y - Gap - Height >= rect.yMin + Gap;
            bubble.pivot = new Vector2(.5f, below ? 1 : 0);
            float y = below ? corners[0].y - Gap : corners[1].y + Gap;
            x = Mathf.Clamp(x, rect.xMin + Gap + width / 2, rect.xMax - Gap - width / 2);
            bubble.anchoredPosition = new Vector2(x, y) - rect.center;
        }

        void OnDisable() { if (shown == this) Show(false); }
        public void OnPointerEnter(PointerEventData e) => Show(true);
        public void OnPointerExit(PointerEventData e) => Show(false);
        public void OnSelect(BaseEventData e) => Show(true);
        public void OnDeselect(BaseEventData e) => Show(false);
    }
}

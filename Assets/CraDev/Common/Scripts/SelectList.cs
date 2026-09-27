using System;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev
{
    /// <summary>
    /// Bittasi tanlanadigan tugmalar guruhi: yon menyu (kategoriyalar), yorliqlar (tab) va shunga o'xshashlar.
    /// Tanlanganida foni, matni va ikonkasi rangi o'zgaradi; ixtiyoriy "indicator" tanlangan element ostiga suriladi.
    /// </summary>
    public class SelectList : MonoBehaviour
    {
        [SerializeField] Button[] items;
        [SerializeField] Image[] fills;
        [SerializeField] Text[] labels;
        [SerializeField] Image[] icons;
        [Tooltip("Tanlangan element ostidagi chiziq yoki fon (ixtiyoriy): shu elementga silliq suriladi.")]
        [SerializeField] RectTransform indicator;
        [SerializeField] Color selectedFill = new Color(0.24f, 0.4f, 1f, 1f);
        [SerializeField] Color normalFill = new Color(1f, 1f, 1f, 0.05f);
        [SerializeField] Color selectedText = Color.white;
        [SerializeField] Color normalText = new Color(1f, 1f, 1f, 0.82f);
        [SerializeField] Color selectedIcon = Color.white;
        [SerializeField] Color normalIcon = new Color(1f, 1f, 1f, 0.82f);

        public int Selected { get; private set; } = -1;
        public int Count => items.Length;

        /// <summary>Foydalanuvchi boshqa elementni tanlaganda (kod orqali tanlanganda chaqirilmaydi).</summary>
        public event Action<int> Changed;

        void Awake()
        {
            for (int i = 0; i < items.Length; i++)
            {
                int index = i;
                items[i].onClick.AddListener(() =>
                {
                    if (index == Selected)
                        return;
                    Select(index);
                    Changed?.Invoke(index);
                });
            }
            if (Selected < 0)
                Select(0);
        }

        public void Select(int index)
        {
            Selected = index;
            for (int i = 0; i < items.Length; i++)
            {
                bool on = i == index;
                if (fills != null && i < fills.Length && fills[i] != null) fills[i].color = on ? selectedFill : normalFill;
                if (labels != null && i < labels.Length && labels[i] != null) labels[i].color = on ? selectedText : normalText;
                if (icons != null && i < icons.Length && icons[i] != null) icons[i].color = on ? selectedIcon : normalIcon;
            }
            if (indicator != null)
                indicator.gameObject.SetActive(index >= 0 && index < items.Length);
        }

        public void SetVisible(int index, bool visible) => items[index].gameObject.SetActive(visible);

        void Update()
        {
            if (indicator == null || Selected < 0 || Selected >= items.Length)
                return;
            // Indikator tanlangan tugma ostiga silliq suriladi (o'lchami ham moslashadi)
            var target = (RectTransform)items[Selected].transform;
            float t = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            indicator.position = Vector3.Lerp(indicator.position, new Vector3(target.position.x, indicator.position.y, indicator.position.z), t);
            var size = indicator.sizeDelta;
            indicator.sizeDelta = new Vector2(Mathf.Lerp(size.x, target.rect.width, t), size.y);
        }
    }
}

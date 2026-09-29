using System;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev
{
    /// <summary>
    /// Yoqish/o'chirish tugmasi (iOS uslubidagi "switch"): bosilganda qiymat almashadi, tugmacha silliq suriladi.
    /// Builder maydonlarni Set bilan yozadi; ish vaqtida yaratilgan qatorlar <see cref="Init"/> ni chaqiradi.
    /// Tugmacha trek markaziga bog'lanadi va -travel..+travel oralig'ida suriladi.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SwitchToggle : MonoBehaviour
    {
        [SerializeField] Image track;
        [SerializeField] RectTransform knob;
        [SerializeField] Color onColor = new Color(0.24f, 0.4f, 1f, 1f);
        [SerializeField] Color offColor = new Color(1f, 1f, 1f, 0.16f);
        [SerializeField] float travel = 18f;

        bool value;
        float shown;

        public event Action<bool> Changed;

        /// <summary>Qiymat (animatsiya bilan suriladi, Changed chaqirilmaydi).</summary>
        public bool Value
        {
            get => value;
            set { this.value = value; }
        }

        /// <summary>Ish vaqtida yaratilgan switch uchun: qismlar, ranglar va boshlang'ich qiymat (animatsiyasiz).</summary>
        public void Init(Image track, RectTransform knob, Color on, Color off, float travel, bool value)
        {
            this.track = track;
            this.knob = knob;
            onColor = on;
            offColor = off;
            this.travel = travel;
            Set(value);
        }

        void Awake() => GetComponent<Button>().onClick.AddListener(() =>
        {
            value = !value;
            Changed?.Invoke(value);
        });

        /// <summary>Qiymatni animatsiyasiz o'rnatadi (sahifa ochilganda).</summary>
        public void Set(bool on)
        {
            value = on;
            shown = on ? 1f : 0f;
            Apply();
        }

        void Update()
        {
            shown = Mathf.MoveTowards(shown, value ? 1f : 0f, Time.unscaledDeltaTime / 0.15f);
            Apply();
        }

        void Apply()
        {
            if (track == null || knob == null)
                return;
            float t = Ease.InOutSine(shown);
            track.color = Color.Lerp(offColor, onColor, t);
            knob.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, t), 0f);
        }
    }
}

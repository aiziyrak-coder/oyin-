using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraDev.CharacterCreation
{
    /// <summary>
    /// O'ng paneldagi katta qahramon. Sichqoncha bilan chapga-o'ngga tortilsa 360° aylanadi.
    ///
    /// Qahramonning bir necha tomondan rasmlari bor (old, yon, orqa, bo'lsa 45° burchaklar ham).
    /// Burilish burchagiga qarab ikki qo'shni rasm orasida o'tish qilinadi va rasm biroz torayib-kengayadi,
    /// shunda aylanish hissi paydo bo'ladi. Qo'yib yuborilgach, qahramon eng yaqin tomonga silliq to'xtaydi.
    /// Chap tomon rasmlari o'ng tomon rasmlarining ko'zgu aksidan olinadi.
    /// </summary>
    public class AvatarViewer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] Image layerA;
        [SerializeField] Image layerB;
        [Tooltip("Qahramon rasmining ekrandagi balandligi (1920x1080 birliklarida).")]
        [SerializeField] float height = 780f;
        [Tooltip("Sichqoncha 1 birlik surilganda necha gradus aylanadi.")]
        [SerializeField] float dragSensitivity = 0.45f;
        [Tooltip("Qo'yib yuborilgach eng yaqin tomonga qaytish tezligi.")]
        [SerializeField] float snapSpeed = 9f;
        [Tooltip("Burilish paytida rasm qanchalik torayadi (aylanish hissi uchun).")]
        [SerializeField, Range(0f, 0.5f)] float turnSquash = 0.2f;

        [Header("Ko'rinish tugmalari (Front, Side, Back)")]
        [SerializeField] Button[] viewButtons;
        [SerializeField] Image[] viewFills;
        [SerializeField] Text[] viewLabels;

        static readonly Color ActiveFill = new Color32(42, 43, 49, 255);
        static readonly Color Muted = new Color32(142, 147, 154, 255);
        static readonly float[] ButtonAngles = { 0f, 90f, 180f };

        struct View
        {
            public float Angle;
            public Sprite Sprite;
            public bool Mirror;
        }

        readonly List<View> views = new List<View>();
        float yaw;       // 0 = old, 90 = o'ngga qaragan, 180 = orqa, 270 = chapga qaragan
        float target;
        float velocity;  // gradus/soniya, qo'yib yuborilgandan keyingi inersiya
        bool dragging;
        Canvas canvas;

        void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            for (int i = 0; i < viewButtons.Length; i++)
            {
                float angle = ButtonAngles[i];
                viewButtons[i].onClick.AddListener(() => RotateTo(angle));
            }
        }

        public void SetAvatar(AvatarOption option)
        {
            views.Clear();
            Add(0f, option.front, false);
            Add(45f, option.frontQuarter, false);
            Add(90f, option.side, false);
            Add(135f, option.backQuarter, false);
            Add(180f, option.back, false);
            Add(225f, option.backQuarter, true);
            Add(270f, option.side, true);
            Add(315f, option.frontQuarter, true);
            Apply();
        }

        void Add(float angle, Sprite sprite, bool mirror)
        {
            if (sprite != null)
                views.Add(new View { Angle = angle, Sprite = sprite, Mirror = mirror });
        }

        /// <summary>Qahramonni berilgan tomonga eng qisqa yo'l bilan buradi.</summary>
        public void RotateTo(float angle)
        {
            velocity = 0f;
            target = yaw + Mathf.DeltaAngle(yaw, angle);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (!dragging)
            {
                if (Mathf.Abs(velocity) > 20f)
                {
                    // Qo'yib yuborilgandan keyin biroz o'zi aylanib, sekinlashadi
                    yaw += velocity * dt;
                    velocity *= Mathf.Exp(-5f * dt);
                    target = NearestViewAngle(yaw);
                }
                else
                {
                    velocity = 0f;
                    yaw = Mathf.Lerp(yaw, target, 1f - Mathf.Exp(-snapSpeed * dt));
                }
            }
            Apply();
        }

        float NearestViewAngle(float angle)
        {
            float best = angle, bestDistance = float.MaxValue;
            foreach (var v in views)
            {
                float d = Mathf.Abs(Mathf.DeltaAngle(angle, v.Angle));
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = angle + Mathf.DeltaAngle(angle, v.Angle);
                }
            }
            return best;
        }

        void Apply()
        {
            if (views.Count == 0)
                return;

            // Joriy burchakni o'rab turgan ikki ko'rinishni topamiz
            float a = Mathf.Repeat(yaw, 360f);
            int i = views.Count - 1;
            for (int k = 0; k < views.Count; k++)
                if (views[k].Angle <= a) i = k;
            View from = views[i];
            View to = views[(i + 1) % views.Count];
            float span = Mathf.Repeat(to.Angle - from.Angle, 360f);
            if (span <= 0f) span = 360f;
            float t = Mathf.Clamp01(Mathf.Repeat(a - from.Angle, 360f) / span);

            // Birinchi yarmida yangi rasm paydo bo'ladi, ikkinchi yarmida eskisi yo'qoladi: shaffoflik "cho'kmaydi"
            float appear = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 0.5f, t));
            float vanish = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 0.85f, t));
            float squash = 1f - turnSquash * Mathf.Sin(t * Mathf.PI);

            Place(layerA, from, 1f - vanish, squash);
            Place(layerB, to, appear, squash);

            // O'tishning birinchi yarmida eski ko'rinish ustida, ikkinchi yarmida yangisi
            Transform top = (t < 0.5f ? layerA : layerB).transform;
            Transform bottom = (t < 0.5f ? layerB : layerA).transform;
            if (top.GetSiblingIndex() < bottom.GetSiblingIndex())
            {
                int low = top.GetSiblingIndex();
                top.SetSiblingIndex(bottom.GetSiblingIndex());
                bottom.SetSiblingIndex(low);
            }

            UpdateButtons(a);
        }

        void Place(Image image, View view, float alpha, float squash)
        {
            image.sprite = view.Sprite;
            float aspect = view.Sprite.rect.width / view.Sprite.rect.height;
            image.rectTransform.sizeDelta = new Vector2(height * aspect, height);
            image.rectTransform.localScale = new Vector3((view.Mirror ? -1f : 1f) * squash, 1f, 1f);
            Anim.SetAlpha(image, alpha);
        }

        void UpdateButtons(float angle)
        {
            // Yon tugmasi ikkala yon tomon uchun ham yonadi
            float folded = angle > 180f ? 360f - angle : angle;
            int active = folded < 45f ? 0 : folded <= 135f ? 1 : 2;
            for (int i = 0; i < viewButtons.Length; i++)
            {
                bool on = i == active;
                viewFills[i].color = on ? ActiveFill : new Color(0f, 0f, 0f, 0f);
                viewLabels[i].color = on ? Color.white : Muted;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = true;
            velocity = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
            float delta = eventData.delta.x / scale * dragSensitivity;
            yaw += delta;
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.001f);
            velocity = Mathf.Lerp(velocity, delta / dt, 0.3f);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragging = false;
            target = NearestViewAngle(yaw + velocity * 0.12f);
        }
    }
}

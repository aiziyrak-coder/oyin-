using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraDev.CharacterCreation
{
    /// <summary>
    /// O'ng paneldagi katta avatar. Old, yon va orqa rasmlari orasida silliq almashadi.
    /// Sichqoncha bilan chapga-o'ngga tortilsa, avatar "aylanadi": old → yon → orqa → ikkinchi yon.
    /// Haqiqiy 3D modellar tayyor bo'lganda shu komponent 3D ko'rinish bilan almashtiriladi.
    /// </summary>
    public class AvatarViewer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] Image current;
        [SerializeField] Image previous;
        [Tooltip("Avatar rasmining ekrandagi balandligi (1920x1080 birliklarida).")]
        [SerializeField] float height = 780f;
        [Tooltip("Keyingi ko'rinishga o'tish uchun qancha tortish kerak.")]
        [SerializeField] float dragStep = 90f;
        [SerializeField] float fadeDuration = 0.18f;

        [Header("Ko'rinish tugmalari (Front, Side, Back)")]
        [SerializeField] Button[] viewButtons;
        [SerializeField] Image[] viewFills;
        [SerializeField] Text[] viewLabels;

        static readonly Color ActiveFill = new Color32(42, 43, 49, 255);
        static readonly Color Muted = new Color32(142, 147, 154, 255);

        // 0 = old, 1 = yon, 2 = orqa, 3 = ikkinchi yon (yon rasm ko'zgu aksida)
        int view;
        AvatarOption avatar;
        float fade = 1f;
        float dragAccum;
        Canvas canvas;

        void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            for (int i = 0; i < viewButtons.Length; i++)
            {
                int index = i;
                viewButtons[i].onClick.AddListener(() => SetView(index));
            }
            previous.enabled = false;
        }

        public void SetAvatar(AvatarOption option)
        {
            avatar = option;
            Show(view);
        }

        public void SetView(int newView)
        {
            view = ((newView % 4) + 4) % 4;
            Show(view);
        }

        void Show(int v)
        {
            if (avatar == null)
                return;
            Sprite sprite = v == 0 ? avatar.front : v == 2 ? avatar.back : avatar.side;
            if (sprite == null)
                sprite = avatar.front;

            // Oldingi rasm biroz ko'rinib turadi va yangisi ustidan paydo bo'ladi
            if (current.sprite != null)
            {
                previous.sprite = current.sprite;
                previous.rectTransform.sizeDelta = current.rectTransform.sizeDelta;
                previous.rectTransform.localScale = current.rectTransform.localScale;
                previous.enabled = true;
            }
            current.sprite = sprite;
            float aspect = sprite.rect.width / sprite.rect.height;
            current.rectTransform.sizeDelta = new Vector2(height * aspect, height);
            current.rectTransform.localScale = new Vector3(v == 3 ? -1f : 1f, 1f, 1f);
            fade = 0f;
            UpdateButtons();
        }

        void UpdateButtons()
        {
            int active = view == 3 ? 1 : view;
            for (int i = 0; i < viewButtons.Length; i++)
            {
                bool on = i == active;
                viewFills[i].color = on ? ActiveFill : new Color(0f, 0f, 0f, 0f);
                viewLabels[i].color = on ? Color.white : Muted;
            }
        }

        void Update()
        {
            if (fade >= 1f)
                return;
            fade = Mathf.Min(1f, fade + Time.unscaledDeltaTime / fadeDuration);
            Anim.SetAlpha(current, fade);
            Anim.SetAlpha(previous, 1f - fade);
            if (fade >= 1f)
                previous.enabled = false;
        }

        public void OnBeginDrag(PointerEventData eventData) => dragAccum = 0f;

        public void OnDrag(PointerEventData eventData)
        {
            float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
            dragAccum += eventData.delta.x / scale;
            if (Mathf.Abs(dragAccum) >= dragStep)
            {
                SetView(view + (dragAccum > 0f ? 1 : -1));
                dragAccum = 0f;
            }
        }

        public void OnEndDrag(PointerEventData eventData) => dragAccum = 0f;
    }
}

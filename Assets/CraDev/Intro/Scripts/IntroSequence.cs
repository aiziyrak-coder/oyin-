using UnityEngine;
using UnityEngine.UI;

namespace CraDev.Intro
{
    /// <summary>
    /// O'yin ochilganda birinchi ko'rsatiladigan CraDev kompaniyasi intro'si. Zamonaviy, tekis uslub.
    ///
    /// Ketma-ketlik: ko'k belgi ekran markazida paydo bo'ladi -> chapga suriladi va uning
    /// ortidan "CraDev" yozuvi chiqib keladi -> ostida "A NEW ERA OF GAMING" -> qorong'ilashish ->
    /// keyingi sahna (CDCGroup).
    ///
    /// Barcha qiymatlar Inspector'da o'zgartiriladi. Sahna quruvchi elementlarni yakuniy
    /// joylashuvda saqlaydi, skript esa ularni shu joyga qarab animatsiya qiladi.
    /// </summary>
    public class IntroSequence : SplashSequence
    {
        [Header("Elementlar")]
        [Tooltip("Belgi (fon kvadrati + ichidagi oq shakl) konteyneri.")]
        [SerializeField] RectTransform mark;
        [SerializeField] Image tile;
        [SerializeField] Image glyph;
        [Tooltip("Niqob (RectMask2D) ichidagi \"CraDev\" yozuvi.")]
        [SerializeField] RectTransform wordmark;
        [SerializeField] Image tagline;

        [Header("Vaqtlar (soniya)")]
        [SerializeField] float markStart = 0.25f;
        [SerializeField] float markDuration = 0.6f;
        [SerializeField] float glyphStart = 0.35f;
        [SerializeField] float glyphDuration = 0.55f;
        [Tooltip("Belgi chapga surilib, yozuv chiqib keladigan payt.")]
        [SerializeField] float slideStart = 0.95f;
        [SerializeField] float slideDuration = 0.85f;
        [SerializeField] float taglineStart = 1.75f;
        [SerializeField] float taglineDuration = 0.7f;

        [Header("Ko'rinish")]
        [Tooltip("Belgi boshida qanchalik kichik bo'ladi.")]
        [SerializeField, Range(0f, 1f)] float markStartScale = 0.6f;
        [Tooltip("Yozuv niqob ortidan qancha masofadan chiqib keladi.")]
        [SerializeField] float wordmarkTravel = 580f;
        [SerializeField] float taglineRise = 12f;

        Vector2 markFinal;
        Vector2 wordmarkFinal;
        Vector2 taglineFinal;

        protected override void Awake()
        {
            if (mark != null) markFinal = mark.anchoredPosition;
            if (wordmark != null) wordmarkFinal = wordmark.anchoredPosition;
            if (tagline != null) taglineFinal = tagline.rectTransform.anchoredPosition;
            base.Awake();
        }

        protected override void Apply(float t, float visible)
        {
            // Belgi: markazda kichikdan kattalashadi, keyin chapdagi joyiga suriladi
            float m = Anim.Progress(t, markStart, markDuration);
            float slide = Ease.InOutCubic(Anim.Progress(t, slideStart, slideDuration));
            if (mark != null)
            {
                float s = Mathf.Lerp(markStartScale, 1f, Ease.OutQuint(m));
                mark.localScale = new Vector3(s, s, 1f);
                mark.anchoredPosition = new Vector2(Mathf.Lerp(0f, markFinal.x, slide), markFinal.y);
            }
            if (tile != null)
                Anim.SetAlpha(tile, Ease.OutCubic(Anim.Progress(t, markStart, markDuration * 0.4f)));

            // Ichidagi oq shakl biroz kechroq "sakrab" chiqadi
            float g = Anim.Progress(t, glyphStart, glyphDuration);
            if (glyph != null)
            {
                float s = Mathf.LerpUnclamped(0.4f, 1f, Ease.OutBack(g));
                glyph.rectTransform.localScale = new Vector3(s, s, 1f);
                Anim.SetAlpha(glyph, Ease.OutCubic(Anim.Progress(t, glyphStart, glyphDuration * 0.4f)));
            }

            // Yozuv niqobning chap chetidan o'ngga chiqib keladi
            if (wordmark != null)
            {
                float w = Ease.OutQuint(Anim.Progress(t, slideStart + 0.05f, slideDuration));
                wordmark.anchoredPosition = wordmarkFinal + new Vector2(-wordmarkTravel * (1f - w), 0f);
            }

            float tg = Ease.OutCubic(Anim.Progress(t, taglineStart, taglineDuration));
            if (tagline != null)
            {
                Anim.SetAlpha(tagline, tg);
                tagline.rectTransform.anchoredPosition = taglineFinal + new Vector2(0f, -taglineRise * (1f - tg));
            }
        }
    }
}

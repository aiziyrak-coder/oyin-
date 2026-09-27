using UnityEngine;
using UnityEngine.UI;

namespace CraDev.CDCGroup
{
    /// <summary>
    /// Ikkinchi brend: CDCGroup kumush logosi.
    ///
    /// Ketma-ketlik: emblema aylanib joyiga tushadi (ovozdagi yumshoq zarba bilan) ->
    /// "CDC GROUP" yozuvi chapdan o'ngga ochiladi -> butun logo ustidan kumush yaltirash
    /// o'tadi (metall "shiing" ovozi bilan), ostida chiziq cho'ziladi -> qorong'ilashish ->
    /// keyingi sahna (Loading).
    /// </summary>
    public class CdcGroupSplash : SplashSequence
    {
        [Header("Elementlar")]
        [SerializeField] RectTransform lockup;
        [SerializeField] Image glow;
        [SerializeField] Image emblem;
        [SerializeField] RectTransform wordmarkReveal;
        [SerializeField] RectTransform wordmark;
        [SerializeField] RectTransform glintWindow;
        [SerializeField] Image glintHighlight;
        [SerializeField] Image line;
        [SerializeField] UiParticles particles;

        [Header("Vaqtlar (soniya)")]
        [SerializeField] float emblemStart = 0.35f;
        [Tooltip("Emblema aylanib joyiga tushguncha. Oxiri ovozdagi zarbaga to'g'ri keladi.")]
        [SerializeField] float emblemDuration = 0.95f;
        [SerializeField] float wordmarkStart = 1.05f;
        [SerializeField] float wordmarkDuration = 0.8f;
        [SerializeField] float glintStart = 1.95f;
        [SerializeField] float glintDuration = 0.75f;
        [SerializeField] float lineStart = 2.05f;
        [SerializeField] float lineDuration = 0.9f;

        [Header("Ko'rinish")]
        [Tooltip("Emblema boshida necha gradusga burilgan bo'ladi.")]
        [SerializeField] float emblemSpin = 90f;
        [SerializeField, Range(0f, 1f)] float glowAlpha = 0.16f;
        [SerializeField, Range(0f, 1f)] float glowFlash = 0.22f;
        [SerializeField] float wordmarkRevealWidth = 520f;
        [SerializeField] float wordmarkSlide = 24f;
        [SerializeField] float glintTravel = 430f;
        [SerializeField, Range(0f, 1f)] float glintAlpha = 0.9f;
        [SerializeField, Range(0f, 1f)] float lineAlpha = 0.6f;
        [SerializeField] float lockupZoom = 0.03f;

        Vector2 wordmarkBasePosition;
        Vector2 highlightPosition;

        protected override void Awake()
        {
            if (wordmark != null)
                wordmarkBasePosition = wordmark.anchoredPosition;
            // Yaltirash oynasi harakatlansa ham, ichidagi oq nusxa logoning ustida qimirlamay turishi kerak
            if (glintWindow != null && glintHighlight != null)
                highlightPosition = glintWindow.anchoredPosition + glintHighlight.rectTransform.anchoredPosition;
            base.Awake();
        }

        protected override void Apply(float t, float visible)
        {
            float impact = emblemStart + emblemDuration;

            if (lockup != null)
            {
                float zoom = 1f + lockupZoom * Ease.InOutSine(Anim.Progress(t, 0f, TotalDuration));
                lockup.localScale = new Vector3(zoom, zoom, 1f);
            }

            // Emblema aylanib, kattalashib joyiga tushadi
            float e = Ease.OutCubic(Anim.Progress(t, emblemStart, emblemDuration));
            if (emblem != null)
            {
                emblem.rectTransform.localRotation = Quaternion.Euler(0f, 0f, emblemSpin * (1f - e));
                float s = Mathf.Lerp(0.85f, 1f, e);
                emblem.rectTransform.localScale = new Vector3(s, s, 1f);
                Anim.SetAlpha(emblem, Ease.OutCubic(Anim.Progress(t, emblemStart, emblemDuration * 0.5f)));
            }

            if (glow != null)
            {
                float flash = t >= impact ? glowFlash * Mathf.Exp(-(t - impact) / 0.4f) : 0f;
                Anim.SetAlpha(glow, glowAlpha * e + flash);
            }

            // "CDC GROUP" chapdan o'ngga ochiladi va biroz siljib joyiga keladi
            float w = Anim.Progress(t, wordmarkStart, wordmarkDuration);
            if (wordmarkReveal != null)
            {
                Vector2 size = wordmarkReveal.sizeDelta;
                size.x = wordmarkRevealWidth * Ease.InOutCubic(w);
                wordmarkReveal.sizeDelta = size;
            }
            if (wordmark != null)
                wordmark.anchoredPosition = wordmarkBasePosition + new Vector2(-wordmarkSlide * (1f - Ease.OutCubic(w)), 0f);

            // Kumush yaltirash: tor oyna logoning ustidan chapdan o'ngga o'tadi
            float g = Anim.Progress(t, glintStart, glintDuration);
            if (glintWindow != null)
            {
                Vector2 p = glintWindow.anchoredPosition;
                p.x = Mathf.Lerp(-glintTravel, glintTravel, Ease.InOutSine(g));
                glintWindow.anchoredPosition = p;
                if (glintHighlight != null)
                {
                    glintHighlight.rectTransform.anchoredPosition = highlightPosition - p;
                    Anim.SetAlpha(glintHighlight, glintAlpha * Mathf.Sin(g * Mathf.PI));
                }
            }

            // Logo ostidagi ingichka chiziq markazdan cho'ziladi
            float l = Ease.OutCubic(Anim.Progress(t, lineStart, lineDuration));
            if (line != null)
            {
                line.rectTransform.localScale = new Vector3(l, 1f, 1f);
                Anim.SetAlpha(line, lineAlpha * l);
            }

            if (particles != null)
                particles.Intensity = Ease.InOutSine(Anim.Progress(t, fadeInDuration, 1.2f)) * visible;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace CraDev.Intro
{
    /// <summary>
    /// O'yin ochilganda birinchi ko'rsatiladigan CraDev kompaniyasi intro'si.
    ///
    /// Butun animatsiya bitta vaqt o'qiga bog'langan: har kadrda elementlarning holati
    /// faqat o'tgan vaqtdan (t) hisoblanadi. Shu sababli "Vaqtlar" bo'limidagi
    /// qiymatlarni Inspector'da bemalol o'zgartirish mumkin.
    ///
    /// Ketma-ketlik: qorong'ilikdan chiqish -> emblema paydo bo'ladi (ovozdagi zarba bilan)
    /// -> "CraDev" yozuvi markazdan ochiladi -> yozuv ustidan nur o'tadi -> chiziq va
    /// "A NEW ERA OF GAMING" -> qorong'ilashish -> keyingi sahna (CDCGroup).
    /// </summary>
    public class IntroSequence : SplashSequence
    {
        [Header("Elementlar")]
        [SerializeField] RectTransform logoRoot;
        [SerializeField] Image glow;
        [SerializeField] Image emblem;
        [SerializeField] RectTransform wordmarkReveal;
        [SerializeField] Image wordmarkGlow;
        [SerializeField] Image shine;
        [SerializeField] Image divider;
        [SerializeField] Image tagline;
        [SerializeField] UiParticles particles;

        [Header("Vaqtlar (soniya)")]
        [SerializeField] float emblemStart = 0.55f;
        [SerializeField] float emblemDuration = 0.5f;
        [Tooltip("Ovoz faylidagi zarba ovoz boshidan qancha keyin keladi. Nur chaqnashi shunga moslanadi.")]
        [SerializeField] float soundImpactOffset = 0.45f;
        [SerializeField] float wordmarkStart = 1.45f;
        [SerializeField] float wordmarkDuration = 1.0f;
        [SerializeField] float shineStart = 2.45f;
        [SerializeField] float shineDuration = 0.8f;
        [SerializeField] float dividerStart = 2.75f;
        [SerializeField] float dividerDuration = 0.6f;
        [SerializeField] float taglineStart = 2.95f;
        [SerializeField] float taglineDuration = 0.9f;

        [Header("Ko'rinish")]
        [SerializeField, Range(0f, 1f)] float glowAlpha = 0.35f;
        [Tooltip("Zarba paytidagi qo'shimcha nur chaqnashi.")]
        [SerializeField, Range(0f, 1f)] float glowFlash = 0.45f;
        [SerializeField, Range(0f, 1f)] float wordmarkGlowAlpha = 0.8f;
        [Tooltip("Yozuv to'liq ochilgandagi niqob kengligi. Yumshoq chetlar harflarni to'smasligi uchun yozuvdan ancha keng.")]
        [SerializeField] float wordmarkRevealWidth = 820f;
        [SerializeField] float shineTravel = 330f;
        [SerializeField] float taglineRise = 14f;
        [Tooltip("Butun intro davomida logo sekin yaqinlashadi (0.04 = 4%).")]
        [SerializeField] float logoZoom = 0.04f;

        Vector2 taglineBasePosition;

        protected override void Awake()
        {
            if (tagline != null)
                taglineBasePosition = tagline.rectTransform.anchoredPosition;
            base.Awake();
        }

        protected override void Apply(float t, float visible)
        {
            float impact = soundStart + soundImpactOffset;

            if (logoRoot != null)
            {
                float zoom = 1f + logoZoom * Ease.InOutSine(Anim.Progress(t, 0f, TotalDuration));
                logoRoot.localScale = new Vector3(zoom, zoom, 1f);
            }

            // Emblema: kichikdan kattalashib, biroz "sakrab" joyiga tushadi
            float e = Anim.Progress(t, emblemStart, emblemDuration);
            if (emblem != null)
            {
                float s = Mathf.LerpUnclamped(0.6f, 1f, Ease.OutBack(e));
                emblem.rectTransform.localScale = new Vector3(s, s, 1f);
                Anim.SetAlpha(emblem, Ease.OutCubic(Anim.Progress(t, emblemStart, emblemDuration * 0.5f)));
            }

            // Emblema ortidagi nur: zarba paytida chaqnaydi, so'ng bir tekis yonib turadi
            if (glow != null)
            {
                float flash = t >= impact ? glowFlash * Mathf.Exp(-(t - impact) / 0.35f) : 0f;
                float breathe = 1f + 0.08f * Mathf.Sin((t - impact) * 1.6f);
                Anim.SetAlpha(glow, (glowAlpha * breathe + flash) * Ease.OutCubic(e));
                float gs = Mathf.Lerp(0.75f, 1f, Ease.OutCubic(Anim.Progress(t, emblemStart, 1.2f))) + flash * 0.15f;
                glow.rectTransform.localScale = new Vector3(gs, gs, 1f);
            }

            // "CraDev" yozuvi markazdan ikki tomonga ochiladi
            float w = Ease.InOutCubic(Anim.Progress(t, wordmarkStart, wordmarkDuration));
            if (wordmarkReveal != null)
            {
                Vector2 size = wordmarkReveal.sizeDelta;
                size.x = wordmarkRevealWidth * w;
                wordmarkReveal.sizeDelta = size;
            }
            if (wordmarkGlow != null)
            {
                float pulse = 0.9f + 0.1f * Mathf.Sin(t * 2.2f);
                Anim.SetAlpha(wordmarkGlow, wordmarkGlowAlpha * pulse * w);
            }

            // Yozuv ustidan o'tadigan nur
            float sh = Anim.Progress(t, shineStart, shineDuration);
            if (shine != null)
            {
                Vector2 p = shine.rectTransform.anchoredPosition;
                p.x = Mathf.Lerp(-shineTravel, shineTravel, Ease.InOutSine(sh));
                shine.rectTransform.anchoredPosition = p;
                Anim.SetAlpha(shine, Mathf.Sin(sh * Mathf.PI));
            }

            // Ajratuvchi chiziq markazdan cho'ziladi
            float d = Anim.Progress(t, dividerStart, dividerDuration);
            if (divider != null)
            {
                divider.rectTransform.localScale = new Vector3(Ease.OutCubic(d), 1f, 1f);
                Anim.SetAlpha(divider, Ease.OutCubic(Anim.Progress(t, dividerStart, dividerDuration * 0.5f)));
            }

            // "A NEW ERA OF GAMING" pastdan ko'tarilib paydo bo'ladi
            float g = Ease.OutCubic(Anim.Progress(t, taglineStart, taglineDuration));
            if (tagline != null)
            {
                Anim.SetAlpha(tagline, g);
                tagline.rectTransform.anchoredPosition = taglineBasePosition + new Vector2(0f, -taglineRise * (1f - g));
            }

            if (particles != null)
                particles.Intensity = Ease.InOutSine(Anim.Progress(t, fadeInDuration, 1.2f)) * visible;
        }
    }
}

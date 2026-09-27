using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CraDev.Intro
{
    /// <summary>
    /// O'yin ochilganda ko'rsatiladigan CraDev kompaniyasi intro'si.
    ///
    /// Butun animatsiya bitta vaqt o'qiga bog'langan: har kadrda elementlarning holati
    /// faqat o'tgan vaqtdan (t) hisoblanadi. Shu sababli "Vaqtlar" bo'limidagi
    /// qiymatlarni Inspector'da bemalol o'zgartirish mumkin.
    ///
    /// Ketma-ketlik: qorong'ilikdan chiqish -> emblema paydo bo'ladi (ovozdagi zarba bilan)
    /// -> "CraDev" yozuvi markazdan ochiladi -> yozuv ustidan nur o'tadi -> chiziq va
    /// "A NEW ERA OF GAMING" -> qorong'ilashish -> keyingi sahna.
    /// </summary>
    [DisallowMultipleComponent]
    public class IntroSequence : MonoBehaviour
    {
        [Header("Keyingi sahna")]
        [Tooltip("Intro tugagach yuklanadigan sahna nomi. U Build Settings ro'yxatida bo'lishi kerak.")]
        [SerializeField] string nextScene = "MainMenu";
        [Tooltip("Istalgan tugma, sichqoncha yoki geympad tugmasi bosilsa intro o'tkazib yuboriladi.")]
        [SerializeField] bool allowSkip = true;

        [Header("Elementlar")]
        [SerializeField] RectTransform logoRoot;
        [SerializeField] Image glow;
        [SerializeField] Image emblem;
        [SerializeField] RectTransform wordmarkReveal;
        [SerializeField] Image wordmarkGlow;
        [SerializeField] Image shine;
        [SerializeField] Image divider;
        [SerializeField] Image tagline;
        [SerializeField] Image fader;
        [SerializeField] IntroParticles particles;
        [SerializeField] AudioSource sound;

        [Header("Vaqtlar (soniya)")]
        [SerializeField] float fadeInDuration = 0.8f;
        [SerializeField] float emblemStart = 0.55f;
        [SerializeField] float emblemDuration = 0.5f;
        [Tooltip("Ovoz qachon boshlanadi.")]
        [SerializeField] float soundStart = 0.6f;
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
        [SerializeField] float fadeOutStart = 5.6f;
        [SerializeField] float fadeOutDuration = 1.0f;
        [Tooltip("O'tkazib yuborilganda qorong'ilashish davomiyligi.")]
        [SerializeField] float skipFadeDuration = 0.4f;

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

        float time;
        float endStart;
        float endDuration;
        bool soundPlayed;
        bool finished;
        Vector2 taglineBasePosition;
        float soundVolume = 1f;

        void Awake()
        {
            endStart = fadeOutStart;
            endDuration = fadeOutDuration;
            if (tagline != null)
                taglineBasePosition = tagline.rectTransform.anchoredPosition;
            if (sound != null)
                soundVolume = sound.volume;
            Apply(0f);
        }

        void OnEnable() => Cursor.visible = false;

        void OnDisable() => Cursor.visible = true;

        void Update()
        {
            if (finished)
                return;

            // Sahna yuklanishidagi birinchi og'ir kadr animatsiyani "sakratib" yubormasligi uchun
            time += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

            if (allowSkip && time > 0.3f && time < endStart && SkipPressed())
            {
                endStart = time;
                endDuration = skipFadeDuration;
            }

            if (!soundPlayed && time >= soundStart)
            {
                soundPlayed = true;
                if (sound != null && sound.clip != null)
                    sound.Play();
            }

            Apply(time);

            if (time >= endStart + endDuration)
            {
                finished = true;
                LoadNextScene();
            }
        }

        /// <summary>Barcha elementlarni t vaqtdagi holatiga keltiradi.</summary>
        void Apply(float t)
        {
            float impact = soundStart + soundImpactOffset;
            float fadeIn = 1f - Ease.OutCubic(Progress(t, 0f, fadeInDuration));
            float fadeOut = Ease.InOutSine(Progress(t, endStart, endDuration));
            float visible = 1f - fadeOut;

            if (fader != null)
                SetAlpha(fader, Mathf.Max(fadeIn, fadeOut));

            if (logoRoot != null)
            {
                float zoom = 1f + logoZoom * Ease.InOutSine(Progress(t, 0f, fadeOutStart + fadeOutDuration));
                logoRoot.localScale = new Vector3(zoom, zoom, 1f);
            }

            // Emblema: kichikdan kattalashib, biroz "sakrab" joyiga tushadi
            float e = Progress(t, emblemStart, emblemDuration);
            if (emblem != null)
            {
                float s = Mathf.LerpUnclamped(0.6f, 1f, Ease.OutBack(e));
                emblem.rectTransform.localScale = new Vector3(s, s, 1f);
                SetAlpha(emblem, Ease.OutCubic(Progress(t, emblemStart, emblemDuration * 0.5f)));
            }

            // Emblema ortidagi nur: zarba paytida chaqnaydi, so'ng bir tekis yonib turadi
            if (glow != null)
            {
                float flash = t >= impact ? glowFlash * Mathf.Exp(-(t - impact) / 0.35f) : 0f;
                float breathe = 1f + 0.08f * Mathf.Sin((t - impact) * 1.6f);
                float a = (glowAlpha * breathe + flash) * Ease.OutCubic(e);
                SetAlpha(glow, a);
                float gs = Mathf.Lerp(0.75f, 1f, Ease.OutCubic(Progress(t, emblemStart, 1.2f))) + flash * 0.15f;
                glow.rectTransform.localScale = new Vector3(gs, gs, 1f);
            }

            // "CraDev" yozuvi markazdan ikki tomonga ochiladi
            float w = Ease.InOutCubic(Progress(t, wordmarkStart, wordmarkDuration));
            if (wordmarkReveal != null)
            {
                Vector2 size = wordmarkReveal.sizeDelta;
                size.x = wordmarkRevealWidth * w;
                wordmarkReveal.sizeDelta = size;
            }
            if (wordmarkGlow != null)
            {
                float pulse = 0.9f + 0.1f * Mathf.Sin(t * 2.2f);
                SetAlpha(wordmarkGlow, wordmarkGlowAlpha * pulse * w);
            }

            // Yozuv ustidan o'tadigan nur
            float sh = Progress(t, shineStart, shineDuration);
            if (shine != null)
            {
                Vector2 p = shine.rectTransform.anchoredPosition;
                p.x = Mathf.Lerp(-shineTravel, shineTravel, Ease.InOutSine(sh));
                shine.rectTransform.anchoredPosition = p;
                SetAlpha(shine, Mathf.Sin(sh * Mathf.PI));
            }

            // Ajratuvchi chiziq markazdan cho'ziladi
            float d = Progress(t, dividerStart, dividerDuration);
            if (divider != null)
            {
                divider.rectTransform.localScale = new Vector3(Ease.OutCubic(d), 1f, 1f);
                SetAlpha(divider, Ease.OutCubic(Progress(t, dividerStart, dividerDuration * 0.5f)));
            }

            // "A NEW ERA OF GAMING" pastdan ko'tarilib paydo bo'ladi
            float g = Ease.OutCubic(Progress(t, taglineStart, taglineDuration));
            if (tagline != null)
            {
                SetAlpha(tagline, g);
                tagline.rectTransform.anchoredPosition = taglineBasePosition + new Vector2(0f, -taglineRise * (1f - g));
            }

            if (particles != null)
                particles.Intensity = Ease.InOutSine(Progress(t, fadeInDuration, 1.2f)) * visible;

            if (sound != null)
                sound.volume = soundVolume * visible;
        }

        void LoadNextScene()
        {
            if (!string.IsNullOrEmpty(nextScene) && Application.CanStreamedLevelBeLoaded(nextScene))
            {
                SceneManager.LoadScene(nextScene);
                return;
            }
            Debug.Log($"[CraDev] Intro tugadi. Keyingi sahna \"{nextScene}\" hali yaratilmagan yoki Build Settings'ga qo'shilmagan.");
        }

        static bool SkipPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)) return true;
            if (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame)) return true;
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.anyKeyDown;
#else
            return false;
#endif
        }

        static float Progress(float t, float start, float duration) =>
            duration <= 0f ? (t >= start ? 1f : 0f) : Mathf.Clamp01((t - start) / duration);

        static void SetAlpha(Graphic graphic, float alpha)
        {
            Color c = graphic.color;
            c.a = Mathf.Clamp01(alpha);
            graphic.color = c;
        }

        static class Ease
        {
            public static float OutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);

            public static float InOutCubic(float x) =>
                x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f;

            public static float InOutSine(float x) => -(Mathf.Cos(Mathf.PI * x) - 1f) / 2f;

            public static float OutBack(float x)
            {
                const float c1 = 1.70158f;
                const float c3 = c1 + 1f;
                return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
            }
        }
    }
}

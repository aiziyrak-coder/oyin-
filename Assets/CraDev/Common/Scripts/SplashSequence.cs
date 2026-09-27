using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CraDev
{
    /// <summary>
    /// Brend splash ekranlari (CraDev, CDCGroup) uchun umumiy asos.
    ///
    /// Vaqtni hisoblaydi, qora parda bilan ochadi va yopadi, ovozni o'z vaqtida boshlaydi,
    /// tugma bosilsa o'tkazib yuboradi va oxirida keyingi sahnani yuklaydi.
    /// Voris klass faqat <see cref="Apply"/> da o'z elementlarini t vaqtga mos holatga keltiradi.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class SplashSequence : MonoBehaviour
    {
        [Header("Keyingi sahna")]
        [Tooltip("Splash tugagach yuklanadigan sahna nomi. U Build Settings ro'yxatida bo'lishi kerak.")]
        [SerializeField] string nextScene = "";
        [Tooltip("Istalgan tugma, sichqoncha yoki geympad tugmasi bosilsa splash o'tkazib yuboriladi.")]
        [SerializeField] bool allowSkip = true;

        [Header("Umumiy")]
        [SerializeField] Image fader;
        [SerializeField] AudioSource sound;
        [SerializeField] protected float fadeInDuration = 0.8f;
        [Tooltip("Ovoz qachon boshlanadi (soniya).")]
        [SerializeField] protected float soundStart = 0.6f;
        [SerializeField] protected float fadeOutStart = 5.6f;
        [SerializeField] protected float fadeOutDuration = 1.0f;
        [Tooltip("O'tkazib yuborilganda qorong'ilashish davomiyligi.")]
        [SerializeField] float skipFadeDuration = 0.4f;

        float time;
        float endStart;
        float endDuration;
        float soundVolume = 1f;
        bool soundPlayed;
        bool finished;

        /// <summary>Splashning to'liq davomiyligi (o'tkazib yuborilmasa).</summary>
        protected float TotalDuration => fadeOutStart + fadeOutDuration;

        /// <summary>Elementlarni t vaqtdagi holatga keltiradi. visible: 1 = to'liq ko'rinadi, 0 = qorong'ilashib bo'lgan.</summary>
        protected abstract void Apply(float t, float visible);

        protected virtual void Awake()
        {
            endStart = fadeOutStart;
            endDuration = fadeOutDuration;
            if (sound != null)
                soundVolume = sound.volume;
            ApplyAll(0f);
        }

        void OnEnable() => Cursor.visible = false;

        void OnDisable() => Cursor.visible = true;

        void Update()
        {
            if (finished)
                return;

            // Sahna yuklanishidagi birinchi og'ir kadr animatsiyani "sakratib" yubormasligi uchun
            time += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

            if (allowSkip && time > 0.3f && time < endStart && Anim.AnyInputPressed())
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

            ApplyAll(time);

            if (time >= endStart + endDuration)
            {
                finished = true;
                LoadNextScene();
            }
        }

        void ApplyAll(float t)
        {
            float fadeIn = 1f - Ease.OutCubic(Anim.Progress(t, 0f, fadeInDuration));
            float fadeOut = Ease.InOutSine(Anim.Progress(t, endStart, endDuration));
            float visible = 1f - fadeOut;

            if (fader != null)
                Anim.SetAlpha(fader, Mathf.Max(fadeIn, fadeOut));
            if (sound != null)
                sound.volume = soundVolume * visible;

            Apply(t, visible);
        }

        void LoadNextScene()
        {
            if (!string.IsNullOrEmpty(nextScene) && Application.CanStreamedLevelBeLoaded(nextScene))
            {
                SceneManager.LoadScene(nextScene);
                return;
            }
            Debug.Log($"[CraDev] {name} tugadi. Keyingi sahna \"{nextScene}\" hali yaratilmagan yoki Build Settings'ga qo'shilmagan.");
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CraDev.Loading
{
    /// <summary>
    /// Loading ekrani: keyingi sahnani fonda yuklaydi va jarayonni foizda ko'rsatadi.
    ///
    /// Yuklanadigan sahna <see cref="SceneLoader.Load"/> orqali beriladi, berilmagan bo'lsa
    /// "Default Scene" (MainMenu) olinadi. Yuklash juda tez tugasa ham ekran kamida
    /// "Minimum Duration" soniya ko'rinib turadi. 100% ga yetgach qorong'ilashib, sahna faollashadi.
    /// </summary>
    [DisallowMultipleComponent]
    public class LoadingScreen : MonoBehaviour
    {
        [Header("Yuklanadigan sahna")]
        [SerializeField] string defaultScene = "MainMenu";
        [Tooltip("Yuklash juda tez bo'lsa ham ekran kamida shuncha soniya ko'rinib turadi.")]
        [SerializeField] float minimumDuration = 3f;
        [Tooltip("100% bo'lgach sahna almashguncha kutish.")]
        [SerializeField] float holdAtFull = 0.35f;

        [Header("Elementlar")]
        [SerializeField] Image fader;
        [SerializeField] RectTransform spinnerOuter;
        [SerializeField] RectTransform spinnerInner;
        [SerializeField] Image hex;
        [SerializeField] RectTransform barFill;
        [SerializeField] Image barHead;
        [SerializeField] Text percentText;
        [SerializeField] Text statusText;
        [SerializeField] UiParticles particles;

        [Header("Ko'rinish")]
        [SerializeField] float fadeInDuration = 0.5f;
        [SerializeField] float fadeOutDuration = 0.6f;
        [Tooltip("Tashqi yoyning aylanish tezligi, gradus/soniya.")]
        [SerializeField] float spinnerSpeed = 150f;
        [SerializeField] string loadingLabel = "L O A D I N G";
        [SerializeField] string readyLabel = "R E A D Y";

        AsyncOperation operation;
        float time;
        float shown;
        float fadeOutAt = -1f;
        int lastPercent = -1;
        bool activated;
        bool waitingWithoutScene;

        void OnEnable() => Cursor.visible = false;

        void OnDisable() => Cursor.visible = true;

        void Start()
        {
            string target = SceneLoader.TakePendingScene();
            if (string.IsNullOrEmpty(target))
                target = defaultScene;

            if (Application.CanStreamedLevelBeLoaded(target))
            {
                operation = SceneManager.LoadSceneAsync(target);
                operation.allowSceneActivation = false;
            }
            else
            {
                Debug.Log($"[CraDev] Loading: \"{target}\" sahnasi hali yaratilmagan yoki Build Settings'ga qo'shilmagan. Jarayon faqat namoyish uchun ko'rsatiladi.");
            }

            if (statusText != null)
                statusText.text = loadingLabel;
            Apply(0f);
        }

        void Update()
        {
            if (activated)
                return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            time += dt;

            // Ko'rsatiladigan foiz: haqiqiy yuklanish va minimal vaqtning kichigi, silliq o'zgaradi.
            // Unity'da progress 0.9 da to'xtaydi (qolgani sahnani faollashtirish), shuning uchun 0.9 = 100%.
            float real = operation != null ? Mathf.Clamp01(operation.progress / 0.9f) : 1f;
            float timed = Ease.InOutSine(Mathf.Clamp01(time / minimumDuration));
            shown = Mathf.MoveTowards(shown, Mathf.Min(real, timed), dt * 1.5f);

            if (shown >= 1f && fadeOutAt < 0f)
            {
                if (operation != null)
                    fadeOutAt = time + holdAtFull;
                else if (!waitingWithoutScene)
                {
                    waitingWithoutScene = true;
                    if (statusText != null)
                        statusText.text = readyLabel;
                }
            }

            Apply(time);

            if (fadeOutAt >= 0f && time >= fadeOutAt + fadeOutDuration)
            {
                activated = true;
                operation.allowSceneActivation = true;
            }
        }

        void Apply(float t)
        {
            float fadeIn = 1f - Ease.OutCubic(Anim.Progress(t, 0f, fadeInDuration));
            float fadeOut = fadeOutAt >= 0f ? Ease.InOutSine(Anim.Progress(t, fadeOutAt, fadeOutDuration)) : 0f;
            float visible = 1f - fadeOut;

            if (fader != null)
                Anim.SetAlpha(fader, Mathf.Max(fadeIn, fadeOut));

            // Ikki yoy qarama-qarshi tomonga aylanadi
            if (spinnerOuter != null)
                spinnerOuter.localRotation = Quaternion.Euler(0f, 0f, -t * spinnerSpeed);
            if (spinnerInner != null)
                spinnerInner.localRotation = Quaternion.Euler(0f, 0f, t * spinnerSpeed * 0.65f + 120f);
            if (hex != null)
                Anim.SetAlpha(hex, 0.2f + 0.08f * Mathf.Sin(t * 2.4f));

            if (barFill != null)
                barFill.anchorMax = new Vector2(shown, 1f);
            if (barHead != null)
                Anim.SetAlpha(barHead, shown > 0.001f ? 0.9f : 0f);

            int percent = Mathf.RoundToInt(shown * 100f);
            if (percentText != null && percent != lastPercent)
            {
                lastPercent = percent;
                percentText.text = percent + "%";
            }
            if (statusText != null)
                Anim.SetAlpha(statusText, waitingWithoutScene ? 0.9f : 0.55f + 0.35f * Mathf.Sin(t * 3f));

            if (particles != null)
                particles.Intensity = 0.7f * Ease.InOutSine(Anim.Progress(t, 0f, 1f)) * visible;
        }
    }
}

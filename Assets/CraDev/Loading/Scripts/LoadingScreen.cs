using CraDev.Online;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CraDev.Loading
{
    /// <summary>
    /// Loading ekrani: keyingi sahnani fonda yuklaydi va jarayonni katta foiz raqami va
    /// ingichka chiziq bilan ko'rsatadi.
    ///
    /// Yuklanadigan sahna <see cref="SceneLoader.Load"/> orqali beriladi. Berilmagan bo'lsa (o'yin endi
    /// ochilganda): profili yo'q yangi o'yinchi avatar yaratish ekraniga, qaytgan o'yinchi bosh menyuga o'tadi. Yuklash juda tez tugasa ham ekran kamida
    /// "Minimum Duration" soniya ko'rinib turadi. 100% ga yetgach qorong'ilashib, sahna faollashadi.
    /// </summary>
    [DisallowMultipleComponent]
    public class LoadingScreen : MonoBehaviour
    {
        [Header("Yuklanadigan sahna")]
        [Tooltip("Yangi o'yinchi (profili yo'q) uchun: avatar yaratish.")]
        [SerializeField] string newPlayerScene = "CharacterCreation";
        [Tooltip("Profili bor o'yinchi uchun.")]
        [SerializeField] string returningPlayerScene = "MainMenu";
        [Tooltip("Yuklash juda tez bo'lsa ham ekran kamida shuncha soniya ko'rinib turadi.")]
        [SerializeField] float minimumDuration = 3f;
        [Tooltip("100% bo'lgach sahna almashguncha kutish.")]
        [SerializeField] float holdAtFull = 0.35f;

        [Header("Elementlar")]
        [SerializeField] Image fader;
        [SerializeField] Text percentText;
        [SerializeField] Text statusText;
        [Tooltip("Progress chizig'ining to'ladigan qismi (anchorMax.x = foiz).")]
        [SerializeField] RectTransform barFill;

        [Header("Ko'rinish")]
        [SerializeField] float fadeInDuration = 0.4f;
        [SerializeField] float fadeOutDuration = 0.5f;
        [SerializeField] int percentSignSize = 48;
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
                target = PlayerProfile.Exists ? returningPlayerScene : newPlayerScene;

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
            if (fader != null)
                Anim.SetAlpha(fader, Mathf.Max(fadeIn, fadeOut));

            if (barFill != null)
                barFill.anchorMax = new Vector2(shown, 1f);

            int percent = Mathf.RoundToInt(shown * 100f);
            if (percentText != null && percent != lastPercent)
            {
                lastPercent = percent;
                percentText.text = $"{percent}<size={percentSignSize}>%</size>";
            }
            if (statusText != null)
                Anim.SetAlpha(statusText, waitingWithoutScene ? 1f : 0.6f + 0.4f * Mathf.Sin(t * 3f));
        }
    }
}

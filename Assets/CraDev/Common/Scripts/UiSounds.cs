using UnityEngine;
using UnityEngine.UI;

namespace CraDev
{
    /// <summary>
    /// Sahnadagi barcha tugmalarga (yashiringan oynalardagilar ham) sichqoncha ustiga kelganda va bosilganda
    /// qisqa ovoz beradi. Sahnada bitta bo'ladi; ovozlarni builder yaratadi (Assets/CraDev/UI/Audio).
    /// Ish vaqtida yaratilgan tugmalar <see cref="Hook"/> bilan ulanadi. Balandlik: volume x GameSettings.SfxVolume.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class UiSounds : MonoBehaviour
    {
        [SerializeField] AudioClip hover;
        [SerializeField] AudioClip click;
        [SerializeField, Range(0f, 1f)] float volume = 0.6f;

        static UiSounds instance;
        AudioSource source;
        float lastHover;

        void Awake()
        {
            instance = this;
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        void Start()
        {
            foreach (var selectable in FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Hook(selectable);
        }

        /// <summary>
        /// Tugma yoki boshqa boshqaruv elementiga ovoz ulaydi: ustiga kelganda (faol bo'lsa) va tugma bosilganda.
        /// Ikki marta chaqirilsa ham ovoz bir marta chiqadi. Ish vaqtida yaratilgan qatorlar uchun.
        /// </summary>
        public static void Hook(Selectable selectable)
        {
            if (selectable == null)
                return;
            if (!selectable.TryGetComponent(out UiSoundHook hook))
                hook = selectable.gameObject.AddComponent<UiSoundHook>();
            if (hook.ClickHooked || !(selectable is Button button))
                return;
            hook.ClickHooked = true;
            button.onClick.AddListener(PlayClick);
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public static void PlayHover()
        {
            // Tugmalar ustidan tez o'tganda ovozlar ustma-ust tushmasin
            if (instance == null || instance.hover == null || Volume <= 0f || Time.unscaledTime - instance.lastHover < 0.05f)
                return;
            instance.lastHover = Time.unscaledTime;
            instance.source.PlayOneShot(instance.hover, Volume);
        }

        public static void PlayClick()
        {
            if (instance != null && instance.click != null && Volume > 0f)
                instance.source.PlayOneShot(instance.click, Volume);
        }

        /// <summary>Interfeys ovozi balandligi: komponentdagi asosiy qiymat x sozlamalardagi "Interfeys ovozlari".</summary>
        static float Volume => instance == null ? 0f : instance.volume * Mathf.Clamp01(GameSettings.SfxVolume);
    }
}

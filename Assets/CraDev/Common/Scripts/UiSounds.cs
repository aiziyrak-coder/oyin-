using UnityEngine;
using UnityEngine.UI;

namespace CraDev
{
    /// <summary>
    /// Sahnadagi barcha tugmalarga (yashiringan oynalardagilar ham) sichqoncha ustiga kelganda va bosilganda
    /// qisqa ovoz beradi. Sahnada bitta bo'ladi; ovozlarni builder yaratadi (Assets/CraDev/UI/Audio).
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
            {
                if (selectable.GetComponent<UiSoundHook>() == null)
                    selectable.gameObject.AddComponent<UiSoundHook>();
                if (selectable is Button button)
                    button.onClick.AddListener(PlayClick);
            }
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public static void PlayHover()
        {
            // Tugmalar ustidan tez o'tganda ovozlar ustma-ust tushmasin
            if (instance == null || instance.hover == null || Time.unscaledTime - instance.lastHover < 0.05f)
                return;
            instance.lastHover = Time.unscaledTime;
            instance.source.PlayOneShot(instance.hover, instance.volume);
        }

        public static void PlayClick()
        {
            if (instance != null && instance.click != null)
                instance.source.PlayOneShot(instance.click, instance.volume);
        }
    }
}

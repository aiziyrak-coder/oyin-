using UnityEngine;
using UnityEngine.UI;

namespace CraDev.CDCGroup
{
    /// <summary>
    /// Ikkinchi brend: CDCGroup. Zamonaviy, tekis kumush uslub.
    ///
    /// Ketma-ketlik: ingichka kumush chiziq markazdan cho'ziladi -> "CDC" chiziq ortidan
    /// yuqoriga ko'tariladi, "GROUP" pastga tushadi -> qorong'ilashish -> keyingi sahna (Loading).
    /// </summary>
    public class CdcGroupSplash : SplashSequence
    {
        [Header("Elementlar")]
        [SerializeField] Image line;
        [Tooltip("Yuqori niqob (RectMask2D) ichidagi \"CDC\".")]
        [SerializeField] RectTransform letters;
        [Tooltip("Pastki niqob (RectMask2D) ichidagi \"GROUP\".")]
        [SerializeField] RectTransform group;

        [Header("Vaqtlar (soniya)")]
        [SerializeField] float lineStart = 0.2f;
        [SerializeField] float lineDuration = 0.7f;
        [SerializeField] float lettersStart = 0.5f;
        [SerializeField] float lettersDuration = 0.75f;
        [SerializeField] float groupStart = 0.65f;
        [SerializeField] float groupDuration = 0.75f;

        [Header("Ko'rinish")]
        [Tooltip("\"CDC\" chiziq ortidan qancha pastdan ko'tariladi.")]
        [SerializeField] float lettersTravel = 160f;
        [Tooltip("\"GROUP\" chiziq ortidan qancha yuqoridan tushadi.")]
        [SerializeField] float groupTravel = 60f;

        Vector2 lettersFinal;
        Vector2 groupFinal;

        protected override void Awake()
        {
            if (letters != null) lettersFinal = letters.anchoredPosition;
            if (group != null) groupFinal = group.anchoredPosition;
            base.Awake();
        }

        protected override void Apply(float t, float visible)
        {
            float l = Ease.OutQuint(Anim.Progress(t, lineStart, lineDuration));
            if (line != null)
            {
                line.rectTransform.localScale = new Vector3(l, 1f, 1f);
                Anim.SetAlpha(line, Ease.OutCubic(Anim.Progress(t, lineStart, lineDuration * 0.3f)));
            }

            if (letters != null)
            {
                float a = Ease.OutQuint(Anim.Progress(t, lettersStart, lettersDuration));
                letters.anchoredPosition = lettersFinal + new Vector2(0f, -lettersTravel * (1f - a));
            }

            if (group != null)
            {
                float b = Ease.OutQuint(Anim.Progress(t, groupStart, groupDuration));
                group.anchoredPosition = groupFinal + new Vector2(0f, groupTravel * (1f - b));
            }
        }
    }
}

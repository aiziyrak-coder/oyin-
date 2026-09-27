using UnityEngine;
using UnityEngine.UI;

namespace CraDev
{
    /// <summary>Til tugmasi ("UZ" / "EN"): bosilganda o'zbekcha va inglizcha almashadi.</summary>
    [RequireComponent(typeof(Button))]
    public class LanguageToggle : MonoBehaviour
    {
        [SerializeField] Text label;

        void Awake() => GetComponent<Button>().onClick.AddListener(() =>
            Loc.Current = Loc.Current == Language.Uz ? Language.En : Language.Uz);

        void OnEnable()
        {
            Loc.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => Loc.Changed -= Refresh;

        void Refresh() => label.text = Loc.Code;
    }
}

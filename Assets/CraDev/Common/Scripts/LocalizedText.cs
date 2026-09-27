using UnityEngine;
using UnityEngine.UI;

namespace CraDev
{
    /// <summary>Text komponentiga tarjima kaliti: joriy tildagi matnni qo'yadi va til almashsa yangilaydi.</summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] string key;
        [Tooltip("Harflar orasini ochish (\"X U S H\").")]
        [SerializeField] bool spaced;

        Text text;

        public string Key
        {
            get => key;
            set
            {
                key = value;
                Refresh();
            }
        }

        void Awake() => text = GetComponent<Text>();

        void OnEnable()
        {
            Loc.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => Loc.Changed -= Refresh;

        void Refresh()
        {
            if (text == null || string.IsNullOrEmpty(key))
                return;
            string value = Loc.T(key);
            text.text = spaced ? Loc.Spaced(value) : value;
        }
    }
}

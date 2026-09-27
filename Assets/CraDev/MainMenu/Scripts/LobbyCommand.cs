using UnityEngine;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    // Builder yozgan tugma vazifasi sahna faylida saqlanadi.
    [RequireComponent(typeof(Button))]
    public class LobbyCommand : MonoBehaviour
    {
        public string action = "page";
        public string value;
        void Awake() => GetComponent<Button>().onClick.AddListener(Execute);
        void Execute()
        {
            var lobby = FindFirstObjectByType<MainMenuScreen>();
            if (lobby == null) return;
            if (action == "page") lobby.Show(value);
            else if (action == "soon") lobby.Soon(Loc.T(value));
            else if (action == "customize") lobby.Customize();
            else if (action == "choose" && lobby.Current is LobbyContent content) content.Choose(value);
        }
    }
}

using System.Collections;
using CraDev.Online;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    public class LobbyWallet : MonoBehaviour
    {
        [SerializeField] MainMenuScreen lobby;
        [SerializeField] Button button;
        [SerializeField] Text balance;
        [SerializeField] WalletWindow window;
        bool loading;
        WalletResponse data;
        void Start()
        {
            button.onClick.AddListener(() => { window.Show(data); Refresh(); });
            StartCoroutine(Poll());
        }
        IEnumerator Poll() { while (true) { Refresh(); yield return new WaitForSecondsRealtime(30); } }
        public void Refresh() { if (!loading && lobby.Api != null) StartCoroutine(Load()); }
        IEnumerator Load()
        {
            loading = true;
            yield return lobby.Api.Wallet(PlayerProfile.Token, result => {
                if (this == null) return;
                data = result.Ok ? result.Data : null;
                balance.text = data == null ? "— CDCoin  +" : data.balance.ToString("N0") + " CDCoin  +";
                window.Render(data);
            });
            loading = false;
        }
    }
}

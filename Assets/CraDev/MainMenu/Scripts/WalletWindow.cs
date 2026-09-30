using CraDev.Online;
using UnityEngine;
using UnityEngine.UI;
using System.Text;

namespace CraDev.MainMenu
{
    public class WalletWindow : ModalWindow
    {
        [SerializeField] Text summary, history;
        [SerializeField] Button closeButton;
        [SerializeField] Text[] packs;
        WalletResponse data;
        protected override void Awake() { closeButton.onClick.AddListener(Close); base.Awake(); }
        protected override void OnEnable() { base.OnEnable(); Loc.Changed += Redraw; Redraw(); }
        protected override void OnDisable() { Loc.Changed -= Redraw; base.OnDisable(); }
        public void Show(WalletResponse value) { data = value; Open(); Redraw(); }
        public void Render(WalletResponse value) { data = value; if (gameObject.activeInHierarchy) Redraw(); }
        void Redraw()
        {
            summary.text = data == null ? Loc.T("wallet.unavailable") : Loc.F("wallet.balance", data.balance.ToString("N0"));
            for (int i = 0; i < packs.Length; i++)
                packs[i].text = data?.packs != null && i < data.packs.Length
                    ? data.packs[i].coins.ToString("N0") + " CDCoin\n" + data.packs[i].amountUzs.ToString("N0") + " UZS" : "—";
            var text = new StringBuilder(Loc.T("wallet.history"));
            if (data?.transactions == null || data.transactions.Length == 0) text.Append("\n" + Loc.T("wallet.empty"));
            else for (int i = 0; i < Mathf.Min(4, data.transactions.Length); i++)
            {
                var item = data.transactions[i];
                text.Append("\n" + (item.delta > 0 ? "+" : "") + item.delta + " CDCoin · " + Loc.T("wallet.kind." + item.kind));
            }
            history.text = text.ToString();
        }
    }
}

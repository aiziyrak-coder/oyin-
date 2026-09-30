using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CraDev.MainMenu;
using CraDev.Online;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraDev
{
    // Read-only runtime checks: no purchases, social writes, camera or microphone activation.
    public static class DevEconomySmoke
    {
        static void Check(bool ok,string text) { Debug.Log("[EconomyTest] "+(ok?"PASS: ":"FAIL: ")+text); }
        public static IEnumerator Run(MainMenuScreen lobby)
        {
            yield return new WaitForSecondsRealtime(2);
            var panel=lobby.FriendsPanel;
            panel.Choose("find");
            yield return new WaitForSecondsRealtime(.4f);
            var input=panel.Search;
            var rect=(RectTransform)input.transform;
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Check(hits.Count>0&&hits[0].gameObject.transform.IsChildOf(input.transform),"search pointer target: "+(hits.Count>0?hits[0].gameObject.name:"none"));
            ExecuteEvents.Execute(input.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            input.ActivateInputField();yield return null;
            Check(input.isFocused,"search accepts focus");
            input.ProcessEvent(Event.KeyboardEvent("1"));input.ProcessEvent(Event.KeyboardEvent("2"));
            Check(input.text=="12","search accepts keyboard characters");
            input.text="";yield return new WaitForSecondsRealtime(.5f);
            Check(panel.VisibleCount==0,"empty query never lists users");
            input.text="999999";yield return new WaitForSecondsRealtime(1);
            Check(panel.Mode=="find","numeric ID search remains in search mode");
            input.text="";input.DeactivateInputField();panel.Choose("friends");
            yield return lobby.Api.Wallet(PlayerProfile.Token,r=>Check(r.Ok&&r.Data.coinUzs==100&&!r.Data.checkoutAvailable,"wallet API, exchange rate and disabled checkout"));
            for(int i=0;i<4;i++)
                yield return lobby.Api.Ping((ok,ms)=>Debug.Log("[EconomyTest] PING "+(ok?ms.ToString():"failed")+" ms"));
            var button=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name=="LobbyCDCoin");
            Check(button!=null,"lobby balance button exists");
            if(button!=null)
            {
                button.onClick.Invoke();yield return new WaitForSecondsRealtime(.5f);
                Check(ModalWindow.AnyOpen,"wallet opens as lobby modal");
                var wallet=Object.FindFirstObjectByType<WalletWindow>();
                Check(wallet!=null&&wallet.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("500 CDCoin")),"coin packs render");
                if(wallet!=null)wallet.Close();yield return new WaitForSecondsRealtime(.3f);
                Check(!ModalWindow.AnyOpen,"wallet closes without leaving lobby");
            }
            Debug.Log("[EconomyTest] COMPLETE");
        }
    }
}

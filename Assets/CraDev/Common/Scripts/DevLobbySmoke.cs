using System;
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
    // Faqat -cradevUiSmoke bayrog'ida. Profilni saqlamaydi yoki serverga test ma'lumot yubormaydi.
    public static class DevLobbySmoke
    {
        static int failures;
        static void Check(bool ok,string name)
        {
            if(ok) Debug.Log("[LobbyTest] PASS: "+name);
            else {failures++;Debug.LogError("[LobbyTest] FAIL: "+name);}
        }
        static Button Find(string name) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name==name&&b.isActiveAndEnabled);
        static void Click(Button button)
        {
            Check(button!=null,"button exists");
            if(button==null)return;
            var rect=(RectTransform)button.transform;
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Check(hits.Count>0&&(hits[0].gameObject.transform==button.transform||hits[0].gameObject.transform.IsChildOf(button.transform)),"raycast "+button.name);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        public static IEnumerator Run(MainMenuScreen lobby)
        {
            failures=0;
            string outfit=PlayerProfile.Outfit;
            var language=Loc.Current;
            foreach(string id in new[]{"home","world","friends","top","settings"})
            {
                Click(Find("Button_nav."+id));
                yield return new WaitForSecondsRealtime(.5f);
                Check(lobby.Current!=null&&lobby.Current.Id==id,"navigation "+id);
            }
            foreach(var pair in new[]{("settings",8),("shops",8),("education",8),("business",7),("entertainment",6),("world",8)})
            {
                lobby.Show(pair.Item1);yield return new WaitForSecondsRealtime(.5f);
                var page=(LobbyContent)lobby.Current;
                for(int i=0;i<pair.Item2;i++){page.Choose(i.ToString());yield return null;}
                page.Choose("0");
                Check(page.Id==pair.Item1,"categories "+pair.Item1);
            }
            lobby.Show("wardrobe");yield return new WaitForSecondsRealtime(1);
            var garment=UnityEngine.Object.FindObjectsByType<RawImage>(FindObjectsSortMode.None).FirstOrDefault(i=>i.name=="Garment");
            Check(garment!=null&&garment.texture!=null,"3D garment thumbnails");
            if(garment!=null)
            {
                var button=garment.GetComponentInParent<Button>();
                button.onClick.Invoke();yield return null;
                Check(!lobby.Viewer.Outfit.SameAs(Wardrobe.Outfit.FromJson(outfit)),"outfit preview");
                lobby.Show("home");yield return new WaitForSecondsRealtime(.3f);
                Check(lobby.Current.Id=="wardrobe"&&ModalWindow.AnyOpen,"unsaved outfit confirmation");
                var discard=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>
                    b.GetComponentInChildren<Text>()?.text==Loc.T("wardrobe.discard"));
                Check(discard!=null,"discard control");
                if(discard!=null) discard.onClick.Invoke();
                yield return new WaitForSecondsRealtime(.5f);
                Check(PlayerProfile.Outfit==outfit,"preview did not change saved outfit");
            }
            Loc.Current=language==Language.Uz?Language.En:Language.Uz;
            yield return null;
            Check(!Loc.T("nav.settings").Contains("."),"language table");
            Loc.Current=language;
            lobby.Show("home");
            yield return new WaitForSecondsRealtime(.5f);
            Debug.Log("[LobbyTest] COMPLETE: "+failures+" failures");
        }
    }
}

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
            lobby.Show("shops");yield return new WaitForSecondsRealtime(.5f);
            foreach(var testLanguage in new[]{Language.Uz,Language.En})
            {
                Loc.Current=testLanguage;yield return null;
                var page=(LobbyContent)lobby.Current;
                var labels=page.GetComponentsInChildren<Text>().Select(t=>t.text).ToArray();
                Check(labels.Contains(Loc.T("shops.store.clothing")),"generic store labels "+testLanguage);
                string[] removed={"AUREL","VELO","ATLAS","NEXORA","ORBIT","MIRA","ÉLAN","PULSE","DOMA","LUMA","STRIDE","NOVA"};
                Check(!labels.Any(t=>removed.Contains(t)),"no store brands "+testLanguage);
                var search=page.GetComponentInChildren<InputField>();
                search.text=Loc.T("shops.store.computers");yield return new WaitForSecondsRealtime(.4f);
                var cards=page.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Item_")).ToArray();
                Check(cards.Length==1&&cards[0].GetComponentInChildren<Text>().text==Loc.T("shops.store.computers"),"generic store search "+testLanguage);
                search.text="";yield return new WaitForSecondsRealtime(.4f);
            }
            Loc.Current=language;
            lobby.Show("settings");yield return new WaitForSecondsRealtime(.5f);
            ((LobbyContent)lobby.Current).Choose("4");yield return null;
            var switches=lobby.Current.GetComponentsInChildren<Image>().Where(g=>g.name=="Switch").ToArray();
            Check(switches.Length==2&&switches.All(g=>g.material.shader.name=="UI/Default"),"flat notification switches");
            var images=lobby.GetComponentsInChildren<Image>(true);
            Check(!images.Any(g=>g.GetComponent<GlassSurface>()!=null || g.material.shader.name=="CraDev/UI/LobbyGlass"),"no glass or grab-pass surfaces");
            Check(lobby.GetComponentsInChildren<Text>(true).Count(t=>t.text=="NewWorld")==2,"NewWorld platform branding");
            lobby.Show("wardrobe");yield return new WaitForSecondsRealtime(1);
            var garment=UnityEngine.Object.FindObjectsByType<RawImage>(FindObjectsSortMode.None).FirstOrDefault(i=>i.name=="Garment");
            Check(garment!=null&&garment.texture!=null,"3D garment thumbnails");
            Check(garment!=null&&garment.texture is RenderTexture,"GPU-only garment previews");
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
                Check(lobby.Viewer.Outfit.SameAs(Wardrobe.Outfit.FromJson(outfit)),"discard restored visible outfit");
            }
            Loc.Current=language==Language.Uz?Language.En:Language.Uz;
            yield return null;
            Check(!Loc.T("nav.settings").Contains("."),"language table");
            Loc.Current=language;
            lobby.Show("home");
            yield return new WaitForSecondsRealtime(.5f);
            lobby.Show("world");lobby.Show("home");
            yield return new WaitForSecondsRealtime(.5f);
            Check(lobby.Current.Id=="home","rapid navigation cancels pending page");
            Debug.Log("[LobbyTest] COMPLETE: "+failures+" failures");
        }

        public static IEnumerator PreviewRegression(MainMenuScreen lobby)
        {
            var original=lobby.Viewer.CurrentOption;
            var outfit=lobby.Viewer.Outfit;
            string saved=PlayerProfile.Outfit, nickname=PlayerProfile.Nickname;
            int failed=0;
            lobby.Show("home");yield return new WaitForSecondsRealtime(.5f);
            try
            {
                lobby.Stage.Apply(1);
                foreach(string id in new[]{"M1","M2","M3","M5","F1","F2","F3","F4","F5"})
                {
                    var option=lobby.FindAvatar(id);
                    if(option==null){failed++;continue;}
                    lobby.Viewer.SetAvatar(option);yield return null;
                    var model=GameObject.Find(id);
                    var materials=model.GetComponentsInChildren<Renderer>().SelectMany(r=>r.materials).ToArray();
                    var textures=materials.Select(m=>m.mainTexture).ToArray();
                    bool valid=true;
                    foreach(Wardrobe.OutfitSlot slot in Enum.GetValues(typeof(Wardrobe.OutfitSlot)))
                    {
                        var item=Wardrobe.WardrobeCatalog.For(slot).First();
                        var result=lobby.Viewer.CaptureGarment(item);
                        var active=RenderTexture.active;
                        Texture2D pixels=null;
                        try
                        {
                            RenderTexture.active=result;
                            pixels=new Texture2D(192,192,TextureFormat.RGBA32,false);
                            // Readback is only in this explicit regression test, never normal navigation.
                            pixels.ReadPixels(new Rect(0,0,192,192),0,0);pixels.Apply();
                            valid &= pixels.GetPixels32().Count(p=>p.a>32&&(p.r+p.g+p.b)>30)>100;
                            valid &= materials.Select((m,i)=>m.mainTexture==textures[i]).All(s=>s);
                            valid &= lobby.Viewer.Outfit.SameAs(outfit);
                        }
                        finally
                        {
                            RenderTexture.active=active;
                            if(pixels!=null)UnityEngine.Object.Destroy(pixels);
                            if(result!=null){result.Release();UnityEngine.Object.Destroy(result);}
                        }
                        yield return null;
                    }
                    if(!valid)failed++;
                    Debug.Log($"[PreviewTest] {id}: {(valid?"PASS":"FAIL")} four slots, material and outfit unchanged");
                }
            }
            finally
            {
                lobby.Viewer.SetAvatar(original);lobby.Viewer.SetOutfit(outfit);
                lobby.Stage.Apply(lobby.Current.StagePose);
            }
            if(saved!=PlayerProfile.Outfit || nickname!=PlayerProfile.Nickname)failed++;
            Debug.Log($"[PreviewTest] COMPLETE: {failed} failures; profile preserved");
        }
    }
}

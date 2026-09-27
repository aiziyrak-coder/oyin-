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
    public static class DevSingleLobbySmoke
    {
        static int failures,checks;
        static void Check(bool ok,string text){checks++;if(!ok)failures++;Debug.Log($"[SingleLobbyTest] {(ok?"PASS":"FAIL")}: {text}");}
        static Button Find(string name)=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name==name&&b.isActiveAndEnabled);
        static void Click(string name)
        {
            var button=Find(name);Check(button!=null,"control "+name);if(button==null)return;
            var rect=(RectTransform)button.transform;
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Check(hits.Count>0&&hits[0].gameObject.transform.IsChildOf(button.transform),"raycast "+name);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        static IEnumerator Loaded(LobbyFriendsPanel panel)
        {
            float until=Time.realtimeSinceStartup+8;
            while(panel.Loading&&Time.realtimeSinceStartup<until)yield return null;
            Check(!panel.Loading,"friend request completed");
        }
        public static IEnumerator Run(MainMenuScreen lobby)
        {
            failures=checks=0;
            var language=Loc.Current;string outfit=PlayerProfile.Outfit,nickname=PlayerProfile.Nickname,avatar=PlayerProfile.AvatarId;
            Check(lobby.SingleWindow&&lobby.Current.Id=="home","one-screen mode");
            var pages=lobby.GetComponentsInChildren<LobbyPage>(true);
            Check(pages.Length==10,"all ten original pages preserved");
            Check(pages.Count(p=>p.gameObject.activeSelf)==1,"only home visible");
            Check(!lobby.GetComponentsInChildren<Transform>().Any(t=>t.name=="Navigation"||t.name=="ExploreDock"||t.name=="NavigationDock"),"no website navigation or dock");
            var profileRect=(RectTransform)Find("LobbyProfile").transform;
            var entryRect=(RectTransform)Find("EnterNewWorld").transform;
            var eventRect=(RectTransform)lobby.Current.transform.Find("LobbyRightRail/LobbyEvent");
            Check(profileRect.parent==entryRect.parent&&eventRect.parent==entryRect.parent&&profileRect.rect.width==entryRect.rect.width&&eventRect.rect.width==entryRect.rect.width&&profileRect.anchoredPosition.x==eventRect.anchoredPosition.x,"right cards share one aligned column");
            Check(Mathf.Abs(entryRect.anchoredPosition.y-entryRect.rect.height-64)<1,"entry button keeps bottom safe margin");
            Check(Find("EnterNewWorld").GetComponent<Image>().color==LobbyPalette.Accent,"entry uses shared vivid blue");
            foreach(string id in new[]{"world","wardrobe","shops","education","business","friends","top","entertainment"})
            {lobby.Show(id);yield return null;Check(lobby.Current.Id=="home"&&!pages.First(p=>p.Id==id).gameObject.activeSelf,"archived route blocked "+id);}
            var friends=lobby.FriendsPanel;yield return Loaded(friends);
            var dock=(RectTransform)friends.transform;
            Check(dock.anchorMin==new Vector2(0,0)&&dock.anchorMax==new Vector2(0,1)&&dock.anchoredPosition==Vector2.zero&&Mathf.Abs(dock.rect.height-((RectTransform)dock.parent).rect.height)<1,"friends dock flush left and full height");
            yield return null;Canvas.ForceUpdateCanvases();
            var names=friends.GetComponentsInChildren<Text>().Where(t=>t.name=="FriendNickname").ToArray();
            Check(names.All(t=>!string.IsNullOrEmpty(t.text)&&t.cachedTextGenerator.vertexCount>0),"friend names render inside rows");
            Click("FriendTool_online");yield return Loaded(friends);
            Check(friends.Mode=="online"&&friends.VisibleAreOnline,"online filter stays in lobby");
            Click("FriendTool_online");yield return Loaded(friends);
            Click("FriendTool_find");yield return Loaded(friends);
            friends.Search.text="NoSuchPlayer74921";yield return new WaitForSecondsRealtime(.4f);yield return Loaded(friends);
            Check(friends.Mode=="find"&&friends.VisibleCount==0&&lobby.Current.Id=="home","search empty result inline");
            friends.Search.text="Lynxos";yield return new WaitForSecondsRealtime(.4f);yield return Loaded(friends);
            Check(lobby.Current.Id=="home","search never changes page");
            Click("FriendTool_remove");yield return Loaded(friends);
            var remove=Find("Remove");
            if(remove!=null)
            {
                remove.onClick.Invoke();yield return null;Check(ModalWindow.AnyOpen,"remove requires confirmation");lobby.Dialog.Close();
            }
            Click("FriendTool_requests");yield return Loaded(friends);
            Check(friends.Mode=="requests"&&lobby.Current.Id=="home","requests inline");
            Click("FriendTool_requests");yield return Loaded(friends);
            Click("LobbySettings");yield return null;
            Check(lobby.SettingsOpen&&lobby.Current.Id=="home"&&lobby.Stage.Current==0,"settings overlay keeps lobby and avatar");
            Check(!lobby.Current.Group.blocksRaycasts,"modal blocks underlying lobby");
            lobby.ChooseSection("2");yield return null;
            Check(pages.First(p=>p.Id=="settings").GetComponentsInChildren<Image>().Count(i=>i.name=="Switch")==3,"settings sections still work");
            Click("SettingsClose");yield return null;
            Check(!lobby.SettingsOpen&&lobby.Current.Group.blocksRaycasts,"settings close restores controls");
            Click("EnterNewWorld");yield return null;
            Check(lobby.Current.Id=="home","unconnected world does not open archived map");
            Loc.Current=language==Language.Uz?Language.En:Language.Uz;yield return null;
            Check(Find("EnterNewWorld").GetComponentInChildren<Text>().text==Loc.T("lobby.enter"),"localized entry button");
            Loc.Current=language;
            Check(PlayerProfile.Outfit==outfit&&PlayerProfile.Nickname==nickname&&PlayerProfile.AvatarId==avatar,"saved profile unchanged");
            Check(!lobby.GetComponentsInChildren<LobbyCommand>().Any(c=>c.action=="page"&&c.value!="settings"),"no hidden-page shortcuts");
            Debug.Log($"[SingleLobbyTest] COMPLETE: {checks} checks, {failures} failures");
        }
    }
}

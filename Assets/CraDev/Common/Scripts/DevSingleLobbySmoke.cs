using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CraDev.Face;
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
        // Avatar studiyasi: lobbydan chiqmaydi, qoralama party signalida saqlanadi, Esc avval skanerni yopadi,
        // bekor qilish saqlangan avatar va yuzni qaytaradi. Profil va yuz fayliga hech narsa yozilmaydi.
        static IEnumerator AvatarStudio(MainMenuScreen lobby,string avatar)
        {
            var viewer=lobby.Viewer;var savedFace=viewer.Face;
            Click("LobbyAvatar");yield return new WaitForSecondsRealtime(.5f);
            var studio=lobby.AvatarStudio;
            Check(studio!=null&&lobby.AvatarStudioOpen&&lobby.Current.Id=="home"&&!lobby.Current.Group.blocksRaycasts,"avatar studio opens inside the lobby");
            if(studio==null||!lobby.AvatarStudioOpen)yield break;
            Check(lobby.Stage.StudioActive&&!viewer.LockRotation,"studio frames the avatar and allows rotation");
            Check(studio.Choices.Count>0&&studio.Choices.All(a=>a.gender==viewer.CurrentOption.gender),"studio lists avatars of the player's gender");
            var other=studio.Choices.FirstOrDefault(a=>a.id!=avatar);
            if(other!=null)
            {
                Click("StudioAvatar_"+studio.Choices.ToList().IndexOf(other));yield return null;
                Check(viewer.CurrentOption==other&&studio.Dirty,"draft avatar previewed on the lobby character");
                lobby.GetComponent<LobbyParty>()?.Refresh();yield return null;
                Check(viewer.CurrentOption==other,"party heartbeat keeps the draft avatar");
            }
            if(savedFace!=null){Click("StudioRemoveFace");yield return null;Check(viewer.Face==null&&studio.Dirty,"face removal previewed");}
            Click("StudioScan");yield return new WaitForSecondsRealtime(.5f);
            Check(studio.ScanOpen,"live scanner opens inside the studio");
            var camera=studio.Scanner.CameraState;
            if(camera==FaceScanner.CameraState.NoDevice||camera==FaceScanner.CameraState.Failed)Check(studio.Scanner.FailureShown,"camera problem explained with retry");
            lobby.Back();yield return null;
            Check(!studio.ScanOpen&&lobby.AvatarStudioOpen,"Esc closes the scanner first");
            lobby.Back();yield return null;
            if(studio.Dirty)
            {
                Check(ModalWindow.AnyOpen,"unsaved studio asks before discarding");
                var buttons=lobby.Dialog.GetComponentsInChildren<Button>();buttons[buttons.Length-1].onClick.Invoke();yield return null;
            }
            Check(!lobby.AvatarStudioOpen&&viewer.CurrentOption.id==avatar&&viewer.Face==savedFace,"cancel restores the saved avatar and face");
            yield return new WaitForSecondsRealtime(.6f);
            Check(lobby.Current.Group.blocksRaycasts&&!lobby.Stage.StudioActive&&viewer.LockRotation,"lobby controls and camera restored");
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
            Check(profileRect.parent==entryRect.parent&&eventRect.parent==entryRect.parent&&entryRect.rect.width==650,"reference right column and compact profile");
            Check(entryRect.anchorMin==Vector2.zero&&Mathf.Abs(entryRect.anchoredPosition.y-259)<1,"compact entry stays bottom anchored");
            Check(Vector2.Distance(lobby.GetComponent<CanvasScaler>().referenceResolution,new Vector2(1920,1080)/.65f)<1,"all interface elements scaled to 65 percent");
            Check(Find("EnterNewWorld").GetComponent<ReferenceSurface>()?.Style==1,"entry uses reference emerald gold landscape surface");
            Check(Find("EnterNewWorld").transform.Find("PlayOrb")?.GetComponent<ReferenceSurface>()?.Style==2,"entry has separate illuminated play orb");
            Check(lobby.Current.transform.Find("LobbyRightRail/LobbyPromo").GetComponent<ReferenceSurface>()?.Style==3,"promo uses edge-to-edge landscape with dark text scrim");
            foreach(string id in new[]{"world","wardrobe","shops","education","business","friends","top","entertainment"})
            {lobby.Show(id);yield return null;Check(lobby.Current.Id=="home"&&!pages.First(p=>p.Id==id).gameObject.activeSelf,"archived route blocked "+id);}
            var friends=lobby.FriendsPanel;yield return Loaded(friends);
            var dock=(RectTransform)friends.transform;
            Check(dock.anchoredPosition==new Vector2(36,-124)&&dock.rect.width==492&&Mathf.Abs(dock.rect.height-((RectTransform)dock.parent).rect.height+148)<1,"friends panel stretches to bottom margin");
            var drawer=lobby.Current.GetComponent<LobbyFriendsDrawer>();
            Click("FriendsDrawerToggle");yield return new WaitForSecondsRealtime(.3f);
            Check(drawer.Collapsed&&dock.anchoredPosition.x<=-dock.rect.width&&!dock.GetComponent<CanvasGroup>().blocksRaycasts,"drawer hidden and noninteractive");
            Click("FriendsDrawerToggle");yield return new WaitForSecondsRealtime(.3f);
            Check(!drawer.Collapsed&&Mathf.Abs(dock.anchoredPosition.x-36)<1&&dock.GetComponent<CanvasGroup>().blocksRaycasts,"drawer reopened with live controls");
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
            friends.Search.Select();friends.Search.ActivateInputField();yield return null;
            lobby.Back();yield return null;
            Check(!ModalWindow.AnyOpen&&EventSystem.current.currentSelectedGameObject==null,"Esc in search only leaves the field");
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
            var settingsPage=pages.First(p=>p.Id=="settings");
            var editAvatar=Find("EditAvatar");
            Check(editAvatar!=null&&editAvatar.interactable,"avatar and face editing reachable from settings");
            lobby.ChooseSection("3");yield return null;
            Check(settingsPage.GetComponentsInChildren<Image>().Count(i=>i.name=="Switch")==2,"settings sections still work");
            Check(!settingsPage.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("Keys_")),"key list is read-only");
            var sensitivity=settingsPage.GetComponentsInChildren<Slider>().FirstOrDefault(s=>s.name=="Sensitivity");
            float savedSensitivity=World.WorldPreferences.Sensitivity;
            if(sensitivity!=null)sensitivity.value=savedSensitivity>2?1.5f:2.5f;
            Check(sensitivity!=null&&Mathf.Abs(World.WorldPreferences.Sensitivity-sensitivity.value)<.01f,"lobby sensitivity drives world preferences");
            World.WorldPreferences.Sensitivity=savedSensitivity;World.WorldPreferences.Save();
            lobby.ChooseSection("2");yield return null;
            var volume=settingsPage.GetComponentsInChildren<Slider>().FirstOrDefault(s=>s.name=="MasterVolume");
            float savedVolume=GameSettings.Volume;
            if(volume!=null){volume.value=1f;volume.value=1.5f;}
            Check(volume!=null&&Mathf.Approximately(GameSettings.Volume,1f),"volume clamps instead of wrapping");
            GameSettings.Volume=savedVolume;GameSettings.Apply();GameSettings.Save();
            lobby.ChooseSection("1");yield return null;
            var smaller=Find("Resolution_Prev");var larger=Find("Resolution_Next");
            Check(smaller!=null&&larger!=null&&(GameSettings.DisplayMode!=FullScreenMode.FullScreenWindow||(!smaller.interactable&&!larger.interactable)),"resolution fixed in borderless");
            lobby.ChooseSection("4");yield return null;
            Check(Find("LobbyWeather_0")!=null&&Find("LobbyWeather_3")!=null,"weather segments");
            Click("SettingsClose");yield return null;
            Check(!lobby.SettingsOpen&&lobby.Current.Group.blocksRaycasts,"settings close restores controls");
            yield return AvatarStudio(lobby,avatar);
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

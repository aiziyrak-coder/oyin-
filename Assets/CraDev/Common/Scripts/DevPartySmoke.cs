using System.Collections;
using System.Linq;
using CraDev.MainMenu;
using CraDev.Online;
using UnityEngine;

namespace CraDev
{
    // Faqat maxsus buyruq bilan: profilga/bazaga hech narsa yozmaydigan vizual tekshiruv.
    public static class DevPartySmoke
    {
        public static IEnumerator Run(MainMenuScreen lobby,string folder)
        {
            var party=lobby.GetComponent<LobbyParty>();
            yield return new WaitForSecondsRealtime(4);
            party.StopAllCoroutines();
            int failures=0;
            for(int seed=0;seed<200;seed++)
            {
                var positions=LobbyParty.Formation("test-room-"+seed);
                var again=LobbyParty.Formation("test-room-"+seed);
                if(positions.Max(p=>p.z)-positions.Min(p=>p.z)<1)failures++;
                for(int i=0;i<5;i++)
                {
                    if(positions[i]!=again[i])failures++;
                    for(int j=i+1;j<5;j++)if(Mathf.Abs(positions[i].x-positions[j].x)<.77f)failures++;
                }
            }
            var toast=lobby.transform.Find("Toast").GetComponent<RectTransform>();
            if(toast.anchorMin!=new Vector2(.5f,1)||toast.anchoredPosition.y>0||toast.anchoredPosition.y < -100)failures++;
            party.Apply(new PartyState{host=true,members=new[]{new PartyMember{nickname=PlayerProfile.Nickname,avatarId=PlayerProfile.AvatarId,seat=0,pingMs=30,online=true}}});
            // Profile refresh is allowed to restore the real player's avatar. Test animation on an
            // isolated replica so a periodic profile update cannot destroy the bones being sampled.
            var sample=lobby.Viewer.Replica(null,lobby.FindAvatar(PlayerProfile.AvatarId));
            foreach(string id in new[]{"M1","M2","M3","M5","F1","F2","F3","F4","F5"})
            {
                // O'z qahramonimiz avatari mahalliy profildan olinadi (party emas): shuning uchun to'g'ridan-to'g'ri qo'yiladi
                var option=lobby.FindAvatar(id);if(option==null){failures++;continue;}
                sample.SetAvatar(option);
                yield return new WaitForSecondsRealtime(.1f);
                var idle=sample.CurrentModel.GetComponent<Animator>();
                if(idle==null||!idle.enabled||idle.runtimeAnimatorController==null){failures++;continue;}
                var bones=sample.CurrentModel.GetComponentsInChildren<Transform>();
                var head=bones.First(b=>b.name=="Bip01 Head");
                var before=head.position;
                yield return new WaitForSecondsRealtime(.7f);
                if(Vector3.Distance(before,head.position)<.00001f)failures++;
                foreach(var b in bones.Where(b=>b.name.EndsWith(" Foot")))if(b.position.y < -.1f||b.position.y > .25f)failures++;
                Debug.Log($"[PartyTest] Standing {id}: head={head.position.y:F2}, animated={Vector3.Distance(before,head.position):F4}");
            }
            Object.Destroy(sample.gameObject);yield return null;
            var members=new PartyMember[5];string[] avatars={PlayerProfile.AvatarId,"M2","F1","M3","F3"};
            for(int i=0;i<5;i++)members[i]=new PartyMember{nickname=i==0?PlayerProfile.Nickname:"TEST_"+i,avatarId=avatars[i],seat=i,online=true,pingMs=i*80+25};
            party.Apply(new PartyState{host=true,members=members});
            yield return new WaitForSecondsRealtime(1);
            if(party.OccupiedCount!=5)failures++;
            var viewers=Object.FindObjectsByType<CraDev.CharacterCreation.AvatarViewer>(FindObjectsSortMode.None).Where(v=>v.CurrentModel!=null).ToArray();
            foreach(var v in viewers)if(v.ModelRoot.localScale!=Vector3.one)failures++;
            for(int i=0;i<viewers.Length;i++)for(int j=i+1;j<viewers.Length;j++)
            {
                var camera=lobby.Stage.Camera;
                var a=viewers[i].ModelRoot.position;var b=viewers[j].ModelRoot.position;
                float screenA=camera.WorldToScreenPoint(a).x,screenB=camera.WorldToScreenPoint(b).x;
                float radiusA=Mathf.Abs(camera.WorldToScreenPoint(a+Vector3.right*.18f).x-screenA);
                float radiusB=Mathf.Abs(camera.WorldToScreenPoint(b+Vector3.right*.18f).x-screenB);
                // A fixed 10% of the entire display incorrectly fails compact side-panel layouts.
                // Verify separation against the avatars' projected body cores and actual world space.
                if(Mathf.Abs(screenA-screenB)<radiusA+radiusB||Vector3.Distance(a,b)<.5f)
                {failures++;Debug.LogError("[PartyTest] Avatar body cores overlap");}
            }
            foreach(var plate in lobby.GetComponentsInChildren<RectTransform>().Where(r=>r.name.StartsWith("Nameplate_")))
            {
                var screen=RectTransformUtility.WorldToScreenPoint(null,plate.position);
                if(screen.x<0||screen.x>Screen.width||screen.y<0||screen.y>Screen.height)failures++;
                // Nomlar chap panel (ochiq) va o'ng ustun orasida: panellar ostida qolmaydi
                var drawer=lobby.Current.GetComponent<LobbyFriendsDrawer>();var rail=lobby.Current.transform.Find("LobbyRightRail") as RectTransform;
                if(drawer!=null&&rail!=null)
                {
                    var corners=new Vector3[4];rail.GetWorldCorners(corners);float scale=lobby.GetComponent<Canvas>().scaleFactor;
                    if(screen.x<drawer.ExpandedRight*scale||screen.x>corners[0].x){failures++;Debug.LogError($"[PartyTest] Nameplate under side panel: {plate.name} x={screen.x:F0}");}
                }
            }
            // "Lobbydan chiqish" yuqori o'ng boshqaruvda (builder); eski sahnada ish vaqtida yaratilgan zaxira tugma
            var leave=lobby.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="LobbyLeaveParty")??lobby.transform.Find("LeaveParty");
            if(leave==null||!leave.gameObject.activeInHierarchy||(leave.parent==lobby.transform&&leave.GetSiblingIndex()>lobby.Dialog.transform.GetSiblingIndex())){failures++;Debug.LogError("[PartyTest] Leave button missing or drawn above dialogs");}
            if(GameObject.Find("Sofa")!=null||GameObject.Find("ArmchairLeft")!=null||GameObject.Find("LobbySeating")!=null)failures++;
            foreach(var viewer in Object.FindObjectsByType<CraDev.CharacterCreation.AvatarViewer>(FindObjectsSortMode.None))
                if(viewer.CurrentModel!=null&&viewer.CurrentModel.GetComponent<Animator>().runtimeAnimatorController==null)failures++;
            lobby.Toast(Loc.T("party.joined"));
            yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();
            System.IO.Directory.CreateDirectory(folder);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"five-standing.png"));
            yield return new WaitForSecondsRealtime(2);
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"five-standing-idle.png"));
            // Avatar studiyasi: boshqa a'zolar va nom yorliqlari yashiriladi, yopilganda qaytadi (profilga yozilmaydi)
            lobby.SetAvatarStudio(true);
            yield return new WaitForSecondsRealtime(.8f);
            if(!lobby.AvatarStudioOpen||GameObject.Find("StandingParty")!=null)failures++;
            if(lobby.GetComponentsInChildren<RectTransform>().Any(r=>r.name.StartsWith("Nameplate_")))failures++;
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"avatar-studio.png"));
            lobby.SetAvatarStudio(false);
            yield return new WaitForSecondsRealtime(.8f);
            if(lobby.AvatarStudioOpen||GameObject.Find("StandingParty")==null||party.OccupiedCount!=5)failures++;
            Debug.Log($"[PartyTest] 9 standing avatars, 5 members, no furniture, top toast, studio hides party: failures={failures}");
            party.Apply(new PartyState{host=true,members=new[]{new PartyMember{nickname=PlayerProfile.Nickname,avatarId=PlayerProfile.AvatarId,outfit=PlayerProfile.Outfit,seat=0,pingMs=-1}}});
            yield return new WaitForSecondsRealtime(.5f);
        }
    }
}

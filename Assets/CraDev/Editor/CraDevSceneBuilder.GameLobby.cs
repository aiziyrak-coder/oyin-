using CraDev.MainMenu;
using UnityEngine;
using UnityEngine.UI;
using static CraDev.EditorTools.UiBuild;

namespace CraDev.EditorTools
{
    static partial class CraDevSceneBuilder
    {
        // Single-screen game lobby. The old V2Home/V2Navigation builders remain archived.
        static void V2GameHome(Transform root,LobbyContent page)
        {
            V2Text(root,"NewWorld",534,58,420,68,43,false).font=v2.SemiBold;
            V2Text(root,"lobby.caption",537,128,410,34,21).color=new Color32(167,185,199,255);

            var panel=V2Panel(root,"LobbyFriends",0,0,470,1080,new Color(.045f,.06f,.08f,.96f));
            panel.sprite=null;panel.type=Image.Type.Simple;
            panel.rectTransform.anchorMin=new Vector2(0,0);
            panel.rectTransform.anchorMax=new Vector2(0,1);
            panel.rectTransform.anchoredPosition=Vector2.zero;
            panel.rectTransform.sizeDelta=new Vector2(470,0);
            var friends=panel.gameObject.AddComponent<LobbyFriendsPanel>();
            Set(friends,"font",v2.Medium);Set(friends,"rounded",v2.RoundFill);
            Set(friends,"addIcon",LineIcon("UserPlus"));Set(friends,"removeIcon",LineIcon("Minus"));Set(friends,"acceptIcon",LineIcon("Check"));
            V2Text(panel.transform,"friends.tab.friends",24,20,270,44,28).font=v2.SemiBold;
            Set(friends,"countLabel",V2Text(panel.transform,"",24,65,420,34,21,false));
            string[] modes={"find","online","remove","requests","refresh"};
            string[] icons={"UserPlus","Users","Minus","Bell","Refresh"};
            var buttons=new Button[5];
            var hint=V2Text(panel.transform,"lobby.friends_hint",24,175,420,32,19);
            for(int i=0;i<5;i++)
            {
                buttons[i]=V2Button(panel.transform,"",icons[i],24+i*84,112,66,52);
                buttons[i].name="FriendTool_"+modes[i];
                var tooltip=buttons[i].gameObject.AddComponent<LobbyIconHint>();
                Set(tooltip,"label",hint);Set(tooltip,"key","lobby.tool."+modes[i]);Set(tooltip,"fallback","lobby.friends_hint");
            }
            SetArray(friends,"tools",buttons);
            var field=V2Search(panel.transform,"friends.search",24,216,422);
            Set(friends,"search",field);
            var viewport=CreateRect("FriendViewport",panel.transform);PlaceTopLeft(viewport,24,292,422,764);
            viewport.anchorMin=new Vector2(0,0);viewport.anchorMax=new Vector2(0,1);
            viewport.sizeDelta=new Vector2(422,-316);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;
            scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
            var rows=CreateRect("FriendRows",viewport);PlaceTopLeft(rows,0,0,422,764);scroll.content=rows;
            Set(friends,"rows",rows);

            // One anchored column, not independently floating cards.
            var right=CreateRect("LobbyRightRail",root);
            right.anchorMin=new Vector2(1,0);right.anchorMax=new Vector2(1,1);right.pivot=new Vector2(1,1);
            right.anchoredPosition=new Vector2(-64,0);right.sizeDelta=new Vector2(384,0);
            var eventCard=V2Panel(right,"LobbyEvent",0,264,384,126,V2Glass);
            V2Icon(eventCard.transform,"Calendar",24,22,22).color=new Color32(146,177,241,255);
            V2Text(eventCard.transform,"home.event_next",58,13,302,40,20).color=new Color32(169,183,204,255);
            Set(page,"eventTitle",V2Text(eventCard.transform,"",24,60,336,46,23,false));
            var entryHint=V2Text(right,"lobby.enter_hint",0,0,384,36,20);
            entryHint.color=new Color32(194,207,224,255);
            entryHint.rectTransform.anchorMin=entryHint.rectTransform.anchorMax=Vector2.zero;
            entryHint.rectTransform.anchoredPosition=new Vector2(0,184);
            var enter=V2Button(right,"lobby.enter","Play",0,0,384,68,"enter-world",blue:true);
            enter.name="EnterNewWorld";
            var entryRect=(RectTransform)enter.transform;
            entryRect.anchorMin=entryRect.anchorMax=Vector2.zero;entryRect.anchoredPosition=new Vector2(0,132);
            var entryLabel=enter.GetComponentInChildren<Text>();entryLabel.font=v2.SemiBold;
            PlaceTopLeft(entryLabel.rectTransform,24,0,294,68);
            PlaceTopLeft(enter.transform.Find("Play").GetComponent<RectTransform>(),336,22,24,24);
        }

        static void V2GameControls(Transform home,MainMenuScreen screen)
        {
            Set(screen,"friendsPanel",home.GetComponentInChildren<LobbyFriendsPanel>());
            var right=home.Find("LobbyRightRail");
            var language=V2Button(right,"UZ","Globe",136,48,120,52,localized:false);
            Set(language.gameObject.AddComponent<LanguageToggle>(),"label",language.GetComponentInChildren<Text>());
            var settings=V2Button(right,"","Gear",268,48,52,52,"page","settings");settings.name="LobbySettings";
            var quit=V2Button(right,"","Logout",332,48,52,52,"quit");quit.name="LobbyQuit";
            PlaceTopLeft(settings.transform.Find("Gear").GetComponent<RectTransform>(),14,14,24,24);
            PlaceTopLeft(quit.transform.Find("Logout").GetComponent<RectTransform>(),14,14,24,24);
            var hint=V2Text(right,"",0,103,384,30,18,false);hint.alignment=TextAnchor.MiddleRight;
            foreach(var pair in new[]{(settings,"menu.settings"),(quit,"menu.quit")})
            {
                var tip=pair.Item1.gameObject.AddComponent<LobbyIconHint>();Set(tip,"label",hint);Set(tip,"key",pair.Item2);
            }
            var profile=V2Button(right,"","User",0,136,384,112,"page","settings");profile.name="LobbyProfile";
            Set(screen,"profileName",profile.GetComponentInChildren<Text>());
            PlaceTopLeft(profile.GetComponentInChildren<Text>().rectTransform,96,19,264,40);
            profile.GetComponentInChildren<Text>().font=v2.SemiBold;profile.GetComponentInChildren<Text>().fontSize=25;
            var icon=profile.transform.Find("User").GetComponent<Image>();PlaceTopLeft(icon.rectTransform,24,28,52,52);
            Set(screen,"profileIcon",icon);Set(screen,"profileThumb",V2Picture(profile.transform,"Avatar",null,24,28,52,52));
            V2Text(profile.transform,"lobby.profile_hint",96,62,264,32,18);
        }
    }
}

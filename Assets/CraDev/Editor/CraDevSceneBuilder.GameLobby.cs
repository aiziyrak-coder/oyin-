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

            var eventCard=V2Panel(root,"LobbyEvent",1420,460,430,150,V2Glass);
            V2Icon(eventCard.transform,"Calendar",22,22,24);
            V2Text(eventCard.transform,"home.event_next",60,12,344,44,21).color=new Color32(173,189,206,255);
            Set(page,"eventTitle",V2Text(eventCard.transform,"",24,67,382,60,24,false));
            V2Text(root,"lobby.enter_hint",1420,863,430,44,21).color=new Color32(190,205,220,255);
            var enter=V2Button(root,"lobby.enter","ArrowRight",1420,925,430,68,"enter-world",blue:true);
            enter.name="EnterNewWorld";
        }

        static void V2GameControls(Transform home,MainMenuScreen screen)
        {
            Set(screen,"friendsPanel",home.GetComponentInChildren<LobbyFriendsPanel>());
            var language=V2Button(home,"UZ","Globe",1598,65,110,52,localized:false);
            Set(language.gameObject.AddComponent<LanguageToggle>(),"label",language.GetComponentInChildren<Text>());
            var settings=V2Button(home,"","Gear",1720,65,60,52,"page","settings");settings.name="LobbySettings";
            var quit=V2Button(home,"","Logout",1792,65,60,52,"quit");quit.name="LobbyQuit";
            var hint=V2Text(home,"",1590,126,260,32,19,false);hint.alignment=TextAnchor.MiddleRight;
            foreach(var pair in new[]{(settings,"menu.settings"),(quit,"menu.quit")})
            {
                var tip=pair.Item1.gameObject.AddComponent<LobbyIconHint>();Set(tip,"label",hint);Set(tip,"key",pair.Item2);
            }
            var profile=V2Button(home,"","User",1420,236,430,132,"page","settings");profile.name="LobbyProfile";
            Set(screen,"profileName",profile.GetComponentInChildren<Text>());
            PlaceTopLeft(profile.GetComponentInChildren<Text>().rectTransform,112,24,292,45);
            profile.GetComponentInChildren<Text>().font=v2.SemiBold;profile.GetComponentInChildren<Text>().fontSize=27;
            var icon=profile.transform.Find("User").GetComponent<Image>();PlaceTopLeft(icon.rectTransform,24,28,64,64);
            Set(screen,"profileIcon",icon);Set(screen,"profileThumb",V2Picture(profile.transform,"Avatar",null,24,28,64,64));
            V2Text(profile.transform,"lobby.profile_hint",112,73,294,36,19);
        }
    }
}

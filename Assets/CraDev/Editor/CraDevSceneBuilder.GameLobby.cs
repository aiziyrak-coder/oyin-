using CraDev.MainMenu;
using UnityEngine;
using UnityEngine.UI;
using static CraDev.EditorTools.UiBuild;
namespace CraDev.EditorTools
{
    static partial class CraDevSceneBuilder
    {
        static void V2GameHome(Transform root,LobbyContent page)
        {
            var mark=V2Panel(root,"BrandMark",48,30,76,76,V2Glass);
            V2Icon(mark.transform,"Play",22,22,32).color=new Color32(255,225,159,255);
            V2Text(root,"NewWorld",142,30,350,48,36,false).font=v2.SemiBold;
            V2Text(root,"lobby.caption",144,76,370,30,19).color=new Color32(201,195,187,255);
            var panel=V2Panel(root,"LobbyFriends",36,124,492,896,new Color32(31,30,31,242));
            var friends=panel.gameObject.AddComponent<LobbyFriendsPanel>();
            Set(friends,"font",v2.Medium);Set(friends,"rounded",v2.RoundFill);
            Set(friends,"addIcon",LineIcon("UserPlus"));Set(friends,"removeIcon",LineIcon("Minus"));Set(friends,"acceptIcon",LineIcon("Check"));
            V2Button(panel.transform,"friends.tab.friends","Users",18,18,160,74,"friends","friends");
            V2Button(panel.transform,"lobby.online",null,188,18,136,74,"friends","online");
            V2Button(panel.transform,"lobby.offline",null,334,18,140,74,"friends","offline");
            var field=V2Search(panel.transform,"friends.search",18,110,456);Set(friends,"search",field);
            string[] modes={"find","online","remove","requests","refresh"};
            string[] icons={"UserPlus","Users","Minus","Bell","Refresh"};
            var buttons=new Button[5];
            for(int i=0;i<5;i++)
            {
                buttons[i]=V2Button(panel.transform,"",icons[i],18+i*94,176,80,52);
                buttons[i].name="FriendTool_"+modes[i];
                PlaceTopLeft(buttons[i].transform.Find(icons[i]).GetComponent<RectTransform>(),28,14,24,24);
            }
            SetArray(friends,"tools",buttons);
            Set(friends,"countLabel",V2Text(panel.transform,"",20,238,452,32,18,false));
            var viewport=CreateRect("FriendViewport",panel.transform);PlaceTopLeft(viewport,18,282,456,506);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;
            scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
            var rows=CreateRect("FriendRows",viewport);PlaceTopLeft(rows,0,0,456,506);scroll.content=rows;Set(friends,"rows",rows);
            var footer=V2Panel(panel.transform,"SelfFooter",16,810,460,70,V2Glass);
            V2Icon(footer.transform,"User",18,19,32);
            Set(friends,"footerName",V2Text(footer.transform,"",66,14,215,42,23,false));
            V2Button(footer.transform,"","Gear",326,9,56,52,"page","settings").name="LobbySettings";
            V2Button(footer.transform,"","Logout",392,9,56,52,"quit").name="LobbyQuit";

            var right=CreateRect("LobbyRightRail",root);
            right.anchorMin=new Vector2(1,0);right.anchorMax=new Vector2(1,1);right.pivot=new Vector2(1,1);
            right.anchoredPosition=new Vector2(-48,0);right.sizeDelta=new Vector2(650,0);
            V2Text(right,"W E L C O M E   T O",0,163,650,38,18,false).color=new Color32(206,195,194,255);
            var title=V2Text(right,"NewWorld",-5,188,680,130,94,false);title.font=v2.SemiBold;title.verticalOverflow=VerticalWrapMode.Overflow;
            var gradient=title.gameObject.AddComponent<LobbyGradient>();gradient.left=Color.white;
            V2Text(right,"lobby.caption",0,301,650,46,31).color=new Color32(236,226,216,255);
            var promo=V2Panel(right,"LobbyPromo",0,437,650,190,V2Glass);
            V2Picture(promo.transform,"WorldPreview",V2Texture("sunset-home"),325,8,317,174);
            V2Text(promo.transform,"lobby.promo",34,20,330,90,27).font=v2.SemiBold;
            V2Text(promo.transform,"lobby.promo_hint",34,107,300,64,22).color=new Color32(194,189,183,255);
            string[] cardIcons={"Users","Globe","Calendar"};
            string[] cardTitles={"lobby.with_friends","lobby.your_world","home.event_next"};
            string[] cardHints={"lobby.together","lobby.possibilities",""};
            for(int i=0;i<3;i++)
            {
                var card=V2Button(right,"",null,i*222,643,206,147,i==0?"friends":i==1?"enter-world":"soon",i==0?"find":null);
                card.name=i==2?"LobbyEvent":"LobbyFeature"+i;
                V2Icon(card.transform,cardIcons[i],87,24,32);
                var label=V2Text(card.transform,cardTitles[i],6,67,194,38,23);label.alignment=TextAnchor.MiddleCenter;
                var hint=V2Text(card.transform,cardHints[i],8,107,190,29,17,i!=2);hint.alignment=TextAnchor.MiddleCenter;hint.color=new Color32(178,174,169,255);
                if(i==2)Set(page,"eventTitle",hint);
            }
            var enter=V2Button(right,"lobby.enter","Play",0,821,650,126,"enter-world");enter.name="EnterNewWorld";
            enter.GetComponent<Image>().color=Color.white;enter.gameObject.AddComponent<LobbyGradient>();
            PlaceTopLeft(enter.transform.Find("Play").GetComponent<RectTransform>(),49,39,48,48);
            var entryLabel=enter.GetComponentInChildren<Text>();entryLabel.font=v2.SemiBold;entryLabel.fontSize=32;
            PlaceTopLeft(entryLabel.rectTransform,137,0,495,126);
            var sign=V2Text(right,"—   Game On   —",0,965,650,48,27,false);sign.alignment=TextAnchor.MiddleCenter;sign.color=new Color32(199,198,176,255);
            foreach(var label in right.GetComponentsInChildren<Text>())
                if(label.transform.parent==right)
                {
                    var shadow=label.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.8f);shadow.effectDistance=new Vector2(2,-2);
                }
            foreach(var surface in root.GetComponentsInChildren<Image>())
            {
                if(surface.type!=Image.Type.Sliced||surface.sprite!=v2.RoundFill)continue;
                surface.pixelsPerUnitMultiplier=1;
                var rim=surface.gameObject.AddComponent<Outline>();rim.effectColor=new Color(1,.89f,.68f,.16f);rim.effectDistance=new Vector2(1,-1);rim.useGraphicAlpha=true;
            }
        }
        static void V2GameControls(Transform home,MainMenuScreen screen)
        {
            Set(screen,"friendsPanel",home.GetComponentInChildren<LobbyFriendsPanel>());
            var right=home.Find("LobbyRightRail");
            var language=V2Button(right,"UZ","Globe",146,32,110,60,localized:false);
            Set(language.gameObject.AddComponent<LanguageToggle>(),"label",language.GetComponentInChildren<Text>());
            V2Button(right,"","Bell",274,32,66,60,"friends","requests");
            var profile=V2Button(right,"","User",358,32,292,66,"page","settings");profile.name="LobbyProfile";
            var name=profile.GetComponentInChildren<Text>();Set(screen,"profileName",name);name.font=v2.SemiBold;name.fontSize=23;
            PlaceTopLeft(name.rectTransform,76,4,206,34);
            var icon=profile.transform.Find("User").GetComponent<Image>();PlaceTopLeft(icon.rectTransform,14,10,46,46);
            Set(screen,"profileIcon",icon);Set(screen,"profileThumb",V2Picture(profile.transform,"Avatar",null,14,10,46,46));
            V2Text(profile.transform,"lobby.profile_hint",76,36,200,24,17).color=new Color32(173,190,163,255);
        }
    }
}

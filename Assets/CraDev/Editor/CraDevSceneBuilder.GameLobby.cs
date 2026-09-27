using CraDev.MainMenu;
using UnityEngine;
using UnityEngine.UI;
using static CraDev.EditorTools.UiBuild;
namespace CraDev.EditorTools
{
    static partial class CraDevSceneBuilder
    {
        static LobbySolidIcon Solid(Transform parent,string name,float x,float y,float size,int kind)
        {
            var rect=CreateRect(name,parent);PlaceTopLeft(rect,x,y,size,size);
            var icon=rect.gameObject.AddComponent<LobbySolidIcon>();icon.kind=kind;icon.color=Color.white;icon.raycastTarget=false;return icon;
        }
        static void V2GameHome(Transform root,LobbyContent page)
        {
            var mark=V2Panel(root,"BrandMark",48,30,76,76,V2Glass);
            Solid(mark.transform,"Play",15,14,48,0).color=new Color32(255,225,159,255);
            V2Text(root,"NewWorld",142,30,350,48,36,false).font=v2.SemiBold;
            V2Text(root,"lobby.caption",144,76,370,30,19).color=new Color32(201,195,187,255);
            var panel=V2Panel(root,"LobbyFriends",36,124,492,896,new Color32(31,30,31,242));
            panel.rectTransform.anchorMin=Vector2.zero;panel.rectTransform.anchorMax=new Vector2(0,1);
            panel.rectTransform.sizeDelta=new Vector2(492,-148);
            var drawer=root.gameObject.AddComponent<LobbyFriendsDrawer>();
            var panelGroup=panel.gameObject.AddComponent<CanvasGroup>();
            var friends=panel.gameObject.AddComponent<LobbyFriendsPanel>();
            Set(friends,"font",v2.Medium);Set(friends,"rounded",v2.RoundFill);
            Set(friends,"addIcon",LineIcon("UserPlus"));Set(friends,"removeIcon",LineIcon("Minus"));Set(friends,"acceptIcon",LineIcon("Check"));
            var filters=new Image[3];var counts=new Text[3];
            string[] filterModes={"friends","online","offline"};string[] filterKeys={"friends.tab.friends","lobby.online","lobby.offline"};
            for(int i=0;i<3;i++)
            {
                float x=i==0?18:164+(i-1)*112;float w=i==0?136:100;
                var tab=V2Button(panel.transform,filterKeys[i],null,x,18,w,80,"friends",filterModes[i]);tab.name="FriendFilter_"+filterModes[i];filters[i]=tab.GetComponent<Image>();
                var label=tab.GetComponentInChildren<Text>();label.fontSize=17;label.alignment=TextAnchor.MiddleCenter;PlaceTopLeft(label.rectTransform,4,40,w-8,32);
                Solid(tab.transform,"Status",i==0?32:23,12,25,i==0?1:4).color=i==1?new Color32(48,226,137,255):i==2?new Color32(149,152,166,255):Color.white;
                counts[i]=V2Text(tab.transform,"0",i==0?92:61,13,36,30,17,false);
            }
            SetArray(friends,"filters",filters);SetArray(friends,"filterCounts",counts);
            var summary=V2Panel(panel.transform,"FriendSummary",388,18,86,80,V2Glass);
            Set(friends,"countLabel",V2Text(summary.transform,"",5,12,76,56,17,false));
            var field=V2Search(panel.transform,"friends.search",18,110,456);Set(friends,"search",field);
            Set(friends,"drawer",drawer);
            var handle=V2Button(root,"‹",null,528,148,48,76,localized:false);handle.name="FriendsDrawerToggle";
            var arrow=handle.GetComponentInChildren<Text>();arrow.alignment=TextAnchor.MiddleCenter;arrow.fontSize=40;
            PlaceTopLeft(arrow.rectTransform,0,0,48,76);
            Set(drawer,"panel",panel.rectTransform);Set(drawer,"handle",handle.GetComponent<RectTransform>());Set(drawer,"content",panelGroup);
            Set(drawer,"button",handle);Set(drawer,"arrow",arrow);Set(drawer,"search",field);
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
            var viewport=CreateRect("FriendViewport",panel.transform);PlaceTopLeft(viewport,18,242,456,552);
            viewport.anchorMin=Vector2.zero;viewport.anchorMax=new Vector2(0,1);viewport.sizeDelta=new Vector2(456,-344);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;
            scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
            var rows=CreateRect("FriendRows",viewport);PlaceTopLeft(rows,0,0,456,552);scroll.content=rows;Set(friends,"rows",rows);
            var footer=V2Panel(panel.transform,"SelfFooter",16,810,460,70,V2Glass);
            footer.rectTransform.anchorMin=footer.rectTransform.anchorMax=Vector2.zero;footer.rectTransform.anchoredPosition=new Vector2(16,86);
            Set(friends,"footerPortrait",V2Picture(footer.transform,"SelfPortrait",null,12,8,54,54));
            Set(friends,"footerName",V2Text(footer.transform,"",66,14,215,42,23,false));
            V2Button(footer.transform,"","Gear",326,9,56,52,"page","settings").name="LobbySettings";
            V2Button(footer.transform,"","Logout",392,9,56,52,"quit").name="LobbyQuit";

            var right=CreateRect("LobbyRightRail",root);
            right.anchorMin=new Vector2(1,0);right.anchorMax=new Vector2(1,1);right.pivot=new Vector2(1,1);
            right.anchoredPosition=new Vector2(-48,0);right.sizeDelta=new Vector2(650,0);
            V2Text(right,"W E L C O M E   T O",0,163,650,38,18,false).color=new Color32(206,195,194,255);
            var title=V2Text(right,"NewWorld",-5,188,680,130,104,false);title.font=v2.Bold;title.fontSize=104;title.verticalOverflow=VerticalWrapMode.Overflow;
            var gradient=title.gameObject.AddComponent<LobbyGradient>();gradient.left=Color.white;
            V2Text(right,"lobby.caption",0,301,650,46,31).color=new Color32(236,226,216,255);
            var promo=V2Panel(right,"LobbyPromo",0,437,650,190,V2Glass);
            V2Text(promo.transform,"lobby.promo",34,20,330,90,27).font=v2.SemiBold;
            V2Text(promo.transform,"lobby.promo_hint",34,107,300,64,22).color=new Color32(194,189,183,255);
            var promoArrow=V2Button(promo.transform,"›",null,560,105,62,62,"enter-world",localized:false);promoArrow.name="PromoArrow";promoArrow.GetComponentInChildren<Text>().fontSize=42;
            V2Panel(promo.transform,"PromoProgress",38,174,36,3,new Color32(116,224,188,255));
            string[] cardIcons={"Users","Globe","Calendar"};
            string[] cardTitles={"lobby.with_friends","lobby.your_world","home.event_next"};
            string[] cardHints={"lobby.together","lobby.possibilities",""};
            for(int i=0;i<3;i++)
            {
                var card=V2Button(right,"",null,i*222,643,206,147,i==0?"friends":i==1?"enter-world":"soon",i==0?"find":null);
                card.name=i==2?"LobbyEvent":"LobbyFeature"+i;
                if(i<2)Solid(card.transform,cardIcons[i],87,24,32,i==0?1:2);else V2Icon(card.transform,cardIcons[i],87,24,32);
                var label=V2Text(card.transform,cardTitles[i],6,67,194,38,23);label.alignment=TextAnchor.MiddleCenter;
                var hint=V2Text(card.transform,cardHints[i],8,107,190,29,17,i!=2);hint.alignment=TextAnchor.MiddleCenter;hint.color=new Color32(178,174,169,255);
                if(i==2)Set(page,"eventTitle",hint);
            }
            var entryGlow=V2Panel(right,"EntryGlow",-18,803,686,162,Color.white);entryGlow.raycastTarget=false;
            var enter=V2Button(right,"lobby.enter","Play",0,821,650,126,"enter-world");enter.name="EnterNewWorld";
            enter.transform.Find("Play").gameObject.SetActive(false);
            var orb=V2Panel(enter.transform,"PlayOrb",139,18,90,90,Color.white);
            Solid(orb.transform,"PlayFill",24,24,42,0);
            var entryLabel=enter.GetComponentInChildren<Text>();entryLabel.font=v2.SemiBold;entryLabel.fontSize=32;
            PlaceTopLeft(entryLabel.rectTransform,260,0,338,126);
            V2Text(enter.transform,"→",574,20,58,82,45,false);
            var sign=V2Text(right,"—   Game On   —",0,965,650,48,27,false);sign.alignment=TextAnchor.MiddleCenter;sign.color=new Color32(199,198,176,255);
            foreach(var label in right.GetComponentsInChildren<Text>())
                if(label.transform.parent==right)
                {
                    var shadow=label.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.8f);shadow.effectDistance=new Vector2(2,-2);
                }
            foreach(var surface in root.GetComponentsInChildren<Image>())
                if(surface.type==Image.Type.Sliced&&surface.sprite==v2.RoundFill)ReferenceSurface.Apply(surface,5,14);
            ReferenceSurface.Apply(panel,0,27);ReferenceSurface.Apply(footer,0,19);
            ReferenceSurface.Apply(promo,3,27);ReferenceSurface.Apply(enter.GetComponent<Image>(),1,32);ReferenceSurface.Apply(orb,2,45);
            ReferenceSurface.Apply(entryGlow,6,48);
            ReferenceSurface.Apply(promoArrow.GetComponent<Image>(),0,31);
            foreach(string n in new[]{"LobbyFeature0","LobbyFeature1","LobbyEvent"})ReferenceSurface.Apply(right.Find(n).GetComponent<Image>(),0,27);
            ReferenceSurface.Apply(filters[0],4,16);
            // Ixcham kartalar va kirish tugmasi ekranning pastki o'ng chetida qoladi.
            foreach(var item in new[]{promo.rectTransform,(RectTransform)right.Find("LobbyFeature0"),(RectTransform)right.Find("LobbyFeature1"),(RectTransform)right.Find("LobbyEvent"),entryGlow.rectTransform,(RectTransform)enter.transform,sign.rectTransform})
            {
                float y=item.anchoredPosition.y;item.anchorMin=item.anchorMax=Vector2.zero;item.anchoredPosition=new Vector2(item.anchoredPosition.x,1080+y);
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
            ReferenceSurface.Apply(language.GetComponent<Image>(),0,22);ReferenceSurface.Apply(profile.GetComponent<Image>(),0,22);
            foreach(var button in right.GetComponentsInChildren<Button>())if(button.GetComponent<ReferenceSurface>()==null)ReferenceSurface.Apply(button.GetComponent<Image>(),5,18);
        }
    }
}

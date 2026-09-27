using System;
using System.Linq;
using CraDev.Online;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    public partial class LobbyContent
    {
        [SerializeField] Texture educationImage, friendsImage;
        [SerializeField] Sprite[] businessIcons;
        void Portrait(Transform parent,string avatarId,float x,float y,float side)
        {
            var card=Lobby.FindAvatar(avatarId)?.card;
            if(card==null)return;
            var background=Panel("PortraitBackground",x,y,side,side,new Color32(84,106,128,255),parent);
            var image=Rect("Portrait",background.transform,0,0,side,side).gameObject.AddComponent<RawImage>();
            image.texture=card.texture;image.uvRect=new Rect(.32f,.76f,.36f,.2112f);image.raycastTarget=false;
        }
        void Photo(Texture texture,Rect crop,float x,float y,float w,float h)
        {
            var frame=Rect("PhotoFrame",surface,x,y,w,h).gameObject.AddComponent<Image>();
            frame.sprite=rounded;frame.type=Image.Type.Sliced;frame.pixelsPerUnitMultiplier=1;
            frame.raycastTarget=false;frame.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var image=Rect("Photo",frame.transform,0,0,w,h).gameObject.AddComponent<RawImage>();
            image.texture=texture;image.uvRect=crop;image.raycastTarget=false;
        }
        void Shops()
        {
            string[] names={"clothing","sportswear","shoes","electronics","computers","casual","accessories","audio","home","beauty","sports","gifts"};
            names=names.Select(key=>Loc.T("shops.store."+key)).ToArray();
            int[] groups={1,6,1,2,2,1,3,2,4,5,6,7};
            var results=Enumerable.Range(0,names.Length).Where(i=>(section==0||groups[i]==section) &&
                (search==null||names[i].IndexOf(search.text,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
            float cell=(width-48)/4;
            for(int n=0;n<results.Length;n++)
            {
                int i=results[n];
                var card=Button(names[i],n%4*(cell+16),n/4*155,cell,138,()=>Lobby.Toast(Loc.F("shops.store_soon",names[i])));
                card.GetComponent<Image>().color=Glass;
                var label=card.GetComponentInChildren<Text>(); label.color=new Color32(231,236,244,255);label.font=font;label.fontSize=25;label.alignment=TextAnchor.MiddleCenter;
            }
            if(results.Length==0) Message("shops.no_results");
            ContentHeight(Mathf.Max(300,Mathf.Ceil(results.Length/4f)*155));
        }
        void Places(bool fun)
        {
            string[] names=fun?new[]{"fun.place.concert_hall","fun.place.cinema","fun.place.arcade","fun.place.stadium","fun.place.park","fun.place.club"}:
                new[]{"lobby.place.it","lobby.place.medicine","lobby.place.languages","lobby.place.business","lobby.place.art","lobby.place.science"};
            int[] categories=fun?new[]{1,2,3,4,5,1}:new[]{4,1,3,5,6,2};
            int n=0;
            for(int i=0;i<names.Length;i++)
            {
                if(section!=0 && categories[i]!=section) continue;
                string key=names[i];float cell=(width-45)/4;int column=n%4,row=n/4;n++;
                var button=Button(Loc.T(key),column*(cell+15),row*270+195,cell,64,()=>Lobby.Soon(Loc.T(key)));
                if(educationImage!=null && !fun)
                    Photo(educationImage,new Rect((i%2)*.5f+.003f,1-((i%4)/2+1)*.5f+.003f,.494f,.494f),column*(cell+15),row*270,cell,190);
                else
                {
                    var image=Panel("Place",column*(cell+15),row*270,cell,190,new Color(.15f+.04f*i,.22f,.36f,1));
                    Text(Loc.T(key),20,25,cell-40,130,30,image.transform);
                }
            }
            ContentHeight(Mathf.Ceil(n/4f)*270);
        }
        void Business()
        {
            string[][] keys={
                new[]{"office_rent","find_partner","post_startup","meet_investors"},
                new[]{"small_office","floor","meeting_room"},new[]{"open_desk","fixed_desk","meeting_room"},
                new[]{"post_startup","accelerator","pitch"},new[]{"meet_investors","funds","networking"},
                new[]{"pitch","networking"},new[]{"legal","accounting"}};
            var actions=keys[Mathf.Clamp(section,0,keys.Length-1)];
            float y=section==0?455:60;
            for(int i=0;i<actions.Length;i++)
            {
                string key="business.action."+actions[i];
                var button=Button(Loc.T(key),i*(width/actions.Length),y,width/actions.Length-22,185,()=>Lobby.Toast(Loc.F("business.action_soon",Loc.T(key))));
                var icon=Panel("ActionIcon",22,20,52,52,Color.white,button.transform);
                icon.sprite=businessIcons[i%businessIcons.Length];icon.type=Image.Type.Simple;
                var caption=button.GetComponentInChildren<Text>().rectTransform;
                caption.anchoredPosition=new Vector2(20,-90);caption.sizeDelta=new Vector2(width/actions.Length-62,80);
            }
        }
        void Friends()
        {
            if(section==1 || section==3) {Message(section==1?"friends.groups_soon":"friends.chat_soon");return;}
            if(section==2) {Events();return;}
            int request=revision;
            Text(Loc.T("friends.suggestions"),22,5,510,65,28).font=bold;
            if(friendsImage!=null)
            {
                Photo(friendsImage,new Rect(0,0,1,1),630,45,width-640,605);
                Panel("HeroOverlay",650,65,width-680,145,new Color(.05f,.065f,.085f,.78f));
                Text(Loc.T("friends.hero"),680,80,width-730,115,42).font=bold;
                Button(Loc.T("friends.find"),840,440,510,85,()=>{search.Select();search.ActivateInputField();},true);
            }
            Text(Loc.T("common.connecting"),20,80,550,80).name="Loading";
            if(search!=null && !string.IsNullOrWhiteSpace(search.text))
            {
                StartCoroutine(Lobby.Api.SearchPlayers(PlayerProfile.Token,search.text,result=>{
                    if(request!=revision)return;
                    ClearLoading();
                    if(!result.Ok){Text(Loc.T("common.server_error"),20,85,550,160);return;}
                    SocialRows(result.Data.items,80,false);
                }));
            }
            else
            {
                StartCoroutine(Lobby.Api.Friends(PlayerProfile.Token,result=>{
                    if(request!=revision)return;
                    ClearLoading();
                    if(!result.Ok){Text(Loc.T("common.server_error"),20,85,550,160);return;}
                    Lobby.SetIncoming(result.Data.incoming?.Length??0);
                    var rows=(result.Data.incoming??new PlayerSummary[0]).Concat(result.Data.friends??new PlayerSummary[0]).Concat(result.Data.outgoing??new PlayerSummary[0]).ToArray();
                    float y=SocialRows(rows,80,true);
                    StartCoroutine(Lobby.Api.SuggestedPlayers(PlayerProfile.Token,suggestions=>{
                        if(request!=revision)return;
                        if(suggestions.Ok) SocialRows(suggestions.Data.items,y+30,false);
                    }));
                }));
            }
        }
        void ClearLoading()
        {
            var loading=surface.Find("Loading"); if(loading!=null){loading.gameObject.SetActive(false);Destroy(loading.gameObject);}
        }
        float SocialRows(PlayerSummary[] players,float y,bool friends)
        {
            if(players==null || players.Length==0)
            {
                Text(Loc.T(friends?"friends.no_friends":"friends.no_results"),20,y,545,75,23).color=Muted;return y+85;
            }
            foreach(var player in players)
            {
                var panel=Panel("Player_"+player.nickname,0,y,590,110,Glass);
                Portrait(panel.transform,player.avatarId,16,20,70);
                Text(player.nickname,100,10,240,44,27,panel.transform).font=bold;
                Text(Loc.T(player.online?"friends.status.online":"friends.status.offline"),100,58,240,32,20,panel.transform).color=player.online?new Color(.3f,.85f,.5f):Muted;
                string label=player.friendship=="incoming"?"friends.accept":player.friendship=="friends"?"friends.remove":player.friendship=="outgoing"?"common.cancel":"friends.find";
                string nickname=player.nickname;
                Button(Loc.T(label),365,19,210,68,()=>{
                    Action<ApiResult<FriendshipResponse>> done=result=>{
                        Lobby.Toast(Loc.T(result.Ok?"settings.saved":"common.server_error"));if(isActiveAndEnabled)Render();
                    };
                    if(player.friendship=="incoming") Lobby.StartCoroutine(Lobby.Api.AcceptFriend(PlayerProfile.Token,nickname,done));
                    else if(player.friendship=="friends"||player.friendship=="outgoing")
                        Lobby.Dialog.Show(Loc.T("friends.remove"),nickname,Loc.T("common.ok"),()=>Lobby.StartCoroutine(Lobby.Api.RemoveFriend(PlayerProfile.Token,nickname,done)),Loc.T("common.cancel"));
                    else Lobby.StartCoroutine(Lobby.Api.RequestFriend(PlayerProfile.Token,nickname,done));
                },player.friendship=="incoming",panel.transform);
                y+=122;
            }
            ContentHeight(Mathf.Max(650,y));return y;
        }
        void Events()
        {
            int request=revision;
            Message("common.connecting");
            StartCoroutine(Lobby.Api.Events(result=>{
                if(request!=revision)return;
                Begin();
                if(!result.Ok){Message("common.server_error");return;}
                if(result.Data.items==null||result.Data.items.Length==0){Message("friends.events_empty");return;}
                int i=0;
                foreach(var evt in result.Data.items)
                {
                    var card=Button(evt.title.Value,0,i*140,width-15,125,()=>Lobby.Show(evt.zone));
                    var label=card.GetComponentInChildren<Text>();
                    label.text=evt.title.Value+"   ·   "+evt.StartsLocal.ToString("dd.MM  HH:mm")+"\n"+evt.place.Value;
                    i++;
                }
                ContentHeight(i*140);
            }));
        }
        void Ranking()
        {
            int request=revision; Message("common.connecting");
            StartCoroutine(Lobby.Api.Leaderboard(100,result=>{
                if(request!=revision)return;
                Begin();
                if(!result.Ok){Message("common.server_error");return;}
                if(result.Data.items==null||result.Data.items.Length==0){Message("top.empty");return;}
                Text(Loc.T("top.rank"),30,0,170,70);Text(Loc.T("top.player"),220,0,800,70);Text(Loc.T("top.time"),width-400,0,360,70);
                int i=0;
                foreach(var entry in result.Data.items)
                {
                    var row=Panel("Rank_"+entry.rank,0,80+i*90,width-15,80,entry.nickname==PlayerProfile.Nickname?Blue:Glass);
                    Text(entry.rank.ToString(),30,0,170,80,28,row.transform);
                    Text(entry.nickname,220,0,800,80,28,row.transform);
                    Text(Loc.F("top.minutes",entry.minutes),width-410,0,360,80,25,row.transform);i++;
                }
                ContentHeight(90*i+80);
            }));
        }
    }
}

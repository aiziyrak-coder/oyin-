using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CraDev.Online;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    // All social actions stay inside the lobby. No page change and no profile mutation on refresh.
    public class LobbyFriendsPanel : MonoBehaviour
    {
        [SerializeField] Font font;
        [SerializeField] Sprite rounded, addIcon, removeIcon, acceptIcon;
        [SerializeField] Text countLabel;
        [SerializeField] InputField search;
        [SerializeField] RectTransform rows;
        [SerializeField] Button[] tools;
        MainMenuScreen lobby;
        FriendsResponse connections;
        PlayerSummary[] visible=Array.Empty<PlayerSummary>();
        string mode="friends", fingerprint;
        int revision;
        Coroutine debounce;
        readonly HashSet<string> busy=new HashSet<string>();
        static readonly Color Fill=new Color32(28,37,49,248);
        static readonly Color Accent=new Color32(55,102,132,255);
        public string Mode=>mode;
        public int VisibleCount=>visible.Length;
        public bool VisibleAreOnline=>visible.All(p=>p.online);
        public InputField Search=>search;
        public bool Loading {get;private set;}

        public void Begin(MainMenuScreen screen)
        {
            lobby=screen;
            string[] actions={"find","online","remove","requests","refresh"};
            for(int i=0;i<tools.Length;i++){string action=actions[i];tools[i].onClick.AddListener(()=>Choose(action));}
            search.onValueChanged.AddListener(_=>{
                if(debounce!=null)StopCoroutine(debounce);
                debounce=StartCoroutine(SearchLater());
            });
            Loc.Changed+=LanguageChanged;
            Refresh();StartCoroutine(Poll());
        }
        void OnDestroy(){Loc.Changed-=LanguageChanged;}
        void LanguageChanged(){fingerprint=null;Draw();UpdateCount();}
        IEnumerator SearchLater(){yield return new WaitForSecondsRealtime(.3f);mode="find";Refresh();debounce=null;}
        IEnumerator Poll(){while(true){yield return new WaitForSecondsRealtime(30);Refresh();}}
        public void Choose(string action)
        {
            if(lobby==null)return;
            if(debounce!=null){StopCoroutine(debounce);debounce=null;}
            if(action=="refresh"){Refresh();return;}
            mode=mode==action?"friends":action;
            if(mode!="find")search.SetTextWithoutNotify("");
            if(mode=="find"){search.Select();search.ActivateInputField();}
            Refresh();
        }
        public void Refresh()
        {
            if(lobby==null)return;
            int request=++revision;
            Loading=true;
            for(int i=0;i<tools.Length;i++)tools[i].GetComponent<Image>().color=
                mode==new[]{"find","online","remove","requests","refresh"}[i]?Accent:Fill;
            if(connections==null)Empty(Loc.T("common.connecting"));
            StartCoroutine(Load(request));
        }
        IEnumerator Load(int request)
        {
            ApiResult<FriendsResponse> friends=default;
            yield return lobby.Api.Friends(PlayerProfile.Token,r=>friends=r);
            if(request!=revision)yield break;
            if(!friends.Ok){Loading=false;Empty(Loc.T("menu.offline"));yield break;}
            connections=friends.Data ?? new FriendsResponse();UpdateCount();
            lobby.SetIncoming(connections.incoming?.Length??0);
            if(mode=="find")
            {
                ApiResult<PlayerList> result=default;
                if(string.IsNullOrWhiteSpace(search.text))yield return lobby.Api.SuggestedPlayers(PlayerProfile.Token,r=>result=r);
                else yield return lobby.Api.SearchPlayers(PlayerProfile.Token,search.text,r=>result=r);
                if(request!=revision)yield break;
                if(!result.Ok){Loading=false;Empty(Loc.T("common.server_error"));yield break;}
                visible=result.Data?.items??Array.Empty<PlayerSummary>();
            }
            else
            {
                var accepted=connections.friends??Array.Empty<PlayerSummary>();
                var incoming=connections.incoming??Array.Empty<PlayerSummary>();
                var outgoing=connections.outgoing??Array.Empty<PlayerSummary>();
                visible=(mode=="online"?accepted.Where(p=>p.online):mode=="requests"?incoming.Concat(outgoing):accepted.Concat(incoming).Concat(outgoing))
                    .OrderByDescending(p=>p.online).ThenBy(p=>p.nickname).ToArray();
            }
            Loading=false;Draw();
        }
        void UpdateCount()
        {
            var list=connections?.friends??Array.Empty<PlayerSummary>();
            countLabel.text=Loc.F("lobby.friend_count",list.Count(p=>p.online),list.Length);
        }
        void Clear()
        {
            foreach(Transform child in rows){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        }
        void Empty(string message)
        {
            fingerprint=null;Clear();
            Label(rows,message,10,20,rows.rect.width-20,150,21).color=new Color32(169,188,207,255);
            rows.sizeDelta=new Vector2(rows.sizeDelta.x,rows.parent.GetComponent<RectTransform>().rect.height);
        }
        void Draw()
        {
            string next=mode+Loc.Current+string.Join("|",visible.Select(p=>p.nickname+":"+p.avatarId+":"+p.friendship+":"+p.online+":"+busy.Contains(p.nickname)));
            if(next==fingerprint)return;
            if(visible.Length==0)
            {
                Empty(Loc.T(mode=="online"?"lobby.empty_online":mode=="requests"?"lobby.empty_requests":mode=="find"?"friends.no_results":"lobby.empty_friends"));
                fingerprint=next;return;
            }
            Clear();fingerprint=next;
            float w=rows.rect.width;
            for(int i=0;i<visible.Length;i++)
            {
                var player=visible[i];
                var row=Panel("Friend_"+player.nickname,rows,0,i*90,w,82,Fill);
                var portrait=Panel("Portrait",row.transform,12,16,48,48,new Color32(56,72,90,255));
                var option=lobby.FindAvatar(player.avatarId);
                if(option?.card!=null)
                {
                    var photo=Rect("Avatar",portrait.transform,0,0,48,48).gameObject.AddComponent<RawImage>();
                    photo.texture=option.card.texture;photo.uvRect=new Rect(.32f,.76f,.36f,.2112f);photo.raycastTarget=false;
                }
                var name=Label(row.transform,player.nickname,74,5,w-180,38,22);
                name.name="FriendNickname";
                name.resizeTextForBestFit=true;name.resizeTextMinSize=17;name.resizeTextMaxSize=22;
                string state=player.friendship=="incoming"?"friends.status.incoming":player.friendship=="outgoing"?"friends.status.outgoing":player.online?"common.online":"common.offline";
                Label(row.transform,Loc.T(state),74,43,w-90,27,17).color=player.online?new Color32(116,193,164,255):new Color32(154,172,191,255);
                if(player.friendship=="incoming")
                {
                    ActionButton(row.transform,w-98,acceptIcon,"Accept",()=>Change(player,true),!busy.Contains(player.nickname));
                    ActionButton(row.transform,w-50,removeIcon,"Remove",()=>ConfirmRemove(player),!busy.Contains(player.nickname));
                }
                else if(player.friendship=="outgoing" || (mode=="remove"&&player.friendship=="friends"))
                    ActionButton(row.transform,w-50,removeIcon,"Remove",()=>ConfirmRemove(player),!busy.Contains(player.nickname));
                else if(player.friendship!="friends")
                    ActionButton(row.transform,w-50,addIcon,"Add",()=>Change(player,false),!busy.Contains(player.nickname));
            }
            rows.sizeDelta=new Vector2(w,Mathf.Max(rows.parent.GetComponent<RectTransform>().rect.height,visible.Length*90));
        }
        void ConfirmRemove(PlayerSummary player)
        {
            lobby.Dialog.Show(Loc.T("friends.remove"),Loc.F("lobby.remove_confirm",player.nickname),Loc.T("friends.remove"),()=>{
                if(!busy.Add(player.nickname))return;Draw();
                lobby.StartCoroutine(lobby.Api.RemoveFriend(PlayerProfile.Token,player.nickname,result=>Done(player,result)));
            },Loc.T("common.cancel"));
        }
        void Change(PlayerSummary player,bool accept)
        {
            if(!busy.Add(player.nickname))return;Draw();
            if(accept)lobby.StartCoroutine(lobby.Api.AcceptFriend(PlayerProfile.Token,player.nickname,result=>Done(player,result)));
            else lobby.StartCoroutine(lobby.Api.RequestFriend(PlayerProfile.Token,player.nickname,result=>Done(player,result)));
        }
        void Done(PlayerSummary player,ApiResult<FriendshipResponse> result)
        {
            if(this==null)return;
            busy.Remove(player.nickname);
            lobby.Toast(Loc.T(result.Ok?"settings.saved":"common.server_error"));Refresh();
        }
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.gameObject.layer=5;rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);return rect;
        }
        Image Panel(string name,Transform parent,float x,float y,float w,float h,Color color)
        {
            var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=rounded;image.type=Image.Type.Sliced;
            image.pixelsPerUnitMultiplier=3;image.color=color;image.raycastTarget=false;return image;
        }
        Text Label(Transform parent,string value,float x,float y,float w,float h,int size)
        {
            var text=Rect("Label",parent,x,y,w,h).gameObject.AddComponent<Text>();text.font=font;text.fontSize=size;text.text=value;
            text.color=new Color32(232,238,245,255);text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;return text;
        }
        void ActionButton(Transform parent,float x,Sprite icon,string name,Action action,bool enabled)
        {
            var fill=Panel(name,parent,x,12,38,38,Accent);fill.raycastTarget=true;
            var button=fill.gameObject.AddComponent<Button>();button.targetGraphic=fill;button.interactable=enabled;button.onClick.AddListener(()=>action());
            var glyph=Panel("Icon",fill.transform,9,9,20,20,Color.white);glyph.sprite=icon;glyph.type=Image.Type.Simple;
        }
    }
}

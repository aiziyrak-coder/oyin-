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
    // Panel ichidagi asboblar va filtrlar almashtirgich: ikkinchi bosish do'stlar ro'yxatiga qaytaradi. Tashqi
    // tugmalar (qo'ng'iroq, "Do'stlar bilan" kartasi) Choose orqali kerakli bo'limni faqat ochadi, yopmaydi.
    public class LobbyFriendsPanel : MonoBehaviour
    {
        [SerializeField] Font font;
        [SerializeField] Sprite rounded, addIcon, removeIcon, acceptIcon, inviteIcon;
        [SerializeField] Text countLabel;
        [SerializeField] Text footerName;
        [SerializeField] RawImage footerPortrait;
        [SerializeField] Image[] filters;
        [SerializeField] Text[] filterCounts;
        [SerializeField] InputField search;
        [SerializeField] RectTransform rows;
        [SerializeField] Button[] tools;
        [SerializeField] LobbyFriendsDrawer drawer;
        [Tooltip("So'rovlar asbobidagi son (builder yaratadi): kelgan so'rov bo'lsa doim ko'rinadi.")]
        [SerializeField] Text requestsBadge;
        [Tooltip("Qo'ng'iroqdagi son. Bo'sh bo'lsa ish vaqtida qo'ng'iroq tugmasiga qo'shiladi.")]
        [SerializeField] Text bellBadge;
        [Tooltip("Ikonkali tugmalar uchun umumiy suzuvchi izoh matni (LobbyIconHint).")]
        [SerializeField] Text tooltip;
        MainMenuScreen lobby;
        FriendsResponse connections;
        PlayerSummary[] visible=Array.Empty<PlayerSummary>();
        string mode="friends", fingerprint;
        int revision;
        Coroutine debounce;
        Func<string> notice;
        bool listShown;
        // O'yin davomida allaqachon xabar qilingan so'rovlar: dunyodan lobbyga qaytganda eski so'rov qayta "yangi" bo'lmaydi
        static HashSet<string> knownIncoming;
        bool notifyShown;
        float notifyCheck;
        AudioSource chimeSource;
        static AudioClip chime;
        readonly HashSet<string> busy=new HashSet<string>();
        static readonly string[] ToolModes={"find","online","remove","requests","refresh"};
        static readonly string[] FilterModes={"friends","online","offline"};
        static readonly Color Fill=new Color32(41,40,40,248);
        static readonly Color Accent=LobbyPalette.Accent;
        static readonly Color InviteFill=new Color32(66,59,43,255);
        static readonly Color InviteGlyph=new Color32(255,215,143,255);
        static readonly Color BadgeFill=new Color32(243,65,95,255);
        public string Mode=>mode;
        public int VisibleCount=>visible.Length;
        public bool VisibleAreOnline=>visible.All(p=>p.online);
        public InputField Search=>search;
        public bool Loading {get;private set;}
        /// <summary>Kelgan, javob kutayotgan do'stlik so'rovlari (oxirgi muvaffaqiyatli javob bo'yicha).</summary>
        public int IncomingCount=>connections?.incoming?.Length??0;
        public Text RequestsBadge=>requestsBadge;
        public Text BellBadge=>bellBadge;

        public void Begin(MainMenuScreen screen)
        {
            lobby=screen;
            if(footerName!=null)footerName.text=PlayerProfile.Nickname;
            if(footerPortrait!=null){var avatar=lobby.FindAvatar(PlayerProfile.AvatarId);if(avatar?.card!=null){footerPortrait.texture=avatar.card.texture;footerPortrait.uvRect=new Rect(.32f,.76f,.36f,.2112f);}}
            for(int i=0;i<tools.Length&&i<ToolModes.Length;i++){string action=ToolModes[i];tools[i].onClick.AddListener(()=>Toggle(action));}
            // Filtrlar ham panel ichidagi almashtirgich (eski sahnada ular LobbyCommand orqali Choose'ni chaqiradi)
            if(filters!=null)
                for(int i=0;i<filters.Length&&i<FilterModes.Length;i++)
                {
                    string action=FilterModes[i];
                    if(filters[i].TryGetComponent(out Button filter)&&!filters[i].TryGetComponent(out LobbyCommand _))filter.onClick.AddListener(()=>Toggle(action));
                }
            search.onValueChanged.AddListener(_=>{
                if(debounce!=null)StopCoroutine(debounce);
                debounce=StartCoroutine(SearchLater());
            });
            BindBell();
            notifyShown=LobbyPrefs.NotifyFriendRequests;UpdateBadges();
            Loc.Changed+=LanguageChanged;
            Refresh(false);StartCoroutine(Poll());
        }
        void OnDestroy(){Loc.Changed-=LanguageChanged;}
        void LanguageChanged()
        {
            if(notice!=null)ShowNotice(notice);else{fingerprint=null;Draw();}
            UpdateCount();
        }
        IEnumerator SearchLater(){yield return new WaitForSecondsRealtime(.3f);mode="find";Refresh(true);debounce=null;}
        IEnumerator Poll(){while(true){yield return new WaitForSecondsRealtime(30);Refresh(false);}}
        void Update()
        {
            // Sozlamalarda "Do'stlik so'rovlari" bildirishnomasi almashtirilsa, qo'ng'iroq belgisi darhol yangilanadi
            if(lobby==null||Time.unscaledTime<notifyCheck)return;
            notifyCheck=Time.unscaledTime+.5f;
            bool notify=LobbyPrefs.NotifyFriendRequests;
            if(notify!=notifyShown){notifyShown=notify;UpdateBadges();}
        }

        /// <summary>Tashqi tugmalar uchun: panelni ochadi va shu bo'limni ko'rsatadi (qayta bosilsa ham yopilmaydi).</summary>
        public void Choose(string action)
        {
            if(lobby==null)return;
            if(drawer!=null)drawer.SetCollapsed(false);
            StopDebounce();
            if(action=="refresh"){Refresh(true);return;}
            SetMode(action);
        }
        // Panel ichidagi asbob yoki filtr: ikkinchi bosish do'stlar ro'yxatiga qaytaradi.
        void Toggle(string action)
        {
            if(lobby==null)return;
            StopDebounce();
            if(action=="refresh"){Refresh(true);return;}
            SetMode(mode==action?"friends":action);
        }
        void StopDebounce(){if(debounce!=null){StopCoroutine(debounce);debounce=null;}}
        void SetMode(string next)
        {
            mode=next;
            if(mode!="find")search.SetTextWithoutNotify("");
            if(mode=="find"){search.Select();search.ActivateInputField();}
            Refresh(true);
        }
        public void Refresh()=>Refresh(true);
        void Refresh(bool user)
        {
            if(lobby==null)return;
            int request=++revision;
            Loading=true;
            for(int i=0;i<tools.Length&&i<ToolModes.Length;i++)
                if(tools[i].TryGetComponent(out ReferenceSurface tool))tool.Style=mode==ToolModes[i]?4:5;
            if(filters!=null)
                for(int i=0;i<filters.Length&&i<FilterModes.Length;i++)
                    if(filters[i].TryGetComponent(out ReferenceSurface filter))filter.Style=mode==FilterModes[i]?4:5;
            if(connections==null)ShowNotice(()=>Loc.T("common.connecting"));
            StartCoroutine(Load(request,user));
        }
        IEnumerator Load(int request,bool user)
        {
            ApiResult<FriendsResponse> friends=default;
            yield return lobby.Api.Friends(PlayerProfile.Token,r=>friends=r);
            if(request!=revision)yield break;
            Func<string> failure=null;
            if(friends.Ok)
            {
                connections=friends.Data;
                var incoming=connections.incoming??Array.Empty<PlayerSummary>();
                lobby.SetIncoming(incoming.Length);
                NotifyIncoming(incoming);
                UpdateCount();UpdateBadges();
            }
            else
            {
                // Oxirgi muvaffaqiyatli ro'yxat saqlanadi: bitta xato so'rov (tarmoq, 429) qatorlarni o'chirmaydi
                bool network=friends.NetworkError;long status=friends.Status;
                failure=()=>ErrorMessage(network,status,null);
                if(user&&connections!=null&&mode!="find")lobby.Toast(failure());
            }
            if(mode=="find")
            {
                ApiResult<PlayerList> result=default;
                string query=search.text.Trim();
                if(query.Length==0)yield return lobby.Api.SuggestedPlayers(PlayerProfile.Token,r=>result=r);
                else yield return lobby.Api.SearchPlayers(PlayerProfile.Token,query,r=>result=r);
                if(request!=revision)yield break;
                Loading=false;
                if(!result.Ok)
                {
                    bool network=result.NetworkError;long status=result.Status;string code=status==400&&query.Length>0?"invalid_query":null;
                    ShowNotice(()=>ErrorMessage(network,status,code));yield break;
                }
                visible=result.Data.items??Array.Empty<PlayerSummary>();
            }
            else
            {
                Loading=false;
                if(connections==null){ShowNotice(failure??(()=>Loc.T("menu.offline")));yield break;}
                var accepted=connections.friends??Array.Empty<PlayerSummary>();
                var incoming=connections.incoming??Array.Empty<PlayerSummary>();
                var outgoing=connections.outgoing??Array.Empty<PlayerSummary>();
                var list=mode=="online"?accepted.Where(p=>p.online):mode=="offline"?accepted.Where(p=>!p.online):mode=="requests"?incoming.Concat(outgoing):accepted.Concat(incoming).Concat(outgoing);
                // Kelgan so'rovlar doim tepada: javob kutayotgan odam uzun ro'yxat oxirida yo'qolib qolmaydi
                visible=list.OrderBy(p=>p.friendship=="incoming"?0:1).ThenByDescending(p=>p.online).ThenBy(p=>p.nickname,StringComparer.OrdinalIgnoreCase).ToArray();
            }
            Draw();
        }
        void UpdateCount()
        {
            var list=connections?.friends??Array.Empty<PlayerSummary>();
            if(countLabel!=null){countLabel.text=list.Length.ToString();countLabel.alignment=TextAnchor.MiddleCenter;}
            if(filterCounts!=null&&filterCounts.Length==3){filterCounts[0].text=list.Length.ToString();filterCounts[1].text=list.Count(p=>p.online).ToString();filterCounts[2].text=list.Count(p=>!p.online).ToString();}
        }

        // ------------------------------------------------------------------ Kelgan so'rovlar: belgilar, xabar, ovoz

        // Qo'ng'iroq o'ng ustunda (V2GameControls): shu yerda topilib, son belgisi va izoh ulanadi.
        void BindBell()
        {
            var bell=lobby.GetComponentsInChildren<LobbyCommand>(true).FirstOrDefault(c=>c.action=="friends"&&c.value=="requests"&&!c.transform.IsChildOf(transform));
            if(bell==null)return;
            if(tooltip!=null&&!bell.TryGetComponent(out LobbyIconHint _))LobbyIconHint.Attach(bell.gameObject,tooltip,"lobby.tool.requests");
            if(bellBadge==null)bellBadge=Badge((RectTransform)bell.transform);
        }
        // Builder'dagi so'rovlar belgisi bilan bir xil: qizil yumaloq, burchakdan biroz chiqib turadi.
        Text Badge(RectTransform host)
        {
            var badge=Panel("RequestBadge",host,0,0,26,24,BadgeFill);
            var rect=badge.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one;rect.anchoredPosition=new Vector2(6,6);
            var text=Label(badge.transform,"",0,0,26,24,17);text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;
            var tr=text.rectTransform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;
            badge.gameObject.SetActive(false);
            return text;
        }
        void UpdateBadges()
        {
            int count=IncomingCount;
            SetBadge(requestsBadge,count);
            SetBadge(bellBadge,LobbyPrefs.NotifyFriendRequests?count:0);
        }
        static void SetBadge(Text badge,int count)
        {
            if(badge==null||!(badge.transform.parent is RectTransform host))return;
            host.gameObject.SetActive(count>0);
            badge.text=count>9?"9+":count.ToString();
            host.sizeDelta=new Vector2(count>9?34:26,host.sizeDelta.y);
        }
        // Yangi so'rov kelganda (va lobby ochilganda kutib turganlari bo'lsa): toast va yumshoq ovoz.
        void NotifyIncoming(PlayerSummary[] incoming)
        {
            bool first=knownIncoming==null;
            var fresh=first?incoming:incoming.Where(p=>!knownIncoming.Contains(p.nickname??"")).ToArray();
            knownIncoming=new HashSet<string>(incoming.Select(p=>p.nickname??""),StringComparer.OrdinalIgnoreCase);
            if(fresh.Length==0||!LobbyPrefs.NotifyFriendRequests)return;
            lobby.Toast(fresh.Length==1?Loc.F("lobby.requests.new_one",fresh[0].nickname):Loc.F(first?"lobby.requests.pending_many":"lobby.requests.new_many",fresh.Length));
            Chime();
        }
        // Ikki notali qisqa "ding": fayl emas, bir marta hisoblanadi. Umumiy ovoz balandligiga bo'ysunadi.
        void Chime()
        {
            if(chime==null)
            {
                const int rate=44100;int length=rate/2;var samples=new float[length];
                for(int i=0;i<length;i++)
                {
                    float t=(float)i/rate;
                    samples[i]=(Note(t,659.25f)+Note(t-.11f,987.77f))*Mathf.Clamp01((length-i)/(rate*.04f));
                }
                chime=AudioClip.Create("FriendRequestChime",length,1,rate,false);chime.SetData(samples,0);
            }
            if(chimeSource==null){chimeSource=gameObject.AddComponent<AudioSource>();chimeSource.playOnAwake=false;chimeSource.spatialBlend=0;}
            chimeSource.PlayOneShot(chime,.5f);
        }
        static float Note(float t,float frequency)
        {
            if(t<0)return 0;
            float envelope=Mathf.Clamp01(t/.004f)*Mathf.Exp(-t*7f);
            return (Mathf.Sin(2*Mathf.PI*frequency*t)+.25f*Mathf.Sin(4*Mathf.PI*frequency*t))*envelope*.22f;
        }

        // ------------------------------------------------------------------ Qatorlar

        void Clear()
        {
            foreach(Transform child in rows){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        }
        // Ro'yxat o'rnida xabar (ulanmoqda, oflayn, xato): til almashsa qayta yoziladi.
        void ShowNotice(Func<string> text)
        {
            notice=text;listShown=false;
            RenderMessage(text());
        }
        void RenderMessage(string message)
        {
            fingerprint=null;Clear();
            Label(rows,message,10,20,rows.rect.width-20,150,21).color=new Color32(169,188,207,255);
            rows.sizeDelta=new Vector2(rows.sizeDelta.x,rows.parent.GetComponent<RectTransform>().rect.height);
        }
        /// <summary>Qatorlarni joriy ma'lumot bilan qayta chizadi (masalan, lobby guruhi tarkibi o'zgarganda).</summary>
        public void Redraw(){if(lobby!=null&&listShown&&!Loading)Draw();}
        void Draw()
        {
            notice=null;listShown=true;
            var party=lobby.GetComponent<LobbyParty>();
            string next=mode+Loc.Current+string.Join("|",visible.Select(p=>p.nickname+":"+p.avatarId+":"+p.friendship+":"+p.online+":"+busy.Contains(p.nickname)+":"+(party!=null&&party.IsInviting(p.nickname))+":"+(party!=null&&party.InParty(p.nickname))));
            if(next==fingerprint)return;
            if(visible.Length==0)
            {
                RenderMessage(Loc.T(mode=="online"?"lobby.empty_online":mode=="requests"?"lobby.empty_requests":mode=="find"?"friends.no_results":"lobby.empty_friends"));
                fingerprint=next;return;
            }
            Clear();fingerprint=next;
            float w=rows.rect.width;
            for(int i=0;i<visible.Length;i++)
            {
                var player=visible[i];
                bool member=party!=null&&player.friendship=="friends"&&party.InParty(player.nickname);
                var row=Panel("Friend_"+player.nickname,rows,0,i*72,w,70,Fill);ReferenceSurface.Apply(row,player.online?4:5,14);
                var portrait=Panel("Portrait",row.transform,10,8,54,54,new Color32(56,72,90,255));
                var option=lobby.FindAvatar(player.avatarId);
                if(option?.card!=null)
                {
                    var photo=Rect("Avatar",portrait.transform,0,0,54,54).gameObject.AddComponent<RawImage>();
                    photo.texture=option.card.texture;photo.uvRect=new Rect(.32f,.76f,.36f,.2112f);photo.raycastTarget=false;
                }
                var name=Label(row.transform,player.nickname,74,5,w-180,38,22);
                name.name="FriendNickname";
                name.resizeTextForBestFit=true;name.resizeTextMinSize=17;name.resizeTextMaxSize=22;
                string state=player.friendship=="incoming"?"friends.status.incoming":player.friendship=="outgoing"?"friends.status.outgoing":member?"lobby.party.member":player.online?"common.online":"common.offline";
                Label(row.transform,Loc.T(state),74,36,w-90,27,17).color=player.online||member?new Color32(53,211,139,255):new Color32(154,155,167,255);
                bool idle=!busy.Contains(player.nickname);
                if(player.friendship=="incoming")
                {
                    ActionButton(row.transform,w-98,acceptIcon,"Accept","lobby.friend.accept_hint",()=>Change(player,true),idle);
                    ActionButton(row.transform,w-50,removeIcon,"Remove","lobby.friend.decline_hint",()=>ConfirmRemove(player),idle);
                }
                else if(player.friendship=="outgoing")
                    ActionButton(row.transform,w-50,removeIcon,"Remove","lobby.friend.cancel_hint",()=>ConfirmRemove(player),idle);
                else if(mode=="remove"&&player.friendship=="friends")
                    ActionButton(row.transform,w-50,removeIcon,"Remove","lobby.friend.remove_hint",()=>ConfirmRemove(player),idle);
                else if(player.friendship!="friends")
                    ActionButton(row.transform,w-50,addIcon,"Add","lobby.friend.add_hint",()=>Change(player,false),idle);
                else if(player.online&&party!=null&&!member)
                    ActionButton(row.transform,w-50,inviteIcon!=null?inviteIcon:addIcon,"InviteParty","lobby.party.invite_hint",()=>party.Invite(player.nickname),!party.IsInviting(player.nickname),InviteFill,InviteGlyph);
            }
            rows.sizeDelta=new Vector2(w,Mathf.Max(rows.parent.GetComponent<RectTransform>().rect.height,visible.Length*72));
        }
        void ConfirmRemove(PlayerSummary player)
        {
            string kind=player.friendship=="incoming"?"decline":player.friendship=="outgoing"?"cancel":"remove";
            string confirm=kind=="decline"?"friends.decline":kind=="cancel"?"lobby.friend.withdraw":"friends.remove";
            lobby.Dialog.Show(Loc.T("lobby.friend."+kind+"_hint"),Loc.F("lobby.friend."+kind+"_confirm",player.nickname),Loc.T(confirm),()=>{
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
            lobby.Toast(FriendMessage(player.friendship,player.nickname,result));Refresh(false);
        }

        /// <summary>
        /// Do'stlik amali natijasi: muvaffaqiyatda serverdagi yangi munosabat bo'yicha (so'rov yuborildi, do'st
        /// bo'ldingiz, o'chirildi), xatoda server kodi bo'yicha aniq matn. before - amaldan oldingi munosabat.
        /// </summary>
        public static string FriendMessage(string before,string nickname,ApiResult<FriendshipResponse> result)
        {
            if(!result.Ok)return ErrorMessage(result.NetworkError,result.Status,result.Data?.error);
            switch(result.Data.friendship)
            {
                case "friends":return Loc.F("friends.now_friends",nickname);
                case "outgoing":return Loc.F("friends.request_sent",nickname);
                default:return Loc.F(before=="incoming"?"lobby.friend.declined":before=="outgoing"?"lobby.friend.cancelled":"friends.removed",nickname);
            }
        }
        /// <summary>Server xato kodi (Server/src/app.js) yoki HTTP holati bo'yicha o'yinchiga tushunarli matn.</summary>
        public static string ErrorMessage(bool network,long status,string code)
        {
            if(network)return Loc.T("menu.offline");
            switch(code)
            {
                case "requests_disabled":return Loc.T("friends.requests_disabled");
                case "too_many_pending":return Loc.T("friends.too_many");
                case "not_found":return Loc.T("lobby.friend.not_found");
                case "self":return Loc.T("lobby.friend.self");
                case "no_request":return Loc.T("lobby.friend.no_request");
                case "invalid_query":return Loc.T("lobby.friend.invalid_query");
                case "too_many_requests":return Loc.T("lobby.error.rate_limited");
                case "unauthorized":return Loc.T("party.error.unauthorized");
            }
            if(status==429)return Loc.T("lobby.error.rate_limited");
            if(status==401)return Loc.T("party.error.unauthorized");
            return Loc.T(status>=500?"lobby.error.server":"common.server_error");
        }

        // ------------------------------------------------------------------ Yordamchilar

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
        void ActionButton(Transform parent,float x,Sprite icon,string name,string hint,Action action,bool enabled,Color? fill=null,Color? glyph=null)
        {
            var image=Panel(name,parent,x,12,38,38,fill??Accent);image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.interactable=enabled;button.onClick.AddListener(()=>action());
            var mark=Panel("Icon",image.transform,9,9,20,20,glyph??Color.white);mark.sprite=icon;mark.type=Image.Type.Simple;
            Sounds(button);
            if(tooltip!=null)LobbyIconHint.Attach(image.gameObject,tooltip,hint);
        }
        /// <summary>Ish vaqtida yaratilgan tugma ham boshqalardek ovoz beradi (UiSounds faqat sahna ochilganda ulaydi).</summary>
        public static void Sounds(Button button)
        {
            if(!button.TryGetComponent(out UiSoundHook _))button.gameObject.AddComponent<UiSoundHook>();
            button.onClick.AddListener(UiSounds.PlayClick);
        }
    }
}

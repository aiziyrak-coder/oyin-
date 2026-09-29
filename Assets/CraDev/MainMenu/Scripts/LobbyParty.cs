using System.Collections;
using System.Collections.Generic;
using CraDev.CharacterCreation;
using CraDev.Online;
using CraDev.Wardrobe;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    public sealed class LobbyParty : MonoBehaviour
    {
        [SerializeField] MainMenuScreen lobby;
        [SerializeField] Font font;
        // Yuqori o'ng boshqaruvdagi "Lobbydan chiqish" tugmasi (builder); eski sahnada bo'lmasa ish vaqtida yaratiladi
        [SerializeField] Button leaveButton;
        sealed class Occupant
        {
            public AvatarViewer viewer;public Transform head;public GameObject model;public RectTransform label;public Text name,status;public Image[] bars;public PartyMember member;
            // Ovoz holati belgilari va salom pufagi ("Assalomu alaykum!")
            public LobbyVoiceIcon mic,speaker;public CanvasGroup bubble;public Text bubbleText;public bool greetPending;public float greetStart=-100;
        }
        readonly Dictionary<int,Occupant> occupants=new Dictionary<int,Occupant>();
        readonly HashSet<string> shownInvites=new HashSet<string>();
        Transform partyRoot;
        RectTransform overlay;
        Button leave;
        bool runtimeLeave;
        LobbyVoice voice;
        string lastRoom;
        float heartbeat;
        int ping=-1;
        float lastContact=-100;
        // Barcha party so'rovlari bitta navbatda ketma-ket yuboriladi: heartbeat paytida bosilgan taklif yo'qolmaydi
        // va eski heartbeat javobi yangiroq natijani (qabul qilish, chiqish) ustidan yozib yubormaydi.
        readonly Queue<(string action,PartyRequest request)> queue=new Queue<(string,PartyRequest)>();
        readonly HashSet<string> inviting=new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        string memberList;
        int screenWidth,screenHeight;
        float canvasScale;
        Canvas canvas;
        public PartyState State {get;private set;}
        public int OccupiedCount=>occupants.Count;
        // X bo'yicha xavfsiz yo'laklar, chuqurlik va mehmonlarning o'rni lobbyga qarab aralashadi.
        // Bir xil roomId barcha mijozlarda bir xil joylashuv beradi, heartbeat joyni o'zgartirmaydi.
        public static Vector3[] Formation(string roomId)
        {
            uint hash=2166136261;foreach(char c in roomId??"")hash=unchecked((hash^c)*16777619);
            var random=new System.Random((int)(hash&0x7fffffff));
            var lanes=new[]{-1.68f,-.84f,.84f,1.68f};
            for(int i=3;i>0;i--){int j=random.Next(i+1);(lanes[i],lanes[j])=(lanes[j],lanes[i]);}
            var positions=new Vector3[5];
            for(int i=1;i<5;i++)
            {
                float depth=(i%2==0?1.05f:.2f)+(float)random.NextDouble()*.3f;
                positions[i]=new Vector3(lanes[i-1]+((float)random.NextDouble()-.5f)*.045f,0,depth);
            }
            return positions;
        }
        IEnumerator Start()
        {
            while(lobby.Viewer.CurrentModel==null)yield return null;
            partyRoot=new GameObject("StandingParty").transform;
            overlay=Rect("PartyNameplates",transform,Vector2.zero,Vector2.zero);
            overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.sizeDelta=Vector2.zero;
            overlay.pivot=new Vector2(.5f,.5f);
            overlay.SetSiblingIndex(1);
            voice=GetComponent<LobbyVoice>();
            // Mikrofon/karnay almashtirilsa holat darhol serverga (nameplate belgilari) yuboriladi
            if(voice!=null)voice.StateChanged+=()=>heartbeat=0;
            if(leaveButton!=null)leave=leaveButton;
            else
            {
                runtimeLeave=true;
                leave=Rect("LeaveParty",transform,new Vector2(0,115),new Vector2(220,48)).gameObject.AddComponent<Button>();
                // Lobby sahifasi ustida, lekin sozlamalar, toast, dialog va qorayish (fader) ostida chiziladi
                var home=lobby.Current;
                leave.transform.SetSiblingIndex(home!=null&&home.transform.parent==transform?home.transform.GetSiblingIndex()+1:lobby.Dialog.transform.GetSiblingIndex());
                var lr=leave.GetComponent<RectTransform>();lr.anchorMin=lr.anchorMax=lr.pivot=new Vector2(.5f,1);lr.anchoredPosition=new Vector2(0,-115);
                var fill=leave.gameObject.AddComponent<Image>();fill.color=new Color(.55f,.15f,.19f,.92f);leave.targetGraphic=fill;
                var label=Label(leave.transform,"",Vector2.zero,new Vector2(220,48),20);
                label.rectTransform.anchorMin=label.rectTransform.anchorMax=label.rectTransform.pivot=new Vector2(.5f,.5f);
                label.rectTransform.anchoredPosition=Vector2.zero;
                LobbyFriendsPanel.Sounds(leave);
            }
            leave.onClick.AddListener(()=>{
                if(ModalWindow.AnyOpen||lobby.SettingsOpen)return;
                lobby.Dialog.Show(Loc.T("voice.leave_lobby"),Loc.T("lobby.party.leave_confirm"),Loc.T("voice.leave_lobby"),()=>Enqueue("leave",new PartyRequest()),Loc.T("common.cancel"));
            });
            Apply(new PartyState{host=true,members=new[]{new PartyMember{nickname=PlayerProfile.Nickname,avatarId=PlayerProfile.AvatarId,outfit=PlayerProfile.Outfit,seat=0,pingMs=-1}}});
            heartbeat=0;
            StartCoroutine(PingLoop());
            while(true)
            {
                if(queue.Count>0)
                {
                    var (action,request)=queue.Dequeue();
                    yield return Command(action,request);
                    if(action=="invite"&&inviting.Remove(request.nickname??""))lobby.FriendsPanel?.Redraw();
                    continue;
                }
                if(Time.realtimeSinceStartup>=heartbeat)
                {
                    heartbeat=Time.realtimeSinceStartup+3;
                    yield return Command("heartbeat",new PartyRequest{pingMs=ping,micOn=voice!=null&&voice.MicOn,speakerOn=voice!=null&&voice.SpeakerOn});
                    continue;
                }
                yield return null;
            }
        }
        // Ping ilgari party heartbeat'ining vaqti edi: u navbatdagi boshqa so'rovlar, bazadagi ish, JSON va
        // ovoz so'rovlari bilan birga o'lchanardi va sakrab turardi. Endi alohida, bazaga tegmaydigan GET /api/ping
        // har 2 soniyada o'lchanadi; birinchi natija (TLS ulanish ochilishi) tashlanadi, ko'rsatiladigani oxirgi
        // 7 ta o'lchovning medianasi (bitta sekin javob raqamni buzmaydi).
        const int PingWindow=7;
        readonly List<int> pingSamples=new List<int>();
        IEnumerator PingLoop()
        {
            bool warm=false;
            var wait=new WaitForSecondsRealtime(2);
            while(true)
            {
                bool ok=false;int ms=0;
                yield return lobby.Api.Ping((success,elapsed)=>{ok=success;ms=elapsed;});
                if(ok)
                {
                    if(warm)
                    {
                        pingSamples.Add(ms);if(pingSamples.Count>PingWindow)pingSamples.RemoveAt(0);
                        var sorted=new List<int>(pingSamples);sorted.Sort();ping=sorted[sorted.Count/2];
                    }
                    warm=true;
                }
                else{warm=false;pingSamples.Clear();ping=-1;}
                yield return wait;
            }
        }
        /// <summary>O'zimizning ping (ms, median); o'lchanmagan bo'lsa -1.</summary>
        public int PingMs=>ping;
        /// <summary>Do'stni lobbyga chaqirish: navbatga qo'yiladi, hozirgi heartbeat tugashi bilan yuboriladi.</summary>
        public void Invite(string nickname)
        {
            if(string.IsNullOrEmpty(nickname)||!inviting.Add(nickname))return;
            Enqueue("invite",new PartyRequest{nickname=nickname});
            lobby.FriendsPanel?.Redraw();
        }
        public bool IsInviting(string nickname)=>nickname!=null&&inviting.Contains(nickname);
        public bool InParty(string nickname)=>State?.members!=null&&System.Array.Exists(State.members,m=>string.Equals(m.nickname,nickname,System.StringComparison.OrdinalIgnoreCase));
        void Enqueue(string action,PartyRequest request)=>queue.Enqueue((action,request));
        // Mehmon yo'laklari (±1.68 m) ekranda nameplate'lari bilan do'stlar paneli (ochiq holatda) va o'ng ustun
        // orasiga sig'adigan koeffitsient. 16:9 va kengroq ekranda 1 (dizayn joylashuvi), torroqda kichrayadi.
        float LaneScale()
        {
            var camera=lobby.Stage!=null?lobby.Stage.Camera:null;
            if(camera==null||Screen.width<=0||camera.transform.position.z>=0)return 1;
            var home=lobby.Current;if(canvas==null)canvas=lobby.GetComponent<Canvas>();
            var drawer=home!=null?home.GetComponent<LobbyFriendsDrawer>():null;
            var rail=home!=null?home.transform.Find("LobbyRightRail") as RectTransform:null;
            // Panellar topilmasa: 16:9 dagi ekran ulushini saqlash (kadr torayganicha siqiladi)
            if(drawer==null||rail==null||canvas==null)return Mathf.Clamp(camera.aspect/(16f/9),.45f,1);
            float scale=canvas.scaleFactor,plate=(110+8)*scale/Screen.width;
            var corners=new Vector3[4];rail.GetWorldCorners(corners);
            float left=drawer.ExpandedRight*scale/Screen.width+plate,right=corners[0].x/Screen.width-plate;
            // z=0 tekisligida kadrning yarim kengligi (metr); ekran ulushi: .5+(x-kameraX)/(2*half)
            float camX=camera.transform.position.x;
            float half=-camera.transform.position.z*Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad)*camera.aspect;
            float fitLeft=(.5f-left)*2*half/(OuterLane+camX),fitRight=(right-.5f)*2*half/(OuterLane-camX);
            return Mathf.Clamp(Mathf.Min(1,fitLeft,fitRight),.45f,1);
        }
        const float OuterLane=1.68f;
        // Oyna o'lchami yoki UI masshtabi o'zgarsa joylashuv darhol qayta hisoblanadi, heartbeat kutilmaydi.
        void Update()
        {
            if(State==null||overlay==null)return;
            if(canvas==null)canvas=lobby.GetComponent<Canvas>();
            float scale=canvas!=null?canvas.scaleFactor:1;
            if(Screen.width==screenWidth&&Screen.height==screenHeight&&Mathf.Approximately(scale,canvasScale))return;
            screenWidth=Screen.width;screenHeight=Screen.height;canvasScale=scale;
            Apply(State);
        }
        static string ErrorText(ApiResult<PartyState> result)
        {
            if(result.NetworkError)return Loc.T("party.error.network");
            string key="party.error."+result.Data?.error;
            if(!string.IsNullOrEmpty(result.Data?.error)&&Loc.Has(key))return Loc.T(key);
            if(result.Status==429)return Loc.T("party.error.too_many_requests");
            if(result.Status==401)return Loc.T("party.error.unauthorized");
            return Loc.T(result.Status>=500?"party.error.server_error":"common.server_error");
        }
        IEnumerator Command(string action,PartyRequest request)
        {
            ApiResult<PartyState> result=default;
            yield return lobby.Api.Party(PlayerProfile.Token,action,request,r=>result=r);
            if(!result.Ok)
            {
                if(action!="heartbeat")lobby.Toast(ErrorText(result));
                yield break;
            }
            lastContact=Time.realtimeSinceStartup;
            Apply(result.Data);
            // Guruh tarkibi o'zgarsa do'stlar paneli "Lobbyingizda" belgisini va taklif tugmalarini yangilaydi
            string members=string.Join("|",System.Array.ConvertAll(result.Data.members??System.Array.Empty<PartyMember>(),m=>m.nickname));
            if(members!=memberList){memberList=members;lobby.FriendsPanel?.Redraw();}
            if(action=="invite")lobby.Toast(Loc.T("party.sent"));
            if(action=="accept")lobby.Toast(Loc.T("party.joined"));
            if(result.Data.invitations!=null&&!ModalWindow.AnyOpen&&!lobby.SettingsOpen)
                foreach(var invite in result.Data.invitations)
                    if(shownInvites.Add(invite.id))
                    {
                        lobby.Dialog.Show(Loc.T("party.invite"),Loc.F("party.invite_from",invite.nickname),Loc.T("party.accept"),
                            ()=>Enqueue("accept",new PartyRequest{invitationId=invite.id}),Loc.T("party.decline"),
                            ()=>Enqueue("decline",new PartyRequest{invitationId=invite.id}));
                        break;
                    }
            // Faqat joriy takliflar saqlanadi: xotira o'sib ketmaydi.
            var active=new HashSet<string>();if(result.Data.invitations!=null)foreach(var i in result.Data.invitations)active.Add(i.id);
            shownInvites.IntersectWith(active);
        }
        public void Apply(PartyState state)
        {
            State=state;var active=new HashSet<int>();
            // Salom: shu guruhga yangi qo'shilgan a'zo salom beradi. O'zimiz boshqa guruhga qo'shilsak - o'zimiz
            // (qolganlar "yangi" emas). Lobby birinchi ochilganda (lastRoom yo'q) hech kim salom bermaydi.
            bool roomChanged=state.roomId!=lastRoom,firstState=lastRoom==null;
            if(!string.IsNullOrEmpty(state.roomId))lastRoom=state.roomId;
            var positions=Formation(state.roomId??PlayerProfile.Nickname);
            float lanes=LaneScale();
            foreach(var member in state.members??System.Array.Empty<PartyMember>())
            {
                if(member.seat<0||member.seat>=5)continue;
                active.Add(member.seat);
                if(occupants.TryGetValue(member.seat,out var old)&&old.member.nickname!=member.nickname){Remove(old);occupants.Remove(member.seat);}
                if(!occupants.TryGetValue(member.seat,out var o))
                {
                    bool local=member.nickname==PlayerProfile.Nickname;
                    var option=lobby.FindAvatar(member.avatarId);if(option==null)continue;
                    o=new Occupant{viewer=local?lobby.Viewer:lobby.Viewer.Replica(partyRoot,option)};
                    o.viewer.LockRotation=true;o.label=Rect("Nameplate_"+member.seat,overlay,Vector2.zero,new Vector2(220,58));
                    o.label.pivot=new Vector2(.5f,0);
                    o.name=Label(o.label,"",new Vector2(0,0),new Vector2(220,30),23);
                    o.status=Label(o.label,"",new Vector2(8,30),new Vector2(145,25),17);
                    o.bars=new Image[3];for(int j=0;j<3;j++){o.bars[j]=Rect("Ping_"+j,o.label,new Vector2(162+j*10,48-j*5),new Vector2(6,7+j*5)).gameObject.AddComponent<Image>();o.bars[j].raycastTarget=false;}
                    o.mic=VoiceIcon(o.label,0,new Vector2(194,37));o.speaker=VoiceIcon(o.label,1,new Vector2(214,37));
                    var bubble=Rect("Greeting",o.label,new Vector2(0,-50),new Vector2(220,42));
                    var bubbleFill=bubble.gameObject.AddComponent<Image>();bubbleFill.color=new Color(.1f,.11f,.13f,.86f);bubbleFill.raycastTarget=false;
                    o.bubble=bubble.gameObject.AddComponent<CanvasGroup>();o.bubble.alpha=0;o.bubble.blocksRaycasts=false;
                    o.bubbleText=Label(bubble,"",Vector2.zero,new Vector2(220,42),20);o.bubbleText.color=new Color(1,.93f,.72f);
                    bubble.gameObject.SetActive(false);
                    if(!firstState&&(roomChanged?local&&!state.host:!local))o.greetPending=true;
                    occupants.Add(member.seat,o);
                }
                o.member=member;
                // O'zimiz: avatar mahalliy profildan (saqlangandan keyin eski heartbeat javobi qaytarib yubormaydi);
                // avatar studiyasi ochiq bo'lsa qoralama avatarga tegilmaydi va qahramonni aylantirish mumkin.
                bool self=o.viewer==lobby.Viewer,studio=self&&lobby.AvatarStudioOpen;
                string avatarId=self?PlayerProfile.AvatarId:member.avatarId;
                var wanted=lobby.FindAvatar(avatarId);
                if(!studio&&wanted!=null&&o.viewer.CurrentOption?.id!=avatarId)o.viewer.SetAvatar(wanted);
                if(self)o.viewer.LockRotation=!studio;
                if(member.nickname!=PlayerProfile.Nickname)o.viewer.SetOutfit(Outfit.FromJson(member.outfit));
                var root=o.viewer.ModelRoot;
                var position=positions[member.seat];
                var camera=lobby.Stage.Camera;
                // Perspektiva uzoqdagi personajlarni qo'shnisining ustiga siljitmasin. Mehmonlar yo'lagi tor ekranda
                // (16:10, 4:3, kichik ekran) do'stlar paneli va o'ng ustun orasidagi bo'sh joyga siqiladi.
                float squeeze=member.seat==0?1:lanes;
                position.x=camera.transform.position.x+(position.x-camera.transform.position.x)*squeeze*
                    (position.z-camera.transform.position.z)/(-camera.transform.position.z);
                root.position=position;
                root.rotation=Quaternion.Euler(0,180-Mathf.Sign(position.x)*6,0);
                root.localScale=Vector3.one;
            }
            var remove=new List<int>();foreach(var pair in occupants)if(!active.Contains(pair.Key)){Remove(pair.Value);remove.Add(pair.Key);}
            foreach(int key in remove)occupants.Remove(key);
            leave.gameObject.SetActive(occupants.Count>1||!state.host);
        }
        /// <summary>Oxirgi holatni qayta qo'llaydi (avatar studiyasi yopilganda: joy, burilish, avatar).</summary>
        public void Refresh(){if(State!=null&&partyRoot!=null)Apply(State);}
        // Replika yo'q qilinganda AvatarViewer.OnDestroy material nusxalari va teksturalarini o'zi bo'shatadi.
        void Remove(Occupant o){if(o.viewer!=lobby.Viewer)Destroy(o.viewer.gameObject);Destroy(o.label.gameObject);}
        void LateUpdate()
        {
            if(overlay==null)return;
            // Avatar studiyasida faqat o'yinchining o'z qahramoni ko'rinadi (party signali davom etadi)
            bool studio=lobby.AvatarStudioOpen;
            if(partyRoot.gameObject.activeSelf==studio)partyRoot.gameObject.SetActive(!studio);
            if(overlay.gameObject.activeSelf==studio)overlay.gameObject.SetActive(!studio);
            if(studio){if(leave.gameObject.activeSelf)leave.gameObject.SetActive(false);return;}
            foreach(var pair in occupants)
            {
                var o=pair.Value;
                if(o.model!=o.viewer.CurrentModel)
                {
                    o.model=o.viewer.CurrentModel;
                    o.head=null;
                    if(o.model!=null)
                    {
                        foreach(var bone in o.model.GetComponentsInChildren<Transform>())if(bone.name=="Bip01 Head"){o.head=bone;break;}
                        var animator=o.model.GetComponent<Animator>();
                        if(animator!=null && animator.runtimeAnimatorController!=null)
                        {animator.Update(0);animator.Play(0,0,(pair.Key*.213f)%1f);}
                    }
                }
                if(o.greetPending&&o.model!=null&&o.head!=null)
                {
                    o.greetPending=false;o.greetStart=Time.time;
                    var anim=o.model.GetComponent<LobbyGreeting>();if(anim==null)anim=o.model.AddComponent<LobbyGreeting>();
                    anim.Play();
                }
                if(o.head==null)continue;
                // Salom pufagi: animatsiya bilan bir vaqtda, oxirida so'nadi
                float greetAge=Time.time-o.greetStart;
                bool greeting=greetAge>=0&&greetAge<LobbyGreeting.Duration;
                if(o.bubble.gameObject.activeSelf!=greeting)o.bubble.gameObject.SetActive(greeting);
                if(greeting)
                {
                    o.bubbleText.text=Loc.T("greet.hello");
                    o.bubble.alpha=Mathf.Min(1,greetAge/.3f,(LobbyGreeting.Duration-greetAge)/.5f);
                }
                var point=lobby.Stage.Camera.WorldToScreenPoint(o.head.position+Vector3.up*.19f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay,point,null,out var pos);
                o.label.anchorMin=o.label.anchorMax=new Vector2(.5f,.5f);o.label.anchoredPosition=pos;
                o.name.text=o.member.nickname;
                bool online=Time.realtimeSinceStartup-lastContact<12&&o.member.online;
                int latency=o.member.nickname==PlayerProfile.Nickname?ping:o.member.pingMs;
                o.status.text=Loc.T(online?"common.online":"common.offline")+(online&&latency>=0?" · "+latency+" ms":"");
                var color=!online||latency<0?Color.gray:latency<=100?new Color(.22f,.88f,.52f):latency<=200?new Color(1,.78f,.22f):new Color(1,.3f,.3f);
                o.status.color=color;int strength=!online||latency<0?0:latency<=100?3:latency<=200?2:1;
                for(int i=0;i<3;i++)o.bars[i].color=i<strength?color:new Color(.35f,.36f,.38f,.7f);
                // Ovoz belgilari: o'zimiz - mahalliy holat (darhol), boshqalar - server holati; gapirayotgan yashil
                bool self=o.viewer==lobby.Viewer;
                bool micOn=self?voice!=null&&voice.MicOn:o.member.micOn,speakerOn=self?voice!=null&&voice.SpeakerOn:o.member.speakerOn;
                bool speaking=voice!=null&&(self?voice.SelfSpeaking:voice.IsSpeaking(o.member.nickname));
                var offColor=new Color(1,.45f,.45f,.95f);
                o.mic.Off=!micOn;o.mic.color=!micOn?offColor:speaking?new Color(.35f,1,.6f):Color.white;
                o.speaker.Off=!speakerOn;o.speaker.color=speakerOn?Color.white:offColor;
                o.name.color=speaking?new Color(.55f,1,.75f):Color.white;
            }
            if(runtimeLeave)leave.GetComponentInChildren<Text>().text=Loc.T("voice.leave_lobby");
        }
        static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(pos.x,-pos.y);r.sizeDelta=size;return r;
        }
        static LobbyVoiceIcon VoiceIcon(Transform parent,int kind,Vector2 pos)
        {
            var icon=Rect(kind==0?"Mic":"Speaker",parent,pos,new Vector2(18,18)).gameObject.AddComponent<LobbyVoiceIcon>();
            icon.kind=kind;icon.raycastTarget=false;
            var shadow=icon.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.8f);shadow.effectDistance=new Vector2(1,-1);
            return icon;
        }
        Text Label(Transform parent,string value,Vector2 pos,Vector2 size,int fontSize)
        {
            var t=Rect("Text",parent,pos,size).gameObject.AddComponent<Text>();t.font=font;t.fontSize=fontSize;t.text=value;t.alignment=TextAnchor.MiddleCenter;t.color=Color.white;t.raycastTarget=false;
            var shadow=t.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.85f);shadow.effectDistance=new Vector2(1,-1);return t;
        }
        void OnDestroy(){if(partyRoot!=null)Destroy(partyRoot.gameObject);}
    }
}

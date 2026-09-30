using System;
using System.Collections;
using System.Collections.Generic;
using CraDev.Online;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CraDev.World
{
    /// <summary>Two-player shootout, same shared world. Server controls seats, choices, score and ball timing.</summary>
    public sealed class WorldPenalty : MonoBehaviour
    {
        [SerializeField] Font font;
        [SerializeField] Font boldFont;
        [SerializeField] Sprite rounded;
        static readonly Color Ink = new Color(.96f,.97f,.94f), Muted = new Color(.67f,.74f,.71f);
        static readonly Color Dark = new Color(.035f,.065f,.06f,.96f), Accent = new Color(.56f,.85f,.59f);
        readonly List<(Text label,string key)> localized = new List<(Text,string)>();
        WorldNetwork network;
        WorldPlayerController player;
        WorldHud hud;
        PenaltyBallRenderer ball;
        PenaltySnapshot state;
        RectTransform overlay, joinPanel, controls, aimArea, target, shooterPanel, keeperPanel;
        Text prompt, scoreboard, status, round, role, clock, powerLabel, footer, joinNote, historyLabel;
        Button join, shoot, left, centre, right, leave, rematch;
        Text leaveLabel, rematchLabel;
        Slider power;
        string session = "", errorKey;
        float nextPoll, snapshotAt, errorUntil, confirmUntil;
        float aimX = .65f, aimY = .45f;
        bool polling, sending;
        int generation;
        public bool IsOpen => overlay && overlay.gameObject.activeSelf;
        public PenaltySnapshot CurrentState => state;
        public bool ActionPending => sending;
        public Vector3 BallPosition => ball != null ? ball.Position : new Vector3(64,.11f,68);
        public bool IsParticipant => state != null && PlayerProfile.PublicId > 0 &&
            (state.player1?.publicId == PlayerProfile.PublicId || state.player2?.publicId == PlayerProfile.PublicId);
        bool IsShooter => IsParticipant && state.shooterId == PlayerProfile.PublicId;
        bool Committed => state != null && (IsShooter ? state.shooterReady : state.keeperReady);
        bool CanChoose => IsOpen && IsParticipant && state.phase == "aiming" && !Committed && !sending && network && network.Connected;
        double ServerNow => state == null ? 0 : state.serverTime + (Time.unscaledTime - snapshotAt) * 1000d;

        public void Configure(Font regular, Font bold, Sprite panel) { font=regular; boldFont=bold; rounded=panel; }
        void Start()
        {
            network = FindFirstObjectByType<WorldNetwork>(); player = FindFirstObjectByType<WorldPlayerController>();
            hud = GetComponent<WorldHud>(); if (!hud) hud = FindFirstObjectByType<WorldHud>();
            if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ball = new PenaltyBallRenderer(); Build(); Loc.Changed += Refresh; Refresh();
        }
        void Update()
        {
            if (!player) return;
            if (!network) network = FindFirstObjectByType<WorldNetwork>();
            string active = network && network.Connected ? network.SessionId : "";
            if (session != active)
            {
                session=active; state=null; generation++; nextPoll=0; sending=false;
                if (string.IsNullOrEmpty(active)) Close(); Refresh();
            }
            ball.Draw(state,ServerNow);
            bool near = Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(64,43))<=8;
            bool free = !IsOpen && !player.Paused && (near || IsParticipant);
            prompt.gameObject.SetActive(free);
            if (free && UsePressed()) Open();
            if (IsOpen)
            {
                if (!IsParticipant && !near) Close();
                if (CanChoose)
                {
                    if (IsShooter && ShotPressed()) SubmitShot();
                    else if (!IsShooter)
                    {
                        // Goalkeeper faces south: their left is world-east (+X).
                        if (LeftPressed()) SubmitDive(1);
                        else if (RightPressed()) SubmitDive(-1);
                        else if (ShotPressed()) SubmitDive(0);
                    }
                }
                clock.text = state != null && state.phase=="aiming" ? Mathf.CeilToInt(Mathf.Max(0,(float)((state.deadline-ServerNow)/1000)))+" s" : "";
                if (errorKey != null && Time.unscaledTime>errorUntil) { errorKey=null; Refresh(); }
                if (confirmUntil>0 && Time.unscaledTime>confirmUntil) { confirmUntil=0; Refresh(); }
            }
            if (!network || !network.Connected || polling || sending || Time.unscaledTime<nextPoll) return;
            float distance = Vector3.Distance(player.transform.position,new Vector3(64,0,68));
            if (IsOpen || IsParticipant || distance<65) StartCoroutine(Poll());
            else nextPoll=Time.unscaledTime+1;
        }
        public void Open()
        {
            if (!overlay || !network || !network.Connected) return;
            float distance=Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(64,43));
            if (!IsParticipant && distance>8) return;
            overlay.gameObject.SetActive(true); overlay.SetAsLastSibling();
            if (hud) hud.SetInteraction(true); else player.SetPaused(true);
            nextPoll=0; errorKey=null; confirmUntil=0; Refresh();
        }
        public void Close()
        {
            if (!IsOpen) return;
            overlay.gameObject.SetActive(false); confirmUntil=0;
            if (hud) hud.SetInteraction(false); else if (player) player.SetPaused(false);
        }
        IEnumerator Poll()
        {
            polling=true; string expected=session; int stamp=generation;
            ApiResult<PenaltySnapshot> response=default;
            yield return network.Api.PenaltyState(PlayerProfile.Token,expected,value=>response=value);
            polling=false; nextPoll=Time.unscaledTime+((IsOpen || IsParticipant) ? .15f : .3f);
            if (expected!=session || stamp!=generation) yield break;
            if (response.Ok) Accept(response.Data); else if (IsOpen) Error(response);
        }
        void Accept(PenaltySnapshot snapshot)
        {
            if (snapshot==null || string.IsNullOrEmpty(snapshot.phase)) return;
            snapshot.Normalize();
            if (state!=null && (snapshot.version<state.version || snapshot.serverTime<state.serverTime)) return;
            bool newTurn=state==null || state.turn!=snapshot.turn || state.phase!=snapshot.phase;
            state=snapshot; snapshotAt=Time.unscaledTime;
            if (newTurn) confirmUntil=0;
            Refresh();
        }
        public void JoinMatch() => Send("join");
        public void SubmitShot()
        {
            if (!CanChoose || !IsShooter) return;
            Send("shoot",new PenaltyRequest { aimX=aimX,aimY=aimY,power=power.value });
        }
        public void SubmitDive(int direction)
        {
            if (!CanChoose || IsShooter) return;
            Send("dive",new PenaltyRequest { direction=direction });
        }
        public void SetAim(float x,float y) { aimX=Mathf.Clamp(x,-1.12f,1.12f); aimY=Mathf.Clamp(y,0,1.08f); PaintAim(); }
        void Send(string action,PenaltyRequest request=null)
        {
            if (sending || !network || !network.Connected || state==null) return;
            request=request??new PenaltyRequest(); request.sessionId=session; request.version=state.version; request.turn=state.turn;
            StartCoroutine(Act(action,request));
        }
        IEnumerator Act(string action,PenaltyRequest request)
        {
            sending=true; Refresh(); int stamp=generation;
            ApiResult<PenaltySnapshot> response=default;
            yield return network.Api.PenaltyAction(PlayerProfile.Token,action,request,value=>response=value);
            if (stamp!=generation || request.sessionId!=session) yield break;
            sending=false; nextPoll=0;
            if (response.Ok) { errorKey=null; Accept(response.Data); }
            else Error(response);
            Refresh();
        }
        void Error(ApiResult<PenaltySnapshot> response)
        {
            string code=response.Data?.error;
            errorKey=response.NetworkError || response.Status>=500 ? "penalty.error" : response.Status==429 ? "penalty.wait" :
                code=="arena_full" ? "penalty.full" : code=="activity_busy" ? "penalty.busy" : code=="too_far" ? "penalty.far" : "penalty.changed";
            errorUntil=Time.unscaledTime+4; Refresh();
        }
        void Leave()
        {
            if (state!=null && (state.phase=="aiming"||state.phase=="flight"||state.phase=="result") && Time.unscaledTime>=confirmUntil)
            { confirmUntil=Time.unscaledTime+5; Refresh(); return; }
            confirmUntil=0; Send("leave");
        }
        void Refresh()
        {
            if (!scoreboard) return;
            foreach (var entry in localized) entry.label.text=Loc.T(entry.key);
            string first=state?.player1?.nickname??Loc.T("penalty.empty"),second=state?.player2?.nickname??Loc.T("penalty.empty");
            scoreboard.text=first+"     "+(state?.score1??0)+"  :  "+(state?.score2??0)+"     "+second;
            round.text=state==null ? "" : Loc.F(state.attempts1>=5&&state.attempts2>=5?"penalty.sudden":"penalty.round",state.attempts1,state.attempts2);
            role.text=Loc.T(IsParticipant ? IsShooter?"penalty.shooter":"penalty.keeper":"penalty.spectator");
            status.text=Status();
            bool participant=IsParticipant;
            joinPanel.gameObject.SetActive(!participant);
            controls.gameObject.SetActive(participant);
            join.interactable=!sending&&state!=null&&(state.player1==null||state.player2==null);
            joinNote.text=Loc.T(state?.player1!=null&&state?.player2!=null?"penalty.full":"penalty.rules");
            shooterPanel.gameObject.SetActive(participant&&IsShooter&&state.phase=="aiming");
            keeperPanel.gameObject.SetActive(participant&&!IsShooter&&state.phase=="aiming");
            shoot.interactable=CanChoose&&IsShooter; power.interactable=CanChoose&&IsShooter;
            left.interactable=centre.interactable=right.interactable=CanChoose&&!IsShooter;
            aimArea.GetComponent<Image>().raycastTarget=CanChoose&&IsShooter;
            leave.interactable=!sending;
            leaveLabel.text=Loc.T(confirmUntil>Time.unscaledTime?"penalty.confirm":"penalty.leave");
            bool voted=state?.resetVotes!=null&&Array.IndexOf(state.resetVotes,PlayerProfile.PublicId)>=0;
            rematch.gameObject.SetActive(participant&&(state.phase=="finished"||state.phase=="abandoned"));
            rematch.interactable=!sending&&!voted;
            rematchLabel.text=Loc.T(voted?"penalty.voted":"penalty.rematch");
            footer.text=Loc.T(errorKey??(Committed&&state?.phase=="aiming"?"penalty.committed":participant?"penalty.closed_note":"penalty.fair"));
            footer.color=errorKey!=null?new Color(1,.64f,.4f):Muted;
            var attempts=state?.history??Array.Empty<PenaltyAttempt>();
            string line="";
            for (int i=Mathf.Max(0,attempts.Length-10);i<attempts.Length;i++) line+=(attempts[i].result=="goal"?"●":"○")+"  ";
            historyLabel.text=line; PaintAim();
        }
        string Status()
        {
            if (state==null) return Loc.T("penalty.loading");
            if (state.phase=="aiming") return Loc.T(Committed?"penalty.committed":IsShooter?"penalty.aim":IsParticipant?"penalty.keeper_hint":"penalty.fair");
            if (state.phase=="result") return Loc.T("penalty."+state.result);
            if (state.phase=="finished"||state.phase=="abandoned")
            {
                string name=state.player1?.publicId==state.winner?state.player1.nickname:state.player2?.publicId==state.winner?state.player2.nickname:"";
                return Loc.T("penalty."+state.phase)+"  ·  "+(state.winner>0?Loc.F("penalty.won",name):Loc.T("penalty.draw"));
            }
            return Loc.T("penalty."+state.phase);
        }
        void PaintAim()
        {
            if (!target || !powerLabel) return;
            target.anchorMin=target.anchorMax=new Vector2((aimX/1.12f+1)*.5f,aimY/1.08f);
            target.anchoredPosition=Vector2.zero;
            powerLabel.text=Loc.T("penalty.power")+"  "+Mathf.RoundToInt(power.value*100)+"%";
        }
        void Build()
        {
            prompt=Label("PenaltyPrompt",transform,21,Ink); Centre(prompt.rectTransform,new Vector2(0,-390),new Vector2(740,45));
            prompt.alignment=TextAnchor.MiddleCenter; localized.Add((prompt,"penalty.prompt"));
            prompt.gameObject.AddComponent<Shadow>().effectDistance=new Vector2(1,-1);
            overlay=Panel("PenaltyOverlay",transform,new Color(0,0,0,.04f)); Stretch(overlay);
            var top=Panel("PenaltyScorePanel",overlay,Dark); Anchor(top,new Vector2(.5f,1),new Vector2(0,-24),new Vector2(860,155));
            var title=Label("PenaltyTitle",top,14,Accent,true); Place(title.rectTransform,22,12,650,24); localized.Add((title,"penalty.title"));
            Button("PenaltyClose",top,700,10,140,30,"penalty.close",Close,out _);
            scoreboard=Label("PenaltyScore",top,26,Ink,true); Place(scoreboard.rectTransform,22,40,800,40); scoreboard.alignment=TextAnchor.MiddleCenter;
            round=Label("PenaltyRound",top,14,Muted); Place(round.rectTransform,22,85,810,24); round.alignment=TextAnchor.MiddleCenter;
            historyLabel=Label("PenaltyHistory",top,16,Accent); Place(historyLabel.rectTransform,22,112,800,24); historyLabel.alignment=TextAnchor.MiddleCenter;
            var bottom=Panel("PenaltyControls",overlay,Dark); Anchor(bottom,new Vector2(.5f,0),new Vector2(0,24),new Vector2(1080,282)); controls=bottom;
            role=Label("PenaltyRole",bottom,14,Accent,true); Place(role.rectTransform,22,14,700,26);
            clock=Label("PenaltyClock",bottom,22,Ink,true); Place(clock.rectTransform,905,14,140,32); clock.alignment=TextAnchor.MiddleRight;
            status=Label("PenaltyStatus",bottom,21,Ink,true); Place(status.rectTransform,22,48,1015,50);
            shooterPanel=Panel("PenaltyShooterControls",bottom,Color.clear); Place(shooterPanel,22,108,1015,125);
            aimArea=Panel("PenaltyAimGoal",shooterPanel,new Color(.055f,.12f,.08f)); Place(aimArea,0,0,342,112);
            aimArea.GetComponent<Image>().sprite=null;
            for (int i=0;i<7;i++) { var line=Panel("NetVertical"+i,aimArea,new Color(1,1,1,.15f)); Place(line,1+i*56,0,1,112); }
            for (int i=0;i<4;i++) { var line=Panel("NetHorizontal"+i,aimArea,new Color(1,1,1,.15f)); Place(line,0,i*37,342,1); }
            var frame=aimArea.gameObject.AddComponent<Outline>(); frame.effectColor=new Color(.9f,.95f,.91f); frame.effectDistance=new Vector2(2,-2);
            var pointer=aimArea.gameObject.AddComponent<PenaltyAimInput>(); pointer.Selected=point=> { if(CanChoose&&IsShooter) SetAim((point.x*2-1)*1.12f,point.y*1.08f); };
            target=Panel("PenaltyAimTarget",aimArea,Accent); target.sizeDelta=new Vector2(16,16); target.pivot=new Vector2(.5f,.5f);
            powerLabel=Label("PenaltyPower",shooterPanel,17,Ink,true); Place(powerLabel.rectTransform,375,0,330,28);
            power=MakeSlider(shooterPanel); power.value=.72f; power.onValueChanged.AddListener(_=>PaintAim());
            var warning=Label("PenaltyPowerWarning",shooterPanel,12,Muted); Place(warning.rectTransform,375,75,350,42); localized.Add((warning,"penalty.warning"));
            shoot=Button("PenaltyShoot",shooterPanel,752,18,260,64,"penalty.shoot",SubmitShot,out _);
            keeperPanel=Panel("PenaltyKeeperControls",bottom,Color.clear); Place(keeperPanel,22,108,1015,125);
            left=Button("PenaltyDiveLeft",keeperPanel,100,12,260,64,"penalty.left",()=>SubmitDive(1),out _);
            centre=Button("PenaltyDiveCentre",keeperPanel,378,12,260,64,"penalty.center",()=>SubmitDive(0),out _);
            right=Button("PenaltyDiveRight",keeperPanel,656,12,260,64,"penalty.right",()=>SubmitDive(-1),out _);
            leave=Button("PenaltyLeave",bottom,22,240,230,28,null,Leave,out leaveLabel);
            rematch=Button("PenaltyRematch",bottom,650,144,360,50,null,()=>Send("reset"),out rematchLabel);
            footer=Label("PenaltyFooter",overlay,14,Muted); Anchor(footer.rectTransform,new Vector2(.5f,0),new Vector2(0,3),new Vector2(1100,24)); footer.alignment=TextAnchor.MiddleCenter;
            joinPanel=Panel("PenaltyJoinPanel",overlay,Dark); Centre(joinPanel,Vector2.zero,new Vector2(660,276));
            joinNote=Label("PenaltyJoinNote",joinPanel,20,Ink); Place(joinNote.rectTransform,30,28,600,128);
            join=Button("PenaltyJoin",joinPanel,30,186,600,58,"penalty.join",JoinMatch,out _);
            overlay.gameObject.SetActive(false);
        }
        Slider MakeSlider(Transform parent)
        {
            var track=Panel("PenaltyPowerSlider",parent,new Color(.19f,.26f,.22f)); Place(track,375,40,340,22); track.GetComponent<Image>().raycastTarget=true;
            var fill=Panel("Fill",track,Accent); Stretch(fill);
            var handle=Panel("Handle",track,Ink); handle.sizeDelta=new Vector2(18,28);
            var slider=track.gameObject.AddComponent<Slider>(); slider.fillRect=fill; slider.handleRect=handle; slider.targetGraphic=handle.GetComponent<Image>();
            slider.minValue=.35f; slider.maxValue=1; slider.navigation=new Navigation { mode=Navigation.Mode.None }; return slider;
        }
        Text Label(string name,Transform parent,int size,Color color,bool bold=false)
        {
            var text=Rect(name,parent).gameObject.AddComponent<Text>(); text.font=bold&&boldFont?boldFont:font;
            text.fontSize=size; text.fontStyle=bold&&!boldFont?FontStyle.Bold:FontStyle.Normal; text.color=color;
            text.supportRichText=false; text.raycastTarget=false; text.alignment=TextAnchor.MiddleLeft;
            text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Truncate; return text;
        }
        Button Button(string name,Transform parent,float x,float y,float w,float h,string key,Action action,out Text label)
        {
            var rect=Panel(name,parent,new Color(.16f,.28f,.20f)); Place(rect,x,y,w,h); rect.GetComponent<Image>().raycastTarget=true;
            var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=rect.GetComponent<Image>(); button.navigation=new Navigation { mode=Navigation.Mode.None };
            button.onClick.AddListener(()=>action()); label=Label(name+"Label",rect,h<40?13:18,Ink,true); Stretch(label.rectTransform); label.alignment=TextAnchor.MiddleCenter;
            if(key!=null) localized.Add((label,key)); return button;
        }
        RectTransform Panel(string name,Transform parent,Color color)
        {
            var rect=Rect(name,parent); var image=rect.gameObject.AddComponent<Image>(); image.sprite=rounded; image.type=rounded?Image.Type.Sliced:Image.Type.Simple;
            image.pixelsPerUnitMultiplier=5; image.color=color; image.raycastTarget=false; return rect;
        }
        static RectTransform Rect(string name,Transform parent) { var go=new GameObject(name,typeof(RectTransform)); go.layer=5; var r=(RectTransform)go.transform; r.SetParent(parent,false); return r; }
        static void Place(RectTransform r,float x,float y,float w,float h) { r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1); r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); }
        static void Centre(RectTransform r,Vector2 p,Vector2 s) { r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f); r.anchoredPosition=p; r.sizeDelta=s; }
        static void Anchor(RectTransform r,Vector2 a,Vector2 p,Vector2 s) { r.anchorMin=r.anchorMax=r.pivot=a; r.anchoredPosition=p; r.sizeDelta=s; }
        static void Stretch(RectTransform r) { r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero; }
        static bool UsePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.E);
#else
            return false;
#endif
        }
        static bool ShotPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current!=null&&Keyboard.current.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }
        static bool LeftPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current!=null&&(Keyboard.current.aKey.wasPressedThisFrame||Keyboard.current.leftArrowKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.A)||Input.GetKeyDown(KeyCode.LeftArrow);
#else
            return false;
#endif
        }
        static bool RightPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current!=null&&(Keyboard.current.dKey.wasPressedThisFrame||Keyboard.current.rightArrowKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.D)||Input.GetKeyDown(KeyCode.RightArrow);
#else
            return false;
#endif
        }
        void OnDestroy() { Loc.Changed-=Refresh; StopAllCoroutines(); ball?.Dispose(); }
    }

    public sealed class PenaltyAimInput : MonoBehaviour,IPointerDownHandler,IDragHandler
    {
        public Action<Vector2> Selected;
        public void OnPointerDown(PointerEventData data) => Choose(data);
        public void OnDrag(PointerEventData data) => Choose(data);
        void Choose(PointerEventData data)
        {
            var rect=(RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,data.position,data.pressEventCamera,out var p)) return;
            Selected?.Invoke(new Vector2(Mathf.InverseLerp(rect.rect.xMin,rect.rect.xMax,p.x),Mathf.InverseLerp(rect.rect.yMin,rect.rect.yMax,p.y)));
        }
    }
}

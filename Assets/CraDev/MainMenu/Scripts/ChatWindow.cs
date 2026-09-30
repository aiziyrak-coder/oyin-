using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CraDev.Online;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    public class ChatWindow : ModalWindow
    {
        [SerializeField] RectTransform contacts, messages;
        [SerializeField] ScrollRect scroll;
        [SerializeField] InputField input;
        [SerializeField] Text heading, status;
        [SerializeField] Button closeButton,sendButton,olderButton,latestButton;
        [SerializeField] Font font;
        MainMenuScreen lobby;
        string kind,target,ownerToken;
        sealed class Draft { public string text="",key,sentText;public bool sending; }
        readonly Dictionary<string,Draft> drafts=new Dictionary<string,Draft>();
        Draft draft;
        int revision;
        bool reading,opened,browsingOlder;
        readonly List<ChatMessage> history=new List<ChatMessage>();
        protected override void Awake()
        {
            closeButton.onClick.AddListener(Close);sendButton.onClick.AddListener(Send);
            olderButton.onClick.AddListener(()=>{if(!reading&&history.Count>0)StartCoroutine(Read(true));});
            latestButton.onClick.AddListener(Latest);
            input.onValueChanged.AddListener(value=>{if(draft!=null)draft.text=value;RefreshSend();});
            base.Awake();
        }
        protected override void OnDisable(){revision++;StopAllCoroutines();reading=false;base.OnDisable();}
        public override void Close()
        {
            opened=false;revision++;StopAllCoroutines();reading=false;base.Close();
        }
        void RefreshSend(){sendButton.interactable=opened&&input.interactable&&draft!=null&&!draft.sending&&!string.IsNullOrWhiteSpace(input.text);}
        void Latest()
        {
            if(target==null)return;
            revision++;reading=false;browsingOlder=false;history.Clear();Clear(messages);
            latestButton.interactable=false;StartCoroutine(Read(false));
        }
        public void Show(MainMenuScreen screen)
        {
            StopAllCoroutines();revision++;opened=true;lobby=screen;Open();
            if(ownerToken!=PlayerProfile.Token){drafts.Clear();ownerToken=PlayerProfile.Token;}
            draft=null;
            Clear(contacts);Clear(messages);history.Clear();kind=target=null;reading=false;browsingOlder=false;
            heading.text=Loc.T("chat.choose");status.text=Loc.T("common.connecting");input.text="";input.interactable=false;
            sendButton.interactable=false;olderButton.interactable=false;latestButton.interactable=false;
            StartCoroutine(Contacts());StartCoroutine(Poll());
        }
        IEnumerator Contacts()
        {
            float y=0;bool connected=false;
            yield return lobby.Api.Friends(PlayerProfile.Token,r=>{
                if(!r.Ok)return;connected=true;
                foreach(var person in r.Data.friends??Array.Empty<PlayerSummary>())
                    Contact(person.nickname,"direct",person.publicId.ToString("D6"),ref y);
            });
            yield return lobby.Api.GroupsMine(PlayerProfile.Token,r=>{
                if(!r.Ok)return;connected=true;
                foreach(var group in r.Data.groups??Array.Empty<GroupInfo>())Contact("# "+group.name,"group",group.code,ref y);
            });
            contacts.sizeDelta=new Vector2(contacts.sizeDelta.x,Mathf.Max(y,100));
            if(target==null)status.text=Loc.T(!connected?"chat.connection_error":y==0?"chat.empty_contacts":"chat.choose");
        }
        void Contact(string name,string type,string id,ref float y)
        {
            var row=Rect("ChatContact",contacts,0,y,contacts.rect.width,54);y+=62;
            var fill=row.gameObject.AddComponent<Image>();fill.color=new Color32(49,49,48,255);
            var button=row.gameObject.AddComponent<Button>();button.targetGraphic=fill;
            var text=Label(row,name,12,0,row.rect.width-24,54,21);text.alignment=TextAnchor.MiddleLeft;
            button.onClick.AddListener(()=>ChooseConversation(name,type,id));
        }
        void ChooseConversation(string name,string type,string id,bool load=true)
        {
                revision++;kind=type;target=id;heading.text=name;
                string key=type+":"+id;if(!drafts.TryGetValue(key,out draft)){draft=new Draft();drafts[key]=draft;}
                input.text=draft.text;input.interactable=true;RefreshSend();
                history.Clear();Clear(messages);reading=false;browsingOlder=false;latestButton.interactable=false;
                olderButton.interactable=false;if(load)StartCoroutine(Read(false));
        }
        IEnumerator Poll()
        {
            while(true){yield return new WaitForSecondsRealtime(2);if(target!=null&&!reading&&!browsingOlder)yield return Read(false);}
        }
        IEnumerator Read(bool older)
        {
            if(target==null||reading)yield break;
            reading=true;int generation=revision;
            if(older){browsingOlder=true;latestButton.interactable=true;}
            long after=!older&&history.Count>0?history[history.Count-1].id:0;
            long before=older&&history.Count>0?history[0].id:0;
            yield return lobby.Api.ChatMessages(PlayerProfile.Token,kind,target,after,before,r=>{
                if(this==null||generation!=revision)return;
                if(!r.Ok)
                {
                    status.text=Loc.T(r.Status==403?"chat.forbidden":"chat.connection_error");
                    if(r.Status==403){history.Clear();Clear(messages);input.interactable=false;sendButton.interactable=false;olderButton.interactable=false;}
                    return;
                }
                input.interactable=true;RefreshSend();
                status.text="";
                var incoming=r.Data.items??Array.Empty<ChatMessage>();
                foreach(var message in incoming)if(!history.Any(m=>m.id==message.id))history.Add(message);
                history.Sort((a,b)=>a.id.CompareTo(b.id));
                if(history.Count>200){if(older)history.RemoveRange(200,history.Count-200);else history.RemoveRange(0,history.Count-200);}
                if(after==0)olderButton.interactable=r.Data.hasMore;
                if(incoming.Length>0||history.Count==0)DrawMessages(older,after==0&&!older);
            });
            if(generation==revision)reading=false;
        }
        void DrawMessages(bool older,bool initial)
        {
            bool bottom=scroll.verticalNormalizedPosition<.06f;float old=scroll.verticalNormalizedPosition;
            Clear(messages);float y=0;
            foreach(var message in history)
            {
                string time=DateTime.TryParse(message.createdAt,out var date)?date.ToLocalTime().ToString("HH:mm"):"";
                var title=Label(messages,message.nickname+" · "+time,8,y,messages.rect.width-16,28,17);
                title.color=message.publicId==PlayerProfile.PublicId?LobbyPalette.Accent:new Color32(208,193,162,255);y+=30;
                var text=Label(messages,message.text,8,y,messages.rect.width-16,32,23);
                text.alignment=TextAnchor.UpperLeft;text.horizontalOverflow=HorizontalWrapMode.Wrap;
                float height=Mathf.Max(32,text.preferredHeight+6);text.rectTransform.sizeDelta=new Vector2(text.rectTransform.sizeDelta.x,height);y+=height+20;
            }
            messages.sizeDelta=new Vector2(messages.sizeDelta.x,Mathf.Max(y,scroll.viewport.rect.height));
            Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=older?1:initial||bottom?0:old;
        }
        void Send()
        {
            string text=(input.text??"").Trim();if(!opened||draft==null||draft.sending||target==null||text.Length==0||!input.interactable)return;
            var sentDraft=draft;
            if(sentDraft.key==null||sentDraft.sentText!=text){sentDraft.key=Guid.NewGuid().ToString("N");sentDraft.sentText=text;}
            sentDraft.sending=true;RefreshSend();
            var request=new ChatSendRequest{kind=kind,target=target,text=text,clientId=sentDraft.key};
            lobby.StartCoroutine(lobby.Api.ChatSend(PlayerProfile.Token,request,r=>{
                sentDraft.sending=false;
                if(r.Ok){if(sentDraft.text.Trim()==text)sentDraft.text="";sentDraft.key=null;}
                if(this==null||!opened||draft!=sentDraft)return;
                input.text=sentDraft.text;RefreshSend();
                if(!r.Ok){status.text=Loc.T(r.Status==403?"chat.forbidden":r.Status==429?"chat.slow_down":"chat.connection_error");return;}
                Latest();input.ActivateInputField();
            }));
        }
        // Local-only regression checks: never sends a message or changes a server/profile record.
        internal void VerifyForDevelopment(Action<bool,string> check)
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-cradevSingleLobbySmoke")<0)return;
            StopAllCoroutines();
            ChooseConversation("AUDIT_A","direct","000000",false);
            input.text="Saqlanadigan qoralama";draft.key="audit-idempotency-key";
            check(sendButton.interactable,"nonempty chat draft enables send");
            ChooseConversation("AUDIT_B","direct","000001",false);
            check(input.text==""&&!sendButton.interactable,"conversations have separate drafts");
            input.text="Ikkinchi qoralama";
            ChooseConversation("AUDIT_A","direct","000000",false);
            check(input.text=="Saqlanadigan qoralama","switching contacts restores unsent draft");
            int previousRevision=revision;Close();
            check(revision>previousRevision&&!opened&&!reading,"closing immediately invalidates pending chat reads");
            Show(lobby);StopAllCoroutines();ChooseConversation("AUDIT_A","direct","000000",false);
            check(input.text=="Saqlanadigan qoralama"&&draft.key=="audit-idempotency-key","rapid reopen preserves draft and retry key");
            input.text="  ";check(!sendButton.interactable,"whitespace cannot be sent");
            for(int n=1;n<=30;n++)history.Add(new ChatMessage{id=n,nickname="AUDIT",text="<b>literal</b> "+new string('x',100),createdAt="2026-09-30T00:00:00Z"});
            DrawMessages(false,true);
            check(scroll.verticalNormalizedPosition<.01f,"initial chat history opens at newest message");
            check(messages.GetComponentsInChildren<Text>().All(t=>!t.supportRichText),"message markup is never interpreted");
            browsingOlder=true;latestButton.interactable=true;DrawMessages(true,false);
            check(scroll.verticalNormalizedPosition>.99f&&latestButton.interactable,"older history has explicit return-to-latest control");
            drafts.Remove("direct:000000");drafts.Remove("direct:000001");Show(lobby);
        }
        static void Clear(Transform parent){foreach(Transform child in parent){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.gameObject.layer=5;rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);return rect;
        }
        Text Label(Transform parent,string text,float x,float y,float w,float h,int size)
        {
            var label=Rect("ChatText",parent,x,y,w,h).gameObject.AddComponent<Text>();label.font=font;label.fontSize=size;
            label.supportRichText=false;label.raycastTarget=false;label.color=Color.white;label.text=text;return label;
        }
    }
}

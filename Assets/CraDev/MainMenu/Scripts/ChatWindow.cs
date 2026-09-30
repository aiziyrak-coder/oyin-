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
        [SerializeField] Button closeButton,sendButton,olderButton;
        [SerializeField] Font font;
        MainMenuScreen lobby;
        string kind,target,pendingKey,pendingText;
        int revision;
        bool sending,reading;
        readonly List<ChatMessage> history=new List<ChatMessage>();
        protected override void Awake()
        {
            closeButton.onClick.AddListener(Close);sendButton.onClick.AddListener(Send);
            olderButton.onClick.AddListener(()=>{if(!reading&&history.Count>0)StartCoroutine(Read(true));});
            base.Awake();
        }
        protected override void OnDisable(){revision++;StopAllCoroutines();reading=false;base.OnDisable();}
        public void Show(MainMenuScreen screen)
        {
            lobby=screen;Open();
            Clear(contacts);Clear(messages);history.Clear();kind=target=null;reading=false;sending=false;
            heading.text=Loc.T("chat.choose");status.text=Loc.T("common.connecting");input.text="";
            sendButton.interactable=false;olderButton.interactable=false;
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
            button.onClick.AddListener(()=>{
                if(sending)return;
                revision++;kind=type;target=id;heading.text=name;input.text="";pendingKey=null;
                history.Clear();Clear(messages);reading=false;sendButton.interactable=true;
                olderButton.interactable=false;StartCoroutine(Read(false));
            });
        }
        IEnumerator Poll()
        {
            while(true){yield return new WaitForSecondsRealtime(2);if(target!=null&&!reading)yield return Read(false);}
        }
        IEnumerator Read(bool older)
        {
            if(target==null||reading)yield break;
            reading=true;int generation=revision;
            long after=!older&&history.Count>0?history[history.Count-1].id:0;
            long before=older&&history.Count>0?history[0].id:0;
            yield return lobby.Api.ChatMessages(PlayerProfile.Token,kind,target,after,before,r=>{
                if(this==null||generation!=revision)return;
                if(!r.Ok){status.text=Loc.T(r.Status==403?"chat.forbidden":"chat.connection_error");return;}
                status.text="";
                var incoming=r.Data.items??Array.Empty<ChatMessage>();
                foreach(var message in incoming)if(!history.Any(m=>m.id==message.id))history.Add(message);
                history.Sort((a,b)=>a.id.CompareTo(b.id));
                if(history.Count>200){if(older)history.RemoveRange(200,history.Count-200);else history.RemoveRange(0,history.Count-200);}
                if(after==0)olderButton.interactable=r.Data.hasMore;
                if(incoming.Length>0||history.Count==0)DrawMessages(older);
            });
            if(generation==revision)reading=false;
        }
        void DrawMessages(bool older)
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
            Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=older?1:bottom?0:old;
        }
        void Send()
        {
            string text=(input.text??"").Trim();if(sending||target==null||text.Length==0)return;
            if(pendingKey==null||pendingText!=text){pendingKey=Guid.NewGuid().ToString("N");pendingText=text;}
            int generation=revision;sending=true;sendButton.interactable=false;
            var request=new ChatSendRequest{kind=kind,target=target,text=text,clientId=pendingKey};
            lobby.StartCoroutine(lobby.Api.ChatSend(PlayerProfile.Token,request,r=>{
                if(this==null||generation!=revision)return;
                sending=false;sendButton.interactable=true;
                if(!r.Ok){status.text=Loc.T(r.Status==403?"chat.forbidden":r.Status==429?"chat.slow_down":"chat.connection_error");return;}
                if(input.text.Trim()==text)input.text="";pendingKey=null;
                if(!reading)StartCoroutine(Read(false));input.ActivateInputField();
            }));
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

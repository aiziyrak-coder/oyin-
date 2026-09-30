using CraDev.MainMenu;
using UnityEngine;
using UnityEngine.UI;
using static CraDev.EditorTools.UiBuild;
namespace CraDev.EditorTools
{
    static partial class CraDevSceneBuilder
    {
        static void V2ChatWindow(Transform root)
        {
            var host=CreateRect("ChatWindow",root);Stretch(host);
            var window=host.gameObject.AddComponent<ChatWindow>();Set(window,"group",host.gameObject.AddComponent<CanvasGroup>());Set(window,"font",v2.Medium);
            var shade=CreateFullscreen("ChatShade",host,new Color(0,0,0,.6f));shade.raycastTarget=true;
            var panel=V2Panel(host,"ChatPanel",0,0,1060,780,new Color32(28,29,28,255));panel.raycastTarget=true;
            panel.rectTransform.anchorMin=panel.rectTransform.anchorMax=panel.rectTransform.pivot=new Vector2(.5f,.5f);panel.rectTransform.anchoredPosition=Vector2.zero;
            Set(window,"panel",panel.rectTransform);V2Text(panel.transform,"chat.title",28,22,700,50,32);
            Set(window,"closeButton",V2Button(panel.transform,"×",null,974,22,58,50,localized:false));
            Set(window,"heading",V2Text(panel.transform,"",310,84,700,40,26,false));
            Set(window,"olderButton",V2Button(panel.transform,"chat.older",null,310,134,300,42));
            Set(window,"latestButton",V2Button(panel.transform,"chat.latest",null,632,134,400,42));
            ScrollRect Pane(string name,float x,float w,out RectTransform rows)
            {
                var image=V2Panel(panel.transform,name,x,190,w,440,new Color32(21,23,22,255));image.raycastTarget=true;
                image.gameObject.AddComponent<RectMask2D>();var scroll=image.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.viewport=image.rectTransform;
                scroll.movementType=ScrollRect.MovementType.Clamped;
                rows=CreateRect(name+"Rows",image.transform);PlaceTopLeft(rows,0,0,w,440);scroll.content=rows;return scroll;
            }
            Pane("ChatContacts",28,260,out var contacts);Set(window,"contacts",contacts);
            var scroller=Pane("ChatMessages",310,722,out var messages);Set(window,"messages",messages);Set(window,"scroll",scroller);
            Set(window,"status",V2Text(panel.transform,"",310,638,722,38,18,false));
            var field=V2Search(panel.transform,"chat.placeholder",310,690,550);field.characterLimit=1000;field.targetGraphic=field.GetComponent<Image>();
            field.textComponent.supportRichText=false;field.textComponent.raycastTarget=false;field.placeholder.raycastTarget=false;
            Set(window,"input",field);Set(window,"sendButton",V2Button(panel.transform,"chat.send",null,878,690,154,56));
        }
    }
}

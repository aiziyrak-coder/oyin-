using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CraDev.MainMenu
{
    public class LobbyFriendsDrawer : MonoBehaviour
    {
        [SerializeField] RectTransform panel,handle;
        [SerializeField] CanvasGroup content;
        [SerializeField] Button button;
        [SerializeField] Text arrow;
        [SerializeField] InputField search;
        const float Margin=36;
        float progress=1;
        public bool Collapsed {get;private set;}
        /// <summary>Panel ochiq turganda uning (tugmachasi bilan) o'ng cheti, kanvas birligida (chapdan).</summary>
        public float ExpandedRight=>Margin+panel.rect.width+handle.rect.width;
        void Awake(){button.onClick.AddListener(()=>SetCollapsed(!Collapsed));Apply();}
        public void SetCollapsed(bool value)
        {
            Collapsed=value;content.interactable=content.blocksRaycasts=!value;
            arrow.text=value?"›":"‹";
            if(value){search.DeactivateInputField();if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);}
        }
        void Update()
        {
            float target=Collapsed?0:1;
            if(Mathf.Approximately(progress,target))return;
            progress=Mathf.MoveTowards(progress,target,Time.unscaledDeltaTime/.22f);Apply();
        }
        void Apply()
        {
            float t=progress*progress*(3-2*progress);
            float x=Mathf.Lerp(-panel.rect.width-8,Margin,t);
            panel.anchoredPosition=new Vector2(x,panel.anchoredPosition.y);
            handle.anchoredPosition=new Vector2(Mathf.Lerp(8,Margin+panel.rect.width,t),handle.anchoredPosition.y);
            content.alpha=progress==0?0:1;
        }
    }
}

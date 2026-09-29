using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CraDev.Online;
using CraDev.Wardrobe;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    // Sahifa qobig'i builderda, katalog va server ro'yxatlari ish vaqtida to'ldiriladi.
    public partial class LobbyContent : LobbyPage
    {
        [SerializeField] Font font, bold;
        [SerializeField] Sprite rounded, shirt, shoe, hair;
        [SerializeField] RectTransform content;
        [SerializeField] RectTransform footer;
        [SerializeField] Button[] categories;
        [SerializeField] InputField search;
        [SerializeField] Text status;
        [SerializeField] Text eventTitle;
        RectTransform surface;
        int section, revision;
        float width, zoom = 1;
        Coroutine searchRoutine;
        Outfit draft, saved;
        OutfitSlot slot = OutfitSlot.Top;
        OutfitStyle style;
        GameEvent nextEvent;
        int? onlineCount;
        bool saving;
        bool rendered;
        Language renderedLanguage;
        readonly Dictionary<string,RenderTexture> thumbnails = new Dictionary<string,RenderTexture>();
        readonly Queue<(string key,WardrobeItem item,RawImage image)> previewQueue = new Queue<(string,WardrobeItem,RawImage)>();
        Coroutine previewRoutine;
        static readonly Color Blue = LobbyPalette.Accent;
        static readonly Color Glass = LobbyPalette.Surface;
        static readonly Color Muted = new Color32(180, 189, 203, 255);
        [Serializable] class LookList { public List<string> items = new List<string>(); }

        void Start()
        {
            if (search != null) search.onValueChanged.AddListener(_ =>
            {
                if (searchRoutine != null) StopCoroutine(searchRoutine);
                searchRoutine = StartCoroutine(SearchAfterDelay());
            });
        }
        IEnumerator SearchAfterDelay() { yield return new WaitForSecondsRealtime(.3f); Render(); }
        public override void OnShow()
        {
            if (Id == "wardrobe") { draft = Outfit.FromJson(PlayerProfile.Outfit); saved = draft.Clone(); }
            if (Id == "settings") SettingsShown();
            // Static pages retain their existing hierarchy; live data still refreshes on entry.
            if(!rendered || renderedLanguage!=Loc.Current || Id=="wardrobe" || Id=="settings" || Id=="friends" || Id=="top") Render();
            if(Id=="home" && eventTitle!=null) StartCoroutine(Lobby.Api.Events(result=>{
                var evt=result.Ok?result.Data.items?.FirstOrDefault():null;
                nextEvent=evt; RefreshLiveLabels();
            }));
            if (Id == "world") StartCoroutine(Lobby.Api.Stats(result =>
            {
                onlineCount=result.Ok?result.Data.online:(int?)null; RefreshLiveLabels();
            }));
        }
        public override void OnHide()
        {
            if (Id == "settings") SettingsHidden();
            revision++; StopAllCoroutines(); searchRoutine = null;
            previewRoutine=null;previewQueue.Clear();
            if (Id == "world") SetMapZoom(1);
            if (Id == "wardrobe") Lobby.Viewer.SetOutfit(Outfit.FromJson(PlayerProfile.Outfit));
        }
        protected override void OnLanguageChanged() { if (Lobby != null && isActiveAndEnabled) { keepSettingsScroll = Id == "settings"; Render(); } }
        public override bool OnBack()
        {
            if (Id == "settings") return SettingsBack();
            if (search != null && search.text.Length > 0) { search.text = ""; return true; }
            return false;
        }
        public override bool CanLeave(Action leave)
        {
            if (saving) return false;
            if (Id != "wardrobe" || draft == null || draft.SameAs(saved)) return true;
            Lobby.Dialog.Show(Loc.T("wardrobe.unsaved_title"), Loc.T("wardrobe.unsaved_message"),
                Loc.T("wardrobe.discard"), () => { draft = saved.Clone(); leave(); }, Loc.T("common.cancel"));
            return false;
        }
        public void Choose(string value)
        {
            if(value=="events")
            {
                Lobby.Show("friends");
                var friends=FindObjectsByType<LobbyContent>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(p=>p.Id=="friends");
                if(friends!=null) friends.section=2;
                return;
            }
            if (value == "zoom-in" || value == "zoom-out") { SetMapZoom(Mathf.Clamp(zoom + (value == "zoom-in" ? .15f : -.15f), 1, 1.8f)); return; }
            if (int.TryParse(value, out int index)) { section = index; countryView = false; Render(); }
        }
        void SetMapZoom(float value)
        {
            zoom = value;
            // Karta belgisi va rasm bir xil markaz atrofida kattalashadi.
            foreach (var image in FindObjectsByType<RawImage>(FindObjectsSortMode.None))
                if (image.name == "BackgroundA" || image.name == "BackgroundB")
                    image.uvRect = new Rect(.5f-.5f/zoom,.5f-.5f/zoom,1/zoom,1/zoom);
            foreach (Transform child in transform)
                if (child.name.StartsWith("Map_"))
                {
                    var marker = child.GetComponent<MapMarker>();
                    if (marker == null) marker = child.gameObject.AddComponent<MapMarker>();
                    marker.Zoom(zoom);
                }
        }
        void Render()
        {
            // Sozlamalarda shu bo'lim qayta chizilsa (til, profil) aylantirish joyi saqlanadi
            float scrollY = Id == "settings" && keepSettingsScroll && surface != null ? surface.anchoredPosition.y : 0;
            keepSettingsScroll = false;
            revision++;
            rendered=true;renderedLanguage=Loc.Current;
            previewQueue.Clear();
            if(previewRoutine!=null){StopCoroutine(previewRoutine);previewRoutine=null;}
            RefreshLiveLabels();
            if (categories != null)
                for (int i=0;i<categories.Length;i++) categories[i].GetComponent<Image>().color = i==section ? Blue : Glass;
            if (Id == "world")
            {
                foreach (Transform child in transform)
                    if(child.name.StartsWith("Map_"))
                    {
                        int i=int.Parse(child.name.Substring(4));
                        string key=i<4?new[]{"zone.business","zone.education","zone.shops","zone.entertainment"}[i]:new[]{"world.cat.housing","world.cat.events","world.cat.services"}[i-4];
                        int[] mapSections={3,2,1,4,5,6,7};
                        child.gameObject.SetActive((section==0 || section==mapSections[i]) && (search==null || Loc.T(key).IndexOf(search.text,StringComparison.OrdinalIgnoreCase)>=0));
                    }
                return;
            }
            if(content == null) return;
            Begin();
            switch(Id)
            {
                case "wardrobe": Wardrobe(); break;
                case "shops": Shops(); break;
                case "education": Places(false); break;
                case "entertainment": Places(true); break;
                case "business": Business(); break;
                case "friends": Friends(); break;
                case "top": Ranking(); break;
                case "settings":
                    Settings();
                    if (scrollY > 0) surface.anchoredPosition = new Vector2(0, Mathf.Min(scrollY, Mathf.Max(0, surface.sizeDelta.y - content.rect.height)));
                    break;
            }
        }
        void RefreshLiveLabels()
        {
            if(eventTitle!=null)eventTitle.text=nextEvent==null?Loc.T("home.event_none"):nextEvent.title.Value+"  "+nextEvent.StartsLocal.ToString("HH:mm");
            if(status!=null)status.text=onlineCount.HasValue?Loc.F("world.online",onlineCount.Value):Loc.T("world.online_unknown");
        }
        void Begin(float height = 720)
        {
            foreach(Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            if(footer!=null) foreach(Transform child in footer) {child.gameObject.SetActive(false);Destroy(child.gameObject);}
            width = content.rect.width;
            if (content.GetComponent<RectMask2D>() == null) content.gameObject.AddComponent<RectMask2D>();
            if (!content.TryGetComponent(out ScrollRect scroll)) scroll = content.gameObject.AddComponent<ScrollRect>();
            surface = Rect("Rows",content,0,0,width,height);
            scroll.viewport = content; scroll.content=surface; scroll.horizontal=false; scroll.vertical=true;
            scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=45;
            scroll.verticalNormalizedPosition=1;
        }
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var obj=new GameObject(name,typeof(RectTransform)); obj.layer=5;
            var rect=obj.GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(w,h); return rect;
        }
        Text Text(string text,float x,float y,float w,float h,int size=26,Transform parent=null)
        {
            var label=Rect("Label",parent??surface,x,y,w,h).gameObject.AddComponent<Text>();
            label.font=font; label.fontSize=Mathf.Max(17,Mathf.RoundToInt(size*.82f)); label.text=text; label.color=new Color32(229,235,242,255);
            label.alignment=TextAnchor.MiddleLeft; label.raycastTarget=false;
            label.horizontalOverflow=HorizontalWrapMode.Wrap; label.verticalOverflow=VerticalWrapMode.Truncate; return label;
        }
        Image Panel(string name,float x,float y,float w,float h,Color color,Transform parent=null)
        {
            var image=Rect(name,parent??surface,x,y,w,h).gameObject.AddComponent<Image>();
            image.sprite=rounded; image.type=Image.Type.Sliced; image.pixelsPerUnitMultiplier=3;
            image.color=color; image.raycastTarget=false;
            return image;
        }
        Button Button(string text,float x,float y,float w,float h,Action action,bool primary=false,Transform parent=null)
        {
            var image=Panel("Item_"+text,x,y,w,h,primary?Blue:Glass,parent); image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image;
            var colors=button.colors;colors.highlightedColor=new Color(1.15f,1.15f,1.15f);colors.selectedColor=Color.white;colors.colorMultiplier=1;
            colors.pressedColor=new Color(.85f,.88f,.92f);colors.fadeDuration=.14f;button.colors=colors;
            var label=Text(text,14,0,w-28,h,25,image.transform);
            label.resizeTextForBestFit=false;
            if(action!=null) button.onClick.AddListener(()=>action());
            UiSounds.Hook(button);
            return button;
        }
        void Message(string key) => Text(Loc.T(key),30,30,width-60,180,28).color=Muted;
        void ContentHeight(float h) => surface.sizeDelta=new Vector2(width,h);

        void Wardrobe()
        {
            if(draft==null) return;
            float cell=(width-36)/4;
            string[] styles={"all","suit","sport","casual","special"};
            for(int i=0;i<5;i++)
            {
                var s=i==0?OutfitStyle.None:(OutfitStyle)(1<<(i-1));
                Button(Loc.T("wardrobe.style."+styles[i]),i*width/5,0,width/5-4,56,()=>{style=s;Render();},style==s);
            }
            if(section==2) { Text(Loc.T("wardrobe.accessories_soon"),25,95,width-50,200); return; }
            if(section==4 || section==5)
            {
                var looks=LoadLooks();
                int count=section==4?WardrobeCatalog.Looks.Length:looks.items.Count;
                for(int i=0;i<count;i++)
                {
                    var look=section==4?WardrobeCatalog.Looks[i].Outfit:Outfit.FromJson(looks.items[i]);
                    string title=section==4?WardrobeCatalog.Looks[i].Name:Loc.F("wardrobe.saved_look",i+1);
                    Button(title,(i%2)*(width/2),80+(i/2)*100,width/2-10,88,()=>{draft=look.Clone();Lobby.Viewer.SetOutfit(draft);Render();});
                }
                if(section==5) Button(Loc.T("wardrobe.save_look"),0,80+(count+1)/2*100,width-5,70,SaveLook,true);
                int bottom=100+(count+1)/2*100;
                SaveButton(50); ContentHeight(bottom+80); return;
            }
            if(section==0)
            {
                Button(Loc.T("wardrobe.slot.top"),0,65,width/2-6,55,()=>{slot=OutfitSlot.Top;Render();},slot==OutfitSlot.Top);
                Button(Loc.T("wardrobe.slot.bottom"),width/2,65,width/2-6,55,()=>{slot=OutfitSlot.Bottom;Render();},slot==OutfitSlot.Bottom);
            }
            else slot=section==1?OutfitSlot.Shoes:OutfitSlot.Hair;
            var items=WardrobeCatalog.For(slot).Where(i=>style==OutfitStyle.None || (i.Styles&style)!=0).ToArray();
            for(int i=0;i<items.Length;i++)
            {
                var item=items[i];
                var card=Button("",(i%4)*(cell+12),140+(i/4)*190,cell,178,()=>{draft.Set(slot,item.Id,item.Color);Lobby.Viewer.SetOutfit(draft);Render();},draft.Item(slot)==item.Id);
                ColorUtility.TryParseHtmlString("#"+item.Color,out var color);
                string previewKey=Lobby.Viewer.CurrentOption.id+":"+item.Id;
                thumbnails.TryGetValue(previewKey,out var thumbnail);
                var icon=Rect("Garment",card.transform,14,5,cell-28,115).gameObject.AddComponent<RawImage>();
                icon.texture=thumbnail;icon.raycastTarget=false;icon.color=thumbnail==null?Color.clear:Color.white;
                if(thumbnail==null) previewQueue.Enqueue((previewKey,item,icon));
                var label=card.GetComponentInChildren<Text>(); label.text=item.Name; label.fontSize=19;
                label.rectTransform.anchoredPosition=new Vector2(12,-112); label.rectTransform.sizeDelta=new Vector2(cell-24,60);
            }
            float y=150+Mathf.Ceil(items.Length/4f)*190;
            var palette=slot==OutfitSlot.Hair?WardrobeCatalog.HairPalette:WardrobeCatalog.Palette;
            for(int i=0;i<palette.Length;i++)
            {
                string color=palette[i];
                var swatch=Button("",i%8*52,i/8*48,38,38,()=>{
                    var item=WardrobeCatalog.Find(draft.Item(slot))??WardrobeCatalog.For(slot).First();
                    draft.Set(slot,item.Id,color); Lobby.Viewer.SetOutfit(draft); Render();
                },false,footer);
                ColorUtility.TryParseHtmlString("#"+color,out var tint); swatch.GetComponent<Image>().color=tint;
            }
            Button(Loc.T("wardrobe.original"),width-300,0,145,45,()=>{draft=new Outfit();Lobby.Viewer.SetOutfit(draft);Render();},false,footer);
            SaveButton(50); ContentHeight(y);
            if(previewQueue.Count>0) previewRoutine=StartCoroutine(FillPreviews());
        }
        IEnumerator FillPreviews()
        {
            // Give the page its first frame before rendering at most one small preview per frame.
            while(previewQueue.Count>0)
            {
                yield return null;
                var request=previewQueue.Dequeue();
                if(request.image==null)continue;
                if(!thumbnails.TryGetValue(request.key,out var texture))
                    thumbnails[request.key]=texture=Lobby.Viewer.CaptureGarment(request.item);
                request.image.texture=texture;
                request.image.color=texture==null?Color.clear:Color.white;
            }
            previewRoutine=null;
        }
        void SaveButton(float y)
        {
            var button=Button(Loc.T("wardrobe.save"),width-310,y,300,76,()=>{
                if(saving) return;
                saving=true; Render();
                var toSave=draft.Clone();
                Lobby.SaveOutfit(toSave,ok=>{
                    saving=false;
                    if(ok) saved=toSave;
                    Lobby.Toast(Loc.T(ok?"wardrobe.saved":"wardrobe.save_failed"));
                    if(isActiveAndEnabled) Render();
                });
            },true,footer);
            button.GetComponent<RectTransform>().sizeDelta=new Vector2(300,55);
            button.interactable=!saving;
        }
        LookList LoadLooks()
        {
            try { return string.IsNullOrEmpty(PlayerProfile.SavedLooks)?new LookList():JsonUtility.FromJson<LookList>(PlayerProfile.SavedLooks)??new LookList(); }
            catch { return new LookList(); }
        }
        void SaveLook()
        {
            var looks=LoadLooks();
            if(looks.items.Count>=11) {Lobby.Toast(Loc.T("wardrobe.looks_full"));return;}
            looks.items.Add(draft.ToJson()); PlayerProfile.SetSavedLooks(JsonUtility.ToJson(looks));
            Lobby.Toast(Loc.T("wardrobe.look_saved")); Render();
        }
        void OnDestroy() {foreach(var texture in thumbnails.Values) if(texture!=null){texture.Release();Destroy(texture);}}
        // CONTENT
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    // Bitta umumiy material: o'lcham va holat vertexlarda, har panel uchun material yaratilmaydi.
    [ExecuteAlways, RequireComponent(typeof(Image))]
    public sealed class GlassSurface : BaseMeshEffect, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] float radius = 28;
        [SerializeField] bool solid;
        float hover, press;
        bool over, down;
        static Material shared;
        public bool Solid { get => solid; set { solid=value; graphic.SetVerticesDirty(); } }
        public static GlassSurface Apply(Image image, float corner = 28)
        {
            var glass=image.GetComponent<GlassSurface>() ?? image.gameObject.AddComponent<GlassSurface>();
            glass.radius=corner;
            glass.Prepare();
            image.SetVerticesDirty();
            return glass;
        }
        protected override void OnEnable() { base.OnEnable(); Prepare(); }
        void Prepare()
        {
            var image=GetComponent<Image>();
            if(shared==null) shared=Resources.Load<Material>("LobbyGlass");
            if(shared!=null) image.material=shared;
            image.sprite=null;image.type=Image.Type.Simple;
            var canvas=GetComponentInParent<Canvas>();
            if(canvas!=null) canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
        }
        public override void ModifyMesh(VertexHelper mesh)
        {
            if(!IsActive())return;
            var rect=((RectTransform)transform).rect;
            var vertex=new UIVertex();
            for(int i=0;i<mesh.currentVertCount;i++)
            {
                mesh.PopulateUIVertex(ref vertex,i);
                vertex.uv0=new Vector4((vertex.position.x-rect.xMin)/Mathf.Max(1,rect.width),(vertex.position.y-rect.yMin)/Mathf.Max(1,rect.height),0,0);
                vertex.uv1=new Vector4(rect.width,rect.height,Mathf.Min(radius,Mathf.Min(rect.width,rect.height)*.5f),0);
                vertex.uv2=new Vector4(hover,press,solid?1:0,0);
                mesh.SetUIVertex(vertex,i);
            }
        }
        void Update()
        {
            if(!Application.isPlaying)return;
            var button=GetComponent<Selectable>();
            bool interactive=button!=null&&button.IsInteractable();
            float h=Mathf.MoveTowards(hover,over&&interactive?1:0,Time.unscaledDeltaTime*7);
            float p=Mathf.MoveTowards(press,down&&interactive?1:0,Time.unscaledDeltaTime*12);
            if(Mathf.Abs(h-hover)+Mathf.Abs(p-press)<.0001f)return;
            hover=h;press=p;graphic.SetVerticesDirty();
        }
        protected override void OnDisable() { over=down=false;hover=press=0;base.OnDisable(); }
        public void OnPointerEnter(PointerEventData e) {over=true;}
        public void OnPointerExit(PointerEventData e) {over=down=false;}
        public void OnPointerDown(PointerEventData e) {down=true;}
        public void OnPointerUp(PointerEventData e) {down=false;}
    }
}

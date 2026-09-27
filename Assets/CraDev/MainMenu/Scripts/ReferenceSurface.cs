using UnityEngine;
using UnityEngine.UI;
namespace CraDev.MainMenu
{
    [RequireComponent(typeof(Image))]
    public class ReferenceSurface : BaseMeshEffect
    {
        [SerializeField] int style;
        [SerializeField] float radius=24;
        public int Style {get=>style;set{style=value;graphic.SetVerticesDirty();}}
        public static ReferenceSurface Apply(Image image,int style=0,float radius=24)
        {
            var effect=image.GetComponent<ReferenceSurface>()??image.gameObject.AddComponent<ReferenceSurface>();
            image.sprite=null;image.type=Image.Type.Simple;image.color=Color.white;
            image.material=Resources.Load<Material>("ReferenceSurface");
            effect.style=style;effect.radius=radius;effect.Prepare();image.SetVerticesDirty();return effect;
        }
        void Prepare(){var canvas=GetComponentInParent<Canvas>();if(canvas!=null)canvas.additionalShaderChannels|=AdditionalCanvasShaderChannels.TexCoord1;}
        protected override void OnEnable(){base.OnEnable();Prepare();}
        public override void ModifyMesh(VertexHelper mesh)
        {
            if(!IsActive())return;
            var rect=((RectTransform)transform).rect;UIVertex v=default;
            for(int i=0;i<mesh.currentVertCount;i++)
            {
                mesh.PopulateUIVertex(ref v,i);
                v.uv0=new Vector2(Mathf.InverseLerp(rect.xMin,rect.xMax,v.position.x),Mathf.InverseLerp(rect.yMin,rect.yMax,v.position.y));
                v.uv1=new Vector4(rect.width,rect.height,radius,style);mesh.SetUIVertex(v,i);
            }
        }
    }
}

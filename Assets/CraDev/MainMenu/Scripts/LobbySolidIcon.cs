using UnityEngine;
using UnityEngine.UI;
namespace CraDev.MainMenu
{
    public class LobbySolidIcon : MaskableGraphic
    {
        public int kind;
        void Triangle(VertexHelper h,Vector2 a,Vector2 b,Vector2 c)
        {
            int n=h.currentVertCount;var r=rectTransform.rect;
            foreach(var p in new[]{a,b,c})h.AddVert(new Vector3(r.xMin+p.x*r.width,r.yMin+p.y*r.height),color,Vector2.zero);
            h.AddTriangle(n,n+1,n+2);
        }
        void Disc(VertexHelper h,float x,float y,float rx,float ry)
        {
            for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Triangle(h,new Vector2(x,y),new Vector2(x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry),new Vector2(x+Mathf.Cos(b)*rx,y+Mathf.Sin(b)*ry));}
        }
        protected override void OnPopulateMesh(VertexHelper h)
        {
            h.Clear();
            if(kind==0)Triangle(h,new Vector2(.25f,.12f),new Vector2(.88f,.5f),new Vector2(.25f,.88f));
            else if(kind==1)
            {
                Disc(h,.42f,.72f,.16f,.18f);Disc(h,.76f,.69f,.12f,.14f);Disc(h,.42f,.24f,.29f,.25f);Disc(h,.77f,.27f,.18f,.20f);
            }
            else if(kind==2)
            {
                Triangle(h,new Vector2(.2f,.18f),new Vector2(.4f,.70f),new Vector2(.83f,.88f));
                Triangle(h,new Vector2(.2f,.18f),new Vector2(.83f,.88f),new Vector2(.65f,.36f));
            }
            else if(kind==3){Disc(h,.2f,.5f,.065f,.065f);Disc(h,.5f,.5f,.065f,.065f);Disc(h,.8f,.5f,.065f,.065f);}
            else Disc(h,.5f,.5f,.42f,.42f);
        }
    }
}

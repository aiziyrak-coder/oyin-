using UnityEngine;
using UnityEngine.UI;
namespace CraDev.MainMenu
{
    // Vertex-only tint: no blur, framebuffer copies, textures or per-frame work.
    public class LobbyGradient : BaseMeshEffect
    {
        public Color left = new Color32(91,197,133,255);
        public Color right = new Color32(255,215,143,255);
        public override void ModifyMesh(VertexHelper mesh)
        {
            if(!IsActive())return;
            var rect=((RectTransform)transform).rect;
            UIVertex vertex=default;
            for(int i=0;i<mesh.currentVertCount;i++)
            {
                mesh.PopulateUIVertex(ref vertex,i);
                var tint=Color.Lerp(left,right,Mathf.InverseLerp(rect.xMin,rect.xMax,vertex.position.x));
                vertex.color=(Color)vertex.color*tint;mesh.SetUIVertex(vertex,i);
            }
        }
    }
}

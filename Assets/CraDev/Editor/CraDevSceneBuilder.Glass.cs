using CraDev.MainMenu;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static CraDev.EditorTools.UiBuild;

namespace CraDev.EditorTools
{
    static partial class CraDevSceneBuilder
    {
        static void PrepareLobbyGlass()
        {
            const string directory="Assets/CraDev/MainMenu/Resources";
            if(!AssetDatabase.IsValidFolder(directory)) AssetDatabase.CreateFolder("Assets/CraDev/MainMenu","Resources");
            const string path=directory+"/LobbyGlass.mat";
            var shader=Shader.Find("CraDev/UI/LobbyGlass");
            if(shader==null)throw new System.InvalidOperationException("LobbyGlass shader topilmadi");
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)AssetDatabase.CreateAsset(new Material(shader),path);
            else {material.shader=shader;EditorUtility.SetDirty(material);}
        }
        static ConfirmDialog GlassDialog(Transform root)
        {
            var dim=CreateFullscreen("ConfirmDialog",root,new Color(0,0,0,.6f));dim.raycastTarget=true;
            var group=dim.gameObject.AddComponent<CanvasGroup>();
            var panel=V2Panel(dim.transform,"Sheet",0,0,680,350,new Color(.065f,.077f,.098f,.97f));
            Place(panel.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(680,350));
            GlassSurface.Apply(panel,40);
            var title=V2Text(panel.transform,"",40,32,600,55,34,false);title.font=v2.Bold;
            var message=V2Text(panel.transform,"",40,98,600,128,24,false);
            message.alignment=TextAnchor.UpperLeft;message.color=new Color(.77f,.8f,.85f);
            var cancel=V2Button(panel.transform,"",null,32,260,296,62);
            var confirm=V2Button(panel.transform,"",null,352,260,296,62,blue:true);
            var dialog=dim.gameObject.AddComponent<ConfirmDialog>();
            Set(dialog,"group",group);Set(dialog,"panel",panel.rectTransform);
            Set(dialog,"titleText",title);Set(dialog,"messageText",message);
            Set(dialog,"cancelButton",cancel);Set(dialog,"cancelLabel",cancel.GetComponentInChildren<Text>());
            Set(dialog,"confirmButton",confirm);Set(dialog,"confirmLabel",confirm.GetComponentInChildren<Text>());
            return dialog;
        }
    }
}

using System.IO;
using System.Linq;
using CraDev.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static CraDev.EditorTools.UiBuild;

namespace CraDev.EditorTools
{
    static partial class CraDevSceneBuilder
    {
        internal static void BuildWorldSandbox()
        {
            var root=NewUiScene(out var scene);root.name="WorldHUD";
            root.gameObject.AddComponent<GraphicRaycaster>();CreateEventSystem();
            var camera=Camera.main;camera.clearFlags=CameraClearFlags.Skybox;camera.cullingMask=~(1<<5);
            camera.nearClipPlane=.045f;camera.farClipPlane=350;camera.allowHDR=false;
            var player=new GameObject("WorldPlayer",typeof(CharacterController));player.layer=8;
            camera.transform.SetParent(player.transform,false);camera.transform.localPosition=new Vector3(0,1.65f,0);
            var controller=player.AddComponent<WorldPlayerController>();controller.Configure(camera);
            // Tekis maydon: harakat sifatini tekshirish uchun ortiqcha shahar va og'ir aktivlar yo'q.
            var grass=WorldMaterial("Grass",new Color(.32f,.4f,.27f),true);
            var paving=WorldMaterial("PracticeFloor",new Color(.47f,.50f,.46f),true);paving.SetFloat("_Grid",1);EditorUtility.SetDirty(paving);
            var stone=WorldMaterial("Stone",new Color(.57f,.60f,.58f));
            var dark=WorldMaterial("DarkStone",new Color(.22f,.28f,.27f));
            var accent=WorldMaterial("Markers",new Color(.28f,.57f,.43f));
            WorldBox("Ground",new Vector3(0,-.5f,0),new Vector3(500,1,500),grass);
            // Bir tekis collider: choklarda personaj sakrab ketmaydi. Ustki qatlam faqat chizma.
            var pad=WorldBox("PracticeArea",new Vector3(0,-.012f,8),new Vector3(44,.025f,48),paving);
            Object.DestroyImmediate(pad.GetComponent<Collider>());
            WorldBox("Wall",new Vector3(0,1,12),new Vector3(6,2,.5f),stone);
            WorldBox("WallStripe",new Vector3(0,1.1f,11.739f),new Vector3(5.7f,.07f,.016f),accent,false);
            // Cho'kkalab o'tish va bosh ustidagi bo'shliq testi.
            WorldBox("LowCeiling",new Vector3(-10,1.45f,0),new Vector3(4,.2f,4),stone);
            foreach(float x in new[]{-12.1f,-7.9f})WorldBox("CeilingLeg",new Vector3(x,.65f,0),new Vector3(.18f,1.3f,4),dark);
            for(int i=0;i<3;i++)
            {
                float height=(i+1)*.15f;
                WorldBox("Step"+i,new Vector3(10,height*.5f,4+i),new Vector3(3,height,1),stone);
            }
            WorldRamp("GentleRamp",new Vector3(12,0,16),4,7,2,stone);
            WorldRamp("SteepRamp",new Vector3(-12,0,16),3,3,4,dark);
            for(int i=0;i<3;i++)WorldBox("JumpBlock"+i,new Vector3(6+i*2.8f,.25f+i*.1f,-7),new Vector3(1.8f,.5f+i*.2f,1.8f),stone);
            // Vizual yo'nalish belgilarigina: collider o'yinchi harakatiga ta'sir qilmaydi.
            foreach(var p in new[]{new Vector3(-20,0,25),new Vector3(20,0,25),new Vector3(-20,0,-12),new Vector3(20,0,-12)})
            {
                WorldBox("MarkerPost",p+Vector3.up*1.2f,new Vector3(.12f,2.4f,.12f),dark);
                WorldBox("MarkerCap",p+Vector3.up*2.4f,new Vector3(.22f,.15f,.22f),accent);
            }
            var sun=NewLight("WorldSun",null,LightType.Directional,new Color(1,.96f,.87f),1.05f,Quaternion.Euler(48,-32,0));
            sun.shadows=LightShadows.Soft;sun.shadowStrength=.72f;sun.shadowNormalBias=.2f;
            RenderSettings.sun=sun;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.54f,.66f,.79f);RenderSettings.ambientEquatorColor=new Color(.52f,.58f,.62f);RenderSettings.ambientGroundColor=new Color(.3f,.32f,.27f);
            var sky=WorldMaterial("Sky",Color.white,false,"Skybox/Procedural");
            sky.SetFloat("_SunSize",.035f);sky.SetFloat("_AtmosphereThickness",1.15f);sky.SetColor("_SkyTint",new Color(.46f,.53f,.61f));sky.SetColor("_GroundColor",new Color(.44f,.49f,.43f));sky.SetFloat("_Exposure",1.1f);EditorUtility.SetDirty(sky);
            RenderSettings.skybox=sky;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=90;RenderSettings.fogEndDistance=240;RenderSettings.fogColor=new Color(.69f,.77f,.8f);
            var map=new GameObject("MinimapCamera").AddComponent<Camera>();map.orthographic=true;map.orthographicSize=24;
            map.nearClipPlane=.1f;map.farClipPlane=120;map.cullingMask=~((1<<5)|(1<<8));map.clearFlags=CameraClearFlags.SolidColor;map.backgroundColor=new Color(.25f,.33f,.23f);
            map.transform.SetPositionAndRotation(new Vector3(0,60,0),Quaternion.Euler(90,0,0));map.enabled=false;
            var hud=root.gameObject.AddComponent<WorldHud>();var kit=Kit();
            Set(hud,"player",controller);Set(hud,"font",kit.Medium);Set(hud,"rounded",kit.RoundFill);Set(hud,"mapCamera",map);
            Save(scene,WorldScene);
            var scenes=EditorBuildSettings.scenes.Where(s=>s.path!=WorldScene).ToList();scenes.Add(new EditorBuildSettingsScene(WorldScene,true));EditorBuildSettings.scenes=scenes.ToArray();
            AssetDatabase.SaveAssets();Debug.Log("[CraDev] WorldSandbox yaratildi.");
        }
        static Material WorldMaterial(string name,Color color,bool ground=false,string shader=null)
        {
            const string folder="Assets/CraDev/World/Materials";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/CraDev/World","Materials");
            string path=folder+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find(shader??(ground?"CraDev/WorldGround":"Standard")));AssetDatabase.CreateAsset(material,path);}
            if(material.HasProperty("_Color"))material.color=color;
            if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",.15f);
            EditorUtility.SetDirty(material);return material;
        }
        static GameObject WorldBox(string name,Vector3 position,Vector3 size,Material material,bool collider=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=position;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material;go.isStatic=true;
            if(!collider)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        static void WorldRamp(string name,Vector3 position,float width,float length,float height,Material material)
        {
            var go=new GameObject(name);go.transform.position=position;go.isStatic=true;
            var mesh=new Mesh{name=name};float x=width/2;
            mesh.vertices=new[]{new Vector3(-x,0,0),new Vector3(x,0,0),new Vector3(-x,height,length),new Vector3(x,height,length),new Vector3(-x,0,length),new Vector3(x,0,length)};
            mesh.triangles=new[]{0,2,1,1,2,3,0,4,2,1,3,5,2,4,3,3,4,5,0,1,4,1,5,4};mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path="Assets/CraDev/World/Materials/"+name+".asset";var asset=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(asset==null){AssetDatabase.CreateAsset(mesh,path);asset=mesh;}else{EditorUtility.CopySerialized(mesh,asset);Object.DestroyImmediate(mesh);}
            go.AddComponent<MeshFilter>().sharedMesh=asset;go.AddComponent<MeshRenderer>().sharedMaterial=material;go.AddComponent<MeshCollider>().sharedMesh=asset;
        }
    }
}

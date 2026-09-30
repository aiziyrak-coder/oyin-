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
            camera.nearClipPlane=.045f;camera.farClipPlane=450;camera.allowHDR=true;camera.allowMSAA=true;
            var effects=camera.gameObject.AddComponent<WorldImageEffects>();
            Set(effects,"material",WorldMaterial("ImageEffects",Color.white,false,"Hidden/CraDev/WorldImageEffects"));
            var player=new GameObject("WorldPlayer",typeof(CharacterController));player.layer=8;
            camera.transform.SetParent(player.transform,false);camera.transform.localPosition=new Vector3(0,1.65f,0);
            var controller=player.AddComponent<WorldPlayerController>();controller.Configure(camera);
            BuildCityInfrastructure();
            var sun=NewLight("WorldSun",null,LightType.Directional,new Color(1,.985f,.96f),1.15f,Quaternion.Euler(47.8564f,-55.7666f,0));
            sun.shadows=LightShadows.Soft;sun.shadowStrength=.9f;sun.shadowNormalBias=.15f;sun.shadowBias=.025f;
            RenderSettings.sun=sun;
            // Explicit daylight bounce remains deterministic in batch builds; an asynchronous sky GI
            // update can otherwise serialize a black ambient probe and turn the indoor furniture black.
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.48f,.55f,.66f);
            RenderSettings.ambientEquatorColor=new Color(.32f,.35f,.38f);
            RenderSettings.ambientGroundColor=new Color(.21f,.20f,.17f);
            RenderSettings.ambientIntensity=1;
            var sky=WorldMaterial("Sky",Color.white,false,"Skybox/Panoramic");
            sky.SetTexture("_MainTex",WorldTexture("Sky.hdr",false,true));
            sky.SetColor("_Tint",Color.gray);sky.SetFloat("_Exposure",.9f);sky.SetFloat("_Rotation",0);sky.SetFloat("_Mapping",1);sky.SetFloat("_ImageType",0);EditorUtility.SetDirty(sky);
            RenderSettings.skybox=sky;
            RenderSettings.defaultReflectionMode=UnityEngine.Rendering.DefaultReflectionMode.Skybox;RenderSettings.reflectionIntensity=.8f;
            // Yumshoq uzoq masofa tumani HDR osmon ufqiga qo'shiladi, yaqin sirtlarni xiralashtirmaydi.
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=140;RenderSettings.fogEndDistance=430;RenderSettings.fogColor=new Color(.65f,.72f,.76f);
            DynamicGI.UpdateEnvironment();
            BakeWorldReflection();
            // Mini-xarita kamerasini WorldMinimap o'zi yaratadi va sozlaydi (yagona manba): bu yerda kamera qurilmaydi.
            var hud=root.gameObject.AddComponent<WorldHud>();var kit=Kit();
            Set(hud,"player",controller);Set(hud,"font",kit.Medium);Set(hud,"boldFont",kit.SemiBold);Set(hud,"rounded",kit.RoundFill);
            var templateGo = new GameObject("WorldAvatarTemplate");
            var template = templateGo.AddComponent<CraDev.CharacterCreation.AvatarViewer>();
            var modelRoot = new GameObject("TemplateModel").transform; modelRoot.SetParent(templateGo.transform,false);
            Set(template,"turntable",modelRoot); Set(template,"maleIdle",IdleController("m_idle_neutral_01","Idle_Male"));
            Set(template,"femaleIdle",IdleController("f_idle_neutral_01","Idle_Female"));
            Set(template,"facePaint",FacePaintMaterial());Set(template,"outfitPaint",OutfitPaintMaterial());
            Set(template,"glossVariant",GlossVariantMaterial());Set(template,"driveCamera",false);templateGo.SetActive(false);
            var network = root.gameObject.AddComponent<WorldNetwork>();
            Set(network,"player",controller);Set(network,"avatarTemplate",template);Set(network,"font",kit.Medium);
            SetAvatars(network,new System.Collections.Generic.Dictionary<string,Sprite>(),LoadFaceMaps());
            root.gameObject.AddComponent<WorldVoice>();
            var chess = root.gameObject.AddComponent<WorldChess>();
            Set(chess,"font",kit.Medium);Set(chess,"boldFont",kit.SemiBold);Set(chess,"rounded",kit.RoundFill);
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
            var selectedShader=Shader.Find(shader??(ground?"CraDev/WorldGround":"Standard"));
            if(selectedShader==null)throw new System.InvalidOperationException("World shader topilmadi: "+shader);
            if(material==null){material=new Material(selectedShader);AssetDatabase.CreateAsset(material,path);}else material.shader=selectedShader;
            if(material.HasProperty("_Color"))material.color=color;
            if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",.15f);
            EditorUtility.SetDirty(material);return material;
        }
        static Texture2D WorldTexture(string file,bool srgb,bool hdr=false)
        {
            string path="Assets/CraDev/World/Art/"+file;
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new System.IO.FileNotFoundException("World teksturasi topilmadi",path);
            bool normal=file.Contains("_Normal");
            var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            bool changed=importer.textureType!=type||importer.sRGBTexture!=(srgb&&!normal)||importer.maxTextureSize!=(hdr?4096:2048)||importer.mipmapEnabled==hdr||importer.anisoLevel!=(hdr?1:16)||importer.wrapModeU!=TextureWrapMode.Repeat||importer.wrapModeV!=(hdr?TextureWrapMode.Clamp:TextureWrapMode.Repeat);
            importer.textureType=type;importer.sRGBTexture=srgb&&!normal;importer.maxTextureSize=hdr?4096:2048;
            // Panoramaning 0/360 chokida ddx katta bo'ladi: HDRni no-mip bilinear o'qish oq chiziqni yo'qotadi.
            importer.mipmapEnabled=!hdr;importer.anisoLevel=hdr?1:16;importer.filterMode=hdr?FilterMode.Bilinear:FilterMode.Trilinear;
            importer.wrapModeU=TextureWrapMode.Repeat;importer.wrapModeV=hdr?TextureWrapMode.Clamp:TextureWrapMode.Repeat;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.compressionQuality=100;importer.isReadable=false;
            if(changed)importer.SaveAndReimport();else AssetDatabase.WriteImportSettingsIfDirty(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Material WorldPhotoMaterial(string name,string set,float metres,Color tint,float normal,string shader="CraDev/WorldPBR")
        {
            var mat=WorldMaterial(name,tint,false,shader);
            mat.SetTexture("_MainTex",WorldTexture(set+"_Diffuse.jpg",true));
            mat.SetTexture("_BumpMap",WorldTexture(set+"_Normal.jpg",false));
            mat.SetTexture("_Roughness",WorldTexture(set+"_Rough.jpg",false));
            mat.SetTexture("_Occlusion",WorldTexture(set+"_AO.jpg",false));
            if(mat.HasProperty("_Scale"))mat.SetFloat("_Scale",1/metres);
            mat.SetFloat("_NormalStrength",normal);mat.SetFloat("_Smoothness",.8f);
            EditorUtility.SetDirty(mat);return mat;
        }
        static void BakeWorldReflection()
        {
            // Bir marta editor'da olinadi: o'yin davomida har kadr cubemap chizilmaydi.
            var go=new GameObject("WorldEnvironmentReflection");go.transform.position=new Vector3(0,3,0);
            var probe=go.AddComponent<ReflectionProbe>();probe.mode=UnityEngine.Rendering.ReflectionProbeMode.Baked;
            probe.resolution=128;probe.hdr=true;probe.size=new Vector3(500,100,500);probe.cullingMask=0;probe.intensity=.8f;
            const string path="Assets/CraDev/World/Materials/WorldReflection.exr";
            if(Lightmapping.BakeReflectionProbe(probe,path))
            {
                AssetDatabase.ImportAsset(path);probe.bakedTexture=AssetDatabase.LoadAssetAtPath<Texture>(path);
                EditorUtility.SetDirty(probe);
            }
        }
        static GameObject WorldBox(string name,Vector3 position,Vector3 size,Material material,bool collider=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=position;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material;go.isStatic=true;
            if(name!="Ground"&&name!="PracticeArea")
            {
                go.transform.localScale=Vector3.one;go.GetComponent<BoxCollider>().size=size;
                go.GetComponent<MeshFilter>().sharedMesh=WorldBevelMesh(name,size);
            }
            if(!collider)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        static Mesh WorldBevelMesh(string name,Vector3 size)
        {
            // Kollayder o'lchami o'zgarmaydi; faqat ko'rinadigan qirralar tabiiy yorug'lik oladi.
            var vertices=new System.Collections.Generic.List<Vector3>();var normals=new System.Collections.Generic.List<Vector3>();var indices=new System.Collections.Generic.List<int>();
            Vector3 half=size*.5f;float radius=Mathf.Min(.028f,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.15f);Vector3 inner=half-Vector3.one*radius;
            foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
            {
                Vector3 axis=Mathf.Abs(normal.y)>.9f?Vector3.forward:Vector3.up;
                Vector3 u=Vector3.Cross(axis,normal),v=Vector3.Cross(normal,u);
                float hu=Vector3.Dot(new Vector3(Mathf.Abs(u.x),Mathf.Abs(u.y),Mathf.Abs(u.z)),half);
                float hv=Vector3.Dot(new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z)),half);
                var us=new[]{-hu,-hu+radius,hu-radius,hu};var vs=new[]{-hv,-hv+radius,hv-radius,hv};int start=vertices.Count;
                for(int j=0;j<4;j++)for(int i=0;i<4;i++)
                {
                    Vector3 p=Vector3.Scale(normal,half)+u*us[i]+v*vs[j];
                    Vector3 closest=new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));
                    Vector3 n=(p-closest).normalized;vertices.Add(closest+n*radius);normals.Add(n);
                }
                for(int j=0;j<3;j++)for(int i=0;i<3;i++){int a=start+j*4+i;indices.AddRange(new[]{a,a+1,a+4,a+1,a+5,a+4});}
            }
            var mesh=new Mesh{name=name+"Bevel"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
            return SaveWorldGeometry(mesh,name+"Bevel");
        }
        static Mesh SaveWorldGeometry(Mesh mesh,string name)
        {
            string path="Assets/CraDev/World/Materials/"+name+".asset";var asset=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(asset==null){AssetDatabase.CreateAsset(mesh,path);asset=mesh;}else{EditorUtility.CopySerialized(mesh,asset);Object.DestroyImmediate(mesh);}return asset;
        }
        static void WorldRamp(string name,Vector3 position,float width,float length,float height,Material material)
        {
            var go=new GameObject(name);go.transform.position=position;go.isStatic=true;
            var mesh=new Mesh{name=name};float x=width/2;
            mesh.vertices=new[]{new Vector3(-x,0,0),new Vector3(x,0,0),new Vector3(-x,height,length),new Vector3(x,height,length),new Vector3(-x,0,length),new Vector3(x,0,length)};
            var points=mesh.vertices;var faces=new[]{0,2,1,1,2,3,0,4,2,1,3,5,2,4,3,3,4,5,0,1,4,1,5,4};
            mesh.vertices=faces.Select(i=>points[i]).ToArray();mesh.triangles=Enumerable.Range(0,faces.Length).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path="Assets/CraDev/World/Materials/"+name+".asset";var asset=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(asset==null){AssetDatabase.CreateAsset(mesh,path);asset=mesh;}else{EditorUtility.CopySerialized(mesh,asset);Object.DestroyImmediate(mesh);}
            go.AddComponent<MeshFilter>().sharedMesh=asset;go.AddComponent<MeshRenderer>().sharedMaterial=material;go.AddComponent<MeshCollider>().sharedMesh=asset;
        }
    }
}

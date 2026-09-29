using System.Collections.Generic;
using System.Linq;
using CraDev.CharacterCreation;
using CraDev.MainMenu;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static CraDev.EditorTools.UiBuild;

namespace CraDev.EditorTools
{
    static partial class CraDevSceneBuilder
    {
        const string V2Art = "Assets/CraDev/MainMenu/Pages/";
        static UiKit v2;
        static readonly Dictionary<string,Texture2D> v2Textures=new Dictionary<string,Texture2D>();
        static readonly Color V2Blue = LobbyPalette.Accent;
        static readonly Color V2Glass = LobbyPalette.Surface;

        // Faqat lobby qayta quriladi: tekshirilgan avatar niqoblarini qayta pishirmaydi.
        public static void RebuildLobby()
        {
            AssetDatabase.Refresh();
            BuildMainMenu();
            BuildWorldSandbox();
            AssetDatabase.SaveAssets();
            Debug.Log("[CraDev] Lobby V2 sahnasi yaratildi.");
        }

        static Texture2D V2Texture(string name)
        {
            if(v2Textures.TryGetValue(name,out var cached)) return cached;
            string path = V2Art + name + ".png";
            string night = V2Art + "Night/" + name + ".png";
            if (System.IO.File.Exists(night)) path=night;
            if (!System.IO.File.Exists(path)) return null;
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.maxTextureSize = 4096;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                // Full-screen artwork must retain source detail, including future 3840x2160 assets.
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.crunchedCompression = false;
                importer.filterMode = FilterMode.Bilinear;
                var desktop = importer.GetPlatformTextureSettings("Standalone");
                desktop.overridden = true;
                desktop.maxTextureSize = 4096;
                desktop.format = TextureImporterFormat.RGBA32;
                desktop.textureCompression = TextureImporterCompression.Uncompressed;
                desktop.crunchedCompression = false;
                importer.SetPlatformTextureSettings(desktop);
                importer.SaveAndReimport();
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                var source = (TextureImporter)AssetImporter.GetAtPath(path);
                source.GetSourceTextureWidthAndHeight(out int width, out int height);
                if (texture.width != width || texture.height != height)
                    throw new System.InvalidOperationException($"Lobby image was downscaled: {name}, source {width}x{height}, imported {texture.width}x{texture.height}");
                Debug.Log($"[CraDev] Lobby texture {name}: {texture.width}x{texture.height}, {texture.format}; source size preserved.");
            }
            return v2Textures[name] = texture;
        }

        static Image V2Panel(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var image = CreateSliced(name, parent, v2.RoundFill, 8, 24, color);
            PlaceTopLeft(image.rectTransform, x, y, w, h);
            return image;
        }

        static Text V2Text(Transform parent, string text, float x, float y, float w, float h, int size = 26, bool localized = true)
        {
            var label = CreateLabel("Text_" + text, parent, v2.Medium, localized ? "" : text, Mathf.Max(17,Mathf.RoundToInt(size*.88f)), new Color32(229,235,242,255), TextAnchor.MiddleLeft);
            PlaceTopLeft(label.rectTransform, x, y, w, h);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            if (localized && !string.IsNullOrEmpty(text)) Localized(label, text);
            return label;
        }

        static Image V2Icon(Transform parent, string icon, float x, float y, float side)
        {
            var image = CreateImage(icon, parent, LineIcon(icon), Vector2.zero, Vector2.zero, Color.white);
            PlaceTopLeft(image.rectTransform, x, y, side, side);
            return image;
        }

        static Button V2Button(Transform parent, string key, string icon, float x, float y, float w, float h,
            string action = null, string value = null, bool blue = false, bool localized = true)
        {
            var fill = V2Panel(parent, "Button_" + (value ?? key), x, y, w, h, blue ? V2Blue : V2Glass);
            fill.raycastTarget = true;
            var button = TintButton(fill);
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.15f,1.15f,1.15f);
            colors.pressedColor=new Color(.85f,.88f,.92f);colors.selectedColor=Color.white;
            colors.fadeDuration=.14f;button.colors=colors;
            float left = icon == null ? 18 : 54;
            if (icon != null) V2Icon(fill.transform, icon, 18, (h - 24) / 2, 24);
            var label = V2Text(fill.transform, key, left, 0, w - left - 12, h, 24, localized);
            label.name = "Label";
            label.resizeTextForBestFit=false;
            if (action != null)
            {
                var command = fill.gameObject.AddComponent<LobbyCommand>();
                command.action = action; command.value = value;
            }
            return button;
        }

        static RawImage V2Picture(Transform parent, string name, Texture texture, float x, float y, float w, float h, Rect? crop = null)
        {
            if(!name.StartsWith("Background"))
            {
                var mask=CreateSliced(name+"Frame",parent,v2.RoundFill,8,24,Color.white);
                PlaceTopLeft(mask.rectTransform,x,y,w,h);
                mask.gameObject.AddComponent<Mask>().showMaskGraphic=false;
                parent=mask.transform;x=y=0;
            }
            var rect = CreateRect(name, parent);
            PlaceTopLeft(rect, x, y, w, h);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture; image.raycastTarget = false;
            if (crop.HasValue) image.uvRect = crop.Value;
            return image;
        }

        static void BuildMainMenu()
        {
            const string referencePath="Assets/CraDev/MainMenu/Resources/ReferenceSurface.mat";
            var reference=AssetDatabase.LoadAssetAtPath<Material>(referencePath);
            if(reference==null){reference=new Material(Shader.Find("CraDev/ReferenceSurface"));AssetDatabase.CreateAsset(reference,referencePath);}
            reference.SetTexture("_Art",V2Texture("reference-world"));EditorUtility.SetDirty(reference);
            foreach(string name in LobbyEnvironment.Names)
            {
                string path="Assets/CraDev/MainMenu/Resources/LobbyTimes/"+name+".png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null)throw new System.InvalidOperationException("Missing environment asset: "+path);
                importer.textureType=TextureImporterType.Default;importer.maxTextureSize=4096;
                importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.isReadable=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            v2 = Kit();
            v2Textures.Clear();
            var root = NewUiScene(out var scene);
            // Barcha UI o'lchamlari oldingi ko'rinishning 65 foizi, fon esa o'zgarmaydi.
            root.GetComponent<CanvasScaler>().referenceResolution=new Vector2(1920,1080)/.65f;
            root.gameObject.AddComponent<GraphicRaycaster>();
            CreateEventSystem();
            var screen = root.gameObject.AddComponent<MainMenuScreen>();
            Set(screen,"gameplayScene","WorldSandbox");
            var camera = Camera.main;
            var bgCamera = new GameObject("BackgroundCamera").AddComponent<Camera>();
            bgCamera.depth = -1; bgCamera.cullingMask = 1 << 5;
            bgCamera.clearFlags = CameraClearFlags.SolidColor;
            bgCamera.backgroundColor = new Color32(10, 13, 19, 255);
            var bgRoot = new GameObject("BackgroundCanvas", typeof(RectTransform), typeof(Canvas));
            bgRoot.layer = 5;
            var canvas = bgRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = bgCamera; canvas.planeDistance = 10;
            var backgroundA = V2Picture(bgRoot.transform, "BackgroundA", null, 0, 0, 1920, 1080);
            V2Envelope(backgroundA);
            var backgroundB = V2Picture(bgRoot.transform, "BackgroundB", null, 0, 0, 1920, 1080);
            V2Envelope(backgroundB);
            backgroundA.color = Color.white;
            backgroundB.color = new Color(1,1,1,0);
            Set(screen, "backgroundA", backgroundA); Set(screen, "backgroundB", backgroundB);
            const string atmospherePath="Assets/CraDev/MainMenu/Pages/LobbyAtmosphere.mat";
            var atmosphere=AssetDatabase.LoadAssetAtPath<Material>(atmospherePath);
            if(atmosphere==null){atmosphere=new Material(Shader.Find("CraDev/LobbyAtmosphere"));AssetDatabase.CreateAsset(atmosphere,atmospherePath);}
            var environment=root.gameObject.AddComponent<LobbyEnvironment>();
            Set(environment,"lobby",screen);Set(environment,"backgroundA",backgroundA);Set(environment,"backgroundB",backgroundB);Set(environment,"atmosphere",atmosphere);
            const string backdropPath = V2Art + "Backdrop.asset";
            var backdrop = AssetDatabase.LoadAssetAtPath<Texture2D>(backdropPath);
            if(backdrop == null)
            {
                backdrop = new Texture2D(2,2,TextureFormat.RGB24,false);
                AssetDatabase.CreateAsset(backdrop,backdropPath);
            }
            backdrop.SetPixels(new[]{new Color32(9,12,18,255),new Color32(17,22,31,255),new Color32(15,19,27,255),new Color32(29,35,46,255)}.Select(c=>(Color)c).ToArray());
            backdrop.Apply();backdrop.wrapMode=TextureWrapMode.Clamp;EditorUtility.SetDirty(backdrop);
            Set(screen, "defaultBackground", backdrop);

            camera.clearFlags = CameraClearFlags.Depth; camera.depth = 0;
            camera.cullingMask = ~(1 << 5); camera.allowHDR = false;
            var lc = camera.gameObject.AddComponent<LobbyCamera>();
            var turntable = new GameObject("Turntable").transform;
            var sun = NewLight("Sun", null, LightType.Directional, Color.white, 1.2f, Quaternion.Euler(30, -30, 0));
            sun.shadows=LightShadows.Soft;sun.shadowNormalBias=.2f;
            var rim = NewLight("Rim", null, LightType.Directional, Color.white, .7f, Quaternion.Euler(20, 155, 0));
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.fog = false; RenderSettings.skybox = null;
            var stage = turntable.gameObject.AddComponent<LobbyStage>();
            Set(stage, "stageCamera", camera); Set(stage, "lobbyCamera", lc); Set(stage, "turntable", turntable);
            Set(stage, "sun", sun); Set(stage, "rim", rim);
            var shadow=GameObject.CreatePrimitive(PrimitiveType.Quad);
            shadow.name="ShadowCatcher";Object.DestroyImmediate(shadow.GetComponent<Collider>());
            shadow.transform.SetPositionAndRotation(new Vector3(0,.002f,0),Quaternion.Euler(90,0,0));
            shadow.transform.localScale=new Vector3(3,3,3);
            var shadowRenderer=shadow.GetComponent<Renderer>();
            shadowRenderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/CraDev/MainMenu/ShadowCatcher.mat");
            shadowRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Set(stage,"shadowCatcher",shadowRenderer);
            V2Poses(stage);
            var input = CreateFullscreen("AvatarDrag", root, Color.clear);
            input.raycastTarget = true;
            var viewer = input.gameObject.AddComponent<AvatarViewer>();
            Set(viewer, "stageCamera", camera); Set(viewer, "turntable", turntable); Set(viewer, "driveCamera", false);
            Set(viewer, "maleIdle", IdleController("m_idle_neutral_01", "Idle_Male"));
            Set(viewer, "femaleIdle", IdleController("f_idle_neutral_01", "Idle_Female"));
            Set(viewer, "facePaint", FacePaintMaterial()); Set(viewer, "outfitPaint", OutfitPaintMaterial());
            Set(viewer, "glossVariant", GlossVariantMaterial());
            SetArray(viewer, "viewButtons", new Object[0]); SetArray(viewer, "viewFills", new Object[0]); SetArray(viewer, "viewLabels", new Object[0]);
            Set(screen, "viewer", viewer); Set(screen, "stage", stage);
            SetAvatars(screen, AvatarList.ToDictionary(a=>a.Id,a=>LoadSprite("Assets/CraDev/Avatars/Cards/"+a.Id+".png")), LoadFaceMaps());

            var pages = new List<LobbyPage>();
            foreach (string id in new[] { "home", "world", "wardrobe", "shops", "education", "business", "friends", "settings", "top", "entertainment" })
                pages.Add(V2Page(root, id));
            SetArray(screen, "pages", pages.Cast<Object>().ToArray());
            Set(screen,"singleWindow",true);
            V2GameControls(pages.First(p=>p.Id=="home").transform,screen);
            var toast = V2Panel(root, "Toast", 560, 945, 800, 78, V2Glass);
            toast.rectTransform.anchorMin=toast.rectTransform.anchorMax=toast.rectTransform.pivot=new Vector2(.5f,1);
            toast.rectTransform.anchoredPosition=new Vector2(0,-36);
            toast.raycastTarget=false;
            Set(screen, "toastGroup", toast.gameObject.AddComponent<CanvasGroup>());
            Set(screen, "toastText", V2Text(toast.transform, "", 24, 0, 752, 78, 26, false));
            Set(screen, "dialog", FlatDialog(root));
            Set(screen, "fader", CreateFullscreen("Fader", root, Color.black));
            var party=root.gameObject.AddComponent<LobbyParty>();
            Set(party,"lobby",screen);Set(party,"font",v2.Medium);
            foreach(var button in root.GetComponentsInChildren<Button>(true))if(button.name=="LobbyLeaveParty")Set(party,"leaveButton",button);
            AddUiSounds();
            Save(scene, MenuScene);
        }

        // Fon ekranni to'liq qoplaydi, ortiqchasi kesiladi (16:10, 4:3, 21:9 da cho'zilmaydi). Nisbatni
        // MainMenuScreen.Fit har rasm uchun yangilaydi; LobbyCamera ham shu kesishga mos FOV tanlaydi.
        static void V2Envelope(RawImage image)
        {
            var rect = image.rectTransform;
            Stretch(rect); rect.pivot = new Vector2(.5f, .5f);
            var fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9;
        }

        static void V2Poses(LobbyStage stage)
        {
            var so = new SerializedObject(stage);
            var poses = so.FindProperty("poses"); poses.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                // Proportsiyalar manba rasmdagi bosh va oyoq nuqtalaridan hisoblangan.
                float head = i == 0 ? 75.8f / 415 : 46.3f / 309;
                float feet = i == 0 ? 650f / 720 : 296f / 309;
                if(i==0)head=feet-(feet-head)*.65f;
                float x = i == 0 ? 610f / 1280 : 197f / 519;
                float fov = 30, height = 1.85f, viewHeight = height / (feet - head);
                float distance = viewHeight / (2 * Mathf.Tan(fov * Mathf.Deg2Rad / 2));
                var pose = poses.GetArrayElementAtIndex(i);
                pose.FindPropertyRelative("name").stringValue = i == 0 ? "Penthouse" : "Wardrobe";
                pose.FindPropertyRelative("position").vector3Value = new Vector3((.5f-x)*viewHeight*16/9, (feet-.5f)*viewHeight, -distance);
                pose.FindPropertyRelative("rotation").vector3Value = Vector3.zero;
                pose.FindPropertyRelative("fieldOfView").floatValue = fov;
                pose.FindPropertyRelative("imageAspect").floatValue = 16f/9;
                pose.FindPropertyRelative("sunRotation").vector3Value = new Vector3(28,-32,0);
                pose.FindPropertyRelative("sunColor").colorValue = new Color(1f,.92f,.81f);
                pose.FindPropertyRelative("sunIntensity").floatValue = .85f;
                pose.FindPropertyRelative("rimRotation").vector3Value = new Vector3(20,155,0);
                pose.FindPropertyRelative("rimColor").colorValue = new Color(.65f,.8f,1);
                pose.FindPropertyRelative("rimIntensity").floatValue = .4f;
                pose.FindPropertyRelative("shadowStrength").floatValue = .55f;
                pose.FindPropertyRelative("ambientSky").colorValue = new Color(.32f,.36f,.43f);
                pose.FindPropertyRelative("ambientEquator").colorValue = new Color(.24f,.26f,.31f);
                pose.FindPropertyRelative("ambientGround").colorValue = new Color(.12f,.14f,.18f);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void V2Navigation(Transform root, MainMenuScreen screen)
        {
            var nav = CreateRect("Navigation", root); Stretch(nav);
            V2Panel(nav,"NavigationDock",50,24,1820,76,new Color(.045f,.06f,.085f,.96f));
            var brand = V2Button(nav, "NewWorld", null, 78, 36, 280, 52, "page", "home", false, false);
            brand.GetComponent<Image>().color = Color.clear;
            V2Panel(brand.transform,"BrandAccent",0,17,4,18,V2Blue);
            brand.GetComponentInChildren<Text>().font=v2.SemiBold;
            brand.GetComponentInChildren<Text>().fontSize=28;
            PlaceTopLeft(brand.transform.Find("Label").GetComponent<RectTransform>(), 18, 0, 250, 52);
            string[] keys = { "home", "world", "friends", "top", "settings" };
            var buttons = new List<Button>();
            for (int i = 0; i < keys.Length; i++)
                buttons.Add(V2Button(nav, "nav." + keys[i], null, 525 + i * 142, 36, 140, 52));
            var tabs = nav.gameObject.AddComponent<SelectList>();
            foreach(var button in buttons)
            {
                var text=button.GetComponentInChildren<Text>();
                text.resizeTextForBestFit=false;text.fontSize=20;
                text.horizontalOverflow=HorizontalWrapMode.Overflow;text.alignment=TextAnchor.MiddleCenter;
            }
            SetArray(tabs, "items", buttons.Cast<Object>().ToArray());
            SetArray(tabs, "fills", buttons.Select(b => (Object)b.GetComponent<Image>()).ToArray());
            Set(tabs, "normalFill", Color.clear); Set(tabs, "selectedFill", new Color(.14f,.20f,.26f,1));
            Set(screen, "navTabs", tabs);
            var language = V2Button(nav, "UZ", "Globe", 1370, 36, 125, 52, null, null, false, false);
            language.GetComponent<Image>().color = V2Glass;
            Set(language.gameObject.AddComponent<LanguageToggle>(), "label", language.GetComponentInChildren<Text>());
            var bell = V2Button(nav, "", "Bell", 1510, 36, 60, 52, null, null, false, false);
            Set(screen, "bellButton", bell);
            Set(screen, "bellDot", V2Panel(bell.transform, "NotificationDot", 43, 14, 10, 10, new Color32(243,65,95,255)));
            var profile = V2Button(nav, "", "User", 1600, 36, 250, 52, null, null, false, false);
            Set(screen, "profileButton", profile); Set(screen, "profileName", profile.GetComponentInChildren<Text>());
            PlaceTopLeft(profile.GetComponentInChildren<Text>().rectTransform,62,0,165,52);
            Set(screen, "profileIcon", profile.transform.Find("User").GetComponent<Image>());
            Set(screen, "profileThumb", V2Picture(profile.transform, "Avatar", null, 12, 8, 36, 36));
            var blocker = CreateFullscreen("ProfileMenuBlocker", root, Color.clear);
            blocker.raycastTarget = true; Set(screen, "profileMenuBlocker", TintButton(blocker));
            var menu = V2Panel(root, "ProfileMenu", 1510, 110, 340, 260, V2Glass);
            Set(screen, "profileMenu", menu.gameObject.AddComponent<CanvasGroup>());
            var menuButtons = new List<Button>();
            string[] menuKeys = { "wardrobe", "customize", "settings", "quit" };
            string[] icons = { "Shirt", "User", "Gear", "Logout" };
            for (int i = 0; i < 4; i++)
                menuButtons.Add(V2Button(menu.transform, "menu." + menuKeys[i], icons[i], 8, 8 + i * 62, 324, 56));
            SetArray(screen, "profileMenuButtons", menuButtons.Cast<Object>().ToArray());
        }

        static LobbyContent V2Page(Transform root, string id)
        {
            var rect = CreateRect("Page_" + id, root); Stretch(rect);
            var page = rect.gameObject.AddComponent<LobbyContent>();
            Set(page, "pageId", id);
            Set(page, "navTab", System.Array.IndexOf(new[] { "home", "world", "friends", "top", "settings" }, id));
            Set(page, "stagePose", id == "home" ? 0 : id == "wardrobe" ? 1 : -1);
            Set(page, "font", v2.Medium); Set(page, "bold", v2.Bold); Set(page, "rounded", v2.RoundFill);
            Set(page, "shirt", LineIcon("Shirt")); Set(page, "shoe", LineIcon("Shoe")); Set(page, "hair", LineIcon("Hair"));
            // Katta rasmlar faqat ularni ishlatadigan sahifada: bosh sahifa ta'lim/do'stlar rasmlarini xotirada ushlamaydi
            if (id == "education") Set(page, "educationImage", V2Texture("education-cards"));
            if (id == "friends") Set(page, "friendsImage", V2Texture("hero-friends"));
            SetArray(page,"businessIcons",new Object[]{LineIcon("Office"),LineIcon("Coworking"),LineIcon("Rocket"),LineIcon("Coins")});
            SetArray(page,"placeIcons",new Object[]{LineIcon("Music"),LineIcon("Film"),LineIcon("Gamepad"),LineIcon("Ball"),LineIcon("Home"),LineIcon("Music")});
            string bg = id == "world" ? "map" : id;
            // Bosh sahifa fonini LobbyEnvironment soat/ob-havoga qarab birinchi kadrdan qo'yadi: sahnada qo'shimcha
            // to'liq ekranli rasm (sunset-home, clean-home) xotirada turmaydi va ochilishda boshqa rasm miltillamaydi.
            if (id != "home") Set(page, "background", V2Texture("clean-" + bg));
            if (id == "home") { V2GameHome(rect,page); return page; }
            if(id=="settings")
            {
                // Sahifa butun ekranga cho'zilgan: qorong'i fon har qanday ekran nisbatida hammasini qoplaydi va
                // oyna tashqarisiga bosilsa sozlamalar yopiladi. Qolgan hamma narsa markazdagi 1920x1080 ramkada.
                var dim=CreateFullscreen("SettingsBackdrop",rect,new Color(0,0,0,.62f));dim.raycastTarget=true;
                var dimButton=dim.gameObject.AddComponent<Button>();dimButton.transition=Selectable.Transition.None;
                dimButton.navigation=new Navigation{mode=Navigation.Mode.None};
                dim.gameObject.AddComponent<LobbyCommand>().action="close-settings";
                Set(dim.gameObject.AddComponent<UiSoundHook>(),"hover",false);
                var frame=CreateRect("SettingsFrame",rect);
                frame.anchorMin=frame.anchorMax=frame.pivot=new Vector2(.5f,.5f);
                frame.sizeDelta=new Vector2(1920,1080);frame.anchoredPosition=Vector2.zero;
                rect=frame;
                V2Panel(rect,"SettingsSheet",40,140,1830,900,LobbyContent.SettingsSheet).raycastTarget=true;
                V2Button(rect,"","Close",1788,156,60,52,"close-settings").name="SettingsClose";
                Set(page,"chevronLeft",LineIcon("ChevronLeft"));Set(page,"chevronRight",LineIcon("ChevronRight"));
                Set(page,"pencil",LineIcon("Pencil"));Set(page,"resetIcon",LineIcon("Refresh"));
            }
            string prefix = id == "entertainment" ? "fun" : id;
            V2Text(rect, prefix + ".title", 65, 153, id == "friends" ? 660 : 590, 64, 38).font = v2.SemiBold;
            var subtitle = V2Text(rect, prefix + ".subtitle", 68, 217, 650, 38, 23);
            subtitle.color = new Color32(185,199,219,255);

            string[] categories;
            string[] categoryIcons;
            switch (id)
            {
                case "world":
                    categories = new[] { "world.cat.all", "zone.shops", "zone.education", "zone.business", "zone.entertainment", "world.cat.housing", "world.cat.events", "world.cat.services" };
                    categoryIcons = new[] { "Grid", "Bag", "GradCap", "Briefcase", "Gamepad", "Home", "Calendar", "Gear" }; break;
                case "wardrobe":
                    categories = new[] { "wardrobe.cat.clothes", "wardrobe.cat.shoes", "wardrobe.cat.accessories", "wardrobe.cat.hair", "wardrobe.cat.looks", "wardrobe.cat.saved" };
                    categoryIcons = new[] { "Shirt", "Shoe", "Glasses", "Hair", "Mannequin", "Bookmark" }; break;
                case "shops":
                    categories = new[] { "shops.cat.all", "shops.cat.clothes", "shops.cat.electronics", "shops.cat.accessories", "shops.cat.home", "shops.cat.beauty", "shops.cat.sport", "shops.cat.other" };
                    categoryIcons = new[] { "Grid", "Shirt", "Laptop", "Watch", "Sofa", "Sparkle", "Ball", "Dots" }; break;
                case "education":
                    categories = new[] { "education.cat.all", "education.cat.medicine", "education.cat.technology", "education.cat.languages", "education.cat.it", "education.cat.business", "education.cat.art", "education.cat.other" };
                    categoryIcons = new[] { "Grid", "Medical", "Atom", "Language", "Code", "Briefcase", "Palette", "Dots" }; break;
                case "business":
                    categories = new[] { "business.cat.main", "business.cat.offices", "business.cat.coworking", "business.cat.startups", "business.cat.investors", "business.cat.events", "business.cat.services" };
                    categoryIcons = new[] { "Home", "Office", "Coworking", "Rocket", "Coins", "Calendar", "Gear" }; break;
                case "settings":
                    categories = LobbyContent.SettingsSections.Select(s => "settings.section." + s).ToArray();
                    categoryIcons = new[] { "User", "Laptop", "Volume", "Mouse", "Home", "Lock", "Globe", "Help" }; break;
                case "entertainment":
                    categories = new[] { "fun.cat.all", "fun.cat.concerts", "fun.cat.cinema", "fun.cat.games", "fun.cat.sport", "fun.cat.parks" };
                    categoryIcons = new[] { "Grid", "Music", "Film", "Gamepad", "Ball", "Home" }; break;
                default: categories = new string[0]; categoryIcons = new string[0]; break;
            }
            var categoriesButtons = new List<Button>();
            if(categories.Length>0)V2Panel(rect,"Sidebar",49,283,367,categories.Length*64+24,id=="settings"?LobbyContent.SettingsSidebar:new Color(.045f,.06f,.085f,.94f));
            for (int i = 0; i < categories.Length; i++)
                categoriesButtons.Add(V2Button(rect, categories[i], categoryIcons[i], 61, 295+i*64, 343, 56, "choose", i.ToString(), i == 0));
            SetArray(page, "categories", categoriesButtons.Cast<Object>().ToArray());
            if (id == "world")
            {
                V2Map(rect);
                Set(page, "search", V2Search(rect, "world.search", 1340, 155, 510));
                var status = V2Panel(rect, "CityStatus", 1390, 930, 460, 96, V2Glass);
                V2Text(status.transform, "world.center", 24, 6, 410, 40, 24);
                Set(page, "status", V2Text(status.transform, Loc.T("world.online_unknown"), 24, 46, 410, 38, 21,false));
                return page;
            }
            var body = CreateRect("Content", rect);
            float bodyX = id == "wardrobe" ? 1030 : id == "friends" || id == "top" ? 65 : 545;
            PlaceTopLeft(body, bodyX, id == "shops" ? 650 : 315, 1850-bodyX, id == "shops" ? 370 : 720);
            Set(page, "content", body);
            // Sozlamalar: yon menyudan 24 px keyin, sarlavha ostidan boshlanadi (bo'sh ustun qolmaydi)
            if(id=="settings") PlaceTopLeft(body,440,283,1410,747);
            if(id=="wardrobe")
            {
                PlaceTopLeft(body,1030,195,820,708);
                var footer=CreateRect("WardrobeFooter",rect);PlaceTopLeft(footer,1030,915,820,110);
                Set(page,"footer",footer);
            }
            if (id == "shops") Set(page, "search", V2Search(rect, "shops.search", 810, 165, 1035));
            if (id == "friends")
            {
                string[] tabs = { "friends", "groups", "events", "chat" };
                var friendTabs=new List<Button>();
                for (int i=0;i<4;i++) friendTabs.Add(V2Button(rect, "friends.tab."+tabs[i], null, 865+i*245, 183, 239, 52, "choose", i.ToString(), i==0));
                SetArray(page,"categories",friendTabs.Cast<Object>().ToArray());
                Set(page,"search",V2Search(rect,"friends.search",865,265,975));
                PlaceTopLeft(body,65,355,1785,650);
            }
            if (id == "education" || id == "entertainment")
            {
                var hero = V2Picture(rect, "Hero", V2Texture(id == "education" ? "hero-education" : "clean-map"), 545, 168, 1300, 480);
                var box = V2Panel(rect,"HeroInfo",1320,250,480,300,V2Glass);
                V2Text(box.transform,prefix+".hero.title",28,25,420,60,32).font=v2.SemiBold;
                V2Text(box.transform,prefix+".hero.subtitle",28,95,410,65,24);
                V2Button(box.transform,prefix+".visit","ArrowRight",28,210,320,56,"soon",prefix+".title",true);
                V2Text(rect,prefix+".recommended",545,670,600,60,30);
                PlaceTopLeft(body,545,745,1300,id=="education"?260:290);
            }
            return page;
        }

        static InputField V2Search(Transform root, string key, float x, float y, float w)
        {
            var fill = V2Panel(root, "Search", x, y, w, 56, V2Glass);
            fill.raycastTarget = true;
            V2Icon(fill.transform,"Search",18,16,24);
            var field = fill.gameObject.AddComponent<InputField>();
            field.textComponent = V2Text(fill.transform,"",58,0,w-76,56,23,false);
            var placeholder = V2Text(fill.transform,key,58,0,w-76,56,23);
            placeholder.color = new Color(1,1,1,.55f); field.placeholder = placeholder;
            field.characterLimit = 40;
            return field;
        }

        static void V2Home(Transform root,LobbyContent page)
        {
            V2Panel(root,"WelcomePanel",65,220,520,286,new Color(.045f,.06f,.085f,.9f));
            V2Text(root,"NewWorld",100,248,450,76,54,false).font=v2.SemiBold;
            V2Text(root,"home.subtitle",102,328,450,48,25).color = new Color32(178,190,204,255);
            V2Button(root,"home.enter","Play",100,410,290,56,"page","world",true);
            var note=V2Panel(root,"WorldNote",1430,225,415,160,new Color(.05f,.06f,.08f,.72f));
            V2Text(note.transform,"home.tagline1",26,26,365,46,25).color = new Color32(224,231,241,255);
            V2Text(note.transform,"home.tagline2",26,77,365,46,25).color = new Color32(183,195,212,255);
            var eventCard = V2Button(root,"home.event_next",null,1430,552,415,156,"choose","events");
            var eventLabel=eventCard.GetComponentInChildren<Text>();
            PlaceTopLeft(eventLabel.rectTransform,135,10,245,40);eventLabel.fontSize=20;
            V2Picture(eventCard.transform,"EventPhoto",V2Texture("home"),18,20,100,100,new Rect(584f/766,1-268f/415,46f/766,48f/415));
            Set(page,"eventTitle",V2Text(eventCard.transform,Loc.T("home.event_none"),135,55,245,76,24,false));
            V2Button(root,"menu.wardrobe","Shirt",785,830,240,52,"page","wardrobe");
            V2Panel(root,"ExploreDock",50,942,1820,102,new Color(.045f,.06f,.085f,.96f));
            string[] ids={"world","shops","education","business","entertainment"};
            string[] icons={"Map","Bag","GradCap","Buildings","Gamepad"};
            for(int i=0;i<5;i++)
            {
                string key=i==0?"home.card.world":"zone."+ids[i];
                var card=V2Button(root,key,icons[i],66+i*360,954,348,78,"page",ids[i]);
                if(i<2)
                {
                    card.transform.Find(icons[i]).gameObject.SetActive(false);
                    V2Picture(card.transform,"Photo",V2Texture(i==0?"clean-map":"clean-shops"),12,13,52,52,new Rect(.4f,.1f,.45f,.8f));
                }
                card.GetComponentInChildren<Text>().fontSize=21;
                var label=card.GetComponentInChildren<Text>();
                PlaceTopLeft(label.rectTransform,78,7,254,32);
                V2Text(card.transform,i==0?"home.card.world.sub":key+".sub",78,39,254,30,19).color=new Color32(157,174,191,255);
            }
        }

        static void V2Map(Transform root)
        {
            string[] ids={"business","education","shops","entertainment","housing","events","services"};
            string[] icons={"Buildings","GradCap","Bag","Gamepad","Home","Calendar","Gear"};
            Vector2[] points={new Vector2(780,230),new Vector2(1200,290),new Vector2(625,390),new Vector2(1370,480),new Vector2(630,620),new Vector2(990,740),new Vector2(1440,790)};
            Color[] colors={new Color32(55,75,96,238),new Color32(53,80,73,238),new Color32(87,58,72,238),new Color32(70,62,90,238),new Color32(86,72,56,238),new Color32(77,60,81,238),new Color32(54,76,88,238)};
            for(int i=0;i<ids.Length;i++)
            {
                string key=i<4?"zone."+ids[i]:"world.cat."+ids[i];
                var button=V2Button(root,key,icons[i],points[i].x,points[i].y,280,58,i<4?"page":i==5?"choose":"soon",i<4?ids[i]:i==5?"events":key);
                button.name="Map_"+i; button.GetComponent<Image>().color=colors[i];
            }
            V2Button(root,"","Plus",1790,315,60,52,"choose","zoom-in",false,false);
            V2Button(root,"","Minus",1790,377,60,52,"choose","zoom-out",false,false);
        }
    }
}

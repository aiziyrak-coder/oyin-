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
        static readonly Color V2Blue = new Color32(36, 99, 255, 255);
        static readonly Color V2Glass = new Color32(20, 36, 55, 210);

        // Faqat lobby qayta quriladi: tekshirilgan avatar niqoblarini qayta pishirmaydi.
        public static void RebuildLobby()
        {
            AssetDatabase.Refresh();
            BuildMainMenu();
            AssetDatabase.SaveAssets();
            Debug.Log("[CraDev] Lobby V2 sahnasi yaratildi.");
        }

        static Texture2D V2Texture(string name)
        {
            if(v2Textures.TryGetValue(name,out var cached)) return cached;
            string path = V2Art + name + ".png";
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
            var image = CreateSliced(name, parent, v2.RoundFill, 14, 24, color);
            PlaceTopLeft(image.rectTransform, x, y, w, h);
            return image;
        }

        static Text V2Text(Transform parent, string text, float x, float y, float w, float h, int size = 26, bool localized = true)
        {
            var label = CreateLabel("Text_" + text, parent, v2.Medium, localized ? "" : text, size, Color.white, TextAnchor.MiddleLeft);
            PlaceTopLeft(label.rectTransform, x, y, w, h);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            if (localized) Localized(label, text);
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
            if(blue) {fill.sprite=GradientSprite();fill.color=Color.white;}
            fill.raycastTarget = true;
            var button = TintButton(fill);
            float left = icon == null ? 20 : 64;
            if (icon != null) V2Icon(fill.transform, icon, 20, (h - 30) / 2, 30);
            var label = V2Text(fill.transform, key, left, 0, w - left - 12, h, 25, localized);
            label.name = "Label";
            label.resizeTextForBestFit=true; label.resizeTextMinSize=18; label.resizeTextMaxSize=25;
            if (action != null)
            {
                var command = fill.gameObject.AddComponent<LobbyCommand>();
                command.action = action; command.value = value;
            }
            return button;
        }

        static RawImage V2Picture(Transform parent, string name, Texture texture, float x, float y, float w, float h, Rect? crop = null)
        {
            var rect = CreateRect(name, parent);
            PlaceTopLeft(rect, x, y, w, h);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture; image.raycastTarget = false;
            if (crop.HasValue) image.uvRect = crop.Value;
            return image;
        }

        static void BuildMainMenu()
        {
            v2 = Kit();
            v2Textures.Clear();
            var root = NewUiScene(out var scene);
            root.gameObject.AddComponent<GraphicRaycaster>();
            CreateEventSystem();
            var screen = root.gameObject.AddComponent<MainMenuScreen>();
            var camera = Camera.main;
            var bgCamera = new GameObject("BackgroundCamera").AddComponent<Camera>();
            bgCamera.depth = -1; bgCamera.cullingMask = 1 << 5;
            bgCamera.clearFlags = CameraClearFlags.SolidColor;
            bgCamera.backgroundColor = new Color32(13, 29, 48, 255);
            var bgRoot = new GameObject("BackgroundCanvas", typeof(RectTransform), typeof(Canvas));
            bgRoot.layer = 5;
            var canvas = bgRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = bgCamera; canvas.planeDistance = 10;
            var backgroundA = V2Picture(bgRoot.transform, "BackgroundA", null, 0, 0, 1920, 1080);
            Stretch(backgroundA.rectTransform);
            var backgroundB = V2Picture(bgRoot.transform, "BackgroundB", null, 0, 0, 1920, 1080);
            Stretch(backgroundB.rectTransform);
            backgroundA.color = Color.white;
            backgroundB.color = new Color(1,1,1,0);
            Set(screen, "backgroundA", backgroundA); Set(screen, "backgroundB", backgroundB);
            const string backdropPath = V2Art + "Backdrop.asset";
            var backdrop = AssetDatabase.LoadAssetAtPath<Texture2D>(backdropPath);
            if(backdrop == null)
            {
                backdrop = new Texture2D(2,2,TextureFormat.RGB24,false);
                backdrop.SetPixels(new[]{new Color32(10,26,42,255),new Color32(20,38,58,255),new Color32(16,32,52,255),new Color32(42,67,99,255)}.Select(c=>(Color)c).ToArray());
                backdrop.Apply(); backdrop.wrapMode=TextureWrapMode.Clamp;
                AssetDatabase.CreateAsset(backdrop,backdropPath);
            }
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
            V2Navigation(root, screen);
            var toast = V2Panel(root, "Toast", 560, 945, 800, 78, V2Glass);
            Set(screen, "toastGroup", toast.gameObject.AddComponent<CanvasGroup>());
            Set(screen, "toastText", V2Text(toast.transform, "", 24, 0, 752, 78, 26, false));
            Set(screen, "dialog", BuildConfirmDialog(root, v2));
            Set(screen, "fader", CreateFullscreen("Fader", root, Color.black));
            AddUiSounds();
            Save(scene, MenuScene);
        }

        static void V2Poses(LobbyStage stage)
        {
            var so = new SerializedObject(stage);
            var poses = so.FindProperty("poses"); poses.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                // Proportsiyalar manba rasmdagi bosh va oyoq nuqtalaridan hisoblangan.
                float head = i == 0 ? 75.8f / 415 : 46.3f / 309;
                float feet = i == 0 ? 332.6f / 415 : 296f / 309;
                float x = i == 0 ? 357.2f / 766 : 197f / 519;
                float fov = 30, height = 1.85f, viewHeight = height / (feet - head);
                float distance = viewHeight / (2 * Mathf.Tan(fov * Mathf.Deg2Rad / 2));
                var pose = poses.GetArrayElementAtIndex(i);
                pose.FindPropertyRelative("name").stringValue = i == 0 ? "Penthouse" : "Wardrobe";
                pose.FindPropertyRelative("position").vector3Value = new Vector3((.5f-x)*viewHeight*16/9, (feet-.5f)*viewHeight, -distance);
                pose.FindPropertyRelative("rotation").vector3Value = Vector3.zero;
                pose.FindPropertyRelative("fieldOfView").floatValue = fov;
                pose.FindPropertyRelative("imageAspect").floatValue = 16f/9;
                pose.FindPropertyRelative("sunRotation").vector3Value = new Vector3(28,-32,0);
                pose.FindPropertyRelative("sunColor").colorValue = new Color(1,.9f,.8f);
                pose.FindPropertyRelative("sunIntensity").floatValue = 1.2f;
                pose.FindPropertyRelative("rimRotation").vector3Value = new Vector3(20,155,0);
                pose.FindPropertyRelative("rimColor").colorValue = new Color(.65f,.8f,1);
                pose.FindPropertyRelative("rimIntensity").floatValue = .65f;
                pose.FindPropertyRelative("shadowStrength").floatValue = .55f;
                pose.FindPropertyRelative("ambientSky").colorValue = new Color(.52f,.56f,.64f);
                pose.FindPropertyRelative("ambientEquator").colorValue = new Color(.38f,.36f,.34f);
                pose.FindPropertyRelative("ambientGround").colorValue = new Color(.22f,.21f,.21f);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void V2Navigation(Transform root, MainMenuScreen screen)
        {
            var nav = CreateRect("Navigation", root); Stretch(nav);
            var brand = V2Button(nav, "Lynxos", null, 70, 30, 240, 70, "page", "home", false, false);
            brand.GetComponent<Image>().color = Color.clear;
            var tile=V2Panel(brand.transform,"Logo",0,12,48,48,V2Blue);
            var glyph=CreateImage("Glyph",tile.transform,LoadSprite(IntroArt+"CraDev_Glyph.png"),new Vector2(42,42),Vector2.zero,Color.white);
            Place(glyph.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(42,42));
            brand.GetComponentInChildren<Text>().font=v2.Bold;
            PlaceTopLeft(brand.transform.Find("Label").GetComponent<RectTransform>(), 66, 0, 174, 70);
            string[] keys = { "home", "world", "friends", "top", "settings" };
            var buttons = new List<Button>();
            for (int i = 0; i < keys.Length; i++)
                buttons.Add(V2Button(nav, "nav." + keys[i], null, 525 + i * 142, 30, 140, 68));
            var tabs = nav.gameObject.AddComponent<SelectList>();
            foreach(var button in buttons)
            {
                var text=button.GetComponentInChildren<Text>();
                text.resizeTextForBestFit=false;text.fontSize=22;
                text.horizontalOverflow=HorizontalWrapMode.Overflow;text.alignment=TextAnchor.MiddleCenter;
            }
            var underline=V2Panel(nav,"SelectedUnderline",525,95,140,3,new Color32(130,187,255,255));
            Set(tabs,"indicator",underline.rectTransform);
            SetArray(tabs, "items", buttons.Cast<Object>().ToArray());
            SetArray(tabs, "fills", buttons.Select(b => (Object)b.GetComponent<Image>()).ToArray());
            Set(tabs, "normalFill", Color.clear); Set(tabs, "selectedFill", new Color(.13f,.28f,.65f,.85f));
            Set(screen, "navTabs", tabs);
            var language = V2Button(nav, "UZ", "Globe", 1350, 30, 160, 68, null, null, false, false);
            language.GetComponent<Image>().color = Color.clear;
            Set(language.gameObject.AddComponent<LanguageToggle>(), "label", language.GetComponentInChildren<Text>());
            var bell = V2Button(nav, "", "Bell", 1510, 30, 70, 68, null, null, false, false);
            bell.GetComponent<Image>().color = Color.clear; Set(screen, "bellButton", bell);
            Set(screen, "bellDot", V2Panel(bell.transform, "NotificationDot", 43, 14, 10, 10, new Color32(243,65,95,255)));
            var profile = V2Button(nav, "Lynxos_user", "User", 1600, 30, 250, 68, null, null, false, false);
            Set(screen, "profileButton", profile); Set(screen, "profileName", profile.GetComponentInChildren<Text>());
            PlaceTopLeft(profile.GetComponentInChildren<Text>().rectTransform,80,0,150,68);
            Set(screen, "profileIcon", profile.transform.Find("User").GetComponent<Image>());
            Set(screen, "profileThumb", V2Picture(profile.transform, "Avatar", null, 18, 10, 48, 48));
            var blocker = CreateFullscreen("ProfileMenuBlocker", root, Color.clear);
            blocker.raycastTarget = true; Set(screen, "profileMenuBlocker", TintButton(blocker));
            var menu = V2Panel(root, "ProfileMenu", 1470, 110, 380, 304, V2Glass);
            Set(screen, "profileMenu", menu.gameObject.AddComponent<CanvasGroup>());
            var menuButtons = new List<Button>();
            string[] menuKeys = { "wardrobe", "customize", "settings", "quit" };
            string[] icons = { "Shirt", "User", "Gear", "Logout" };
            for (int i = 0; i < 4; i++)
                menuButtons.Add(V2Button(menu.transform, "menu." + menuKeys[i], icons[i], 8, 8 + i * 72, 364, 68));
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
            Set(page, "educationImage", V2Texture("education-cards")); Set(page, "friendsImage", V2Texture("hero-friends"));
            SetArray(page,"businessIcons",new Object[]{LineIcon("Office"),LineIcon("Coworking"),LineIcon("Rocket"),LineIcon("Coins")});
            string bg = id == "world" ? "map" : id;
            Set(page, "background", V2Texture("clean-" + bg));
            if (id == "home") { V2Home(rect,page); return page; }
            string prefix = id == "entertainment" ? "fun" : id;
            V2Text(rect, prefix + ".title", 65, 153, id == "friends" ? 660 : 590, 78, 52).font = v2.Bold;
            var subtitle = V2Text(rect, prefix + ".subtitle", 68, 232, 650, 42, 27);
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
                    categories = new[] { "settings.section.profile", "settings.section.security", "settings.section.notifications", "settings.section.sound", "settings.section.controls", "settings.section.privacy", "settings.section.language", "settings.section.help" };
                    categoryIcons = new[] { "User", "Shield", "Bell", "Volume", "Gamepad", "Lock", "Globe", "Help" }; break;
                case "entertainment":
                    categories = new[] { "fun.cat.all", "fun.cat.concerts", "fun.cat.cinema", "fun.cat.games", "fun.cat.sport", "fun.cat.parks" };
                    categoryIcons = new[] { "Grid", "Music", "Film", "Gamepad", "Ball", "Home" }; break;
                default: categories = new string[0]; categoryIcons = new string[0]; break;
            }
            var categoriesButtons = new List<Button>();
            for (int i = 0; i < categories.Length; i++)
                categoriesButtons.Add(V2Button(rect, categories[i], categoryIcons[i], 65, 315+i*80, 385, 74, "choose", i.ToString(), i == 0));
            SetArray(page, "categories", categoriesButtons.Cast<Object>().ToArray());
            if (id == "world")
            {
                V2Map(rect);
                Set(page, "search", V2Search(rect, "world.search", 1340, 155, 510));
                var status = V2Panel(rect, "CityStatus", 1320, 930, 530, 105, V2Glass);
                V2Text(status.transform, "world.center", 24, 6, 480, 45, 25);
                Set(page, "status", V2Text(status.transform, Loc.T("world.online_unknown"), 24, 50, 480, 42, 22,false));
                return page;
            }
            var body = CreateRect("Content", rect);
            float bodyX = id == "wardrobe" ? 1030 : id == "friends" || id == "top" ? 65 : 545;
            PlaceTopLeft(body, bodyX, id == "shops" ? 650 : 315, 1850-bodyX, id == "shops" ? 370 : 720);
            Set(page, "content", body);
            if(id=="settings") PlaceTopLeft(body,705,210,1145,820);
            if(id=="wardrobe")
            {
                PlaceTopLeft(body,1030,195,820,690);
                var footer=CreateRect("WardrobeFooter",rect);PlaceTopLeft(footer,1030,915,820,110);
                Set(page,"footer",footer);
            }
            if (id == "shops") Set(page, "search", V2Search(rect, "shops.search", 810, 165, 1035));
            if (id == "friends")
            {
                string[] tabs = { "friends", "groups", "events", "chat" };
                var friendTabs=new List<Button>();
                for (int i=0;i<4;i++) friendTabs.Add(V2Button(rect, "friends.tab."+tabs[i], null, 865+i*245, 183, 239, 65, "choose", i.ToString(), i==0));
                SetArray(page,"categories",friendTabs.Cast<Object>().ToArray());
                Set(page,"search",V2Search(rect,"friends.search",865,265,975));
                PlaceTopLeft(body,65,355,1785,650);
            }
            if (id == "education" || id == "entertainment")
            {
                var hero = V2Picture(rect, "Hero", V2Texture(id == "education" ? "hero-education" : "clean-map"), 545, 168, 1300, 480);
                var box = V2Panel(rect,"HeroInfo",1300,205,500,390,V2Glass);
                V2Text(box.transform,prefix+".hero.title",35,35,430,80,36).font=v2.Bold;
                V2Text(box.transform,prefix+".hero.subtitle",35,125,420,75,26);
                V2Button(box.transform,prefix+".visit","ArrowRight",35,260,430,84,"soon",prefix+".title",true);
                V2Text(rect,prefix+".recommended",545,670,600,60,30);
                PlaceTopLeft(body,545,745,1300,290);
            }
            return page;
        }

        static InputField V2Search(Transform root, string key, float x, float y, float w)
        {
            var fill = V2Panel(root, "Search", x, y, w, 76, new Color(.7f,.78f,.9f,.22f));
            fill.raycastTarget = true;
            V2Icon(fill.transform,"Search",22,22,30);
            var field = fill.gameObject.AddComponent<InputField>();
            field.textComponent = V2Text(fill.transform,"",70,0,w-85,76,24,false);
            var placeholder = V2Text(fill.transform,key,70,0,w-85,76,24);
            placeholder.color = new Color(1,1,1,.55f); field.placeholder = placeholder;
            field.characterLimit = 40;
            return field;
        }

        static void V2Home(Transform root,LobbyContent page)
        {
            V2Text(root,"Lynxos",100,190,550,135,88,false).font=v2.Display;
            V2Text(root,"home.subtitle",104,327,560,65,34).color = new Color32(190,204,224,255);
            V2Button(root,"home.enter","Play",92,427,485,108,"page","world",true);
            V2Text(root,"home.tagline1",1540,207,340,50,28).color = new Color32(18,33,50,255);
            V2Text(root,"home.tagline2",1540,252,340,50,28).color = new Color32(18,33,50,255);
            var eventCard = V2Button(root,"home.event_next",null,1460,545,405,145,"choose","events");
            var eventLabel=eventCard.GetComponentInChildren<Text>();
            PlaceTopLeft(eventLabel.rectTransform,135,10,245,40);eventLabel.fontSize=20;
            V2Picture(eventCard.transform,"EventPhoto",V2Texture("home"),18,20,100,100,new Rect(584f/766,1-268f/415,46f/766,48f/415));
            Set(page,"eventTitle",V2Text(eventCard.transform,Loc.T("home.event_none"),135,55,245,76,24,false));
            V2Button(root,"menu.wardrobe","Shirt",770,805,280,65,"page","wardrobe");
            string[] ids={"world","shops","education","business","entertainment"};
            string[] icons={"Map","Bag","GradCap","Buildings","Gamepad"};
            for(int i=0;i<5;i++)
            {
                string key=i==0?"home.card.world":"zone."+ids[i];
                var card=V2Button(root,key,icons[i],75+i*360,906,335,131,"page",ids[i]);
                if(i<2)
                {
                    card.transform.Find(icons[i]).gameObject.SetActive(false);
                    V2Picture(card.transform,"Photo",V2Texture(i==0?"clean-map":"clean-shops"),18,26,70,78,new Rect(.4f,.1f,.45f,.8f));
                }
                card.GetComponentInChildren<Text>().fontSize=24;
                var label=card.GetComponentInChildren<Text>();
                PlaceTopLeft(label.rectTransform,96,16,225,50);
                V2Text(card.transform,i==0?"home.card.world.sub":key+".sub",96,65,225,50,20);
            }
        }

        static void V2Map(Transform root)
        {
            string[] ids={"business","education","shops","entertainment","housing","events","services"};
            string[] icons={"Buildings","GradCap","Bag","Gamepad","Home","Calendar","Gear"};
            Vector2[] points={new Vector2(780,230),new Vector2(1200,290),new Vector2(625,390),new Vector2(1370,480),new Vector2(630,620),new Vector2(990,740),new Vector2(1440,790)};
            Color[] colors={new Color32(32,92,163,235),new Color32(43,132,97,235),new Color32(159,37,74,235),new Color32(104,56,169,235),new Color32(154,96,52,235),new Color32(121,51,152,235),new Color32(37,105,166,235)};
            for(int i=0;i<ids.Length;i++)
            {
                string key=i<4?"zone."+ids[i]:"world.cat."+ids[i];
                var button=V2Button(root,key,icons[i],points[i].x,points[i].y,305,88,i<4?"page":i==5?"choose":"soon",i<4?ids[i]:i==5?"events":key);
                button.name="Map_"+i; button.GetComponent<Image>().color=colors[i];
            }
            V2Button(root,"+","Plus",1780,315,70,70,"choose","zoom-in",false,false);
            V2Button(root,"","Minus",1780,390,70,70,"choose","zoom-out",false,false);
        }
    }
}

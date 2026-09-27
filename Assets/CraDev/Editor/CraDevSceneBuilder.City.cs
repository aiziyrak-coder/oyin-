using System.Collections.Generic;
using System.IO;
using System.Linq;
using CraDev.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using static CraDev.EditorTools.UiBuild;

namespace CraDev.EditorTools
{
    /// <summary>
    /// Bosh menyu ortidagi virtual shahar (hammasi koddan): quyoshli osmon, plitkali maydon va favvora,
    /// daraxtlar, qahramon turadigan yonib turuvchi platforma, 5 ta zona binosi (Shopping, Education,
    /// Business, Entertainment, Community), globusli markaziy minora, osma ko'prik, uzoqdagi osmono'par
    /// binolar, maydonda yuruvchi odamlar va uchib yuruvchi transport. Kinodagidek effektlar: post-processing.
    ///
    /// Koordinatalar: qahramon (0, 0, 0) da kameraga (-Z) qarab turadi, kamera janubda (-Z), shahar shimolda (+Z).
    /// </summary>
    static partial class CraDevSceneBuilder
    {
        const string WorldFolder = "Assets/CraDev/World/";
        const string WorldMaterials = WorldFolder + "Materials/";
        const string ZoneCards = WorldFolder + "Cards/";

        /// <summary>Shahar zonasi: dumaloq bino (markazi, radiusi, qavatlari), peshtaxta yozuvi, ikonka, karta matnlari.</summary>
        class Zone
        {
            public string Id, Sign, Icon;
            public Vector3 Center;
            public float Radius, Span;
            public int Floors;
            public Color Accent;
            public float Height => Floors * FloorHeight;
            /// <summary>Binoning maydonga qaragan eng yaqin nuqtasi (kamera shunga buriladi).</summary>
            public Vector3 Front
            {
                get
                {
                    var dir = (PlazaCenter - Center);
                    dir.y = 0f;
                    return Center + dir.normalized * Radius + Vector3.up * Height * 0.55f;
                }
            }
        }

        const float FloorHeight = 4.4f;
        static readonly Vector3 PlazaCenter = new Vector3(0f, 0f, 14f);

        static readonly Zone[] Zones =
        {
            new Zone { Id = "shops", Sign = "SHOPPING", Icon = "Bag", Center = new Vector3(-24f, 0f, 16f), Radius = 11f, Span = 230f, Floors = 3, Accent = new Color(0.35f, 0.75f, 1f) },
            new Zone { Id = "education", Sign = "EDUCATION", Icon = "Cap", Center = new Vector3(-19f, 0f, 44f), Radius = 10f, Span = 220f, Floors = 3, Accent = new Color(0.4f, 0.8f, 1f) },
            new Zone { Id = "business", Sign = "BUSINESS", Icon = "Chart", Center = new Vector3(36f, 0f, 58f), Radius = 9f, Span = 260f, Floors = 10, Accent = new Color(0.45f, 0.7f, 1f) },
            new Zone { Id = "entertainment", Sign = "ENTERTAINMENT", Icon = "Gamepad", Center = new Vector3(21f, 0f, 34f), Radius = 10f, Span = 220f, Floors = 3, Accent = new Color(0.75f, 0.45f, 1f) },
            new Zone { Id = "community", Sign = "COMMUNITY", Icon = "People", Center = new Vector3(25f, 0f, 11f), Radius = 8f, Span = 220f, Floors = 2, Accent = new Color(0.35f, 0.85f, 0.9f) },
        };

        class CityResult
        {
            public Transform Turntable;
            public Vector3 CameraPosition, CameraTarget;
            public ReflectionProbe Probe;
        }

        static CityResult BuildCity(Camera camera, RuntimeAnimatorController maleIdle, RuntimeAnimatorController femaleIdle)
        {
            Directory.CreateDirectory(WorldMaterials);
            var root = new GameObject("City").transform;
            var result = new CityResult();

            BuildSkyAndLight(root);
            BuildGround(root);
            result.Turntable = BuildPlatform(root);
            BuildFountain(root, new Vector3(0f, 0f, 17f));
            BuildGreenery(root);
            foreach (var zone in Zones)
                BuildPavilion(root, zone);
            BuildEntertainmentScreen(root, Zones.First(z => z.Id == "entertainment"));
            BuildFerrisWheel(root, new Vector3(40f, 0f, 44f));
            BuildCentralTower(root, new Vector3(9f, 0f, 108f));
            BuildSkybridge(root);
            BuildSkyline(root);
            BuildFlyers(root);
            BuildPeople(root, maleIdle, femaleIdle);

            // Shisha binolar atrofni aks ettirishi uchun (WorldLighting sahna ochilganda yangilaydi)
            var probeGo = new GameObject("ReflectionProbe");
            probeGo.transform.SetParent(root, false);
            probeGo.transform.position = new Vector3(0f, 6f, 22f);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.resolution = 256;
            probe.size = new Vector3(260f, 80f, 260f);
            probe.hdr = true;
            result.Probe = probe;

            // Kamera: qahramon kadr balandligining ~60% ida, ortida shahar
            result.CameraPosition = new Vector3(-0.35f, 1.0f, -5.7f);
            result.CameraTarget = new Vector3(-0.35f, 1.16f, 0f);
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 38f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1500f;
            camera.allowHDR = true;
            camera.allowMSAA = false; // silliqlashni post-processing (SMAA) qiladi
            camera.cullingMask = ~(1 << LayerMask.NameToLayer("UI"));
            camera.transform.SetPositionAndRotation(result.CameraPosition, Quaternion.LookRotation(result.CameraTarget - result.CameraPosition));
            AddPostProcessing(camera);
            return result;
        }

        // ---------------------------------------------------------------- Osmon, quyosh, effektlar

        static void BuildSkyAndLight(Transform root)
        {
            var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(WorldFolder + "Sky/Sky_PartlyCloudy.hdr");
            var sky = WorldMaterial("Sky", Shader.Find("Skybox/Cubemap"), m =>
            {
                m.SetTexture("_Tex", cube);
                m.SetFloat("_Exposure", 1.05f);
                m.SetFloat("_Rotation", SkyRotation);
                m.SetColor("_Tint", new Color(0.52f, 0.52f, 0.54f));
            });
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 0.95f;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = cube;
            RenderSettings.reflectionIntensity = 1f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.74f, 0.82f, 0.9f);
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 650f;

            // Quyosh: osmondagi quyosh bilan bir tomonda (kamera orqasida, o'ngda), 48° balandlikda
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.94f, 0.84f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            sun.shadowBias = 0.03f;
            sun.shadowNormalBias = 0.35f;
            sun.transform.rotation = Quaternion.Euler(SunElevation, SunAzimuth, 0f);
            RenderSettings.sun = sun;
        }

        // HDRI'dagi quyosh (u = 0.595, 48°) kamera orqasida o'ngda bo'lishi uchun osmon buriladi
        const float SunElevation = 42f;
        const float SunAzimuth = -35f;
        const float SkyRotation = 110f;

        static void AddPostProcessing(Camera camera)
        {
            var layer = camera.gameObject.AddComponent<PostProcessLayer>();
            var resources = AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset");
            layer.Init(resources);
            Set(layer, "m_Resources", resources);
            layer.volumeLayer = ~0;
            layer.volumeTrigger = camera.transform;
            layer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;

            string path = WorldFolder + "PostFX.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var bloom = profile.AddSettings<Bloom>();
            bloom.intensity.Override(2.2f);
            bloom.threshold.Override(1.05f);
            bloom.softKnee.Override(0.6f);
            bloom.diffusion.Override(7f);

            var grading = profile.AddSettings<ColorGrading>();
            grading.tonemapper.Override(Tonemapper.ACES);
            grading.postExposure.Override(-0.1f);
            grading.temperature.Override(6f);
            grading.saturation.Override(12f);
            grading.contrast.Override(8f);

            var ao = profile.AddSettings<AmbientOcclusion>();
            ao.mode.Override(AmbientOcclusionMode.MultiScaleVolumetricObscurance);
            ao.intensity.Override(0.6f);

            // Qahramon tiniq, orqa fon va oldingi barglar yumshoq xira (kinodagidek)
            var dof = profile.AddSettings<DepthOfField>();
            dof.focusDistance.Override(5.7f);
            dof.aperture.Override(9f);
            dof.focalLength.Override(35f);
            dof.kernelSize.Override(KernelSize.Medium);

            var vignette = profile.AddSettings<Vignette>();
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.45f);

            foreach (var setting in profile.settings)
            {
                setting.name = setting.GetType().Name;
                AssetDatabase.AddObjectToAsset(setting, profile);
            }
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            var volume = new GameObject("PostFX").AddComponent<PostProcessVolume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        // ---------------------------------------------------------------- Maydon va platforma

        static void BuildGround(Transform root)
        {
            var tiles = WorldMaterial("PlazaTiles", Shader.Find("Standard"), m =>
            {
                m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(WorldFolder + "Textures/Plaza_diff.jpg"));
                m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(WorldFolder + "Textures/Plaza_nor.jpg"));
                m.EnableKeyword("_NORMALMAP");
                m.SetTextureScale("_MainTex", new Vector2(40f, 40f));
                m.color = new Color(0.62f, 0.64f, 0.68f);
                m.SetFloat("_Glossiness", 0.5f);
                m.SetFloat("_Metallic", 0f);
            });
            var plaza = At(MeshObject("Plaza", root, Disc(60f, 0.02f, 128, 80f), tiles), Vector3.zero);
            plaza.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var ground = WorldMaterial("Ground", Shader.Find("Standard"), m =>
            {
                m.color = new Color(0.58f, 0.6f, 0.62f);
                m.SetFloat("_Glossiness", 0.3f);
            });
            At(MeshObject("Ground", root, Disc(900f, 0.0f, 64, 1f), ground), new Vector3(0f, -0.01f, 0f));

            // Yonib turuvchi yo'lkalar: platformadan favvoraga va favvora atrofida halqalar
            var glow = Emissive("GlowCyan", new Color(0.3f, 0.85f, 1f), 2.2f);
            for (int i = -1; i <= 1; i += 2)
                At(MeshObject("PathLine", root, Box(0.035f, 0.01f, 10f), Emissive("GlowPath", new Color(0.3f, 0.85f, 1f), 1.4f)), new Vector3(i * 1.7f, 0.02f, 9.2f));
            At(MeshObject("FountainRing", root, Torus(7.2f, 0.035f, 128, 6), glow), new Vector3(0f, 0.03f, 17f));
            At(MeshObject("FountainRing2", root, Torus(8.4f, 0.02f, 128, 6), Emissive("GlowCyanSoft", new Color(0.3f, 0.85f, 1f), 1.6f)), new Vector3(0f, 0.03f, 17f));
        }

        static Transform BuildPlatform(Transform root)
        {
            var metal = WorldMaterial("PlatformMetal", Shader.Find("Standard"), m =>
            {
                m.color = new Color(0.16f, 0.18f, 0.22f);
                m.SetFloat("_Metallic", 0.8f);
                m.SetFloat("_Glossiness", 0.82f);
            });
            At(MeshObject("Platform", root, Disc(1.35f, 0.06f, 128, 1f), metal), Vector3.zero);
            var glow = Emissive("GlowPlatform", new Color(0.35f, 0.8f, 1f), 4.5f);
            At(MeshObject("PlatformRing", root, Torus(1.32f, 0.03f, 128, 8), glow), new Vector3(0f, 0.06f, 0f));
            At(MeshObject("PlatformRingInner", root, Torus(0.98f, 0.012f, 128, 6), glow), new Vector3(0f, 0.062f, 0f));
            At(MeshObject("PlatformRingOuter", root, Torus(1.9f, 0.012f, 128, 6), Emissive("GlowPlatformSoft", new Color(0.35f, 0.8f, 1f), 1.8f)), new Vector3(0f, 0.02f, 0f));

            var turntable = new GameObject("Turntable").transform;
            turntable.SetParent(root, false);
            turntable.localPosition = new Vector3(0f, 0.06f, 0f);
            return turntable;
        }

        static void BuildFountain(Transform root, Vector3 at)
        {
            var stone = WorldMaterial("WhiteStone", Shader.Find("Standard"), m =>
            {
                m.color = new Color(0.9f, 0.91f, 0.93f);
                m.SetFloat("_Glossiness", 0.55f);
            });
            var water = WorldMaterial("Water", Shader.Find("Standard"), m =>
            {
                m.color = new Color(0.25f, 0.55f, 0.7f, 0.75f);
                m.SetFloat("_Metallic", 0.3f);
                m.SetFloat("_Glossiness", 0.97f);
                SetTransparent(m);
            });
            var fountain = new GameObject("Fountain").transform;
            fountain.SetParent(root, false);
            fountain.localPosition = at;
            // Hovuz devori, suv, o'rtada ikki qavatli kosa
            MeshObject("Basin", fountain, Lathe(new[] { V(5.0f, 0.1f), V(5.0f, 0.55f), V(5.0f, 0.55f), V(5.4f, 0.55f), V(5.4f, 0.55f), V(5.4f, 0f) }, 96), stone);
            MeshObject("Water", fountain, Disc(5.0f, 0.0f, 96, 1f), water).transform.localPosition = new Vector3(0f, 0.42f, 0f);
            MeshObject("Column", fountain, Lathe(new[] { V(0.001f, 2.6f), V(0.35f, 2.6f), V(0.35f, 2.6f), V(0.28f, 1.5f), V(0.3f, 0.3f), V(0.3f, 0.3f), V(0.6f, 0.3f), V(0.6f, 0f) }, 48), stone);
            MeshObject("Bowl", fountain, Lathe(new[] { V(0.001f, 1.5f), V(1.5f, 1.55f), V(1.5f, 1.55f), V(1.5f, 1.75f), V(1.5f, 1.75f), V(1.65f, 1.75f), V(1.65f, 1.75f), V(1.65f, 1.6f), V(0.35f, 1.35f) }, 64), stone);

            // Suv otilishi: markazda baland, atrofida 12 ta kichik
            var jetMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
            AddJet(fountain, "JetCenter", new Vector3(0f, 2.6f, 0f), Vector3.up, 6f, 1.3f, 0.1f, 260, jetMaterial);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                var p = new Vector3(Mathf.Cos(a) * 4.6f, 0.5f, Mathf.Sin(a) * 4.6f);
                var dir = (-p.normalized * 0.45f + Vector3.up).normalized;
                AddJet(fountain, "Jet" + i, p, dir, 5.5f, 1.1f, 0.06f, 120, jetMaterial);
            }
        }

        static void AddJet(Transform parent, string name, Vector3 position, Vector3 direction, float speed, float lifetime, float size, int rate, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.LookRotation(direction);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startSpeed = speed;
            main.startLifetime = lifetime;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = new Color(0.85f, 0.95f, 1f, 0.55f);
            main.gravityModifier = 1f;
            main.maxParticles = rate * 3;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 2.5f;
            shape.radius = 0.02f;
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.05f;
            renderer.lengthScale = 2f;
            renderer.sharedMaterial = material;
        }

        static void BuildGreenery(Transform root)
        {
            var tree = AssetDatabase.LoadAssetAtPath<GameObject>(WorldFolder + "Models/Tree/island_tree_02.fbx");
            var shrub = AssetDatabase.LoadAssetAtPath<GameObject>(WorldFolder + "Models/Shrub/shrub_01.fbx");
            var planterMat = WorldMaterial("Planter", Shader.Find("Standard"), m =>
            {
                m.color = new Color(0.88f, 0.9f, 0.92f);
                m.SetFloat("_Glossiness", 0.5f);
            });
            var soil = WorldMaterial("Soil", Shader.Find("Standard"), m => m.color = new Color(0.16f, 0.2f, 0.1f));

            // Daraxtli gulzorlar: maydon chetlari bo'ylab
            var planters = new[]
            {
                new Vector3(-7.5f, 0f, 8f), new Vector3(7.5f, 0f, 7f), new Vector3(-9f, 0f, 26f), new Vector3(10f, 0f, 25f),
                new Vector3(-4.5f, 0f, 33f), new Vector3(5f, 0f, 34f), new Vector3(-13f, 0f, 2f), new Vector3(14f, 0f, 1f),
            };
            var random = new System.Random(7);
            foreach (var p in planters)
            {
                var planter = new GameObject("Planter").transform;
                planter.SetParent(root, false);
                planter.localPosition = p;
                MeshObject("Box", planter, Lathe(new[] { V(1.95f, 0.45f), V(1.95f, 0.6f), V(1.95f, 0.6f), V(2.1f, 0.6f), V(2.1f, 0.6f), V(2.1f, 0f) }, 48), planterMat);
                MeshObject("Soil", planter, Disc(1.95f, 0f, 48, 1f), soil).transform.localPosition = new Vector3(0f, 0.5f, 0f);
                if (tree != null)
                {
                    var t = (GameObject)PrefabUtility.InstantiatePrefab(tree, planter);
                    t.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                    t.transform.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                    t.transform.localScale *= 1.35f + (float)random.NextDouble() * 0.35f; // FBX o'z masshtabi (100) saqlanadi
                }
                if (shrub != null)
                    for (int k = 0; k < 5; k++)
                    {
                        float a = k * 1.3f + (float)random.NextDouble();
                        var s = ShrubVariant(shrub, planter, random.Next(9));
                        s.localPosition = new Vector3(Mathf.Cos(a) * 1.35f, 0.5f, Mathf.Sin(a) * 1.35f);
                        s.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                        s.localScale = Vector3.one * 2.2f;
                    }
            }

            // Oldingi reja: kameraga yaqin barglar (xira ko'rinadi, rasmga chuqurlik beradi)
            if (shrub != null)
                foreach (var (p, scale, variant) in new[] { (new Vector3(-2.7f, 0f, -2.3f), 4.2f, 2), (new Vector3(-3.4f, 0f, -1.2f), 3.6f, 5), (new Vector3(3.1f, 0f, -2.1f), 4f, 7), (new Vector3(3.8f, 0f, -0.6f), 3.2f, 1) })
                {
                    var s = ShrubVariant(shrub, root, variant);
                    s.name = "ForegroundShrub";
                    s.position = p;
                    s.localScale = Vector3.one * scale;
                }

            // Skameykalar va chiroq ustunlari
            var bench = WorldMaterial("BenchWood", Shader.Find("Standard"), m => { m.color = new Color(0.55f, 0.4f, 0.28f); m.SetFloat("_Glossiness", 0.35f); });
            var steel = Steel();
            var lampGlow = Emissive("LampGlow", new Color(1f, 0.95f, 0.85f), 3f);
            foreach (var p in new[] { new Vector3(-4.5f, 0f, 12f), new Vector3(4.5f, 0f, 12f), new Vector3(-6f, 0f, 21f), new Vector3(6f, 0f, 22f) })
            {
                var b = new GameObject("Bench").transform;
                b.SetParent(root, false);
                b.localPosition = p;
                b.localRotation = Quaternion.LookRotation(PlazaCenter + Vector3.forward * 3f - p);
                At(MeshObject("Seat", b, Box(1.8f, 0.08f, 0.5f), bench), new Vector3(0f, 0.45f, 0f));
                At(MeshObject("LegL", b, Box(0.08f, 0.45f, 0.45f), steel), new Vector3(-0.8f, 0.22f, 0f));
                At(MeshObject("LegR", b, Box(0.08f, 0.45f, 0.45f), steel), new Vector3(0.8f, 0.22f, 0f));
            }
            foreach (var p in new[] { new Vector3(-3.4f, 0f, 4f), new Vector3(3.4f, 0f, 4f), new Vector3(-10f, 0f, 16f), new Vector3(10f, 0f, 16f), new Vector3(-7f, 0f, 30f), new Vector3(7f, 0f, 30f) })
            {
                var l = new GameObject("Lamp").transform;
                l.SetParent(root, false);
                l.localPosition = p;
                MeshObject("Pole", l, Lathe(new[] { V(0.001f, 4.2f), V(0.05f, 4.2f), V(0.05f, 4.2f), V(0.06f, 0.3f), V(0.14f, 0.2f), V(0.14f, 0f) }, 16), steel);
                At(MeshObject("Head", l, Box(0.5f, 0.06f, 0.18f), steel), new Vector3(0f, 4.25f, 0f));
                At(MeshObject("Light", l, Box(0.44f, 0.02f, 0.12f), lampGlow), new Vector3(0f, 4.21f, 0f));
            }
        }

        // ---------------------------------------------------------------- Zona binolari

        /// <summary>
        /// Dumaloq zamonaviy bino: shisha fasad (ichkarisi iliq yoritilgan), har qavatda oq plita-balkon,
        /// tomida gulzor, tepasida yonib turuvchi peshtaxta (ikonka + nom).
        /// </summary>
        static void BuildPavilion(Transform root, Zone zone)
        {
            var building = new GameObject("Zone_" + zone.Sign).transform;
            building.SetParent(root, false);
            building.localPosition = zone.Center;
            var toPlaza = PlazaCenter - zone.Center;
            float facing = Mathf.Atan2(toPlaza.z, toPlaza.x) * Mathf.Rad2Deg;
            float a0 = facing - zone.Span / 2f, a1 = facing + zone.Span / 2f;
            float r = zone.Radius, h = zone.Height;

            var glass = FacadeGlass();
            var white = WhitePanel();
            // Fasad: shisha, orqa tomoni yopiq (oq devor) - bino hamma tomondan to'liq ko'rinadi
            MeshObject("Facade", building, Sector(r - 0.25f, r, a0, a1, 0f, h, 96, 8f, FloorHeight * 2f), glass);
            MeshObject("BackWall", building, Sector(r - 0.25f, r, a1, a0 + 360f, 0f, h, 48, 8f, FloorHeight * 2f), white);
            MeshObject("Core", building, Lathe(new[] { V(0.001f, h), V(r - 0.3f, h), V(r - 0.3f, h), V(r - 0.3f, 0f) }, 64), WarmInterior());
            for (int f = 0; f <= zone.Floors; f++)
            {
                float y = f * FloorHeight;
                float over = f == zone.Floors ? 1.2f : 0.9f;
                float thick = f == zone.Floors ? 0.7f : 0.32f;
                MeshObject("Slab" + f, building, Sector(0f, r + over, a0 - 4f, a1 + 4f, y - thick / 2f, y + thick / 2f, 96, 4f, 1f), white);
            }
            // Tomdagi yashil gulzor
            var shrub = AssetDatabase.LoadAssetAtPath<GameObject>(WorldFolder + "Models/Shrub/shrub_01.fbx");
            if (shrub != null)
                for (int i = 0; i < 7; i++)
                {
                    float a = Mathf.Lerp(a0 + 20f, a1 - 20f, i / 6f) * Mathf.Deg2Rad;
                    var s = ShrubVariant(shrub, building, i % 9);
                    s.localPosition = new Vector3(Mathf.Cos(a) * (r - 1.2f), h + 0.35f, Mathf.Sin(a) * (r - 1.2f));
                    s.localScale = Vector3.one * 2.6f;
                }

            // Peshtaxta: tomning oldingi chetida, maydonga qaragan
            float fr = facing * Mathf.Deg2Rad;
            var signPos = new Vector3(Mathf.Cos(fr) * (r + 1.3f), h + 1.5f, Mathf.Sin(fr) * (r + 1.3f));
            var outward = new Vector3(Mathf.Cos(fr), 0f, Mathf.Sin(fr));
            BuildSign(building, zone.Sign, zone.Icon, signPos, Quaternion.LookRotation(-outward), 0.72f);
            // Fasad ostidagi yonib turuvchi chiziq
            MeshObject("GlowBand", building, Sector(r + 0.9f, r + 0.95f, a0, a1, FloorHeight - 0.2f, FloorHeight - 0.12f, 96, 1f, 1f),
                Emissive("Glow_" + zone.Id, zone.Accent, 3f));
        }

        /// <summary>Bino peshtaxtasi: to'q shisha panel, oq ikonka va katta yozuv (dunyo fazosidagi Canvas).</summary>
        static void BuildSign(Transform parent, string text, string icon, Vector3 localPosition, Quaternion localRotation, float scale)
        {
            var kit = Kit();
            var canvasGo = new GameObject("Sign_" + text, typeof(RectTransform), typeof(Canvas));
            var rect = (RectTransform)canvasGo.transform;
            rect.SetParent(parent, false);
            rect.localPosition = localPosition;
            rect.localRotation = localRotation;
            rect.localScale = Vector3.one * 0.012f * scale;
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var label = CreateLabel("Text", rect, kit.Display, text, 110, Color.white, TextAnchor.MiddleLeft);
            float textWidth = label.preferredWidth;
            float width = 110f + 36f + textWidth + 90f;
            rect.sizeDelta = new Vector2(width, 200f);
            var panel = CreateSliced("Panel", rect, kit.RoundFill, 30f, 24f, new Color(0.05f, 0.08f, 0.12f, 0.82f));
            Stretch(panel.rectTransform);
            panel.transform.SetAsFirstSibling();
            var border = CreateSliced("Border", rect, kit.RoundStroke, 30f, 24f, new Color(0.55f, 0.85f, 1f, 0.6f));
            Stretch(border.rectTransform);
            var image = CreateImage("Icon", rect, DrawnIcon(icon), new Vector2(110f, 110f), Vector2.zero, Color.white);
            Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(50f, 0f), new Vector2(110f, 110f));
            Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(50f + 110f + 36f, 0f), new Vector2(textWidth + 20f, 160f));
        }

        static void BuildEntertainmentScreen(Transform root, Zone zone)
        {
            // Katta reklama ekrani: binoning maydonga qaragan tomonida, yonib turadi
            var toPlaza = PlazaCenter - zone.Center;
            toPlaza.y = 0f;
            var dir = toPlaza.normalized;
            var side = Vector3.Cross(Vector3.up, dir);
            var pos = zone.Center + dir * (zone.Radius + 1.6f) - side * 4.5f + Vector3.up * 6.8f;
            var screen = MeshObject("BigScreen", root, Box(9f, 5f, 0.15f), Emissive("ScreenGlow", Color.white, 0.9f, ScreenTexture()));
            screen.transform.position = pos;
            screen.transform.rotation = Quaternion.LookRotation(-dir);
            var frame = MeshObject("ScreenFrame", root, Box(9.4f, 5.4f, 0.1f), Steel());
            frame.transform.position = pos - dir * 0.1f;
            frame.transform.rotation = screen.transform.rotation;
        }

        /// <summary>Ko'ngilochar zona ortidagi charxpalak: sekin aylanadi, kabinalar tik osilib turadi.</summary>
        static void BuildFerrisWheel(Transform root, Vector3 at)
        {
            const float radius = 15f, hub = 17f;
            var white = WhitePanel();
            var steel = Steel();
            var neon = Emissive("WheelNeon", new Color(0.85f, 0.45f, 1f), 2.6f);
            var wheelRoot = new GameObject("FerrisWheel").transform;
            wheelRoot.SetParent(root, false);
            wheelRoot.localPosition = at;
            // Maydonga qarab turadi
            var toPlaza = PlazaCenter - at;
            toPlaza.y = 0f;
            wheelRoot.localRotation = Quaternion.LookRotation(toPlaza.normalized);
            // Tayanch oyoqlar (A shaklida, ikki tomonda)
            foreach (float z in new[] { -1.6f, 1.6f })
                foreach (float x in new[] { -6f, 6f })
                {
                    var leg = MeshObject("Leg", wheelRoot, Box(0.6f, hub + 1f, 0.6f), white);
                    leg.transform.localPosition = new Vector3(x * 0.5f, hub / 2f, z);
                    leg.transform.localRotation = Quaternion.Euler(0f, 0f, x > 0 ? 19f : -19f);
                }
            var wheel = new GameObject("Wheel").transform;
            wheel.SetParent(wheelRoot, false);
            wheel.localPosition = new Vector3(0f, hub, 0f);
            foreach (float z in new[] { -0.9f, 0.9f })
            {
                var rim = MeshObject("Rim", wheel, Torus(radius, 0.18f, 128, 8), neon);
                rim.transform.localPosition = new Vector3(0f, 0f, z);
                rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            for (int i = 0; i < 16; i++)
            {
                var spoke = MeshObject("Spoke", wheel, Box(0.12f, radius, 0.12f), steel);
                spoke.transform.localRotation = Quaternion.Euler(0f, 0f, i * 22.5f);
                spoke.transform.localPosition = spoke.transform.localRotation * new Vector3(0f, radius / 2f, 0f);
            }
            var cabins = new List<Transform>();
            var cabinMat = WorldMaterial("Cabin", Shader.Find("Standard"), m => { m.color = new Color(0.95f, 0.95f, 1f); m.SetFloat("_Glossiness", 0.8f); });
            for (int i = 0; i < 16; i++)
            {
                float a = i * 22.5f * Mathf.Deg2Rad;
                var pivot = new GameObject("Cabin").transform;
                pivot.SetParent(wheel, false);
                pivot.localPosition = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
                At(MeshObject("Body", pivot, Box(1.4f, 1.6f, 1.6f), cabinMat), new Vector3(0f, -1.1f, 0f));
                At(MeshObject("Light", pivot, Box(1.42f, 0.12f, 1.62f), neon), new Vector3(0f, -0.35f, 0f));
                cabins.Add(pivot);
            }
            At(MeshObject("Hub", wheel, Lathe(new[] { V(0.001f, 1.2f), V(1.2f, 1.2f), V(1.2f, 1.2f), V(1.2f, -1.2f) }, 24), steel), Vector3.zero)
                .localRotation = Quaternion.Euler(90f, 0f, 0f);
            var rotator = wheel.gameObject.AddComponent<Rotator>();
            Set(rotator, "axis", Vector3.forward);
            Set(rotator, "speed", 3.5f);
            SetArray(rotator, "keepUpright", cabins.Select(c => (Object)c).ToArray());
            foreach (var t in wheel.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        }

        // ---------------------------------------------------------------- Markaziy minora, ko'prik, osmono'par binolar

        static void BuildCentralTower(Transform root, Vector3 at)
        {
            var tower = new GameObject("CentralTower").transform;
            tower.SetParent(root, false);
            tower.localPosition = at;
            var white = WhitePanel();
            var glass = FacadeGlass();
            // Pastdan tepaga torayib boruvchi shisha minora, oq qovurg'alar bilan
            // Globus ostidagi ingichka minora va globus ustidagi nayza
            MeshObject("Body", tower, Lathe(new[] { V(0.001f, 22f), V(3.2f, 22f), V(3.2f, 22f), V(4.2f, 12f), V(6.5f, 3f), V(6.5f, 3f), V(9f, 3f), V(9f, 0f) }, 64, 8f, 8.8f), glass);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                var leg = MeshObject("Leg" + i, tower, Box(0.7f, 26f, 0.7f), white);
                leg.transform.localPosition = new Vector3(Mathf.Cos(a) * 5.5f, 13f, Mathf.Sin(a) * 5.5f);
                leg.transform.localRotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a))) * Quaternion.Euler(10f, 0f, 0f);
            }
            MeshObject("Spire", tower, Lathe(new[] { V(0.001f, 66f), V(0.35f, 44f), V(0.35f, 44f), V(0.001f, 44f) }, 12), white);

            // Globus: shisha shar, yonib turuvchi meridianlar va aylanib turuvchi halqalar
            var globe = new GameObject("Globe").transform;
            globe.SetParent(tower, false);
            globe.localPosition = new Vector3(0f, 33f, 0f);
            var globeGlass = WorldMaterial("GlobeGlass", Shader.Find("Standard"), m =>
            {
                m.color = new Color(0.45f, 0.75f, 1f, 0.35f);
                m.SetFloat("_Metallic", 0.4f);
                m.SetFloat("_Glossiness", 0.95f);
                m.SetColor("_EmissionColor", new Color(0.2f, 0.55f, 1f) * 0.8f);
                m.EnableKeyword("_EMISSION");
                SetTransparent(m);
            });
            MeshObject("Sphere", globe, Sphere(11f, 64, 32), globeGlass);
            var glow = Emissive("GlobeLines", new Color(0.45f, 0.85f, 1f), 3.5f);
            for (int i = 0; i < 6; i++)
            {
                var m = MeshObject("Meridian" + i, globe, Torus(11.05f, 0.08f, 128, 6), glow);
                m.transform.localRotation = Quaternion.Euler(90f, i * 30f, 0f);
            }
            for (int i = -2; i <= 2; i++)
            {
                float y = i * 4f;
                var p = MeshObject("Parallel" + i, globe, Torus(Mathf.Sqrt(11.05f * 11.05f - y * y), 0.07f, 128, 6), glow);
                p.transform.localPosition = new Vector3(0f, y, 0f);
            }
            var ring = MeshObject("Ring", globe, Torus(16f, 0.22f, 160, 8), Emissive("GlobeRing", new Color(0.5f, 0.85f, 1f), 2.2f));
            ring.transform.localRotation = Quaternion.Euler(12f, 0f, 8f);

        }

        static void BuildSkybridge(Transform root)
        {
            // Maydon ortidan o'tuvchi egri osma yo'lak (ustunlar ustida, shisha panjara)
            var center = new Vector3(0f, 0f, 92f);
            var white = WhitePanel();
            var rail = WorldMaterial("RailGlass", Shader.Find("Standard"), m =>
            {
                m.color = new Color(0.6f, 0.8f, 0.95f, 0.35f);
                m.SetFloat("_Glossiness", 0.95f);
                SetTransparent(m);
            });
            var bridge = new GameObject("Skybridge").transform;
            bridge.SetParent(root, false);
            bridge.localPosition = center;
            MeshObject("Deck", bridge, Sector(46f, 52f, 200f, 340f, 17f, 18.2f, 128, 6f, 1f), white);
            MeshObject("RailOut", bridge, Sector(51.8f, 52f, 200f, 340f, 18.2f, 19.4f, 128, 6f, 1f), rail);
            MeshObject("GlowLine", bridge, Sector(51.9f, 52.05f, 200f, 340f, 17.2f, 17.35f, 128, 1f, 1f), Emissive("GlowCyan", new Color(0.3f, 0.85f, 1f), 3.2f));
            for (int i = 0; i <= 8; i++)
            {
                float a = Mathf.Lerp(205f, 335f, i / 8f) * Mathf.Deg2Rad;
                var pillar = MeshObject("Pillar", bridge, Lathe(new[] { V(1.1f, 17f), V(0.7f, 12f), V(0.7f, 0f) }, 24), white);
                pillar.transform.localPosition = new Vector3(Mathf.Cos(a) * 49f, 0f, Mathf.Sin(a) * 49f);
            }
        }

        static void BuildSkyline(Transform root)
        {
            var random = new System.Random(2026);
            var skyline = new GameObject("Skyline").transform;
            skyline.SetParent(root, false);
            var glassA = FacadeGlass();
            var glassB = TowerGlass();
            glassA = glassB;
            var white = WhitePanel();
            // Shimol tomonda (kamera ko'radigan) ikki qator osmono'par binolar
            for (int i = 0; i < 46; i++)
            {
                float angle = Mathf.Lerp(-72f, 72f, (float)random.NextDouble());
                float distance = Mathf.Lerp(120f, 330f, (float)random.NextDouble());
                var p = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * distance, 0f, Mathf.Cos(angle * Mathf.Deg2Rad) * distance + 20f);
                if (Mathf.Abs(p.x - 9f) < 26f && p.z < 170f)
                    continue; // globusli minoraning orqasi ochiq tursin
                float height = Mathf.Lerp(45f, 170f, Mathf.Pow((float)random.NextDouble(), 0.8f));
                float width = Mathf.Lerp(12f, 26f, (float)random.NextDouble());
                var material = random.NextDouble() < 0.5 ? glassA : glassB;
                var tower = new GameObject("Tower").transform;
                tower.SetParent(skyline, false);
                tower.localPosition = p;
                tower.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 90f, 0f);
                switch (random.Next(4))
                {
                    case 0: // to'g'ri to'rtburchak
                        At(MeshObject("Body", tower, Box(width, height, width * 0.8f, 8f, 8.8f), material), new Vector3(0f, height / 2f, 0f));
                        break;
                    case 1: // silindr, tepasi torayadi
                        MeshObject("Body", tower, Lathe(new[] { V(0.001f, height), V(width * 0.35f, height), V(width * 0.35f, height), V(width * 0.5f, height * 0.7f), V(width * 0.5f, 0f) }, 40, 8f, 8.8f), material);
                        break;
                    case 2: // pog'onali
                        At(MeshObject("Low", tower, Box(width, height * 0.6f, width, 8f, 8.8f), material), new Vector3(0f, height * 0.3f, 0f));
                        At(MeshObject("High", tower, Box(width * 0.65f, height * 0.4f, width * 0.65f, 8f, 8.8f), material), new Vector3(0f, height * 0.8f, 0f));
                        At(MeshObject("Crown", tower, Box(width * 0.7f, 1.2f, width * 0.7f), white), new Vector3(0f, height * 0.6f, 0f));
                        break;
                    default: // egilgan ikki qanotli
                        At(MeshObject("WingA", tower, Box(width * 0.55f, height, width * 0.9f, 8f, 8.8f), material), new Vector3(-width * 0.3f, height / 2f, 0f));
                        At(MeshObject("WingB", tower, Box(width * 0.55f, height * 0.82f, width * 0.9f, 8f, 8.8f), material), new Vector3(width * 0.3f, height * 0.41f, 0f));
                        break;
                }
            }
        }

        static void BuildFlyers(Transform root)
        {
            var body = WhitePanel();
            var cockpit = WorldMaterial("Cockpit", Shader.Find("Standard"), m => { m.color = new Color(0.05f, 0.1f, 0.16f); m.SetFloat("_Metallic", 0.9f); m.SetFloat("_Glossiness", 0.95f); });
            var glow = Emissive("FlyerGlow", new Color(0.4f, 0.85f, 1f), 5f);
            var routes = new[]
            {
                (new Vector3(0f, 42f, 110f), new Vector2(95f, 45f), 5.5f, 0f),
                (new Vector3(20f, 58f, 150f), new Vector2(120f, 60f), -4f, 120f),
                (new Vector3(-25f, 30f, 90f), new Vector2(70f, 38f), 7f, 250f),
                (new Vector3(10f, 75f, 180f), new Vector2(150f, 70f), -3f, 40f),
            };
            foreach (var (center, radius, speed, phase) in routes)
            {
                var flyer = new GameObject("Flyer").transform;
                flyer.SetParent(root, false);
                MeshObject("Hull", flyer, Lathe(new[] { V(0.001f, 3.2f), V(0.5f, 2.4f), V(0.75f, 0.8f), V(0.7f, -1.4f), V(0.3f, -2.6f), V(0.001f, -2.8f) }, 24), body)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                At(MeshObject("Cockpit", flyer, Sphere(0.55f, 16, 8), cockpit), new Vector3(0f, 0.35f, 1.3f));
                At(MeshObject("WingL", flyer, Box(2.6f, 0.08f, 1.1f), body), new Vector3(-1.4f, 0f, -0.4f));
                At(MeshObject("WingR", flyer, Box(2.6f, 0.08f, 1.1f), body), new Vector3(1.4f, 0f, -0.4f));
                At(MeshObject("Engine", flyer, Box(1.2f, 0.25f, 0.1f), glow), new Vector3(0f, 0f, -2.75f));
                var f = flyer.gameObject.AddComponent<Flyer>();
                Set(f, "center", center);
                Set(f, "radius", radius);
                Set(f, "speed", speed);
                Set(f, "phase", phase);
                flyer.localScale = Vector3.one * 1.4f;
                // Harakatlanadi: statik birlashtirishga kirmasin (aks holda joyida qotib qoladi)
                foreach (var t in flyer.GetComponentsInChildren<Transform>())
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                flyer.position = center + Vector3.right * radius.x;
            }
        }

        // ---------------------------------------------------------------- Odamlar

        static void BuildPeople(Transform root, RuntimeAnimatorController maleIdle, RuntimeAnimatorController femaleIdle)
        {
            var maleWalk = IdleController("m_walk_neutral_01", "Walk_Male");
            var femaleWalk = IdleController("f_walk_neutral_01", "Walk_Female");
            var people = new GameObject("People").transform;
            people.SetParent(root, false);
            var models = AvatarList.Select(a => (a, AssetDatabase.LoadAssetAtPath<GameObject>(a.ModelPath))).Where(p => p.Item2 != null).ToArray();
            if (models.Length == 0)
                return;

            // Favvora atrofida aylana yo'l va maydon bo'ylab to'g'ri yo'llar
            var circle = Enumerable.Range(0, 24).Select(i =>
            {
                float a = i * Mathf.PI * 2f / 24f;
                return new Vector3(Mathf.Cos(a) * 9.5f, 0f, 17f + Mathf.Sin(a) * 9.5f);
            }).ToArray();
            var reverse = circle.Reverse().Select(p => (p - new Vector3(0f, 0f, 17f)) * 1.15f + new Vector3(0f, 0f, 17f)).ToArray();
            var routes = new[]
            {
                (circle, 0.0f), (circle, 0.33f), (reverse, 0.1f), (reverse, 0.6f),
                (new[] { new Vector3(-16f, 0f, 30f), new Vector3(15f, 0f, 29f) }, 0.2f),
                (new[] { new Vector3(12f, 0f, 38f), new Vector3(-12f, 0f, 37f) }, 0.7f),
                (new[] { new Vector3(-12f, 0f, 6f), new Vector3(-6f, 0f, 30f) }, 0.4f),
                (new[] { new Vector3(13f, 0f, 5f), new Vector3(7f, 0f, 31f) }, 0.85f),
            };
            for (int i = 0; i < routes.Length; i++)
            {
                var (info, model) = models[(i * 3 + 1) % models.Length];
                var go = (GameObject)Object.Instantiate(model, people);
                go.name = "Walker_" + info.Id;
                var animator = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
                animator.runtimeAnimatorController = info.Gender == "female" ? femaleWalk : maleWalk;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                var walker = go.AddComponent<Walker>();
                SetVectorArray(walker, "path", routes[i].Item1);
                Set(walker, "start", routes[i].Item2);
                Set(walker, "speed", 1.15f + (i % 3) * 0.1f);
            }

            // Suhbatlashib turgan ikki kishi
            foreach (var (position, look, index) in new[] { (new Vector3(-5.2f, 0f, 24f), new Vector3(-4.2f, 0f, 24.4f), 5), (new Vector3(-4.2f, 0f, 24.4f), new Vector3(-5.2f, 0f, 24f), 6) })
            {
                var (info, model) = models[index % models.Length];
                var go = (GameObject)Object.Instantiate(model, people);
                go.name = "Standing_" + info.Id;
                go.transform.position = position;
                go.transform.rotation = Quaternion.LookRotation(look - position);
                var animator = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
                animator.runtimeAnimatorController = info.Gender == "female" ? femaleIdle : maleIdle;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }
        }

        /// <summary>
        /// Buta modelida 9 xil buta yonma-yon turadi (shrub_01_a … _i): bittasini olib, markazga qo'yadi.
        /// </summary>
        static Transform ShrubVariant(GameObject shrub, Transform parent, int index)
        {
            var go = (GameObject)Object.Instantiate(shrub, parent);
            go.name = "Shrub";
            var parts = go.GetComponentsInChildren<MeshRenderer>(true);
            var keep = parts[index % parts.Length];
            foreach (var part in parts)
                if (part != keep)
                    Object.DestroyImmediate(part.gameObject);
            // Tanlangan buta markazini ota obyekt nuqtasiga keltirish
            var bounds = keep.bounds;
            keep.transform.position += go.transform.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            return go.transform;
        }

        static void SetVectorArray(Object target, string field, Vector3[] values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).vector3Value = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- Zona kartalari uchun rasmlar

        /// <summary>Har bir zona binosini chiroyli burchakdan chizib, karta rasmi qiladi (World/Cards/&lt;id&gt;.png).</summary>
        static Dictionary<string, Sprite> RenderZoneCards()
        {
            var result = new Dictionary<string, Sprite>();
            Directory.CreateDirectory(ZoneCards);
            if (!CanRender)
            {
                foreach (var zone in Zones)
                    if (File.Exists(ZoneCards + zone.Id + ".png"))
                        result[zone.Id] = LoadSprite(ZoneCards + zone.Id + ".png");
                return result;
            }
            const int width = 540, height = 300;
            var camera = new GameObject("CardCamera").AddComponent<Camera>();
            camera.fieldOfView = 32f;
            camera.farClipPlane = 1500f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.aspect = (float)width / height;
            var rt = new RenderTexture(width * 2, height * 2, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            camera.targetTexture = rt;
            foreach (var zone in Zones)
            {
                var toPlaza = PlazaCenter - zone.Center;
                toPlaza.y = 0f;
                var dir = toPlaza.normalized;
                var side = Vector3.Cross(Vector3.up, dir);
                var look = zone.Center + Vector3.up * (zone.Height * 0.6f + 0.5f);
                // Bino peshtaxtasi bilan kadrga sig'adi: kamera maydon tomonidan, biroz yondan qaraydi
                float distance = zone.Radius + Mathf.Max(20f, zone.Height * 1.25f);
                camera.transform.position = zone.Center + dir * distance + side * 5f + Vector3.up * (zone.Height * 0.5f + 3f);
                if (zone.Id == "entertainment")
                {
                    // Charxpalak ham kadrga tushsin
                    look = Vector3.Lerp(look, new Vector3(40f, 15f, 44f), 0.5f);
                    camera.transform.position += dir * 14f + Vector3.up * 3f;
                }
                camera.transform.rotation = Quaternion.LookRotation(look - camera.transform.position);
                camera.Render();
                var small = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(rt, small);
                var texture = ReadRenderTexture(small);
                RenderTexture.ReleaseTemporary(small);
                string path = ZoneCards + zone.Id + ".png";
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                result[zone.Id] = LoadSprite(path);
            }
            camera.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camera.gameObject);
            return result;
        }

        // ---------------------------------------------------------------- Materiallar

        static Material WorldMaterial(string name, Shader shader, System.Action<Material> setup)
        {
            string path = WorldMaterials + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            setup(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material Emissive(string name, Color color, float intensity, Texture texture = null) =>
            WorldMaterial(name, Shader.Find("Standard"), m =>
            {
                m.color = Color.black;
                m.SetFloat("_Glossiness", 0.5f);
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * intensity);
                m.SetTexture("_EmissionMap", texture);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            });

        static Material WhitePanel() => WorldMaterial("WhitePanel", Shader.Find("Standard"), m =>
        {
            m.color = new Color(0.93f, 0.94f, 0.96f);
            m.SetFloat("_Metallic", 0.1f);
            m.SetFloat("_Glossiness", 0.6f);
        });

        static Material Steel() => WorldMaterial("Steel", Shader.Find("Standard"), m =>
        {
            m.color = new Color(0.62f, 0.65f, 0.7f);
            m.SetFloat("_Metallic", 0.9f);
            m.SetFloat("_Glossiness", 0.7f);
        });

        static Material WarmInterior() => WorldMaterial("WarmInterior", Shader.Find("Standard"), m =>
        {
            m.color = new Color(0.3f, 0.26f, 0.22f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.55f) * 0.6f);
        });

        /// <summary>Zona binolari: aks ettiruvchi ko'k shisha, ichkarida iliq chiroqlar yongan qavatlar.</summary>
        static Material FacadeGlass() => WorldMaterial("FacadeGlass", Shader.Find("Standard"), m =>
        {
            var (albedo, emission) = FacadeTextures();
            m.SetTexture("_MainTex", albedo);
            m.color = Color.white;
            m.SetFloat("_Metallic", 0.75f);
            m.SetFloat("_Glossiness", 0.93f);
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", emission);
            m.SetColor("_EmissionColor", new Color(1f, 0.82f, 0.6f) * 1.1f);
        });

        /// <summary>Uzoqdagi osmono'par binolar: mayda oynali ko'k shisha, osmonni aks ettiradi.</summary>
        static Material TowerGlass() => WorldMaterial("TowerGlass", Shader.Find("Standard"), m =>
        {
            m.SetTexture("_MainTex", TowerTexture());
            m.color = Color.white;
            m.SetFloat("_Metallic", 0.85f);
            m.SetFloat("_Glossiness", 0.9f);
        });

        static void SetTransparent(Material m)
        {
            m.SetFloat("_Mode", 3f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHABLEND_ON");
            m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        /// <summary>
        /// Fasad teksturasi (bir qavat = 128 px): ko'k-kulrang oynalar, ingichka oq ramkalar, har qavat tepasida
        /// iliq yorug'lik chizig'i (faqat emission rasmida). Bir marta yaratiladi.
        /// </summary>
        static (Texture2D albedo, Texture2D emission) FacadeTextures()
        {
            string albedoPath = WorldFolder + "Textures/Facade.png", emissionPath = WorldFolder + "Textures/Facade_Emission.png";
            if (!File.Exists(albedoPath) || !File.Exists(emissionPath))
            {
                const int size = 256;
                var albedo = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var emission = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var random = new System.Random(11);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        int fy = y % 128, fx = x % 64;
                        bool slab = fy < 10;
                        bool mullion = fx < 3;
                        float shade = 0.18f + 0.1f * (fy / 128f);
                        Color c = slab ? new Color(0.9f, 0.91f, 0.93f) : mullion ? new Color(0.75f, 0.78f, 0.82f) : new Color(shade * 0.7f, shade * 0.95f, shade * 1.25f);
                        albedo.SetPixel(x, y, c);
                        // Shipdagi yorug'lik: qavatning yuqori qismi, ba'zi derazalarda kuchliroq
                        float light = !slab && !mullion && fy > 100 ? Mathf.InverseLerp(100f, 124f, fy) : 0f;
                        emission.SetPixel(x, y, new Color(light, light, light));
                    }
                // Ba'zi derazalar ichkarisi yorqinroq
                for (int k = 0; k < 10; k++)
                {
                    int wx = random.Next(4) * 64, wy = random.Next(2) * 128;
                    for (int y = wy + 12; y < wy + 100; y++)
                        for (int x = wx + 4; x < wx + 64; x++)
                            emission.SetPixel(x, y, emission.GetPixel(x, y) + new Color(0.22f, 0.22f, 0.22f));
                }
                File.WriteAllBytes(albedoPath, albedo.EncodeToPNG());
                File.WriteAllBytes(emissionPath, emission.EncodeToPNG());
                Object.DestroyImmediate(albedo);
                Object.DestroyImmediate(emission);
                AssetDatabase.ImportAsset(albedoPath);
                AssetDatabase.ImportAsset(emissionPath);
            }
            return (AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath), AssetDatabase.LoadAssetAtPath<Texture2D>(emissionPath));
        }

        /// <summary>
        /// Osmono'par bino oynalari: bir takror = 8 m x 8.8 m (4 ustun, 2 qavat). To'q ko'k shisha (osmonni aks
        /// ettiradi), ingichka kumush ramkalar, qavatlar orasida kulrang chiziq.
        /// </summary>
        static Texture2D TowerTexture()
        {
            string path = WorldFolder + "Textures/TowerFacade.png";
            if (!File.Exists(path))
            {
                const int size = 256;
                var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var random = new System.Random(5);
                var tints = Enumerable.Range(0, 8).Select(_ => 0.85f + (float)random.NextDouble() * 0.3f).ToArray();
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        int fy = y % 128, fx = x % 64;
                        Color c;
                        if (fy < 8) c = new Color(0.55f, 0.6f, 0.66f);            // qavat orasidagi plita
                        else if (fx < 2) c = new Color(0.72f, 0.76f, 0.82f);      // vertikal ramka
                        else
                        {
                            float tint = tints[(x / 64) + (y / 128) * 4];
                            float v = (fy - 8) / 120f;
                            c = new Color(0.08f, 0.15f, 0.26f) * tint + new Color(0.06f, 0.08f, 0.1f) * v;
                        }
                        t.SetPixel(x, y, c);
                    }
                File.WriteAllBytes(path, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Katta ekrandagi "reklama": binafsha-ko'k gradient va yorug' shakllar.</summary>
        static Texture2D ScreenTexture()
        {
            string path = WorldFolder + "Textures/Screen.png";
            if (!File.Exists(path))
            {
                const int w = 512, h = 288;
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float u = x / (float)w, v = y / (float)h;
                        Color c = Color.Lerp(new Color(0.25f, 0.1f, 0.6f), new Color(0.1f, 0.55f, 0.95f), u * 0.7f + v * 0.3f);
                        // Yorug' doiralar (sahna chiroqlari)
                        float glow = Mathf.Exp(-((u - 0.3f) * (u - 0.3f) + (v - 0.6f) * (v - 0.6f)) * 30f) + Mathf.Exp(-((u - 0.72f) * (u - 0.72f) + (v - 0.35f) * (v - 0.35f)) * 45f);
                        c += new Color(1f, 0.6f, 0.95f) * glow * 0.8f;
                        t.SetPixel(x, y, c);
                    }
                File.WriteAllBytes(path, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ---------------------------------------------------------------- Mesh yasash

        static Vector2 V(float radius, float height) => new Vector2(radius, height);

        static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        static Transform At(GameObject go, Vector3 localPosition)
        {
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        /// <summary>
        /// Aylanish jismi: profil (radius, balandlik) o'q atrofida aylantiriladi. Profilda takrorlangan nuqta -
        /// o'tkir qirra. uScale/vScale: tekstura necha metrda takrorlanadi (0 - bir marta).
        /// </summary>
        static Mesh Lathe(Vector2[] profile, int segments, float uScale = 0f, float vScale = 0f)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            float[] along = new float[profile.Length];
            for (int i = 1; i < profile.Length; i++)
                along[i] = along[i - 1] + Vector2.Distance(profile[i], profile[i - 1]);
            float maxRadius = profile.Max(p => p.x);
            float circumference = 2f * Mathf.PI * maxRadius;
            for (int i = 0; i < profile.Length; i++)
                for (int s = 0; s <= segments; s++)
                {
                    float a = s * Mathf.PI * 2f / segments;
                    vertices.Add(new Vector3(Mathf.Cos(a) * profile[i].x, profile[i].y, Mathf.Sin(a) * profile[i].x));
                    float u = uScale > 0f ? s / (float)segments * circumference / uScale : s / (float)segments;
                    float v = vScale > 0f ? profile[i].y / vScale : along[i] / Mathf.Max(along[profile.Length - 1], 0.001f);
                    uvs.Add(new Vector2(u, v));
                }
            int row = segments + 1;
            for (int i = 0; i < profile.Length - 1; i++)
            {
                if (Vector2.Distance(profile[i], profile[i + 1]) < 1e-5f)
                    continue; // o'tkir qirra: ikki qator orasida uchburchak yo'q
                for (int s = 0; s < segments; s++)
                {
                    int a = i * row + s, b = a + 1, c = a + row, d = c + 1;
                    triangles.AddRange(new[] { a, b, c, b, d, c });
                }
            }
            return Finish("Lathe", vertices, uvs, triangles);
        }

        /// <summary>Halqa sektori: [rInner, rOuter] x [a0, a1] (gradus) x [y0, y1]. Tekis qirralar alohida.</summary>
        static Mesh Sector(float rInner, float rOuter, float a0, float a1, float y0, float y1, int segments, float uScale, float vScale)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            void Quad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector2 t0, Vector2 t1, Vector2 t2, Vector2 t3)
            {
                int i = vertices.Count;
                vertices.AddRange(new[] { p0, p1, p2, p3 });
                uvs.AddRange(new[] { t0, t1, t2, t3 });
                triangles.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
            Vector3 P(float r, float a, float y) => new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * r, y, Mathf.Sin(a * Mathf.Deg2Rad) * r);
            float arc = (a1 - a0) * Mathf.Deg2Rad * rOuter;
            for (int s = 0; s < segments; s++)
            {
                float b0 = Mathf.Lerp(a0, a1, s / (float)segments), b1 = Mathf.Lerp(a0, a1, (s + 1) / (float)segments);
                float u0 = arc * s / segments / uScale, u1 = arc * (s + 1) / segments / uScale;
                float v0 = y0 / vScale, v1 = y1 / vScale;
                // Tashqi (qavariq) yuz
                Quad(P(rOuter, b0, y0), P(rOuter, b0, y1), P(rOuter, b1, y1), P(rOuter, b1, y0), V(u0, v0), V(u0, v1), V(u1, v1), V(u1, v0));
                // Ichki yuz
                if (rInner > 0.001f)
                    Quad(P(rInner, b1, y0), P(rInner, b1, y1), P(rInner, b0, y1), P(rInner, b0, y0), V(u1, v0), V(u1, v1), V(u0, v1), V(u0, v0));
                // Ust va ost
                Quad(P(rInner, b0, y1), P(rInner, b1, y1), P(rOuter, b1, y1), P(rOuter, b0, y1), V(u0, 0f), V(u1, 0f), V(u1, 1f), V(u0, 1f));
                Quad(P(rOuter, b0, y0), P(rOuter, b1, y0), P(rInner, b1, y0), P(rInner, b0, y0), V(u0, 1f), V(u1, 1f), V(u1, 0f), V(u0, 0f));
            }
            // Yon qirralar
            Quad(P(rInner, a0, y0), P(rInner, a0, y1), P(rOuter, a0, y1), P(rOuter, a0, y0), V(0f, 0f), V(0f, 1f), V(1f, 1f), V(1f, 0f));
            Quad(P(rOuter, a1, y0), P(rOuter, a1, y1), P(rInner, a1, y1), P(rInner, a1, y0), V(0f, 0f), V(0f, 1f), V(1f, 1f), V(1f, 0f));
            return Finish("Sector", vertices, uvs, triangles, smoothAngle: 30f);
        }

        static Mesh Box(float w, float h, float d, float uScale = 0f, float vScale = 0f)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var half = new Vector3(w, h, d) / 2f;
            var faces = new[]
            {
                (Vector3.forward, Vector3.left, w, d), (Vector3.back, Vector3.right, w, d), (Vector3.right, Vector3.forward, d, w),
                (Vector3.left, Vector3.back, d, w), (Vector3.up, Vector3.right, w, h), (Vector3.down, Vector3.right, w, h),
            };
            foreach (var (n, t, faceWidth, _) in faces)
            {
                var b = Vector3.Cross(n, t);
                float extentT = Mathf.Abs(Vector3.Dot(half, t)), extentB = Mathf.Abs(Vector3.Dot(half, b)), extentN = Mathf.Abs(Vector3.Dot(half, n));
                var c = n * extentN;
                int i = vertices.Count;
                vertices.Add(c - t * extentT - b * extentB);
                vertices.Add(c - t * extentT + b * extentB);
                vertices.Add(c + t * extentT + b * extentB);
                vertices.Add(c + t * extentT - b * extentB);
                float uw = uScale > 0f ? extentT * 2f / uScale : 1f, vh = vScale > 0f ? extentB * 2f / vScale : 1f;
                uvs.AddRange(new[] { V(0f, 0f), V(0f, vh), V(uw, vh), V(uw, 0f) });
                triangles.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            }
            return Finish("Box", vertices, uvs, triangles);
        }

        static Mesh Disc(float radius, float height, int segments, float uvScale)
        {
            var profile = height > 0f
                ? new[] { V(0.001f, height), V(radius, height), V(radius, height), V(radius, 0f) }
                : new[] { V(0.001f, 0f), V(radius, 0f) };
            var mesh = Lathe(profile, segments);
            // Tekis yuzada planar UV (plitkalar to'g'ri joylashadi)
            var v = mesh.vertices;
            var uv = new Vector2[v.Length];
            for (int i = 0; i < v.Length; i++)
                uv[i] = new Vector2(v[i].x / (radius * 2f) * uvScale, v[i].z / (radius * 2f) * uvScale);
            mesh.uv = uv;
            return mesh;
        }

        static Mesh Torus(float radius, float tube, int segments, int sides)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int s = 0; s <= segments; s++)
            {
                float a = s * Mathf.PI * 2f / segments;
                var center = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                for (int k = 0; k <= sides; k++)
                {
                    float b = k * Mathf.PI * 2f / sides;
                    var normal = new Vector3(Mathf.Cos(a) * Mathf.Cos(b), Mathf.Sin(b), Mathf.Sin(a) * Mathf.Cos(b));
                    vertices.Add(center + normal * tube);
                    uvs.Add(V(s / (float)segments, k / (float)sides));
                }
            }
            int row = sides + 1;
            for (int s = 0; s < segments; s++)
                for (int k = 0; k < sides; k++)
                {
                    int a = s * row + k, b = a + 1, c = a + row, d = c + 1;
                    triangles.AddRange(new[] { a, b, c, b, d, c });
                }
            return Finish("Torus", vertices, uvs, triangles);
        }

        static Mesh Sphere(float radius, int segments, int rings)
        {
            var profile = new Vector2[rings + 1];
            for (int i = 0; i <= rings; i++)
            {
                float a = Mathf.PI * i / rings;
                profile[i] = new Vector2(Mathf.Max(Mathf.Sin(a) * radius, 0.001f), Mathf.Cos(a) * radius);
            }
            return Lathe(profile, segments);
        }

        static Mesh Finish(string name, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles, float smoothAngle = 0f)
        {
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

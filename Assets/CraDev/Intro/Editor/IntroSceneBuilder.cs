using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev.Intro.EditorTools
{
    /// <summary>
    /// "CraDev > Intro sahnasini yaratish" menyusi: Intro sahnasini noldan quradi,
    /// saqlaydi va Build Settings'da birinchi o'ringa qo'yadi.
    /// Sahnani qo'lda yig'ish shart emas: istalgan payt shu buyruq bilan qayta yaratish mumkin.
    /// </summary>
    static class IntroSceneBuilder
    {
        const string ScenePath = "Assets/CraDev/Scenes/Intro.unity";
        const string AudioPath = "Assets/CraDev/Intro/Audio/CraDev_IntroSound.wav";

        // Rasmlar 2x o'lchamda chizilgan (4K ekranda ham tiniq bo'lishi uchun),
        // sahnada esa 1920x1080 mo'ljal o'lchamida joylashtiriladi.
        const float ArtScale = 2f;
        static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        // Joylashuvlar Design/Logo/layout.json dan olingan (ekran markaziga nisbatan)
        static readonly Vector2 EmblemPos = new Vector2(0f, 140f);
        static readonly Vector2 WordmarkPos = new Vector2(2.5f, -70.5f);
        static readonly Vector2 WordmarkGlowPos = new Vector2(-2.5f, -71f);
        static readonly Vector2 DividerPos = new Vector2(0f, -167.5f);
        static readonly Vector2 TaglinePos = new Vector2(-0.5f, -205f);
        const float GlowSize = 1100f;

        static readonly Color BackgroundColor = new Color32(5, 7, 13, 255);

        [MenuItem("CraDev/Intro sahnasini yaratish", priority = 1)]
        static void Build()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Intro sahnasi",
                    "Intro sahnasi allaqachon mavjud. Uni noldan qayta yaratamizmi? Qo'lda qilingan o'zgarishlar yo'qoladi.",
                    "Ha, qayta yaratish", "Bekor qilish"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var emblemSprite = LoadSprite("CraDev_Emblem");
            var glowSprite = LoadSprite("CraDev_Glow");
            var wordmarkSprite = LoadSprite("CraDev_Wordmark");
            var wordmarkGlowSprite = LoadSprite("CraDev_WordmarkGlow");
            var shineSprite = LoadSprite("CraDev_Shine");
            var dividerSprite = LoadSprite("CraDev_Divider");
            var taglineSprite = LoadSprite("CraDev_Tagline");
            var particleSprite = LoadSprite("CraDev_Particle");
            var introSound = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);

            if (new Object[] { emblemSprite, glowSprite, wordmarkSprite, wordmarkGlowSprite, shineSprite,
                    dividerSprite, taglineSprite, particleSprite }.Any(s => s == null))
            {
                EditorUtility.DisplayDialog("Intro sahnasi",
                    "Logo rasmlari topilmadi. Ular " + IntroArtImporter.ArtFolder + " papkasida bo'lishi kerak.", "OK");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Kamera: faqat qora fon. Intro to'liq UI (Screen Space - Overlay) orqali chiziladi,
            // shuning uchun Built-in, URP va HDRP'da bir xil ishlaydi.
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
            cameraGo.AddComponent<AudioListener>();

            var canvasGo = new GameObject("IntroCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var root = canvasGo.transform;

            var background = CreateImage("Background", root, null, Vector2.zero, Vector2.zero, BackgroundColor);
            Stretch(background.rectTransform);

            var particlesRect = CreateRect("Particles", root);
            Stretch(particlesRect);
            var particles = particlesRect.gameObject.AddComponent<IntroParticles>();
            SetField(particles, "sprite", particleSprite);

            var logo = CreateRect("Logo", root);

            var glow = CreateImage("Glow", logo, glowSprite, new Vector2(GlowSize, GlowSize), EmblemPos, Color.white);
            var emblem = CreateImage("Emblem", logo, emblemSprite, NativeSize(emblemSprite), EmblemPos, Color.white);

            // Yozuvni markazdan ochish uchun yumshoq chetli RectMask2D
            var reveal = CreateRect("WordmarkReveal", logo);
            reveal.anchoredPosition = WordmarkPos;
            reveal.sizeDelta = new Vector2(820f, 260f);
            var revealMask = reveal.gameObject.AddComponent<RectMask2D>();
            revealMask.softness = new Vector2Int(90, 0);

            var wordmarkGlow = CreateImage("WordmarkGlow", reveal, wordmarkGlowSprite, NativeSize(wordmarkGlowSprite),
                WordmarkGlowPos - WordmarkPos, Color.white);
            var wordmark = CreateImage("Wordmark", reveal, wordmarkSprite, NativeSize(wordmarkSprite), Vector2.zero, Color.white);

            // Nur faqat harflar ustida ko'rinishi uchun yozuvning o'zi niqob (Mask) vazifasini bajaradi
            wordmark.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var shine = CreateImage("Shine", wordmark.transform, shineSprite, new Vector2(80f, 260f), Vector2.zero,
                new Color(1f, 1f, 1f, 0f));
            shine.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -18f);

            var divider = CreateImage("Divider", logo, dividerSprite, NativeSize(dividerSprite), DividerPos, Color.white);
            var tagline = CreateImage("Tagline", logo, taglineSprite, NativeSize(taglineSprite), TaglinePos, Color.white);

            // Qora parda: intro boshida ochiladi, oxirida yopiladi. Tahrirlashda xalaqit bermasligi uchun shaffof saqlanadi.
            var fader = CreateImage("Fader", root, null, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            Stretch(fader.rectTransform);

            var directorGo = new GameObject("IntroDirector");
            var audio = directorGo.AddComponent<AudioSource>();
            audio.clip = introSound;
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;
            var sequence = directorGo.AddComponent<IntroSequence>();
            SetField(sequence, "logoRoot", logo);
            SetField(sequence, "glow", glow);
            SetField(sequence, "emblem", emblem);
            SetField(sequence, "wordmarkReveal", reveal);
            SetField(sequence, "wordmarkGlow", wordmarkGlow);
            SetField(sequence, "shine", shine);
            SetField(sequence, "divider", divider);
            SetField(sequence, "tagline", tagline);
            SetField(sequence, "fader", fader);
            SetField(sequence, "particles", particles);
            SetField(sequence, "sound", audio);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                EditorUtility.DisplayDialog("Intro sahnasi", "Sahnani saqlab bo'lmadi: " + ScenePath, "OK");
                return;
            }

            // Intro o'yinda birinchi ochiladigan sahna bo'lishi kerak
            var buildScenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            buildScenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            if (PlayerSettings.companyName == "DefaultCompany")
                PlayerSettings.companyName = "CraDev";

            Selection.activeGameObject = directorGo;
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
            Debug.Log("[CraDev] Intro sahnasi yaratildi: " + ScenePath + ". Ko'rish uchun Play tugmasini bosing.");
        }

        static Sprite LoadSprite(string name)
        {
            string path = IntroArtImporter.ArtFolder + name + ".png";
            if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                importer.textureType != TextureImporterType.Sprite)
            {
                IntroArtImporter.Apply(importer);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Vector2 NativeSize(Sprite sprite) => sprite.rect.size / ArtScale;

        static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        static Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 position, Color color)
        {
            var rect = CreateRect(name, parent);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Komponentning private [SerializeField] maydoniga qiymat yozadi.</summary>
        static void SetField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
                throw new System.ArgumentException($"{target.GetType().Name} da \"{field}\" maydoni topilmadi.");
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

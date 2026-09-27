using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev.EditorTools
{
    /// <summary>Sahnalarni koddan qurish uchun yordamchi funksiyalar (UI elementlari, rasmlar, maydonlar).</summary>
    static class UiBuild
    {
        // Rasmlar 2x o'lchamda chizilgan (4K ekranda ham tiniq bo'lishi uchun),
        // sahnada esa 1920x1080 mo'ljal o'lchamida joylashtiriladi.
        public const float ArtScale = 2f;
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        /// <summary>Yangi bo'sh sahna: qora fonli kamera va 1920x1080 ga moslashuvchi Canvas.</summary>
        public static Transform NewUiScene(out UnityEngine.SceneManagement.Scene scene)
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Kamera faqat qora fon beradi. Hamma narsa UI (Screen Space - Overlay) orqali chiziladi,
            // shuning uchun Built-in, URP va HDRP'da bir xil ishlaydi.
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
            cameraGo.AddComponent<AudioListener>();

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            return canvasGo.transform;
        }

        public static Sprite LoadSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                importer.textureType != TextureImporterType.Sprite)
            {
                CraDevArtImporter.Apply(importer);
                importer.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException("Rasm topilmadi: " + path);
            return sprite;
        }

        public static Vector2 NativeSize(Sprite sprite) => sprite.rect.size / ArtScale;

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        public static Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 position, Color color)
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

        /// <summary>Rasmni o'zining (2x dan qaytarilgan) o'lchamida yaratadi.</summary>
        public static Image CreateSprite(string name, Transform parent, Sprite sprite, Vector2 position) =>
            CreateImage(name, parent, sprite, NativeSize(sprite), position, Color.white);

        public static Text CreateText(string name, Transform parent, Font font, string text, int size, Vector2 position, Color color)
        {
            var rect = CreateRect(name, parent);
            rect.sizeDelta = new Vector2(600f, size * 1.6f);
            rect.anchoredPosition = position;
            var label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.text = text;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        public static Image CreateFullscreen(string name, Transform parent, Color color)
        {
            var image = CreateImage(name, parent, null, Vector2.zero, Vector2.zero, color);
            Stretch(image.rectTransform);
            return image;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // --- Komponentlarning private [SerializeField] maydonlariga qiymat yozish ---

        public static void Set(Object target, string field, Object value) => Edit(target, field, p => p.objectReferenceValue = value);
        public static void Set(Object target, string field, string value) => Edit(target, field, p => p.stringValue = value);
        public static void Set(Object target, string field, float value) => Edit(target, field, p => p.floatValue = value);
        public static void Set(Object target, string field, int value) => Edit(target, field, p => p.intValue = value);
        public static void Set(Object target, string field, Color value) => Edit(target, field, p => p.colorValue = value);
        public static void Set(Object target, string field, Vector2 value) => Edit(target, field, p => p.vector2Value = value);

        static void Edit(Object target, string field, System.Action<SerializedProperty> assign)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
                throw new System.ArgumentException($"{target.GetType().Name} da \"{field}\" maydoni topilmadi.");
            assign(property);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static AudioSource AddSound(GameObject go, string clipPath)
        {
            var audio = go.AddComponent<AudioSource>();
            audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;
            return audio;
        }

        public static void Save(UnityEngine.SceneManagement.Scene scene, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new IOException("Sahnani saqlab bo'lmadi: " + path);
        }
    }
}

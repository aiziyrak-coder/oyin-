using UnityEngine;
using UnityEngine.UI;

namespace CraDev.World
{
    /// <summary>
    /// Kichik xarita: shimol doim yuqorida, o'yinchi markazda; chizish ko'pi bilan 30 Hz.
    /// Xarita kamerasini faqat shu komponent yaratadi va sozlaydi (yagona manba): builder kamera qurmaydi.
    /// </summary>
    public sealed class WorldMinimap : MonoBehaviour
    {
        const float ViewRadius = 32f;       // ortografik yarim balandlik, metr
        const float CameraHeight = 60f;     // o'yinchidan balandlik
        const float FarClip = 150f;
        static readonly Color Background = new Color(.2f, .29f, .24f);

        Transform target;
        Camera mapCamera;
        RectTransform heading;
        RenderTexture texture;
        float nextRender;

        public RenderTexture Texture => texture;
        public Camera MapCamera => mapCamera;

        public void Configure(Transform follow, RawImage output, RectTransform marker)
        {
            target = follow;
            heading = marker;
            if (!mapCamera) mapCamera = new GameObject("WorldMapCamera").AddComponent<Camera>();
            mapCamera.enabled = false;
            mapCamera.orthographic = true;
            mapCamera.orthographicSize = ViewRadius;
            mapCamera.nearClipPlane = .1f;
            mapCamera.farClipPlane = FarClip;
            mapCamera.cullingMask = ~((1 << 5) | (1 << 8));
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = Background;
            mapCamera.allowHDR = false;
            mapCamera.allowMSAA = false;
            mapCamera.useOcclusionCulling = false;
            mapCamera.depthTextureMode = DepthTextureMode.None;
            mapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            if (!texture)
            {
                texture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32)
                {
                    name = "WorldMinimap256",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    antiAliasing = 1,
                    useMipMap = false
                };
                texture.Create();
            }
            mapCamera.targetTexture = texture;
            output.texture = texture;
            output.raycastTarget = false;
        }

        void LateUpdate()
        {
            if (!target || !mapCamera || !texture) return;
            mapCamera.transform.position = target.position + Vector3.up * CameraHeight;
            if (heading) heading.localRotation = Quaternion.Euler(0f, 0f, -target.eulerAngles.y);
            if (Time.unscaledTime < nextRender) return;
            nextRender = Time.unscaledTime + 1f / 30f;
            mapCamera.Render();
        }

        void OnDestroy()
        {
            if (mapCamera) mapCamera.targetTexture = null;
            if (texture)
            {
                texture.Release();
                Destroy(texture);
            }
            if (mapCamera) Destroy(mapCamera.gameObject);
        }
    }

    /// <summary>Rasmsiz tiniq xarita ko'rsatkichi. Uchburchakning uchi qarash yo'nalishini bildiradi.</summary>
    public sealed class WorldMapArrow : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            var c = color;
            Vector2 center = rect.center;
            float w = rect.width * .5f, h = rect.height * .5f;
            AddTriangle(vh, center + new Vector2(0f, h), center + new Vector2(-w, -h),
                center + new Vector2(0f, -h * .5f), c);
            AddTriangle(vh, center + new Vector2(0f, h), center + new Vector2(0f, -h * .5f),
                center + new Vector2(w, -h), c);
        }

        static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int index = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero);
            vh.AddVert(b, color, Vector2.zero);
            vh.AddVert(c, color, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
        }
    }
}

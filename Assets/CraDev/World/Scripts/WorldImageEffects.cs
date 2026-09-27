using UnityEngine;

namespace CraDev.World
{
    /// <summary>HDR ranglarini ekranga moslaydi; yaqin tutashgan sirtlarga yengil soya beradi.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class WorldImageEffects : MonoBehaviour
    {
        [SerializeField] Material material;
        [SerializeField, Range(0f, 1.5f)] float aoIntensity = .45f;
        [SerializeField, Range(.15f, 1.5f)] float radius = .7f;
        [SerializeField, Range(.25f, 2.5f)] float exposure = 1f;
        [SerializeField, Range(0f, .15f)] float vignette = .045f;
        public bool AOEnabled = true;

        Camera viewCamera;
        Material runtimeMaterial;
        Material sourceMaterial;
        DepthTextureMode addedDepthModes;

        static readonly int AoParameters = Shader.PropertyToID("_AOParameters");
        static readonly int ViewParameters = Shader.PropertyToID("_ViewParameters");
        static readonly int ColorParameters = Shader.PropertyToID("_ColorParameters");
        static readonly int BlurDirection = Shader.PropertyToID("_BlurDirection");
        static readonly int OcclusionTexture = Shader.PropertyToID("_OcclusionTexture");

        public bool IsSupported => material != null && material.shader != null && material.shader.isSupported;

        void OnEnable()
        {
            viewCamera = GetComponent<Camera>();
            RequestDepthNormals();
        }

        void OnPreCull() => RequestDepthNormals();

        void RequestDepthNormals()
        {
            if (viewCamera == null) viewCamera = GetComponent<Camera>();
            bool needsDepth = AOEnabled && aoIntensity > .001f && !viewCamera.orthographic;
            if (needsDepth)
            {
                addedDepthModes |= DepthTextureMode.DepthNormals & ~viewCamera.depthTextureMode;
                viewCamera.depthTextureMode |= DepthTextureMode.DepthNormals;
            }
            else ReleaseDepthNormals();
        }

        void ReleaseDepthNormals()
        {
            if (viewCamera != null) viewCamera.depthTextureMode &= ~addedDepthModes;
            addedDepthModes = DepthTextureMode.None;
        }

        void OnDisable()
        {
            ReleaseDepthNormals();
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
            runtimeMaterial = null;
            sourceMaterial = null;
        }

        bool EnsureMaterial()
        {
            if (!IsSupported) return false;
            if (runtimeMaterial != null && sourceMaterial == material) return true;
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
            runtimeMaterial = new Material(material) { hideFlags = HideFlags.HideAndDontSave };
            sourceMaterial = material;
            return true;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!EnsureMaterial() || viewCamera == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            bool useAo = AOEnabled && aoIntensity > .001f && !viewCamera.orthographic &&
                SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf);
            float safeRadius = Mathf.Clamp(radius, .15f, 1.5f);
            Matrix4x4 projection = viewCamera.projectionMatrix;
            runtimeMaterial.SetVector(ViewParameters, new Vector4(
                2f / projection.m00, 2f / projection.m11, viewCamera.farClipPlane, 0f));
            runtimeMaterial.SetVector(AoParameters, new Vector4(safeRadius,
                Mathf.Clamp(aoIntensity, 0f, 1.5f), Mathf.Max(.018f, safeRadius * .035f), useAo ? 1f : 0f));
            runtimeMaterial.SetVector(ColorParameters, new Vector4(
                Mathf.Clamp(exposure, .25f, 2.5f), Mathf.Clamp(vignette, 0f, .15f), 0f, 0f));

            RenderTexture occlusion = null;
            RenderTexture scratch = null;
            bool oldSrgbWrite = GL.sRGBWrite;
            try
            {
                if (useAo)
                {
                    int width = Mathf.Max(1, (source.width + 1) / 2);
                    int height = Mathf.Max(1, (source.height + 1) / 2);
                    occlusion = RenderTexture.GetTemporary(width, height, 0,
                        RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                    scratch = RenderTexture.GetTemporary(width, height, 0,
                        RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                    occlusion.filterMode = scratch.filterMode = FilterMode.Bilinear;
                    occlusion.wrapMode = scratch.wrapMode = TextureWrapMode.Clamp;
                    GL.sRGBWrite = false;
                    Graphics.Blit(source, occlusion, runtimeMaterial, 0);
                    runtimeMaterial.SetVector(BlurDirection, new Vector4(1f / width, 0f, 0f, 0f));
                    Graphics.Blit(occlusion, scratch, runtimeMaterial, 1);
                    runtimeMaterial.SetVector(BlurDirection, new Vector4(0f, 1f / height, 0f, 0f));
                    Graphics.Blit(scratch, occlusion, runtimeMaterial, 1);
                    runtimeMaterial.SetTexture(OcclusionTexture, occlusion);
                }
                else runtimeMaterial.SetTexture(OcclusionTexture, Texture2D.whiteTexture);

                GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear &&
                    (destination == null || destination.sRGB);
                Graphics.Blit(source, destination, runtimeMaterial, 2);
            }
            finally
            {
                GL.sRGBWrite = oldSrgbWrite;
                runtimeMaterial.SetTexture(OcclusionTexture, Texture2D.whiteTexture);
                if (scratch != null) RenderTexture.ReleaseTemporary(scratch);
                if (occlusion != null) RenderTexture.ReleaseTemporary(occlusion);
            }
        }
    }
}

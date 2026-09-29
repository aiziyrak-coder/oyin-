using System;
using UnityEngine;

namespace CraDev.MainMenu
{
    /// <summary>
    /// 3D qahramon sahnasi: kamera va yorug'lik fon rasmiga mos holatlarda (Bosh sahifa - penthaus, Garderob - xona).
    /// Har bir holatni builder fon rasmidagi qahramon o'rniga qarab hisoblaydi. Qahramon doim (0,0,0) da turadi.
    /// </summary>
    public class LobbyStage : MonoBehaviour
    {
        [Serializable]
        public class Pose
        {
            public string name;
            public Vector3 position;
            public Vector3 rotation;
            [Tooltip("Rasm ekranni balandligi bo'yicha to'liq egallaganda vertikal ko'rish burchagi.")]
            public float fieldOfView = 30f;
            public float imageAspect = 16f / 9f;
            public Vector3 sunRotation;
            public Color sunColor = Color.white;
            public float sunIntensity = 1f;
            public Vector3 rimRotation;
            public Color rimColor = Color.white;
            public float rimIntensity = 0.5f;
            public Color ambientSky = Color.gray, ambientEquator = Color.gray, ambientGround = Color.black;
            [Tooltip("Qahramon ostidagi soya qanchalik to'q (0..1).")]
            public float shadowStrength = 0.5f;
        }

        [SerializeField] Camera stageCamera;
        [SerializeField] LobbyCamera lobbyCamera;
        [SerializeField] Transform turntable;
        [SerializeField] Light sun;
        [SerializeField] Light rim;
        [SerializeField] Renderer shadowCatcher;
        [SerializeField] Pose[] poses;

        public Camera Camera => stageCamera;
        public int Current { get; private set; } = -2;

        static readonly int StrengthId = Shader.PropertyToID("_Strength");

        // ------------------------------------------------------------------ Avatar studiyasi
        // Kamera qahramonga silliq yaqinlashadi (fon rasmi o'z joyida qoladi): 0 - to'liq bo'y, 1 - yuz.
        // Qahramon ekranning chap qismida turadi (o'ngda studiya paneli), kamera to'g'ri oldinga qaraydi.
        Transform focus;
        float focusHeight = 1.75f, focusScreenX = 0.38f;
        float studio, studioTarget, zoom, zoomTarget;

        /// <summary>Studiya holati yoqilganmi (kamera qahramonga yaqinlashgan yoki yaqinlashmoqda).</summary>
        public bool StudioActive => studioTarget > 0f;

        /// <summary>Studiyadagi yaqinlik: 0 - to'liq bo'y, 1 - yuz.</summary>
        public float StudioZoom
        {
            get => zoomTarget;
            set => zoomTarget = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Studiya kamerasini yoqadi/o'chiradi. target - qahramon turgan nuqta, height - uning bo'yi,
        /// screenX - qahramon ekranning qayerida turadi (kenglikka nisbatan, 0 - chap chet).
        /// </summary>
        public void SetStudio(bool on, Transform target = null, float height = 1.75f, float screenX = 0.38f)
        {
            studioTarget = on ? 1f : 0f;
            if (!on)
                return;
            focus = target;
            SetStudioFocus(height, screenX);
        }

        public void SetStudioFocus(float height, float screenX)
        {
            focusHeight = Mathf.Clamp(height, 1.2f, 2.3f);
            focusScreenX = Mathf.Clamp(screenX, 0.2f, 0.5f);
        }

        void LateUpdate()
        {
            if (studio <= 0f && studioTarget <= 0f)
                return;
            float dt = Time.unscaledDeltaTime;
            studio = Mathf.MoveTowards(studio, studioTarget, dt / 0.45f);
            zoom = Mathf.Abs(zoom - zoomTarget) < 0.001f ? zoomTarget : Mathf.Lerp(zoom, zoomTarget, 1f - Mathf.Exp(-7f * dt));
            if (stageCamera == null || poses == null || Current < 0 || Current >= poses.Length)
                return;
            var pose = poses[Current];
            var baseRotation = Quaternion.Euler(pose.rotation);
            if (studio <= 0f)
            {
                // Studiya yopildi: kamera aynan fon rasmiga mos holatga qaytadi
                stageCamera.transform.SetPositionAndRotation(pose.position, baseRotation);
                focus = null;
                zoom = zoomTarget = 0f;
                return;
            }

            Vector3 origin = focus != null ? focus.position : Vector3.zero;
            float z = Ease.InOutSine(zoom);
            float halfTan = Mathf.Tan(stageCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            // Kadr balandligi: to'liq bo'yda boshdan oyoqqacha (pastda boshqaruv tugmalari uchun joy), yaqinda - yuz
            float span = Mathf.Lerp(focusHeight * 1.36f, 0.56f, z);
            float center = Mathf.Lerp(focusHeight * 0.46f, focusHeight - 0.13f, z);
            float distance = span * 0.5f / halfTan;
            float side = (0.5f - focusScreenX) * 2f * distance * halfTan * stageCamera.aspect;
            var studioPosition = origin + new Vector3(side, center, -distance);

            float t = Ease.InOutCubic(studio);
            stageCamera.transform.SetPositionAndRotation(Vector3.Lerp(pose.position, studioPosition, t),
                Quaternion.Slerp(baseRotation, Quaternion.identity, t));
        }

        /// <summary>Holatni qo'llaydi; -1 - qahramon yashiriladi (sahifada 3D qahramon yo'q).</summary>
        public void Apply(int index)
        {
            Current = index;
            // Sahna builderi UI'ni alohida tekshirayotganda 3D qismlar hali ulanmagan bo'lishi mumkin.
            // Bunday holda sahifa ishlashda davom etadi, avatar esa ko'rsatilmaydi.
            if (turntable == null || stageCamera == null || lobbyCamera == null || poses == null)
                return;
            bool visible = index >= 0 && index < poses.Length;
            turntable.gameObject.SetActive(visible);
            stageCamera.enabled = visible;
            if (shadowCatcher != null)
                shadowCatcher.gameObject.SetActive(visible);
            if (!visible)
                return;
            var pose = poses[index];
            stageCamera.transform.SetPositionAndRotation(pose.position, Quaternion.Euler(pose.rotation));
            lobbyCamera.Configure(pose.fieldOfView, pose.imageAspect);
            sun.transform.rotation = Quaternion.Euler(pose.sunRotation);
            sun.color = pose.sunColor;
            sun.intensity = pose.sunIntensity;
            rim.transform.rotation = Quaternion.Euler(pose.rimRotation);
            rim.color = pose.rimColor;
            rim.intensity = pose.rimIntensity;
            RenderSettings.ambientSkyColor = pose.ambientSky;
            RenderSettings.ambientEquatorColor = pose.ambientEquator;
            RenderSettings.ambientGroundColor = pose.ambientGround;
            if (shadowCatcher != null)
                shadowCatcher.material.SetFloat(StrengthId, pose.shadowStrength);
        }
    }
}

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

        /// <summary>Holatni qo'llaydi; -1 - qahramon yashiriladi (sahifada 3D qahramon yo'q).</summary>
        public void Apply(int index)
        {
            Current = index;
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

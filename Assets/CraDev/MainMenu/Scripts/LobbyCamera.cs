using UnityEngine;

namespace CraDev.MainMenu
{
    /// <summary>
    /// Bosh menyu kamerasi: 3D qahramonni orqa fondagi 16:9 rasm bilan bir xil nuqtai nazardan ko'rsatadi.
    /// Rasm ekranni to'liq qoplaydi (ortiqchasi kesiladi). Ekran 16:9 dan kengroq bo'lsa, rasmning yuqori-pasti
    /// kesiladi: kamera ham xuddi shunday "kesilishi" uchun vertikal ko'rish burchagi kichraytiriladi.
    /// Torroq ekranda (16:10, 4:3) rasm yon tomonlardan kesiladi - vertikal burchak o'zgarmaydi.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class LobbyCamera : MonoBehaviour
    {
        [Tooltip("Rasm 16:9 bo'lganda kameraning vertikal ko'rish burchagi (builder rasmga moslab hisoblaydi).")]
        [SerializeField] float fieldOfView16x9 = 30f;
        const float ImageAspect = 16f / 9f;

        Camera cam;
        int width, height;

        void Awake() => cam = GetComponent<Camera>();

        void LateUpdate()
        {
            if (Screen.width == width && Screen.height == height)
                return;
            width = Screen.width;
            height = Screen.height;
            float aspect = (float)width / Mathf.Max(1, height);
            float fov = fieldOfView16x9;
            if (aspect > ImageAspect)
            {
                float half = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * ImageAspect / aspect;
                fov = 2f * Mathf.Atan(half) * Mathf.Rad2Deg;
            }
            cam.fieldOfView = fov;
        }
    }
}

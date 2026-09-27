using UnityEngine;

namespace CraDev.MainMenu
{
    /// <summary>
    /// Lobby kamerasi: 3D qahramonni orqa fondagi rasm bilan bir xil nuqtai nazardan ko'rsatadi. Rasm ekranni to'liq
    /// qoplaydi (ortiqchasi kesiladi). Ekran rasmdan kengroq bo'lsa, rasmning yuqori-pasti kesiladi: kamera ham xuddi
    /// shunday "kesilishi" uchun vertikal ko'rish burchagi kichraytiriladi. Torroq ekranda rasm yon tomonlardan kesiladi -
    /// vertikal burchak o'zgarmaydi. Sahifa almashganda (boshqa fon) <see cref="Configure"/> chaqiriladi.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class LobbyCamera : MonoBehaviour
    {
        [Tooltip("Rasm ekranni balandligi bo'yicha to'liq egallaganda kameraning vertikal ko'rish burchagi.")]
        [SerializeField] float baseFieldOfView = 30f;
        [Tooltip("Orqa fon rasmining eni/bo'yi nisbati.")]
        [SerializeField] float imageAspect = 16f / 9f;

        Camera cam;
        int width, height;

        void Awake() => cam = GetComponent<Camera>();

        public void Configure(float fieldOfView, float aspect)
        {
            baseFieldOfView = fieldOfView;
            imageAspect = aspect;
            width = height = 0; // keyingi kadrda qayta hisoblanadi
        }

        void LateUpdate()
        {
            if (Screen.width == width && Screen.height == height)
                return;
            width = Screen.width;
            height = Screen.height;
            float aspect = (float)width / Mathf.Max(1, height);
            float fov = baseFieldOfView;
            if (aspect > imageAspect)
            {
                float half = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * imageAspect / aspect;
                fov = 2f * Mathf.Atan(half) * Mathf.Rad2Deg;
            }
            cam.fieldOfView = fov;
        }
    }
}

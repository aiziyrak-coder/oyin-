using UnityEngine;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    /// <summary>
    /// Lobby UI masshtabi: 1080p va undan katta ekranlarda foydalanuvchi tanlagan ixcham dizayn (1920x1080/0.65)
    /// aynan saqlanadi. Kichik ekranlarda (1366x768, 1280x720) CanvasScaler UI'ni yana kichraytirib, yozuvlarni
    /// 8 px gacha tushirardi: bu yerda masshtab minimumScale dan pastga tushmaydi. Kanvas hech qachon 1920x1080
    /// birlikdan kichik bo'lmaydi, shuning uchun sozlamalar oynasi va chap/o'ng panellar doim sig'adi.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class LobbyUiScale : MonoBehaviour
    {
        [Tooltip("Kichik ekranlarda UI masshtabi bundan pastga tushmaydi (1080p dagi dizayn masshtabi 0.65).")]
        [SerializeField] float minimumScale = .65f;
        [Tooltip("Kanvasning eng kichik balandligi (birlik): 1920x1080 lik sozlamalar oynasi sig'ishi uchun.")]
        [SerializeField] float minimumHeight = 1080;
        CanvasScaler scaler;
        Vector2 design;
        int width, height;

        void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            design = scaler.referenceResolution;
            Apply();
        }

        void Update()
        {
            if (Screen.width != width || Screen.height != height) Apply();
        }

        void Apply()
        {
            width = Screen.width; height = Screen.height;
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize || design.y <= 0 || height <= 0) return;
            // Balandlik bo'yicha masshtab minimumScale dan kichik bo'lmasin, lekin dizayndan katta ham bo'lmasin
            float target = Mathf.Clamp(height / minimumScale, minimumHeight, design.y);
            scaler.referenceResolution = new Vector2(target * design.x / design.y, target);
        }
    }
}

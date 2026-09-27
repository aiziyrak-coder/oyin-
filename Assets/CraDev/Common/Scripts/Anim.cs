using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CraDev
{
    /// <summary>Animatsiyalar uchun silliqlash (easing) funksiyalari. Kirish va chiqish 0..1 oralig'ida.</summary>
    public static class Ease
    {
        public static float OutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);

        /// <summary>Tez boshlanib, juda yumshoq to'xtaydi. Zamonaviy UI harakatlari uchun.</summary>
        public static float OutQuint(float x) => 1f - Mathf.Pow(1f - x, 5f);

        public static float InOutCubic(float x) =>
            x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f;

        public static float InOutSine(float x) => -(Mathf.Cos(Mathf.PI * x) - 1f) / 2f;

        public static float OutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }

    public static class Anim
    {
        /// <summary>t vaqt [start, start + duration] oralig'ining qayerida ekanini 0..1 da qaytaradi.</summary>
        public static float Progress(float t, float start, float duration) =>
            duration <= 0f ? (t >= start ? 1f : 0f) : Mathf.Clamp01((t - start) / duration);

        public static void SetAlpha(Graphic graphic, float alpha)
        {
            Color c = graphic.color;
            c.a = Mathf.Clamp01(alpha);
            graphic.color = c;
        }

        /// <summary>
        /// Shu kadrda istalgan tugma, sichqoncha yoki geympad tugmasi bosildimi.
        /// Eski Input Manager'da ham, yangi Input System'da ham ishlaydi.
        /// </summary>
        public static bool AnyInputPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)) return true;
            if (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame)) return true;
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.anyKeyDown;
#else
            return false;
#endif
        }

        /// <summary>Shu kadrda Esc (yoki geympadda B / Start) bosildimi: orqaga, oynani yopish, menyu.</summary>
        public static bool BackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
            return Gamepad.current != null && (Gamepad.current.buttonEast.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        /// <summary>Shu kadrda Enter bosildimi (forma yuborish uchun).</summary>
        public static bool SubmitPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null &&
                   (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#else
            return false;
#endif
        }
    }
}

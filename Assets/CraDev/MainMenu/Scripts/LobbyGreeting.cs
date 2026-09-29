using UnityEngine;

namespace CraDev.MainMenu
{
    /// <summary>
    /// "Assalomu alaykum" salomi: o'ng qo'l ko'krakning chap tomoniga qo'yiladi, bosh biroz egiladi.
    /// Tayyor animatsiya yo'q, shuning uchun Rocketbox Bip01 skeletida protsedural: Animator idle pozasi ustidan
    /// LateUpdate'da ikki bo'g'imli IK (yelka - tirsak - kaft) va bosh/bo'yin egilishi aralashtiriladi.
    /// O'qlarga bog'liq emas (yo'nalishlar dunyo fazosida hisoblanadi): 9 ta avatarning hammasida ishlaydi.
    /// </summary>
    public sealed class LobbyGreeting : MonoBehaviour
    {
        public const float Duration = 6f;
        const float EaseIn = .5f, EaseOut = .6f;

        Transform upper, fore, hand, finger, head, neck, spine, leftUpper;
        float start = -100;
        bool found;

        public bool Playing => Time.time - start < Duration;

        public void Play()
        {
            if (!found) Find();
            start = Time.time;
        }

        void Find()
        {
            found = true;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                switch (t.name)
                {
                    case "Bip01 R UpperArm": upper = t; break;
                    case "Bip01 R Forearm": fore = t; break;
                    case "Bip01 R Hand": hand = t; break;
                    case "Bip01 L UpperArm": leftUpper = t; break;
                    case "Bip01 Head": head = t; break;
                    case "Bip01 Neck": neck = t; break;
                    case "Bip01 Spine2": spine = t; break;
                    case "Bip01 R Finger2": finger = t; break;
                    case "Bip01 R Finger1": if (finger == null) finger = t; break;
                }
            }
            if (spine == null)
                foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == "Bip01 Spine1") { spine = t; break; }
        }

        float Weight(float t)
        {
            if (t < 0 || t > Duration) return 0;
            if (t < EaseIn) return Ease.InOutSine(t / EaseIn);
            if (t > Duration - EaseOut) return Ease.InOutSine((Duration - t) / EaseOut);
            return 1;
        }

        void LateUpdate()
        {
            float t = Time.time - start;
            float w = Weight(t);
            if (w <= 0 || upper == null || fore == null || hand == null || leftUpper == null) return;

            // Tana o'qlari: chap yelka tomoni, yuqori, oldinga (modelning o'z o'qlaridan qat'i nazar)
            Vector3 left = leftUpper.position - upper.position;
            float shoulders = left.magnitude;
            if (shoulders < 1e-3f) return;
            left /= shoulders;
            Vector3 up = Vector3.up;
            Vector3 forward = Vector3.Cross(up, left).normalized;
            Vector3 right = -left;
            float unit = shoulders / .36f;

            // Egilish avval (qo'l nishoni egilgan tanaga nisbatan hisoblanadi)
            float bow = w * (5f + 9f * Bump(t, .5f, 2.4f));
            if (spine != null) spine.rotation = Quaternion.AngleAxis(bow * .35f, right) * spine.rotation;
            if (neck != null) neck.rotation = Quaternion.AngleAxis(bow * .45f, right) * neck.rotation;
            if (head != null) head.rotation = Quaternion.AngleAxis(bow * .6f, right) * head.rotation;

            // Nishon: ko'krakning chap-markazi, tanadan biroz oldinda
            Vector3 mid = (upper.position + leftUpper.position) * .5f;
            Vector3 target = mid - up * (.17f * unit) + left * (.07f * unit) + forward * (.12f * unit);

            Vector3 s = upper.position;
            float a = Vector3.Distance(upper.position, fore.position), b = Vector3.Distance(fore.position, hand.position);
            if (a < 1e-3f || b < 1e-3f) return;
            // Kaft ko'krakka tegishi uchun bilak nuqtasi kaftdan biroz tashqarida
            target += right * (.05f * unit);
            Vector3 toTarget = target - s;
            float d = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(a - b) + 1e-3f, a + b - 1e-3f);
            Vector3 dir = toTarget.normalized;
            // Tirsak pastga, tashqariga va biroz orqaga qaraydi
            Vector3 pole = right * .8f - up * 1f - forward * .15f;
            pole = (pole - dir * Vector3.Dot(pole, dir)).normalized;
            float cos = Mathf.Clamp((a * a + d * d - b * b) / (2 * a * d), -1, 1);
            Vector3 elbow = s + dir * (a * cos) + pole * (a * Mathf.Sqrt(1 - cos * cos));
            Vector3 wrist = s + dir * d;

            var upperGoal = Quaternion.FromToRotation(fore.position - upper.position, elbow - s) * upper.rotation;
            upper.rotation = Quaternion.Slerp(upper.rotation, upperGoal, w);
            var foreGoal = Quaternion.FromToRotation(hand.position - fore.position, wrist - fore.position) * fore.rotation;
            fore.rotation = Quaternion.Slerp(fore.rotation, foreGoal, w);
            // Kaft ko'krak bo'ylab chapga yotadi (bilak davomida, biroz ichkariga bukilgan)
            var handDir = finger != null ? finger.position - hand.position : Vector3.zero;
            Vector3 flat = (left * .9f - up * .15f - forward * .2f).normalized;
            if (handDir.sqrMagnitude > 1e-6f)
            {
                var handGoal = Quaternion.FromToRotation(handDir, flat) * hand.rotation;
                hand.rotation = Quaternion.Slerp(hand.rotation, handGoal, w * .8f);
            }
        }

        // 0..1..0 bitta "bosh irg'ash" to'lqini
        static float Bump(float t, float from, float to)
        {
            if (t <= from || t >= to) return 0;
            return Mathf.Sin(Mathf.PI * (t - from) / (to - from));
        }
    }
}

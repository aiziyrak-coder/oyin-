using System;
using System.Collections;
using UnityEngine;

namespace CraDev.Online
{
    /// <summary>
    /// "Men o'yindaman" signali: profili bor o'yinchi o'yinda ekan, serverga har 30 soniyada xabar beradi (do'stlar
    /// uni onlayn ko'radi, reyting uchun onlayn vaqt hisoblanadi). Javobda hozir onlayn o'yinchilar soni keladi.
    /// Sahnalar almashganda ham ishlayveradi (bitta nusxa).
    /// </summary>
    public class Presence : MonoBehaviour
    {
        const float Interval = 30f;
        const float MaxRejectedInterval = 300f;

        static Presence instance;

        /// <summary>Hozir onlayn o'yinchilar (-1: hali ma'lum emas yoki server bilan aloqa yo'q).</summary>
        public static int Online { get; private set; } = -1;

        /// <summary>Onlayn soni yangilanganda.</summary>
        public static event Action Changed;

        string serverUrl;

        public static void Begin(string serverUrl)
        {
            if (instance != null || !PlayerProfile.Exists)
                return;
            var go = new GameObject("Presence");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<Presence>();
            instance.serverUrl = serverUrl;
        }

        IEnumerator Start()
        {
            var api = new GameApi(serverUrl);
            int rejected = 0; // ketma-ket 401 javoblari
            while (true)
            {
                if (PlayerProfile.Exists)
                    yield return api.Presence(PlayerProfile.Token, result =>
                    {
                        Online = result.Ok ? result.Data.online : -1;
                        rejected = !result.NetworkError && result.Status == 401 ? rejected + 1 : 0;
                        Changed?.Invoke();
                    });
                // Server profilni tanimasa (bosh menyu uni tiklaguncha yoki o'yinchi yangi qahramon yaratguncha) signal
                // kamroq yuboriladi: birinchi rad etishdan keyin odatdagidek, keyin 1, 2, 4, 5 daqiqa
                float wait = rejected <= 1 ? Interval : Mathf.Min(Interval * (1 << Mathf.Min(rejected - 1, 4)), MaxRejectedInterval);
                yield return new WaitForSecondsRealtime(wait);
            }
        }
    }
}

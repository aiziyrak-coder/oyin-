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
            while (true)
            {
                if (PlayerProfile.Exists)
                    yield return api.Presence(PlayerProfile.Token, result =>
                    {
                        Online = result.Ok ? result.Data.online : -1;
                        Changed?.Invoke();
                    });
                yield return new WaitForSecondsRealtime(Interval);
            }
        }
    }
}

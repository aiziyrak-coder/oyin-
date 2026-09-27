using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CraDev
{
    /// <summary>
    /// Ishlab chiquvchi uchun: o'yin o'z ekranini faylga saqlaydi (oddiy ishga tushirishda o'chiq).
    /// <c>CraDev.exe -cradevShot D:\shot.png [-cradevScene MainMenu] [-cradevDelay 3] [-cradevPress Customize] [-cradevQuit]</c>
    /// Ko'rsatilgan sahna ochilgach, delay soniyadan keyin skrinshot olinadi. -cradevPress berilsa, shu nomli tugma
    /// bosiladi va keyingi sahnaning skrinshoti ham olinadi (<c>shot_2.png</c>). Sahna almashishlari Player.log ga
    /// yoziladi. -cradevQuit bo'lsa oxirida o'yin yopiladi.
    /// </summary>
    public class DevCapture : MonoBehaviour
    {
        string path;
        string scene;
        string press;
        float delay = 3f;
        bool quit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            var args = System.Environment.GetCommandLineArgs();
            string shot = Argument(args, "-cradevShot");
            if (string.IsNullOrEmpty(shot))
                return;
            var go = new GameObject("DevCapture");
            DontDestroyOnLoad(go);
            var capture = go.AddComponent<DevCapture>();
            capture.path = shot;
            capture.scene = Argument(args, "-cradevScene") ?? "MainMenu";
            capture.press = Argument(args, "-cradevPress");
            if (float.TryParse(Argument(args, "-cradevDelay"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float d))
                capture.delay = d;
            capture.quit = System.Array.IndexOf(args, "-cradevQuit") >= 0;
            SceneManager.activeSceneChanged += (_, next) => Debug.Log($"[CraDev] Sahna: {next.name} ({Time.realtimeSinceStartup:0.00} s)");
        }

        IEnumerator Start()
        {
            while (SceneManager.GetActiveScene().name != scene)
                yield return null;
            yield return Shot(path);

            if (!string.IsNullOrEmpty(press))
            {
                var button = FindButton(press);
                if (button == null)
                    Debug.LogWarning("[CraDev] DevCapture: tugma topilmadi: " + press);
                else
                {
                    Debug.Log("[CraDev] DevCapture: bosildi: " + press);
                    button.onClick.Invoke();
                    while (SceneManager.GetActiveScene().name == scene)
                        yield return null;
                    string second = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path) ?? "",
                        System.IO.Path.GetFileNameWithoutExtension(path) + "_2" + System.IO.Path.GetExtension(path));
                    yield return Shot(second);
                }
            }
            if (quit)
                Application.Quit();
        }

        IEnumerator Shot(string file)
        {
            yield return new WaitForSecondsRealtime(delay);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(file);
            Debug.Log("[CraDev] Skrinshot: " + file);
            yield return new WaitForSecondsRealtime(1f);
        }

        static Button FindButton(string name)
        {
            foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
                if (button.name == name && button.isActiveAndEnabled)
                    return button;
            return null;
        }

        static string Argument(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name)
                    return args[i + 1];
            return null;
        }
    }
}

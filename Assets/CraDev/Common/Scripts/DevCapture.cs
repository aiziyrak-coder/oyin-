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
            float deadline = Time.realtimeSinceStartup + 90;
            while (SceneManager.GetActiveScene().name != scene && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (SceneManager.GetActiveScene().name != scene)
            {
                Debug.LogError("[CraDev] Capture: kutilgan sahna ochilmadi: " + scene);
                if (quit) Application.Quit(1);
                yield break;
            }
            string page = Argument(System.Environment.GetCommandLineArgs(), "-cradevPage");
            var lobby = FindFirstObjectByType<MainMenu.MainMenuScreen>();
            if (lobby != null && page != null)
            {
                yield return null;
                lobby.Show(page);
            }
            yield return Shot(path);

            if (lobby != null && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-cradevAllPages") >= 0)
                foreach (string id in new[] { "world", "wardrobe", "shops", "education", "business", "friends", "settings", "top", "entertainment" })
                {
                    lobby.Show(id);
                    string file = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path) ?? "", id + ".png");
                    yield return Shot(file);
                    if (lobby.Current == null || lobby.Current.Id != id)
                        Debug.LogError("[CraDev] Capture: sahifa ochilmadi: " + id);
                    else Debug.Log("[CraDev] PAGE OK: " + id);
                }

            if(lobby!=null && System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-cradevUiSmoke")>=0)
                yield return DevLobbySmoke.Run(lobby);

            if(lobby!=null && System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-cradevGlassDetails")>=0)
            {
                string folder=System.IO.Path.GetDirectoryName(path)??"";
                lobby.Show("settings");yield return new WaitForSecondsRealtime(.5f);
                ((MainMenu.LobbyContent)lobby.Current).Choose("2");
                yield return Shot(System.IO.Path.Combine(folder,"settings-switches.png"));
                lobby.Dialog.Show(Loc.T("wardrobe.unsaved_title"),Loc.T("wardrobe.unsaved_message"),Loc.T("wardrobe.discard"),()=>{},Loc.T("common.cancel"));
                yield return Shot(System.IO.Path.Combine(folder,"dialog.png"));
                lobby.Dialog.Close();lobby.Show("home");
                yield return new WaitForSecondsRealtime(.6f);
                float elapsed=0;const int frames=120;
                for(int i=0;i<frames;i++){yield return null;elapsed+=Time.unscaledDeltaTime;}
                Debug.Log($"[CraDev] Glass steady-frame sample: {frames/Mathf.Max(.001f,elapsed):0.0} FPS, {elapsed/frames*1000:0.00} ms; {Screen.width}x{Screen.height}.");
            }

            if (!string.IsNullOrEmpty(press))
            {
                var button = FindButton(press);
                if (button == null)
                    Debug.LogWarning("[CraDev] DevCapture: tugma topilmadi: " + press);
                else
                {
                    Debug.Log("[CraDev] DevCapture: bosildi: " + press);
                    button.onClick.Invoke();
                    float until = Time.realtimeSinceStartup + 3;
                    while (SceneManager.GetActiveScene().name == scene && Time.realtimeSinceStartup < until)
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
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            var pixels = screenshot.GetPixels32();
            int lo=255, hi=0;
            for(int i=0;i<pixels.Length;i+=997)
            {
                int light=(pixels[i].r+pixels[i].g+pixels[i].b)/3;
                lo=Mathf.Min(lo,light);hi=Mathf.Max(hi,light);
            }
            if(hi-lo<15) Debug.LogError("[CraDev] Capture: ekran bir xil rang yoki qora: "+file);
            System.IO.File.WriteAllBytes(file,screenshot.EncodeToPNG());
            Destroy(screenshot);
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

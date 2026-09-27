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
            bool worldSmoke=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-cradevWorldSmoke")>=0;
            bool enterWorld=worldSmoke||System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-cradevEnterWorld")>=0;
            if(enterWorld)
            {
                float enterDeadline=Time.realtimeSinceStartup+90;
                while(SceneManager.GetActiveScene().name!="MainMenu"&&Time.realtimeSinceStartup<enterDeadline)yield return null;
                var menu=FindFirstObjectByType<MainMenu.MainMenuScreen>();
                if(menu!=null){yield return new WaitForSecondsRealtime(1);menu.EnterWorld();}
                scene="WorldSandbox";
            }
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
            if(worldSmoke)
            {
                yield return DevWorldSmoke.Run();
                if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-cradevWorldVisuals")>=0)
                    yield return WorldVisuals();
                else yield return Shot(path);
                var hud=FindFirstObjectByType<World.WorldHud>();hud.SetSettings(true);
                yield return Shot(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path),"world-settings.png"));
                hud.SetSettings(false);
                hud.ReturnToLobby();
                float returnDeadline=Time.realtimeSinceStartup+30;
                while(SceneManager.GetActiveScene().name!="MainMenu"&&Time.realtimeSinceStartup<returnDeadline)yield return null;
                bool returned=SceneManager.GetActiveScene().name=="MainMenu";
                Debug.Log("[WorldTest] "+(returned?"PASS":"FAIL")+": return to lobby through loading");
                if(returned)
                {
                    yield return new WaitForSecondsRealtime(.6f);
                    yield return Shot(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path),"returned-lobby.png"));
                }
                if(quit)Application.Quit(DevWorldSmoke.Failures>0||!returned?1:0);
                yield break;
            }
            var lobby = FindFirstObjectByType<MainMenu.MainMenuScreen>();
            if(lobby!=null && System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-cradevPartySmoke")>=0)
                yield return DevPartySmoke.Run(lobby,System.IO.Path.GetDirectoryName(path));
            if(lobby!=null && System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-cradevEnvironmentTest")>=0)
            {
                yield return new WaitForSecondsRealtime(2);
                var environment=lobby.GetComponent<MainMenu.LobbyEnvironment>();
                int failures=0;
                for(int minute=0;minute<1440;minute++)
                {
                    int expected=minute<300?5:minute<540?0:minute<720?1:minute<1020?2:minute<1200?3:4;
                    if(MainMenu.LobbyEnvironment.Period(minute/60)!=expected)failures++;
                    for(int weather=1;weather<=3;weather++)if(MainMenu.LobbyEnvironment.Resolve(minute/60,weather)!=weather+5)failures++;
                }
                bool motion=MainMenu.LobbyEnvironment.Motion;
                int savedWeather=MainMenu.LobbyEnvironment.Weather;
                string folder=System.IO.Path.GetDirectoryName(path)??"";
                try
                {
                    for(int i=0;i<9;i++)
                    {
                        environment.Preview(i);
                        float until=Time.realtimeSinceStartup+20;
                        while((environment.Current!=i||environment.Loading)&&Time.realtimeSinceStartup<until)yield return null;
                        if(environment.Current!=i||environment.Loading){failures++;Debug.LogError("[EnvironmentTest] Loading failed "+i);}
                        yield return Shot(System.IO.Path.Combine(folder,MainMenu.LobbyEnvironment.Names[i]+".png"));
                    }
                    environment.Preview(1);
                    float motionDeadline=Time.realtimeSinceStartup+20;
                    while((environment.Current!=1||environment.Loading)&&Time.realtimeSinceStartup<motionDeadline)yield return null;
                    MainMenu.LobbyEnvironment.Motion=false;yield return new WaitForSecondsRealtime(.7f);
                    if(environment.MotionActive)failures++;
                    yield return new WaitForEndOfFrame();var stillA=SkyPixels();
                    yield return new WaitForSecondsRealtime(1);yield return new WaitForEndOfFrame();var stillB=SkyPixels();
                    int stillDiff=PixelDifference(stillA,stillB);if(stillDiff!=0)failures++;
                    MainMenu.LobbyEnvironment.Motion=true;yield return new WaitForSecondsRealtime(.7f);
                    if(Application.isFocused&&!environment.MotionActive)failures++;
                    yield return new WaitForEndOfFrame();var movingA=SkyPixels();
                    yield return new WaitForSecondsRealtime(2);yield return new WaitForEndOfFrame();var movingB=SkyPixels();
                    int movingDiff=PixelDifference(movingA,movingB);if(Application.isFocused&&movingDiff==0)failures++;
                    Debug.Log($"[EnvironmentTest] Sky pixel differences: motion off={stillDiff}, motion on={movingDiff}, focused={Application.isFocused}");
                    lobby.Show("settings");lobby.ChooseSection("3");
                    for(int n=0;n<4;n++)
                    {
                        var selector=FindButton("LobbyWeatherSelector");
                        if(selector==null){failures++;break;}
                        selector.onClick.Invoke();yield return null;
                        if(MainMenu.LobbyEnvironment.Weather!=(savedWeather+n+1)%4)failures++;
                    }
                    yield return Shot(System.IO.Path.Combine(folder,"weather-settings.png"));lobby.SetSettings(false);
                }
                finally {MainMenu.LobbyEnvironment.Motion=motion;MainMenu.LobbyEnvironment.Weather=savedWeather;environment.Preview(-1);}
                yield return new WaitForSecondsRealtime(3.5f);
                if(MainMenu.LobbyEnvironment.Weather!=savedWeather)failures++;
                yield return Shot(path);
                float elapsed=0;for(int i=0;i<120;i++){yield return null;elapsed+=Time.unscaledDeltaTime;}
                Debug.Log($"[EnvironmentTest] COMPLETE: 1440 clock minutes, 4320 weather resolutions, 9 loaded captures, motion/settings; {failures} failures; {120/elapsed:0.0} FPS.");
                if(quit)Application.Quit();yield break;
            }
            if(lobby!=null && System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-cradevSingleLobbySmoke")>=0)
            {
                yield return new WaitForSecondsRealtime(2);
                yield return DevSingleLobbySmoke.Run(lobby);
                bool referencePreview=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-cradevReferencePreview")>=0;
                var environment=lobby.GetComponent<MainMenu.LobbyEnvironment>();
                if(referencePreview&&environment!=null)
                {
                    environment.Preview(3);float until=Time.realtimeSinceStartup+15;
                    while((environment.Current!=3||environment.Loading)&&Time.realtimeSinceStartup<until)yield return null;
                    lobby.SetEnvironmentBackground(lobby.Current.Background);
                }
                yield return new WaitForSecondsRealtime(3.5f);
                yield return Shot(path);
                string folder=System.IO.Path.GetDirectoryName(path)??"";
                lobby.Show("settings");lobby.ChooseSection("0");yield return Shot(System.IO.Path.Combine(folder,"settings.png"));
                lobby.ChooseSection("2");yield return Shot(System.IO.Path.Combine(folder,"notifications.png"));
                lobby.SetSettings(false);
                lobby.FriendsPanel.Choose("find");yield return Shot(System.IO.Path.Combine(folder,"friends-search.png"));
                lobby.FriendsPanel.Choose("find");
                yield return Shot(System.IO.Path.Combine(folder,"home-final.png"));
                var drawer=lobby.Current.GetComponent<MainMenu.LobbyFriendsDrawer>();
                if(drawer!=null){drawer.SetCollapsed(true);yield return Shot(System.IO.Path.Combine(folder,"friends-collapsed.png"));drawer.SetCollapsed(false);yield return new WaitForSecondsRealtime(.3f);}
                if(referencePreview&&environment!=null){environment.Preview(-1);yield return new WaitForSecondsRealtime(3.5f);yield return Shot(System.IO.Path.Combine(folder,"live-clock.png"));}
                Debug.Log("[SingleLobbyTest] CAPTURES COMPLETE; lobby left open");
                if(quit)Application.Quit();
                yield break;
            }
            if(lobby!=null && System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-cradevTransitionTest")>=0)
            {
                yield return new WaitForSecondsRealtime(2);
                for(int pass=0;pass<2;pass++)
                    foreach(string id in new[]{"world","wardrobe","shops","education","business","friends","settings","top","entertainment","home"})
                    {
                        var timer=System.Diagnostics.Stopwatch.StartNew();
                        lobby.Show(id);
                        double worst=0;
                        while(timer.Elapsed.TotalSeconds<1.2)
                        {
                            double before=timer.Elapsed.TotalMilliseconds;
                            yield return null;
                            worst=System.Math.Max(worst,timer.Elapsed.TotalMilliseconds-before);
                        }
                        Debug.Log($"[TransitionTest] pass={pass+1} page={id} worst={worst:0.00} ms");
                    }
            }
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
            if(lobby!=null && System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-cradevAvatarPreviews")>=0)
                yield return DevLobbySmoke.PreviewRegression(lobby);

            if(lobby!=null && (System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-cradevUiDetails")>=0 || System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-cradevGlassDetails")>=0))
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
                Debug.Log($"[CraDev] UI steady-frame sample: {frames/Mathf.Max(.001f,elapsed):0.0} FPS, {elapsed/frames*1000:0.00} ms; {Screen.width}x{Screen.height}.");
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

        IEnumerator WorldVisuals()
        {
            var player=FindFirstObjectByType<World.WorldPlayerController>();
            if(player==null)yield break;
            var camera=player.ViewCamera;var effects=camera.GetComponent<World.WorldImageEffects>();
            var position=player.transform.position;float yaw=player.Yaw;
            bool test=player.TestMode,paused=player.Paused,ao=effects.AOEnabled,enabled=effects.enabled;
            int msaa=QualitySettings.antiAliasing;
            string folder=System.IO.Path.GetDirectoryName(path);
            player.TestMode=true;player.SetPaused(false);
            try
            {
                player.Teleport(new Vector3(0,.06f,0),0);camera.transform.localRotation=Quaternion.Euler(5,0,0);
                yield return Shot(path);
                player.Teleport(new Vector3(-25,.06f,24),308);camera.transform.localRotation=Quaternion.Euler(8,0,0);
                yield return Shot(System.IO.Path.Combine(folder,"world-nature.png"));
                player.Teleport(new Vector3(0,.06f,0),270);camera.transform.localRotation=Quaternion.Euler(-18,0,0);
                yield return Shot(System.IO.Path.Combine(folder,"world-panorama-seam.png"));
                player.Teleport(new Vector3(0,.06f,9),0);camera.transform.localRotation=Quaternion.Euler(22,0,0);
                effects.AOEnabled=true;yield return Shot(System.IO.Path.Combine(folder,"world-contact-ao.png"));
                effects.AOEnabled=false;yield return Shot(System.IO.Path.Combine(folder,"world-contact-no-ao.png"));
                effects.AOEnabled=true;QualitySettings.antiAliasing=0;
                yield return Shot(System.IO.Path.Combine(folder,"world-msaa-off.png"));
                QualitySettings.antiAliasing=msaa;effects.enabled=false;
                yield return Shot(System.IO.Path.Combine(folder,"world-no-postfx.png"));
            }
            finally
            {
                QualitySettings.antiAliasing=msaa;effects.AOEnabled=ao;effects.enabled=enabled;
                player.Teleport(position,yaw);player.TestMode=test;player.SetPaused(paused);
            }
        }

        IEnumerator Shot(string file)
        {
            var directory=System.IO.Path.GetDirectoryName(file);
            if(!string.IsNullOrEmpty(directory))System.IO.Directory.CreateDirectory(directory);
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

        static Color32[] SkyPixels()
        {
            // Test-only GPU readback, never enabled during ordinary gameplay.
            var texture=ScreenCapture.CaptureScreenshotAsTexture();var all=texture.GetPixels32();
            var sample=new Color32[1600];int k=0;
            for(int y=0;y<40;y++)for(int x=0;x<40;x++)sample[k++]=all[(int)(texture.height*(.72f+y*.001f))*texture.width+(int)(texture.width*(.55f+x*.001f))];
            Destroy(texture);return sample;
        }
        static int PixelDifference(Color32[] a,Color32[] b)
        {
            int count=0;for(int i=0;i<a.Length;i++)if(Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>2)count++;return count;
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

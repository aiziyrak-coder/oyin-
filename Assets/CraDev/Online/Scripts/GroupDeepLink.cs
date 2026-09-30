using System;
using System.Collections;
using System.Text.RegularExpressions;
using CraDev.MainMenu;
using UnityEngine;
namespace CraDev.Online
{
    public sealed class GroupDeepLink : MonoBehaviour
    {
        string pending;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var host=new GameObject("GroupDeepLink");DontDestroyOnLoad(host);host.AddComponent<GroupDeepLink>();
        }
        void Awake()
        {
            Application.deepLinkActivated+=Receive;
            Receive(Application.absoluteURL);
            foreach(var arg in Environment.GetCommandLineArgs())if(arg.StartsWith("newworld://",StringComparison.OrdinalIgnoreCase))Receive(arg);
        }
        public static string Parse(string url)
        {
            if(string.IsNullOrEmpty(url)||url.Length>128)return null;
            var match=Regex.Match(url,@"\Anewworld://group/([ABCDEFGHJKMNPQRSTUVWXYZ23456789]{6})/?\z",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
            return match.Success?match.Groups[1].Value.ToUpperInvariant():null;
        }
        void Receive(string url){var code=Parse(url);if(code!=null)pending=code;}
        IEnumerator Start()
        {
            while(true)
            {
                if(pending!=null)
                {
                    var lobby=FindFirstObjectByType<MainMenuScreen>();
                    if(lobby!=null&&lobby.FriendsPanel!=null&&!ModalWindow.AnyOpen&&!lobby.SettingsOpen&&!lobby.AvatarStudioOpen)
                    {
                        var groups=lobby.FriendsPanel.GetComponent<LobbyGroupsPanel>();
                        if(groups!=null){string code=pending;pending=null;groups.OpenLink(code);}
                    }
                }
                yield return new WaitForSecondsRealtime(.5f);
            }
        }
        void OnDestroy(){Application.deepLinkActivated-=Receive;}
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace CraDev.Online
{
    /// <summary>
    /// O'yin serveri manzili. Sahnadagi (builder yozgan) manzilni o'yinni qayta yig'masdan almashtirish mumkin. Tartib:
    /// 1) buyruq qatori: <c>-server &lt;url&gt;</c>, <c>-server=&lt;url&gt;</c> yoki <c>-cradevServer &lt;url&gt;</c>;
    /// 2) <c>CRADEV_SERVER</c> muhit o'zgaruvchisi;
    /// 3) o'yin (CraDev.exe) yonidagi <c>server.txt</c>: birinchi to'ldirilgan qator, "#" bilan boshlangani izoh;
    /// 4) sahnadagi manzil (standart http://localhost:8080 - o'yin yonidagi server).
    /// Sxema yozilmasa http:// qo'shiladi ("192.168.1.5:8080"). Noto'g'ri qiymat Player.log'ga yoziladi va keyingi
    /// manba olinadi. <see cref="GameApi"/> har doim shu manzilni ishlatadi.
    /// </summary>
    public static class ServerAddress
    {
        public const string EnvVariable = "CRADEV_SERVER";
        public const string FileName = "server.txt";

        static readonly string[] Flags = { "-server", "--server", "-cradevServer", "--cradevServer" };

        static bool loaded;
        static string overrideUrl, overrideSource, logged;

        /// <summary>Oxirgi ishlatilgan manzil (bo'sh: hali GameApi yaratilmagan).</summary>
        public static string Current { get; private set; } = "";

        /// <summary>Manzil sahnadagidan boshqa manbadan (buyruq qatori, muhit, server.txt) olinganmi.</summary>
        public static bool IsOverridden
        {
            get
            {
                Load();
                return overrideUrl != null;
            }
        }

        /// <summary>Ishlatiladigan manzil: tashqi sozlama bo'lsa u, aks holda <paramref name="configured"/>.</summary>
        public static string Resolve(string configured)
        {
            Load();
            string url = overrideUrl, source = overrideSource;
            if (url == null)
            {
                url = Normalize(configured);
                source = "sahna";
            }
            if (url == null)
            {
                url = Normalize(GameApi.DefaultServerUrl);
                source = "standart";
            }
            Current = url;
            if (logged != url)
            {
                logged = url;
                Debug.Log($"[CraDev] O'yin serveri: {url} ({source})");
                if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !IsLocalNetwork(url))
                    Debug.LogWarning("[CraDev] Server internetda, lekin manzil http: o'yinchi tokeni shifrlanmasdan yuboriladi. HTTPS ishlating.");
            }
            return url;
        }

        /// <summary>
        /// "host:port", "http://host:port/" kabi yozuvni "http://host:port" ko'rinishiga keltiradi. Yaroqsiz bo'lsa
        /// (http/https emas, xost yo'q, so'rov qismi yoki login bor) null.
        /// </summary>
        public static string Normalize(string raw)
        {
            string text = (raw ?? "").Trim().Trim('"', '\'').Trim();
            if (text.Length == 0)
                return null;
            if (text.IndexOf("://", StringComparison.Ordinal) < 0)
                text = "http://" + text;
            if (!Uri.TryCreate(text, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                || string.IsNullOrEmpty(uri.Host) || uri.Host.StartsWith("-")
                || uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.UserInfo.Length > 0)
                return null;
            return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        }

        /// <summary>Manzil shu kompyuter yoki mahalliy tarmoq (uy, klub) ichidami: u yerda http ham maqbul.</summary>
        public static bool IsLocalNetwork(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;
            string host = uri.DnsSafeHost.ToLowerInvariant();
            if (host == "localhost" || host.EndsWith(".localhost") || host.EndsWith(".local") || host.EndsWith(".lan"))
                return true;
            if (!IPAddress.TryParse(host, out var ip))
                return false;
            if (IPAddress.IsLoopback(ip))
                return true;
            byte[] b = ip.GetAddressBytes();
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168)
                    || (b[0] == 169 && b[1] == 254) || (b[0] == 100 && b[1] >= 64 && b[1] <= 127);
            return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || (b[0] & 0xFE) == 0xFC;
        }

        static void Load()
        {
            if (loaded)
                return;
            loaded = true;
            foreach (var (value, source) in Sources())
            {
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                string url = Normalize(value);
                if (url == null)
                {
                    Debug.LogWarning($"[CraDev] Server manzili noto'g'ri ({source}): \"{value.Trim()}\" - e'tiborsiz qoldirildi");
                    continue;
                }
                overrideUrl = url;
                overrideSource = source;
                return;
            }
        }

        /// <summary>Tashqi manbalar navbat bilan (keyingisi faqat oldingisi bo'sh yoki yaroqsiz bo'lsa o'qiladi).</summary>
        static IEnumerable<(string value, string source)> Sources()
        {
            yield return (FromCommandLine(out string flag), "buyruq qatori " + flag);
            string env = null;
            try { env = Environment.GetEnvironmentVariable(EnvVariable); }
            catch (Exception) { }
            yield return (env, EnvVariable);
            string file = FromFile(out string path);
            yield return (file, path);
        }

        static string FromCommandLine(out string flag)
        {
            flag = "";
            string[] args;
            try { args = Environment.GetCommandLineArgs(); }
            catch (Exception) { return null; }
            for (int i = 0; i < args.Length; i++)
            {
                foreach (var name in Flags)
                {
                    flag = name;
                    // "-server" dan keyin manzil o'rniga boshqa bayroq kelsa - manzil berilmagan
                    if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                        return i + 1 < args.Length && !args[i + 1].StartsWith("-") ? args[i + 1] : null;
                    if (args[i].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
                        return args[i].Substring(name.Length + 1);
                }
            }
            return null;
        }

        static string FromFile(out string path)
        {
            path = FileName;
            foreach (var folder in FileFolders())
            {
                string file = Path.Combine(folder, FileName);
                try
                {
                    if (!File.Exists(file))
                        continue;
                    path = file;
                    foreach (var line in File.ReadAllLines(file))
                    {
                        string text = line.Trim();
                        if (text.Length > 0 && !text.StartsWith("#"))
                            return text;
                    }
                    return null;
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    Debug.LogWarning($"[CraDev] {file} o'qilmadi: {e.Message}");
                }
            }
            return null;
        }

        /// <summary>server.txt qidiriladigan papkalar: o'yin papkasi (tahrirlovchida - loyiha papkasi).</summary>
        static List<string> FileFolders()
        {
            var folders = new List<string>();
            try
            {
                // Windows/Linux: <o'yin>/CraDev_Data; macOS: CraDev.app/Contents
                string game = Path.GetDirectoryName(Application.dataPath);
                if (!string.IsNullOrEmpty(game))
                {
                    folders.Add(game);
                    if (Application.platform == RuntimePlatform.OSXPlayer && Path.GetDirectoryName(game) is string outer)
                        folders.Add(outer);
                }
            }
            catch (Exception) { }
            return folders;
        }
    }
}

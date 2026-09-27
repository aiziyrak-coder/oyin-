using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CraDev.EditorTools
{
    /// <summary>
    /// Buyruq qatori (batchmode) uchun kirish nuqtalari: Unity oynasini ochmasdan sahnalarni yaratish va o'yinni yig'ish.
    /// Windows'da eng osoni: tools/unity.ps1 skripti. To'g'ridan-to'g'ri:
    /// <c>Unity.exe -batchmode -quit -projectPath . -executeMethod CraDev.EditorTools.CraDevBatch.CreateScenes -logFile Logs/batch.log</c>
    /// Muvaffaqiyatda chiqish kodi 0, xatoda 1.
    /// </summary>
    public static class CraDevBatch
    {
        public static void BuildLobby()
        {
            try
            {
                CraDevSceneBuilder.RebuildLobby();
                var target = ResolveTarget();
                var path = Argument("-customBuildPath") ?? "Builds/LobbyV2/CraDev.exe";
                var report = CraDevSceneBuilder.BuildGame(target, path, run: false);
                Debug.Log("[CraDev] Lobby V2 build: " + report.summary.result);
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
            }
            catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        /// <summary>Barcha sahnalarni noldan yaratadi (menyudagi "CraDev → Sahnalarni yaratish" bilan bir xil).</summary>
        public static void CreateScenes()
        {
            try
            {
                CraDevSceneBuilder.CreateScenes();
                Debug.Log("[CraDev] BATCH OK: sahnalar yaratildi.");
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[CraDev] BATCH XATO: " + e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Sahnalarni yaratadi va o'yinni yig'adi (ishga tushirmaydi). Standart: Builds/&lt;tizim&gt;/.
        /// GitHub Actions (GameCI) uzatadigan <c>-customBuildTarget</c> va <c>-customBuildPath</c> ham qo'llanadi.
        /// </summary>
        public static void BuildGame()
        {
            try
            {
                CraDevSceneBuilder.CreateScenes();
                var target = ResolveTarget();
                var path = Argument("-customBuildPath") ?? CraDevSceneBuilder.DefaultBuildPath(target);
                Debug.Log($"[CraDev] Build: {target} -> {path}");

                var report = CraDevSceneBuilder.BuildGame(target, path, run: false);
                bool ok = report.summary.result == BuildResult.Succeeded;
                Debug.Log(ok
                    ? $"[CraDev] BATCH OK: o'yin yig'ildi: {report.summary.outputPath} ({report.summary.totalSize / (1024 * 1024)} MB)"
                    : $"[CraDev] BATCH XATO: o'yinni yig'ib bo'lmadi ({report.summary.result}, {report.summary.totalErrors} ta xato).");
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[CraDev] BATCH XATO: " + e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Faqat garderob kiyim niqoblarini hisoblaydi (tekshiruv rasmlari Logs/Outfit/ da).</summary>
        public static void BakeOutfits()
        {
            try
            {
                CraDevSceneBuilder.BakeOutfitsOnly();
                Debug.Log("[CraDev] BATCH OK: kiyim niqoblari tayyor.");
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[CraDev] BATCH XATO: " + e);
                EditorApplication.Exit(1);
            }
        }

        static BuildTarget ResolveTarget()
        {
            var name = Argument("-customBuildTarget");
            if (name != null)
                return (BuildTarget)System.Enum.Parse(typeof(BuildTarget), name);

            // "-buildTarget StandaloneLinux64" bilan ochilgan bo'lsa, o'sha tizim faol bo'ladi
            var active = EditorUserBuildSettings.activeBuildTarget;
            if (active == BuildTarget.StandaloneWindows64 || active == BuildTarget.StandaloneLinux64 || active == BuildTarget.StandaloneOSX)
                return active;
            return CraDevSceneBuilder.EditorPlatformTarget();
        }

        /// <summary>Buyruq qatoridagi "-nom qiymat" juftligidan qiymatni oladi.</summary>
        static string Argument(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, System.StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(args[i + 1]))
                    return args[i + 1];
            return null;
        }
    }
}

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

        /// <summary>Sahnalarni yaratadi va o'yinni Builds/ papkasiga yig'adi (ishga tushirmaydi).</summary>
        public static void BuildGame()
        {
            try
            {
                CraDevSceneBuilder.CreateScenes();
                var report = CraDevSceneBuilder.BuildGame(run: false);
                bool ok = report.summary.result == BuildResult.Succeeded;
                Debug.Log(ok
                    ? $"[CraDev] BATCH OK: o'yin yig'ildi: {report.summary.outputPath} ({report.summary.totalSize / (1024 * 1024)} MB)"
                    : $"[CraDev] BATCH XATO: o'yinni yig'ib bo'lmadi ({report.summary.totalErrors} ta xato).");
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[CraDev] BATCH XATO: " + e);
                EditorApplication.Exit(1);
            }
        }
    }
}

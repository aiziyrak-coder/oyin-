using UnityEngine.SceneManagement;

namespace CraDev
{
    /// <summary>
    /// Sahna almashtirish. Loading ekrani faqat og'ir yuklashlarda (o'yin ochilishi, keyin dunyoga kirish):
    /// <c>SceneLoader.Load("Level01");</c>. Kichik ekranlar orasida (bosh menyu va personaj sozlash) ekran
    /// qorong'ilashgach sahna to'g'ridan-to'g'ri almashadi: <c>SceneLoader.Switch("MainMenu");</c>.
    /// </summary>
    public static class SceneLoader
    {
        public const string LoadingScene = "Loading";

        /// <summary>Loading ekrani yuklashi kerak bo'lgan sahna. Bo'sh bo'lsa, LoadingScreen'dagi standart sahna olinadi.</summary>
        public static string PendingScene { get; private set; }

        /// <summary>Loading ekrani orqali (foiz va chiziq bilan).</summary>
        public static void Load(string sceneName)
        {
            PendingScene = sceneName;
            SceneManager.LoadScene(LoadingScene);
        }

        /// <summary>Loading ekranisiz: chaqiruvchi ekranni qorong'ilatgan bo'ladi, yangi sahna o'zi yorishib ochiladi.</summary>
        public static void Switch(string sceneName) => SceneManager.LoadSceneAsync(sceneName);

        /// <summary>Kutilayotgan sahna nomini qaytaradi va uni tozalaydi.</summary>
        public static string TakePendingScene()
        {
            string scene = PendingScene;
            PendingScene = null;
            return scene;
        }
    }
}

using UnityEngine.SceneManagement;

namespace CraDev
{
    /// <summary>
    /// Sahnalarni Loading ekrani orqali yuklash.
    /// Masalan, menyudan o'yinga o'tishda: <c>SceneLoader.Load("Level01");</c>
    /// </summary>
    public static class SceneLoader
    {
        public const string LoadingScene = "Loading";

        /// <summary>Loading ekrani yuklashi kerak bo'lgan sahna. Bo'sh bo'lsa, LoadingScreen'dagi standart sahna olinadi.</summary>
        public static string PendingScene { get; private set; }

        public static void Load(string sceneName)
        {
            PendingScene = sceneName;
            SceneManager.LoadScene(LoadingScene);
        }

        /// <summary>Kutilayotgan sahna nomini qaytaradi va uni tozalaydi.</summary>
        public static string TakePendingScene()
        {
            string scene = PendingScene;
            PendingScene = null;
            return scene;
        }
    }
}

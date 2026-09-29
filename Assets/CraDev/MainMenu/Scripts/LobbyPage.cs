using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    /// <summary>
    /// Lobby sahifasi (Bosh sahifa, Dunyo, Garderob, Magazinlar ...): hammasi bitta sahnada, yuqoridagi navigatsiya
    /// bilan almashadi. Sahifa o'z fon rasmini, 3D qahramon ko'rinishini (kamera holati) va yuqoridagi qaysi yorliq
    /// yonishini aytadi. Kontenti sahifa ochilganda yangilanadi.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class LobbyPage : MonoBehaviour
    {
        [SerializeField] string pageId;
        [Tooltip("Yuqoridagi navigatsiyada qaysi yorliq yonadi (-1 - hech biri).")]
        [SerializeField] int navTab = -1;
        [Tooltip("Orqa fon rasmi (bo'sh - umumiy to'q fon).")]
        [SerializeField] Texture background;
        [Tooltip("3D qahramon kamerasi holati (LobbyStage.poses indeksi), -1 - qahramon ko'rinmaydi.")]
        [SerializeField] int stagePose = -1;

        CanvasGroup group;
        Texture liveBackground;

        public string Id => pageId;
        public int NavTab => navTab;
        public Texture Background => liveBackground != null ? liveBackground : background;
        public int StagePose => stagePose;
        public CanvasGroup Group => group != null ? group : group = GetComponent<CanvasGroup>();

        protected MainMenuScreen Lobby { get; private set; }

        public void Bind(MainMenuScreen lobby)
        {
            Lobby = lobby;
            if (lobby.SingleWindow) ReleaseArchivedArt(lobby);
        }

        /// <summary>Ish vaqtidagi fon (masalan, soat bo'yicha lobby rasmi): builder bergan rasm o'rniga ishlatiladi.</summary>
        public void SetLiveBackground(Texture texture) => liveBackground = texture;

        // Yagona oyna rejimida arxivdagi sahifalar ochilmaydi (MainMenuScreen.Show), lekin ularning to'liq ekranli
        // rasmlari (har biri ~6 MB, jami ~42 MB) sahna bilan birga xotiraga yuklanadi. Ular xotiradan bo'shatiladi:
        // sahifa baribir ko'rsatilsa, Unity rasmni diskdan o'zi qayta yuklaydi. Sahifalar va havolalar o'zgarmaydi.
        static MainMenuScreen released;
        static void ReleaseArchivedArt(MainMenuScreen lobby)
        {
            if (released == lobby || Application.isEditor) return;
            released = lobby;
            var live = new HashSet<Texture>();
            var archived = new HashSet<Texture>();
            foreach (var page in lobby.GetComponentsInChildren<LobbyPage>(true))
            {
                var target = page.Id == "home" || page.Id == "settings" ? live : archived;
                foreach (var texture in page.ArtTextures())
                    if (texture != null) target.Add(texture);
            }
            foreach (var texture in archived)
                if (texture is Texture2D && !live.Contains(texture)) Resources.UnloadAsset(texture);
        }

        /// <summary>Sahifa sahnada ushlab turgan katta rasmlar (fon, hero rasmlar va h.k.).</summary>
        protected virtual IEnumerable<Texture> ArtTextures()
        {
            yield return background;
            foreach (var image in GetComponentsInChildren<RawImage>(true))
                yield return image.texture;
        }

        /// <summary>Sahifa ochilganda (kontentni yangilash, serverdan ma'lumot olish).</summary>
        public virtual void OnShow() { }

        /// <summary>Sahifa yopilganda.</summary>
        public virtual void OnHide() { }

        /// <summary>Esc: sahifa o'zi ishlatsa true (masalan, qidiruvni tozalash). Aks holda Bosh sahifaga qaytiladi.</summary>
        public virtual bool OnBack() => false;

        /// <summary>Sahifadan chiqishdan oldin (masalan, saqlanmagan kiyim): false - chiqish to'xtatiladi, sahifa o'zi hal qiladi.</summary>
        public virtual bool CanLeave(System.Action leave) => true;

        protected virtual void OnEnable() => Loc.Changed += OnLanguageChanged;

        protected virtual void OnDisable() => Loc.Changed -= OnLanguageChanged;

        /// <summary>Til almashganda (runtime'da yozilgan matnlarni yangilash).</summary>
        protected virtual void OnLanguageChanged() { }
    }
}

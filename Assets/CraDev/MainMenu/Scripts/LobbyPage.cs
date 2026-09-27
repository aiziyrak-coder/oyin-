using UnityEngine;

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

        public string Id => pageId;
        public int NavTab => navTab;
        public Texture Background => background;
        public int StagePose => stagePose;
        public CanvasGroup Group => group != null ? group : group = GetComponent<CanvasGroup>();

        protected MainMenuScreen Lobby { get; private set; }

        public void Bind(MainMenuScreen lobby) => Lobby = lobby;

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

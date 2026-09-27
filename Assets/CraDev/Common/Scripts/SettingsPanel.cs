using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraDev
{
    /// <summary>
    /// Sozlamalar oynasi. Har bir qator: nomi va "&lt; qiymat &gt;" tanlagichi. O'zgarish darhol qo'llanadi va saqlanadi.
    /// Qatorlar tartibi: ekran rejimi, oyna o'lchami, grafika, V-Sync, ovoz, til.
    /// </summary>
    public class SettingsPanel : ModalWindow
    {
        [SerializeField] Button[] previousButtons;
        [SerializeField] Button[] nextButtons;
        [SerializeField] Text[] values;
        [SerializeField] CanvasGroup[] rows;
        [SerializeField] Button closeButton;

        const int Display = 0, Size = 1, Graphics = 2, Sync = 3, Sound = 4, Lang = 5;
        List<Vector2Int> resolutions;

        protected override void Awake()
        {
            base.Awake();
            for (int i = 0; i < values.Length; i++)
            {
                int row = i;
                previousButtons[i].onClick.AddListener(() => Change(row, -1));
                nextButtons[i].onClick.AddListener(() => Change(row, +1));
            }
            closeButton.onClick.AddListener(Close);
        }

        public void Show()
        {
            resolutions = GameSettings.Resolutions();
            Refresh();
            Open();
        }

        void Change(int row, int step)
        {
            switch (row)
            {
                case Display:
                    GameSettings.Fullscreen = !GameSettings.Fullscreen;
                    break;
                case Size:
                    if (GameSettings.Fullscreen || resolutions.Count == 0)
                        return;
                    int index = Mathf.Max(0, resolutions.IndexOf(GameSettings.Resolution));
                    GameSettings.Resolution = resolutions[Mod(index - step, resolutions.Count)]; // "keyingi" = kattaroq
                    break;
                case Graphics:
                    GameSettings.Quality = Mod(GameSettings.Quality + step, QualitySettings.names.Length);
                    break;
                case Sync:
                    GameSettings.VSync = !GameSettings.VSync;
                    break;
                case Sound:
                    GameSettings.Volume = Mathf.Clamp01(Mathf.Round(GameSettings.Volume * 10f + step) / 10f);
                    break;
                case Lang:
                    Loc.Current = Loc.Current == Language.Uz ? Language.En : Language.Uz;
                    break;
            }
            GameSettings.Apply();
            GameSettings.Save();
            Refresh();
        }

        void Refresh()
        {
            values[Display].text = Loc.T(GameSettings.Fullscreen ? "settings.fullscreen" : "settings.windowed");
            var size = GameSettings.Fullscreen ? new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height) : GameSettings.Resolution;
            values[Size].text = $"{size.x} × {size.y}";
            // Oyna o'lchami faqat oynali rejimda tanlanadi
            rows[Size].alpha = GameSettings.Fullscreen ? 0.4f : 1f;
            rows[Size].interactable = !GameSettings.Fullscreen;
            values[Graphics].text = QualityName(GameSettings.Quality);
            values[Sync].text = Loc.T(GameSettings.VSync ? "settings.on" : "settings.off");
            values[Sound].text = Mathf.RoundToInt(GameSettings.Volume * 100f) + "%";
            if (values.Length > Lang)
                values[Lang].text = Loc.Current == Language.Uz ? "O'zbekcha" : "English";
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Loc.Changed += Refresh;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Loc.Changed -= Refresh;
        }

        /// <summary>Unity sifat darajalari (Very Low … Ultra) joriy tilda.</summary>
        static string QualityName(int level)
        {
            string key = "quality." + level;
            return QualitySettings.names.Length == 6 ? Loc.T(key) : QualitySettings.names[level];
        }

        static int Mod(int a, int n) => ((a % n) + n) % n;
    }
}

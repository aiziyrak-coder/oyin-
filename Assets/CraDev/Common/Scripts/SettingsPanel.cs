using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraDev
{
    /// <summary>
    /// Sozlamalar oynasi. Har bir qator: nomi va "&lt; qiymat &gt;" tanlagichi. O'zgarish darhol qo'llanadi va saqlanadi.
    /// Qatorlar tartibi: ekran rejimi, oyna o'lchami, grafika, V-Sync, ovoz.
    /// </summary>
    public class SettingsPanel : ModalWindow
    {
        [SerializeField] Button[] previousButtons;
        [SerializeField] Button[] nextButtons;
        [SerializeField] Text[] values;
        [SerializeField] CanvasGroup[] rows;
        [SerializeField] Button closeButton;

        const int Display = 0, Size = 1, Graphics = 2, Sync = 3, Sound = 4;
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
            }
            GameSettings.Apply();
            GameSettings.Save();
            Refresh();
        }

        void Refresh()
        {
            values[Display].text = GameSettings.Fullscreen ? "Fullscreen" : "Windowed";
            var size = GameSettings.Fullscreen ? new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height) : GameSettings.Resolution;
            values[Size].text = $"{size.x} × {size.y}";
            // Oyna o'lchami faqat oynali rejimda tanlanadi
            rows[Size].alpha = GameSettings.Fullscreen ? 0.4f : 1f;
            rows[Size].interactable = !GameSettings.Fullscreen;
            values[Graphics].text = QualitySettings.names[GameSettings.Quality];
            values[Sync].text = GameSettings.VSync ? "On" : "Off";
            values[Sound].text = Mathf.RoundToInt(GameSettings.Volume * 100f) + "%";
        }

        static int Mod(int a, int n) => ((a % n) + n) % n;
    }
}

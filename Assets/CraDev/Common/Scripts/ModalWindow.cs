using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraDev
{
    /// <summary>
    /// Ekran o'rtasidagi oyna (ConfirmDialog, SettingsPanel) uchun umumiy asos: silliq ochiladi/yopiladi,
    /// orqadagi elementlarni bosib bo'lmaydi, Esc bilan yopiladi.
    /// </summary>
    public abstract class ModalWindow : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform panel;

        static readonly List<ModalWindow> open = new List<ModalWindow>();
        float shown;
        bool visible;

        /// <summary>Hozir ekranda biror oyna ochiqmi (ekranlar Esc'ni o'zi ishlatmasligi uchun).</summary>
        public static bool AnyOpen => open.Count > 0;

        protected virtual void Awake()
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            gameObject.SetActive(false);
        }

        protected void Open()
        {
            gameObject.SetActive(true);
            visible = true;
            group.blocksRaycasts = true;
            group.interactable = true;
            if (!open.Contains(this))
                open.Add(this);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        public virtual void Close()
        {
            visible = false;
            group.blocksRaycasts = false;
            group.interactable = false;
            open.Remove(this);
        }

        protected virtual void OnEnable() { }

        protected virtual void OnDisable() => open.Remove(this);

        protected virtual void Update()
        {
            float dt = Time.unscaledDeltaTime;
            shown = Mathf.MoveTowards(shown, visible ? 1f : 0f, dt / 0.18f);
            group.alpha = Ease.OutCubic(shown);
            panel.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, Ease.OutCubic(shown));
            if (!visible && shown <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }
            // Eng ustidagi oyna Esc bilan yopiladi
            if (visible && open.Count > 0 && open[open.Count - 1] == this && Anim.BackPressed())
                OnBack();
        }

        protected virtual void OnBack() => Close();
    }
}

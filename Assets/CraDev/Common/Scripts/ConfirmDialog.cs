using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraDev
{
    /// <summary>"Quit game?" kabi tasdiqlash oynasi.</summary>
    public class ConfirmDialog : ModalWindow
    {
        [SerializeField] Text titleText;
        [SerializeField] Text messageText;
        [SerializeField] Button confirmButton;
        [SerializeField] Text confirmLabel;
        [SerializeField] Button cancelButton;
        [SerializeField] Text cancelLabel;

        Action onConfirm;
        Action onCancel;
        bool dismissable = true;

        protected override void Awake()
        {
            base.Awake();
            confirmButton.onClick.AddListener(() => { Close(); onConfirm?.Invoke(); });
            cancelButton.onClick.AddListener(OnBack);
        }

        /// <summary>
        /// cancel = null bo'lsa, bekor qilish tugmasi yashiriladi (faqat bitta tanlov).
        /// dismissable = false: Esc bilan yopilmaydi (majburiy tanlov).
        /// </summary>
        public void Show(string title, string message, string confirm, Action confirmed, string cancel = null, Action cancelled = null, bool dismissable = true)
        {
            this.dismissable = dismissable;
            titleText.text = title;
            messageText.text = message;
            confirmLabel.text = confirm;
            onConfirm = confirmed;
            onCancel = cancelled;
            cancelButton.gameObject.SetActive(cancel != null);
            if (cancel != null)
                cancelLabel.text = cancel;
            Open();
        }

        protected override void OnBack()
        {
            if (!dismissable)
                return; // majburiy tanlov: Esc bilan yopilmaydi
            Close();
            onCancel?.Invoke();
        }

        /// <summary>O'yindan chiqish (tahrirlovchida Play rejimi to'xtaydi).</summary>
        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

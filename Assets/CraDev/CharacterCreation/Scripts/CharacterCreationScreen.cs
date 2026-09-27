using System.Collections;
using CraDev.Online;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev.CharacterCreation
{
    /// <summary>
    /// Avatar yaratish ekrani (Loading'dan keyin, faqat birinchi kirishda).
    ///
    /// Chap tomonda nickname va jins tanlanadi, o'ngda 3D avatar ko'rinadi.
    /// Nickname yozilayotganda server'da band yoki bo'shligi tekshiriladi (kichik kechikish bilan).
    /// "Create character" bosilganda server nickname'ni band qiladi; bir vaqtda boshqa o'yinchi
    /// olib qo'ygan bo'lsa, server rad etadi va o'yinchi boshqa nom tanlaydi.
    /// </summary>
    public class CharacterCreationScreen : MonoBehaviour
    {
        enum NickState { Empty, Invalid, Checking, Available, Taken, Offline }

        [Header("Server")]
        [Tooltip("O'yin serveri manzili (Server/ papkasi). Keyinchalik haqiqiy server manziliga almashtiriladi.")]
        [SerializeField] string serverUrl = "http://localhost:8080";
        [Tooltip("Profil yaratilgach ochiladigan sahna.")]
        [SerializeField] string nextScene = "MainMenu";
        [Tooltip("Yozish to'xtagach, server'dan so'rashdan oldin kutish (soniya).")]
        [SerializeField] float checkDelay = 0.35f;

        [Header("Nickname")]
        [SerializeField] InputField nicknameInput;
        [SerializeField] Image fieldBorder;
        [SerializeField] Image fieldGlow;
        [SerializeField] Image statusIcon;
        [SerializeField] Image helpIcon;
        [SerializeField] Text helpText;

        [Header("Jins")]
        [SerializeField] GenderCard maleCard;
        [SerializeField] GenderCard femaleCard;

        [Header("Tugma va xatolar")]
        [SerializeField] Button createButton;
        [SerializeField] Image createFill;
        [SerializeField] Text createLabel;
        [SerializeField] Image createIcon;
        [SerializeField] Image errorIcon;
        [SerializeField] Text errorText;

        [Header("Avatar")]
        [SerializeField] AvatarPreview avatar;
        [SerializeField] RectTransform nameplate;
        [SerializeField] Image nameplateIcon;
        [SerializeField] Text nameplateText;
        [SerializeField] Image hintIcon;
        [SerializeField] Text hintText;
        [SerializeField] Image fader;

        [Header("Ikonkalar")]
        [SerializeField] Sprite checkSprite;
        [SerializeField] Sprite closeSprite;
        [SerializeField] Sprite spinnerSprite;
        [SerializeField] Sprite alertSprite;
        [SerializeField] Sprite arrowSprite;
        [SerializeField] Sprite maleSprite;
        [SerializeField] Sprite femaleSprite;

        const string HelpDefault = "3–16 characters: letters, numbers and _. Starts with a letter.";

        static readonly Color Accent = new Color32(61, 90, 254, 255);
        static readonly Color Ok = new Color32(34, 197, 94, 255);
        static readonly Color Bad = new Color32(240, 82, 82, 255);
        static readonly Color Warn = new Color32(245, 165, 36, 255);
        static readonly Color Muted = new Color32(142, 147, 154, 255);
        static readonly Color Faint = new Color32(90, 94, 102, 255);
        static readonly Color Line = new Color32(38, 39, 44, 255);
        static readonly Color Disabled = new Color32(28, 29, 33, 255);

        GameApi api;
        NickState state;
        string checkedNickname;
        bool female;
        bool submitting;
        bool done;
        int checkSeq;
        Coroutine checkRoutine;
        float time;

        void Start()
        {
            Cursor.visible = true;
            api = new GameApi(serverUrl);

            nicknameInput.characterLimit = NicknameRules.MaxLength;
            nicknameInput.onValidateInput += (text, index, c) => NicknameRules.IsAllowedChar(c) ? c : '\0';
            nicknameInput.onValueChanged.AddListener(OnNicknameChanged);
            maleCard.GetComponent<Button>().onClick.AddListener(() => SelectGender(false));
            femaleCard.GetComponent<Button>().onClick.AddListener(() => SelectGender(true));
            createButton.onClick.AddListener(Submit);

            SelectGender(false, instant: true);
            SetNickState(NickState.Empty);
            SetError(null);
            SetButton("Create character", arrowSprite, Accent);
            UpdateNameplate();
            LayoutIconLabel(hintIcon.rectTransform, hintText, 10f);
            nicknameInput.ActivateInputField();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            time += dt;

            if (fader != null)
                Anim.SetAlpha(fader, 1f - Ease.OutCubic(Anim.Progress(time, 0f, 0.4f)));

            // Maydon ramkasi: holatga qarab rang, yozilayotganda ko'k va atrofida yengil nur
            bool focused = nicknameInput.isFocused;
            bool neutral = state == NickState.Empty || state == NickState.Checking;
            fieldBorder.color = state == NickState.Available ? Ok
                : state == NickState.Invalid || state == NickState.Taken ? Bad
                : state == NickState.Offline ? Warn
                : focused ? Accent : Line;
            fieldGlow.enabled = focused && neutral;

            // Aylanuvchi "yuklanmoqda" belgilari
            if (state == NickState.Checking)
                statusIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -time * 450f);
            if (submitting)
                createIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -time * 450f);

            if (Anim.SubmitPressed())
                Submit();
        }

        // ------------------------------------------------------------ Nickname

        void OnNicknameChanged(string value)
        {
            SetError(null);
            UpdateNameplate();

            checkSeq++;
            if (checkRoutine != null)
                StopCoroutine(checkRoutine);

            if (string.IsNullOrWhiteSpace(value))
            {
                SetNickState(NickState.Empty);
                return;
            }
            if (!NicknameRules.Validate(value, out string nickname, out string message))
            {
                SetNickState(NickState.Invalid, message);
                return;
            }
            SetNickState(NickState.Checking);
            checkRoutine = StartCoroutine(CheckAvailability(nickname, checkSeq));
        }

        IEnumerator CheckAvailability(string nickname, int seq)
        {
            yield return new WaitForSecondsRealtime(checkDelay);
            while (true)
            {
                ApiResult<AvailabilityResponse> result = default;
                yield return api.CheckNickname(nickname, r => result = r);
                if (seq != checkSeq)
                    yield break; // o'yinchi bu orada nomni o'zgartirdi

                if (result.NetworkError || result.Data == null)
                {
                    SetNickState(NickState.Offline, "Can't reach the server. Retrying…");
                    yield return new WaitForSecondsRealtime(4f);
                    if (seq != checkSeq)
                        yield break;
                    SetNickState(NickState.Checking);
                    continue;
                }

                if (result.Data.available)
                {
                    checkedNickname = nickname;
                    SetNickState(NickState.Available);
                }
                else
                {
                    SetNickState(result.Data.reason == "taken" ? NickState.Taken : NickState.Invalid,
                        string.IsNullOrEmpty(result.Data.message) ? "This nickname can't be used." : result.Data.message);
                }
                yield break;
            }
        }

        void SetNickState(NickState newState, string message = null)
        {
            state = newState;
            if (newState != NickState.Available)
                checkedNickname = null;

            statusIcon.enabled = newState != NickState.Empty && newState != NickState.Offline;
            statusIcon.rectTransform.localRotation = Quaternion.identity;
            switch (newState)
            {
                case NickState.Checking: statusIcon.sprite = spinnerSprite; statusIcon.color = Muted; break;
                case NickState.Available: statusIcon.sprite = checkSprite; statusIcon.color = Ok; break;
                case NickState.Invalid:
                case NickState.Taken: statusIcon.sprite = closeSprite; statusIcon.color = Bad; break;
            }

            switch (newState)
            {
                case NickState.Available: SetHelp("Nickname is available", Ok, checkSprite); break;
                case NickState.Checking: SetHelp("Checking availability…", Muted, null); break;
                case NickState.Invalid:
                case NickState.Taken: SetHelp(message, Bad, null); break;
                case NickState.Offline: SetHelp(message, Warn, alertSprite); break;
                default: SetHelp(HelpDefault, Muted, null); break;
            }
            UpdateCreateButton();
        }

        void SetHelp(string text, Color color, Sprite icon)
        {
            helpText.text = text;
            helpText.color = color;
            helpIcon.enabled = icon != null;
            if (icon != null)
            {
                helpIcon.sprite = icon;
                helpIcon.color = color;
            }
            var rect = helpText.rectTransform;
            rect.anchoredPosition = new Vector2(icon != null ? 24f : 0f, rect.anchoredPosition.y);
        }

        // ------------------------------------------------------------ Jins va avatar

        void SelectGender(bool isFemale, bool instant = false)
        {
            female = isFemale;
            maleCard.SetSelected(!isFemale);
            femaleCard.SetSelected(isFemale);
            avatar.SetFemale(isFemale, instant);
            nameplateIcon.sprite = isFemale ? femaleSprite : maleSprite;
        }

        void UpdateNameplate()
        {
            string value = nicknameInput.text.Trim();
            bool empty = value.Length == 0;
            nameplateText.text = empty ? "Your nickname" : value;
            nameplateText.color = empty ? Faint : Color.white;

            // Pill kengligi matnga moslashadi: 14 + ikonka 18 + 10 + matn + 20
            float width = 14f + 18f + 10f + nameplateText.preferredWidth + 20f;
            nameplate.sizeDelta = new Vector2(width, nameplate.sizeDelta.y);
            nameplateIcon.rectTransform.anchoredPosition = new Vector2(-width / 2f + 14f + 9f, 0f);
            nameplateText.rectTransform.anchoredPosition = new Vector2(-width / 2f + 14f + 18f + 10f + nameplateText.preferredWidth / 2f, 0f);
        }

        // ------------------------------------------------------------ Yaratish

        bool CanSubmit => state == NickState.Available && !submitting && !done && checkedNickname != null;

        void UpdateCreateButton()
        {
            if (done)
                return;
            bool enabled = CanSubmit;
            createButton.interactable = enabled;
            if (!submitting)
                SetButtonColors(enabled ? Accent : Disabled, enabled ? Color.white : Faint);
        }

        void Submit()
        {
            if (!CanSubmit)
                return;
            submitting = true;
            SetError(null);
            createButton.interactable = false;
            SetButton("Creating…", spinnerSprite, Accent);
            StartCoroutine(api.CreatePlayer(checkedNickname, female ? "female" : "male", OnCreated));
        }

        void OnCreated(ApiResult<PlayerResponse> result)
        {
            submitting = false;
            createIcon.rectTransform.localRotation = Quaternion.identity;

            if (!result.NetworkError && result.Status == 201 && result.Data != null)
            {
                done = true;
                PlayerProfile.Save(result.Data);
                nicknameInput.interactable = false;
                maleCard.GetComponent<Button>().interactable = false;
                femaleCard.GetComponent<Button>().interactable = false;
                createButton.interactable = false;
                SetButton($"Welcome, {result.Data.nickname}", checkSprite, Ok);
                StartCoroutine(GoNext());
                return;
            }

            SetButton("Create character", arrowSprite, Accent);
            if (result.NetworkError)
                SetError("Can't reach the server. Check your connection and try again.");
            else if (result.Status == 409)
                SetNickState(NickState.Taken, "Someone just took this nickname. Try another one.");
            else if (result.Status == 400)
                SetNickState(NickState.Invalid, result.Data?.message ?? "This nickname can't be used.");
            else if (result.Status == 429)
                SetError("Too many attempts. Wait a minute and try again.");
            else
                SetError("Something went wrong on the server. Try again.");
            UpdateCreateButton();
        }

        IEnumerator GoNext()
        {
            yield return new WaitForSecondsRealtime(1.1f);
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                if (fader != null)
                    Anim.SetAlpha(fader, Ease.InOutSine(t / 0.5f));
                yield return null;
            }
            SceneLoader.Load(nextScene);
        }

        // ------------------------------------------------------------ Yordamchilar

        void SetButton(string label, Sprite icon, Color fill)
        {
            createLabel.text = label;
            createIcon.sprite = icon;
            SetButtonColors(fill, Color.white);
            LayoutIconLabel(createIcon.rectTransform, createLabel, 12f, iconAfter: true);
        }

        void SetButtonColors(Color fill, Color content)
        {
            createFill.color = fill;
            createLabel.color = content;
            createIcon.color = content;
        }

        void SetError(string message)
        {
            bool show = !string.IsNullOrEmpty(message);
            errorText.text = show ? message : "";
            errorIcon.enabled = show;
        }

        /// <summary>Ikonka va matnni bitta guruh sifatida markazga joylaydi.</summary>
        static void LayoutIconLabel(RectTransform icon, Text label, float gap, bool iconAfter = false)
        {
            float iconWidth = icon.sizeDelta.x;
            float textWidth = label.preferredWidth;
            float total = iconWidth + gap + textWidth;
            float left = -total / 2f;
            if (iconAfter)
            {
                label.rectTransform.anchoredPosition = new Vector2(left + textWidth / 2f, label.rectTransform.anchoredPosition.y);
                icon.anchoredPosition = new Vector2(left + textWidth + gap + iconWidth / 2f, icon.anchoredPosition.y);
            }
            else
            {
                icon.anchoredPosition = new Vector2(left + iconWidth / 2f, icon.anchoredPosition.y);
                label.rectTransform.anchoredPosition = new Vector2(left + iconWidth + gap + textWidth / 2f, label.rectTransform.anchoredPosition.y);
            }
        }
    }
}

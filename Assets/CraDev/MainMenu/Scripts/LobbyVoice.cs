using System;
using System.Collections;
using System.Collections.Generic;
using CraDev.Online;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CraDev.MainMenu
{
    /// <summary>
    /// Lobbydagi ovozli chat (party a'zolari orasida). WebSocket yo'q, shuning uchun oddiy HTTP(S):
    /// mikrofon 20 ms kadrlarga bo'linadi, sodda energiya darvozasi sukutni yubormaydi, ~200 ms bo'laklar 8-bit mu-law
    /// qilib POST /api/party/voice ga yuboriladi; boshqalarning bo'laklari GET /api/party/voice?since= bilan o'qiladi
    /// va har so'zlovchi uchun jitter buferli oqimli AudioClip'da ijro etiladi.
    /// Karnay o'chsa mikrofon ham o'chadi; karnayni yoqish mikrofonni avtomatik yoqmaydi.
    /// Holatlar PlayerPrefs'da; M tugmasi mikrofonni almashtiradi. Aks-sado bekor qilinmaydi: quloqchin tavsiya etiladi.
    /// </summary>
    public sealed class LobbyVoice : MonoBehaviour
    {
        [SerializeField] MainMenuScreen lobby;
        [SerializeField] Button micButton, speakerButton;
        [SerializeField] LobbyVoiceIcon micIcon, speakerIcon;

        const string MicPref = "cradev.voice.mic", SpeakerPref = "cradev.voice.speaker";
        const int PreferredRate = 16000;
        const float GateLevel = .012f, GateHold = .45f, ChunkSeconds = .2f;

        bool micOn, speakerOn, micMissing;
        AudioClip micClip;
        string activeDevice;
        int micRate, readPos, frameSize;
        float[] frame, previous;
        bool previousValid;
        readonly List<float> pending = new List<float>();
        float gateUntil = -1, lastVoiced = -10;
        readonly Queue<VoiceChunk> outbox = new Queue<VoiceChunk>();
        int seq, cursor = -1;
        string cursorRoom;
        readonly Dictionary<string, Stream> streams = new Dictionary<string, Stream>(StringComparer.OrdinalIgnoreCase);
        LobbyParty party;

        /// <summary>Holat o'zgardi (party heartbeat'i uni darhol serverga yuboradi).</summary>
        public event Action StateChanged;
        public bool MicOn => micOn;
        public bool SpeakerOn => speakerOn;
        /// <summary>O'yinchining o'zi hozir gapiryaptimi (darvoza ochiq).</summary>
        public bool SelfSpeaking => micClip != null && Time.realtimeSinceStartup - lastVoiced < .3f;
        /// <summary>Shu a'zoning ovozi hozir ijro etilyaptimi.</summary>
        public bool IsSpeaking(string nickname) =>
            speakerOn && nickname != null && streams.TryGetValue(nickname, out var s) && s.Audible;

        void Start()
        {
            party = GetComponent<LobbyParty>();
            micMissing = Microphone.devices == null || Microphone.devices.Length == 0;
            speakerOn = PlayerPrefs.GetInt(SpeakerPref, 1) == 1;
            micOn = speakerOn && !micMissing && PlayerPrefs.GetInt(MicPref, 0) == 1;
            // Read-only UI diagnostics must never open the microphone or change saved preferences.
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-cradevShot") >= 0) micOn = false;
            if (micButton != null) micButton.onClick.AddListener(ToggleMic);
            if (speakerButton != null) speakerButton.onClick.AddListener(() => SetSpeaker(!speakerOn));
            RefreshButtons();
            StartCoroutine(SendLoop());
            StartCoroutine(PollLoop());
        }

        public void ToggleMic()
        {
            if (!speakerOn) { lobby.Toast(Loc.T("voice.need_speaker")); return; }
            if (!micOn && MicUnavailable()) return;
            micOn = !micOn;
            lobby.Toast(Loc.T(micOn ? "voice.mic_on" : "voice.mic_off"));
            Changed();
        }

        /// <summary>Karnay: o'chsa mikrofon ham o'chadi; yoqilsa mikrofon avtomatik yoqiladi.</summary>
        public void SetSpeaker(bool on)
        {
            speakerOn = on;
            if (!on) micOn = false; // Enabling playback is not consent to enable the microphone.
            lobby.Toast(Loc.T(on ? (micOn ? "voice.speaker_on" : "voice.speaker_on_nomic") : "voice.speaker_off"));
            Changed();
        }

        bool MicUnavailable()
        {
            micMissing = VoicePreferences.Device == null;
            if (micMissing) lobby.Toast(Loc.T("voice.no_mic"));
            return micMissing;
        }

        void Changed()
        {
            PlayerPrefs.SetInt(SpeakerPref, speakerOn ? 1 : 0);
            // Mikrofon tanlovi karnay o'chganda ham eslab qolinmaydi: karnay yoqilsa u baribir yoqiladi
            PlayerPrefs.SetInt(MicPref, micOn ? 1 : 0);
            PlayerPrefs.Save();
            RefreshButtons();
            StateChanged?.Invoke();
        }

        void RefreshButtons()
        {
            if (micButton != null) micButton.interactable = speakerOn;
            if (speakerIcon != null) { speakerIcon.Off = !speakerOn; speakerIcon.color = speakerOn ? Color.white : new Color(1f, .45f, .45f); }
            if (micIcon != null)
            {
                micIcon.Off = !micOn;
                micIcon.color = !speakerOn || micMissing ? new Color(.6f, .6f, .62f, .55f) : micOn ? Color.white : new Color(1f, .45f, .45f);
            }
        }

        bool Together => party != null && party.State != null && party.State.members != null && party.State.members.Length > 1;

        void Update()
        {
            if (HotkeyPressed()) ToggleMic();
            if(micClip!=null&&activeDevice!=VoicePreferences.Device)StopMic();
            bool capture = micOn && speakerOn && Together && (!VoicePreferences.PushToTalk || TalkHeld());
            if (capture && micClip == null) StartMic();
            else if (!capture && micClip != null) StopMic();
            if (micClip != null) ReadMic();
            // Gapirayotgan a'zoning mikrofon belgisi yashil (o'zimiz ham)
            if (micIcon != null && micOn && speakerOn) micIcon.color = SelfSpeaking ? new Color(.35f, 1f, .6f) : Color.white;
            // Guruhdan chiqqan yoki karnay o'chgan: oqimlar yopiladi
            if (streams.Count > 0)
            {
                List<string> gone = null;
                foreach (var pair in streams)
                {
                    pair.Value.SetVolume(VoicePreferences.Volume);
                    if (!speakerOn || !party.InParty(pair.Key) || Time.realtimeSinceStartup - pair.Value.LastChunk > 20)
                        (gone ??= new List<string>()).Add(pair.Key);
                }
                if (gone != null) foreach (var key in gone) { streams[key].Dispose(); streams.Remove(key); }
            }
        }

        bool HotkeyPressed()
        {
            if (ModalWindow.AnyOpen || lobby.SettingsOpen || lobby.AvatarStudioOpen) return false;
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            var field = selected != null ? selected.GetComponent<InputField>() : null;
            if (field != null && field.isFocused) return false;
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.M);
#else
            return false;
#endif
        }

        // ---------------- Yozish
        bool TalkHeld()
        {
            if(ModalWindow.AnyOpen||lobby.SettingsOpen||lobby.AvatarStudioOpen)return false;
            var selected=EventSystem.current?.currentSelectedGameObject;
            if(selected!=null&&selected.GetComponent<InputField>()?.isFocused==true)return false;
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current!=null&&Keyboard.current.vKey.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.V);
#else
            return false;
#endif
        }
        void StartMic()
        {
            if (VoicePreferences.Device == null)
            {
                micMissing = true; micOn = false; lobby.Toast(Loc.T("voice.no_mic")); Changed(); return;
            }
            string device = activeDevice = VoicePreferences.Device;
            Microphone.GetDeviceCaps(device, out int min, out int max);
            micRate = PreferredRate;
            // 0/0 - qurilma istalgan chastotani qabul qiladi
            if (max > 0) micRate = Mathf.Clamp(PreferredRate, Mathf.Max(8000, min), Mathf.Min(48000, max));
            micClip = Microphone.Start(device, true, 1, micRate);
            if (micClip == null)
            {
                micOn = false; lobby.Toast(Loc.T("voice.mic_error")); Changed(); return;
            }
            micRate = micClip.frequency;
            // 20 ms kadr: 1 s halqa kadrlarga teng bo'linadi (o'qish joyi hech qachon kadr o'rtasida uzilmaydi)
            frameSize = Mathf.Max(1, micRate / 50);
            if (micClip.samples % frameSize != 0) frameSize = micClip.samples / 50;
            frame = new float[frameSize]; previous = new float[frameSize]; previousValid = false;
            readPos = 0; pending.Clear(); gateUntil = -1;
        }

        void StopMic()
        {
            if (micClip == null) return;
            Flush();
            Microphone.End(activeDevice);
            Destroy(micClip);
            micClip = null;
            activeDevice = null;
        }

        void ReadMic()
        {
            string device = activeDevice;
            if (device == null || !Microphone.IsRecording(device)) { StopMic(); micOn = false; lobby.Toast(Loc.T("voice.mic_error")); Changed(); return; }
            int position = Microphone.GetPosition(device), length = micClip.samples;
            if (position < 0) return;
            int available = (position - readPos + length) % length;
            float now = Time.realtimeSinceStartup;
            while (available >= frameSize)
            {
                micClip.GetData(frame, readPos);
                readPos = (readPos + frameSize) % length; available -= frameSize;
                float sum = 0; for (int i = 0; i < frameSize; i++) sum += frame[i] * frame[i];
                float rms = Mathf.Sqrt(sum / frameSize);
                if (rms > GateLevel) { lastVoiced = now; if (gateUntil < now && previousValid) pending.AddRange(previous); gateUntil = now + GateHold; }
                if (now < gateUntil) pending.AddRange(frame);
                else if (pending.Count > 0) Flush();
                Array.Copy(frame, previous, frameSize); previousValid = true;
                if (pending.Count >= micRate * ChunkSeconds) Flush();
            }
        }

        void Flush()
        {
            if (pending.Count == 0) return;
            var bytes = new byte[pending.Count];
            for (int i = 0; i < bytes.Length; i++) bytes[i] = MuLaw.Encode(pending[i]);
            pending.Clear();
            outbox.Enqueue(new VoiceChunk { seq = ++seq, rate = micRate, data = Convert.ToBase64String(bytes) });
            // Tarmoq sekin bo'lsa eski bo'laklar tashlanadi (kechikish o'smasin)
            while (outbox.Count > 6) outbox.Dequeue();
        }

        IEnumerator SendLoop()
        {
            while (true)
            {
                if (outbox.Count == 0 || string.IsNullOrEmpty(PlayerProfile.Token)) { yield return null; continue; }
                var chunk = outbox.Dequeue();
                ApiResult<VoiceCursor> result = default;
                yield return lobby.Api.VoiceSend(PlayerProfile.Token, chunk, r => result = r);
                if (result.NetworkError) { outbox.Clear(); yield return new WaitForSecondsRealtime(.5f); }
            }
        }

        // ---------------- Eshitish
        IEnumerator PollLoop()
        {
            while (true)
            {
                if (!speakerOn || !Together || string.IsNullOrEmpty(PlayerProfile.Token))
                {
                    cursor = -1; yield return new WaitForSecondsRealtime(.3f); continue;
                }
                string room = party.State.roomId;
                if (room != cursorRoom) { cursorRoom = room; cursor = -1; }
                ApiResult<VoiceBatch> result = default;
                yield return lobby.Api.VoicePoll(PlayerProfile.Token, cursor, r => result = r);
                if (!result.Ok) { yield return new WaitForSecondsRealtime(1f); continue; }
                if (cursorRoom != party.State?.roomId) continue;
                cursor = result.Data.cursor;
                if (speakerOn && result.Data.chunks != null)
                    foreach (var chunk in result.Data.chunks) Play(chunk);
                yield return new WaitForSecondsRealtime(.12f);
            }
        }

        void Play(VoiceChunk chunk)
        {
            if (chunk == null || string.IsNullOrEmpty(chunk.nickname) || string.IsNullOrEmpty(chunk.data)) return;
            if (chunk.rate < 8000 || chunk.rate > 48000) return;
            byte[] bytes;
            try { bytes = Convert.FromBase64String(chunk.data); } catch (FormatException) { return; }
            if (!streams.TryGetValue(chunk.nickname, out var stream) || stream.Rate != chunk.rate)
            {
                stream?.Dispose();
                stream = new Stream(transform, chunk.nickname, chunk.rate);
                streams[chunk.nickname] = stream;
            }
            var samples = new float[bytes.Length];
            for (int i = 0; i < bytes.Length; i++) samples[i] = MuLaw.Decode(bytes[i]);
            stream.Push(samples, Time.realtimeSinceStartup);
        }

        void OnDisable()
        {
            StopMic();
            foreach (var s in streams.Values) s.Dispose();
            streams.Clear();
            outbox.Clear();
        }

        /// <summary>
        /// Bitta so'zlovchining oqimi: jitter bufer (~180 ms to'planguncha jim), kechikish 700 ms dan oshsa eski qismi
        /// tashlanadi. Audio ma'lumotni oqimli AudioClip o'qish funksiyasi oladi (audio oqimida ham chaqirilishi mumkin: lock).
        /// </summary>
        sealed class Stream
        {
            public readonly int Rate;
            public float LastChunk { get; private set; }
            readonly GameObject host;
            readonly AudioClip clip;
            readonly AudioSource source;
            readonly float[] ring;
            readonly object gate = new object();
            int read, count;
            bool playing;
            public bool Audible { get { lock (gate) return playing; } }

            public Stream(Transform parent, string nickname, int rate)
            {
                Rate = rate;
                ring = new float[rate * 2];
                host = new GameObject("Voice_" + nickname);
                host.transform.SetParent(parent, false);
                source = host.AddComponent<AudioSource>();
                clip = AudioClip.Create("Voice_" + nickname, rate, 1, rate, true, Read);
                source.clip = clip; source.loop = true; source.spatialBlend = 0; source.playOnAwake = false;
                source.volume=VoicePreferences.Volume;source.Play();
            }

            public void SetVolume(float value) { if(source!=null)source.volume=value; }

            public void Push(float[] samples, float now)
            {
                LastChunk = now;
                lock (gate)
                {
                    foreach (float s in samples)
                    {
                        if (count == ring.Length) { read = (read + 1) % ring.Length; count--; }
                        ring[(read + count) % ring.Length] = s; count++;
                    }
                    int limit = (int)(Rate * .7f), keep = (int)(Rate * .25f);
                    if (count > limit) { read = (read + count - keep) % ring.Length; count = keep; }
                }
            }

            void Read(float[] data)
            {
                lock (gate)
                {
                    if (!playing && count >= Rate * .18f) playing = true;
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (playing && count > 0) { data[i] = ring[read]; read = (read + 1) % ring.Length; count--; }
                        else { data[i] = 0; playing = false; }
                    }
                }
            }

            public void Dispose()
            {
                if (host != null) UnityEngine.Object.Destroy(host);
                if (clip != null) UnityEngine.Object.Destroy(clip);
            }
        }
    }

    /// <summary>G.711 mu-law: 16-bit PCM namunani 8 bitga siqish va qaytarish.</summary>
    public static class MuLaw
    {
        const int Bias = 0x84, Clip = 32635;

        public static byte Encode(float sample)
        {
            int pcm = (int)(Mathf.Clamp(sample, -1f, 1f) * 32767f);
            int sign = (pcm >> 8) & 0x80;
            if (sign != 0) pcm = -pcm;
            if (pcm > Clip) pcm = Clip;
            pcm += Bias;
            int exponent = 7;
            for (int mask = 0x4000; (pcm & mask) == 0 && exponent > 0; mask >>= 1) exponent--;
            int mantissa = (pcm >> (exponent + 3)) & 0x0F;
            return (byte)~(sign | (exponent << 4) | mantissa);
        }

        public static float Decode(byte value)
        {
            int u = ~value & 0xFF;
            int sign = u & 0x80, exponent = (u >> 4) & 0x07, mantissa = u & 0x0F;
            int pcm = (((mantissa << 3) + Bias) << exponent) - Bias;
            return (sign != 0 ? -pcm : pcm) / 32768f;
        }
    }
}

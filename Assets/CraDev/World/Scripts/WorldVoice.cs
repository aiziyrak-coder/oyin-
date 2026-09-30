using System;
using System.Collections;
using System.Collections.Generic;
using CraDev.MainMenu;
using CraDev.Online;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CraDev.World
{
    /// <summary>Opt-in microphone, session-scoped transport, and spatial playback inside 18 metres.</summary>
    public sealed class WorldVoice : MonoBehaviour
    {
        public const float Radius = 18;
        WorldNetwork network;
        WorldHud hud;
        WorldPlayerController player;
        bool micOn, speakerOn = true, stopped, receivedSamples;
        string session, captureSession, device;
        AudioClip microphone;
        int rate, position, frameSize, sequence, receiveRevision;
        long cursor = -1;
        float[] frame;
        float gateUntil, started;
        readonly List<float> pending = new List<float>();
        readonly Queue<WorldAudio> outbox = new Queue<WorldAudio>();
        readonly Dictionary<int, SpatialStream> streams = new Dictionary<int, SpatialStream>();
        readonly List<int> remove = new List<int>();
        public bool MicOn => micOn && !stopped && network && network.Connected && Application.isFocused;
        public bool SpeakerOn => speakerOn && !stopped;
        string noticeKey;
        public string Notice => noticeKey == null ? null : Loc.T(noticeKey);
        public bool IsSpeaking(int id) => SpeakerOn && streams.TryGetValue(id, out var stream) && stream.Audible;

        void Start()
        {
            network = GetComponent<WorldNetwork>(); hud = GetComponent<WorldHud>();
            player = FindFirstObjectByType<WorldPlayerController>();
            // Never inherit recording consent from a private lobby into the public world.
            micOn = false;
            StartCoroutine(SendLoop()); StartCoroutine(ReadLoop());
        }
        public void ToggleMic()
        {
            if (stopped || !network || !network.Connected) { noticeKey = "city.voice.connect"; return; }
            if (!speakerOn) { noticeKey = "city.voice.speaker"; return; }
            if (!micOn && VoicePreferences.Device == null) { noticeKey = "city.voice.missing"; return; }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-cradevShot") >= 0)
            { noticeKey = "city.voice.test"; return; }
            micOn = !micOn; noticeKey = null;
            if (!micOn) StopMic();
        }
        public void SetSpeaker(bool value)
        {
            receiveRevision++;
            speakerOn = value;
            if (!value) { micOn = false; StopMic(); ClearStreams(); }
            cursor = -1; noticeKey = null;
        }
        void Update()
        {
            if (stopped || !network) return;
            string current = network.Connected ? network.SessionId : null;
            if (session != current)
            {
                StopMic(); ClearStreams(); cursor = -1; receiveRevision++; session = current; micOn = false;
            }
            if ((!hud || !hud.SettingsOpen) && MicPressed()) ToggleMic();
            if ((!hud || !hud.SettingsOpen) && SpeakerPressed()) SetSpeaker(!SpeakerOn);
            bool capture = MicOn && SpeakerOn && (!VoicePreferences.PushToTalk || TalkHeld()) && (!hud || !hud.SettingsOpen);
            if (microphone && device != VoicePreferences.Device) StopMic();
            if (capture && !microphone) StartMic();
            if (!capture && microphone) StopMic();
            if (microphone) Capture();
            remove.Clear();
            foreach (var pair in streams)
            {
                var peer = network.Peer(pair.Key);
                if (!SpeakerOn || peer == null || !peer.micOn || !peer.speakerOn ||
                    (peer.Position-player.transform.position).sqrMagnitude > Radius*Radius ||
                    Time.realtimeSinceStartup-pair.Value.LastChunk > 2)
                    remove.Add(pair.Key);
                else pair.Value.Update(peer.Position + Vector3.up * 1.5f, VoicePreferences.Volume);
            }
            foreach (int id in remove) { streams[id].Dispose(); streams.Remove(id); }
        }
        static bool MicPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.M);
#else
            return false;
#endif
        }
        static bool SpeakerPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.N);
#else
            return false;
#endif
        }
        static bool TalkHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.vKey.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.V);
#else
            return false;
#endif
        }
        void StartMic()
        {
            device = VoicePreferences.Device;
            if (device == null) { micOn = false; noticeKey = "city.voice.missing"; return; }
            try
            {
                Microphone.GetDeviceCaps(device, out int min, out int max);
                rate = max > 0 ? Mathf.Clamp(16000, Mathf.Max(8000,min), Mathf.Min(48000,max)) : 16000;
                microphone = Microphone.Start(device,true,1,rate);
                if (!microphone) throw new InvalidOperationException();
                rate = microphone.frequency; frameSize = Mathf.Max(1,microphone.samples/50);
                frame = new float[frameSize*microphone.channels]; position = 0;
                captureSession = session; pending.Clear(); gateUntil = -1; started = Time.realtimeSinceStartup; receivedSamples = false;
            }
            catch (Exception) { micOn = false; StopMic(); noticeKey = "city.voice.error"; }
        }
        void StopMic()
        {
            pending.Clear(); outbox.Clear(); captureSession = null;
            if (microphone)
            {
                Microphone.End(device); Destroy(microphone); microphone = null;
            }
            device = null;
        }
        void Capture()
        {
            int at = Microphone.GetPosition(device);
            if (at > 0) receivedSamples = true;
            // Position zero is also a valid wrap of the one-second ring, not a device failure.
            if ((!receivedSamples && at <= 0 && Time.realtimeSinceStartup-started > 4) ||
                (receivedSamples && !Microphone.IsRecording(device)))
            { micOn = false; StopMic(); noticeKey = "city.voice.signal"; return; }
            if (at < 0) return;
            int available = (at-position+microphone.samples)%microphone.samples;
            while (available >= frameSize)
            {
                microphone.GetData(frame,position); position=(position+frameSize)%microphone.samples; available-=frameSize;
                float energy=0; foreach(float sample in frame)energy+=sample*sample;
                if (Mathf.Sqrt(energy/frame.Length) > .009f) gateUntil=Time.realtimeSinceStartup+.35f;
                if (Time.realtimeSinceStartup < gateUntil)
                    for(int i=0;i<frameSize;i++)
                    {
                        float sum=0; for(int c=0;c<microphone.channels;c++)sum+=frame[i*microphone.channels+c];
                        pending.Add(sum/microphone.channels);
                    }
                if (pending.Count >= rate*.2f || (pending.Count>0 && Time.realtimeSinceStartup>=gateUntil)) Flush();
            }
        }
        void Flush()
        {
            if (pending.Count == 0) return;
            var bytes=new byte[pending.Count]; for(int i=0;i<bytes.Length;i++)bytes[i]=MuLaw.Encode(pending[i]);
            pending.Clear();
            outbox.Enqueue(new WorldAudio { sessionId=captureSession,seq=++sequence,rate=rate,data=Convert.ToBase64String(bytes) });
            while(outbox.Count>3)outbox.Dequeue();
        }
        IEnumerator SendLoop()
        {
            while (!stopped)
            {
                if (outbox.Count==0 || !network || network.Api==null) { yield return null; continue; }
                var chunk=outbox.Dequeue();
                if (!MicOn || chunk.sessionId!=network.SessionId)continue;
                yield return network.Api.WorldSendVoice(PlayerProfile.Token,chunk,_=>{});
            }
        }
        IEnumerator ReadLoop()
        {
            while (!stopped)
            {
                if (SpeakerOn && network && network.Connected && network.Api!=null)
                {
                    string expected=network.SessionId; long since=cursor; int revision=receiveRevision;
                    ApiResult<WorldAudioBatch> result=default;
                    yield return network.Api.WorldReadVoice(PlayerProfile.Token,expected,since,r=>result=r);
                    if (SpeakerOn && network.Connected && expected==network.SessionId && revision==receiveRevision && result.Ok)
                    {
                        cursor=result.Data.cursor;
                        foreach(var chunk in result.Data.chunks??Array.Empty<WorldAudio>()) Play(chunk);
                    }
                }
                yield return new WaitForSecondsRealtime(.12f);
            }
        }
        void Play(WorldAudio chunk)
        {
            var peer=network.Peer(chunk.publicId);
            if (peer==null || !peer.micOn || !peer.speakerOn || !player ||
                (peer.Position-player.transform.position).sqrMagnitude>Radius*Radius || chunk.rate<8000 || chunk.rate>48000) return;
            byte[] bytes; try { bytes=Convert.FromBase64String(chunk.data); } catch(FormatException) { return; }
            if(bytes.Length>12000)return;
            if(!streams.TryGetValue(chunk.publicId,out var stream)||stream.Rate!=chunk.rate)
            {
                stream?.Dispose();stream=new SpatialStream(transform,chunk.publicId,chunk.rate);streams[chunk.publicId]=stream;
            }
            stream.Update(peer.Position+Vector3.up*1.5f,VoicePreferences.Volume);
            stream.Push(bytes,Time.realtimeSinceStartup);
        }
        public void Shutdown()
        {
            receiveRevision++;stopped=true;micOn=false;speakerOn=false;StopMic();ClearStreams();StopAllCoroutines();
        }
        void ClearStreams() { foreach(var stream in streams.Values)stream.Dispose();streams.Clear(); }
        void OnDisable() => Shutdown();
        void OnApplicationFocus(bool focus) { if(!focus){micOn=false;StopMic();} }

        sealed class SpatialStream
        {
            public readonly int Rate;
            public float LastChunk { get; private set; }
            readonly GameObject host;
            readonly AudioClip clip;
            readonly AudioSource source;
            readonly float[] ring;
            readonly object gate=new object();
            int read,count;bool playing;
            public bool Audible { get { lock(gate)return playing; } }
            public SpatialStream(Transform parent,int id,int rate)
            {
                Rate=rate;ring=new float[rate*2];host=new GameObject("WorldVoice_"+id);host.transform.SetParent(parent,false);
                source=host.AddComponent<AudioSource>();clip=AudioClip.Create("ProximityVoice",rate,1,rate,true,Read);
                source.clip=clip;source.loop=true;source.playOnAwake=false;source.spatialBlend=1;
                source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=1.5f;source.maxDistance=Radius;
                source.dopplerLevel=0;source.spread=30;source.Play();
            }
            public void Update(Vector3 position,float volume) { host.transform.position=position;source.volume=volume; }
            public void Push(byte[] bytes,float now)
            {
                LastChunk=now;
                lock(gate)
                {
                    foreach(byte value in bytes)
                    {
                        if(count==ring.Length){read=(read+1)%ring.Length;count--;}
                        ring[(read+count)%ring.Length]=MuLaw.Decode(value);count++;
                    }
                    int keep=(int)(Rate*.25f);
                    if(count>Rate*.7f){read=(read+count-keep)%ring.Length;count=keep;}
                }
            }
            void Read(float[] output)
            {
                lock(gate)
                {
                    if(!playing&&count>=Rate*.16f)playing=true;
                    for(int i=0;i<output.Length;i++)
                        if(playing&&count>0){output[i]=ring[read];read=(read+1)%ring.Length;count--;}
                        else {output[i]=0;playing=false;}
                }
            }
            public void Dispose() { UnityEngine.Object.Destroy(host);UnityEngine.Object.Destroy(clip); }
        }
    }
}

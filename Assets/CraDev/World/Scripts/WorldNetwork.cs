using System.Collections;
using System.Collections.Generic;
using CraDev.CharacterCreation;
using CraDev.Online;
using CraDev.Wardrobe;
using UnityEngine;

namespace CraDev.World
{
    /// <summary>Shared test-world presence. Bounded HTTP snapshots; remote transforms are smoothed locally.</summary>
    public sealed class WorldNetwork : MonoBehaviour
    {
        [SerializeField] WorldPlayerController player;
        [SerializeField] AvatarViewer avatarTemplate;
        [SerializeField] AvatarOption[] avatars;
        [SerializeField] Font font;
        readonly Dictionary<int, Remote> remotes = new Dictionary<int, Remote>();
        readonly Dictionary<int, WorldPeer> peers = new Dictionary<int, WorldPeer>();
        readonly List<int> remove = new List<int>();
        WorldVoice voice;
        float lastContact = -100, nextVisual;
        bool leaving;
        Coroutine loop;
        Transform remoteRoot;
        public GameApi Api { get; private set; }
        public string SessionId { get; private set; }
        public bool Connected => !leaving && !string.IsNullOrEmpty(SessionId) && Time.realtimeSinceStartup - lastContact < 3;
        string statusKey = "city.connecting";
        public string Status => Loc.T(statusKey);
        public int PingMs { get; private set; }
        public int PlayerCount => Connected ? peers.Count + 1 : 0;
        public int VisibleCount => remotes.Count;
        public WorldPeer Self { get; private set; }
        public double ServerNow => serverTime + (Time.realtimeSinceStartup - lastContact) * 1000.0;
        long serverTime;
        public WorldPeer Peer(int id) => Connected && peers.TryGetValue(id, out var peer) ? peer : null;

        void Start()
        {
            if (!player) player = FindFirstObjectByType<WorldPlayerController>();
            voice = GetComponent<WorldVoice>();
            Api = new GameApi(GameApi.DefaultServerUrl);
            remoteRoot = new GameObject("WorldPlayers").transform;
            loop = StartCoroutine(PresenceLoop());
        }

        IEnumerator PresenceLoop()
        {
            while (!leaving)
            {
                if (string.IsNullOrEmpty(PlayerProfile.Token)) { statusKey = "city.login"; yield break; }
                if (string.IsNullOrEmpty(SessionId))
                {
                    statusKey = "city.connecting";
                    ApiResult<WorldSnapshot> joined = default;
                    yield return Api.WorldJoin(PlayerProfile.Token, r => joined = r);
                    if (leaving) yield break;
                    if (!joined.Ok || joined.Data.self == null)
                    {
                        statusKey = joined.Status == 401 ? "city.refresh" :
                            joined.Data?.error == "world_full" ? "city.full" : "city.unavailable";
                        yield return new WaitForSecondsRealtime(4); continue;
                    }
                    SessionId = joined.Data.sessionId;
                    if (player) player.SetWorldSpawn(joined.Data.self.Position, joined.Data.self.yaw);
                    Apply(joined.Data);
                }
                float started = Time.realtimeSinceStartup;
                Vector3 position = player.transform.position;
                var state = new WorldStateRequest { sessionId = SessionId, x = position.x, y = position.y,
                    z = position.z, yaw = player.Yaw, crouching = player.IsCrouching,
                    movementRevision = Self?.movementRevision ?? 0,
                    micOn = voice && voice.MicOn, speakerOn = voice && voice.SpeakerOn };
                ApiResult<WorldSnapshot> result = default;
                yield return Api.WorldState(PlayerProfile.Token, state, r => result = r);
                if (leaving) yield break;
                if (result.Ok && result.Data.self != null)
                {
                    PingMs = Mathf.RoundToInt((Time.realtimeSinceStartup - started) * 1000);
                    Apply(result.Data);
                }
                else
                {
                    statusKey = "city.reconnecting";
                    if (result.Status == 409 || result.Status == 401 || result.Status == 400 || Time.realtimeSinceStartup - lastContact > 10)
                    {
                        SessionId = null; ClearPeers();
                        yield return new WaitForSecondsRealtime(2);
                    }
                }
                float remaining = .125f - (Time.realtimeSinceStartup - started);
                if (remaining > 0) yield return new WaitForSecondsRealtime(remaining);
            }
        }

        void Apply(WorldSnapshot snapshot)
        {
            lastContact = Time.realtimeSinceStartup;
            serverTime = snapshot.serverTime;
            statusKey = "city.shared";
            var oldSelf = Self; Self = snapshot.self;
            if (player && Self != null) {
                if (!string.IsNullOrEmpty(Self.activity)) player.ApplyActivityPose(Self.Position, Self.yaw, Self.pose);
                else {
                    player.ClearActivityPose();
                    if (snapshot.corrected || (oldSelf != null && oldSelf.movementRevision != Self.movementRevision))
                        player.CorrectPosition(Self.Position);
                }
            }
            peers.Clear();
            foreach (var peer in snapshot.players ?? System.Array.Empty<WorldPeer>())
                if (peer.publicId != PlayerProfile.PublicId) {
                    peers[peer.publicId] = peer;
                    if (remotes.TryGetValue(peer.publicId, out var remote)) remote.Push(peer, serverTime);
                }
        }

        void Update()
        {
            if (!Connected)
            {
                if (remotes.Count > 0) ClearPeers();
                return;
            }
            // Instantiate at most one detailed avatar per frame, and only inside a practical view radius.
            if (Time.unscaledTime >= nextVisual)
            {
                nextVisual = Time.unscaledTime + .06f;
                foreach (var peer in peers.Values)
                    if (!remotes.ContainsKey(peer.publicId) && Vector3.SqrMagnitude(peer.Position - player.transform.position) < 10000)
                    { Create(peer); break; }
            }
            remove.Clear();
            foreach (var pair in remotes)
            {
                if (!peers.TryGetValue(pair.Key, out var peer) || (peer.Position-player.transform.position).sqrMagnitude > 12100)
                { remove.Add(pair.Key); continue; }
                var remote = pair.Value;
                if (remote.avatarId != peer.avatarId || remote.outfit != peer.outfit)
                { remove.Add(pair.Key); continue; }
                // Render a short buffered timeline instead of speeding up/slowing down at each HTTP packet.
                remote.Sample(ServerNow - 180, out var position, out float heading, out var velocity);
                remote.root.position = position;
                remote.root.rotation = Quaternion.Euler(0, heading, 0);
                remote.motion.SetMotion(velocity.magnitude, peer.crouching, remote.root.InverseTransformDirection(velocity));
                remote.motion.SetActivity(peer.activity, peer.pose, peer.action,
                    Mathf.Max(0, (float)((ServerNow - peer.actionAt) / 1000.0)));
                remote.label.transform.position = remote.root.position + Vector3.up * (peer.pose == "sit" ? 1.45f : peer.crouching ? 1.52f : 2.05f);
                if (player.ViewCamera)
                {
                    remote.label.transform.rotation = player.ViewCamera.transform.rotation;
                    float distance = Vector3.Distance(player.ViewCamera.transform.position, remote.label.transform.position);
                    // Keep nearby names modest and distant ones legible, without giant labels at arm's length.
                    // Scale its transform, not the TextMesh geometry/atlas on every network frame.
                    remote.label.transform.localScale = Vector3.one * Mathf.Clamp(distance / 2f, .6f, 8f);
                }
                remote.label.gameObject.SetActive((peer.Position-player.transform.position).sqrMagnitude < 625);
                string caption = peer.nickname + (voice && voice.IsSpeaking(peer.publicId) ? "  •" : "");
                if (remote.label.text != caption) remote.label.text = caption;
                Color captionColor = voice && voice.IsSpeaking(peer.publicId) ? new Color(.42f,1,.69f) : Color.white;
                if (remote.label.color != captionColor) remote.label.color = captionColor;
            }
            foreach (int id in remove) { Destroy(remotes[id].root.gameObject); remotes.Remove(id); }
        }

        void Create(WorldPeer peer)
        {
            if (!avatarTemplate || avatars == null || avatars.Length == 0) return;
            var option = System.Array.Find(avatars, a => a.id == peer.avatarId) ?? avatars[0];
            var host = new GameObject("Player_" + peer.publicId).transform;
            host.SetParent(remoteRoot, false); host.position = peer.Position; host.rotation = Quaternion.Euler(0,peer.yaw,0);
            var viewer = avatarTemplate.Replica(host, option, 512);
            viewer.transform.localPosition = Vector3.zero;
            viewer.SetOutfit(Outfit.FromJson(peer.outfit));
            // Gameplay metres, independent of lobby portrait framing or avatar source-file scale.
            if (viewer.BodyHeight > .1f) viewer.transform.localScale = Vector3.one * (1.8f / viewer.BodyHeight);
            foreach (var collider in host.GetComponentsInChildren<Collider>()) Destroy(collider);
            var motion = viewer.gameObject.AddComponent<WorldAvatarMotion>(); motion.Configure(viewer.CurrentModel);
            var label = new GameObject("Nickname", typeof(TextMesh)).GetComponent<TextMesh>();
            label.transform.SetParent(host, false); label.gameObject.layer = 9;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
            label.fontSize = 48; label.characterSize = .009f; label.richText = false;
            if (font) { label.font = font; label.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
            remotes[peer.publicId] = new Remote { root = host, label = label, motion = motion, avatarId = peer.avatarId, outfit = peer.outfit };
            remotes[peer.publicId].Push(peer, serverTime);
        }

        public IEnumerator Leave()
        {
            if (leaving) yield break;
            leaving = true; string session = SessionId; SessionId = null;
            if (voice) voice.Shutdown();
            if (loop != null) StopCoroutine(loop);
            ClearPeers();
            if (!string.IsNullOrEmpty(session) && Api != null)
                yield return Api.WorldLeave(PlayerProfile.Token, session, _ => { });
        }
        void ClearPeers()
        {
            foreach (var remote in remotes.Values) if (remote.root) Destroy(remote.root.gameObject);
            remotes.Clear(); peers.Clear();
            Self = null; if (player) player.ClearActivityPose();
        }
        void OnDestroy() { ClearPeers(); if (remoteRoot) Destroy(remoteRoot.gameObject); }
        sealed class Remote
        {
            public Transform root; public TextMesh label; public WorldAvatarMotion motion; public string avatarId, outfit;
            readonly List<SamplePoint> samples = new List<SamplePoint>(10);
            string activity; int revision;
            public void Push(WorldPeer peer, long time)
            {
                if (activity != peer.activity || revision != peer.movementRevision ||
                    (samples.Count > 0 && (peer.Position - samples[samples.Count-1].position).sqrMagnitude > 16)) samples.Clear();
                activity = peer.activity; revision = peer.movementRevision;
                if (samples.Count > 0 && samples[samples.Count-1].time >= time) return;
                samples.Add(new SamplePoint { position = peer.Position, yaw = peer.yaw, time = time });
                if (samples.Count > 10) samples.RemoveAt(0);
            }
            public void Sample(double time, out Vector3 position, out float heading, out Vector3 velocity)
            {
                velocity = Vector3.zero;
                var newest = samples[samples.Count-1]; position = newest.position; heading = newest.yaw;
                if (!string.IsNullOrEmpty(activity) || samples.Count < 2) return;
                for (int i=1; i<samples.Count; i++) {
                    var a = samples[i-1]; var b = samples[i];
                    if (time > b.time && i < samples.Count-1) continue;
                    float span = Mathf.Max(1, b.time-a.time);
                    float t = Mathf.Clamp((float)(time-a.time)/span, 0, 1);
                    position = Vector3.Lerp(a.position,b.position,t); heading = Mathf.LerpAngle(a.yaw,b.yaw,t);
                    velocity = (b.position-a.position)/(span*.001f); velocity.y=0;
                    if (time>b.time) {
                        // Brief jitter tolerance only; never keep walking through geometry on a lost connection.
                        float extra = Mathf.Clamp((float)(time-b.time)*.001f,0,.1f);
                        position += Vector3.ClampMagnitude(velocity,6.5f)*extra;
                        if(time-b.time>150)velocity=Vector3.zero;
                    }
                    return;
                }
            }
            struct SamplePoint { public Vector3 position; public float yaw; public long time; }
        }
    }
}

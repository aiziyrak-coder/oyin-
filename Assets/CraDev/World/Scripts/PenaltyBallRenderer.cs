using CraDev.Online;
using UnityEngine;
using UnityEngine.Rendering;

namespace CraDev.World
{
    /// <summary>One shared visual ball. Flight is sampled analytically from server time, never client physics scores.</summary>
    public sealed class PenaltyBallRenderer : System.IDisposable
    {
        readonly GameObject ball;
        readonly Material material;
        readonly Texture2D texture;
        readonly Vector3 start = new Vector3(64, .11f, 68);
        public Vector3 Position => ball ? ball.transform.position : start;
        public PenaltyBallRenderer()
        {
            ball = GameObject.CreatePrimitive(PrimitiveType.Sphere); ball.name = "PenaltyBall_Authoritative";
            ball.transform.localScale = Vector3.one * .22f; ball.transform.position = start;
            Object.Destroy(ball.GetComponent<Collider>());
            material = new Material(Shader.Find("Standard")); material.name = "StitchedMatchBall";
            material.SetFloat("_Glossiness", .32f); material.SetFloat("_Metallic", 0f);
            texture = MakeBallTexture(); material.mainTexture = texture;
            var renderer = ball.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
        }
        public void Draw(PenaltySnapshot state, double serverNow)
        {
            if (!ball) return;
            var shot = state?.shot;
            if (shot == null || state.phase == "waiting" || state.phase == "aiming")
            { ball.transform.position = start; ball.transform.rotation = Quaternion.identity; return; }
            float elapsed = Mathf.Max(0, (float)((serverNow - shot.startedAt) / 1000));
            float duration = shot.durationMs / 1000f;
            Vector3 target = new Vector3(shot.targetX, shot.targetY, shot.targetZ);
            Vector3 velocity = (target - start) / Mathf.Max(.1f, duration);
            velocity.y += 4.905f * duration;
            float time = Mathf.Min(elapsed, duration);
            Vector3 position = start + velocity * time + Vector3.down * 4.905f * time * time;
            if (elapsed > duration)
            {
                float after = Mathf.Min(elapsed - duration, 2.5f);
                bool saved = shot.outcome == "save";
                // A net contact decelerates the ball; a save sends it back into the area. This
                // response is visual only and cannot change the authoritative result.
                Vector3 rebound = new Vector3((target.x - 64) * .25f, saved ? 2.2f : .9f, saved ? -3.1f : .8f);
                position = target + rebound * (1 - Mathf.Exp(-2 * after));
                position.y = Mathf.Max(.11f, target.y + rebound.y * after - 4.905f * after * after);
                if (position.y <= .111f) position.y += Mathf.Abs(Mathf.Sin(after * 12)) * .07f * Mathf.Exp(-3 * after);
            }
            ball.transform.position = position;
            ball.transform.rotation = Quaternion.Euler(elapsed * (420 + shot.power * 400), 0, elapsed * (shot.targetX - 64) * -50);
        }
        static Texture2D MakeBallTexture()
        {
            const int width = 512, height = 256;
            var pixels = new Color32[width * height];
            float golden = (1 + Mathf.Sqrt(5)) / 2;
            var vertices = new[] { new Vector3(-1,golden,0),new Vector3(1,golden,0),new Vector3(-1,-golden,0),new Vector3(1,-golden,0),
                new Vector3(0,-1,golden),new Vector3(0,1,golden),new Vector3(0,-1,-golden),new Vector3(0,1,-golden),
                new Vector3(golden,0,-1),new Vector3(golden,0,1),new Vector3(-golden,0,-1),new Vector3(-golden,0,1) };
            for (int i = 0; i < vertices.Length; i++) vertices[i].Normalize();
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                float latitude = (y + .5f) / height * Mathf.PI, longitude = (x + .5f) / width * Mathf.PI * 2;
                var normal = new Vector3(Mathf.Sin(latitude) * Mathf.Cos(longitude), Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(longitude));
                float nearest = -1, next = -1;
                foreach (var vertex in vertices) { float d = Vector3.Dot(vertex, normal); if (d > nearest) { next = nearest; nearest = d; } else next = Mathf.Max(next, d); }
                bool darkPanel = nearest > .938f, seam = Mathf.Abs(nearest - .938f) < .006f || nearest - next < .007f;
                byte grain = (byte)((x * 17 + y * 31) % 5);
                byte value = darkPanel ? (byte)(22 + grain) : seam ? (byte)115 : (byte)(236 + grain);
                pixels[y * width + x] = new Color32(value, value, value, 255);
            }
            var result = new Texture2D(width, height, TextureFormat.RGBA32, true); result.name = "ProceduralStitchedFootball";
            result.SetPixels32(pixels); result.Apply(true, true); result.anisoLevel = 4; return result;
        }
        public void Dispose() { if (ball) Object.Destroy(ball); if (material) Object.Destroy(material); if (texture) Object.Destroy(texture); }
    }
}

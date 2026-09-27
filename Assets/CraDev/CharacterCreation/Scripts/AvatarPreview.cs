using System.Collections.Generic;
using UnityEngine;

namespace CraDev.CharacterCreation
{
    /// <summary>
    /// Avatar yaratish ekranidagi 3D manekenli ko'rinish (haqiqiy 3D modellar tayyor bo'lguncha).
    /// Qismlar oddiy shakllardan (shar, kapsula, silindr) yig'iladi. Jins almashganda har bir qism
    /// erkak va ayol o'lchamlari orasida silliq o'zgaradi. Avatar sekin aylanadi, sichqoncha bilan buriladi.
    /// </summary>
    public class AvatarPreview : MonoBehaviour
    {
        [SerializeField] Material skinMaterial;
        [SerializeField] Material hairMaterial;
        [Tooltip("Hech kim tegmaganda aylanish tezligi, gradus/soniya.")]
        [SerializeField] float idleSpinSpeed = 14f;
        [Tooltip("Sichqoncha 1 birlik surilganda necha gradus buriladi.")]
        [SerializeField] float dragSensitivity = 0.45f;
        [SerializeField] float morphDuration = 0.45f;
        [SerializeField] float startYaw = 20f;

        // Har bir qism: [x, y, oldinga, eni, bo'yi, qalinligi, yon tomonga og'ish (gradus)], metrda.
        // Juft qismlar (qo'l, oyoq) o'ng tomon uchun yozilgan, chap tomoni ko'zgu aksida quriladi.
        // Brauzerdagi ko'rish sahifasida ham aynan shu qiymatlar ishlatiladi.
        struct PartSpec
        {
            public PrimitiveType Shape;
            public bool Hair;
            public bool Paired;
            public float[] Male;
            public float[] Female;

            public PartSpec(PrimitiveType shape, bool hair, bool paired, float[] male, float[] female)
            {
                Shape = shape; Hair = hair; Paired = paired; Male = male; Female = female;
            }
        }

        static readonly PartSpec[] Parts =
        {
            new PartSpec(PrimitiveType.Sphere, false, false, new[] { 0f, 1.66f, 0f, 0.20f, 0.24f, 0.22f, 0f }, new[] { 0f, 1.59f, 0f, 0.185f, 0.225f, 0.205f, 0f }),     // bosh
            new PartSpec(PrimitiveType.Cylinder, false, false, new[] { 0f, 1.50f, 0f, 0.09f, 0.07f, 0.09f, 0f }, new[] { 0f, 1.435f, 0f, 0.075f, 0.07f, 0.075f, 0f }),  // bo'yin
            new PartSpec(PrimitiveType.Sphere, false, false, new[] { 0f, 1.27f, 0f, 0.42f, 0.50f, 0.25f, 0f }, new[] { 0f, 1.215f, 0f, 0.34f, 0.46f, 0.225f, 0f }),     // ko'krak
            new PartSpec(PrimitiveType.Sphere, false, false, new[] { 0f, 1.07f, 0f, 0.30f, 0.30f, 0.20f, 0f }, new[] { 0f, 1.03f, 0f, 0.25f, 0.28f, 0.18f, 0f }),       // bel
            new PartSpec(PrimitiveType.Sphere, false, false, new[] { 0f, 0.94f, 0f, 0.34f, 0.30f, 0.22f, 0f }, new[] { 0f, 0.915f, 0f, 0.37f, 0.32f, 0.23f, 0f }),      // chanoq
            new PartSpec(PrimitiveType.Sphere, false, true, new[] { 0.215f, 1.43f, 0f, 0.12f, 0.12f, 0.12f, 0f }, new[] { 0.17f, 1.37f, 0f, 0.10f, 0.10f, 0.10f, 0f }), // yelka
            new PartSpec(PrimitiveType.Capsule, false, true, new[] { 0.245f, 1.22f, 0f, 0.095f, 0.18f, 0.095f, 7f }, new[] { 0.195f, 1.17f, 0f, 0.08f, 0.17f, 0.08f, 8f }),          // yelka-tirsak
            new PartSpec(PrimitiveType.Capsule, false, true, new[] { 0.275f, 0.92f, 0.01f, 0.08f, 0.16f, 0.08f, 4f }, new[] { 0.225f, 0.885f, 0.01f, 0.068f, 0.155f, 0.068f, 5f }), // bilak
            new PartSpec(PrimitiveType.Sphere, false, true, new[] { 0.29f, 0.73f, 0.02f, 0.07f, 0.11f, 0.045f, 0f }, new[] { 0.24f, 0.705f, 0.02f, 0.06f, 0.10f, 0.04f, 0f }),       // kaft
            new PartSpec(PrimitiveType.Capsule, false, true, new[] { 0.095f, 0.64f, 0f, 0.15f, 0.24f, 0.15f, 0f }, new[] { 0.095f, 0.62f, 0f, 0.145f, 0.23f, 0.145f, 0f }),          // son
            new PartSpec(PrimitiveType.Capsule, false, true, new[] { 0.095f, 0.27f, 0f, 0.11f, 0.21f, 0.11f, 0f }, new[] { 0.09f, 0.26f, 0f, 0.095f, 0.20f, 0.095f, 0f }),           // boldir
            new PartSpec(PrimitiveType.Sphere, false, true, new[] { 0.095f, 0.04f, 0.05f, 0.10f, 0.07f, 0.24f, 0f }, new[] { 0.09f, 0.04f, 0.045f, 0.085f, 0.065f, 0.21f, 0f }),     // oyoq panjasi
            new PartSpec(PrimitiveType.Sphere, true, false, new[] { 0f, 1.725f, -0.012f, 0.21f, 0.14f, 0.23f, 0f }, new[] { 0f, 1.655f, -0.015f, 0.20f, 0.16f, 0.22f, 0f }),       // soch
            new PartSpec(PrimitiveType.Capsule, true, false, new[] { 0f, 1.55f, -0.08f, 0f, 0f, 0f, 0f }, new[] { 0f, 1.49f, -0.07f, 0.17f, 0.16f, 0.08f, 0f }),                    // uzun soch (faqat ayolda)
        };

        class Part
        {
            public Transform Transform;
            public Renderer Renderer;
            public float Side;
            public PartSpec Spec;
        }

        readonly List<Part> parts = new List<Part>();
        readonly float[] values = new float[7];
        Transform body;
        float blend;      // 0 = erkak, 1 = ayol
        float blendFrom;
        float blendTo;
        float morphTime = -1f;
        float yaw;
        float idleResumeTime;
        bool dragging;

        void Awake()
        {
            body = new GameObject("Mannequin").transform;
            body.SetParent(transform, false);
            foreach (var spec in Parts)
            {
                foreach (float side in spec.Paired ? new[] { -1f, 1f } : new[] { 1f })
                {
                    var go = GameObject.CreatePrimitive(spec.Shape);
                    Destroy(go.GetComponent<Collider>());
                    go.transform.SetParent(body, false);
                    var renderer = go.GetComponent<Renderer>();
                    renderer.sharedMaterial = spec.Hair ? hairMaterial : skinMaterial;
                    parts.Add(new Part { Transform = go.transform, Renderer = renderer, Side = side, Spec = spec });
                }
            }
            yaw = startYaw;
            ApplyBlend(0f);
        }

        /// <summary>Avatarni erkak yoki ayol ko'rinishiga silliq o'tkazadi.</summary>
        public void SetFemale(bool female, bool instant = false)
        {
            blendFrom = blend;
            blendTo = female ? 1f : 0f;
            morphTime = instant ? morphDuration : 0f;
        }

        public void BeginDrag() => dragging = true;

        public void Drag(float deltaUnits) => yaw -= deltaUnits * dragSensitivity;

        public void EndDrag()
        {
            dragging = false;
            idleResumeTime = Time.unscaledTime + 2.5f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (!dragging && Time.unscaledTime > idleResumeTime)
                yaw -= idleSpinSpeed * dt;

            if (morphTime >= 0f)
            {
                morphTime += dt;
                float p = Mathf.Clamp01(morphTime / morphDuration);
                ApplyBlend(Mathf.Lerp(blendFrom, blendTo, Ease.InOutCubic(p)));
                if (p >= 1f)
                    morphTime = -1f;
            }

            body.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        void ApplyBlend(float b)
        {
            blend = b;
            foreach (var part in parts)
            {
                float[] m = part.Spec.Male, f = part.Spec.Female;
                var v = values;
                for (int i = 0; i < v.Length; i++)
                    v[i] = Mathf.Lerp(m[i], f[i], b);
                // Kamera -Z tomonda turadi, shuning uchun avatarning "oldi" -Z ga qaragan
                part.Transform.localPosition = new Vector3(v[0] * part.Side, v[1], -v[2]);
                part.Transform.localScale = new Vector3(Mathf.Max(v[3], 0.0001f), Mathf.Max(v[4], 0.0001f), Mathf.Max(v[5], 0.0001f));
                part.Transform.localRotation = Quaternion.Euler(0f, 0f, part.Side * v[6]);
                part.Renderer.enabled = v[3] > 0.001f;
            }
        }
    }
}

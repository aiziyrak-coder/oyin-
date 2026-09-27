using UnityEngine;

namespace CraDev.World
{
    /// <summary>
    /// Maydonda yuruvchi odam: nuqtalar bo'ylab aylana yo'lda yuradi (yurish animatsiyasi joyida o'ynaydi,
    /// harakatni shu skript beradi). Nuqtalar orasida burilish silliq.
    /// </summary>
    public class Walker : MonoBehaviour
    {
        [SerializeField] Vector3[] path;
        [SerializeField] float speed = 1.25f;
        [Tooltip("Boshlang'ich joy (0..1 yo'l bo'ylab): odamlar bir joyda to'planib qolmasin.")]
        [SerializeField, Range(0f, 1f)] float start;

        float[] lengths;
        float total;
        float travelled;

        void Awake()
        {
            if (path == null || path.Length < 2)
            {
                enabled = false;
                return;
            }
            lengths = new float[path.Length];
            for (int i = 0; i < path.Length; i++)
            {
                lengths[i] = Vector3.Distance(path[i], path[(i + 1) % path.Length]);
                total += lengths[i];
            }
            travelled = start * total;
            // Animatsiya ham har xil fazadan boshlansin
            var animator = GetComponentInChildren<Animator>();
            if (animator != null)
                animator.Play(0, 0, start);
            Place(0f);
        }

        void Update() => Place(Time.deltaTime);

        void Place(float dt)
        {
            travelled = Mathf.Repeat(travelled + speed * dt, total);
            float d = travelled;
            int i = 0;
            while (d > lengths[i])
            {
                d -= lengths[i];
                i = (i + 1) % path.Length;
            }
            var a = path[i];
            var b = path[(i + 1) % path.Length];
            var position = Vector3.Lerp(a, b, d / Mathf.Max(lengths[i], 0.001f));
            var direction = (b - a).normalized;
            transform.position = position;
            if (direction.sqrMagnitude > 0f)
            {
                var target = Quaternion.LookRotation(direction);
                transform.rotation = dt > 0f ? Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-5f * dt)) : target;
            }
        }
    }
}

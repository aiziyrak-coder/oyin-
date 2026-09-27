using UnityEngine;

namespace CraDev.World
{
    /// <summary>Osmonda uchib yuruvchi transport: ellips bo'ylab aylanadi, burilishda biroz qiyshayadi, yengil tebranadi.</summary>
    public class Flyer : MonoBehaviour
    {
        [SerializeField] Vector3 center;
        [SerializeField] Vector2 radius = new Vector2(80f, 40f);
        [Tooltip("Gradus/soniya; manfiy - teskari yo'nalish.")]
        [SerializeField] float speed = 6f;
        [SerializeField] float phase;
        [SerializeField] float bob = 0.6f;

        void Update()
        {
            float a = (phase + Time.time * speed) * Mathf.Deg2Rad;
            var p = Point(a);
            var next = Point(a + Mathf.Sign(speed) * 0.02f);
            transform.position = p + Vector3.up * Mathf.Sin(Time.time * 0.8f + phase) * bob;
            var forward = (next - p).normalized;
            if (forward.sqrMagnitude > 0f)
            {
                float bank = -Mathf.Sign(speed) * 12f;
                transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 0f, bank);
            }
        }

        Vector3 Point(float a) => center + new Vector3(Mathf.Cos(a) * radius.x, 0f, Mathf.Sin(a) * radius.y);
    }
}

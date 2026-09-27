using UnityEngine;

namespace CraDev.World
{
    /// <summary>
    /// Sekin aylanuvchi obyekt (masalan, charxpalak). keepChildrenUpright: bolalar (kabinalar) aylanmaydi,
    /// doim tik osilib turadi.
    /// </summary>
    public class Rotator : MonoBehaviour
    {
        [SerializeField] Vector3 axis = Vector3.forward;
        [Tooltip("Gradus/soniya.")]
        [SerializeField] float speed = 4f;
        [SerializeField] Transform[] keepUpright;

        void Update()
        {
            transform.Rotate(axis, speed * Time.deltaTime, Space.Self);
            foreach (var child in keepUpright)
                child.rotation = Quaternion.LookRotation(transform.parent != null ? transform.parent.forward : Vector3.forward, Vector3.up);
        }
    }
}

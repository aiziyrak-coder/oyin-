using UnityEngine;

namespace CraDev.World
{
    /// <summary>
    /// Sahna ochilganda atrof yorug'ligini osmondan hisoblaydi va aks ettirish probini yangilaydi:
    /// shisha binolar osmon va atrofdagi binolarni aks ettiradi. (Oldindan "bake" qilish shart emas.)
    /// </summary>
    public class WorldLighting : MonoBehaviour
    {
        [SerializeField] ReflectionProbe probe;

        void Start()
        {
            DynamicGI.UpdateEnvironment();
            if (probe != null)
                probe.RenderProbe();
        }
    }
}

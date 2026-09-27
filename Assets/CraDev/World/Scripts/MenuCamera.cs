using UnityEngine;

namespace CraDev.World
{
    /// <summary>
    /// Bosh menyu kamerasi: qahramonni ko'rsatadi va sezilar-sezilmas "nafas oladi" (kinodagidek yengil harakat).
    /// Zona tanlanganda o'sha binoga silliq buriladi va yaqinlashadi, keyin qahramonga qaytadi.
    /// </summary>
    public class MenuCamera : MonoBehaviour
    {
        [SerializeField] Vector3 homePosition;
        [SerializeField] Vector3 homeTarget;
        [SerializeField] float sway = 0.06f;

        Vector3 position, target;
        Vector3 goalPosition, goalTarget;

        void Awake()
        {
            position = goalPosition = homePosition;
            target = goalTarget = homeTarget;
        }

        /// <summary>Binoga qarash: kamera qahramon yonidan o'sha tomonga buriladi.</summary>
        public void Focus(Vector3 building)
        {
            var toBuilding = building - homePosition;
            toBuilding.y = 0f;
            // Kamera biroz orqaga va yuqoriga chiqib, binoga qaraydi
            goalPosition = homePosition - toBuilding.normalized * 1.5f + Vector3.up * 1.2f;
            goalTarget = building;
        }

        public void Home()
        {
            goalPosition = homePosition;
            goalTarget = homeTarget;
        }

        void LateUpdate()
        {
            float k = 1f - Mathf.Exp(-2.2f * Time.unscaledDeltaTime);
            position = Vector3.Lerp(position, goalPosition, k);
            target = Vector3.Lerp(target, goalTarget, k);
            float t = Time.unscaledTime;
            var drift = new Vector3(Mathf.Sin(t * 0.21f), Mathf.Sin(t * 0.17f + 1.3f) * 0.5f, Mathf.Sin(t * 0.13f + 2.1f) * 0.6f) * sway;
            transform.position = position + drift;
            transform.rotation = Quaternion.LookRotation(target - transform.position);
        }
    }
}

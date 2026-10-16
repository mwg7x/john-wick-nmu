using LastStep.Core;
using UnityEngine;

namespace LastStep.Weapons
{
    [RequireComponent(typeof(Collider))]
    public sealed class HitDetector : MonoBehaviour
    {
        [SerializeField] private Health owner;
        [SerializeField] private HitZone zone = HitZone.Chest;
        public Health Owner => owner;
        public HitZone Zone => zone;

#if UNITY_EDITOR
        public void Configure(Health health, HitZone hitZone) { owner = health; zone = hitZone; }
#endif
    }
}

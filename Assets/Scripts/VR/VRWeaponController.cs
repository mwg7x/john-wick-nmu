using LastStep.Core;
using LastStep.Weapons;
using UnityEngine;

namespace LastStep.VR
{
    public sealed class VRWeaponController : MonoBehaviour
    {
        [SerializeField] private VRInputController input;
        [SerializeField] private WeaponController weapon;
        [SerializeField] private DuelManager duel;
        [SerializeField] private Transform pistol;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;
        [SerializeField] private HapticController haptics;
        private bool held = true;
        private void OnEnable()
        {
            if (input != null) { input.FirePressed += Fire; input.GripChanged += OnGrip; }
        }
        private void OnDisable()
        {
            if (input != null) { input.FirePressed -= Fire; input.GripChanged -= OnGrip; }
        }
        private void OnGrip(bool gripping) { held = gripping; }
        private void LateUpdate()
        {
            if (pistol == null || input == null || !held) return;
            var hand = input.DominantNode == UnityEngine.XR.XRNode.LeftHand ? leftHand : rightHand;
            if (hand != null) pistol.SetPositionAndRotation(hand.position + hand.forward * 0.1f, hand.rotation);
        }
        private void Fire()
        {
            if (!held || weapon == null || pistol == null || duel == null || !duel.IsFireAllowed) return;
            if (weapon.TryFire(pistol.position + pistol.forward * 0.18f, pistol.forward)) haptics?.Pulse(0.65f, 0.12f);
        }
    }
}

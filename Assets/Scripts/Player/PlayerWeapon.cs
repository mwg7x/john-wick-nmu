using LastStep.Core;
using LastStep.Weapons;
using UnityEngine;

namespace LastStep.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerWeapon : MonoBehaviour
    {
        [SerializeField] private PCInputController input;
        [SerializeField] private ThirdPersonCamera aimCamera;
        [SerializeField] private WeaponController weapon;
        [SerializeField] private DuelManager duel;
        private void OnEnable() { if (input != null) input.FirePressed += Fire; }
        private void OnDisable() { if (input != null) input.FirePressed -= Fire; }
        private void Fire()
        {
            if (aimCamera == null || weapon == null || weapon.Data == null || duel == null || !duel.IsFireAllowed) return;
            Vector3 direction = aimCamera.AimDirection;
            if (weapon.Data != null && weapon.Data.AccuracySpreadDegrees > 0f)
            {
                var movement = GetComponent<PlayerMovement>();
                float multiplier = movement != null && movement.IsSprinting ? 3f : 1f;
                Vector2 spread = Random.insideUnitCircle * weapon.Data.AccuracySpreadDegrees * multiplier;
                direction = Quaternion.Euler(spread.y, spread.x, 0f) * direction;
            }
            // Resolve the crosshair in camera space, then fire from the physical barrel.
            // The weapon ray still hits nearby cover, so a camera peeking past a wall
            // cannot shoot through it.
            Vector3 cameraOrigin = aimCamera.transform.position;
            Vector3 target = cameraOrigin + direction * weapon.Data.Range;
            var hits = Physics.RaycastAll(cameraOrigin, direction, weapon.Data.Range,
                weapon.Data.HitMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            var ownHealth = GetComponent<Health>();
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<Health>() == ownHealth) continue;
                target = hit.point;
                break;
            }
            Vector3 muzzle = weapon.Muzzle != null ? weapon.Muzzle.position : transform.position + Vector3.up * 1.3f;
            Vector3 barrelDirection = target - muzzle;
            if (barrelDirection.sqrMagnitude > .001f) weapon.TryFire(muzzle, barrelDirection.normalized);
        }
    }
}

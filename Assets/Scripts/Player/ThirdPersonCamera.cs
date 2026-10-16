using LastStep.Core;
using LastStep.Weapons;
using UnityEngine;

namespace LastStep.Player
{
    [DisallowMultipleComponent]
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform enemy;
        [SerializeField] private PCInputController input;
        [SerializeField] private DuelManager duel;
        [SerializeField] private Vector3 shoulderOffset = new Vector3(1.2f, 2.0f, -2.45f);
        [SerializeField, Range(0.1f, 1f)] private float aimedFovFactor = 0.78f;
        [SerializeField, Range(0.01f, 1f)] private float lookSensitivity = 0.12f;
        private Vector2 aimOffset;
        private float baseFov;
        private Camera cameraComponent;
        private WeaponController playerGun;
        private PlayerMovement movement;
        private float recoil;
        private float shoulderSign = 1f;
        private float shoulderBlend = 1f;
        private float injuryShake;
        private Health playerHealth;
        public Vector3 AimDirection => transform.forward;

        private void Awake()
        {
            cameraComponent = GetComponent<Camera>();
            if (cameraComponent != null) baseFov = cameraComponent.fieldOfView;
            if (player != null) playerGun = player.GetComponent<WeaponController>();
            if (player != null) movement = player.GetComponent<PlayerMovement>();
            if (player != null) playerHealth = player.GetComponent<Health>();
        }
        private void OnEnable() { if (playerGun != null) playerGun.ShotFired += OnShot; if (playerHealth != null) playerHealth.Changed += OnHealth; }
        private void OnDisable() { if (playerGun != null) playerGun.ShotFired -= OnShot; if (playerHealth != null) playerHealth.Changed -= OnHealth; }
        private int previousHealth = -1;
        private void OnHealth(Health h)
        {
            if (previousHealth >= 0 && h.Current < previousHealth)
                injuryShake = Mathf.Min(.11f, injuryShake + .025f + (previousHealth - h.Current) / (float)h.Maximum * .12f);
            previousHealth = h.Current;
        }
        private void OnShot(bool hit, Vector3 point)
        { recoil = playerGun != null && playerGun.Data != null ? playerGun.Data.RecoilDegrees : 1f; }
        private void LateUpdate()
        {
            if (player == null || enemy == null || input == null || cameraComponent == null) return;
            if (duel != null && duel.CurrentState == GameState.Paused) return;
            if (duel != null && duel.CurrentState == GameState.Finisher)
            {
                bool enemyDefeated = enemy.GetComponent<Health>()?.IsAlive == false;
                Transform target = enemyDefeated ? enemy : player;
                Vector3 axis = (enemy.position - player.position).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, axis).normalized;
                Vector3 focus = target.position + Vector3.up * 1.3f;
                Vector3 cinematic = focus - axis * (enemyDefeated ? 2.8f : -2.8f) + side * 1.4f + Vector3.up * .55f;
                float blend = 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime);
                transform.position = Vector3.Lerp(transform.position, cinematic, blend);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(focus - transform.position), blend);
                cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, 36f, blend);
                return;
            }
            Vector2 look = input.Look;
            if (input.ShoulderSwitchPressed) shoulderSign = -shoulderSign;
            shoulderBlend = Mathf.Lerp(shoulderBlend, shoulderSign, 1f - Mathf.Exp(-9f * Time.deltaTime));
            aimOffset.x = Mathf.Clamp(aimOffset.x + look.x * lookSensitivity, -18f, 18f);
            aimOffset.y = Mathf.Clamp(aimOffset.y - look.y * lookSensitivity, -13f, 13f);
            Vector3 forward = enemy.position - player.position;
            forward.y = 0f;
            forward = forward.sqrMagnitude > .001f ? forward.normalized : player.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            float lean = movement != null ? movement.Lean : 0f;
            Vector3 desired = player.position + right * (shoulderOffset.x * shoulderBlend + lean * .42f) + Vector3.up *
                (shoulderOffset.y + recoil * .055f) + forward * shoulderOffset.z;
            Vector3 pivot = player.position + Vector3.up * 1.55f;
            Vector3 travel = desired - pivot;
            float clearance = travel.magnitude;
            foreach (var wall in Physics.SphereCastAll(pivot, .16f, travel.normalized, travel.magnitude,
                ~0, QueryTriggerInteraction.Ignore))
            {
                if (wall.collider.transform.IsChildOf(player)) continue;
                clearance = Mathf.Min(clearance, Mathf.Max(0f, wall.distance - .05f));
            }
            bool obstructed = clearance < travel.magnitude;
            desired = pivot + travel.normalized * clearance;
            transform.position = obstructed ? desired : Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-11f * Time.deltaTime));
            if (injuryShake > .001f)
            {
                transform.position += right * (Mathf.PerlinNoise(Time.time * 31f, 0f) - .5f) * injuryShake
                    + Vector3.up * (Mathf.PerlinNoise(0f, Time.time * 27f) - .5f) * injuryShake;
                injuryShake = Mathf.MoveTowards(injuryShake, 0f, Time.deltaTime * .18f);
            }
            Vector3 baseline = enemy.position + Vector3.up * 1.2f - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(baseline.normalized) *
                Quaternion.Euler(aimOffset.y - recoil * 1.1f, aimOffset.x, -lean * 3f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-13f * Time.deltaTime));
            float roundFactor = duel != null && duel.CurrentRound == 3 ? .9f : 1f;
            float countdownFactor = duel != null && duel.CurrentState == GameState.Countdown ? .95f : 1f;
            float targetFov = (input.IsAiming ? baseFov * aimedFovFactor : baseFov) * roundFactor * countdownFactor;
            cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, targetFov, Time.deltaTime * 8f);
            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 3f);
        }
        public void ResetAim() { aimOffset = Vector2.zero; }
    }
}

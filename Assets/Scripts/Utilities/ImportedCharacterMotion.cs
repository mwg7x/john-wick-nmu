using LastStep.Core;
using LastStep.Weapons;
using UnityEngine;

namespace LastStep.Utilities
{
    [DisallowMultipleComponent]
    public sealed class ImportedCharacterMotion : MonoBehaviour
    {
        private Transform actor, opponent, pistol, muzzle, rightHand, leftHand;
        private Transform upperRight, lowerRight, upperLeft, lowerLeft;
        private Animator animator;
        private Health health;
        private WeaponController weapon;
        private LastStep.Player.ThirdPersonCamera aimCamera;
        private bool isProtagonist;
        private Vector3 lastPosition;
        private float speed, aimWeight, shotImpulse, hitImpulse;
        private bool ready;
        public float HitImpulse => hitImpulse;
        public Transform RightHand => rightHand;

        public void Initialize(Transform root, bool protagonist)
        {
            actor = root;
            isProtagonist = protagonist;
            if (protagonist) aimCamera = FindAnyObjectByType<LastStep.Player.ThirdPersonCamera>();
            animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;
            health = root.GetComponent<Health>();
            previousHealth = health != null ? health.Current : -1;
            weapon = root.GetComponent<WeaponController>();
            var markers = FindAnyObjectByType<DuelPositionMarkers>();
            opponent = markers == null ? null : protagonist ? markers.EnemyStandIn : markers.PlayerStandIn;
            pistol = root.Find("PistolPlaceholder");
            muzzle = root.Find("Muzzle");
            lastPosition = root.position;
            if (animator != null && animator.isHuman)
            {
                rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            }
            foreach (var bone in GetComponentsInChildren<Transform>())
            {
                switch (bone.name.ToLowerInvariant())
                {
                    case "hand_r": if (rightHand == null) rightHand = bone; break;
                    case "hand_l": if (leftHand == null) leftHand = bone; break;
                    case "upperarm_r": upperRight = bone; break;
                    case "lowerarm_r": lowerRight = bone; break;
                    case "upperarm_l": upperLeft = bone; break;
                    case "lowerarm_l": lowerLeft = bone; break;
                }
            }
            if (weapon != null) weapon.ShotFired += OnShot;
            if (health != null) health.Changed += OnHealth;
        }

        private void OnDestroy()
        {
            if (weapon != null) weapon.ShotFired -= OnShot;
            if (health != null) health.Changed -= OnHealth;
        }

        private void OnShot(bool hit, Vector3 point) { shotImpulse = 1f; }
        private int previousHealth = -1;
        private void OnHealth(Health current)
        {
            if (previousHealth >= 0 && current.Current < previousHealth) hitImpulse = 1f;
            previousHealth = current.Current;
        }

        private void Update()
        {
            if (actor == null || animator == null) return;
            Vector3 displacement = actor.position - lastPosition;
            displacement.y = 0f;
            float rawSpeed = displacement.magnitude / Mathf.Max(Time.deltaTime, .001f);
            if (rawSpeed > 9f) rawSpeed = 0f; // checkpoint placement is not locomotion
            lastPosition = actor.position;
            speed = Mathf.MoveTowards(speed, rawSpeed, Time.deltaTime * 9f);
            animator.SetFloat("Speed", Mathf.Min(speed, 4.8f));
            ready = health != null && health.IsAlive;
            // Maintain a guarded pistol pose while walking instead of letting
            // the run clip swing the weapon arm behind the character.
            float targetAim = ready ? Mathf.Lerp(.8f, 1f, 1f - Mathf.Clamp01(speed / 4.8f)) : 0f;
            aimWeight = Mathf.MoveTowards(aimWeight, targetAim, Time.deltaTime * 4f);
            shotImpulse = Mathf.MoveTowards(shotImpulse, 0f, Time.deltaTime * 5f);
            hitImpulse = Mathf.MoveTowards(hitImpulse, 0f, Time.deltaTime * 3.4f);
        }

        private void OnAnimatorIK(int layer)
        {
            if (actor == null || animator == null || !animator.isHuman) return;
            Vector3 forward = AimDirection();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 grip = actor.position + actor.up * 1.27f + forward * .38f + right * .20f;
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, aimWeight * .85f);
            animator.SetIKPosition(AvatarIKGoal.RightHand, grip);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, aimWeight * .56f);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, grip - right * .15f - actor.up * .06f);
            animator.SetLookAtWeight(aimWeight * .38f);
            animator.SetLookAtPosition(actor.position + forward * 12f + actor.up * 1.35f);
        }

        private void LateUpdate()
        {
            if (actor == null || pistol == null || rightHand == null) return;
            Vector3 forward = AimDirection();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            // The imported model is not always Humanoid. Apply a restrained aim pose
            // to its animated arm bones while leaving the walking clip in control.
            if (animator != null && !animator.isHuman && aimWeight > .01f)
            {
                Vector3 elbow = actor.position + actor.up * 1.24f + forward * .13f + right * .27f;
                Vector3 grip = actor.position + actor.up * 1.23f + forward * .36f + right * .19f;
                AimArm(upperRight, lowerRight, rightHand, elbow, grip, aimWeight * .82f);
                if (leftHand != null)
                {
                    Vector3 leftElbow = actor.position + actor.up * 1.13f + forward * .07f - right * .25f;
                    Vector3 support = grip - right * .14f - actor.up * .06f;
                    AimArm(upperLeft, lowerLeft, leftHand, leftElbow, support, aimWeight * .55f);
                }
            }
            pistol.position = rightHand.position + forward * .045f + actor.up * .012f;
            pistol.rotation = Quaternion.LookRotation(forward, Vector3.up) *
                Quaternion.Euler(-shotImpulse * 7f + hitImpulse * 5f, 0f, hitImpulse * -5f);
            if (muzzle != null)
            {
                muzzle.position = pistol.position + pistol.forward * .17f + pistol.up * .015f;
                muzzle.rotation = pistol.rotation;
            }
        }

        private Vector3 AimDirection()
        {
            if (isProtagonist && aimCamera != null)
            {
                Vector3 origin = aimCamera.transform.position;
                Vector3 cameraDirection = aimCamera.AimDirection;
                Vector3 target = origin + cameraDirection * 70f;
                if (Physics.Raycast(origin, cameraDirection, out var hit, 70f, ~0, QueryTriggerInteraction.Ignore)
                    && hit.collider.GetComponentInParent<Health>() != health)
                    target = hit.point;
                Vector3 gripOrigin = actor.position + actor.up * 1.25f;
                Vector3 aim = target - gripOrigin;
                if (aim.sqrMagnitude > .001f) return aim.normalized;
            }
            Vector3 direction = opponent == null ? actor.forward : opponent.position - actor.position;
            direction.y = 0f;
            return direction.sqrMagnitude < .001f ? actor.forward : direction.normalized;
        }

        private static void AimArm(Transform upper, Transform lower, Transform palm,
            Vector3 elbow, Vector3 grip, float weight)
        {
            if (upper == null || lower == null || palm == null) return;
            Vector3 current = lower.position - upper.position;
            Vector3 target = elbow - upper.position;
            if (current.sqrMagnitude > .0001f && target.sqrMagnitude > .0001f)
                upper.rotation = Quaternion.Slerp(upper.rotation,
                    Quaternion.FromToRotation(current, target) * upper.rotation, weight);
            current = palm.position - lower.position;
            target = grip - lower.position;
            if (current.sqrMagnitude > .0001f && target.sqrMagnitude > .0001f)
                lower.rotation = Quaternion.Slerp(lower.rotation,
                    Quaternion.FromToRotation(current, target) * lower.rotation, weight);
        }
    }
}

using System;
using LastStep.Core;
using UnityEngine;

namespace LastStep.Weapons
{
    [DisallowMultipleComponent]
    public sealed class WeaponController : MonoBehaviour
    {
        [SerializeField] private WeaponData data;
        [SerializeField] private DuelManager duel;
        [SerializeField] private Health owner;
        [SerializeField] private bool playerWeapon;
        [SerializeField] private Transform muzzle;
        [SerializeField] private AudioSource audioSource;
        private float nextShotTime;
        private float reloadUntil;
        public int Ammunition => duel == null ? 0 : playerWeapon ? duel.PlayerRoundAmmo : duel.EnemyRoundAmmo;
        public bool IsReloading => false;
        public int Headshots { get; private set; }
        public event Action<bool, Vector3> ShotFired;
        public event Action<Health, Vector3, Vector3, bool> FleshHit;
        public event Action<RaycastHit> SurfaceHit;
        public event Action<Vector3, Vector3> NearMiss;
        public event Action ReloadStarted;
        public WeaponData Data => data;
        public Transform Muzzle => muzzle;
        public void Equip(int index)
        {
            var selected = PistolCatalog.Load(index);
            if (selected == null) return;
            data = selected;
            reloadUntil = 0f;
            nextShotTime = 0f;
            var visual = GetComponentInChildren<PistolVisualVariant>();
            if (visual != null) visual.Equip(index);
        }
        private void Awake()
        {
            if (!playerWeapon || GameManager.Instance == null) return;
            var selected = PistolCatalog.Load(GameManager.Instance.PistolIndex);
            if (selected != null) data = selected;
            else Debug.LogError("LAST STEP: selected pistol asset missing.", this);
        }

        public bool TryReload()
        {
            // The authoritative three-shot allocation is owned by DuelManager.
            return false;
        }

        public bool TryFire(Vector3 origin, Vector3 direction)
        {
            // The countdown penalty has priority even when the magazine is empty.
            if (playerWeapon && duel != null && duel.CurrentState == GameState.Countdown)
            {
                FindAnyObjectByType<FalseStartManager>()?.ReportEarlyTrigger();
                return false;
            }
            if (data == null || duel == null || owner == null || !owner.IsAlive || Time.time < nextShotTime || IsReloading ||
                direction.sqrMagnitude < 0.9f) return false;
            if (Ammunition == 0) return false;
            if (!duel.TryRegisterShot(playerWeapon)) return false;
            nextShotTime = Time.time + data.FireCooldown;
            bool hitTarget = false;
            Vector3 impact = origin + direction.normalized * data.Range;
            bool hitSurface = false;
            // RaycastAll allows the owner's body and cosmetic primitives to be skipped without
            // hiding stairs and walls from shot occlusion. Hit-zone colliders remain on Default.
            Physics.SyncTransforms(); // Animated hit zones move outside the physics step.
            var hits = Physics.RaycastAll(origin, direction.normalized, data.Range, data.HitMask, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                var body = hit.collider.GetComponentInParent<Health>();
                if (body == owner) continue;
                // The character controller/capsule is for movement and scale. Only zone
                // colliders accept damage; the outer body must not shield them.
                var zone = hit.collider.GetComponent<HitDetector>();
                if (body != null && zone == null) continue;
                impact = hit.point;
                if (zone != null && zone.Owner != null && zone.Owner != owner)
                {
                    hitTarget = zone.Owner.ApplyDamage(data.GetDamage(zone.Zone));
                    if (hitTarget)
                    {
                        bool headshot = zone.Zone == HitZone.Head;
                        if (headshot) Headshots++;
                        FleshHit?.Invoke(zone.Owner, hit.point, direction.normalized, headshot);
                    }
                }
                else { hitSurface = true; SurfaceHit?.Invoke(hit); }
                break;
            }
            if (!playerWeapon && !hitTarget)
            {
                var markers = FindAnyObjectByType<DuelPositionMarkers>();
                var target = markers != null ? markers.PlayerStandIn : null;
                if (target != null)
                {
                    Vector3 head = target.position + Vector3.up * 1.55f;
                    if (TryGetNearMiss(origin, impact, head, out var nearest))
                        NearMiss?.Invoke(nearest, (head - nearest).normalized);
                }
            }
            if (audioSource != null && data.ShotSound != null) audioSource.PlayOneShot(data.ShotSound);
            if (data.MuzzleFlashPrefab != null && muzzle != null)
                Destroy(Instantiate(data.MuzzleFlashPrefab, muzzle.position, muzzle.rotation), 0.15f);
            if (hitSurface && data.ImpactPrefab != null) Destroy(Instantiate(data.ImpactPrefab, impact, Quaternion.identity), 1.5f);
            ShotFired?.Invoke(hitTarget, impact);
            return true;
        }
        public static bool TryGetNearMiss(Vector3 start, Vector3 end, Vector3 head, out Vector3 nearest)
        {
            Vector3 path = end - start;
            float lengthSquared = path.sqrMagnitude;
            nearest = start;
            if (lengthSquared <= .01f) return false;
            float fraction = Mathf.Clamp01(Vector3.Dot(head - start, path) / lengthSquared);
            nearest = start + path * fraction;
            float distance = Vector3.Distance(head, nearest);
            return fraction > .02f && fraction < .98f && distance > .28f && distance < 1.2f;
        }
    }
}

using UnityEngine;

namespace LastStep.Weapons
{
    [CreateAssetMenu(fileName = "Pistol", menuName = "LAST STEP/Weapon Data")]
    public sealed class WeaponData : ScriptableObject
    {
        [SerializeField, Min(1f)] private float range = 100f;
        [SerializeField, Min(0.01f)] private float fireCooldown = 0.2f;
        [SerializeField, Tooltip("Future weapon controller must additionally exclude its owner's layer.")] private LayerMask hitMask = ~0;
        [SerializeField, Min(0)] private int headDamage = 100;
        [SerializeField, Min(0)] private int chestDamage = 50;
        [SerializeField, Min(0)] private int torsoDamage = 40;
        [SerializeField, Min(0)] private int armDamage = 20;
        [SerializeField, Min(0)] private int legDamage = 20;
        [SerializeField, Range(0f, 5f), Tooltip("PC camera only; never apply camera recoil in VR.")] private float recoilDegrees = 0.8f;
        [SerializeField, Range(0f, 2f)] private float accuracySpreadDegrees = 0.1f;
        [SerializeField, Min(1)] private int magazineSize = 12;
        [SerializeField, Min(0.1f)] private float reloadSeconds = 1.35f;
        [SerializeField] private AudioClip shotSound;
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private GameObject impactPrefab;

        public float Range => range;
        public float FireCooldown => fireCooldown;
        public LayerMask HitMask => hitMask;
        public float RecoilDegrees => recoilDegrees;
        public float AccuracySpreadDegrees => accuracySpreadDegrees;
        public int MagazineSize => magazineSize;
        public float ReloadSeconds => reloadSeconds;
        public AudioClip ShotSound => shotSound;
        public GameObject MuzzleFlashPrefab => muzzleFlashPrefab;
        public GameObject ImpactPrefab => impactPrefab;

        public int GetDamage(HitZone zone)
        {
            switch (zone)
            {
                case HitZone.Head: return headDamage;
                case HitZone.Chest: return chestDamage;
                case HitZone.Torso: return torsoDamage;
                case HitZone.Arm: return armDamage;
                case HitZone.Leg: return legDamage;
                default: return 0;
            }
        }

        private void OnValidate()
        {
            range = Mathf.Max(1f, range);
            fireCooldown = Mathf.Max(0.01f, fireCooldown);
            headDamage = Mathf.Max(0, headDamage);
            chestDamage = Mathf.Max(0, chestDamage);
            torsoDamage = Mathf.Max(0, torsoDamage);
            armDamage = Mathf.Max(0, armDamage);
            legDamage = Mathf.Max(0, legDamage);
        }
    }
}

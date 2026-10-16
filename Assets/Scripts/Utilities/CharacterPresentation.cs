using LastStep.Core;
using LastStep.Weapons;
using UnityEngine;

namespace LastStep.Utilities
{
    [DisallowMultipleComponent]
    public sealed class CharacterPresentation : MonoBehaviour
    {
        [SerializeField] private DuelManager duel;
        [SerializeField] private Health health;
        [SerializeField] private WeaponController weapon;
        [SerializeField] private Transform visual;
        [SerializeField] private Transform pistolVisual;
        [SerializeField] private bool player;
        private Vector3 visualBase;
        private Vector3 visualScale;
        private Quaternion gunBase;
        private float recoil;
        private float reaction;
        private bool wasAlive;
        private bool importedMotion;

        private void Start()
        {
            if (visual != null) { visualBase = visual.localPosition; visualScale = visual.localScale; }
            if (pistolVisual != null) gunBase = pistolVisual.localRotation;
            importedMotion = visual != null && visual.GetComponentInChildren<ImportedCharacterMotion>() != null;
            wasAlive = health != null && health.IsAlive;
        }
        private void OnEnable()
        {
            if (weapon != null) weapon.ShotFired += OnShot;
            if (health != null) health.Changed += OnHealth;
        }
        private void OnDisable()
        {
            if (weapon != null) weapon.ShotFired -= OnShot;
            if (health != null) health.Changed -= OnHealth;
        }
        private void OnShot(bool hit, Vector3 point)
        { recoil = weapon != null && weapon.Data != null ? Mathf.Max(.5f,weapon.Data.RecoilDegrees) : 1f; }
        private void OnHealth(Health current)
        {
            if (current.IsAlive && current.Current < current.Maximum) reaction = 1f;
            wasAlive = current.IsAlive;
        }
        private void LateUpdate()
        {
            if (visual == null || health == null) return;
            if (!importedMotion) importedMotion = visual.GetComponentInChildren<ImportedCharacterMotion>() != null;
            float dt = Time.deltaTime;
            recoil = Mathf.MoveTowards(recoil, 0f, dt * 4f);
            reaction = Mathf.MoveTowards(reaction, 0f, dt * 2f);
            bool ready = duel != null && (duel.CurrentState == GameState.Preparing || duel.CurrentState == GameState.Countdown ||
                                            duel.CurrentState == GameState.Fire);
            float bob = health.IsAlive ? Mathf.Sin(Time.time * 1.5f) * 0.012f : 0f;
            visual.localPosition = visualBase + Vector3.up * bob;
            visual.localScale = health.IsAlive
                ? new Vector3(visualScale.x,visualScale.y*(1f+Mathf.Sin(Time.time*2f)*.0025f),visualScale.z)
                : visualScale;
            float lean = !health.IsAlive ? (player ? 24f : -24f) : recoil * 3f + reaction * -9f + (ready ? 1f : 0f);
            visual.localRotation = Quaternion.Lerp(visual.localRotation, Quaternion.Euler(0f, 0f, lean), dt * 6f);
            if (pistolVisual != null && !importedMotion)
                pistolVisual.localRotation = Quaternion.Lerp(pistolVisual.localRotation, gunBase * Quaternion.Euler(-recoil * 20f, 0, 0), dt * 9f);
        }
    }
}

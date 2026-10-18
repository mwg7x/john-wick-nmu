using System.Collections;
using LastStep.Core;
using UnityEngine;

namespace LastStep.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PCInputController input;
        [SerializeField] private DuelManager duel;
        [SerializeField] private DuelPositionMarkers markers;
        [SerializeField] private float walkSpeed = 2.8f;
        [SerializeField] private float sprintSpeed = 4.8f;
        [SerializeField] private float lateralLimit = 2.7f;
        [SerializeField] private float forwardLimit = 1.2f;
        private CharacterController controller;
        private Health health;
        private float stamina = 100f;
        private float recoveryAt;
        private float dodgeCooldown;
        private float dodgeEnds;
        private Vector3 dodgeDirection;
        private float lean;
        private Vector3 planarVelocity;
        private bool dodgeTime;
        private bool sprintExhausted;
        public Vector3 Velocity { get; private set; }
        public float Stamina => stamina;
        public float Lean => lean;
        public bool IsSprinting { get; private set; }
        private void Awake() { controller = GetComponent<CharacterController>(); health = GetComponent<Health>(); }
        private void OnEnable() { if (health != null) health.Evaded += OnEvaded; }
        private void OnDisable()
        {
            if (health != null) health.Evaded -= OnEvaded;
            StopAllCoroutines();
            RestoreDodgeTime();
        }
        private void Update()
        {
            if (input == null || duel == null || markers == null ||
                !markers.TryGetStage(duel.StageIndex, out var pair)) return;
            lean = Mathf.MoveTowards(lean, input.Lean, Time.deltaTime * 5f);
            if (Time.time >= recoveryAt) stamina = Mathf.Min(100f, stamina + Time.deltaTime * 23f);
            bool movingToCheckpoint = duel.CurrentState == GameState.Transition;
            bool combatMove = duel.CurrentState == GameState.Fire && duel.CurrentRound > 1;
            if (!movingToCheckpoint && !combatMove) { IsSprinting = false; planarVelocity = Velocity = Vector3.zero; return; }

            Vector2 axis = Vector2.ClampMagnitude(input.Move, 1f);
            Vector3 planar = new Vector3(axis.x, 0f, axis.y);
            if(stamina<=0f)sprintExhausted=true;
            if(stamina>=20f || !input.SprintHeld)sprintExhausted=false;
            IsSprinting = input.SprintHeld && axis.y>.2f && !input.IsAiming && !sprintExhausted && stamina > 0f;
            if (IsSprinting) { stamina = Mathf.Max(0f, stamina - Time.deltaTime * 18f); recoveryAt = Time.time + .8f; }
            if (input.DodgePressed && Time.time >= dodgeCooldown && stamina >= 30f)
            {
                dodgeDirection = planar.sqrMagnitude > .01f ? planar.normalized : -transform.forward;
                dodgeEnds = Time.time + .28f;
                dodgeCooldown = Time.time + 1.05f;
                stamina -= 30f;
                recoveryAt = Time.time + .9f;
                if (health != null) health.InvulnerableUntil = Time.time + .23f;
            }
            float speed = IsSprinting ? sprintSpeed : walkSpeed;
            if(input.IsAiming)speed*=.72f;
            if(axis.y<0)speed*=.78f;
            else if(Mathf.Abs(axis.x)>Mathf.Abs(axis.y))speed*=.9f;
            bool reversing=Vector3.Dot(planarVelocity,planar)<0;
            planarVelocity = Vector3.MoveTowards(planarVelocity, planar * speed,
                (reversing?38f:planar.sqrMagnitude > .01f ? 24f : 32f) * Time.deltaTime);
            Vector3 motion = (Time.time < dodgeEnds ? dodgeDirection * 7.5f : planarVelocity) * Time.deltaTime;
            Vector3 next = transform.position + motion;
            Vector3 center = pair.Player.position;
            next.x = Mathf.Clamp(next.x, center.x - lateralLimit, center.x + lateralLimit);
            if (combatMove) next.z = Mathf.Clamp(next.z, center.z - forwardLimit, center.z + forwardLimit);
            else if (markers.TryGetStage(duel.StageIndex + 1, out var destination))
            {
                float min = Mathf.Min(center.z, destination.Player.position.z) - .4f;
                float max = Mathf.Max(center.z, destination.Player.position.z) + .4f;
                next.z = Mathf.Clamp(next.z, min, max);
            }
            Vector3 before=transform.position;
            controller.Move(new Vector3(next.x - transform.position.x, -2f * Time.deltaTime,
                next.z - transform.position.z));
            Velocity=Time.deltaTime>0?(transform.position-before)/Time.deltaTime:Vector3.zero;
            Velocity=new Vector3(Velocity.x,0,Velocity.z);
        }
        private void OnEvaded(Health target)
        {
            if (Time.time >= dodgeEnds || Time.timeScale < .99f) return;
            StartCoroutine(PerfectDodge());
        }
        private IEnumerator PerfectDodge()
        {
            dodgeTime = true;
            if (GameManager.Instance != null) GameManager.Instance.SetGameplayTimeScale(.45f);
            else Time.timeScale = .45f;
            yield return new WaitForSecondsRealtime(.5f);
            RestoreDodgeTime();
        }
        private void RestoreDodgeTime()
        {
            if (!dodgeTime) return;
            dodgeTime = false;
            if (duel != null && duel.CurrentState == GameState.Finisher) return;
            if (GameManager.Instance != null) GameManager.Instance.SetGameplayTimeScale(1f);
            else Time.timeScale = 1f;
        }
    }
}

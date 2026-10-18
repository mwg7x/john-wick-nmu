using System.Collections;
using LastStep.Core;
using LastStep.Weapons;
using UnityEngine;

namespace LastStep.AI
{
    [DisallowMultipleComponent]
    public sealed class EnemyAI : MonoBehaviour
    {
        [SerializeField] private DuelManager duel;
        [SerializeField] private Health enemyHealth;
        [SerializeField] private Health playerHealth;
        [SerializeField] private WeaponController weapon;
        [SerializeField] private Transform muzzle;
        private Coroutine action;
        private enum Personality { Precision, Aggressive, Defensive, Elite }
        private Personality personality;
        private float anchorX, targetX, nextShift;
        private int tier;
        public EnemyState State { get; private set; } = EnemyState.Idle;
        public float ReactionTime { get; private set; }

        private void OnEnable()
        {
            if (duel == null) return;
            duel.OnRoundStarted += OnRoundStarted;
            duel.OnFireAllowed += OnFire;
            duel.OnStateChanged += OnState;
        }
        private void OnDisable()
        {
            if (duel != null)
            {
                duel.OnRoundStarted -= OnRoundStarted;
                duel.OnFireAllowed -= OnFire;
                duel.OnStateChanged -= OnState;
            }
            if (action != null) StopCoroutine(action);
            action = null;
        }
        private void OnRoundStarted(int round, float distance) { State = EnemyState.Prepare; }
        private void OnState(GameState previous, GameState current)
        {
            if (current == GameState.Countdown) State = EnemyState.WaitingForFire;
            else if (current == GameState.Paused) return;
            else if (current == GameState.Defeat || current == GameState.Victory)
            {
                State = enemyHealth != null && !enemyHealth.IsAlive ? EnemyState.Dead : EnemyState.Waiting;
                if (action != null) StopCoroutine(action);
                action = null;
            }
            else if (current == GameState.Transition) State = EnemyState.Moving;
        }
        private void OnFire()
        {
            var game = GameManager.Instance;
            tier=game!=null?game.DifficultyIndex:0;
            int index = game != null && game.IsPractice ? game.PracticeOpponentIndex : game != null ? game.OpponentIndex : 0;
            personality = game != null && game.DifficultyIndex == 3 ? Personality.Elite :
                (Personality)(index % 3);
            anchorX = transform.position.x;
            targetX = anchorX;
            nextShift = Time.time + Random.Range(.45f, 1f);
            if (action != null) StopCoroutine(action);
            action = StartCoroutine(ShootAfterReaction());
        }
        private void Update()
        {
            if (duel == null || !duel.IsFireAllowed || enemyHealth == null || !enemyHealth.IsAlive) return;
            if (personality == Personality.Precision && tier<2) return;
            if (Time.time >= nextShift)
            {
                float width = (personality == Personality.Defensive ? 1.1f : .8f)+tier*.22f;
                targetX = anchorX + Random.Range(-width, width);
                nextShift = Time.time + Random.Range(.65f, 1.3f)/(1+tier*.12f);
            }
            var p = transform.position;
            p.x = Mathf.MoveTowards(p.x, targetX,
                (1.0f+tier*.3f) * Time.deltaTime);
            transform.position = p;
        }
        private IEnumerator ShootAfterReaction()
        {
            var difficulty = GameManager.Instance != null ? GameManager.Instance.SelectedDifficulty : null;
            if (difficulty == null || weapon == null || muzzle == null || playerHealth == null || enemyHealth == null)
            { Debug.LogError("LAST STEP: EnemyAI missing difficulty, health or weapon reference.", this); yield break; }
            State = EnemyState.Reacting;
            float reactionFactor = personality == Personality.Precision ? 1.2f :
                personality == Personality.Aggressive ? .8f : personality == Personality.Elite ? .72f : 1.05f;
            ReactionTime = Random.Range(difficulty.MinReactionTime, difficulty.MaxReactionTime) * reactionFactor;
            yield return new WaitForSeconds(ReactionTime);
            while (duel != null && duel.IsFireAllowed && enemyHealth.IsAlive && playerHealth.IsAlive)
            {
                if (weapon.Ammunition == 0) break;
                State = EnemyState.Aiming;
                // Aim at the animated hurt volume, not an assumed height on the actor root.
                bool headshot=Random.value<difficulty.CriticalHitChance;
                var chest = playerHealth.transform.Find(headshot?"HitZones/Head":"HitZones/Chest");
                Vector3 point = chest != null ? chest.position : playerHealth.transform.position + Vector3.up * 1.2f;
                float aimTime=Mathf.Clamp(1f/difficulty.AimSpeed,.10f,.6f);
                var movement=playerHealth.GetComponent<LastStep.Player.PlayerMovement>();
                // Commit before firing. Higher tiers lead observed motion, but a direction
                // change during this aim window can still evade the shot.
                if(movement!=null)point+=Vector3.ClampMagnitude(movement.Velocity,4.8f)*aimTime*Mathf.Lerp(.15f,.8f,tier/3f);
                yield return new WaitForSeconds(aimTime);
                if (!duel.IsFireAllowed || !enemyHealth.IsAlive || !playerHealth.IsAlive) yield break;
                float precision = personality == Personality.Precision ? .78f :
                    personality == Personality.Aggressive ? 1.18f : personality == Personality.Elite ? .7f : 1f;
                float spread = Mathf.Lerp(.6f, .025f, difficulty.Accuracy) * precision;
                point += Random.insideUnitSphere * spread;
                if (Random.value < difficulty.MissChance) point += Random.onUnitSphere * 2.8f;
                Vector3 direction = (point - muzzle.position).normalized;
                State = EnemyState.Shooting;
                weapon.TryFire(muzzle.position, direction);
                float cadence = personality == Personality.Aggressive || personality == Personality.Elite ? .85f : 1.1f;
                yield return new WaitForSeconds(Mathf.Max(.05f, weapon.Data.FireCooldown * cadence));
            }
            State = EnemyState.Waiting;
        }
    }
}

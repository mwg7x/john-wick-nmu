using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastStep.Core
{
    [DisallowMultipleComponent]
    public sealed class RoundManager : MonoBehaviour
    {
        [SerializeField] private DuelManager duel;
        [SerializeField] private DuelPositionManager positions;
        [SerializeField] private Health player;
        [SerializeField] private Health enemy;
        private bool completing;
        private float combatEndTime;
        private int nextStage;
        private bool cinematicTime;
        public float CombatRemaining => duel != null && duel.CurrentState == GameState.Fire
            ? Mathf.Max(0f, combatEndTime - Time.time) : 0f;
        public float CheckpointRemaining => duel != null && duel.CurrentState == GameState.Transition && positions != null
            ? positions.PlayerDistanceToStage(nextStage) : 0f;

        private void Start()
        {
            if (duel == null || duel.Settings == null || player == null || enemy == null)
            { Debug.LogError("LAST STEP: RoundManager references are incomplete.", this); return; }
            player.ResetHealth(duel.Settings.StartingHealth);
            enemy.ResetHealth(duel.Settings.StartingHealth);
        }
        private void OnEnable()
        {
            if (duel == null) return;
            duel.OnRoundStarted += OnRoundStarted;
            duel.OnFireAllowed += OnFireAllowed;
            if (player != null) player.Defeated += OnDefeated;
            if (enemy != null) enemy.Defeated += OnDefeated;
        }
        private void OnDisable()
        {
            if (cinematicTime) { SetTimeScale(1f); cinematicTime = false; }
            if (duel != null) { duel.OnRoundStarted -= OnRoundStarted; duel.OnFireAllowed -= OnFireAllowed; }
            if (player != null) player.Defeated -= OnDefeated;
            if (enemy != null) enemy.Defeated -= OnDefeated;
        }
        private void OnRoundStarted(int round, float distance)
        {
            completing = false;
            if (positions == null || !positions.PlaceStage(duel.StageIndex))
                Debug.LogError("LAST STEP: missing stage " + duel.StageIndex, this);
        }
        private void OnFireAllowed() { combatEndTime = Time.time + duel.Settings.CombatSeconds; }
        private void OnDefeated(Health target)
        {
            if (!completing && duel != null && duel.CurrentState == GameState.Fire)
                StartCoroutine(Resolve(true));
        }
        private void Update()
        {
            if (duel == null || completing) return;
            if (duel.CurrentState == GameState.Fire && Time.time >= combatEndTime)
                StartCoroutine(Resolve(false));
            else if (duel.CurrentState == GameState.Transition && nextStage > 0 &&
                     positions.PlayerDistanceToStage(nextStage) <= 1.5f)
                duel.TryPrepareRound(nextStage + 1, nextStage);
        }
        private IEnumerator Resolve(bool lethal)
        {
            if (completing || duel.CurrentState != GameState.Fire) yield break;
            completing = true;
            if (lethal)
            {
                if (!duel.TryBeginFinisher()) yield break;
                cinematicTime = true;
                SetTimeScale(.35f);
                float finishUntil = Time.realtimeSinceStartup + .65f;
                while (Time.realtimeSinceStartup < finishUntil)
                {
                    if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) break;
                    yield return null;
                }
                SetTimeScale(1f);
                cinematicTime = false;
            }
            // The finisher may complete while a state notification or pause is being
            // processed. Resume resolution on the next frame rather than stranding it.
            while (!duel.TryBeginResolution())
            {
                if (duel.CurrentState != GameState.Finisher && duel.CurrentState != GameState.Fire &&
                    duel.CurrentState != GameState.Paused) yield break;
                yield return null;
            }
            if (!player.IsAlive) { duel.TryDeclareDefeat(); yield break; }
            if (!enemy.IsAlive) { DeclareVictory(); yield break; }
            if (duel.StageIndex + 1 >= duel.Settings.StageCount)
            {
                if (player.Current > enemy.Current) DeclareVictory();
                else duel.TryDeclareDefeat();
                yield break;
            }
            nextStage = duel.StageIndex + 1;
            if (duel.TryBeginTransition())
            {
                positions.PlaceEnemyStage(nextStage);
                completing = false;
            }
        }
        private void DeclareVictory()
        {
            duel.TryDeclareVictory();
        }
        private static void SetTimeScale(float value)
        {
            if (GameManager.Instance != null) GameManager.Instance.SetGameplayTimeScale(value);
            else Time.timeScale = value;
        }
    }
}

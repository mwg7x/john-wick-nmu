using System;
using UnityEngine;

namespace LastStep.Core
{
    /// <summary>Scene-local duel state authority. Timing, input and round progression are external.</summary>
    [DisallowMultipleComponent]
    public sealed class DuelManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Shared balance settings used to report the selected duel distance.")]
        private DuelSettings settings;
        [SerializeField, Tooltip("Enter Intro when this scene starts. No timed transitions run in Phase 3.")]
        private bool beginOnStart = true;

        private GameManager gameManager;
        private GameState resumeState;
        private bool notifying;
        private bool started;

        public event Action<GameState, GameState> OnStateChanged;
        public event Action<int, float> OnRoundStarted;
        public event Action OnCountdownStarted;
        public event Action OnFireAllowed;
        public event Action OnRoundFinished;
        public event Action OnVictory;
        public event Action OnDefeat;
        public event Action OnPlayerShot;
        public event Action OnEnemyShot;

        public GameState CurrentState { get; private set; } = GameState.Boot;
        public int CurrentRound { get; private set; }
        public int StageIndex { get; private set; }
        public float CurrentDistance => settings != null && CurrentRound > 0 ? settings.GetDistance(StageIndex) : 0f;
        public DuelSettings Settings => settings;
        public bool IsInitialized => started && settings != null;
        public bool PlayerShotAvailable { get; private set; }
        public bool EnemyShotAvailable { get; private set; }
        public const int ShotsPerRound = 3;
        public int PlayerRoundAmmo { get; private set; }
        public int EnemyRoundAmmo { get; private set; }
        public bool IsFireAllowed => isActiveAndEnabled && CurrentState == GameState.Fire &&
                                     (gameManager == null || !gameManager.IsPaused);

        private void Start()
        {
            started = true;
            BindPauseService();
            if (settings == null)
            {
                Debug.LogError("LAST STEP: assign DuelSettings on the scene's DuelSystems/DuelManager.", this);
                return;
            }
            if (beginOnStart) TryBeginDuel();
        }

        private void OnEnable() { if (started) { BindPauseService(); SynchronizePause(); } }
        private void OnDisable()
        {
            if (gameManager != null) gameManager.PauseChanged -= OnPauseChanged;
            gameManager = null;
        }

        private void BindPauseService()
        {
            if (gameManager != null) gameManager.PauseChanged -= OnPauseChanged;
            gameManager = GameManager.Instance;
            if (gameManager != null) gameManager.PauseChanged += OnPauseChanged;
        }

        public bool TryBeginDuel()
        {
            if (!CanChange(GameState.Boot)) return false;
            CurrentRound = 0;
            StageIndex = 0;
            PlayerRoundAmmo = EnemyRoundAmmo = 0;
            ChangeState(GameState.Intro);
            return true;
        }

        /// <summary>Called by an external coordinator after intro or movement completes.</summary>
        public bool TryPrepareRound(int roundNumber, int stageIndex)
        {
            if (!CanChange(GameState.Intro) && !CanChange(GameState.Transition)) return false;
            if (roundNumber < 1 || roundNumber > 3 || stageIndex != roundNumber - 1 || stageIndex >= settings.StageCount) return false;
            if (CurrentState == GameState.Intro && (roundNumber != 1 || stageIndex != 0)) return false;
            if (CurrentState == GameState.Transition && roundNumber != CurrentRound + 1) return false;
            CurrentRound = roundNumber;
            StageIndex = stageIndex;
            PlayerShotAvailable = true;
            EnemyShotAvailable = true;
            PlayerRoundAmmo = EnemyRoundAmmo = ShotsPerRound;
            ChangeState(GameState.Preparing, () => OnRoundStarted?.Invoke(CurrentRound, CurrentDistance));
            return true;
        }

        public bool TryRegisterShot(bool player)
        {
            if (!Application.isPlaying || notifying || !IsFireAllowed) return false;
            if (player)
            {
                if (PlayerRoundAmmo <= 0) return false;
                PlayerRoundAmmo--;
                OnPlayerShot?.Invoke();
            }
            else
            {
                if (EnemyRoundAmmo <= 0) return false;
                EnemyRoundAmmo--;
                OnEnemyShot?.Invoke();
            }
            return true;
        }

        public bool TryStartCountdown() => Move(GameState.Preparing, GameState.Countdown, () => OnCountdownStarted?.Invoke());
        public bool TryAllowFire() => Move(GameState.Countdown, GameState.Fire, () => OnFireAllowed?.Invoke());
        public bool TryBeginFinisher() => Move(GameState.Fire, GameState.Finisher);
        public bool TryBeginResolution() => CurrentState == GameState.Finisher
            ? Move(GameState.Finisher, GameState.Resolving) : Move(GameState.Fire, GameState.Resolving);
        public bool TryBeginTransition() => Move(GameState.Resolving, GameState.Transition, () => OnRoundFinished?.Invoke());
        public bool TryDeclareVictory() => Move(GameState.Resolving, GameState.Victory, () =>
        {
            OnRoundFinished?.Invoke();
            OnVictory?.Invoke();
        });

        public bool TryDeclareEnemyFalseStartVictory() => Move(GameState.Countdown, GameState.Victory,
            () => OnVictory?.Invoke());

        // Countdown defeat is reserved for the future false-start disqualification coordinator.
        public bool TryDeclareDefeat()
        {
            if (!CanChange(GameState.Resolving) && !CanChange(GameState.Countdown)) return false;
            bool completedExchange = CurrentState == GameState.Resolving;
            ChangeState(GameState.Defeat, () =>
            {
                if (completedExchange) OnRoundFinished?.Invoke();
                OnDefeat?.Invoke();
            });
            return true;
        }

        private bool CanChange(GameState expected) => Application.isPlaying && IsInitialized && isActiveAndEnabled &&
            !notifying && CurrentState == expected && (gameManager == null || !gameManager.IsPaused);

        private bool Move(GameState expected, GameState next, Action signal = null)
        {
            if (!CanChange(expected)) return false;
            ChangeState(next, signal);
            return true;
        }

        private void ChangeState(GameState next, Action signal = null)
        {
            GameState previous = CurrentState;
            CurrentState = next;
            notifying = true;
            try
            {
                OnStateChanged?.Invoke(previous, next);
                signal?.Invoke();
            }
            finally
            {
                notifying = false;
                // A listener may pause through GameManager while events are being dispatched.
                SynchronizePause();
            }
        }

        private void OnPauseChanged(bool paused) { SynchronizePause(); }

        private void SynchronizePause()
        {
            if (notifying || !isActiveAndEnabled || gameManager == null || !started) return;
            if (gameManager.IsPaused && CurrentState != GameState.Paused && CurrentState != GameState.Boot)
            {
                resumeState = CurrentState;
                ChangeState(GameState.Paused);
            }
            else if (!gameManager.IsPaused && CurrentState == GameState.Paused)
            {
                // Resume changes the state only; it must not replay countdown/FIRE/result signals.
                ChangeState(resumeState);
            }
        }
    }
}

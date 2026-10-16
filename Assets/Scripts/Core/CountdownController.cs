using System;
using UnityEngine;

namespace LastStep.Core
{
    public enum CountdownCue { Hidden, Three, Two, One, Draw, Fire }

    [DisallowMultipleComponent]
    public sealed class CountdownController : MonoBehaviour
    {
        [SerializeField, Tooltip("Scene-local state authority.")] private DuelManager duel;
        [SerializeField, Tooltip("Prepare round 1 after Intro. Disable when a later cinematic intro coordinator takes over.")]
        private bool prepareFirstRound = true;
        private int activeRound;
        private int cueFrame;
        private bool initialized;

        public event Action<CountdownCue> CueChanged;
        public CountdownCue CurrentCue { get; private set; }
        public float RemainingSeconds { get; private set; }
        public DuelManager Duel => duel;

        private void OnEnable()
        {
            if (duel == null) return;
            duel.OnCountdownStarted += BeginCountdown;
            duel.OnFireAllowed += ShowFire;
            duel.OnStateChanged += OnStateChanged;
            if (initialized) Synchronize();
        }

        private void Start()
        {
            initialized = true;
            if (duel == null)
            {
                Debug.LogError("LAST STEP: assign DuelManager on CountdownController.", this);
                enabled = false;
                return;
            }
            Synchronize();
        }

        private void OnDisable()
        {
            if (duel == null) return;
            duel.OnCountdownStarted -= BeginCountdown;
            duel.OnFireAllowed -= ShowFire;
            duel.OnStateChanged -= OnStateChanged;
        }

        private void Synchronize()
        {
            if (duel == null || !duel.IsInitialized) return;
            if (duel.CurrentState == GameState.Paused) return;
            if (duel.CurrentState == GameState.Countdown)
            {
                if (activeRound != duel.CurrentRound || CurrentCue == CountdownCue.Hidden || CurrentCue == CountdownCue.Fire)
                    BeginCountdown();
            }
            else if (duel.CurrentState != GameState.Fire) SetCue(CountdownCue.Hidden, 0f);
            else if (CurrentCue != CountdownCue.Fire) SetCue(CountdownCue.Hidden, 0f);
        }

        private void Update()
        {
            if (duel == null || !duel.IsInitialized || !duel.isActiveAndEnabled) return;
            // Advance outside state-event delivery; DuelManager deliberately rejects reentrant transitions.
            if (duel.CurrentState == GameState.Intro && prepareFirstRound)
            { duel.TryPrepareRound(1, 0); return; }
            if (duel.CurrentState == GameState.Preparing)
            { duel.TryStartCountdown(); return; }
            if (duel.CurrentState == GameState.Paused) return;
            if (duel.CurrentState != GameState.Countdown && duel.CurrentState != GameState.Fire) return;
            if (CurrentCue == CountdownCue.Hidden || Time.frameCount == cueFrame) return;

            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
            if (RemainingSeconds > 0f) return;
            switch (CurrentCue)
            {
                case CountdownCue.Three: SetCue(CountdownCue.Two, duel.Settings.CountdownDuration); break;
                case CountdownCue.Two: SetCue(CountdownCue.One, duel.Settings.CountdownDuration); break;
                case CountdownCue.One:
                    // Authorization and the DRAW cue begin in the same state transition.
                    if (duel.TryAllowFire()) SetCue(CountdownCue.Draw, duel.Settings.DrawDuration);
                    break;
                case CountdownCue.Draw: SetCue(CountdownCue.Fire, duel.Settings.FireDisplayDuration); break;
                case CountdownCue.Fire: SetCue(CountdownCue.Hidden, 0f); break;
            }
        }

        private void BeginCountdown()
        {
            activeRound = duel.CurrentRound;
            SetCue(CountdownCue.Three, duel.Settings.CountdownDuration);
        }

        private void ShowFire() { /* DRAW is published by the countdown after authorization. */ }

        private void OnStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.Countdown || current == GameState.Fire || current == GameState.Paused) return;
            SetCue(CountdownCue.Hidden, 0f);
        }

        private void SetCue(CountdownCue cue, float duration)
        {
            bool changed = CurrentCue != cue;
            CurrentCue = cue;
            RemainingSeconds = duration;
            cueFrame = Time.frameCount;
            if (changed) CueChanged?.Invoke(cue);
        }
    }
}

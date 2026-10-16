using System;
using System.Collections.Generic;
using System.IO;
using LastStep.Core;
using LastStep.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastStep.Editor
{
    [InitializeOnLoad]
    public static class Phase4Validation
    {
        private const string Key = "LastStep.Phase4Validation.";
        private static CountdownController countdown;
        private static CountdownView view;
        private static GameManager persistent;
        private static readonly List<CountdownCue> cues = new List<CountdownCue>();
        private static readonly List<double> times = new List<double>();
        private static readonly HashSet<CountdownCue> pausedCues = new HashSet<CountdownCue>();
        private static double pauseEnd;
        private static float pausedRemaining;
        private static CountdownCue pausedCue;
        private static bool pauseActive;
        private static int fireEvents;

        static Phase4Validation()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key + "running", false))
                    Finish(SessionState.GetString(Key + "error", ""));
            };
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.logMessageReceived += (message, trace, type) =>
            {
                if (SessionState.GetBool(Key + "running", false) &&
                    (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                    SessionState.SetString(Key + "error", message + "\n" + trace);
            };
        }

        [MenuItem("LAST STEP/Phase 4/Validate Countdown")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Phase4Setup.EnsureResources(true)) return;
            SessionState.SetInt(Key + "stage", 0);
            SessionState.SetInt(Key + "assertions", 0);
            SessionState.SetString(Key + "error", "");
            SessionState.SetString(Key + "timings", "");
            try
            {
                Phase4Setup.Configure();
                string before = File.ReadAllText(Phase2Setup.ScenePath);
                Phase4Setup.Configure();
                Require(before == File.ReadAllText(Phase2Setup.ScenePath), "Setup changed existing scene on second call.");
                EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity");
                SessionState.SetBool(Key + "running", true);
                SessionState.SetFloat(Key + "deadline", (float)EditorApplication.timeSinceStartup + 120f);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception error) { Finish(error.ToString()); }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!SessionState.GetBool(Key + "running", false) || scene.name != "Duel_Staircase") return;
            countdown = UnityEngine.Object.FindAnyObjectByType<CountdownController>();
            view = UnityEngine.Object.FindAnyObjectByType<CountdownView>();
            cues.Clear(); times.Clear(); pausedCues.Clear(); pauseActive = false; fireEvents = 0;
            if (countdown == null) return;
            countdown.CueChanged += cue => { cues.Add(cue); times.Add(Time.timeAsDouble); };
            countdown.Duel.OnFireAllowed += () => fireEvents++;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Key + "running", false)) return;
            string error = SessionState.GetString(Key + "error", "");
            if (error.Length > 0) { Finish(error); return; }
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + "deadline", 0f))
            { Finish("Timed out waiting for countdown validation."); return; }
            if (!EditorApplication.isPlaying || GameManager.Instance == null || GameManager.Instance.IsLoading) return;
            try
            {
                var game = GameManager.Instance;
                int stage = SessionState.GetInt(Key + "stage", 0);
                string scene = SceneManager.GetActiveScene().name;
                if (stage == 0 && scene == "MainMenu")
                {
                    persistent = game;
                    SessionState.SetInt(Key + "stage", 1);
                    game.StartDuel();
                    return;
                }
                if (stage == 5 && scene == "MainMenu") { Complete(); return; }
                if (scene != "Duel_Staircase") return;
                Require(game == persistent, "Persistent manager changed.");
                Require(countdown != null && view != null && countdown.Duel != null, "Missing countdown references.");
                var duel = countdown.Duel;
                if (!duel.IsInitialized) return;
                if (stage == 1)
                {
                    if (pauseActive)
                    {
                        Require(duel.CurrentState == GameState.Paused && !duel.IsFireAllowed, "Pause gate failed.");
                        Require(countdown.CurrentCue == pausedCue && Mathf.Approximately(countdown.RemainingSeconds, pausedRemaining), "Countdown advanced while paused.");
                        if (EditorApplication.timeSinceStartup >= pauseEnd) { pauseActive = false; game.SetPaused(false); }
                        return;
                    }
                    var cue = countdown.CurrentCue;
                    if (cue != CountdownCue.Hidden)
                    {
                        Require(view.IsVisible && view.DisplayedCue == Label(cue), "Displayed cue differs from controller.");
                        Require(duel.IsFireAllowed == (cue == CountdownCue.Fire), "Shooting gate differs from countdown cue.");
                        if (!pausedCues.Contains(cue) && countdown.RemainingSeconds < Duration(cue, duel.Settings) * 0.65f)
                        {
                            pausedCues.Add(cue);
                            pausedCue = cue;
                            pausedRemaining = countdown.RemainingSeconds;
                            pauseEnd = EditorApplication.timeSinceStartup + 0.3;
                            pauseActive = true;
                            game.SetPaused(true);
                        }
                    }
                    else if (fireEvents > 0)
                    {
                        CheckSequence(duel.Settings);
                        Require(pausedCues.Count == 5, "Not every cue was pause-tested.");
                        Require(!view.IsVisible && duel.IsFireAllowed && duel.CurrentState == GameState.Fire, "Hiding FIRE incorrectly ended the firing window.");
                        Require(duel.TryBeginResolution() && duel.TryBeginTransition() && duel.TryPrepareRound(2, 1), "Second exchange setup failed.");
                        cues.Clear(); times.Clear();
                        SessionState.SetInt(Key + "stage", 2);
                    }
                }
                else if (stage == 2 && countdown.CurrentCue == CountdownCue.Two)
                {
                    Require(duel.CurrentRound == 2 && duel.CurrentDistance == duel.Settings.GetDistance(1), "New countdown round metadata incorrect.");
                    Require(duel.TryDeclareDefeat(), "Countdown cancellation failed.");
                    Require(countdown.CurrentCue == CountdownCue.Hidden && !view.IsVisible && !duel.IsFireAllowed, "Defeat did not cancel/hide countdown.");
                    pauseEnd = EditorApplication.timeSinceStartup + 1.2;
                    SessionState.SetInt(Key + "stage", 3);
                }
                else if (stage == 3 && EditorApplication.timeSinceStartup >= pauseEnd)
                {
                    Require(duel.CurrentState == GameState.Defeat && fireEvents == 1 && countdown.CurrentCue == CountdownCue.Hidden, "Cancelled countdown later emitted FIRE.");
                    SessionState.SetInt(Key + "stage", 4);
                    game.RestartDuel();
                }
                else if (stage == 4 && countdown.CurrentCue == CountdownCue.Three)
                {
                    Require(duel.CurrentRound == 1 && fireEvents == 0 && cues.Count == 1, "Restart retained countdown state or listeners.");
                    // Unload a live countdown; no delayed callback may advance the next scene.
                    SessionState.SetInt(Key + "stage", 5);
                    game.ReturnToMainMenu();
                }
            }
            catch (Exception exception) { Finish(exception.ToString()); }
        }

        private static void Complete()
        {
            Require(countdown == null && UnityEngine.Object.FindAnyObjectByType<CountdownController>() == null, "Countdown survived scene unload.");
            SessionState.SetInt(Key + "stage", 6);
            EditorApplication.ExitPlaymode();
        }

        private static void CheckSequence(DuelSettings settings)
        {
            var expected = new[] { CountdownCue.Three, CountdownCue.Two, CountdownCue.One, CountdownCue.Draw, CountdownCue.Fire, CountdownCue.Hidden };
            Require(cues.Count == expected.Length && fireEvents == 1, "Wrong cue count or duplicate FIRE event.");
            string details = "";
            for (int i = 0; i < expected.Length; i++)
            {
                Require(cues[i] == expected[i], "Cue sequence mismatch.");
                if (i == expected.Length - 1) continue;
                double elapsed = times[i + 1] - times[i];
                float duration = Duration(expected[i], settings);
                Require(elapsed >= duration - 0.025 && elapsed <= duration + 0.35, "Cue duration outside frame tolerance: " + expected[i] + " = " + elapsed);
                details += expected[i] + ": " + elapsed.ToString("F3") + " scaled seconds (setting " + duration + ")\n";
            }
            SessionState.SetString(Key + "timings", details);
        }

        private static float Duration(CountdownCue cue, DuelSettings settings) => cue == CountdownCue.Draw ? settings.DrawDuration :
            cue == CountdownCue.Fire ? settings.FireDisplayDuration : settings.CountdownDuration;
        private static string Label(CountdownCue cue) => cue switch
        { CountdownCue.Three => "3", CountdownCue.Two => "2", CountdownCue.One => "1", CountdownCue.Draw => "DRAW", CountdownCue.Fire => "FIRE", _ => "" };
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            SessionState.SetInt(Key + "assertions", SessionState.GetInt(Key + "assertions", 0) + 1);
        }
        private static void Finish(string error)
        {
            SessionState.SetBool(Key + "running", false);
            bool passed = error.Length == 0 && SessionState.GetInt(Key + "stage", 0) == 6;
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/phase4-validation.txt", "Unity: " + Application.unityVersion + "\nResult: " + (passed ? "PASS" : "FAIL") +
                "\nAssertions (includes frame checks): " + SessionState.GetInt(Key + "assertions", 0) + "\nScene flow stage: " + SessionState.GetInt(Key + "stage", 0) +
                "\n" + SessionState.GetString(Key + "timings", "") + error);
            if (passed) Debug.Log("LAST STEP PHASE 4 VALIDATION PASSED: timed cues, pause, permission gate, cancellation and reload.");
            else Debug.LogError("LAST STEP PHASE 4 VALIDATION FAILED: " + error);
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
            else if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }
    }
}

using System;
using System.IO;
using System.Linq;
using LastStep.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastStep.Editor
{
    [InitializeOnLoad]
    public static class Phase3Validation
    {
        private const string Key = "LastStep.Phase3Validation.";
        private static GameManager persistentManager;
        private static DuelManager previousDuel;

        static Phase3Validation()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayChanged;
            Application.logMessageReceived += OnLog;
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                // Keep state-unit scenarios deterministic once later coordinators are installed.
                if (!SessionState.GetBool(Key + "running", false)) return;
                foreach (var controller in UnityEngine.Object.FindObjectsByType<CountdownController>(FindObjectsInactive.Include))
                    controller.enabled = false;
            };
        }

        [MenuItem("LAST STEP/Phase 3/Validate State System")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetInt(Key + "stage", 0);
            SessionState.SetInt(Key + "assertions", 0);
            SessionState.SetString(Key + "error", "");
            try
            {
                Require(UnityEditor.Search.SearchService.EnumerateDatabases().Any(), "Search index initialization failed.");
                Phase3Setup.Configure();
                string sceneBefore = File.ReadAllText(Phase2Setup.ScenePath);
                Phase3Setup.Configure();
                Require(sceneBefore == File.ReadAllText(Phase2Setup.ScenePath), "Setup is not idempotent.");
                Require(UnityEngine.Object.FindObjectsByType<DuelManager>(FindObjectsInactive.Include).Length == 1, "Expected one configured DuelManager.");
                EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity");
                SessionState.SetBool(Key + "running", true);
                SessionState.SetFloat(Key + "deadline", (float)EditorApplication.timeSinceStartup + 120f);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception error) { Finish(error.ToString()); }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Key + "running", false)) return;
            string error = SessionState.GetString(Key + "error", "");
            if (!string.IsNullOrEmpty(error)) { Finish(error); return; }
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + "deadline", 0f))
            { Finish("Timed out waiting for state-system scene flow."); return; }
            if (!EditorApplication.isPlaying) return;
            var game = GameManager.Instance;
            if (game == null || game.IsLoading) return;
            try
            {
                int stage = SessionState.GetInt(Key + "stage", 0);
                string scene = SceneManager.GetActiveScene().name;
                if (stage == 0 && scene == "MainMenu")
                {
                    persistentManager = game;
                    Require(UnityEngine.Object.FindAnyObjectByType<DuelManager>() == null, "DuelManager leaked into MainMenu.");
                    SessionState.SetInt(Key + "stage", 1);
                    game.StartDuel();
                }
                else if (stage >= 1 && stage <= 3 && scene == "Duel_Staircase")
                {
                    var duel = UnityEngine.Object.FindAnyObjectByType<DuelManager>();
                    if (duel == null || !duel.IsInitialized) return;
                    CheckFreshScene(duel, game);
                    if (stage == 1) TestVictoryPath(duel, game);
                    else if (stage == 2)
                    {
                        PrepareFire(duel);
                        Require(duel.TryBeginResolution(), "Defeat resolution entry.");
                        int defeats = 0;
                        duel.OnDefeat += () => defeats++;
                        Require(duel.TryDeclareDefeat(), "Defeat entry.");
                        TestPause(duel, game);
                        Require(defeats == 1 && !duel.TryDeclareDefeat(), "Defeat event repeated.");
                        CheckTerminal(duel);
                    }
                    else
                    {
                        Require(duel.TryPrepareRound(1, 0) && duel.TryStartCountdown(), "Disqualification setup.");
                        Require(duel.TryDeclareDefeat() && duel.CurrentState == GameState.Defeat, "Countdown disqualification state path.");
                        Require(!duel.IsFireAllowed && !duel.TryAllowFire(), "Disqualified duel allowed FIRE.");
                    }
                    previousDuel = duel;
                    SessionState.SetInt(Key + "stage", stage + 1);
                    if (stage < 3) game.RestartDuel();
                    else game.ReturnToMainMenu();
                }
                else if (stage == 4 && scene == "MainMenu")
                {
                    Require(game == persistentManager && previousDuel == null, "Scene-local duel survived unloading.");
                    Require(UnityEngine.Object.FindAnyObjectByType<DuelManager>() == null, "Duel component leaked into menu.");
                    Require(!game.IsPaused && Mathf.Approximately(Time.timeScale, 1f), "Pause leaked into menu.");
                    // Old DuelManager subscriptions would surface here if not cleaned up correctly.
                    game.SetPaused(true);
                    game.SetPaused(false);
                    SessionState.SetInt(Key + "stage", 5);
                    game.StartDuel();
                }
                else if (stage == 5 && scene == "Duel_Staircase")
                {
                    var duel = UnityEngine.Object.FindAnyObjectByType<DuelManager>();
                    if (duel == null || !duel.IsInitialized) return;
                    CheckFreshScene(duel, game);
                    TestPauseDuringEvent(duel, game);
                    SessionState.SetInt(Key + "stage", 6);
                    EditorApplication.ExitPlaymode();
                }
            }
            catch (Exception exception) { Finish(exception.ToString()); }
        }

        private static void CheckFreshScene(DuelManager duel, GameManager game)
        {
            Require(game == persistentManager, "Persistent manager replaced.");
            Require(UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include).Length == 1, "Duplicate GameManager.");
            Require(UnityEngine.Object.FindObjectsByType<DuelManager>(FindObjectsInactive.Include).Length == 1, "Duplicate DuelManager.");
            Require(previousDuel == null, "Previous scene-local manager was not destroyed.");
            Require(duel.Settings == game.DuelSettings, "Shared settings reference mismatch.");
            Require(duel.CurrentState == GameState.Intro && duel.CurrentRound == 0 && !duel.IsFireAllowed, "Fresh duel did not reset to Intro.");
        }

        private static void TestVictoryPath(DuelManager duel, GameManager game)
        {
            int rounds = 0, countdowns = 0, fires = 0, finishes = 0, victories = 0;
            bool recursiveAccepted = false;
            duel.OnRoundStarted += (round, distance) =>
            {
                Require(duel.CurrentState == GameState.Preparing && round == duel.CurrentRound && distance == duel.CurrentDistance, "Round event payload/order.");
                rounds++;
                recursiveAccepted |= duel.TryStartCountdown();
            };
            duel.OnCountdownStarted += () => countdowns++;
            duel.OnFireAllowed += () => fires++;
            duel.OnRoundFinished += () => finishes++;
            duel.OnVictory += () => victories++;
            Require(!duel.TryAllowFire() && !duel.TryDeclareVictory() && !duel.TryBeginResolution(), "Intro accepted skipped states.");
            Require(!duel.TryPrepareRound(2, 0) && !duel.TryPrepareRound(1, -1) && !duel.TryPrepareRound(1, duel.Settings.StageCount), "Invalid initial round accepted.");
            TestPause(duel, game);
            for (int round = 1; round <= 4; round++)
            {
                // External test inputs exercise metadata; no participant movement/automatic progression runs.
                int stage = Mathf.Min(round - 1, duel.Settings.StageCount - 1);
                Require(duel.TryPrepareRound(round, stage), "Preparing entry.");
                Require(!recursiveAccepted, "Nested state mutation was accepted during event delivery.");
                Require(duel.CurrentRound == round && duel.CurrentDistance == duel.Settings.GetDistance(stage), "Round metadata mismatch.");
                Require(!duel.TryPrepareRound(round, stage) && !duel.TryAllowFire() && !duel.IsFireAllowed, "Preparing gate.");
                TestPause(duel, game);
                Require(duel.TryStartCountdown() && !duel.TryStartCountdown(), "Countdown entry/duplicate guard.");
                Require(!duel.IsFireAllowed && !duel.TryBeginTransition(), "Countdown gate.");
                TestPause(duel, game);
                Require(duel.TryAllowFire() && duel.IsFireAllowed && !duel.TryAllowFire(), "FIRE gate.");
                TestPause(duel, game);
                Require(duel.IsFireAllowed, "FIRE permission did not return after resume.");
                // Disabled components must close the state gate and catch up to persistent pause on re-enable.
                duel.enabled = false;
                Require(!duel.IsFireAllowed && !duel.TryBeginResolution(), "Disabled duel accepted gameplay.");
                game.SetPaused(true);
                duel.enabled = true;
                Require(duel.CurrentState == GameState.Paused, "Re-enabled duel did not synchronize pause.");
                game.SetPaused(false);
                Require(duel.CurrentState == GameState.Fire, "Re-enabled duel resume failed.");
                Require(duel.TryBeginResolution() && !duel.IsFireAllowed, "Resolving gate.");
                Require(finishes == round - 1, "Round-finished event fired before resolution completed.");
                TestPause(duel, game);
                if (round < 4)
                {
                    Require(duel.TryBeginTransition(), "Transition entry.");
                    TestPause(duel, game);
                    Require(!duel.TryPrepareRound(round, stage) && !duel.TryPrepareRound(round + 1, -1), "Invalid transition metadata accepted.");
                }
            }
            Require(duel.TryDeclareVictory(), "Victory entry.");
            TestPause(duel, game);
            CheckTerminal(duel);
            Require(rounds == 4 && countdowns == 4 && fires == 4 && finishes == 4 && victories == 1, "Events duplicated or lost across pause/round states.");
        }

        private static void PrepareFire(DuelManager duel)
        {
            Require(duel.TryPrepareRound(1, 0) && duel.TryStartCountdown() && duel.TryAllowFire(), "FIRE setup.");
        }

        private static void CheckTerminal(DuelManager duel)
        {
            Require(!duel.IsFireAllowed && !duel.TryBeginDuel() && !duel.TryPrepareRound(1, 0) &&
                    !duel.TryStartCountdown() && !duel.TryAllowFire() && !duel.TryBeginResolution() &&
                    !duel.TryBeginTransition() && !duel.TryDeclareVictory() && !duel.TryDeclareDefeat(), "Terminal state accepted gameplay transition.");
        }

        private static void TestPause(DuelManager duel, GameManager game)
        {
            GameState before = duel.CurrentState;
            float scale = Time.timeScale;
            int stateEvents = 0;
            Action<GameState, GameState> observe = (previous, current) =>
            {
                Require(duel.CurrentState == current && previous != current, "State event reports stale state.");
                stateEvents++;
            };
            duel.OnStateChanged += observe;
            game.SetPaused(true);
            Require(duel.CurrentState == GameState.Paused && !duel.IsFireAllowed && Time.timeScale == 0f, "Pause failed from " + before);
            Require(!duel.TryBeginDuel() && !duel.TryPrepareRound(1, 0) && !duel.TryStartCountdown() &&
                    !duel.TryAllowFire() && !duel.TryBeginResolution() && !duel.TryBeginTransition() &&
                    !duel.TryDeclareVictory() && !duel.TryDeclareDefeat(), "Paused state accepted transition.");
            game.SetPaused(true);
            game.SetPaused(false);
            Require(duel.CurrentState == before && Mathf.Approximately(Time.timeScale, scale), "Resume failed from " + before);
            Require(stateEvents == 2, "Duplicate pause/resume state events.");
            duel.OnStateChanged -= observe;
        }

        private static void TestPauseDuringEvent(DuelManager duel, GameManager game)
        {
            int countdowns = 0;
            bool recursiveAccepted = false;
            Action<GameState, GameState> listener = (previous, next) =>
            {
                if (next != GameState.Countdown) return;
                recursiveAccepted |= duel.TryAllowFire();
                game.SetPaused(true);
            };
            duel.OnStateChanged += listener;
            duel.OnCountdownStarted += () => countdowns++;
            Require(duel.TryPrepareRound(1, 0) && duel.TryStartCountdown(), "Event-driven pause setup.");
            Require(!recursiveAccepted && duel.CurrentState == GameState.Paused && !duel.IsFireAllowed,
                "Pause requested during event delivery was lost, or nested transition accepted.");
            duel.OnStateChanged -= listener;
            game.SetPaused(false);
            Require(duel.CurrentState == GameState.Countdown && countdowns == 1, "Event-driven resume replayed countdown signal.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            SessionState.SetInt(Key + "assertions", SessionState.GetInt(Key + "assertions", 0) + 1);
        }

        private static void OnLog(string message, string trace, LogType type)
        {
            if (SessionState.GetBool(Key + "running", false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                SessionState.SetString(Key + "error", message + "\n" + trace);
        }

        private static void OnPlayChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key + "running", false))
                Finish(SessionState.GetString(Key + "error", ""));
        }

        private static void Finish(string error)
        {
            SessionState.SetBool(Key + "running", false);
            bool passed = string.IsNullOrEmpty(error) && SessionState.GetInt(Key + "stage", 0) == 6;
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/phase3-validation.txt", "Unity: " + Application.unityVersion + "\nResult: " + (passed ? "PASS" : "FAIL") +
                "\nAssertions: " + SessionState.GetInt(Key + "assertions", 0) + "\nScene flow stage: " + SessionState.GetInt(Key + "stage", 0) + "\n" + error);
            if (passed) Debug.Log("LAST STEP PHASE 3 VALIDATION PASSED: state guards, events, pause, outcomes and scene lifetime.");
            else Debug.LogError("LAST STEP PHASE 3 VALIDATION FAILED: " + error);
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
            else if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }
    }
}

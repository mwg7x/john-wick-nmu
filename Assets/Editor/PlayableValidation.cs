using System;
using System.IO;
using System.Linq;
using LastStep.AI;
using LastStep.Audio;
using LastStep.Core;
using LastStep.Player;
using LastStep.UI;
using LastStep.Utilities;
using LastStep.VR;
using LastStep.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace LastStep.Editor
{
    [InitializeOnLoad]
    public static class PlayableValidation
    {
        private const string Key = "LastStep.PlayableValidation.";
        private static GameManager persistent;
        private static DuelManager duel;
        private static DuelPositionMarkers markers;
        private static WeaponController playerWeapon;
        private static WeaponController enemyWeapon;
        private static Health playerHealth;
        private static Health enemyHealth;
        private static FalseStartManager falseStarts;
        private static int aiShots;

        static PlayableValidation()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key + "running", false))
                    Finish(SessionState.GetString(Key + "error", ""));
            };
            SceneManager.sceneLoaded += SceneLoaded;
            Application.logMessageReceived += (message, trace, type) =>
            {
                if (SessionState.GetBool(Key + "running", false) &&
                    (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                    SessionState.SetString(Key + "error", message + "\n" + trace);
            };
        }

        [MenuItem("LAST STEP/Playable/Validate PC Duel")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetInt(Key + "stage", 0);
            SessionState.SetInt(Key + "assertions", 0);
            SessionState.SetBool(Key + "screenshot", false);
            SessionState.SetString(Key + "error", "");
            try
            {
                PlayableSetup.Configure();
                string duelBefore = File.ReadAllText(Phase2Setup.ScenePath);
                string menuBefore = File.ReadAllText("Assets/Scenes/MainMenu.unity");
                PlayableSetup.Configure();
                Require(duelBefore == File.ReadAllText(Phase2Setup.ScenePath) &&
                    menuBefore == File.ReadAllText("Assets/Scenes/MainMenu.unity"), "Playable setup is not idempotent.");
                EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity");
                SessionState.SetBool(Key + "running", true);
                SessionState.SetFloat(Key + "deadline", (float)EditorApplication.timeSinceStartup + 150f);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception error) { Finish(error.ToString()); }
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!SessionState.GetBool(Key + "running", false) || scene.name != "Duel_Staircase") return;
            duel = UnityEngine.Object.FindAnyObjectByType<DuelManager>();
            markers = UnityEngine.Object.FindAnyObjectByType<DuelPositionMarkers>();
            falseStarts = UnityEngine.Object.FindAnyObjectByType<FalseStartManager>();
            if (markers == null) return;
            playerHealth = markers.PlayerStandIn.GetComponent<Health>();
            enemyHealth = markers.EnemyStandIn.GetComponent<Health>();
            playerWeapon = markers.PlayerStandIn.GetComponent<WeaponController>();
            enemyWeapon = markers.EnemyStandIn.GetComponent<WeaponController>();
            aiShots = 0;
            if (duel != null) duel.OnEnemyShot += () => aiShots++;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Key + "running", false)) return;
            string error = SessionState.GetString(Key + "error", "");
            if (error.Length > 0) { Finish(error); return; }
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + "deadline", 0f))
            { Finish("Timed out during playable duel test."); return; }
            if (!EditorApplication.isPlaying || GameManager.Instance == null || GameManager.Instance.IsLoading) return;
            try
            {
                var game = GameManager.Instance;
                var scene = SceneManager.GetActiveScene().name;
                int stage = SessionState.GetInt(Key + "stage", 0);
                if (stage == 0 && scene == "MainMenu")
                {
                    persistent = game;
                    Require(game.DifficultyCount == OpponentRoster.TierCount &&
                        OpponentRoster.Count(0) == 2 && OpponentRoster.Count(1) == 3 &&
                        OpponentRoster.Count(2) == 3 && OpponentRoster.Count(3) == 3,
                        "Four-tier opponent roster is incomplete.");
                    Require(UnityEngine.Object.FindAnyObjectByType<MainMenuController>() != null &&
                        UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null, "Menu UI/EventSystem missing.");
                    Require(GameObject.Find("MainMenuCanvas/Background/Mode") != null, "PC/VR mode choice missing.");
                    Require(UnityEngine.Object.FindAnyObjectByType<MainMenuController>().GetComponent<GraphicRaycaster>() != null,
                        "Menu canvas cannot receive pointer clicks.");
                    Require(GameObject.Find("MainMenuCanvas/Background/PistolSelectButton") != null &&
                        PistolCatalog.Load(0) != null && PistolCatalog.Load(1) != null &&
                        PistolCatalog.Load(2) != null, "Pistol selection menu/assets missing.");
                    SessionState.SetInt(Key + "originalPistol", game.PistolIndex);
                    game.SetPistol(2);
                    Next(1); game.StartDuel(); return;
                }
                if (stage == 10 && scene == "MainMenu")
                {
                    Require(game == persistent && UnityEngine.Object.FindAnyObjectByType<DuelManager>() == null &&
                        UnityEngine.Object.FindAnyObjectByType<MainMenuController>() != null, "Menu return or scene cleanup failed.");
                    game.SetPistol(SessionState.GetInt(Key + "originalPistol", 0));
                    Next(11);
                    EditorApplication.ExitPlaymode();
                    return;
                }
                if (scene != "Duel_Staircase") return;
                if (duel == null || !duel.IsInitialized) return;
                CheckScene(game);
                if (stage == 1 && duel.CurrentState == GameState.Countdown)
                {
                    if (!SessionState.GetBool(Key + "screenshot", false))
                    {
                        Directory.CreateDirectory("Documentation/Previews");
                        CaptureGameplayCamera();
                        SessionState.SetBool(Key + "screenshot", true);
                    }
                    Require(falseStarts.ReportEarlyTrigger() && falseStarts.Offenses == 1 &&
                        duel.CurrentState == GameState.Countdown && !duel.IsFireAllowed, "First false start warning failed.");
                    Next(2);
                }
                else if (stage == 2 && duel.CurrentState == GameState.Fire)
                {
                    Require(FireAt(playerWeapon, enemyHealth, 1.61f), "Player head shot rejected.");
                    Require(!duel.PlayerShotAvailable && !FireAt(playerWeapon, enemyHealth, 1.61f), "Player fired twice in one exchange.");
                    Require(enemyHealth.Current == 0, "Head hit did not defeat enemy.");
                    Next(3);
                }
                else if (stage == 3 && duel.CurrentState == GameState.Victory)
                {
                    Require(playerHealth.IsAlive && !enemyHealth.IsAlive && !duel.IsFireAllowed, "Victory resolution failed.");
                    Require(game.OpponentIndex == 0 && game.OpponentCount > 1, "Tier did not start with its first opponent.");
                    Next(12);
                    Require(game.AdvanceOpponent(), "Could not advance to next roster opponent.");
                }
                else if (stage == 12 && duel.CurrentState == GameState.Countdown)
                {
                    Require(game.OpponentIndex == 1 && game.CurrentOpponent.Name == OpponentRoster.Get(game.DifficultyIndex, 1).Name,
                        "Next duel did not load the correct opponent.");
                    Next(4); game.RestartDuel();
                }
                else if (stage == 4 && duel.CurrentState == GameState.Countdown)
                {
                    Require(game.OpponentIndex == 0, "Restart did not reset the tier sequence.");
                    Require(falseStarts.Offenses == 0, "False-start count leaked across duel restart.");
                    Require(falseStarts.ReportEarlyTrigger() && falseStarts.ReportEarlyTrigger() &&
                        duel.CurrentState == GameState.Defeat, "Second false start did not disqualify player.");
                    Next(5); game.RestartDuel();
                }
                else if (stage == 5 && duel.CurrentState == GameState.Fire)
                {
                    // Keep the normal AI active and verify it consumes its own one shot.
                    Next(6);
                }
                else if (stage == 6 && aiShots > 0)
                {
                    Require(aiShots == 1 && !duel.EnemyShotAvailable, "AI did not take exactly one permitted shot.");
                    Require(markers.EnemyStandIn.GetComponent<EnemyAI>().ReactionTime > 0f, "AI reaction timing missing.");
                    if (playerHealth.IsAlive && duel.CurrentState == GameState.Fire)
                    {
                        Require(duel.PlayerShotAvailable, "Enemy shot consumed player's shot.");
                        Require(FireAt(playerWeapon, enemyHealth, 0.37f), "Surviving player could not answer AI shot.");
                    }
                    Next(7);
                }
                else if (stage == 7 && (duel.CurrentState == GameState.Transition || duel.CurrentState == GameState.Defeat ||
                                         duel.CurrentState == GameState.Victory))
                {
                    Require(aiShots == 1, "AI repeated shot after exchange.");
                    Next(8); game.RestartDuel();
                }
                else if (stage == 8 && duel.CurrentState == GameState.Fire)
                {
                    // Both low-damage shots should leave participants alive and advance to 20 m.
                    markers.EnemyStandIn.GetComponent<EnemyAI>().enabled = false;
                    Require(FireAt(playerWeapon, enemyHealth, 0.37f), "Player leg shot failed.");
                    Require(FireAt(enemyWeapon, playerHealth, 0.37f), "Enemy leg shot failed.");
                    Require(playerHealth.IsAlive && enemyHealth.IsAlive, "Low-damage shots unexpectedly killed participant.");
                    Next(9);
                }
                else if (stage == 9 && duel.CurrentState == GameState.Countdown && duel.CurrentRound == 2)
                {
                    Require(duel.StageIndex == 1 && Mathf.Abs(duel.CurrentDistance - 20f) < 0.01f, "Round did not progress to 20m.");
                    Require(duel.PlayerShotAvailable && duel.EnemyShotAvailable, "Shots did not reset for next exchange.");
                    Require(markers.TryGetStage(1, out var pair) &&
                        Vector3.Distance(markers.PlayerStandIn.position, pair.Player.position) < 0.01f &&
                        Vector3.Distance(markers.EnemyStandIn.position, pair.Enemy.position) < 0.01f, "Participants were not moved to new markers.");
                    Next(10); game.ReturnToMainMenu();
                }
            }
            catch (Exception exception) { Finish(exception.ToString()); }
        }

        private static bool FireAt(WeaponController weapon, Health target, float height)
        {
            if (weapon == null || target == null || Camera.main == null) return false;
            Vector3 origin = Camera.main.transform.position;
            if (weapon == enemyWeapon) origin = markers.EnemyStandIn.position + Vector3.up * 1.4f;
            Vector3 direction = (target.transform.position + Vector3.up * height - origin).normalized;
            return weapon.TryFire(origin, direction);
        }
        private static void CaptureGameplayCamera()
        {
            var camera = Camera.main;
            if (camera == null) return;
            var playerActor = markers != null ? markers.PlayerStandIn : null;
            var playerModel = playerActor != null ? playerActor.Find("VisualRig/ImportedHumanoid") : null;
            var animator = playerModel != null ? playerModel.GetComponent<Animator>() : null;
            var hand = animator != null && animator.avatar != null && animator.avatar.isHuman ?
                animator.GetBoneTransform(HumanBodyBones.RightHand) :
                playerModel?.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "hand_r");
            var pistol = playerActor != null ? playerActor.Find("PistolPlaceholder") : null;
            var mesh = pistol != null ? pistol.Find("SelectedPistolMesh")?.GetComponentInChildren<Renderer>() : null;
            File.WriteAllText("Documentation/Previews/placement.txt", "Hand=" + (hand ? camera.WorldToScreenPoint(hand.position).ToString() : "none") +
                " PistolRoot=" + (pistol ? camera.WorldToScreenPoint(pistol.position).ToString() : "none") +
                " Mesh=" + (mesh ? camera.WorldToScreenPoint(mesh.bounds.center).ToString() : "none") +
                " MeshBounds=" + (mesh ? mesh.bounds.size.ToString() : "none"));
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(1280, 720, 24);
            var frame = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                frame.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                frame.Apply();
                File.WriteAllBytes("Documentation/Previews/gameplay-imported.png", frame.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(frame);
            }
        }

        private static void CheckScene(GameManager game)
        {
            Require(game == persistent && duel.Settings == game.DuelSettings, "Manager/settings identity changed.");
            Require(UnityEngine.Object.FindObjectsByType<DuelManager>(FindObjectsInactive.Include).Length == 1 &&
                UnityEngine.Object.FindObjectsByType<RoundManager>(FindObjectsInactive.Include).Length == 1, "Scene manager duplicated.");
            Require(playerHealth != null && enemyHealth != null && playerWeapon != null && enemyWeapon != null &&
                falseStarts != null && markers.PlayerStandIn.GetComponentInChildren<HitDetector>() != null &&
                markers.EnemyStandIn.GetComponentInChildren<HitDetector>() != null, "Combat references/zones missing.");
            Require(playerWeapon.Data == PistolCatalog.Load(game.PistolIndex) &&
                playerWeapon.Data != enemyWeapon.Data, "Selected pistol was not applied to player combat.");
            Require(UnityEngine.Object.FindAnyObjectByType<AudioManager>() != null &&
                markers.PlayerStandIn.GetComponent<CharacterPresentation>() != null &&
                markers.EnemyStandIn.GetComponent<CharacterPresentation>() != null,
                "Audio or character presentation missing.");
            Require(markers.PlayerStandIn.Find("VisualRig/ImportedHumanoid") != null &&
                markers.EnemyStandIn.Find("VisualRig/ImportedHumanoid") != null,
                "Imported humanoid visuals missing.");
            Require(markers.PlayerStandIn.Find("PistolPlaceholder/SelectedPistolMesh") != null &&
                markers.EnemyStandIn.Find("PistolPlaceholder/SelectedPistolMesh") != null,
                "Imported pistol visuals missing.");
            Require(UnityEngine.Object.FindAnyObjectByType<SkyPresetController>() != null &&
                GameObject.Find("CloudBackdrop") != null,
                "Cinematic sky missing.");
            Require(Resources.Load<Texture2D>("OpponentPortraits/" + game.CurrentOpponent.Name.Replace(' ', '_')) != null,
                "Current opponent portrait missing.");
            Require(UnityEngine.Object.FindAnyObjectByType<DuelHUD>()?.GetComponent<GraphicRaycaster>() != null,
                "Duel HUD cannot receive pointer clicks.");
            Require(UnityEngine.Object.FindObjectsByType<XRPlayerSetup>(FindObjectsInactive.Include).Length == 1 &&
                UnityEngine.Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsInactive.Include).Length == 1,
                "XR rig duplicated or absent.");
        }
        private static void Next(int stage) { SessionState.SetInt(Key + "stage", stage); }
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message);
          SessionState.SetInt(Key + "assertions", SessionState.GetInt(Key + "assertions", 0) + 1); }
        private static void Finish(string error)
        {
            SessionState.SetBool(Key + "running", false);
            bool passed = error.Length == 0 && SessionState.GetInt(Key + "stage", 0) == 11;
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/playable-validation.txt", "Unity: " + Application.unityVersion + "\nResult: " + (passed ? "PASS" : "FAIL") +
                "\nAssertions: " + SessionState.GetInt(Key + "assertions", 0) +
                "\nScene flow stage: " + SessionState.GetInt(Key + "stage", 0) + "\n" + error);
            if (passed) Debug.Log("LAST STEP PLAYABLE VALIDATION PASSED.");
            else Debug.LogError("LAST STEP PLAYABLE VALIDATION FAILED: " + error);
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
            else if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }
    }
}

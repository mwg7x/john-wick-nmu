using System;
using System.Collections;
using System.IO;
using System.Reflection;
using LastStep.Core;
using LastStep.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastStep.Editor
{
    [InitializeOnLoad]
    public static class UpgradeValidation
    {
        private const string Prefix = "LastStep.UpgradeValidation.";
        private static bool reloadLocked;
        public static void RunNMU() { SessionState.SetInt(Prefix + "map", 1); Run(); }
        static UpgradeValidation()
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += (message, trace, type) =>
            {
                if (SessionState.GetBool(Prefix + "running", false) &&
                    (type == LogType.Error || type == LogType.Exception))
                    SessionState.SetString(Prefix + "error", message + "\n" + trace);
            };
        }
        [MenuItem("LAST STEP/Playable/Validate Upgraded Duel")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(Prefix + "running", true);
            SessionState.SetString(Prefix + "error", "");
            SessionState.SetInt(Prefix + "stage", 0);
            SessionState.SetInt(Prefix + "checks", 0);
            SessionState.SetBool(Prefix + "finisherSeen", false);
            SessionState.SetBool(Prefix + "lethalStarted", false);
            SessionState.SetFloat(Prefix + "deadline", (float)EditorApplication.timeSinceStartup + 180f);
            EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity");
            EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if (!SessionState.GetBool(Prefix + "running", false)) return;
            // Packages request define-symbol recompilation on startup. A mid-run
            // assembly reload kills gameplay coroutines and invalidates the test.
            if (EditorApplication.isPlaying && !reloadLocked)
            { EditorApplication.LockReloadAssemblies(); reloadLocked = true; }
            string error = SessionState.GetString(Prefix + "error", "");
            if (error.Length > 0 || EditorApplication.timeSinceStartup > SessionState.GetFloat(Prefix + "deadline", 0f))
            {
                var current = UnityEngine.Object.FindAnyObjectByType<DuelManager>();
                var countdown = UnityEngine.Object.FindAnyObjectByType<CountdownController>();
                Finish(error.Length > 0 ? error : "Validation timed out. Duel=" +
                    (current == null ? "missing" : current.CurrentState + " round=" + current.CurrentRound) +
                    " cue=" + (countdown == null ? "missing" : countdown.CurrentCue + " remaining=" + countdown.RemainingSeconds) +
                    " timescale=" + Time.timeScale + " loading=" + (GameManager.Instance != null && GameManager.Instance.IsLoading) +
                    " playing=" + EditorApplication.isPlaying + " scene=" + SceneManager.GetActiveScene().name +
                    " frame=" + Time.frameCount);
                return;
            }
            if (!EditorApplication.isPlaying || GameManager.Instance == null || GameManager.Instance.IsLoading) return;
            try { Step(); }
            catch (Exception e) { Finish(e.ToString()); }
        }
        private static void Step()
        {
            var game = GameManager.Instance;
            int stage = SessionState.GetInt(Prefix + "stage", 0);
            if (stage == 0 && SceneManager.GetActiveScene().name == "MainMenu")
            {
                UnityEngine.Random.InitState(4617);
                game.SetMap(SessionState.GetInt(Prefix + "map", 0));
                var menu = UnityEngine.Object.FindAnyObjectByType<LastStep.UI.MainMenuController>();
                var background = menu != null ? menu.transform.Find("Background") : null;
                Check(background != null && background.Find("UniversityBrand") != null &&
                    background.Find("NightSkyArt")?.GetComponent<UnityEngine.UI.RawImage>()?.texture != null,
                    "NMU menu branding or original background missing.");
                var weaponPanel = background.Find("WeaponSelectionPanel")?.GetComponent<CanvasGroup>();
                var settingsPanel = background.Find("SettingsPanel")?.GetComponent<CanvasGroup>();
                Check(weaponPanel != null && settingsPanel != null, "Functional menu panels missing.");
                background.Find("WeaponOpenButton")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
                Check(weaponPanel.alpha > .9f && weaponPanel.interactable, "Weapon selection did not open.");
                int originalWeapon = game.PistolIndex;
                weaponPanel.transform.Find("PistolSelectButton")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
                weaponPanel.transform.Find("Equip")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
                Check(game.PistolIndex == (originalWeapon + 1) % PistolCatalog.Count && weaponPanel.alpha < .1f,
                    "Weapon EQUIP did not commit selection.");
                game.SetPistol(originalWeapon);
                background.Find("SettingsOpenButton")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
                Check(settingsPanel.alpha > .9f && settingsPanel.interactable, "Settings did not open.");
                settingsPanel.transform.Find("Close")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
                Check(settingsPanel.alpha < .1f, "Settings did not close.");
                background.Find("OpponentGalleryButton")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
                var gallery = menu.GetComponent<LastStep.UI.OpponentGallery>();
                var galleryPanel = menu.transform.Find("GalleryPanel")?.GetComponent<CanvasGroup>();
                Check(gallery != null && galleryPanel != null && galleryPanel.alpha > .9f,
                    "Opponents button did not open gallery.");
                galleryPanel.transform.Find("Close")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
                Check(galleryPanel.alpha < .1f && background.Find("Quit")?.GetComponent<UnityEngine.UI.Button>() != null,
                    "Opponent gallery close or Exit button missing.");
                var styleButton = background.Find("DuelStyleButton")?.GetComponent<UnityEngine.UI.Button>();
                Check(styleButton != null, "Campaign / Quick Duel selector missing.");
                bool originalQuick = game.QuickDuel;
                styleButton.onClick.Invoke();
                Check(game.QuickDuel != originalQuick, "Duel style selector did not change mode.");
                styleButton.onClick.Invoke();
                Check(game.QuickDuel == originalQuick, "Duel style selector did not restore mode.");
                game.SetQuickDuel(false);
                Check(game.DifficultyCount == 4 && PistolCatalog.Count == 3, "Roster or pistols missing.");
                Check(WeaponController.TryGetNearMiss(Vector3.zero, Vector3.forward * 10f,
                    new Vector3(.7f, 0f, 5f), out _) &&
                    !WeaponController.TryGetNearMiss(Vector3.zero, Vector3.forward * 10f,
                        new Vector3(3f, 0f, 5f), out _),
                    "Near-miss geometry accepted a far bullet or rejected a close pass.");
                Check(Resources.Load<AudioClip>("Audio/KenneyImpact/footstep_concrete_000") != null &&
                    Resources.Load<AudioClip>("Audio/KenneyImpact/impactMining_000") != null,
                    "Licensed Kenney footsteps or impacts missing.");
                Check(game.GetComponent<LastStep.Audio.AdaptiveMusic>() != null &&
                    Resources.Load<AudioClip>("Audio/SRG774/title") != null &&
                    Resources.Load<AudioClip>("Audio/SRG774/urgent") != null,
                    "Adaptive music component or licensed tracks missing.");
                int originalTier = game.DifficultyIndex;
                for (int i = 0; i < 4; i++)
                {
                    game.SetDifficulty(i);
                    Check(game.SelectedDifficulty != null && LastStep.AI.OpponentRoster.Count(i) > 0 &&
                        !string.IsNullOrEmpty(LastStep.AI.OpponentRoster.Get(i, 0).Name), "Difficulty tier missing.");
                }
                game.SetDifficulty(originalTier);
                for (int i = 0; i < 3; i++)
                    Check(PistolCatalog.Load(i) != null && Resources.Load<GameObject>("Pistols/Pistol" + i) != null &&
                        Resources.Load<Texture2D>("WeaponPreviews/Pistol" + i) != null,
                        "Pistol model or preview missing: " + i);
                foreach (int index in new[] { 0, 1, 2 })
                {
                    var prefab = Resources.Load<GameObject>("Pistols/Pistol" + index);
                    Check(prefab != null && prefab.transform.childCount > 0 &&
                        Quaternion.Angle(prefab.transform.GetChild(0).localRotation,
                            index == 2 ? Quaternion.Euler(0f, -90f, 0f) : Quaternion.identity) < 1f,
                        "Pistol " + index + " barrel axis was not corrected toward +Z.");
                }
                game.SetQuickDuel(true);
                Next(19); background.Find("Start Duel")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke(); return;
            }
            if (stage == 19 && SceneManager.GetActiveScene().name == game.SelectedDuelScene)
            {
                Check(game.QuickDuel && game.IsPractice && game.OpponentCount == 1 &&
                    !string.IsNullOrEmpty(game.CurrentOpponent.Name),
                    "Quick Duel did not load one named opponent from the selected tier.");
                game.SetQuickDuel(false);
                Next(20); game.ReturnToMainMenu(); return;
            }
            if (stage == 20 && SceneManager.GetActiveScene().name == "MainMenu")
            {
                Check(!game.IsPractice && !game.QuickDuel,
                    "Campaign did not reset single-opponent state.");
                Next(1);
                UnityEngine.Object.FindAnyObjectByType<LastStep.UI.MainMenuController>()?.transform
                    .Find("Background/Start Duel")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
                return;
            }
            if (stage == 18 && SceneManager.GetActiveScene().name == "MainMenu")
            { Finish(""); return; }
            if (SceneManager.GetActiveScene().name != game.SelectedDuelScene) return;
            var duel = UnityEngine.Object.FindAnyObjectByType<DuelManager>();
            var coin = UnityEngine.Object.FindAnyObjectByType<CoinTossFlow>();
            var starts = UnityEngine.Object.FindAnyObjectByType<FalseStartManager>();
            var rounds = UnityEngine.Object.FindAnyObjectByType<RoundManager>();
            var markers = UnityEngine.Object.FindAnyObjectByType<DuelPositionMarkers>();
            if (duel == null || coin == null || starts == null || rounds == null || markers == null || !duel.IsInitialized) return;
            var player = markers.PlayerStandIn.GetComponent<Health>();
            var enemy = markers.EnemyStandIn.GetComponent<Health>();
            var weapon = markers.PlayerStandIn.GetComponent<WeaponController>();
            if (stage == 1 || stage == 4 || stage == 7)
            {
                if (!coin.IsAwaitingSide) return;
                Check(duel.CurrentState == GameState.Intro, "Coin toss did not hold Intro.");
                Check(coin.ChooseSide(0), "Could not choose Heads.");
                Next(stage + 1); return;
            }
            if (stage == 2 || stage == 5 || stage == 8)
            {
                if (!coin.IsAwaitingWeapon) return;
                bool chose = coin.ChooseWeapon(0) || coin.ChooseWeapon(1) || coin.ChooseWeapon(2);
                Check(chose, "Could not choose available pistol.");
                Check(coin.CoinResult == 0 || coin.CoinResult == 1, "Coin result invalid.");
                Next(stage + 1); return;
            }
            if (stage == 3 && duel.CurrentState == GameState.Countdown)
            {
                Check(!weapon.TryFire(Camera.main.transform.position, Camera.main.transform.forward),
                    "Countdown weapon fire was incorrectly accepted.");
                Check(duel.CurrentState == GameState.Defeat && !duel.IsFireAllowed,
                    "First false start did not immediately forfeit.");
                Next(4); game.RestartDuel(); return;
            }
            if (stage == 6 && duel.CurrentState == GameState.Countdown)
            {
                Check(starts.ReportEnemyEarlyTrigger(), "Enemy early fire was not detected.");
                Check(duel.CurrentState == GameState.Victory && !duel.IsFireAllowed,
                    "Enemy false start did not immediately award victory.");
                Next(7); game.RestartDuel(); return;
            }
            if (stage == 9 && duel.CurrentState == GameState.Countdown)
            {
                Check(player.Maximum == 500 && enemy.Maximum == 500, "500 HP encounter setup failed.");
                Check(Mathf.Abs(duel.CurrentDistance - 50f) < .01f, "Round 1 is not 50m.");
                Check(Mathf.Abs(Vector3.Distance(player.transform.position, enemy.transform.position) - 50f) < .2f,
                    "Round 1 world distance wrong.");
                Check(weapon.Data == PistolCatalog.Load(game.PistolIndex), "Selected pistol was not equipped.");
                Check(weapon.Ammunition == DuelManager.ShotsPerRound &&
                    markers.EnemyStandIn.GetComponent<WeaponController>().Ammunition == DuelManager.ShotsPerRound &&
                    !weapon.TryReload(), "Three-shot round budget or no-reload rule failed.");
                var ammoText = UnityEngine.Object.FindAnyObjectByType<LastStep.UI.DuelHUD>()?.transform.Find("AmmoLabel")?.GetComponent<TMPro.TMP_Text>();
                var roundText = UnityEngine.Object.FindAnyObjectByType<LastStep.UI.DuelHUD>()?.transform.Find("RoundLabel")?.GetComponent<TMPro.TMP_Text>();
                Check(ammoText != null && ammoText.text.Contains("AMMO 3/3") && ammoText.text.Contains("▮▮▮") &&
                    roundText != null && roundText.text.Contains("50M"), "Round 1 HUD did not show three shots.");
                Check(weapon.Data.GetDamage(HitZone.Head) == weapon.Data.GetDamage(HitZone.Chest) * 2,
                    "Headshot multiplier wrong.");
                Time.timeScale = 5f;
                Next(10); return;
            }
            if (stage == 10 && duel.CurrentState == GameState.Fire)
            {
                markers.EnemyStandIn.GetComponent<LastStep.AI.EnemyAI>().enabled = false;
                var head = enemy.transform.Find("HitZones/Head");
                Check(head != null && Camera.main != null, "Animated head hitbox missing.");
                int before = enemy.Current;
                Vector3 origin = Camera.main.transform.position;
                Check(weapon.TryFire(origin, (head.position - origin).normalized), "Legal shot was rejected after DRAW.");
                Check(enemy.Current == before - weapon.Data.GetDamage(HitZone.Head) && weapon.Headshots == 1,
                    "Actual headshot did not apply 2x damage.");
                Check(weapon.Ammunition == 2 && duel.PlayerRoundAmmo == 2, "Round ammo did not decrement exactly once.");
                var motion = player.GetComponentInChildren<LastStep.Utilities.ImportedCharacterMotion>();
                var pistolRoot = player.transform.Find("PistolPlaceholder");
                Check(motion != null && motion.RightHand != null && pistolRoot != null &&
                    Vector3.Distance(motion.RightHand.position, pistolRoot.position) < .16f,
                    "Pistol is not attached close to the animated right hand.");
                var enemyGun = markers.EnemyStandIn.GetComponent<WeaponController>();
                var chest = player.transform.Find("HitZones/Chest");
                var enemyMuzzle = markers.EnemyStandIn.Find("Muzzle");
                Check(enemyGun != null && chest != null && enemyMuzzle != null, "Enemy attack rig missing.");
                int playerBefore = player.Current;
                SessionState.SetInt(Prefix + "enemyDamage", enemyGun.Data.GetDamage(HitZone.Chest));
                Check(enemyGun.TryFire(enemyMuzzle.position, (chest.position - enemyMuzzle.position).normalized),
                    "Enemy could not fire during DRAW.");
                Check(player.Current < playerBefore, "Enemy bullet did not reduce player health.");
                Check(motion.HitImpulse > .5f, "Character hit reaction did not trigger.");
                Check(UnityEngine.Object.FindAnyObjectByType<LastStep.UI.DamageVision>() != null,
                    "Player damage vision missing.");
                for (int remaining = 1; remaining >= 0; remaining--)
                {
                    typeof(WeaponController).GetField("nextShotTime", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.SetValue(weapon, 0f);
                    origin = Camera.main.transform.position;
                    Check(weapon.TryFire(origin, (head.position - origin).normalized),
                        "One of three legal round shots was rejected.");
                    Check(weapon.Ammunition == remaining && duel.PlayerRoundAmmo == remaining,
                        "Round ammo and weapon display diverged.");
                }
                Check(!weapon.TryFire(origin, (head.position - origin).normalized) && weapon.Ammunition == 0,
                    "Fourth shot was not blocked at zero ammo.");
                Capture();
                Expire(rounds);
                Next(11); return;
            }
            if (stage == 11 && duel.CurrentRound == 2 && duel.CurrentState == GameState.Countdown)
            { Next(12); return; }
            if (stage == 14 && duel.CurrentRound == 3 && duel.CurrentState == GameState.Countdown)
            { Next(15); return; }
            if ((stage == 11 || stage == 14) && duel.CurrentState == GameState.Transition)
            {
                if (stage == 11)
                {
                    var ammoText = UnityEngine.Object.FindAnyObjectByType<LastStep.UI.DuelHUD>()?.transform.Find("AmmoLabel")?.GetComponent<TMPro.TMP_Text>();
                    Check(ammoText != null && ammoText.text.Contains("AMMO 0/3") && ammoText.text.Contains("▯▯▯"),
                        "Empty round 1 HUD disagreed with authoritative ammo.");
                }
                int nextIndex = stage == 11 ? 1 : 2;
                Check(markers.TryGetStage(nextIndex, out var target), "Checkpoint marker missing.");
                var actor = markers.PlayerStandIn;
                if (Vector3.Distance(actor.position, target.Player.position) > 1.4f)
                {
                    // Move the stand-in along the checkpoint route without invoking keyboard input.
                    var motor = actor.GetComponent<CharacterController>();
                    motor.enabled = false;
                    actor.position = Vector3.MoveTowards(actor.position, target.Player.position, .4f);
                    motor.enabled = true;
                    Physics.SyncTransforms();
                    File.WriteAllText("Logs/checkpoint-debug.txt", "stage=" + stage + " player=" + actor.position +
                        " destination=" + target.Player.position + " remaining=" +
                        Vector3.Distance(actor.position, target.Player.position) + " frame=" + Time.frameCount);
                    return;
                }
                Next(stage + 1); return;
            }
            if (stage == 12 && duel.CurrentState == GameState.Countdown)
            {
                Check(duel.CurrentRound == 2 && duel.CurrentDistance == 30f, "30m round did not begin.");
                Check(player.Current == 500 - SessionState.GetInt(Prefix + "enemyDamage", -1) &&
                    enemy.Current == 500 - 3 * weapon.Data.GetDamage(HitZone.Head),
                    "Headshot damage did not persist between rounds.");
                Check(weapon.Ammunition == 3 && markers.EnemyStandIn.GetComponent<WeaponController>().Ammunition == 3,
                    "Round 2 did not restore exactly three shots.");
                var ammoText = UnityEngine.Object.FindAnyObjectByType<LastStep.UI.DuelHUD>()?.transform.Find("AmmoLabel")?.GetComponent<TMPro.TMP_Text>();
                var roundText = UnityEngine.Object.FindAnyObjectByType<LastStep.UI.DuelHUD>()?.transform.Find("RoundLabel")?.GetComponent<TMPro.TMP_Text>();
                Check(ammoText != null && ammoText.text.Contains("AMMO 3/3") && roundText != null && roundText.text.Contains("30M"),
                    "Round 2 HUD did not update to restored ammo.");
                Check(!duel.TryPrepareRound(2, 1) && weapon.Ammunition == 3,
                    "Duplicate round preparation reset ammo.");
                var hud = UnityEngine.Object.FindAnyObjectByType<LastStep.UI.DuelHUD>();
                var fill = hud != null ? hud.transform.Find("PlayerHealthBar/Fill")?.GetComponent<UnityEngine.UI.Image>() : null;
                Check(fill != null && fill.type == UnityEngine.UI.Image.Type.Filled &&
                    Mathf.Abs(fill.fillAmount - (float)player.Current / player.Maximum) < .001f,
                    "Player health bar did not show damage. fill=" + (fill == null ? "missing" : fill.fillAmount.ToString()) +
                    " hp=" + player.Current + "/" + player.Maximum + " type=" + (fill == null ? "missing" : fill.type.ToString()));
                Action roundTwoHandler = null;
                roundTwoHandler = () =>
                {
                    duel.OnFireAllowed -= roundTwoHandler;
                    File.AppendAllText("Logs/upgrade-stage-trace.txt", "round2 FIRE at " + Time.time + "\n");
                    markers.EnemyStandIn.GetComponent<LastStep.AI.EnemyAI>().enabled = false;
                    Expire(rounds);
                };
                duel.OnFireAllowed += roundTwoHandler;
                Next(13); return;
            }
            if (stage == 13 && duel.CurrentState == GameState.Transition && duel.CurrentRound == 2)
            {
                Next(14); return;
            }
            if (stage == 15 && duel.CurrentState == GameState.Countdown)
            {
                Time.timeScale = 1f;
                Check(duel.CurrentRound == 3 && duel.CurrentDistance == 10f, "10m round did not begin.");
                Check(weapon.Ammunition == 3 && duel.PlayerRoundAmmo == 3,
                    "Round 3 did not restore exactly three shots.");
                var ammoText = UnityEngine.Object.FindAnyObjectByType<LastStep.UI.DuelHUD>()?.transform.Find("AmmoLabel")?.GetComponent<TMPro.TMP_Text>();
                var roundText = UnityEngine.Object.FindAnyObjectByType<LastStep.UI.DuelHUD>()?.transform.Find("RoundLabel")?.GetComponent<TMPro.TMP_Text>();
                Check(ammoText != null && ammoText.text.Contains("AMMO 3/3") && roundText != null && roundText.text.Contains("10M"),
                    "Round 3 HUD did not update to restored ammo.");
                duel.OnStateChanged += (previous, current) =>
                { if (current == GameState.Finisher) SessionState.SetBool(Prefix + "finisherSeen", true); };
                Next(16); return;
            }
            if (stage == 16 && duel.CurrentState == GameState.Fire && !SessionState.GetBool(Prefix + "lethalStarted", false))
            {
                SessionState.SetBool(Prefix + "lethalStarted", true);
                markers.EnemyStandIn.GetComponent<LastStep.AI.EnemyAI>().enabled = false;
                rounds.StartCoroutine(LethalShot(weapon, enemy));
                return;
            }
            if (stage == 16 && duel.CurrentState == GameState.Victory)
            {
                Check(player.IsAlive && !enemy.IsAlive && SessionState.GetBool(Prefix + "finisherSeen", false) &&
                    Mathf.Approximately(Time.timeScale, 1f),
                    "Lethal bullet did not enter Finisher and award victory.");
                Next(18); game.ReturnToMainMenu(); return;
            }
            if (stage == 18 && SceneManager.GetActiveScene().name == "MainMenu") Finish("");
        }
        private static void Expire(RoundManager manager)
        {
            typeof(RoundManager).GetField("combatEndTime", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(manager, Time.time - .1f);
        }
        private static IEnumerator LethalShot(WeaponController weapon, Health enemy)
        {
            yield return null;
            int headDamage = weapon.Data.GetDamage(HitZone.Head);
            enemy.ApplyDamage(Mathf.Max(0, enemy.Current - headDamage));
            var head = enemy.transform.Find("HitZones/Head");
            var origin = Camera.main.transform.position;
            bool shot = head != null && weapon.TryFire(origin, (head.position - origin).normalized);
            File.AppendAllText("Logs/upgrade-stage-trace.txt", "lethal shot=" + shot +
                " enemyHP=" + enemy.Current + "\n");
        }
        private static void Capture()
        {
            var camera = Camera.main;
            if (camera == null) return;
            var target = new RenderTexture(1280, 720, 24);
            var old = camera.targetTexture;
            var oldActive = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Documentation/Previews");
            File.WriteAllBytes("Documentation/Previews/upgraded-duel.png", image.EncodeToPNG());
            camera.targetTexture = old;
            RenderTexture.active = oldActive;
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
        }
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            SessionState.SetInt(Prefix + "checks", SessionState.GetInt(Prefix + "checks", 0) + 1);
        }
        private static void Next(int stage)
        {
            SessionState.SetInt(Prefix + "stage", stage);
            Directory.CreateDirectory("Logs");
            File.AppendAllText("Logs/upgrade-stage-trace.txt", stage + " at " + Time.time + "\n");
        }
        private static void Finish(string error)
        {
            if (!SessionState.GetBool(Prefix + "running", false)) return;
            SessionState.SetBool(Prefix + "running", false);
            Time.timeScale = 1f;
            bool pass = error.Length == 0 && SessionState.GetInt(Prefix + "stage", 0) == 18;
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/upgrade-validation.txt", "Result: " + (pass ? "PASS" : "FAIL") +
                "\nStage: " + SessionState.GetInt(Prefix + "stage", 0) +
                "\nChecks: " + SessionState.GetInt(Prefix + "checks", 0) + "\n" + error);
            if (pass) Debug.Log("LAST STEP UPGRADE VALIDATION PASSED");
            else Debug.LogError("LAST STEP UPGRADE VALIDATION FAILED: " + error);
            if (reloadLocked) { reloadLocked = false; EditorApplication.UnlockReloadAssemblies(); }
            if (Application.isBatchMode) EditorApplication.Exit(pass ? 0 : 1);
            else if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }
    }
}

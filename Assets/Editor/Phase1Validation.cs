using System;
using System.IO;
using System.Linq;
using LastStep.Core;
using LastStep.AI;
using LastStep.Weapons;
using Unity.CodeEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LastStep.Editor
{
    // Editor-only integration check. SessionState survives the Play mode domain reload.
    [InitializeOnLoad]
    public static class Phase1Validation
    {
        private const string Prefix = "LastStep.Validation.";

        static Phase1Validation()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Application.logMessageReceived += OnLog;
        }

        public static void Run()
        {
            try
            {
                ConfigureCodeEditor();
                Require(Application.unityVersion == "6000.6.5f1", "Unexpected Unity version.");
                Require(EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64, "Windows x64 must be active.");
                Require(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset, "URP is not assigned.");
                var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
                Require(settings.FindProperty("activeInputHandler").intValue == 1, "New Input System is not selected.");
                Require(InputBackendEnabled(), "Restart Unity to activate the Input System backend.");
                var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/LastStep.inputactions");
                Require(input != null && input.actionMaps.Count == 2, "Input asset did not import.");
                foreach (string action in new[] { "Player/Aim", "Player/Fire", "Player/Pause", "Player/Look", "VR/Fire", "VR/Grab", "VR/Turn" })
                    Require(input.FindAction(action) != null, "Missing input action: " + action);
                Require(AssetDatabase.LoadAssetAtPath<DuelSettings>("Assets/ScriptableObjects/DuelSettings.asset") != null, "Missing duel asset.");
                Require(AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/ScriptableObjects/Pistol.asset") != null, "Missing weapon asset.");
                foreach (string difficulty in new[] { "Easy", "Normal", "Hard" })
                    Require(AssetDatabase.LoadAssetAtPath<DifficultySettings>("Assets/ScriptableObjects/" + difficulty + ".asset") != null, "Missing difficulty asset.");
                string[] scenePaths = { "Assets/Scenes/Boot.unity", "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Duel_Staircase.unity" };
                Require(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).SequenceEqual(scenePaths), "Incorrect build scene order.");
                foreach (string path in scenePaths)
                {
                    var scene = EditorSceneManager.OpenScene(path);
                    foreach (var root in scene.GetRootGameObjects())
                        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                            Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing scene script.");
                }
                SessionState.SetInt(Prefix + "passes", 0);
                SessionState.SetString(Prefix + "error", "");
                SessionState.SetBool(Prefix + "running", true);
                // Unity 6.6 cannot create its first search index while entering Play mode.
                // Initialize it in Edit mode before the startup callback and domain reload race.
                Require(UnityEditor.Search.SearchService.EnumerateDatabases().Any(), "Search database initialization failed.");
                BeginPlay();
            }
            catch (Exception error) { Finish(error.ToString()); }
        }

        private static void ConfigureCodeEditor()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-lastStepCodeEditor");
            if (index >= 0 && index + 1 < args.Length)
            {
                Require(File.Exists(args[index + 1]), "VS Code executable does not exist.");
                CodeEditor.SetExternalScriptEditor(args[index + 1]);
            }
            Require(Path.GetFileName(CodeEditor.CurrentEditorInstallation).Equals("Code.exe", StringComparison.OrdinalIgnoreCase), "VS Code is not the selected external editor.");
            CodeEditor.CurrentEditor.SyncAll();
            Debug.Log("LAST STEP: external editor = " + CodeEditor.CurrentEditorInstallation);
        }

        private static void BeginPlay()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity");
            SessionState.SetFloat(Prefix + "deadline", (float)EditorApplication.timeSinceStartup + 90f);
            SessionState.SetBool(Prefix + "observed", false);
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Prefix + "running", false)) return;
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Prefix + "deadline", float.MaxValue))
            { Finish("Timed out waiting for Boot -> MainMenu."); return; }
            if (!EditorApplication.isPlaying || SessionState.GetBool(Prefix + "observed", false)) return;
            if (SceneManager.GetActiveScene().name != "MainMenu") return;
            var manager = GameManager.Instance;
            if (manager == null || manager.IsLoading) return;
            try
            {
                Require(UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include).Length == 1, "Duplicate GameManager.");
                Require(manager.DuelSettings != null && manager.SelectedDifficulty != null, "Missing runtime settings references.");
                float scale = Time.timeScale;
                manager.SetPaused(true);
                Require(manager.IsPaused && Time.timeScale == 0f, "Pause failed.");
                manager.SetPaused(false);
                Require(!manager.IsPaused && Time.timeScale == scale, "Unpause failed.");
                SessionState.SetBool(Prefix + "observed", true);
                SessionState.SetInt(Prefix + "passes", SessionState.GetInt(Prefix + "passes", 0) + 1);
                Debug.Log("LAST STEP VALIDATION: Boot -> MainMenu, one persistent manager, references and pause passed.");
                EditorApplication.ExitPlaymode();
            }
            catch (Exception error) { Finish(error.ToString()); }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Prefix + "running", false) || state != PlayModeStateChange.EnteredEditMode) return;
            string error = SessionState.GetString(Prefix + "error", "");
            if (!string.IsNullOrEmpty(error)) { Finish(error); return; }
            if (SessionState.GetInt(Prefix + "passes", 0) < 2) EditorApplication.delayCall += BeginPlay;
            else Finish("");
        }

        private static void OnLog(string condition, string trace, LogType type)
        {
            if (SessionState.GetBool(Prefix + "running", false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                SessionState.SetString(Prefix + "error", condition + "\n" + trace);
        }

        private static bool InputBackendEnabled()
        {
#if ENABLE_INPUT_SYSTEM
            return true;
#else
            return false;
#endif
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Finish(string error)
        {
            SessionState.SetBool(Prefix + "running", false);
            Directory.CreateDirectory("Logs");
            bool passed = string.IsNullOrEmpty(error);
            File.WriteAllText("Logs/phase1-validation.txt", "Unity: " + Application.unityVersion + "\nResult: " + (passed ? "PASS" : "FAIL") +
                "\nBoot -> MainMenu Play mode passes: " + SessionState.GetInt(Prefix + "passes", 0) + "\n" + error);
            if (passed) Debug.Log("LAST STEP PHASE 1 VALIDATION PASSED (two Play mode sessions).");
            else Debug.LogError("LAST STEP PHASE 1 VALIDATION FAILED: " + error);
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
            else if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }
    }
}

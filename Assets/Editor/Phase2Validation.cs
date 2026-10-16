using System;
using System.IO;
using System.Linq;
using System.Text;
using LastStep.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LastStep.Editor
{
    [InitializeOnLoad]
    public static class Phase2Validation
    {
        private const string Key = "LastStep.Phase2Validation.";
        private static GameManager expectedManager;
        private static int captureReadyFrame = -1;

        static Phase2Validation()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayChanged;
            Application.logMessageReceived += OnLog;
        }

        [MenuItem("LAST STEP/Phase 2/Validate Greybox and Scene Flow")]
        public static void Run()
        {
            try
            {
                Require(Application.unityVersion == "6000.6.5f1", "Incorrect editor version.");
                Require(UnityEditor.Search.SearchService.EnumerateDatabases().Any(), "Search index initialization failed.");
                Phase2Setup.Configure();
                // The second setup must preserve the generated scene instead of duplicating it.
                string before = File.ReadAllText(Phase2Setup.ScenePath);
                Phase2Setup.Configure();
                Require(before == File.ReadAllText(Phase2Setup.ScenePath), "Repeated setup modified existing scene.");
                ValidateArena();
                EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity");
                SessionState.SetInt(Key + "stage", 0);
                SessionState.SetString(Key + "error", "");
                SessionState.SetBool(Key + "running", true);
                SessionState.SetFloat(Key + "deadline", (float)EditorApplication.timeSinceStartup + 120f);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception error) { Finish(error.ToString()); }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Key + "running", false)) return;
            string loggedError = SessionState.GetString(Key + "error", "");
            if (!string.IsNullOrEmpty(loggedError)) { Finish(loggedError); return; }
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + "deadline", 0f))
            { Finish("Timed out waiting for Phase 2 scene-flow test."); return; }
            if (!EditorApplication.isPlaying) return;
            var manager = GameManager.Instance;
            if (manager == null || manager.IsLoading) return;
            try
            {
                string scene = SceneManager.GetActiveScene().name;
                int stage = SessionState.GetInt(Key + "stage", 0);
                if (stage == 0 && scene == "MainMenu")
                {
                    expectedManager = manager;
                    captureReadyFrame = -1;
                    CheckManager(manager);
                    SessionState.SetInt(Key + "stage", 1);
                    manager.StartDuel();
                }
                else if (stage == 1 && scene == "Duel_Staircase" && RenderPipelineManager.currentPipeline != null)
                {
                    // Allow the newly loaded scene to render, rather than capturing the previous camera's first frame.
                    if (captureReadyFrame < 0) captureReadyFrame = Time.frameCount + 3;
                    if (Time.frameCount < captureReadyFrame) return;
                    CheckManager(manager);
                    ValidateArena();
                    CaptureViews();
                    SessionState.SetInt(Key + "stage", 2);
                    manager.ReturnToMainMenu();
                }
                else if (stage == 2 && scene == "MainMenu")
                {
                    CheckManager(manager);
                    SessionState.SetInt(Key + "stage", 3);
                    manager.StartDuel();
                }
                else if (stage == 3 && scene == "Duel_Staircase")
                {
                    CheckManager(manager);
                    ValidateArena();
                    SessionState.SetInt(Key + "stage", 4);
                    Debug.Log("LAST STEP PHASE 2: Boot -> MainMenu -> Duel -> MainMenu -> Duel passed.");
                    EditorApplication.ExitPlaymode();
                }
            }
            catch (Exception error) { Finish(error.ToString()); }
        }

        private static void CheckManager(GameManager manager)
        {
            Require(manager == expectedManager, "Persistent manager changed during scene flow.");
            Require(UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include).Length == 1, "Duplicate manager.");
            Require(manager.DuelSettings != null && manager.SelectedDifficulty != null, "Missing manager settings.");
            float previous = Time.timeScale;
            manager.SetPaused(true);
            Require(manager.IsPaused && Time.timeScale == 0f, "Pause failed.");
            manager.SetPaused(false);
            Require(!manager.IsPaused && Time.timeScale == previous, "Resume failed.");
        }

        private static DuelPositionMarkers ValidateArena()
        {
            Physics.SyncTransforms();
            var all = UnityEngine.Object.FindObjectsByType<DuelPositionMarkers>(FindObjectsInactive.Include);
            Require(all.Length == 1, "Expected exactly one marker set.");
            var markers = all[0];
            Require(markers.Settings != null && markers.StageCount == 3, "Expected shared settings and three stages.");
            Require(markers.PlayerStandIn != null && markers.EnemyStandIn != null, "Missing scale stand-ins.");
            var root = markers.transform.root;
            foreach (string child in new[] { "StairsAndLandings", "WallsAndRailings", "LampsAndPillars", "DistantCityGreybox", "GreyboxLighting" })
                Require(root.Find(child) != null, "Missing environment group: " + child);
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script: " + transform.name);
            var renderers = root.GetComponentsInChildren<Renderer>();
            Require(renderers.Length < 600, "Greybox exceeds renderer budget.");
            Require(root.GetComponentsInChildren<Light>().Length <= 3, "Greybox exceeds light budget.");
            Require(root.GetComponentsInChildren<Light>().Count(l => l.shadows != LightShadows.None) == 1, "Expected only one shadow-casting light.");
            foreach (var renderer in renderers)
                Require(renderer.sharedMaterial != null && renderer.sharedMaterial.shader.isSupported, "Missing/unsupported material on " + renderer.name);
            var details = new StringBuilder();
            for (int i = 0; i < markers.StageCount; i++)
            {
                Require(markers.TryGetStage(i, out var pair), "Missing stage reference.");
                Require(Mathf.Abs(pair.MeasuredDistance - markers.Settings.GetDistance(i)) < 0.01f, "Incorrect 3D duel distance.");
                foreach (var point in new[] { pair.Player, pair.Enemy })
                {
                    Require(Physics.Raycast(point.position + Vector3.up * 0.5f, Vector3.down, out var hit, 1f, 1, QueryTriggerInteraction.Ignore), "Unsupported marker: " + point.name);
                    Require(Mathf.Abs(hit.point.y - point.position.y) < 0.01f && hit.normal.y > 0.99f, "Marker is not flush on a level landing.");
                    Require(!Physics.CheckCapsule(point.position + Vector3.up * 0.4f, point.position + Vector3.up * 1.5f, 0.32f, 1, QueryTriggerInteraction.Ignore), "No standing clearance at " + point.name);
                }
                Vector3 a = pair.Player.position + Vector3.up * 1.6f;
                Vector3 b = pair.Enemy.position + Vector3.up * 1.2f;
                Require(!Physics.Linecast(a, b, 1, QueryTriggerInteraction.Ignore), "Geometry obstructs duel sightline.");
                details.AppendLine("Stage " + (i + 1) + ": " + pair.MeasuredDistance.ToString("F3") + " m; grounded, clear standing volume and sightline.");
            }
            // Sample three paths across every tread and landing; the stair core must have no gaps.
            var ground = root.Find("StairsAndLandings").GetComponentsInChildren<Collider>();
            float minZ = ground.Min(c => c.bounds.min.z) + 0.05f;
            float maxZ = ground.Max(c => c.bounds.max.z) - 0.05f;
            int samples = 0;
            for (float z = minZ; z <= maxZ; z += 0.2f)
                foreach (float x in new[] { -4f, 0f, 4f })
                {
                    Require(Physics.Raycast(new Vector3(x, 25f, z), Vector3.down, out var hit, 30f, 1, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.99f,
                        "Missing walk surface at " + x + ", " + z);
                    samples++;
                }
            details.AppendLine("Surface samples: " + samples + "; renderers: " + renderers.Length + "; lights: " + root.GetComponentsInChildren<Light>().Length);
            SessionState.SetString(Key + "geometry", details.ToString());
            return markers;
        }

        private static void CaptureViews()
        {
            var markers = UnityEngine.Object.FindAnyObjectByType<DuelPositionMarkers>();
            var camera = Camera.main;
            Require(camera != null, "Missing main camera.");
            Directory.CreateDirectory("Documentation/Previews");
            for (int i = 0; i < markers.StageCount; i++)
            {
                Phase2Setup.PositionPreview(markers, camera, i);
                SaveRender(camera, "Documentation/Previews/phase2-" + markers.Settings.GetDistance(i).ToString("0") + "m.png");
            }
            camera.transform.position = new Vector3(35f, 37f, -42f);
            camera.transform.LookAt(new Vector3(0, 5f, 0));
            SaveRender(camera, "Documentation/Previews/phase2-overview.png");
            Phase2Setup.PositionPreview(markers, camera, 0);
        }

        private static void SaveRender(Camera camera, string path)
        {
            var target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            float aspect = camera.aspect;
            try
            {
                target.Create();
                camera.aspect = 1600f / 900f;
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                camera.aspect = aspect;
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(pixels);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void OnLog(string condition, string trace, LogType type)
        {
            if (SessionState.GetBool(Key + "running", false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                SessionState.SetString(Key + "error", condition + "\n" + trace);
        }

        private static void OnPlayChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key + "running", false))
                Finish(SessionState.GetString(Key + "error", ""));
        }

        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }

        private static void Finish(string error)
        {
            SessionState.SetBool(Key + "running", false);
            bool passed = string.IsNullOrEmpty(error) && SessionState.GetInt(Key + "stage", 0) == 4;
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/phase2-validation.txt", "Unity: " + Application.unityVersion + "\nResult: " + (passed ? "PASS" : "FAIL") +
                "\n" + SessionState.GetString(Key + "geometry", "") + "Scene flow stage: " + SessionState.GetInt(Key + "stage", 0) + "\n" + error);
            if (passed) Debug.Log("LAST STEP PHASE 2 VALIDATION PASSED.");
            else Debug.LogError("LAST STEP PHASE 2 VALIDATION FAILED: " + error);
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
            else if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }
    }
}

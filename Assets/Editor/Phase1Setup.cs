using System.IO;
using LastStep.AI;
using LastStep.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LastStep.Editor
{
    public static class Phase1Setup
    {
        [MenuItem("LAST STEP/Phase 1/Configure Project and Create Missing Scenes")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogWarning("LAST STEP: stop Play mode before setup."); return; }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();
            PlayerSettings.companyName = "Last Step Studio";
            PlayerSettings.productName = "LAST STEP";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.laststep.duel");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.laststep.duel");
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            ConfigureInput();
            ConfigureRendering();
            CreateScene("Boot", true);
            CreateScene("MainMenu", false);
            CreateScene("Duel_Staircase", false);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Boot.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Duel_Staircase.unity", true)
            };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity");
            Debug.Log("LAST STEP Phase 1 configured. Play Boot to verify transition to the empty MainMenu shell. No gameplay exists yet.");
        }

        private static void ConfigureInput()
        {
            // Active Input Handling has no public setter in this editor version.
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets.Length == 0)
            {
                Debug.LogWarning("LAST STEP: Player Settings unavailable. Set Active Input Handling manually.");
                return;
            }
            var settings = new SerializedObject(assets[0]);
            var property = settings.FindProperty("activeInputHandler");
            if (property == null)
                Debug.LogWarning("Set Player > Other Settings > Active Input Handling to Input System Package (New).");
            else
            {
                property.intValue = 1;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ConfigureRendering()
        {
            const string rendererPath = "Assets/Settings/LastStep_Renderer.asset";
            const string pipelinePath = "Assets/Settings/LastStep_URP.asset";
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.msaaSampleCount = 4;
                pipeline.supportsHDR = false;
                pipeline.shadowDistance = 35f;
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            // Clear template quality overrides so the shared foundation uses this pipeline.
            int initialLevel = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(initialLevel, false);
        }

        private static void CreateScene(string sceneName, bool boot)
        {
            string path = "Assets/Scenes/" + sceneName + ".unity";
            if (File.Exists(path)) return; // Never overwrite work from later phases.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.055f);
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            if (boot)
            {
                var manager = new GameObject("GameManager").AddComponent<GameManager>();
                var serialized = new SerializedObject(manager);
                serialized.FindProperty("duelSettings").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<DuelSettings>("Assets/ScriptableObjects/DuelSettings.asset");
                var difficulties = serialized.FindProperty("difficulties");
                difficulties.arraySize = 3;
                string[] names = { "Easy", "Normal", "Hard" };
                for (int i = 0; i < names.Length; i++)
                    difficulties.GetArrayElementAtIndex(i).objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<DifficultySettings>("Assets/ScriptableObjects/" + names[i] + ".asset");
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var loader = new GameObject("BootLoader").AddComponent<BootLoader>();
                var bootData = new SerializedObject(loader);
                bootData.FindProperty("gameManager").objectReferenceValue = manager;
                bootData.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}

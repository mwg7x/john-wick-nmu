using System;
using System.Linq;
using LastStep.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastStep.Editor
{
    public static class Phase3Setup
    {
        [MenuItem("LAST STEP/Phase 3/Configure Duel State System")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before configuring the duel state system.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(Phase2Setup.ScenePath);
            var managers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<DuelManager>(true)).ToArray();
            if (managers.Length > 1) throw new InvalidOperationException("Multiple DuelManagers found; preserve the scene and resolve duplicates.");
            var settings = AssetDatabase.LoadAssetAtPath<DuelSettings>("Assets/ScriptableObjects/DuelSettings.asset");
            if (settings == null) throw new InvalidOperationException("Missing Phase 1 DuelSettings asset.");
            var manager = managers.Length == 1 ? managers[0] : new GameObject("DuelSystems").AddComponent<DuelManager>();
            var serialized = new SerializedObject(manager);
            var field = serialized.FindProperty("settings");
            if (field.objectReferenceValue == null) field.objectReferenceValue = settings;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = manager.gameObject;
            Debug.Log("LAST STEP PHASE 3: scene-local DuelManager configured; existing environment preserved.");
        }
    }
}

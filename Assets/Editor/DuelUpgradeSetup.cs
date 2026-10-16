using System;
using LastStep.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastStep.Editor
{
    public static class DuelUpgradeSetup
    {
        [MenuItem("LAST STEP/Playable/Rebuild 50-30-10 Staircase")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
            var settings = AssetDatabase.LoadAssetAtPath<DuelSettings>("Assets/ScriptableObjects/DuelSettings.asset");
            if (settings == null || settings.StageCount != 3 || settings.GetDistance(0) != 50f ||
                settings.GetDistance(1) != 30f || settings.GetDistance(2) != 10f)
                throw new InvalidOperationException("Configure 50/30/10m DuelSettings first.");
            var scene = EditorSceneManager.OpenScene(Phase2Setup.ScenePath);
            var root = GameObject.Find(Phase2Setup.RootName);
            var markers = root != null ? root.GetComponentInChildren<DuelPositionMarkers>() : null;
            if (markers != null && markers.TryGetStage(0, out var pair) &&
                Mathf.Abs(pair.MeasuredDistance - 50f) < .05f)
            { PlayableSetup.Configure(); return; }
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.SaveScene(scene);
            Phase2Setup.Configure();
            PlayableSetup.Configure();
            Debug.Log("LAST STEP: 50/30/10m staircase and playable scene rebuilt from backed-up predecessor.");
        }
    }
}

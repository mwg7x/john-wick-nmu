using LastStep.Core;
using UnityEditor;
using UnityEngine;

namespace LastStep.Editor
{
    [CustomEditor(typeof(DuelManager))]
    public sealed class DuelManagerEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(Application.isPlaying)) DrawDefaultInspector();
            var duel = (DuelManager)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("State", duel.CurrentState.ToString());
            EditorGUILayout.LabelField("Round / distance", duel.CurrentRound + " / " + duel.CurrentDistance + " m");
            EditorGUILayout.LabelField("State permits shooting", duel.IsFireAllowed.ToString());
            EditorGUILayout.HelpBox("Editor test controls. An enabled CountdownController drives timed cues. Weapons, damage and movement are not simulated.", MessageType.Info);
            using (new EditorGUI.DisabledScope(!Application.isPlaying || !duel.isActiveAndEnabled))
            {
                switch (duel.CurrentState)
                {
                    case GameState.Boot:
                        if (GUILayout.Button("Begin duel")) duel.TryBeginDuel();
                        break;
                    case GameState.Intro:
                        if (GUILayout.Button("Complete intro / prepare round 1")) duel.TryPrepareRound(1, 0);
                        break;
                    case GameState.Preparing:
                        if (GUILayout.Button("Start countdown state")) duel.TryStartCountdown();
                        break;
                    case GameState.Countdown:
                        var countdown = duel.GetComponent<CountdownController>();
                        using (new EditorGUI.DisabledScope(countdown != null && countdown.enabled))
                            if (GUILayout.Button("Signal FIRE (manual state test only)")) duel.TryAllowFire();
                        if (GUILayout.Button("Test disqualification")) duel.TryDeclareDefeat();
                        break;
                    case GameState.Fire:
                        if (GUILayout.Button("Begin resolution")) duel.TryBeginResolution();
                        break;
                    case GameState.Resolving:
                        if (GUILayout.Button("Test survivors / transition")) duel.TryBeginTransition();
                        if (GUILayout.Button("Test victory")) duel.TryDeclareVictory();
                        if (GUILayout.Button("Test defeat")) duel.TryDeclareDefeat();
                        break;
                    case GameState.Transition:
                        if (GUILayout.Button("Test next round at current stage (no movement)"))
                            duel.TryPrepareRound(duel.CurrentRound + 1, duel.StageIndex);
                        break;
                }
                var manager = GameManager.Instance;
                if (manager != null)
                {
                    if (GUILayout.Button(manager.IsPaused ? "Resume" : "Pause")) manager.TogglePause();
                    if (GUILayout.Button("Restart duel scene")) manager.RestartDuel();
                    if (GUILayout.Button("Return to MainMenu")) manager.ReturnToMainMenu();
                }
                else EditorGUILayout.HelpBox("Start from Boot to include the persistent pause and scene-flow service.", MessageType.Info);
            }
        }
    }
}

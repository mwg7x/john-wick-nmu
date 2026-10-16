using UnityEngine;

namespace LastStep.Core
{
    [CreateAssetMenu(fileName = "DuelSettings", menuName = "LAST STEP/Duel Settings")]
    public sealed class DuelSettings : ScriptableObject
    {
        [SerializeField, Min(1), Tooltip("Initial health for each participant.")] private int startingHealth = 500;
        [SerializeField, Tooltip("Distance in meters for each stage. Final stage can repeat.")] private float[] roundDistances = { 50f, 30f, 10f };
        [SerializeField, Min(0.05f), Tooltip("Duration of EACH countdown number.")] private float countdownDuration = 1f;
        [SerializeField, Min(0.05f)] private float drawDuration = 0.6f;
        [SerializeField, Min(0.05f)] private float fireDisplayDuration = 1f;
        [SerializeField, Min(0)] private int allowedFalseStarts = 0;
        [SerializeField, Min(1f)] private float combatSeconds = 16f;
        [SerializeField, Min(0.05f)] private float roundTransitionDuration = 1.5f;

        public int StartingHealth => startingHealth;
        public int StageCount => roundDistances.Length;
        public float CountdownDuration => countdownDuration;
        public float DrawDuration => drawDuration;
        public float FireDisplayDuration => fireDisplayDuration;
        public int AllowedFalseStarts => allowedFalseStarts;
        public float RoundTransitionDuration => roundTransitionDuration;
        public float CombatSeconds => combatSeconds;
        public float GetDistance(int stageIndex) => roundDistances[Mathf.Clamp(stageIndex, 0, roundDistances.Length - 1)];

        private void OnValidate()
        {
            startingHealth = Mathf.Max(1, startingHealth);
            if (roundDistances == null || roundDistances.Length == 0) roundDistances = new[] { 50f, 30f, 10f };
            for (int i = 0; i < roundDistances.Length; i++)
                roundDistances[i] = Mathf.Clamp(roundDistances[i], 1f, i == 0 ? 1000f : roundDistances[i - 1]);
            countdownDuration = Mathf.Max(0.05f, countdownDuration);
            drawDuration = Mathf.Max(0.05f, drawDuration);
            fireDisplayDuration = Mathf.Max(0.05f, fireDisplayDuration);
            allowedFalseStarts = Mathf.Max(0, allowedFalseStarts);
            roundTransitionDuration = Mathf.Max(0.05f, roundTransitionDuration);
            combatSeconds = Mathf.Max(1f, combatSeconds);
        }
    }
}

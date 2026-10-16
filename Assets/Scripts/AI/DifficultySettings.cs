using UnityEngine;

namespace LastStep.AI
{
    [CreateAssetMenu(fileName = "Difficulty", menuName = "LAST STEP/Difficulty Settings")]
    public sealed class DifficultySettings : ScriptableObject
    {
        [SerializeField] private string difficultyName = "Normal";
        [SerializeField, Min(0.05f)] private float minReactionTime = 0.6f;
        [SerializeField, Min(0.05f)] private float maxReactionTime = 1f;
        [SerializeField, Range(0f, 0.99f), Tooltip("Aim precision, separate from forced miss probability.")] private float accuracy = 0.65f;
        [SerializeField, Range(0.01f, 1f)] private float missChance = 0.2f;
        [SerializeField, Range(0f, 1f)] private float criticalHitChance = 0.1f;
        [SerializeField, Min(0.01f), Tooltip("Aim convergence speed for the future AI.")] private float aimSpeed = 4f;

        public string DifficultyName => difficultyName;
        public float MinReactionTime => minReactionTime;
        public float MaxReactionTime => maxReactionTime;
        public float Accuracy => accuracy;
        public float MissChance => missChance;
        public float CriticalHitChance => criticalHitChance;
        public float AimSpeed => aimSpeed;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(difficultyName)) difficultyName = name;
            minReactionTime = Mathf.Max(0.05f, minReactionTime);
            maxReactionTime = Mathf.Max(minReactionTime, maxReactionTime);
            accuracy = Mathf.Clamp(accuracy, 0f, 0.99f);
            missChance = Mathf.Clamp(missChance, 0.01f, 1f);
            criticalHitChance = Mathf.Clamp01(criticalHitChance);
            aimSpeed = Mathf.Max(0.01f, aimSpeed);
        }
    }
}

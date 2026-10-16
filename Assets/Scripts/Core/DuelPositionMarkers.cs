using System;
using UnityEngine;

namespace LastStep.Core
{
    [Serializable]
    public sealed class DuelMarkerPair
    {
        [SerializeField] private float distance;
        [SerializeField] private Transform player;
        [SerializeField] private Transform enemy;

        public float Distance => distance;
        public Transform Player => player;
        public Transform Enemy => enemy;
        public float MeasuredDistance => player != null && enemy != null
            ? Vector3.Distance(player.position, enemy.position) : 0f;

        public DuelMarkerPair(float distance, Transform player, Transform enemy)
        { this.distance = distance; this.player = player; this.enemy = enemy; }
    }

    // Scene data only. Round progression and participant movement arrive in Phase 8.
    [DisallowMultipleComponent]
    public sealed class DuelPositionMarkers : MonoBehaviour
    {
        [SerializeField, Tooltip("The same balance asset used by the Boot GameManager.")] private DuelSettings duelSettings;
        [SerializeField] private DuelMarkerPair[] stages = Array.Empty<DuelMarkerPair>();
        [SerializeField] private Transform playerStandIn;
        [SerializeField] private Transform enemyStandIn;

        public DuelSettings Settings => duelSettings;
        public int StageCount => stages.Length;
        public Transform PlayerStandIn => playerStandIn;
        public Transform EnemyStandIn => enemyStandIn;

        public bool TryGetStage(int index, out DuelMarkerPair pair)
        {
            pair = index >= 0 && index < stages.Length ? stages[index] : null;
            return pair != null && pair.Player != null && pair.Enemy != null;
        }

#if UNITY_EDITOR
        public void Configure(DuelSettings settings, DuelMarkerPair[] pairs, Transform player, Transform enemy)
        { duelSettings = settings; stages = pairs; playerStandIn = player; enemyStandIn = enemy; }

        private void OnDrawGizmosSelected()
        {
            foreach (var pair in stages)
            {
                if (pair == null || pair.Player == null || pair.Enemy == null) continue;
                Gizmos.color = new Color(0.2f, 0.8f, 0.95f);
                Gizmos.DrawWireSphere(pair.Player.position + Vector3.up * 0.05f, 0.4f);
                Gizmos.color = new Color(1f, 0.55f, 0.25f);
                Gizmos.DrawWireSphere(pair.Enemy.position + Vector3.up * 0.05f, 0.4f);
                Gizmos.DrawLine(pair.Player.position, pair.Enemy.position);
                UnityEditor.Handles.Label((pair.Player.position + pair.Enemy.position) * 0.5f + Vector3.up * (pair.Distance * 0.06f),
                    pair.Distance + " m / measured " + pair.MeasuredDistance.ToString("F2") + " m");
            }
        }
#endif
    }
}

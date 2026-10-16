using UnityEngine;

namespace LastStep.Core
{
    [DisallowMultipleComponent]
    public sealed class DuelPositionManager : MonoBehaviour
    {
        [SerializeField] private DuelPositionMarkers markers;
        [SerializeField] private Transform player;
        [SerializeField] private Transform enemy;
        public bool PlaceStage(int index)
        {
            if (markers == null || player == null || enemy == null || !markers.TryGetStage(index, out var pair)) return false;
            if (index == 0) Place(player, pair.Player);
            Place(enemy, pair.Enemy);
            Physics.SyncTransforms();
            return true;
        }

        public bool PlaceEnemyStage(int index)
        {
            if (markers == null || enemy == null || !markers.TryGetStage(index, out var pair)) return false;
            Place(enemy, pair.Enemy);
            Physics.SyncTransforms();
            return true;
        }

        public float PlayerDistanceToStage(int index)
        {
            if (markers == null || player == null || !markers.TryGetStage(index, out var pair)) return float.PositiveInfinity;
            return Vector3.Distance(player.position, pair.Player.position);
        }

        private static void Place(Transform participant, Transform marker)
        {
            var motor = participant.GetComponent<CharacterController>();
            if (motor != null) motor.enabled = false;
            participant.SetPositionAndRotation(marker.position, marker.rotation);
            if (motor != null) motor.enabled = true;
        }
    }
}

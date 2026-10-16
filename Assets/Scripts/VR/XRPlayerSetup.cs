using LastStep.Core;
using LastStep.Player;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

namespace LastStep.VR
{
    public sealed class XRPlayerSetup : MonoBehaviour
    {
        [SerializeField] private XROrigin origin;
        [SerializeField] private Transform cameraOffset;
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;
        [SerializeField] private DuelManager duel;
        [SerializeField] private DuelPositionMarkers markers;
        [SerializeField] private VRFadeController fade;
        [SerializeField] private Camera pcCamera;
        [SerializeField] private PCInputController pcInput;
        [SerializeField] private PlayerMovement pcMovement;
        [SerializeField] private PlayerWeapon pcWeapon;
        [SerializeField] private VRWeaponController vrWeapon;
        [SerializeField] private VRInputController vrInput;
        private bool active;

        private void Start()
        {
            active = GameManager.Instance != null && GameManager.Instance.Mode == GameMode.VR && XRSettings.isDeviceActive;
            if (GameManager.Instance != null && GameManager.Instance.Mode == GameMode.VR && !active)
                Debug.LogWarning("LAST STEP: VR requested, but no active XR display was detected. Using PC controls.");
            if (origin != null) origin.gameObject.SetActive(active);
            if (pcCamera != null) pcCamera.enabled = !active;
            if (pcCamera != null && pcCamera.TryGetComponent<AudioListener>(out var listener)) listener.enabled = !active;
            if (pcInput != null) pcInput.enabled = !active;
            if (pcMovement != null) pcMovement.enabled = !active;
            if (pcWeapon != null) pcWeapon.enabled = !active;
            if (vrInput != null) vrInput.enabled = active;
            if (vrWeapon != null) vrWeapon.enabled = active;
            if (active)
            {
                if (vrInput != null) vrInput.TurnRequested += OnTurn;
                Calibrate(); PlaceNow(0);
            }
        }
        private void OnEnable() { if (duel != null) duel.OnRoundStarted += OnRound; }
        private void OnDisable()
        {
            if (duel != null) duel.OnRoundStarted -= OnRound;
            if (vrInput != null) vrInput.TurnRequested -= OnTurn;
        }
        private void OnRound(int round, float distance)
        {
            if (!active || markers == null || !markers.TryGetStage(duel.StageIndex, out var pair)) return;
            if (fade != null) fade.FadeTeleport(() => PlaceNow(duel.StageIndex));
            else PlaceNow(duel.StageIndex);
        }
        private void PlaceNow(int index)
        {
            if (origin == null || markers == null || !markers.TryGetStage(index, out var pair)) return;
            // Preserve the tracked head rotation. Only translate the XR Origin.
            origin.MoveCameraToWorldLocation(pair.Player.position + Vector3.up *
                (GameManager.Instance != null ? GameManager.Instance.CalibratedHeight : 1.7f));
        }
        private void Calibrate()
        {
            if (cameraOffset == null || GameManager.Instance == null) return;
            cameraOffset.localPosition = Vector3.up * (GameManager.Instance.Seated ? 0.55f : 0f);
        }
        private void OnTurn(int direction)
        {
            if (active && origin != null && GameManager.Instance != null && GameManager.Instance.SnapTurning)
                origin.RotateAroundCameraUsingOriginUp(30f * direction);
        }
        private void Update()
        {
            if (!active) return;
            Pose(XRNode.Head, head);
            Pose(XRNode.LeftHand, leftHand);
            Pose(XRNode.RightHand, rightHand);
        }
        private static void Pose(XRNode node, Transform target)
        {
            if (target == null) return;
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid) return;
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position)) target.localPosition = position;
            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation)) target.localRotation = rotation;
        }
    }
}

using System;
using LastStep.Core;
using UnityEngine;
using UnityEngine.XR;

namespace LastStep.VR
{
    public sealed class VRInputController : MonoBehaviour
    {
        [SerializeField] private DuelManager duel;
        [SerializeField] private FalseStartManager falseStarts;
        public event Action FirePressed;
        public event Action<bool> GripChanged;
        public event Action<int> TurnRequested;
        private bool triggerHeld, gripHeld, turnHeld;
        public XRNode DominantNode => GameManager.Instance != null && GameManager.Instance.Hand == DominantHand.Left
            ? XRNode.LeftHand : XRNode.RightHand;
        public InputDevice DominantDevice => InputDevices.GetDeviceAtXRNode(DominantNode);

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.Mode != GameMode.VR ||
                !XRSettings.isDeviceActive || GameManager.Instance.IsPaused) return;
            var hand = DominantDevice;
            bool trigger = ReadButton(hand, CommonUsages.triggerButton);
            if (trigger && !triggerHeld)
            {
                if (duel != null && duel.CurrentState == GameState.Countdown) falseStarts?.ReportEarlyTrigger();
                else FirePressed?.Invoke();
            }
            triggerHeld = trigger;
            bool grip = ReadButton(hand, CommonUsages.gripButton);
            if (grip != gripHeld) { gripHeld = grip; GripChanged?.Invoke(grip); }
            var other = InputDevices.GetDeviceAtXRNode(DominantNode == XRNode.LeftHand ? XRNode.RightHand : XRNode.LeftHand);
            other.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis);
            bool turning = Mathf.Abs(axis.x) > 0.7f;
            if (turning && !turnHeld) TurnRequested?.Invoke(axis.x > 0f ? 1 : -1);
            turnHeld = turning;
        }
        private static bool ReadButton(InputDevice device, InputFeatureUsage<bool> usage)
        { return device.isValid && device.TryGetFeatureValue(usage, out bool value) && value; }
    }
}

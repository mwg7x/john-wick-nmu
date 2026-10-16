using UnityEngine;
using UnityEngine.XR;

namespace LastStep.VR
{
    public sealed class HapticController : MonoBehaviour
    {
        [SerializeField] private VRInputController input;
        public void Pulse(float strength, float duration)
        {
            var device = input != null ? input.DominantDevice : default;
            if (device.isValid && device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
                device.SendHapticImpulse(0, Mathf.Clamp01(strength), Mathf.Max(0f, duration));
        }
    }
}

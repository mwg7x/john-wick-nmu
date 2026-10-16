using LastStep.Core;
using UnityEngine;

namespace LastStep.Utilities
{
    [DisallowMultipleComponent]
    public sealed class SkyPresetController : MonoBehaviour
    {
        [SerializeField] private Renderer cloudBackdrop;
        [SerializeField] private Light moonLight;
        [SerializeField] private Camera duelCamera;
        private Material skyInstance;

        private void Start()
        {
            if (cloudBackdrop != null) skyInstance = cloudBackdrop.material;
            Apply();
        }
        private void Apply()
        {
            int preset = GameManager.Instance != null ? GameManager.Instance.SkyPresetIndex : 0;
            bool high = GameManager.Instance == null || GameManager.Instance.HighEffects;
            Color[] tints = {
                new Color(.72f,.84f,1f),
                new Color(.46f,.52f,.68f),
                new Color(.58f,.66f,.73f)
            };
            if (skyInstance != null) skyInstance.color = tints[Mathf.Clamp(preset, 0, 2)];
            if (moonLight != null)
            {
                moonLight.intensity = preset == 1 ? .36f : preset == 2 ? .55f : .8f;
                moonLight.color = preset == 1 ? new Color(.65f,.72f,.9f) : new Color(.72f,.82f,1f);
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = preset == 2 ? new Color(.14f,.18f,.22f) : new Color(.055f,.075f,.11f);
            RenderSettings.fogDensity = (preset == 2 ? .012f : preset == 1 ? .0055f : .0035f) * (high ? 1f : .7f);
            if (duelCamera != null) duelCamera.backgroundColor = RenderSettings.fogColor;
        }
        private void OnDestroy()
        {
            if (skyInstance != null) Destroy(skyInstance);
        }
    }
}

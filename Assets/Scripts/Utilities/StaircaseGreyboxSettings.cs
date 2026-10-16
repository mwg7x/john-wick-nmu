using UnityEngine;

namespace LastStep.Utilities
{
    [CreateAssetMenu(fileName = "StaircaseGreyboxSettings", menuName = "LAST STEP/Staircase Greybox Settings")]
    public sealed class StaircaseGreyboxSettings : ScriptableObject
    {
        [SerializeField, Min(6f), Tooltip("Clear width between the stair edges, in meters.")] private float stairWidth = 12f;
        [SerializeField, Min(1.6f)] private float landingLength = 2.2f;
        [SerializeField, Range(0.05f, 0.35f), Tooltip("Vertical drop divided by straight-line distance. Marker distances remain true 3D distances.")] private float verticalFraction = 0.2f;
        [SerializeField, Range(0.1f, 0.2f)] private float maximumStepRise = 0.18f;
        [SerializeField, Min(6f), Tooltip("Distance along the slope between extension landings.")] private float extensionSpacing = 10f;
        [SerializeField, Range(1, 3)] private int extensionLandings = 2;
        [SerializeField] private int skylineSeed = 2406;

        public float StairWidth => stairWidth;
        public float LandingLength => landingLength;
        public float VerticalFraction => verticalFraction;
        public float MaximumStepRise => maximumStepRise;
        public float ExtensionSpacing => extensionSpacing;
        public int ExtensionLandings => extensionLandings;
        public int SkylineSeed => skylineSeed;

        private void OnValidate()
        {
            stairWidth = Mathf.Max(6f, stairWidth);
            landingLength = Mathf.Max(1.6f, landingLength);
            verticalFraction = Mathf.Clamp(verticalFraction, 0.05f, 0.35f);
            maximumStepRise = Mathf.Clamp(maximumStepRise, 0.1f, 0.2f);
            extensionSpacing = Mathf.Max(6f, extensionSpacing);
            extensionLandings = Mathf.Clamp(extensionLandings, 1, 3);
        }
    }
}

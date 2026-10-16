using LastStep.Core;
using LastStep.Weapons;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LastStep.UI
{
    // Screen-space feedback is subtle at high health and intensifies only near defeat.
    public sealed class DamageVision : MonoBehaviour
    {
        private Health player;
        private RawImage overlay;
        private Texture2D texture;
        private float pulse;
        private int previousHealth;
        [SerializeField, Range(0f, 1f)] private float maximumDesaturation = .72f;
        [SerializeField, Range(0f, 1f)] private float injuryExposure = .28f;
        [SerializeField, Range(.1f, 2f)] private float recoverySeconds = 1.4f;
        private ColorAdjustments colorAdjustments;
        private RawImage wash, directionIndicator;
        private Texture2D indicatorTexture;
        private WeaponController enemyWeapon;
        private float directionalPulse;

        public void Initialize(Health source)
        {
            if (player != null) player.Changed -= OnHealthChanged;
            if (enemyWeapon != null) enemyWeapon.FleshHit -= OnFleshHit;
            player = source;
            previousHealth = player != null ? player.Current : 0;
            if (player != null) player.Changed += OnHealthChanged;
            if (overlay == null) CreateOverlay();
            var volume = FindAnyObjectByType<Volume>();
            if (volume != null)
            {
                var profile = volume.profile;
                if (!profile.TryGet(out colorAdjustments)) colorAdjustments = profile.Add<ColorAdjustments>(true);
                colorAdjustments.saturation.overrideState = true;
                colorAdjustments.postExposure.overrideState = true;
            }
            foreach (var weapon in FindObjectsByType<WeaponController>(FindObjectsSortMode.None))
                if (weapon.GetComponent<Health>() != player) { enemyWeapon = weapon; break; }
            if (enemyWeapon != null) enemyWeapon.FleshHit += OnFleshHit;
        }

        private void CreateOverlay()
        {
            const int size = 128;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = ((x + .5f) / size - .5f) * 2f;
                float ny = ((y + .5f) / size - .5f) * 2f;
                float distance = Mathf.Sqrt(nx * nx * .72f + ny * ny);
                float edge = Mathf.SmoothStep(.35f, 1.08f, distance);
                pixels[y * size + x] = new Color32(68, 8, 15, (byte)(edge * 235));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var go = new GameObject("Damage Vision", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            overlay = go.GetComponent<RawImage>();
            overlay.texture = texture;
            overlay.raycastTarget = false;
            go.transform.SetAsFirstSibling(); // HUD labels and result panels remain crisp.
            var washObject = new GameObject("Injury Wash", typeof(RectTransform), typeof(RawImage));
            washObject.transform.SetParent(transform, false);
            var washRect = (RectTransform)washObject.transform;
            washRect.anchorMin = Vector2.zero; washRect.anchorMax = Vector2.one;
            washRect.offsetMin = washRect.offsetMax = Vector2.zero;
            wash = washObject.GetComponent<RawImage>();
            wash.texture = Texture2D.whiteTexture; wash.raycastTarget = false;
            washObject.transform.SetAsFirstSibling();
            indicatorTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var indicatorPixels = new Color32[64 * 64];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float halfWidth = Mathf.Lerp(3f, 28f, y / 63f);
                indicatorPixels[y * 64 + x] = Mathf.Abs(x - 31.5f) < halfWidth && y > 8
                    ? new Color32(230, 45, 52, (byte)(210 * (1f - y / 64f))) : new Color32(0, 0, 0, 0);
            }
            indicatorTexture.SetPixels32(indicatorPixels); indicatorTexture.Apply();
            var indicatorObject = new GameObject("Hit Direction", typeof(RectTransform), typeof(RawImage));
            indicatorObject.transform.SetParent(transform, false);
            var indicatorRect = (RectTransform)indicatorObject.transform;
            indicatorRect.anchorMin = indicatorRect.anchorMax = new Vector2(.5f, .5f);
            indicatorRect.sizeDelta = new Vector2(96f, 96f);
            indicatorRect.anchoredPosition = new Vector2(0f, 155f);
            directionIndicator = indicatorObject.GetComponent<RawImage>();
            directionIndicator.texture = indicatorTexture; directionIndicator.raycastTarget = false;
        }

        private void OnFleshHit(Health target, Vector3 point, Vector3 shotDirection, bool headshot)
        {
            if (target != player) return;
            directionalPulse = 1f;
            var cam = Camera.main;
            if (cam == null || directionIndicator == null) return;
            Vector3 fromAttacker = -shotDirection;
            float angle = Mathf.Atan2(Vector3.Dot(cam.transform.right, fromAttacker),
                Vector3.Dot(cam.transform.forward, fromAttacker)) * Mathf.Rad2Deg;
            var rect = (RectTransform)directionIndicator.transform;
            rect.anchoredPosition = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad),
                Mathf.Cos(angle * Mathf.Deg2Rad)) * 155f;
            rect.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        private void OnHealthChanged(Health value)
        {
            if (value.Current < previousHealth)
                pulse = Mathf.Clamp01(pulse + .25f + (previousHealth - value.Current) / (float)value.Maximum);
            previousHealth = value.Current;
        }

        private void Update()
        {
            if (overlay == null || player == null) return;
            pulse = Mathf.MoveTowards(pulse, 0f, Time.unscaledDeltaTime / recoverySeconds);
            directionalPulse = Mathf.MoveTowards(directionalPulse, 0f, Time.unscaledDeltaTime / recoverySeconds);
            float lost = 1f - (float)player.Current / player.Maximum;
            float strength = Mathf.Clamp01(lost * lost * .52f + pulse * .38f);
            overlay.color = new Color(1f, 1f, 1f, strength);
            if (wash != null) wash.color = new Color(.68f, .74f, .8f, pulse * injuryExposure);
            if (directionIndicator != null) directionIndicator.color = new Color(1f, 1f, 1f, directionalPulse);
            if (colorAdjustments != null)
            {
                colorAdjustments.saturation.value = -100f * Mathf.Clamp01(lost * .5f + pulse) * maximumDesaturation;
                colorAdjustments.postExposure.value = -Mathf.Clamp01(lost * .4f + pulse) * injuryExposure * 2f;
            }
        }

        private void OnDestroy()
        {
            if (player != null) player.Changed -= OnHealthChanged;
            if (enemyWeapon != null) enemyWeapon.FleshHit -= OnFleshHit;
            if (texture != null) Destroy(texture);
            if (indicatorTexture != null) Destroy(indicatorTexture);
        }
    }
}

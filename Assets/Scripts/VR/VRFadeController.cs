using System.Collections;
using UnityEngine;

namespace LastStep.VR
{
    public sealed class VRFadeController : MonoBehaviour
    {
        [SerializeField] private Renderer fadeRenderer;
        [SerializeField] private float fadeSeconds = 0.28f;
        private Coroutine running;
        public void FadeTeleport(System.Action move)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Run(move));
        }
        private IEnumerator Run(System.Action move)
        {
            yield return Fade(0f, 1f);
            move?.Invoke();
            yield return Fade(1f, 0f);
            running = null;
        }
        private IEnumerator Fade(float from, float to)
        {
            if (fadeRenderer == null) yield break;
            float elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var color = fadeRenderer.material.color;
                color.a = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeSeconds));
                fadeRenderer.material.color = color;
                yield return null;
            }
        }
    }
}

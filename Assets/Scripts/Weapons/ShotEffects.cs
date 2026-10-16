using UnityEngine;
using System.Collections;

namespace LastStep.Weapons
{
    [DisallowMultipleComponent]
    public sealed class ShotEffects : MonoBehaviour
    {
        [SerializeField] private WeaponController weapon;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Color muzzleColor = new Color(1f, 0.65f, 0.28f);
        private ParticleSystem flash;
        private Material tracerMaterial;
        private Material flashMaterial;
        private Material bloodMaterial;
        private Material dustMaterial;
        private Texture2D particleTexture;
        private Light flashLight;

        private void Awake()
        {
            if (muzzle == null) return;
            var root = new GameObject("Muzzle Flash");
            root.transform.SetParent(muzzle, false);
            flash = root.AddComponent<ParticleSystem>();
            flash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = flash.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.12f;
            main.startLifetime = 0.11f;
            main.startSpeed = 2f;
            main.startSize = 0.22f;
            main.startColor = muzzleColor;
            main.maxParticles = 12;
            var flashRenderer = flash.GetComponent<ParticleSystemRenderer>();
            flashRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            var emission = flash.emission;
            emission.enabled = false;
            var shape = flash.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            flashLight = root.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = new Color(1f,.66f,.28f);
            flashLight.range = 4f;
            flashLight.intensity = 0f;
            flashLight.shadows = LightShadows.None;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? shader;
            if (shader != null)
            {
                tracerMaterial = new Material(shader);
                tracerMaterial.color = new Color(1f, .79f, .44f, 1f);
                particleTexture = MakeSoftParticle();
                flashMaterial = ParticleMaterial(particleShader, new Color(1f, .68f, .27f, .85f));
                bloodMaterial = ParticleMaterial(particleShader, new Color(.42f, .014f, .025f, .9f));
                dustMaterial = ParticleMaterial(particleShader, new Color(.35f, .35f, .38f, .7f));
                flashRenderer.sharedMaterial = flashMaterial;
            }
        }
        private Material ParticleMaterial(Shader shader, Color color)
        {
            var material = new Material(shader);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.color = color;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", particleTexture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", particleTexture);
            return material;
        }
        private static Texture2D MakeSoftParticle()
        {
            const int size = 48;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            var colors = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = (x + .5f - size * .5f) / (size * .5f);
                float dy = (y + .5f - size * .5f) / (size * .5f);
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)), .7f);
                colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(colors); texture.Apply(false, true);
            return texture;
        }
        private void OnEnable() { if (weapon != null) { weapon.ShotFired += OnShot; weapon.FleshHit += OnFleshHit; } }
        private void OnDisable() { if (weapon != null) { weapon.ShotFired -= OnShot; weapon.FleshHit -= OnFleshHit; } }
        private void OnShot(bool hit, Vector3 point)
        {
            if (flash != null) flash.Emit(8);
            if (flashLight != null) { flashLight.intensity = 9f; StartCoroutine(FadeFlash()); }
            if (muzzle != null && tracerMaterial != null && (LastStep.Core.GameManager.Instance == null || LastStep.Core.GameManager.Instance.HighEffects))
                StartCoroutine(Tracer(muzzle.position, point));
            if (!hit) Burst("Stone Impact", point, Vector3.up, dustMaterial, 7, .24f, .055f, .55f);
        }
        private void OnFleshHit(LastStep.Core.Health victim, Vector3 point, Vector3 direction, bool headshot)
        {
            Burst(headshot ? "Head Impact" : "Body Impact", point,
                direction * .65f + Vector3.up * .35f, bloodMaterial,
                headshot ? 20 : 13, headshot ? .42f : .32f, .035f, headshot ? 2.1f : 1.45f);
        }
        private void Burst(string name, Vector3 position, Vector3 direction, Material material,
            int count, float life, float size, float speed)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.transform.rotation = Quaternion.LookRotation(direction.sqrMagnitude > .001f ? direction : Vector3.up);
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false; main.playOnAwake = false;
            main.startLifetime = life; main.startSpeed = speed;
            main.startSize = size; main.maxParticles = count + 2;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = .8f;
            var emission = particles.emission; emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 33f; shape.radius = .025f;
            var sizeOverLife = particles.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, .08f)));
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            particles.Emit(count);
            Destroy(go, life + .3f);
        }
        private IEnumerator FadeFlash()
        {
            yield return new WaitForSeconds(.075f);
            if(flashLight != null) flashLight.intensity=0f;
        }
        private IEnumerator Tracer(Vector3 start, Vector3 end)
        {
            var go = new GameObject("Shot Tracer");
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, start + (end - start).normalized * Mathf.Min(3f, Vector3.Distance(start, end)));
            line.sharedMaterial = tracerMaterial;
            line.startWidth = .012f;
            line.endWidth = .002f;
            line.numCapVertices = 2;
            yield return new WaitForSeconds(.08f);
            Destroy(go);
        }
        private void OnDestroy()
        {
            if (tracerMaterial != null) Destroy(tracerMaterial);
            if (flashMaterial != null) Destroy(flashMaterial);
            if (bloodMaterial != null) Destroy(bloodMaterial);
            if (dustMaterial != null) Destroy(dustMaterial);
            if (particleTexture != null) Destroy(particleTexture);
        }
    }
}

using LastStep.Core;
using LastStep.Weapons;
using UnityEngine;

namespace LastStep.Audio
{
    [DisallowMultipleComponent]
    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField] private DuelManager duel;
        [SerializeField] private CountdownController countdown;
        [SerializeField] private FalseStartManager falseStarts;
        [SerializeField] private WeaponController playerWeapon;
        [SerializeField] private WeaponController enemyWeapon;
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private AudioSource playerSource;
        [SerializeField] private AudioSource enemySource;
        private AudioClip tick, draw, fire, gunshot, reload, warning, victory, defeat, wind, hit, footstep;
        private AudioClip[] stoneSteps, bodyHits, stoneHits, metalHits, woodHits;
        private AudioClip nearMiss;
        private AudioSource nearMissSource;
        private float nextNearMiss;
        private Transform playerActor;
        private Vector3 lastPlayerPosition;
        private float nextStepTime;

        private void Awake()
        {
            tick = Tone("Countdown", 620, 0.12f, 0.18f);
            draw = Tone("Draw", 330, 0.34f, 0.22f);
            fire = Tone("Fire", 940, 0.5f, 0.32f);
            warning = Tone("False Start", 180, 0.46f, 0.28f);
            victory = Tone("Victory", 680, 0.95f, 0.23f);
            defeat = Tone("Defeat", 190, 0.95f, 0.22f);
            gunshot = ActionShot();
            hit = ImpactThud();
            footstep = Footstep();
            stoneSteps = LoadClips("footstep_concrete", 3);
            bodyHits = LoadClips("impactSoft_heavy", 2);
            stoneHits = LoadClips("impactMining", 2);
            metalHits = LoadClips("impactMetal_heavy", 2);
            woodHits = LoadClips("impactWood_heavy", 2);
            nearMiss = Whizz();
            nearMissSource = gameObject.AddComponent<AudioSource>();
            nearMissSource.playOnAwake = false;
            nearMissSource.spatialBlend = 0f;
            reload = Tone("Reload", 770, .22f, .13f);
            wind = Noise("Distant Wind", 4f, 0.028f);
        }
        private void OnEnable()
        {
            if (countdown != null) countdown.CueChanged += OnCue;
            if (falseStarts != null) falseStarts.FalseStart += OnFalseStart;
            if (duel != null) { duel.OnVictory += OnVictory; duel.OnDefeat += OnDefeat; }
            if (playerWeapon != null) playerWeapon.ShotFired += OnPlayerShot;
            if (enemyWeapon != null) enemyWeapon.ShotFired += OnEnemyShot;
            if (playerWeapon != null) playerWeapon.ReloadStarted += OnPlayerReload;
            if (enemyWeapon != null) enemyWeapon.ReloadStarted += OnEnemyReload;
            if (playerWeapon != null) playerWeapon.FleshHit += OnFleshHit;
            if (enemyWeapon != null) enemyWeapon.FleshHit += OnFleshHit;
            if (playerWeapon != null) playerWeapon.SurfaceHit += OnSurfaceHit;
            if (enemyWeapon != null)
            { enemyWeapon.SurfaceHit += OnSurfaceHit; enemyWeapon.NearMiss += OnNearMiss; }
        }
        private void Start()
        {
            if (ambienceSource == null) return;
            ambienceSource.clip = wind;
            ambienceSource.loop = true;
            ambienceSource.spatialBlend = 0f;
            ambienceSource.Play();
            playerActor = playerWeapon != null ? playerWeapon.transform : null;
            if (playerActor != null) lastPlayerPosition = playerActor.position;
        }
        private void OnDisable()
        {
            if (countdown != null) countdown.CueChanged -= OnCue;
            if (falseStarts != null) falseStarts.FalseStart -= OnFalseStart;
            if (duel != null) { duel.OnVictory -= OnVictory; duel.OnDefeat -= OnDefeat; }
            if (playerWeapon != null) playerWeapon.ShotFired -= OnPlayerShot;
            if (enemyWeapon != null) enemyWeapon.ShotFired -= OnEnemyShot;
            if (playerWeapon != null) playerWeapon.ReloadStarted -= OnPlayerReload;
            if (enemyWeapon != null) enemyWeapon.ReloadStarted -= OnEnemyReload;
            if (playerWeapon != null) playerWeapon.FleshHit -= OnFleshHit;
            if (enemyWeapon != null) enemyWeapon.FleshHit -= OnFleshHit;
            if (playerWeapon != null) playerWeapon.SurfaceHit -= OnSurfaceHit;
            if (enemyWeapon != null)
            { enemyWeapon.SurfaceHit -= OnSurfaceHit; enemyWeapon.NearMiss -= OnNearMiss; }
        }
        private void Update()
        {
            float volume = GameManager.Instance != null ? GameManager.Instance.AudioVolume : 0.8f;
            if (uiSource != null) uiSource.volume = volume;
            if (playerSource != null) playerSource.volume = volume;
            if (enemySource != null) enemySource.volume = volume;
            if (ambienceSource != null) ambienceSource.volume = volume * 0.45f;
            if (playerActor != null && playerSource != null)
            {
                float motion = Vector3.Distance(playerActor.position, lastPlayerPosition) / Mathf.Max(Time.deltaTime, .001f);
                lastPlayerPosition = playerActor.position;
                if (motion > .8f && Time.time >= nextStepTime)
                {
                    playerSource.PlayOneShot(Pick(stoneSteps, footstep), Mathf.Clamp01(motion / 5f) * .34f);
                    nextStepTime = Time.time + Mathf.Lerp(.5f, .32f, Mathf.Clamp01(motion / 5f));
                }
            }
        }
        private void OnCue(CountdownCue cue)
        {
            var clip = cue switch
            {
                CountdownCue.Three or CountdownCue.Two or CountdownCue.One => tick,
                CountdownCue.Draw => draw,
                CountdownCue.Fire => fire,
                _ => null
            };
            if (clip != null && uiSource != null) uiSource.PlayOneShot(clip);
        }
        private void OnFalseStart(int count, bool disqualified)
        { if (uiSource != null) uiSource.PlayOneShot(warning); }
        private void OnVictory()
        { if (uiSource != null) uiSource.PlayOneShot(victory != null ? victory : Tone("Victory", 680, .95f, .23f)); }
        private void OnDefeat()
        { if (uiSource != null) uiSource.PlayOneShot(defeat != null ? defeat : Tone("Defeat", 190, .95f, .22f)); }
        private void OnPlayerShot(bool hit, Vector3 point)
        {
            if (playerSource == null) return;
            playerSource.pitch = Pitch(playerWeapon);
            playerSource.PlayOneShot(gunshot);
        }
        private void OnEnemyShot(bool hit, Vector3 point)
        {
            if (enemySource == null) return;
            enemySource.pitch = Pitch(enemyWeapon);
            enemySource.PlayOneShot(gunshot);
        }
        private void OnPlayerReload() { if (playerSource != null) playerSource.PlayOneShot(reload); }
        private void OnEnemyReload() { if (enemySource != null) enemySource.PlayOneShot(reload); }
        private void OnFleshHit(Health target, Vector3 point, Vector3 direction, bool headshot)
        {
            var source = playerWeapon != null && target == playerWeapon.GetComponent<Health>() ? playerSource : enemySource;
            if (source != null) source.PlayOneShot(Pick(bodyHits, hit), headshot ? .46f : .32f);
        }
        private void OnSurfaceHit(RaycastHit hitInfo)
        {
            var collider = hitInfo.collider;
            if (collider == null) return;
            string surface = collider.name.ToLowerInvariant();
            AudioClip[] clips = surface.Contains("metal") || surface.Contains("rail") || surface.Contains("lamp")
                ? metalHits : surface.Contains("wood") ? woodHits : stoneHits;
            var source = playerWeapon != null && Vector3.Distance(playerWeapon.transform.position, hitInfo.point) <
                Vector3.Distance(enemyWeapon != null ? enemyWeapon.transform.position : transform.position, hitInfo.point)
                ? playerSource : enemySource;
            if (source != null) source.PlayOneShot(Pick(clips, hit), .38f);
        }
        private void OnNearMiss(Vector3 point, Vector3 towardPlayer)
        {
            if (Time.unscaledTime < nextNearMiss || nearMissSource == null) return;
            nextNearMiss = Time.unscaledTime + .65f;
            nearMissSource.volume = GameManager.Instance != null ? GameManager.Instance.AudioVolume : .8f;
            var camera = Camera.main;
            nearMissSource.panStereo = camera == null ? 0f : Mathf.Clamp(Vector3.Dot(camera.transform.right,
                point - camera.transform.position) / 2f, -.8f, .8f);
            nearMissSource.PlayOneShot(nearMiss, .5f);
        }
        private static AudioClip[] LoadClips(string prefix, int count)
        {
            var clips = new AudioClip[count];
            for (int i = 0; i < count; i++)
                clips[i] = Resources.Load<AudioClip>("Audio/KenneyImpact/" + prefix + "_" + i.ToString("000"));
            return clips;
        }
        private static AudioClip Pick(AudioClip[] clips, AudioClip fallback)
        {
            if (clips == null || clips.Length == 0) return fallback;
            var candidate = clips[Random.Range(0, clips.Length)];
            return candidate != null ? candidate : fallback;
        }
        private static AudioClip Whizz()
        {
            const int rate = 22050;
            int count = rate / 4;
            var samples = new float[count];
            uint seed = 5719;
            float filtered = 0f;
            for (int i = 0; i < count; i++)
            {
                seed = seed * 1664525u + 1013904223u;
                float white = ((seed >> 8) / 16777216f) * 2f - 1f;
                float t = i / (float)count;
                filtered += (white - filtered) * Mathf.Lerp(.035f, .23f, t);
                samples[i] = filtered * Mathf.Sin(Mathf.PI * t) * .35f;
            }
            var clip = AudioClip.Create("Bullet Near Miss", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        private static float Pitch(WeaponController weapon)
        {
            if (weapon == null) return 1f;
            if (weapon.Data == PistolCatalog.Load(1)) return 1.18f;
            if (weapon.Data == PistolCatalog.Load(2)) return .82f;
            return 1f;
        }

        private static AudioClip Tone(string name, float hz, float seconds, float amplitude)
        {
            const int sampleRate = 22050;
            int count = Mathf.CeilToInt(seconds * sampleRate);
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Exp(-5f * t / seconds);
                samples[i] = Mathf.Sin(2f * Mathf.PI * hz * t) * envelope * amplitude;
            }
            var clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        private static AudioClip Noise(string name, float seconds, float amplitude)
        {
            const int sampleRate = 22050;
            int count = Mathf.CeilToInt(seconds * sampleRate);
            var samples = new float[count];
            uint seed = 13017;
            float smooth = 0f;
            for (int i = 0; i < count; i++)
            {
                seed = seed * 1664525u + 1013904223u;
                float white = ((seed >> 8) / 16777216f) * 2f - 1f;
                smooth = Mathf.Lerp(smooth, white, name == "Pistol" ? 0.48f : 0.015f);
                float envelope = name == "Pistol" ? Mathf.Exp(-9f * i / count) : 1f;
                samples[i] = smooth * amplitude * envelope;
            }
            var clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        private static AudioClip ActionShot()
        {
            const int rate = 44100;
            int count = (int)(rate * .48f);
            var samples = new float[count];
            uint seed = 918273u;
            float low = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                seed = seed * 1664525u + 1013904223u;
                float white = ((seed >> 8) / 16777216f) * 2f - 1f;
                low += (white - low) * .09f;
                float crack = white * Mathf.Exp(-t * 125f) * .72f;
                float body = (low * .65f + Mathf.Sin(t * 2f * Mathf.PI * (145f - t * 90f)) * .35f)
                    * Mathf.Exp(-t * 27f) * .75f;
                float echoT = t - .065f;
                float tail = echoT > 0f ? low * Mathf.Exp(-echoT * 9f) * .17f : 0f;
                samples[i] = Mathf.Clamp(crack + body + tail, -.95f, .95f);
            }
            var clip = AudioClip.Create("Layered Pistol Report", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        private static AudioClip ImpactThud()
        {
            const int rate = 22050;
            int count = (int)(rate * .21f);
            var samples = new float[count];
            uint seed = 28375u;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                seed = seed * 1664525u + 1013904223u;
                float noise = ((seed >> 8) / 16777216f) * 2f - 1f;
                samples[i] = (Mathf.Sin(t * 2f * Mathf.PI * (105f - t * 180f)) * .48f + noise * .13f)
                    * Mathf.Exp(-t * 23f);
            }
            var clip = AudioClip.Create("Cloth Impact", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        private static AudioClip Footstep()
        {
            const int rate = 22050;
            int count = (int)(rate * .13f);
            var samples = new float[count];
            uint seed = 7812u;
            float low = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                seed = seed * 1664525u + 1013904223u;
                float white = ((seed >> 8) / 16777216f) * 2f - 1f;
                low += (white - low) * .12f;
                samples[i] = (low * .7f + Mathf.Sin(t * 2f * Mathf.PI * 110f) * .14f) * Mathf.Exp(-t * 38f);
            }
            var clip = AudioClip.Create("Stone Footstep", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

using LastStep.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastStep.Audio
{
    [DisallowMultipleComponent]
    public sealed class AdaptiveMusic : MonoBehaviour
    {
        private AudioSource[] voices;
        private int active;
        private DuelManager duel;
        private Health player;
        private AudioClip menu, intro, countdown, combat, finisher, victory;
        private float fade = 1f;

        private void Awake()
        {
            // Batch Play Mode has no listener/device; decoding long music there can
            // stall gameplay regression frames without testing audible output.
            if (Application.isBatchMode) { enabled = false; return; }
            voices = new[] { gameObject.AddComponent<AudioSource>(), gameObject.AddComponent<AudioSource>() };
            foreach (var voice in voices)
            { voice.playOnAwake = false; voice.spatialBlend = 0f; voice.volume = 0f; voice.loop = true; }
            menu = Load("title"); intro = Load("sector"); countdown = Load("pulse");
            combat = Load("urgent"); finisher = Load("transmission"); victory = Load("victory");
        }
        private static AudioClip Load(string name) => Resources.Load<AudioClip>("Audio/SRG774/" + name);
        private void OnEnable()
        {
            if (GameManager.Instance != null) GameManager.Instance.SceneLoaded += OnSceneLoaded;
        }
        private void Start() { OnSceneLoaded(SceneManager.GetActiveScene().name); }
        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.SceneLoaded -= OnSceneLoaded;
            if (duel != null) duel.OnStateChanged -= OnDuelState;
        }
        private void OnSceneLoaded(string scene)
        {
            if (duel != null) duel.OnStateChanged -= OnDuelState;
            duel = null; player = null;
            if (scene == "MainMenu") { Transition(menu, 1.1f); return; }
            if (scene != "Duel_Staircase") return;
            duel = FindAnyObjectByType<DuelManager>();
            var markers = FindAnyObjectByType<DuelPositionMarkers>();
            player = markers != null && markers.PlayerStandIn != null
                ? markers.PlayerStandIn.GetComponent<Health>() : null;
            if (duel != null) duel.OnStateChanged += OnDuelState;
            Transition(intro, 1.1f);
        }
        private void OnDuelState(GameState oldState, GameState state)
        {
            switch (state)
            {
                case GameState.Intro:
                case GameState.Preparing:
                case GameState.Transition: Transition(intro, 1.2f); break;
                case GameState.Countdown: Transition(countdown, .55f); break;
                case GameState.Fire: Transition(combat, .28f); break;
                case GameState.Finisher: Transition(finisher, .15f); break;
                case GameState.Victory: Transition(victory, .5f); break;
                case GameState.Defeat: Transition(finisher, .55f); break;
            }
        }
        private void Transition(AudioClip clip, float seconds)
        {
            if (clip == null || voices == null) return;
            if (voices[active].clip == clip && voices[active].isPlaying) return;
            active = 1 - active;
            var next = voices[active];
            next.Stop(); next.clip = clip; next.loop = clip != victory; next.volume = 0f; next.Play();
            fade = Mathf.Max(.05f, seconds);
        }
        private void Update()
        {
            if (voices == null) return;
            var game = GameManager.Instance;
            float volume = game != null ? game.AudioVolume * game.MusicVolume * .38f : .2f;
            if (player != null && player.Maximum > 0 && player.Current < player.Maximum * .3f)
                volume *= .65f;
            float step = Time.unscaledDeltaTime / fade * Mathf.Max(volume, .05f);
            voices[active].volume = Mathf.MoveTowards(voices[active].volume, volume, step);
            int fading = 1 - active;
            voices[fading].volume = Mathf.MoveTowards(voices[fading].volume, 0f, step);
            if (voices[fading].volume <= .001f && voices[fading].isPlaying) voices[fading].Stop();
        }
    }
}

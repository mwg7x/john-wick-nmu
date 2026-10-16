using System;
using System.Collections;
using LastStep.AI;
using LastStep.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastStep.Core
{
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        // The sole global service: survives scene changes. Duel state belongs to the future DuelManager.
        public static GameManager Instance { get; private set; }
        [SerializeField, Tooltip("Shared balance asset; do not mutate it to save player preferences.")] private DuelSettings duelSettings;
        [SerializeField, Tooltip("Order: Easy, Medium, Hard, Legendary.")] private DifficultySettings[] difficulties;
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string duelScene = "Duel_Staircase";
        private const string Key = "LastStep.";
        private bool loading;
        private float prePauseTimeScale = 1f;

        public event Action<bool> PauseChanged;
        public event Action SettingsChanged;
        public event Action<string> SceneLoaded;
        public DuelSettings DuelSettings => duelSettings;
        public GameMode Mode { get; private set; }
        public DominantHand Hand { get; private set; }
        public bool Seated { get; private set; }
        public bool ComfortVignette { get; private set; }
        public bool SnapTurning { get; private set; }
        public float CalibratedHeight { get; private set; }
        public float AudioVolume { get; private set; }
        public float MusicVolume { get; private set; }
        public float Sensitivity { get; private set; }
        public int DifficultyIndex { get; private set; }
        public int OpponentIndex { get; private set; }
        public int PracticeOpponentIndex { get; private set; } = -1;
        public bool IsPractice => PracticeOpponentIndex >= 0;
        public bool QuickDuel { get; private set; }
        public int SkyPresetIndex { get; private set; }
        public int PistolIndex { get; private set; }
        public int MapIndex { get; private set; }
        public string SelectedDuelScene => MapIndex == 1 ? "Duel_NMU" : duelScene;
        public bool HighEffects { get; private set; }
        public int DifficultyCount => difficulties != null ? difficulties.Length : 0;
        public OpponentRoster.Entry CurrentOpponent => OpponentRoster.Get(DifficultyIndex, IsPractice ? PracticeOpponentIndex : OpponentIndex);
        public int OpponentCount => IsPractice ? 1 : OpponentRoster.Count(DifficultyIndex);
        public bool IsPaused { get; private set; }
        public bool IsLoading => loading;
        public DifficultySettings SelectedDifficulty =>
            difficulties != null && difficulties.Length > 0
                ? difficulties[Mathf.Clamp(DifficultyIndex, 0, difficulties.Length - 1)] : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            LoadSettings();
            if (GetComponent<LastStep.Audio.AdaptiveMusic>() == null)
                gameObject.AddComponent<LastStep.Audio.AdaptiveMusic>();
            if (duelSettings == null) Debug.LogWarning("LAST STEP: assign DuelSettings on GameManager.", this);
            if (SelectedDifficulty == null) Debug.LogWarning("LAST STEP: assign four difficulty assets on GameManager.", this);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (IsPaused) Time.timeScale = prePauseTimeScale;
            Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { SceneLoaded?.Invoke(scene.name); }

        public void StartDuel()
        {
            if (duelSettings == null || SelectedDifficulty == null)
            { Debug.LogError("LAST STEP: cannot start without duel and difficulty assets.", this); return; }
            OpponentIndex = 0;
            PracticeOpponentIndex = QuickDuel ? UnityEngine.Random.Range(0, OpponentRoster.Count(DifficultyIndex)) : -1;
            LoadScene(SelectedDuelScene);
        }
        public void StartPractice(int tier, int opponent)
        {
            if (tier < 0 || tier >= OpponentRoster.TierCount || opponent < 0 || opponent >= OpponentRoster.Count(tier)) return;
            SetDifficulty(tier);
            PracticeOpponentIndex = opponent;
            OpponentIndex = 0;
            LoadScene(SelectedDuelScene);
        }
        public void ReturnToMainMenu() { OpponentIndex = 0; PracticeOpponentIndex = -1; LoadScene(mainMenuScene); }
        public void RestartDuel() { LoadScene(SelectedDuelScene); }
        public bool AdvanceOpponent()
        {
            if (OpponentIndex + 1 >= OpponentCount) return false;
            OpponentIndex++;
            LoadScene(SelectedDuelScene);
            return true;
        }

        private void LoadScene(string sceneName)
        {
            if (loading) return;
            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            { Debug.LogError("LAST STEP: scene missing from Build Profiles: " + sceneName, this); return; }
            SetPaused(false);
            Time.timeScale = 1f;
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            loading = true;
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation != null) yield return operation;
            else Debug.LogError("LAST STEP: failed to load " + sceneName, this);
            loading = false;
        }

        public void TogglePause() { SetPaused(!IsPaused); }
        public void SetGameplayTimeScale(float scale)
        {
            scale = Mathf.Max(0f, scale);
            if (IsPaused) prePauseTimeScale = scale;
            else Time.timeScale = scale;
        }
        public void SetPaused(bool paused)
        {
            if (IsPaused == paused || (paused && loading)) return;
            IsPaused = paused;
            if (paused) { prePauseTimeScale = Time.timeScale; Time.timeScale = 0f; }
            else Time.timeScale = prePauseTimeScale;
            // XR tracking remains live; future input adapters and UI observe this event.
            PauseChanged?.Invoke(paused);
        }

        public void SetMode(GameMode mode) { Mode = mode == GameMode.VR ? GameMode.VR : GameMode.PC; SaveSettings(); }
        public void SetQuickDuel(bool quick) { QuickDuel = quick; SaveSettings(); }
        public void SetDifficulty(int index)
        {
            DifficultyIndex = Mathf.Clamp(index, 0, Mathf.Max(0, (difficulties?.Length ?? 0) - 1));
            OpponentIndex = 0;
            SaveSettings();
        }
        public void SetSkyPreset(int index)
        {
            SkyPresetIndex = ((index % 3) + 3) % 3;
            SaveSettings();
        }
        public void SetHighEffects(bool high) { HighEffects = high; SaveSettings(); }
        public void SetPistol(int index) { PistolIndex = PistolCatalog.Clamp(index); SaveSettings(); }
        public void SetMap(int index) { MapIndex = Mathf.Clamp(index, 0, 1); SaveSettings(); }
        public void SetAudio(float master, float music)
        {
            AudioVolume = Mathf.Clamp01(master); MusicVolume = Mathf.Clamp01(music); SaveSettings();
        }
        public void SetSensitivity(float value) { Sensitivity = Mathf.Clamp(value, 0.1f, 10f); SaveSettings(); }
        public void SetComfort(DominantHand hand, bool seated, bool vignette, bool snapTurning, float height)
        {
            Hand = hand == DominantHand.Left ? DominantHand.Left : DominantHand.Right;
            Seated = seated; ComfortVignette = vignette; SnapTurning = snapTurning;
            CalibratedHeight = Mathf.Clamp(height, 0.5f, 2.5f); SaveSettings();
        }

        private void LoadSettings()
        {
            Mode = PlayerPrefs.GetInt(Key + "Mode", 0) == 1 ? GameMode.VR : GameMode.PC;
            QuickDuel = PlayerPrefs.GetInt(Key + "QuickDuel", 0) == 1;
            Hand = PlayerPrefs.GetInt(Key + "Hand", 0) == 1 ? DominantHand.Left : DominantHand.Right;
            DifficultyIndex = Mathf.Clamp(PlayerPrefs.GetInt(Key + "Difficulty", 1), 0, Mathf.Max(0, (difficulties?.Length ?? 0) - 1));
            SkyPresetIndex = Mathf.Clamp(PlayerPrefs.GetInt(Key + "Sky", 0), 0, 2);
            PistolIndex = PistolCatalog.Clamp(PlayerPrefs.GetInt(Key + "Pistol", 0));
            MapIndex = Mathf.Clamp(PlayerPrefs.GetInt(Key + "Map", 0), 0, 1);
            HighEffects = PlayerPrefs.GetInt(Key + "HighEffects", 1) == 1;
            AudioVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(Key + "Audio", 0.8f));
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(Key + "Music", 0.6f));
            Sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(Key + "Sensitivity", 1f), 0.1f, 10f);
            Seated = PlayerPrefs.GetInt(Key + "Seated", 0) == 1;
            ComfortVignette = PlayerPrefs.GetInt(Key + "Vignette", 1) == 1;
            SnapTurning = PlayerPrefs.GetInt(Key + "Snap", 1) == 1;
            CalibratedHeight = Mathf.Clamp(PlayerPrefs.GetFloat(Key + "Height", 1.7f), 0.5f, 2.5f);
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt(Key + "Mode", (int)Mode);
            PlayerPrefs.SetInt(Key + "QuickDuel", QuickDuel ? 1 : 0);
            PlayerPrefs.SetInt(Key + "Hand", (int)Hand);
            PlayerPrefs.SetInt(Key + "Difficulty", DifficultyIndex);
            PlayerPrefs.SetInt(Key + "Sky", SkyPresetIndex);
            PlayerPrefs.SetInt(Key + "Pistol", PistolIndex);
            PlayerPrefs.SetInt(Key + "Map", MapIndex);
            PlayerPrefs.SetInt(Key + "HighEffects", HighEffects ? 1 : 0);
            PlayerPrefs.SetFloat(Key + "Audio", AudioVolume);
            PlayerPrefs.SetFloat(Key + "Music", MusicVolume);
            PlayerPrefs.SetFloat(Key + "Sensitivity", Sensitivity);
            PlayerPrefs.SetInt(Key + "Seated", Seated ? 1 : 0);
            PlayerPrefs.SetInt(Key + "Vignette", ComfortVignette ? 1 : 0);
            PlayerPrefs.SetInt(Key + "Snap", SnapTurning ? 1 : 0);
            PlayerPrefs.SetFloat(Key + "Height", CalibratedHeight);
            PlayerPrefs.Save();
            SettingsChanged?.Invoke();
        }

        public void Quit() { Application.Quit(); }
    }
}

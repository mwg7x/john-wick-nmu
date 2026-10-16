using TMPro;
using LastStep.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace LastStep.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button difficultyButton;
        [SerializeField] private Button modeButton;
        [SerializeField] private Button duelStyleButton;
        [SerializeField] private TMP_Text duelStyleLabel;
        [SerializeField] private Button mapButton;
        [SerializeField] private TMP_Text mapLabel;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_Text difficultyLabel;
        [SerializeField] private TMP_Text modeLabel;
        [SerializeField] private Button skyButton;
        [SerializeField] private TMP_Text skyLabel;
        [SerializeField] private Button effectsButton;
        [SerializeField] private TMP_Text effectsLabel;
        [SerializeField] private Button pistolButton;
        [SerializeField] private TMP_Text pistolLabel;
        [SerializeField] private TMP_Text pistolSummary;
        [SerializeField] private RawImage pistolPreview;
        [SerializeField] private Button weaponOpenButton;
        [SerializeField] private Button weaponEquipButton;
        [SerializeField] private Button weaponCloseButton;
        [SerializeField] private CanvasGroup weaponPanel;
        [SerializeField] private Button settingsOpenButton;
        [SerializeField] private Button settingsCloseButton;
        [SerializeField] private CanvasGroup settingsPanel;
        private int previewPistol;
        private static bool qaIntroStarted;
        private void Start()
        {
            previewPistol = Core.GameManager.Instance != null ? Core.GameManager.Instance.PistolIndex : 0;
            if (startButton != null) startButton.onClick.AddListener(() => Core.GameManager.Instance?.StartDuel());
            if (quitButton != null) quitButton.onClick.AddListener(() => Core.GameManager.Instance?.Quit());
            if (mapButton != null) mapButton.onClick.AddListener(() =>
            { var game = Core.GameManager.Instance; if (game != null) { game.SetMap(1 - game.MapIndex); Refresh(); } });
            if (difficultyButton != null) difficultyButton.onClick.AddListener(CycleDifficulty);
            if (modeButton != null) modeButton.onClick.AddListener(CycleMode);
            if (duelStyleButton != null) duelStyleButton.onClick.AddListener(() =>
            { var game = Core.GameManager.Instance; if (game != null) { game.SetQuickDuel(!game.QuickDuel); Refresh(); } });
            if (skyButton != null) skyButton.onClick.AddListener(CycleSky);
            if (effectsButton != null) effectsButton.onClick.AddListener(ToggleEffects);
            if (pistolButton != null) pistolButton.onClick.AddListener(CyclePistol);
            if (weaponOpenButton != null) weaponOpenButton.onClick.AddListener(OpenWeapons);
            if (weaponCloseButton != null) weaponCloseButton.onClick.AddListener(() => SetPanel(weaponPanel, false));
            if (weaponEquipButton != null) weaponEquipButton.onClick.AddListener(EquipPreview);
            if (settingsOpenButton != null) settingsOpenButton.onClick.AddListener(() => SetPanel(settingsPanel, true));
            if (settingsCloseButton != null) settingsCloseButton.onClick.AddListener(() => SetPanel(settingsPanel, false));
            SetPanel(weaponPanel, false); SetPanel(settingsPanel, false);
            Refresh();
            // Standalone screenshot QA can enter the real duel without desktop input automation.
            if (!qaIntroStarted && System.Array.Exists(System.Environment.GetCommandLineArgs(),
                argument => argument == "-laststep-qa-intro"))
            {
                qaIntroStarted = true;
                Core.GameManager.Instance?.StartDuel();
            }
        }
        private void CycleMode()
        {
            var game = Core.GameManager.Instance;
            if (game == null) return;
            game.SetMode(game.Mode == Core.GameMode.PC ? Core.GameMode.VR : Core.GameMode.PC);
            Refresh();
        }
        private void CycleDifficulty()
        {
            var game = Core.GameManager.Instance;
            if (game == null) return;
            game.SetDifficulty((game.DifficultyIndex + 1) % Mathf.Max(1, game.DifficultyCount));
            Refresh();
        }
        private void CycleSky()
        {
            var game = Core.GameManager.Instance;
            if (game == null) return;
            game.SetSkyPreset(game.SkyPresetIndex + 1);
            Refresh();
        }
        private void ToggleEffects()
        {
            var game = Core.GameManager.Instance;
            if (game == null) return;
            game.SetHighEffects(!game.HighEffects);
            Refresh();
        }
        private void CyclePistol()
        {
            previewPistol = (previewPistol + 1) % PistolCatalog.Count;
            Refresh();
        }
        private void OpenWeapons()
        {
            previewPistol = Core.GameManager.Instance != null ? Core.GameManager.Instance.PistolIndex : 0;
            Refresh(); SetPanel(weaponPanel, true);
        }
        private void EquipPreview()
        {
            Core.GameManager.Instance?.SetPistol(previewPistol);
            SetPanel(weaponPanel, false);
        }
        private static void SetPanel(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
        private void Refresh()
        {
            var game = Core.GameManager.Instance;
            if (difficultyLabel != null)
            {
                difficultyLabel.fontSize = 22;
                difficultyLabel.text = "DIFFICULTY: " + (game?.SelectedDifficulty != null ? game.SelectedDifficulty.DifficultyName.ToUpperInvariant() : "MEDIUM");
            }
            if (mapLabel != null) mapLabel.text = game != null && game.MapIndex == 1 ? "MAP: NMU" : "MAP: STAIRCASE";
            if (modeLabel != null) modeLabel.text = "MODE: " + (game != null && game.Mode == Core.GameMode.VR ? "VR" : "PC");
            if (duelStyleLabel != null) duelStyleLabel.text = game != null && game.QuickDuel
                ? "MODE: QUICK DUEL" : "MODE: LEVEL CAMPAIGN";
            if (skyLabel != null)
            {
                string[] skies = { "MOONLIT NIGHT", "STORMY NIGHT", "FOGGY CITY" };
                skyLabel.text = "SKY: " + skies[game != null ? game.SkyPresetIndex : 0];
            }
            if (effectsLabel != null) effectsLabel.text = "EFFECTS: " + (game == null || game.HighEffects ? "HIGH" : "LOW");
            int pistol = previewPistol;
            if (pistolLabel != null) pistolLabel.text = "WEAPON: " + PistolCatalog.Name(pistol).ToUpperInvariant();
            if (pistolSummary != null) pistolSummary.text = PistolCatalog.Summary(pistol);
            if (pistolPreview != null) pistolPreview.texture = Resources.Load<Texture2D>("WeaponPreviews/Pistol" + pistol);
        }
    }
}

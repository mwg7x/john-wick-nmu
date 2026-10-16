using LastStep.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastStep.UI
{
    [DisallowMultipleComponent]
    public sealed class DuelHUD : MonoBehaviour
    {
        [SerializeField] private DuelManager duel;
        [SerializeField] private Health player;
        [SerializeField] private Health enemy;
        [SerializeField] private FalseStartManager falseStarts;
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text notice;
        [SerializeField] private TMP_Text result;
        [SerializeField] private CanvasGroup resultGroup;
        [SerializeField] private CanvasGroup pauseGroup;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Image opponentPortrait;
        [SerializeField] private Image playerPortrait;
        [SerializeField] private TMP_Text playerHealthLabel;
        [SerializeField] private TMP_Text enemyHealthLabel;
        [SerializeField] private TMP_Text roundLabel;
        [SerializeField] private TMP_Text centerBrand;
        [SerializeField] private Image playerBar;
        [SerializeField] private Image enemyBar;
        [SerializeField] private Image staminaBar;
        [SerializeField] private TMP_Text ammoLabel;
        private float noticeUntil;
        private RoundManager rounds;
        private LastStep.Player.PlayerMovement movement;
        private LastStep.Weapons.WeaponController gun;
        private DamageVision damageVision;
        private Sprite playerSprite, opponentSprite;

        private void Start()
        {
            if (player == null || enemy == null)
            {
                var markers = FindAnyObjectByType<DuelPositionMarkers>();
                if (markers != null)
                {
                    if (player == null && markers.PlayerStandIn != null)
                        player = markers.PlayerStandIn.GetComponent<Health>();
                    if (enemy == null && markers.EnemyStandIn != null)
                        enemy = markers.EnemyStandIn.GetComponent<Health>();
                }
            }
            rounds = FindAnyObjectByType<RoundManager>();
            movement = FindAnyObjectByType<LastStep.Player.PlayerMovement>();
            gun = movement != null ? movement.GetComponent<LastStep.Weapons.WeaponController>() : null;
            damageVision = GetComponent<DamageVision>();
            if (damageVision == null) damageVision = gameObject.AddComponent<DamageVision>();
            damageVision.Initialize(player);
            if (gun != null) gun.ShotFired += OnPlayerShot;
            if (restartButton != null) restartButton.onClick.AddListener(() => GameManager.Instance?.RestartDuel());
            if (nextButton != null) nextButton.onClick.AddListener(() => GameManager.Instance?.AdvanceOpponent());
            if (menuButton != null) menuButton.onClick.AddListener(() => GameManager.Instance?.ReturnToMainMenu());
            if (resumeButton != null) resumeButton.onClick.AddListener(() => GameManager.Instance?.SetPaused(false));
            if (falseStarts != null) falseStarts.FalseStart += OnFalseStart;
            if (duel != null) duel.OnStateChanged += OnState;
            var game = GameManager.Instance;
            if (game != null) game.PauseChanged += OnPause;
            SetGroup(resultGroup, false);
            SetGroup(pauseGroup, false);
            if (notice != null) notice.text = "";
            if (opponentPortrait != null && game != null)
            {
                string key = game.CurrentOpponent.Name.Replace(' ', '_');
                var texture = Resources.Load<Texture2D>("OpponentPortraits/" + key);
                if (texture != null)
                {
                    opponentSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
                    opponentPortrait.sprite = opponentSprite;
                }
            }
            if (playerPortrait != null)
            {
                var texture = Resources.Load<Texture2D>("OpponentPortraits/Protagonist");
                if (texture != null)
                {
                    playerSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
                    playerPortrait.sprite = playerSprite;
                }
            }
        }
        private void OnDestroy()
        {
            if (falseStarts != null) falseStarts.FalseStart -= OnFalseStart;
            if (gun != null) gun.ShotFired -= OnPlayerShot;
            if (duel != null) duel.OnStateChanged -= OnState;
            var game = GameManager.Instance;
            if (game != null) game.PauseChanged -= OnPause;
            if (playerSprite != null) Destroy(playerSprite);
            if (opponentSprite != null) Destroy(opponentSprite);
        }
        private void Update()
        {
            if (status != null && duel != null && player != null && enemy != null)
            {
                var game = GameManager.Instance;
                string opponent = game != null ? game.CurrentOpponent.Name.ToUpperInvariant() : "OPPONENT";
                string sequence = game != null ? (game.IsPractice ? "PRACTICE" : $"{game.OpponentIndex + 1}/{game.OpponentCount}") : "";
                string phase = duel.CurrentState == GameState.Transition && rounds != null
                    ? $"  |  CHECKPOINT {rounds.CheckpointRemaining:0.0}m (WASD)"
                    : duel.CurrentState == GameState.Fire && rounds != null
                    ? $"  |  {rounds.CombatRemaining:0.0}s" : "";
                status.text = duel.CurrentState == GameState.Intro ? "" :
                    $"DUEL {sequence}  |  ROUND {duel.CurrentRound}  |  {duel.CurrentDistance:0} METERS{phase}";
                if (centerBrand != null) centerBrand.text = "JOHN WICK <color=#d61d32>NMU</color>";
                if (playerHealthLabel != null) playerHealthLabel.text = $"PLAYER  {player.Current}/{player.Maximum}";
                if (enemyHealthLabel != null) enemyHealthLabel.text = $"{opponent}  {enemy.Current}/{enemy.Maximum}";
            }
            if (playerBar != null && player != null) { playerBar.type = Image.Type.Filled; playerBar.fillMethod = Image.FillMethod.Horizontal; playerBar.fillAmount = (float)player.Current / player.Maximum; }
            if (enemyBar != null && enemy != null) { enemyBar.type = Image.Type.Filled; enemyBar.fillMethod = Image.FillMethod.Horizontal; enemyBar.fillAmount = (float)enemy.Current / enemy.Maximum; }
            if (staminaBar != null && movement != null) staminaBar.fillAmount = movement.Stamina / 100f;
            if (ammoLabel != null && gun != null && duel != null)
            {
                int ammo = gun.Ammunition;
                if (roundLabel != null) roundLabel.text = $"ROUND <color=#d91f34>{duel.CurrentRound}/3</color>  |  {duel.CurrentDistance:0}M";
                ammoLabel.text = new string('▮', ammo) + new string('▯', DuelManager.ShotsPerRound - ammo) +
                    $"   AMMO {ammo}/{DuelManager.ShotsPerRound}";
            }
            if (notice != null && Time.unscaledTime >= noticeUntil) notice.text = "";
        }
        private void OnFalseStart(int count, bool lost)
        {
            if (notice == null) return;
            notice.text = "FALSE START - YOU LOSE";
            noticeUntil = Time.unscaledTime + 2.5f;
        }
        private void OnPlayerShot(bool hit, Vector3 point)
        {
            if (duel != null && duel.CurrentState == GameState.Finisher && gun != null && gun.Headshots > 0 && notice != null)
            { notice.text = "HEADSHOT FINISH"; noticeUntil = Time.unscaledTime + 2f; }
        }
        private void OnState(GameState oldState, GameState current)
        {
            if (current == GameState.Countdown && duel != null && duel.CurrentRound == 3 && notice != null)
            { notice.text = "FINAL SHOWDOWN - 10 METERS"; noticeUntil = Time.unscaledTime + 2.2f; }
            if (current == GameState.Finisher && notice != null)
            { notice.text = "FINAL BULLET"; noticeUntil = Time.unscaledTime + 1.5f; }
            bool finished = current == GameState.Victory || current == GameState.Defeat;
            SetGroup(resultGroup, finished);
            if (finished && result != null)
            {
                var game = GameManager.Instance;
                string name = game != null ? game.CurrentOpponent.Name : "Opponent";
                string weapon = LastStep.Weapons.PistolCatalog.Name(game != null ? game.PistolIndex : 0);
                string tier = game != null && game.SelectedDifficulty != null
                    ? game.SelectedDifficulty.DifficultyName.ToUpperInvariant() : "DUEL";
                result.text = current == GameState.Victory
                    ? $"WIN\n{name} defeated  |  {tier}\n{weapon}  |  HEADSHOTS {gun?.Headshots ?? 0}"
                    : falseStarts != null && falseStarts.PlayerDisqualified
                    ? $"FALSE START - YOU LOSE\n{name}\nRetry or Main Menu"
                    : $"LOSE\n{name}\nRetry or Main Menu";
                if (nextButton != null) nextButton.gameObject.SetActive(current == GameState.Victory &&
                    game != null && !game.IsPractice && game.OpponentIndex + 1 < game.OpponentCount);
            }
            if (current != GameState.Paused) SetGroup(pauseGroup, false);
        }
        private void OnPause(bool paused) { SetGroup(pauseGroup, paused); }
        private static void SetGroup(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}

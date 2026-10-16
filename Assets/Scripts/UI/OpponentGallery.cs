using LastStep.AI;
using LastStep.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastStep.UI
{
    [DisallowMultipleComponent]
    public sealed class OpponentGallery : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button practiceButton;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text details;
        [SerializeField] private Image portrait;
        private int tier;
        private int index;
        private Sprite transientSprite;

        private void Start()
        {
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(() => Show(false));
            if (previousButton != null) previousButton.onClick.AddListener(() => Step(-1));
            if (nextButton != null) nextButton.onClick.AddListener(() => Step(1));
            if (practiceButton != null) practiceButton.onClick.AddListener(() => GameManager.Instance?.StartPractice(tier, index));
            Show(false);
        }
        private void Open()
        {
            tier = GameManager.Instance != null ? GameManager.Instance.DifficultyIndex : 0;
            index = 0;
            Refresh();
            Show(true);
        }
        private void Step(int direction)
        {
            int absolute = index;
            for (int i = 0; i < tier; i++) absolute += OpponentRoster.Count(i);
            absolute = (absolute + direction + 11) % 11;
            tier = 0;
            while (absolute >= OpponentRoster.Count(tier)) absolute -= OpponentRoster.Count(tier++);
            index = absolute;
            Refresh();
        }
        private void Refresh()
        {
            var entry = OpponentRoster.Get(tier, index);
            string[] labels = { "EASY", "MEDIUM", "HARD", "LEGENDARY" };
            if (title != null) title.text = entry.Name.ToUpperInvariant();
            if (details != null) details.text = labels[tier] + "  •  OPPONENT " + (index + 1) + "/" + OpponentRoster.Count(tier) + "\nPRACTICE DUEL AVAILABLE";
            var texture = Resources.Load<Texture2D>("OpponentPortraits/" + entry.Name.Replace(' ', '_'));
            if (portrait == null || texture == null) return;
            if (transientSprite != null) Destroy(transientSprite);
            transientSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f,.5f));
            portrait.sprite = transientSprite;
        }
        private void Show(bool visible)
        {
            if (panel == null) return;
            panel.alpha = visible ? 1f : 0f;
            panel.interactable = visible;
            panel.blocksRaycasts = visible;
        }
        private void OnDestroy() { if (transientSprite != null) Destroy(transientSprite); }
    }
}

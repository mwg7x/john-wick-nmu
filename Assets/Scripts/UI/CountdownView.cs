using LastStep.Core;
using TMPro;
using UnityEngine;

namespace LastStep.UI
{
    [DisallowMultipleComponent]
    public sealed class CountdownView : MonoBehaviour
    {
        [SerializeField] private CountdownController countdown;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text roundLabel;
        [SerializeField] private TMP_Text distanceLabel;
        [SerializeField] private TMP_Text cueLabel;
        [SerializeField] private Color normalColor = new Color(0.94f, 0.94f, 0.9f);
        [SerializeField] private Color fireColor = new Color(1f, 0.2f, 0.22f);

        public string DisplayedCue => cueLabel != null ? cueLabel.text : "";
        public bool IsVisible => group != null && group.alpha > 0f;

        private void OnEnable()
        {
            if (countdown != null) countdown.CueChanged += Refresh;
            if (Ready) Refresh(countdown.CurrentCue);
        }

        private bool Ready => countdown != null && group != null && roundLabel != null && distanceLabel != null && cueLabel != null;

        private void Start()
        {
            if (!Ready)
            {
                Debug.LogError("LAST STEP: incomplete CountdownView references. Run Phase 4 setup.", this);
                enabled = false;
                return;
            }
            Refresh(countdown.CurrentCue);
        }

        private void OnDisable() { if (countdown != null) countdown.CueChanged -= Refresh; }

        private void Refresh(CountdownCue cue)
        {
            if (!Ready) return;
            group.alpha = cue == CountdownCue.Hidden ? 0f : 1f;
            group.interactable = false;
            group.blocksRaycasts = false;
            var duel = countdown.Duel;
            roundLabel.text = duel != null ? "ROUND " + duel.CurrentRound : "";
            distanceLabel.text = duel != null ? duel.CurrentDistance.ToString("0.#") + " METERS" : "";
            cueLabel.text = cue switch
            {
                CountdownCue.Three => "3",
                CountdownCue.Two => "2",
                CountdownCue.One => "1",
                CountdownCue.Draw => "DRAW",
                CountdownCue.Fire => "FIRE",
                _ => ""
            };
            cueLabel.color = cue == CountdownCue.Fire ? fireColor : normalColor;
        }
    }
}

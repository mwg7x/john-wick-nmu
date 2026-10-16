using UnityEngine;

namespace LastStep.Core
{
    public sealed class BootLoader : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        private void Start()
        {
            if (gameManager == null)
            { Debug.LogError("LAST STEP: BootLoader needs the Boot scene's GameManager.", this); return; }
            gameManager.ReturnToMainMenu();
        }
    }
}

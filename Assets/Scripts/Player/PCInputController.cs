using System;
using System.Collections.Generic;
using LastStep.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastStep.Player
{
    [DisallowMultipleComponent]
    public sealed class PCInputController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private DuelManager duel;
        [SerializeField] private FalseStartManager falseStarts;
        private InputActionMap map;
        private InputAction fire;
        private InputAction aim;
        private InputAction look;
        private InputAction move;
        private InputAction pause;
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        public event Action FirePressed;
        public Vector2 Look => look == null ? Vector2.zero : look.ReadValue<Vector2>() *
            (look.activeControl?.device is Gamepad ? 1000f * Time.deltaTime : 1f) *
            (GameManager.Instance != null ? GameManager.Instance.Sensitivity : 1f);
        public Vector2 Move => move != null ? move.ReadValue<Vector2>() : Vector2.zero;
        public bool IsAiming => aim != null && aim.IsPressed();
        public bool IsFiring => fire != null && fire.IsPressed();
        public bool SprintHeld => Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
        public bool DodgePressed => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        public bool ReloadPressed => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        public bool ShoulderSwitchPressed => Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
        public float Lean => Keyboard.current == null ? 0f :
            (Keyboard.current.eKey.isPressed ? 1f : 0f);

        private void OnEnable()
        {
            if (actions == null) { Debug.LogError("LAST STEP: assign LastStep input actions.", this); return; }
            map = actions.FindActionMap("Player", false);
            if (map == null) { Debug.LogError("LAST STEP: Player input map missing.", this); return; }
            fire = map.FindAction("Fire");
            aim = map.FindAction("Aim");
            look = map.FindAction("Look");
            move = map.FindAction("Move");
            pause = map.FindAction("Pause");
            if (fire != null) fire.performed += OnFire;
            if (pause != null) pause.performed += OnPause;
            map.Enable();
        }
        private void OnDisable()
        {
            if (fire != null) fire.performed -= OnFire;
            if (pause != null) pause.performed -= OnPause;
            if (map != null) map.Disable();
        }
        private void OnFire(InputAction.CallbackContext context)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            // Raycast now: IsPointerOverGameObject in an input callback refers to
            // the previous UI update and can misclassify the click that closes UI.
            if (context.control.device is Pointer pointer && IsPointerOverUI(pointer.position.ReadValue())) return;
            SubmitGameplayFire();
        }
        public bool IsPointerOverUI(Vector2 position)
        {
            if (EventSystem.current == null) return false;
            uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, uiHits);
            foreach (var hit in uiHits)
                if (hit.module is GraphicRaycaster) return true;
            return false;
        }
        public void SubmitGameplayFire()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            if (duel != null && duel.CurrentState == GameState.Countdown)
            { if (falseStarts != null) falseStarts.ReportEarlyTrigger(); return; }
            if (duel != null && duel.IsFireAllowed) FirePressed?.Invoke();
        }
        private void OnPause(InputAction.CallbackContext context)
        { if (GameManager.Instance != null) GameManager.Instance.TogglePause(); }
    }
}

# Phase 3 - Duel game-state system

Project: `D:\New Folder\jhon wik`. Unity: **6000.6.5f1**.

## Objective and scope

Provide a scene-local authority for duel states, explicit guarded transitions, event notifications, and integration with the existing persistent pause service. Phase 3 does not run timers, shoot, apply damage, move participants, or select the next distance automatically. Those coordinators will call this API in later phases.

## Files and assignments

| Path relative to project | Purpose / assignment |
| --- | --- |
| `Assets/Scripts/Core/DuelManager.cs` | Runtime component on the new root `DuelSystems` in `Assets/Scenes/Duel_Staircase.unity`. Owns duel state and round metadata. |
| `Assets/Editor/Phase3Setup.cs` | Creates the missing component and assigns settings; preserves existing environment and an existing settings assignment. |
| `Assets/Editor/DuelManagerEditor.cs` | Inspector displaying current state, round/distance and state-level shooting permission; Editor-only transition test buttons. |
| `Assets/Editor/Phase3Validation.cs` | Automated Play mode checks and scene-lifetime regression. |
| `Assets/Scenes/Duel_Staircase.unity` | Existing scene extended with DuelSystems; staircase prefab retained. |

Inspector assignments are already configured: `Settings` references `Assets/ScriptableObjects/DuelSettings.asset`; `Begin On Start` is enabled. No manual wiring is required. No new packages are required by Phase 3. Existing GameState, GameManager and DuelSettings sources remain unchanged.

## State contract

The normal transition sequence is:

```text
Boot -> Intro -> Preparing -> Countdown -> Fire -> Resolving
                                                   |
                         Resolving -> Transition -> Preparing
                         Resolving -> Victory
                         Resolving -> Defeat
                         Countdown -> Defeat (future disqualification)
```

Boot is the component's uninitialized state. Start enters Intro automatically. MainMenu remains a scene-flow responsibility of GameManager; a DuelManager does not persist into that scene. Victory and Defeat are terminal until GameManager reloads the duel scene.

| Method | Valid source / effect |
| --- | --- |
| `TryBeginDuel()` | Boot -> Intro after initialization. |
| `TryPrepareRound(roundNumber, stageIndex)` | Intro -> Preparing requires round 1 / stage index 0. Transition -> Preparing requires the next round number and an in-range stage index. Reports distance from the shared settings asset. |
| `TryStartCountdown()` | Preparing -> Countdown; no timer is started in this phase. |
| `TryAllowFire()` | Countdown -> Fire. |
| `TryBeginResolution()` | Fire -> Resolving; no damage calculation is performed. |
| `TryBeginTransition()` | Resolving -> Transition, after an external coordinator has resolved the exchange. |
| `TryDeclareVictory()` | Resolving -> Victory. |
| `TryDeclareDefeat()` | Resolving or Countdown -> Defeat. Countdown entry is a hook for later false-start disqualification. |

All methods return false for invalid order, duplicate calls, paused/disabled/uninitialized components, or reentrant state changes during event delivery. Invalid requests do not change state or emit state-entry events. Round stage selection is supplied externally; automatic 30/20/10 progression and physical movement remain Phase 8 work.

`IsFireAllowed` is a state permission gate, true only in Fire while the component is enabled and the persistent service is not paused. It does not implement per-participant shot availability, cooldown, or health checks. Weapons must add those checks in their scheduled phases.

## Events and pause

Events: `OnStateChanged(previous, current)`, `OnRoundStarted(round, distance)`, `OnCountdownStarted`, `OnFireAllowed`, `OnRoundFinished`, `OnVictory`, and `OnDefeat`.

The state updates before notifications. OnStateChanged fires first, followed by the corresponding entry signal. OnRoundFinished fires when leaving Resolving for Transition/Victory/Defeat, after external resolution completes. A Countdown disqualification emits OnDefeat without pretending an exchange was completed. OnPlayerShot and OnEnemyShot are deferred until real shot accounting is implemented.

DuelManager subscribes to GameManager.PauseChanged, stores the previous state, and reports Paused while paused. Resume restores that state without replaying round/countdown/FIRE/result events. Disabling/unloading removes the subscription. Re-enabling catches up to the persistent pause state. A pause requested by an event listener is synchronized after current notifications finish. State listeners must schedule subsequent transitions after notification delivery rather than recursively advancing the state.

Starting Duel_Staircase directly supports local state testing. Start through Boot for persistent pause and scene-flow services. No keyboard/controller polling has been added to gameplay code.

## How to test

1. In Edit mode run **LAST STEP > Phase 3 > Validate State System**. The command configures the scene, tests repeated setup, enters Play from Boot, exercises states and outcomes, restarts the scene, visits MainMenu, starts a fresh duel, and exits Play mode.
2. Expected output: `Logs/phase3-validation.txt`, Result PASS, 241 assertions, scene-flow stage 6.
3. For manual state inspection, open Duel_Staircase, press Play, and select DuelSystems. Expected initial state: Intro, round 0, shooting permission false.
4. Use the Inspector buttons to prepare round 1, enter Countdown, signal FIRE, and begin resolution. Permission becomes true only in Fire. Choose a transition or result. The test buttons do not simulate weapons, damage, timers, or movement.
5. When started through Boot, pause/resume via the Inspector controls and verify the original state returns. Restart should create a fresh Intro state. MainMenu intentionally remains blank pending Phase 9.

## Completed validation - 2026-10-14

- Actual Unity 6000.6.5f1 Play mode validation: **PASS**, 241 assertions, stage 6.
- Tested guarded and duplicate transitions, invalid round/stage values, state event ordering and exact signal counts, reentrant-call rejection, pause/resume from active and terminal states, pause inside an event listener, disable/re-enable synchronization, victory, defeat and Countdown disqualification paths.
- Tested four externally supplied round contexts including repeated final-distance metadata; no automated round progression or movement is claimed.
- Tested persistent GameManager identity, scene-local DuelManager destruction, restart reset, return to MainMenu, and fresh scene entry.
- Phase 2 regression with DuelManager installed: **PASS**, all 30/20/10-meter positions, 768 floor samples, 410 renderers, three lights, scene-flow stage 4.
- Console immediately after final Phase 3 validation: one success message, zero warnings/errors. A later inspection found two Unity Account API timeout warnings and a UnityConnect token-exchange error from Editor account services. These later service diagnostics are separate from the successful state tests; they remain unresolved. No authentication settings were changed.
- Strict project verification after implementation: 192 files scanned; zero findings for metadata, GUIDs, conflicts, manifest or Editor-version drift.
- Preservation comparison against `D:\New Folder\LAST_STEP-before-Phase3-20261014-015833.zip`: 153 existing Assets/Packages/ProjectSettings files checked; only Duel_Staircase.unity changed. All pre-existing scripts, prefabs, materials, settings, Boot and MainMenu files matched at that check.

Raw evidence: `Logs/phase3-validation.txt` and `Logs/phase2-validation.txt`. Logs are generated locally and ignored by version control.

## Remaining and unverified

Phase 4 will supply countdown timing and the 3/2/1/DRAW/FIRE presentation using the existing settings and state API. Weapon shot budgets, false-start input handling, health, AI, automatic round progression, full UI, audio, animation, visual polish and VR remain scheduled work. No Windows/IL2CPP build, headset test, frame-rate benchmark, or real combat test was performed in Phase 3.

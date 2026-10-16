# Phase 4 - Countdown

Project: `D:\New Folder\jhon wik`. Unity baseline: **6000.6.5f1**.

## Objective

Run the original 3, 2, 1, DRAW, FIRE sequence with round/distance presentation and keep the state-level shooting gate closed until FIRE. Retain the staircase, shared settings, and Phase 3 state authority. No weapon input, damage, AI, audio or automatic participant movement is added in this phase.

## Files and assignments

| Project-relative path | Responsibility / assignment |
| --- | --- |
| `Assets/Scripts/Core/CountdownController.cs` | Attached alongside DuelManager on `DuelSystems`. `Duel` references that manager. `Prepare First Round` is enabled. |
| `Assets/Scripts/UI/CountdownView.cs` | Attached to `CountdownCanvas/CountdownPanel`. References CountdownController, its own CanvasGroup, and child Round/Distance/Cue TMP labels. |
| `Assets/Editor/Phase4Setup.cs` | Creates missing countdown components/display and assigns references. Existing scene content and assigned references are retained. |
| `Assets/Editor/Phase4Validation.cs` | Measures cue timings and tests pause, display, permission, cancellation and scene lifetime in Play mode. |
| `Assets/Editor/DuelManagerEditor.cs` | Manual FIRE override is disabled while the timed controller is enabled. |
| `Assets/Editor/Phase3Validation.cs` | Disables the timed coordinator in state-only test scenes, keeping its existing deterministic scenarios usable. |
| `Assets/Scenes/Duel_Staircase.unity` | Existing scene extended with the controller and screen-space countdown canvas. |
| `Assets/TextMesh Pro` | Essential TMP font/material/shader resources imported from the installed uGUI package when absent. |

All required Inspector references are assigned by the setup command. No manual wiring is required. The installed uGUI package supplies TextMeshPro; no new package dependency is required. Existing user-added Unity AI packages are retained.

## Behaviour

The controller prepares round 1 after Intro and begins the countdown after Preparing, outside state-event delivery. This is the functional prototype path; a later cinematic coordinator can disable Prepare First Round and call TryPrepareRound after its intro completes. Each subsequent Preparing state starts another countdown using its current round/distance metadata. Automatic stage selection and physical movement remain Phase 8 work.

The display reads ROUND 1, 30 METERS, then 3 / 2 / 1 / DRAW / FIRE. FIRE is red; other cues use a light neutral color on a dark translucent panel. Canvas scaling uses a 1920x1080 reference with balanced width/height scaling. Text does not intercept input. The screen-space UI is for the PC prototype; VR presentation is deferred to the VR conversion phase.

Existing `Assets/ScriptableObjects/DuelSettings.asset` controls:

- Countdown Duration: 1 second for EACH number.
- Draw Duration: 0.6 seconds.
- Fire Display Duration: 1 second.

The timer uses scaled delta time and refuses to advance while the duel is Paused. Resume continues the remaining time without replaying cues. The FIRE cue is emitted only after DuelManager accepts the Fire state. Hiding the FIRE label does not close the firing window or enter Resolving: future weapon/health coordination decides when the exchange is complete. This preserves the requirement that a surviving player may shoot after the opponent.

Leaving Countdown/Fire for a result, resolution or transition cancels the visible cue. Disabling the controller unsubscribes from events and freezes its timer; re-enabling synchronizes with current state. Scene unload destroys the controller, so there are no timer coroutines or delayed callbacks into a new scene.

The public `CueChanged` event and read-only CurrentCue/RemainingSeconds properties provide integration points for later audio and UI work. Full HUD and menu work remain Phase 9; false-start input detection and per-participant shot availability require the later weapon system.

## Reproduce tests

In Edit mode run **LAST STEP > Phase 4 > Validate Countdown**. Expected: automated Boot/MainMenu/duel flow; 3/2/1/DRAW/FIRE measured against the shared settings; pause during each cue; cancellation during a later exchange; restart and unload checks; automatic exit from Play mode. Read `Logs/phase4-validation.txt` for the actual result and measured scaled durations. Assertion count includes frame-by-frame checks and can vary with frame rate.

For a direct visual check, open Duel_Staircase, select Game, and press Play. Expected: ROUND 1 / 30 METERS and the countdown, followed by FIRE hiding while the state remains Fire. There is no shooting control yet. Stop Play mode to return to the authored scene.

**LAST STEP > Phase 4 > Configure Countdown** is available to restore missing components. It preserves an existing countdown display instead of replacing authored layout edits.

## Validation status

The completed automated Play mode run on Unity 6000.6.5f1 passed all scenarios, reaching scene-flow stage 6. It recorded 11,478 assertions including repeated frame checks; this is not a count of unique test cases.

| Cue | Measured scaled duration | Configured duration |
| --- | --- | --- |
| 3 | 1.002 s | 1 s |
| 2 | 1.001 s | 1 s |
| 1 | 1.001 s | 1 s |
| DRAW | 0.601 s | 0.6 s |
| FIRE display | 1.002 s | 1 s |

Pause/resume during all five cues, state permission, view text/visibility, cancellation on defeat, a second externally prepared exchange, restart reset, and unloading a running countdown passed. Hiding FIRE preserved the open firing window. The Phase 3 regression also passed its 241 assertions with the countdown coordinator disabled for deterministic state-only tests.

Strict project verification scanned 286 files and reported zero findings. The final automated-test Console showed no compiler/runtime errors. The separately installed Unity account-service package can still emit Account API timeout diagnostics; these are not countdown failures.

Development fixes: replaced the obsolete asset-package import API with Unity 6.6's supported API, and made initial TMP-resource setup wait for asynchronous font import. The first import attempt failed before Play mode; the successful validation above ran after the font was available. The automatic first-import resume branch has not been exercised on a second clean copy. The bundled font also emitted an old importer-metadata warning during its initial import.

Visual inspection and the final handoff checks were interrupted by the user's Escape input and are being completed separately. No claim is made yet that the on-screen layout has been visually verified.

Windows/IL2CPP builds, headset behaviour, actual weapon firing, audio cues and platform frame-rate targets remain unverified or unimplemented at this phase.


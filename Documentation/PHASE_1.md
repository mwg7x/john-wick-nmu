# Phase 1 — Architecture and core foundation

## Objective and scope

Create a maintainable Unity 6 foundation for LAST STEP. Stop before environment, shooting, health behaviours, AI behaviour, UI, and VR gameplay. GameManager manages scenes, pause, and saved user choices; the future DuelManager will own GameState and exchanges. DifficultySettings is data only, not an AI implementation. WeaponData is data only, not a weapon implementation.

All paths below are relative to **D:\New Folder\jhon wik**. Source code is fully written on disk; nothing needs to be pasted.

## Folder layout

```text
Assets/
  Art/Characters/ Environment/ Weapons/
  Audio/Music/ SFX/ Ambience/
  Materials/
  Prefabs/Characters/ Weapons/ Environment/ UI/
  Scenes/
  Scripts/Core/ AI/ Player/ Weapons/ UI/ VR/ Audio/ Utilities/
  ScriptableObjects/
  Settings/
  Editor/
Packages/
ProjectSettings/
Documentation/
```

Empty folders reserve the later systems without implementing them.

## Files and assignments

| File | GameObject / purpose | Inspector assignment |
| --- | --- | --- |
| Assets/Scripts/Core/GameState.cs | No GameObject; enum definitions | None |
| Assets/Scripts/Core/GameManager.cs | Boot scene, root GameObject named GameManager | Duel Settings = DuelSettings.asset; Difficulties size 3: Easy, Normal, Hard; Main Menu Scene = MainMenu; Duel Scene = Duel_Staircase |
| Assets/Scripts/Core/BootLoader.cs | Boot scene, GameObject named BootLoader | Game Manager = Boot scene's GameManager component |
| Assets/Scripts/Core/DuelSettings.cs | ScriptableObject; never attach to GameObject | Edit DuelSettings.asset |
| Assets/Scripts/AI/DifficultySettings.cs | ScriptableObject; never attach to GameObject | Edit Easy.asset, Normal.asset, Hard.asset |
| Assets/Scripts/Weapons/WeaponData.cs | ScriptableObject; never attach to GameObject | Edit Pistol.asset |
| Assets/Scripts/Weapons/HitZone.cs | No GameObject; enum | None |
| Assets/Editor/Phase1Setup.cs | Editor menu command; never attach to GameObject | None |
| Assets/Settings/LastStep.inputactions | Input action asset; no behaviour yet | See bindings below |
| Packages/manifest.json | Package dependencies | Imported by Unity |
| ProjectSettings/ProjectVersion.txt | Editor baseline | 6000.6.5f1 |

The setup command assigns the GameObject fields automatically. GameManager persists through scene changes and discards duplicates. BootLoader stays in Boot and is destroyed on scene transition. Do not add new GameManagers to MainMenu or Duel_Staircase. Start Play mode from Boot during this phase.

The setup command creates these files, preserving any existing scenes:

- Assets/Scenes/Boot.unity
- Assets/Scenes/MainMenu.unity
- Assets/Scenes/Duel_Staircase.unity
- Assets/Settings/LastStep_Renderer.asset
- Assets/Settings/LastStep_URP.asset
- Unity-generated project settings, import metadata, and package lockfile

Every scene shell has Main Camera with Camera, AudioListener, and UniversalAdditionalCameraData. Boot also has the two objects above. MainMenu and Duel_Staircase intentionally contain no gameplay. The setup command skips existing scenes, so it will not overwrite later work; it does reapply foundation project/render settings and build scene order.

## Required packages

| Package | Pinned version | Purpose |
| --- | --- | --- |
| com.unity.render-pipelines.universal | 17.6.0 | URP for the Unity 6.6 baseline |
| com.unity.inputsystem | 1.20.1 | New Input System |
| com.unity.xr.management | 4.6.1 | XR loader management |
| com.unity.xr.openxr | 1.16.1 | Future PC VR and Quest runtime |
| com.unity.xr.interaction.toolkit | 3.6.1 | Future XR Origin interactions and grabbing |
| com.unity.ugui | 2.6.0 | Unity UI and TextMeshPro in Unity 6 |
| com.unity.ide.visualstudio | 2.0.26 | Supported VS Code integration |

Built-in audio, animation, physics, particles, UI, XR, JSON, and IMGUI modules are also declared. Animator and Audio Mixer are Unity features, not extra Asset Store packages. Do not add the older standalone TextMeshPro package alongside Unity 6 uGUI. Import TMP Essential Resources from Window > TextMeshPro when UI work begins. XRI Starter Assets can be imported through Package Manager > XR Interaction Toolkit > Samples during the VR phase. No samples or XR loader are required to verify Phase 1 PC scene flow.

Version references: [Unity 6 release](https://unity.com/releases/editor/whats-new/6000.6.5f1), [URP changelog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.6/changelog/CHANGELOG.html), [Input System](https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.inputsystem.html), [XR management](https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.xr.management.html), [OpenXR](https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.xr.openxr.html), [XRI](https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.xr.interaction.toolkit.html).

## ScriptableObject assets

Five assets are already created under Assets/ScriptableObjects. To create additional assets, right-click there and choose Create > LAST STEP > Duel Settings, Difficulty Settings, or Weapon Data. Rename the new asset and edit its Inspector; assign replacement duel/difficulty assets on the Boot GameManager.

**DuelSettings.asset:** health 100; distances 30, 20, 10 meters; countdown duration 1 second per number; DRAW 0.6 seconds; FIRE display 1 second; one allowed false-start warning (second violation loses); transition duration 1.5 seconds. Distances are positive and non-increasing. GetDistance clamps an out-of-range stage to the last distance so future rounds may repeat at 10 meters.

| Asset | Reaction seconds | Accuracy | Miss chance | Critical chance | Aim speed |
| --- | --- | --- | --- | --- | --- |
| Easy.asset | 1.0–1.5 | 0.45 | 0.35 | 0.05 | 2 |
| Normal.asset | 0.6–1.0 | 0.65 | 0.20 | 0.10 | 4 |
| Hard.asset | 0.35–0.7 | 0.80 | 0.10 | 0.15 | 6 |

Accuracy describes future aim precision, while miss chance is a separate forced-miss probability. These fields are not two names for hit probability. Accuracy is capped below 1 and miss chance above 0 to avoid perfect AI.

**Pistol.asset:** range 100 meters; cooldown 0.2 seconds; damage Head 100, Chest 50, Torso 40, Arm 20, Leg 20; PC recoil 0.8 degrees. Optional Shot Sound, Muzzle Flash Prefab, and Impact Prefab are intentionally empty until their systems exist. Hit Mask currently includes all layers; the future controller must exclude the shooter's layer. Never apply the PC recoil value to a tracked VR camera.

## Input System setup

1. Allow Package Manager to resolve manifest.json.
2. Run the setup command, then verify Active Input Handling is Input System Package (New). Restart the Editor to activate the backend if needed.
3. Double-click Assets/Settings/LastStep.inputactions to inspect the two action maps.
4. Keep Generate C# Class disabled for now. Future PCInputController and VRInputController will consume actions and send intent to the shared gameplay system.

| Action | Type | Default binding |
| --- | --- | --- |
| Player/Aim | Button | Mouse right button |
| Player/Fire | Button | Mouse left button |
| Player/Pause | Button | Escape |
| Player/Look | Value / Vector2 | Mouse delta |
| VR/Fire | Button | Right XR controller triggerPressed |
| VR/Grab | Button | Right XR controller gripPressed |
| VR/Turn | Value / Vector2 | Left XR controller primary2DAxis |

The maps have KeyboardMouse and XR control schemes. Do not enable both gameplay maps together. Left-handed binding overrides, trigger adapters, and XR tracking actions will be implemented with the VR rig later; storing a handedness preference now does not remap these bindings. No direct keyboard/controller polling exists in the gameplay foundation.

## Scene flow, pause, and persistence

BootLoader calls GameManager.ReturnToMainMenu. GameManager validates that a target scene is in the build scene list, then loads asynchronously, preventing overlapping scene loads. It exposes SceneLoaded, PauseChanged, and SettingsChanged events. Subscribers must unsubscribe when disabled/destroyed in future components.

GameManager stores difficulty, PC/VR choice, audio/music values, sensitivity, handedness, seated mode, vignette, snap turning, and calibrated height through PlayerPrefs. Settings setters save immediately. Audio volumes are stored only; a later AudioManager will apply them to mixer groups. VR mode is stored only; XR initialization is deferred. Pause freezes scaled gameplay time while allowing future XR tracking/UI to remain live. Do not implement VR head tracking with scaled-time game logic.

GameState is defined but no component advances duel states yet. Future DuelManager owns that state machine and observes pause events without losing its previous duel state.

## Build Profiles

Unity 6 uses [Build Profiles](https://docs.unity3d.com/6000.6/Documentation/Manual/build-profiles.html). The setup command assigns the shared scene list in this order: Boot (0), MainMenu (1), Duel_Staircase (2). Custom profile scene overrides must retain those scenes with Boot first.

**Windows PC, Phase 1:** File > Build Profiles > Windows; select x86_64. Use Mono for a quick development build or install Windows IL2CPP support and the required C++ toolchain before selecting IL2CPP. Enable Development Build during testing. Use the shared URP asset and keep XR loaders disabled until the XR lifecycle exists. Targeting 60 FPS will need profiling after gameplay exists.

**Future PC VR:** use a separate Windows profile; configure OpenXR in Project Settings > XR Plug-in Management > Standalone, add the headset's controller interaction profile, and run OpenXR Project Validation. For a combined PC/VR executable, turn off Initialize XR on Startup and implement explicit loader start/stop after choosing VR. That lifecycle is not implemented in Phase 1. Use single-pass instanced stereo where validated by the target runtime. See [OpenXR project configuration](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.16/manual/project-configuration.html).

**Future Quest standalone:** install Android Build Support with SDK, NDK, and OpenJDK using Hub. Create an Android build profile, use IL2CPP and ARM64 (prepared by the setup command), configure OpenXR on the Android tab, select the Quest support feature and appropriate controller interaction profile, and resolve Project Validation issues. Select current supported Android API levels according to that installed OpenXR/Quest toolchain before building. Do not invent an API level from this foundation. Use a distinct low-cost URP quality asset, baked lighting, limited shadows/transparency, and on-device frame profiling. 72 FPS is a future acceptance target, not a measured result.

Neither a VR camera rig nor a Quest-ready build is included yet. Occlusion data, light baking, LODs, performance tuning, Audio Mixer assets, and animations require later content.

## Phase 1 test checklist

- Open in the designated Unity Editor and wait for package import and compilation. Expected: no compiler errors or missing packages.
- Run the Phase 1 setup command. Expected: all three scene files, URP assets, and build scene entries exist.
- Inspect Boot GameManager and BootLoader. Expected: every required reference is assigned as listed above.
- Open each settings asset. Expected: editable defaults match this guide; invalid negative values are clamped by OnValidate.
- Open the input asset. Expected: seven actions, correct action types, and bindings listed above.
- Play Boot. Expected: MainMenu becomes active and exactly one GameManager survives under DontDestroyOnLoad; the Game view remains dark and empty.
- Stop and play Boot again. Expected: no stale singleton or duplicate manager errors.
- Build and run the Windows development shell. Expected: the same Boot-to-MainMenu transition without missing-scene errors.
- Re-run setup after saving a scene change. Expected: existing scene content is preserved.

See VALIDATION.md for measured migration results and checks that remain unverified. Stop after this phase; the next authorized phase would be the primitive staircase greybox.

## Unity 6.6 migration and VS Code

The fixed Editor baseline is Unity 6.6, 6000.6.5f1. Its installed executable is `C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe`. The prior 6.0 source was backed up outside the project before migration. Existing runtime scripts, ScriptableObject values, and input bindings were preserved.

URP 17.6.0 and uGUI 2.6.0 match this Editor's bundled packages. Input System remains pinned to 1.20.1. XRI is updated to the Unity 6-compatible stable 3.6.1; installed XR packages are dependencies only, and no XR loader, rig, or feature is enabled. Packages/packages-lock.json records resolved dependencies.

Use `com.unity.ide.visualstudio` for VS Code integration, as described by [Unity's IDE support documentation](https://docs.unity.com/en-us/engine/6000.6/manual/programming-environment/environment-and-tools/scripting-ide-support). Do not install the deprecated com.unity.ide.vscode package. The workspace recommends Microsoft's Unity extension, which supplies C# dependencies. `.vscode/launch.json` provides Attach to Unity. External editor selection is a machine-level Unity preference; the project-generation files are regenerated by Unity.

`Assets/Editor/Phase1Validation.cs` is an editor-only integration check, not a GameObject component. It validates settings, imported assets, missing scripts, and two Play mode cycles through Boot to MainMenu. It writes results into Logs/phase1-validation.txt. It is invoked in a separate Editor session after setup so the new input backend has restarted.

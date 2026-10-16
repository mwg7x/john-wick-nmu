# Phase 2 - Staircase greybox

Project: `D:\New Folder\jhon wik`. Editor: **6000.6.5f1**.

## Objective

Build the original brief's primitive outdoor staircase, with readable scale, level duel staging areas, and clear sightlines. Phase 2 is environment construction. The game-state system, countdown, weapons, health, AI, and VR behaviour remain scheduled for their original later phases. The complete original brief is retained in `Documentation/ORIGINAL_BRIEF.txt`.

## Implemented scene

`Assets/Scenes/Duel_Staircase.unity` now contains the existing Main Camera and a connected `StaircaseGreybox` prefab instance. The default preview looks downhill over the blue player capsule toward the rust-colored enemy capsule at the 30-meter positions. These are 1.8-meter primitive scale references, with no weapons, health, input, or AI attached.

The environment contains:

- 60 primitive stair treads over 10 flights, with contrasting top-edge strips.
- 11 horizontal landings. The staircase spans about 51.19 meters along its horizontal axis, including the end landings, and descends 10 meters.
- A 12-meter-wide stair core and wider landings.
- Collidable stone side walls, with simple metal handrails and posts.
- 12 pillar instances and 10 lamp instances, made from reusable primitive prefabs.
- 39 distant building masses that establish a city background without copying a real or film location.
- Six named position transforms for 30, 20, and 10 meters, and a scene data component holding all three pairs.
- A restrained night-colored background and readable greybox lighting: one shadow-casting directional key plus two unshadowed local fill lights. Other lamp housings use a shared emissive material without individual lights.

Rain, wet surfaces, reflections, detailed sky, polished architecture, production characters, and cinematic post-processing remain Phase 12 work. There are no expensive weather or reflection effects in this greybox.

## Files and exact assignments

All paths below start at `D:\New Folder\jhon wik`.

| File | Assignment / responsibility |
| --- | --- |
| `Assets/Scripts/Core/DuelPositionMarkers.cs` | Attached to `StaircaseGreybox/DuelPositions`. Holds the shared DuelSettings reference, three marker pairs, and both capsule stand-in references. Draws editor selection gizmos. Does not move participants during gameplay. |
| `Assets/Scripts/Utilities/StaircaseGreyboxSettings.cs` | ScriptableObject definition; do not attach to a GameObject. |
| `Assets/Settings/StaircaseGreyboxSettings.asset` | Inspector-editable generation dimensions and skyline seed. Used by the editor builder. |
| `Assets/Editor/GreyboxAssets.cs` | Editor-only primitive, material, and reusable prop-prefab creation helpers. Do not attach to a GameObject. |
| `Assets/Editor/Phase2Setup.cs` | Editor-only scene generation. Menu: LAST STEP > Phase 2 > Create Missing Greybox. |
| `Assets/Editor/Phase2Validation.cs` | Editor-only geometry, Play mode, and image-capture checks. Menu: LAST STEP > Phase 2 > Validate Greybox and Scene Flow. |
| `Assets/Prefabs/Environment/StaircaseGreybox.prefab` | Connected environment root, including marker data and scale stand-ins. |
| `Assets/Prefabs/Environment/GreyboxPillar.prefab` | Reusable plinth, shaft, and capital. |
| `Assets/Prefabs/Environment/GreyboxLamp.prefab` | Reusable post, lamp housing, and cap. Most instances have no Light component. |
| `Assets/Prefabs/Characters/PlayerStandIn.prefab` | Primitive blue capsule scale reference. |
| `Assets/Prefabs/Characters/EnemyStandIn.prefab` | Primitive rust capsule scale reference. |
| `Assets/Materials/GB_*.mat` | Seven shared URP Lit materials: Stone, StoneEdge, Metal, Lamp, Skyline, Player, Enemy. |
| `Assets/Scenes/Duel_Staircase.unity` | Existing scene updated with environment, camera framing, and lighting settings. |
| `Documentation/Previews/phase2-30m.png` | Actual Unity-rendered 30-meter preview. |
| `Documentation/Previews/phase2-20m.png` | Actual Unity-rendered 20-meter preview. |
| `Documentation/Previews/phase2-10m.png` | Actual Unity-rendered 10-meter preview. |
| `Documentation/Previews/phase2-overview.png` | Actual Unity-rendered overview. |

The generated hierarchy is organized under `StaircaseGreybox`:

```text
StairsAndLandings
WallsAndRailings
LampsAndPillars
DistantCityGreybox
DuelPositions
  PlayerPosition_30 / EnemyPosition_30
  PlayerPosition_20 / EnemyPosition_20
  PlayerPosition_10 / EnemyPosition_10
ScaleStandIns
GreyboxLighting
```

Every required serialized reference is already assigned. There are no mandatory manual wiring steps. The original Boot and MainMenu scenes retain their Phase 1 behaviour. Open Duel_Staircase directly to inspect the environment; the blank MainMenu is still scheduled for UI implementation in Phase 9.

## Inspector settings and distance convention

`StaircaseGreyboxSettings.asset` defaults:

| Field | Value |
| --- | --- |
| Stair Width | 12 m |
| Landing Length | 2.2 m |
| Vertical Fraction | 0.2 |
| Maximum Step Rise | 0.18 m |
| Extension Spacing | 10 m along the slope |
| Extension Landings | 2 at each end |
| Skyline Seed | 2406 |

The six markers are placed on flat landing centers. Distance means the actual straight-line **3D distance between foot-position markers**, including their height difference. It is not the horizontal projection or the total walking route. The existing `DuelSettings.asset` remains the authority for stage distances.

With the defaults, the landing heights for the pairs are 9/3 m, 8/4 m, and 7/5 m. Marker facing is horizontal, +Z for the player and -Z for the enemy. A future weapon/AI can aim up or down at its target separately. No XR camera or participant is rotated by this data component.

Changing settings does not silently regenerate or overwrite an authored scene. The setup command only creates a missing greybox. Re-running it preserves an existing root, prefab edits, scene edits, and material edits. To make layout changes, edit the prefab/scene deliberately; a future regeneration workflow can be added if needed. Do not change DuelSettings distances without updating the environment and rerunning validation: the validator detects a distance mismatch.

## Packages and performance scope

The existing URP 17.6.0 and Unity 6.6 packages are retained. The only manifest addition is Unity's built-in `com.unity.modules.imageconversion` 1.0.0, used by the editor validator to save PNG render evidence. No Asset Store assets or third-party art were imported.

The greybox has 410 renderers and three lights. Structural meshes use shared primitive meshes/materials, GPU instancing-enabled materials, and static batching/occlusion flags. Geometry is generated in the Editor, not rebuilt per frame. Only the stair core, landings, side walls, and pillar blocks need environment colliders; decorative rails, lamps, tread strips, and the skyline have none. Capsule stand-ins use the Ignore Raycast layer so they do not interfere with geometry-only validation; these are not combat hit zones.

These are prototype budgets, not measured PC/Quest performance claims. Lighting is not baked, occlusion flags do not mean occlusion data has been baked, and no LOD or Quest optimization claim is made. Baked lighting, probes, LODs, and platform profiling remain scheduled work.

## How to inspect and test

1. Open `Assets/Scenes/Duel_Staircase.unity` and select the Game tab. Expected: a downhill greybox view with player and enemy capsules, stairs, lamps, walls, and background blocks.
2. Expand `StaircaseGreybox/DuelPositions` and select DuelPositions with Gizmos enabled. Expected: three colored marker pairs and measured-distance labels in Scene view.
3. Press Play in Duel_Staircase. Expected: a static environment preview. Shooting, aiming controls, countdown, AI, and locomotion are not implemented in Phase 2.
4. Stop Play mode, then run **LAST STEP > Phase 2 > Validate Greybox and Scene Flow**. Expected: the validator checks the environment, enters Play mode from Boot, loads MainMenu, enters Duel_Staircase, returns to MainMenu, enters the duel scene again, and exits Play mode.
5. Read `Logs/phase2-validation.txt`. Expected: PASS, 30.000/20.000/10.000 m, 768 supported floor samples, 410 renderers, three lights, and scene-flow stage 4.
6. Inspect `Documentation/Previews`. These PNGs are generated from Unity's renderer, not illustrations or mockups.

The validator verifies required references, missing-script components, material support, marker grounding, capsule standing clearance, unobstructed sightlines, collision continuity across three lanes, scene reloads, a single persistent GameManager, and pause/resume. It also calls setup twice and checks that the second call does not change the existing scene file.

For a closed project, the same validation can run with the actual Editor executable:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe' -batchmode -buildTarget Win64 -projectPath 'D:\New Folder\jhon wik' -executeMethod LastStep.Editor.Phase2Validation.Run -logFile 'D:\New Folder\jhon wik\Logs\phase2-editor.log'
```

Do not add `-quit`; the validator exits after its Play mode checks. Do not run a second Editor against a project that is already open.

## Remaining scope

This environment is ready to host Phase 3's duel-state system. It is not yet the complete playable prototype. Countdown comes in Phase 4, PC shooting in Phase 5, health/hit zones in Phase 6, AI in Phase 7, and round movement in Phase 8. `DuelPositionManager` and VR fades should be implemented when participant movement is introduced, consuming these markers rather than duplicating them.

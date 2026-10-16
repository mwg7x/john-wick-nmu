# Phase 2 validation - 2026-10-14

Editor: Unity 6.6 (6000.6.5f1), Windows, URP. Validation ran in the existing interactive Unity Editor through LAST STEP > Phase 2 > Validate Greybox and Scene Flow. No standalone player build was run.

## Completed tests

- Final automated result: PASS, scene-flow stage 4.
- Boot -> MainMenu -> Duel_Staircase -> MainMenu -> Duel_Staircase succeeded; the same single GameManager persisted. Pause/resume passed.
- Stage marker distances: 30.000, 20.000, and 10.000 meters, measured in 3D between foot markers.
- All six markers were grounded on flat landings and had capsule standing clearance. Each pair had an unobstructed eye-to-chest sightline.
- 768 raycast samples across three lanes found supported floor surfaces throughout the stair route.
- Required references, script components and supported materials passed. Environment: 410 renderers, three lights, one shadow-casting light.
- Repeating scene setup preserved existing scene bytes.
- Actual Unity render captures generated for 30/20/10-meter staging and the overview. Visual review led to corrected tread-edge strips and delayed first capture until scene rendering was ready; final captures were regenerated after those fixes.
- Final visible Unity Console: two success messages, zero warnings, zero errors. No captured runtime errors or exceptions during the validated scene loop.
- Strict Unity project verification: 184 files scanned, zero errors, zero warnings, zero unverifiable checks. Metadata presence/orphans, duplicate GUIDs, conflict markers, manifest validity and expected Editor version all passed.
- Preservation comparison: all 17 checked Phase 1 files matched the pre-Phase-2 backup byte-for-byte (seven runtime scripts, five settings assets, input asset, Boot/MainMenu scenes, and both Phase 1 editor utilities).

## Evidence

- Raw runner result: `Logs/phase2-validation.txt` (local generated log).
- Render evidence: [30 m](Previews/phase2-30m.png), [20 m](Previews/phase2-20m.png), [10 m](Previews/phase2-10m.png), [overview](Previews/phase2-overview.png).
- [Implementation and assigned assets](PHASE_2.md).
- Backup: `D:\New Folder\LAST_STEP-before-Phase2-20261013-184810.zip`.

An earlier batch launch could not acquire the already-open project. It is not successful test evidence; the completed tests above ran through the interactive Editor. The final scene-render timing issue and obsolete-API warning encountered during development were corrected before the successful final run.

## Unverified and deferred

No interactive movement controller, countdown, shooting, health, AI, or round progression exists in this phase, so no claims are made about those mechanics. The raycast support test is not a character-controller traversal test. No Windows/IL2CPP build, VR device test, Quest profiling, debugger-attachment test, baked lighting/occlusion, or frame-rate benchmark was performed. The static greybox establishes geometry and staging, not final cinematic visual fidelity.

Phase 3's duel-state system is the next milestone. Later phases remain tracked in [ROADMAP.md](ROADMAP.md).

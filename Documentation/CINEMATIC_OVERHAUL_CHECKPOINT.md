# Cinematic overhaul checkpoint - 2026-10-16

Recovery archive: `Backups/pre-cinematic-overhaul-2026-10-16.zip` (Scripts, Editor, Scenes, Packages and ProjectSettings before this pass).

Implemented in existing Unity 6000.6.5f1 project:
- Authoritative enemy shot ray can report a near miss only when its unobstructed segment passes 0.28-1.2m from the player head. It never applies damage.
- Imported Kenney Impact Sounds CC0 (`Assets/Resources/Audio/KenneyImpact`): concrete footsteps and varied body, stone, metal and wood impacts. The original gun report remains synthesized; near-miss whizz is synthesized.
- Surface impact audio is chosen from the actual raycast collider name. Existing impact visuals remain generic.
- Enemy timing, cadence, precision and limited lateral repositioning differ by Precision/Aggressive/Defensive/Elite personality. The four existing difficulty assets and roster remain authoritative.
- Finisher slow-motion is limited to 0.65 real seconds and can be skipped with Space; RoundManager restores normal time on completion and disable.
- Round 3 has a FINAL SHOWDOWN HUD callout and modestly tighter camera FOV. Ammo remains 3 per round.
- Direct weapon fire during countdown now calls the false-start disqualification path before ammo validation.
- Wet-stone material variants and top faces are configured on staircase surfaces, but actual standalone capture still shows low-detail flat gray steps. This visual issue is not resolved.

Validation: Unity Play Mode `UpgradeValidation.Run` PASS, 141 checks, including near-miss geometry, imported audio presence, direct early-fire defeat, all 3 rounds and time-scale restoration after finisher. Windows Mono build SUCCESS, 0 errors, 501 warnings. Standalone intro launched without logged exceptions; actual screenshot `Documentation/Previews/overhaul-intro-2026-10-16.png`.

Not yet validated: physical player movement/animation feel, actual near-miss listening test, all surface sounds by collision, adaptive music, graphics performance profiling, Next Opponent UI click, VR device testing. Cinemachine, Animation Rigging and Timeline were not installed; current gameplay camera and animation systems were preserved. Realistic human models and authored animation clips, detailed environment meshes, true wet reflections, spatial bullet audio, AudioMixer and pooled impacts remain production gaps.

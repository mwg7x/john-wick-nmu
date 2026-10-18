# Status ? 2026-10-18

Project: JOHN WICK NMU / LAST STEP. Public repository: https://github.com/mwg7x/john-wick-nmu
Unity 6000.6.5f1, URP 17.6.0 and Input System 1.20.1 retained.

Implemented in the existing project:

- Exactly three fresh shots at 50/30/10m; no reload refill, fourth round, held-trigger replay or UI-triggered shot; countdown early fire forfeits.
- NMU evening campus: three branded coaches on the left; palms, lamps, banners, bollards, paving, gate/security details, asphalt diffuse/normal/wetness, reflection probe and cloud sky. Original Staircase preserved.
- Forward+ within URP removes the old four-light-per-object limit. Palm pieces combined into two meshes per tree.
- Both HUD health numbers/bars track actual damage. 512px portraits from gameplay models; unsupported ammo glyphs replaced by three UI slots.
- Imported solid character materials corrected from transparent to opaque while retaining textures. Damage vision uses an independent volume. Surface dust requires a real collision.
- Walking: velocity-driven forward/reversed-backward clips, smoother starts/stops/reversals, directional lower-body turning, torso counter-rotation, two-bone arm reach, aim-walk/backward/side speed control and forward sprint with exhaustion recovery.
- Harder opponents: faster reactions, higher accuracy, wider/faster strafing on stronger tiers, headshot attempts and observed-motion leading. Aim commits before firing, allowing evasion; no extra ammo or health inflation.
- Recorded CZ pistol report and pain vocals; separate footstep/voice sources. Source/license notes beside assets and in ASSET_MANIFEST.md.

Executed reports are in Documentation/Validation. Standalone evidence uses virtual keyboard input through the real movement controller, disables enemy attacks for repeatability and records actual frames. The performance sample is not a worst-case benchmark. Failed/earlier captures remain locally excluded from Git.

Remaining quality limits: simplified campus/coach/foliage geometry; probe-based reflections; shared roster base meshes with limited facial/eye detail; no complete authored tactical strafe/dodge/hit/death mocap library or verified foot planting for every motion. The game is improved but does not match the reference image's photoreal quality. These limits are not reported as completed work.

Local source checkpoints: Backups/before-nmu-enhancements-20261016 and Backups/before-cinematic-target-20261017. Historical README and failing baseline retained in Documentation. Build products, caches, reference images and backups are excluded from source publication.

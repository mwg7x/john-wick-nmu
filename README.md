# JOHN WICK NMU — LAST STEP

Unity duel project with the original Staircase and a playable New Mansoura University entrance map. This is a work in progress: character realism, directional animation coverage and environment detail do not yet meet the full cinematic brief.

Open this folder in **Unity 6000.6.5f1**, then open `Assets/Scenes/Boot.unity`. Dependencies are pinned in `Packages/manifest.json` and `packages-lock.json`: URP 17.6.0, Input System 1.20.1 and glTFast 6.19.0. Do not upgrade them automatically.

In the menu, open **Settings → Map** to choose Staircase or NMU. Staircase is the default for fresh preferences. Map choice persists across restart, practice and Next Opponent. Opponent tiers, roster, pistol choices, campaign/quick duel, coin toss and finishers are retained.

Controls: WASD move, mouse aim, left click one shot per press, right click aim, Q shoulder switch, Shift sprint, Space dodge/skip finisher, Escape pause. Movement during combat follows the existing round restrictions. Each round starts with exactly three fresh shots at 50m, 30m and 10m. Firing during the active countdown immediately forfeits; reload/equip cannot refill ammo. UI clicks are filtered from gameplay input.

Changes include UI/fire separation, no held-trigger replay across states, strict three-round progression, camera self-collision filtering, pause-aware cinematic time restoration, acceleration/braking, and use of the existing suited rig's walk clip in the velocity blend. These do not constitute a complete mocap/character overhaul.

The NMU cinematic pass adds three left-side university coaches, palms, street and gate lighting, paving, bollards, banners, wet asphalt maps and a cloud sky. URP Forward+ lets the roadside lights affect the courtyard without the old four-light-per-object limit. The HUD keeps its original regions with synchronized numeric health/bars, 512px rendered portraits and real ammo slot graphics. Recorded pistol/pain audio supplements the existing licensed impacts and music.

Movement now supports aim-walking, controlled backward/side steps, reversed backward animation, lower-body turning with a steady aim torso, and a two-bone grip solver. Forward sprinting stops while aiming and recovers from exhaustion before restarting. Difficulty tiers now react faster and aim more accurately; stronger enemies strafe, lead observed motion and attempt headshots. Their aim is committed before the shot, allowing a late direction change to evade it. Health, ammunition and countdown rules remain unchanged.

Validation and evidence:

- `Documentation/Validation/`: current executed reports and build results.
- `Documentation/baseline-validation.txt`: preserved failing baseline. Older success claims in `Documentation/LEGACY_README.md` are historical.
- `Tools/validate.ps1`: sequential Play Mode invariants, both-map regression and Windows Mono build; accepts a custom `-Unity` executable path.
- `Assets/Editor/NMUSetup.cs`: editable original architectural source and regeneration menu. See `Documentation/NMU_RECONSTRUCTION.md` for inferred dimensions and limitations.
- `Assets/Editor/NMUCinematicSetup.cs`: additive cinematic pass; run **LAST STEP > NMU > Apply Cinematic Campus** after regenerating the base map.
- `Documentation/ASSET_MANIFEST.md` and `Documentation/ASSET_SOURCES.md`: provenance, terms, dependencies and asset gaps.
- Windows output, generated locally and excluded from Git: `Builds/WindowsMono/LAST STEP.exe`.
- Optional standalone evidence: launch with `-duel-evidence` (Staircase) or `-duel-evidence -nmu`. It uses a virtual keyboard through the real movement controller, disables enemy attacks for repeatability, records rendered frames and writes a limited 1080p performance sample under `Builds/WindowsMono/Evidence/`. Encode with `python Tools/encode_evidence.py <evidence-folder>` (OpenCV required). This is scripted evidence, not manual acceptance or a worst-case benchmark.

No blanket license is granted for the entire project. Third-party assets retain their documented terms; original project work is not relicensed here. Two unused legacy pistol candidates with unresolved provenance are retained locally and excluded from the public repository. The university reference photograph is not uploaded.

Remaining quality gaps are tracked in `STATUS.md`. A clone includes runtime source/assets and Unity project settings. Library, caches, local settings, backups and build output are excluded.

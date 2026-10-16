# LAST STEP development roadmap

The original requirements are preserved in ORIGINAL_BRIEF.txt. Unity baseline: **6.6 (6000.6.5f1)**. The user authorized continuous development across phases on 2026-10-14.

| Phase | Milestone | Status |
| --- | --- | --- |
| 1 | Project setup | Complete; Boot to MainMenu validated |
| 2 | Greybox staircase | Complete; geometry, markers, clearance and sightlines validated |
| 3 | Game state | Complete; guarded state and pause transitions validated |
| 4 | Countdown | Complete; timed 3/2/1/DRAW/FIRE sequence validated |
| 5 | PC weapon | Playable prototype; selected pistol, muzzle-based trajectory and FIRE gating validated; three authoritative shots per round |
| 6 | Health and hit zones | Playable prototype; seven zones, damage and lethal shot validated |
| 7 | AI opponent | Playable prototype; four tiers with distinct reaction, aim, miss chance and aggression; 11 named opponents |
| 8 | Round progression | Playable prototype; 50/30/10-meter progression, three fresh shots at each official round start, sequential tier opponents, restart and win/loss validated |
| 9 | UI | Functional cinematic NMU menu with original generated background, Start Game, Weapon Selection/Equip, Opponents, Settings and Exit; ammo HUD and four difficulty tiers; automated button checks passed, visual manual QA pending |
| 10 | Audio | Procedural placeholder cues, gunshots and wind wired; custom assets and mixer pending |
| 11 | Animations | Original procedural human visuals and bob/recoil/hit/death reactions; authored animation clips pending |
| 12 | Visual polish | Original cinematic menu artwork, nighttime skyline, imported character and pistol meshes, HUD and hit effects; in-game character realism and authored animation pass pending |
| 13 | VR conversion | XR Origin, tracked hands/head, physical aim, trigger, haptics, comfort fade and snap turn scaffold; headset/device test and XRI grabbing pending |
| 14 | Quest optimization | Not started; Android module, build profile and Quest device required |
| 15 | Build and testing | Latest Unity Editor regression passed 134 checks for menu actions, three-shot round allocation, ammo HUD, combat, checkpoints and victory; Windows Mono build succeeded and launched; IL2CPP blocked by missing C++ toolchain; visual/manual and VR device testing pending |

The latest upgrade regression is in `Logs/upgrade-validation.txt`. It covers menu panel buttons and Equip, false starts, player and AI damage, fourth-shot rejection, round ammo refresh, hit reaction, HUD health, checkpoint transitions and victory. It does not simulate real headset tracking, physical mouse movement, visual quality, or performance.

## Next milestone

Visually inspect the updated standalone menu and two-line ammo HUD at target resolutions, then test mouse and controller feel. Finish authored sound, motion, environment art and a proper audio mixer. Install Visual Studio C++ build tools for IL2CPP; complete XRI grab interaction and headset/Quest testing separately.


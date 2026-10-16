# Asset manifest — 2026-10-16

Status distinguishes presence/integration from production-quality validation. Existing source licenses were rechecked where linked below; no new external asset packs were imported. Original project code/art are not relicensed by this publication. Third-party files retain their own licenses.

| Scope / local path | Source and terms | Use / dependencies | Status and gaps |
| --- | --- | --- | --- |
| `Assets/Art/MakeHuman/`, `Assets/Resources/CharacterS/` | [vsim credits](https://github.com/kunalkushwaha/vsim/blob/main/packages/assets/library/CREDITS.md), MakeHuman outputs described as CC0 | Suited humans / glTFast 6.19.0, existing Animator | Existing/integrated; production realism, identity fidelity and Humanoid deformation validation blocked |
| `Assets/Art/OpenGameArt/BaldMan/` | [EmoPreben](https://opengameart.org/content/bald-man-rigged), CC0 | Alternate human / FBX importer | Existing; source documents ear defects; not production-quality validation |
| `Assets/Art/Kenney/Protagonists/` | [Kenney](https://kenney.nl/assets/animated-characters-protagonists), CC0 | Fallback rig and clips / Animator | Existing/integrated; coherent tactical mocap library missing |
| `Assets/Art/PolyPizza/BalancedAlt/` | [Pichuliru, Pistol Full East](https://poly.pizza/m/pNHeIJFHcQ), CC0 | Balanced pistol / imported FBX, shared WeaponController | Existing/integrated; all-character grip review outstanding |
| `Assets/Art/PolyPizza/Fast/` | [CreativeTrio, Revolver](https://poly.pizza/m/wFQbxzzgqU), CC0 | Fast pistol / same | Existing/integrated |
| `Assets/Art/PolyPizza/Powerful/` | [AdamKokrito, Colt 1911](https://poly.pizza/m/zmuVJOUn4p), CC0 | Powerful pistol / same | Existing/integrated |
| `Assets/Art/PolyPizza/Column/`, `Lamp/` | [Quaternius column](https://poly.pizza/m/wLubNpOTX4), [lamp](https://poly.pizza/m/nFwrlcLvM5), existing CC0 record | Original staircase architecture | Existing/integrated |
| `Assets/Art/PolyPizza/Balanced/`, `FastAlt/` | Older evaluation assets; precise provenance not yet established | Unused candidates | Blocked for public redistribution; excluded from publication until provenance is resolved |
| `Assets/Art/PolyHaven/StoneTiles/` | [Stone Tiles](https://polyhaven.com/a/stone_tiles), [CC0 terms](https://polyhaven.com/license) | Original map PBR textures / URP | Existing/integrated |
| `Assets/Art/NMU/`, `Assets/Prefabs/Environment/NMUEntrance.prefab` | Original geometry/materials authored in `Assets/Editor/NMUSetup.cs`; user photograph used only as local visual reference | NMU 3D architecture / URP Lit, TMP | Created; proportions inferred; detail/LODs incomplete |
| `Assets/Art/Stone/`, `Assets/Art/Menu/`, `Assets/Art/Sky/` | Existing original generated project artwork, recorded in ASSET_SOURCES.md | Stone, menu and distant city imagery / URP, UI | Existing/integrated; no new generated character portraits claimed |
| `Assets/Resources/OpponentPortraits/`, `WeaponPreviews/` | Existing renders from project models / inherited model licenses | Roster and weapon UI | Existing/integrated; likeness quality not validated |
| `Assets/Scripts/Weapons/ShotEffects.cs` and weapon prefabs | Original project effects | Muzzle/tracer/casing/impact / accepted-shot callbacks | Existing/integrated; pooling review outstanding |
| `Assets/Resources/Audio/KenneyImpact/` | [Kenney Impact Sounds](https://kenney.nl/assets/impact-sounds), CC0 | Footsteps and impacts / AudioSource | Existing/integrated; foot-contact synchronization incomplete |
| `Assets/Resources/Audio/SRG774/` | [SRG774 Dark Sci-Fi Audio Pack](https://opengameart.org/content/dark-sci-fi-audio-pack), existing CC0 record | Adaptive music / persistent audio service | Existing/integrated |
| Other project audio / `Assets/Scripts/Audio/` | Original procedural sounds recorded in prior source documentation | Gunshot, UI and reactions | Existing; authentic handling, breathing, pain and ambience library incomplete |
| Lighting and UI / `Assets/Scenes/`, `Assets/TextMesh Pro/` | Original scene setup; bundled TMP font licensing retained | Both maps / URP 17.6.0, UGUI/TMP | Existing plus NMU daylight created; visual QA pending |

Cinemachine, Animation Rigging and Timeline were evaluated against the existing custom camera/rig/state implementation: no package added without a demonstrated need. They would not supply missing character quality or animation clips. Current package versions and lockfile are retained. Importing external editor scripts was unnecessary.

Animation audit correction: the existing `suited.glb` contains idle, run, walk and wave clips. The earlier controller ignored walk. `MotionPolishSetup` now blends idle/walk/run against measured horizontal speed and keeps root motion disabled. Walking playback is initially tuned to 1.6x at 2.8m/s; foot-contact calibration and strafing/backward/dodge/hit/death clips remain incomplete. The upstream credits identify walk as CMU subject 07, trial 01, retargeted by vsim. The [CMU FAQ](http://mocap.cs.cmu.edu/faqs.php) permits copying, modification and redistribution; credit: Carnegie Mellon University Graphics Lab Motion Capture Database, BVH conversion by B. Hahne/cgspeed. This motion source has its own permission terms and is not described here as CC0. The FAQ was checked directly on 2026-10-16 (HTTPS endpoint unavailable; HTTP endpoint accessible).

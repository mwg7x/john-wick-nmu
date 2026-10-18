# NMU reconstruction

Reference inspected locally: `C:/Users/MWG/Desktop/NMU.webp`. The photograph is not bundled or uploaded. It depicts NEW MANSOURA UNIVERSITY, a charcoal rectangular entrance portal, paired lower pavilions with blue vertical glazing and cream fins, beige campus buildings, red tapered pylons, a security kiosk, asphalt and black/yellow curbs.

Original editable source: `Assets/Editor/NMUSetup.cs`. Engine exports: `Assets/Prefabs/Environment/NMUEntrance.prefab`, `Assets/Art/NMU/`, and `Assets/Scenes/Duel_NMU.unity`. Regenerate with LAST STEP > NMU > Build Map and Selector. This overwrites the generated NMU scene, not the original Staircase scene.

One Unity unit is one meter. Estimated gate: 26.2m overall width, 13.6m height; pavilions: 14m wide, approximately 9.8m tall. These dimensions and unseen building depths are inferred, not surveyed. Courtyard is extended to accommodate the game. The shared checkpoint markers are placed at +/-25m, +/-15m and +/-5m on a flat central corridor, leaving the architecture beyond the 50m exchange. The architecture is solid 3D geometry with colliders; no reference photograph is used as a background.

Geometry uses modular roof, pier, window, mullion, curb and step pieces with standard box UVs; custom pylons have per-face UVs/normals. PBR scalar materials distinguish stone, glazing, limestone, terracotta and asphalt. Static geometry is flagged for batching. No navigation mesh is needed by the existing scripted duel AI. Architecture has no LOD chain yet. Material texture detail, planting, buses, Arabic lettering and finer reference details remain incomplete. This is a playable architectural approximation, not a photoreal or exact reconstruction.

Visual review reduced the inferred campus wings from 18m to 12m high so they do not dominate the main gate, enlarged the sign, and reused the existing CC0 Poly Haven stone albedo/normal maps on stone and limestone. The glass and asphalt remain scalar PBR materials. The side-building repetition is an approximation of unseen architecture, not a claim about the campus plan.

Gameplay uses the existing DuelManager, RoundManager, weapon, opponent, HUD and cinematic components. There is one scene-local duel authority per loaded map and one persistent GameManager. The map choice persists through restart, practice and Next Opponent. Staircase remains the default for fresh preferences.

## Cinematic pass, October 17

`Assets/Editor/NMUCinematicSetup.cs` adds the evening presentation to the existing NMU scene. Use LAST STEP > NMU > Apply Cinematic Campus after regenerating the base. This replaces only the named cinematic dressing root, materials and generated coach prefab; it preserves the duel actors, marker transforms and gameplay components.

Three 11m coaches occupy the west transport court, starting at x=-20m. Each has NMU lettering, navy/pearl livery, seats, smoked glazing, wheels and hubs, mirrors, luggage seams, head/tail lamps and roof equipment. A single hull collider per bus remains well outside the playable lane. Procedural palms, warm street pools, bollards, paving joints, banners, bench slats, gate accent strips, window lighting and security controls add scale and depth. Dressing uses shared materials and static batching; small decoration meshes have no colliders.

The road now uses CC0 Poly Haven Asphalt 02 diffuse/normal maps plus an original tileable smoothness mask. A 128px reflection probe captures the campus once at startup. Warm architectural lights contrast with cool moonlight, restrained fog and the existing original MoonlitClouds sky artwork. These are real scene materials/lights; reflected details are not painted into a screenshot. Geometry and foliage are still simplified, and reflections are probe-based rather than ray traced.

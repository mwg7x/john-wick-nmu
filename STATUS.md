# Status — 2026-10-16

Project: JOHN WICK NMU / LAST STEP. Unity 6000.6.5f1, URP 17.6.0, Input System 1.20.1 retained.

- Recoverable source checkpoint: `Backups/before-nmu-enhancements-20261016/` (local only).
- Audited existing scenes, shared duel authority, input, movement, aiming, animation, audio, packages and validation. No applicable AGENTS.md found.
- Opened the actual NMU reference. Created original editable entrance geometry generator, materials and a second scene sharing gameplay scripts. All architectural dimensions are estimates.
- Fixed UI/fire separation, removed held-trigger replay across states, rejected skipped/fourth round preparation, and fixed camera collision self-filtering. Added persistent map preference and Settings selector.
- Historical baseline log fails at stage 16 after 141 checks; README's historical success claim is not current evidence. Baseline attempt reproduced the timeout. Regression runner now observes final-round Fire state directly, with a deterministic seed.
- GitHub identity verified through existing Git Credential Manager: mwg7x. Public publication pending final asset/source review and validation.
- Blocked quality goals: realistic identity-preserving characters, coherent authored locomotion/dodge/hit/death animation library, validated Humanoid mappings for all roster entries, detailed faces/hands/hair/clothing, full surface/foot-contact audio and LODs. Existing procedural motion is retained and is not claimed as production mocap.
- Next: generate/inspect NMU, run both-map regression and focused state/input checks, build and launch Windows player, capture evidence, publish reviewed project.

# Sprint 5 — Art, Audio & Ship (2026-10-03 → 2026-10-12)

**Sprint goal:** Replace the greybox with the PO's chosen theme (models + real sound effects), then produce the final build and the report. Oct 13–14 is the buffer and submission to E3.

Milestone: [Sprint 5 — Art, Audio & Ship](https://github.com/hwupu/115A-535652-3DGP-PA1/milestone/6)

## Sprint backlog
| ID | Issue | Item | Owner | Status |
|---|---|---|---|---|
| PB-18 | #19 | Theme + asset integration (follow `docs/guides/asset-swap.md`) | User | ✅ |
| PB-16 | #17 | Real SFX for the 5 required events (+ collect / catch optional) | User finds, Claude wires if needed | ⬜ |
| PB-20 | #21 | Polish on request: ✅ ghost float-and-fade (ADR-0010); more as the PO asks | Claude | 🟨 |
| PB-28 | #29 | Animated player character (Little Witch), simple (ADR-0011) | Claude setup, User checks | ✅ |
| PB-21 | (#22 reopened if needed) | Final build + full rubric pass with final art | Both | ⬜ |
| PB-22 | #23 | Report PDF: **on hold until the PO asks** | Claude drafts, User reflects | ⬜ |

## Audio quick guide (PB-16)
1. Import the clips (`.wav` / `.ogg` / `.mp3`) into **`Assets/_Project/Audio/`** (same convention as `_Project/Models/`; free CC0 / CC-BY sounds are fine in the public repo). Unity **Asset Store** sound packs go in their own folder at the `Assets/` root instead, and get git-ignored like the skybox.
2. Select `Systems/AudioManager` → drag the clips into **Throw / Hit Target / Hit Obstacle / Invalid Selection / Enter Platform / Leave Platform** (+ Collect, Catch). Any empty slot keeps its placeholder tone.
3. Adjust **Volume**, or trim long clips (short SFX < 1 s feel best).
4. Add each sound to `docs/asset-credits.md` (freesound.org CC0 / CC-BY, Kenney, the Asset Store…).

## Player character quick guide (PB-28)
1. Open Unity (it imports the witch FBX), save the scene, then run **Boomerang Guardian → Setup → Player Character (Witch)**.
2. Press Play: idle when standing, walk/run when moving (SPACE = fast), Jump on F, Throw on a valid click; invisible in first person.
3. Tuning: `Player/Model` scale and position (size and feet), `AC_Player → Locomotion` thresholds and child speeds (leg cadence), `PlayerAnimator → Speed Damp Time`.

## Daily log
- **2026-10-03**: Sprint 4 closed; the PO starts the theme / asset work.

- **2026-10-05**: The PO imported the Sketchfab "Boomerang" (CC-BY) into `Assets/_Project/Models/Boomerang/` and swapped it into `Boomerang.prefab`; credits added (list format, PO preference). **Bug:** the boomerang didn't spin. Cause: the swap replaced the `Spinner` object, so `BoomerangProjectile.spinner` became empty. Also latent: the code spun around the visual's *local* Y, which would tumble an FBX imported with a -90° X rotation. Fix: spin via `RotateAround(root center, root up)`; empty link → auto-use the first visual child + warning; `WiringValidator` now also scans prefabs (verified: it flagged exactly this field); prefab re-linked at the PO's request. The asset-swap guide §4 is updated.

- **2026-10-05 (later)**: PO decisions: Halloween theme; PB-20 stays open as requested polish. First request: ghosts (new target model in `Target_Barrel`) should "flow upward and fade away" when hit instead of bouncing. Implemented `Target` Hit Reaction (Tumble / FloatAway), `Core/MaterialUtility`, `Editor/Sprint5Setup` (creates `M_TargetFade`, links it, sets FloatAway). Verified in a clone: compile OK; setup run 1 created/linked; run 2 made 0 changes; prefab shows `hitReaction: 1` + template; the material has `_SURFACE_TYPE_TRANSPARENT`. Not verifiable by AI: the look of the float and fade → PO play-test.

- **2026-10-05 (later)**: The PO confirmed the ghost float-and-fade ("it is looking good"). Prefab renamed `Target_Barrel` → `Target_Ghost` at the PO's request (`git mv` with `.meta`, so the GUID and all links are kept; root name updated; `Sprint2Setup` updated so a rerun can't recreate a stray `Target_Barrel`).

- **2026-10-05 (later)**: The PO swapped the gem for a **pumpkin** (Halloween pack); it stopped bobbing. Same cause as the boomerang: the swap replaced `Model`, so `Collectible.visual` was empty. Fix: loud fallback (first visual child + warning); `visual` is now a required field (the validator checks it); new **Visual Motion** `Spin` / `FacePlayer`. FacePlayer applies a world yaw toward the player on top of the authored import rotation, independent of the spawner's random root yaw. Prefab linked + set to FacePlayer at the PO's request. Clone test: validator ✓; with the root at yaw 137°, the angle between the pumpkin's front and the player = **0.00°**. The PO also added tombstone/ground models, a lighting bake, and a skybox pack (Asset Store? held back from the public repo pending the license).

- **2026-10-05 (later)**: The PO confirmed the skybox is from the Unity Asset Store → git-ignored, with a README re-import note and a credits entry. The PO accepted and closed PB-18. Remaining: PB-16 (SFX), PB-20 (polish on request), PB-22 (report, when the PO asks), plus the final build + rubric pass (PB-21 repeat).

- **2026-10-05 (later)**: The PO keeps the original intro wording. New ticket **PB-28 (#29)**: animated witch, with a handoff doc from another AI agent (Blender). Claude **ran the handoff's own verification checklist headless before building on it**: 5 clips with the expected lengths, faces +Z, `hand.R` present, Release pose extended forward, jump hip rise +0.34 m; one deviation: height 1.33 m, not 1.07 m. The PO then asked for a simple implementation ("it is okay to have glitches"), so ADR-0011 uses physics-first, a blend tree + 2 triggers, no events or layers. `PlayerMotor.Jumped`/`HorizontalSpeed`, `PlayerAnimator`, `PlayerCharacterSetup`. Clone: setup run 1 OK, run 2 = 0 changes, wiring ✓, macOS build OK, headless run without exceptions. Noticed: 1 failed obstacle placement with the new larger models (149/150, still within the rubric).

- **2026-10-06**: The PO ran the player setup and accepted PB-28 ("working alright"); committed the generated controller, material and import settings. PB-16 started; audio folder agreed: `Assets/_Project/Audio/`.

## Review

## Retro
- Keep:
- Problem:
- Try:

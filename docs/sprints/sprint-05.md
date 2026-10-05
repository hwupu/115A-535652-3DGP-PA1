# Sprint 5 — Art, Audio & Ship (2026-10-03 → 2026-10-12)

**Sprint goal:** Replace the greybox with the PO's chosen theme (models + real sound effects), then produce the final build and the report. Oct 13–14 is the buffer and submission to E3.

Milestone: [Sprint 5 — Art, Audio & Ship](https://github.com/hwupu/115A-535652-3DGP-PA1/milestone/6)

## Sprint backlog
| ID | Issue | Item | Owner | Status |
|---|---|---|---|---|
| PB-18 | #19 | Theme + asset integration (follow `docs/guides/asset-swap.md`) | User | ⬜ |
| PB-16 | #17 | Real SFX for the 5 required events (+ collect / catch optional) | User finds, Claude wires if needed | ⬜ |
| PB-20 | #21 | Polish on request: ✅ ghost float-and-fade (ADR-0010); more as the PO asks | Claude | 🟨 |
| PB-21 | (#22 reopened if needed) | Final build + full rubric pass with final art | Both | ⬜ |
| PB-22 | #23 | Report PDF: **on hold until the PO asks** | Claude drafts, User reflects | ⬜ |

## Audio quick guide (PB-16)
1. Import the clips (`.wav` / `.ogg` / `.mp3`) into `Assets/ThirdParty/<Pack>/` or `Assets/_Project/Audio/`.
2. Select `Systems/AudioManager` → drag the clips into **Throw / Hit Target / Hit Obstacle / Invalid Selection / Enter Platform / Leave Platform** (+ Collect, Catch). Any empty slot keeps its placeholder tone.
3. Adjust **Volume**, or trim long clips (short SFX < 1 s feel best).
4. Add each sound to `docs/asset-credits.md` (freesound.org CC0 / CC-BY, Kenney, the Asset Store…).

## Daily log
- **2026-10-03**: Sprint 4 closed; the PO starts the theme / asset work.

- **2026-10-05**: The PO imported the Sketchfab "Boomerang" (CC-BY) into `Assets/_Project/Models/Boomerang/` and swapped it into `Boomerang.prefab`; credits added (list format, PO preference). **Bug:** the boomerang didn't spin. Cause: the swap replaced the `Spinner` object, so `BoomerangProjectile.spinner` became empty. Also latent: the code spun around the visual's *local* Y, which would tumble an FBX imported with a -90° X rotation. Fix: spin via `RotateAround(root center, root up)`; empty link → auto-use the first visual child + warning; `WiringValidator` now also scans prefabs (verified: it flagged exactly this field); prefab re-linked at the PO's request. The asset-swap guide §4 is updated.

- **2026-10-05 (later)**: PO decisions: Halloween theme; PB-20 stays open as requested polish. First request: ghosts (new target model in `Target_Barrel`) should "flow upward and fade away" when hit instead of bouncing. Implemented `Target` Hit Reaction (Tumble / FloatAway), `Core/MaterialUtility`, `Editor/Sprint5Setup` (creates `M_TargetFade`, links it, sets FloatAway). Verified in a clone: compile OK; setup run 1 created/linked; run 2 made 0 changes; prefab shows `hitReaction: 1` + template; the material has `_SURFACE_TYPE_TRANSPARENT`. Not verifiable by AI: the look of the float and fade → PO play-test.

- **2026-10-05 (later)**: The PO confirmed the ghost float-and-fade ("it is looking good"). Prefab renamed `Target_Barrel` → `Target_Ghost` at the PO's request (`git mv` with `.meta`, so the GUID and all links are kept; root name updated; `Sprint2Setup` updated so a rerun can't recreate a stray `Target_Barrel`).

- **2026-10-05 (later)**: The PO swapped the gem for a **pumpkin** (Halloween pack); it stopped bobbing. Same cause as the boomerang: the swap replaced `Model`, so `Collectible.visual` was empty. Fix: loud fallback (first visual child + warning); `visual` is now a required field (the validator checks it); new **Visual Motion** `Spin` / `FacePlayer`. FacePlayer applies a world yaw toward the player on top of the authored import rotation, independent of the spawner's random root yaw. Prefab linked + set to FacePlayer at the PO's request. Clone test: validator ✓; with the root at yaw 137°, the angle between the pumpkin's front and the player = **0.00°**. The PO also added tombstone/ground models, a lighting bake, and a skybox pack (Asset Store? held back from the public repo pending the license).

## Review

## Retro
- Keep:
- Problem:
- Try:

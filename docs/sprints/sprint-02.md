# Sprint 2 — Core Loop (2026-10-03 → 2026-10-08)

**Sprint goal:** The full core loop works with greybox content. A random, non-overlapping spawn → click to pick → the boomerang curves to the target, hits it with a physical push and returns → the score updates. The platform hides and respawns the world. Collectibles give bonus points. Every value can be tuned through `config.json`.

Milestone: [Sprint 2 — Core Loop](https://github.com/hwupu/115A-535652-3DGP-PA1/milestone/3) · The PO decided to start early (Oct 3) and to drop story-point tracking ("we don't really need to worry about SP").

## Planning decisions (PO, 2026-10-03)
| Topic | Decision |
|---|---|
| Scene wiring | **Hybrid**: Claude's idempotent Editor setup scripts do the wiring; the PO does art and layout (ADR-0009) |
| Boomerang hits an obstacle | **Bounces back early** (thud, small push; the obstacle survives) |
| Invalid selections (error sound + message) | Non-target (obstacle / ground / wall / sky), target **too far** (> `maxThrowRange`, default 40 m), click **during cooldown** |
| Click on an already-hit target | Silently ignored (SM proposal, not one of the PO's options) |
| Scoring | Target +10, collectible +5 (in `config.json`) |

## Sprint backlog
| ID | Issue | Item | Status |
|---|---|---|---|
| PB-06 | #7 | Ray-cast object picking + invalid handling | ✅ |
| PB-07 | #8 | Boomerang throw / curve / return / 3 s cooldown | ✅ |
| PB-08 | #9 | Spawn Manager (330 objects, random, in bounds) | ✅ |
| PB-09 | #10 | No overlap at spawn | ✅ |
| PB-10 | #11 | Hit physics: push, visible 2 s, disappear | ✅ |
| PB-12 | #13 | Guardian Platform trigger (hide / respawn) | ✅ |
| PB-13 | #14 | Collectibles | ✅ |
| PB-14 | #15 | HUD (score, targets, speed/view, cooldown, messages) | ✅ |
| PB-24 | #25 | Runtime `config.json` + F5 | ✅ |
| PB-16 | #17 | *Pulled in partly:* AudioManager + placeholder tones (real clips stay in Sprint 3) | 🟨 |

## Task breakdown
**Claude**
- [x] Core: `GameConfig`, `ConfigLoader` (beside app → StreamingAssets → defaults, clamping, F5), `ScoreManager`
- [x] Interaction: `Target`, `Obstacle`, `Collectible`, `GuardianPlatform`, `TargetSelector`
- [x] Boomerang: `BoomerangThrower` (one projectile per throw; the cooldown is the only gate), `BoomerangProjectile` (Bézier outbound path, impulse on hit, bounce off obstacles, homing return, 8 s safety)
- [x] Spawning: `SpawnConfig` (ScriptableObject), `SpawnManager` (footprints, rejection sampling, CheckBox, ComputePenetration validation, Hide / Restore / Regenerate)
- [x] Audio: `AudioManager`, `PlaceholderTones`. UI: `HudController`
- [x] Editor: `SetupUtils`, `Sprint2Setup` (menu + batch)
- [x] Guide `docs/guides/sprint-02-setup.md`

**User**
- [x] U1 Import TMP Essentials, delete `Obstacles_Test`, run the setup menu
- [x] U2 Play-test the checklist, report feel and bugs
- [ ] U3 (carried over **again** to Sprint 3) Asset hunt for the theme; delete `Assets/Scenes/SampleScene.unity`

## Daily log
- **2026-10-03**: Planning (decisions above). Claude implemented all code + setup script. Verification without touching the open Editor: cloned the project with APFS copy-on-write into `/tmp`, then batch-compiled → 0 errors. Ran the setup script: found a bug (`OpenScene` unloads the in-memory prefab created earlier in the run, so the reference went stale), fixed it by opening the scene first, and re-ran → OK. A second run made no changes (idempotent). Imported TMP Essentials into the clone, and the HUD path works. Spawn smoke test ×3: 330 objects, 0 failed placements, **0 overlapping pairs**, 0 out of bounds, 4–9 ms. Not yet verified: Play-Mode behaviour (picking, flight, platform, audio, HUD); that needs the PO's play-test.

- **2026-10-03 (later)**: The PO imported TMP Essentials, deleted `Obstacles_Test`, ran the setup menu, and play-tested: "It is working great". The PO also captured a sprint-board screenshot for the report (`docs/report/screenshot-of-pa1-sprint-board.png`).

## Review (2026-10-03)
- **Sprint goal met on day 1 of a 6-day sprint.** The PO confirmed the full core loop in Play Mode.
- Done: PB-06, 07, 08, 09, 10, 12, 13, 14, 24. PB-16 is partly done (placeholder tones), so #17 moves back to Sprint 3 for the real clips.
- Carried over: theme / asset hunt; delete `Assets/Scenes/SampleScene.unity`.
- Traceability: every rubric category except the minimap (Cat. 14) and real SFX (Cat. 15) is now ✅.
- Observation: the hybrid setup made PO integration take about 10 minutes, against about 45–60 minutes of manual wiring in Sprint 1.

## Retro (2026-10-03, PO input)
- **Keep:** the auto-setup script ("very helpful") plus a clear guide. Sandbox verification before hand-off.
- **Problem:** no blockers. The PO raised follow-up questions: how to customize / vary target and obstacle models, the player facing while the camera rotates, GUI customization, and a perceived delay when objects spawn.
- **Try:** turn the questions into backlog items (PB-25 … PB-27). The PO will test facing and spawn delay more **before** we change anything (explicit instruction: "Don't change anything yet"). Re-plan into 5 sprints: art/audio moves to Sprint 5, after the logic polish.

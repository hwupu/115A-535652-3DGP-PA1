# Product Backlog

Owner: User (Product Owner) · Facilitator: Claude (Scrum Master)
Priority: MoSCoW. SP = story points (1 SP ≈ 1–2 h). Marks = rubric points covered.
Created 2026-10-01 (Sprint 0). This is a living document; refine it at each Sprint Planning.
This file is the source of truth. GitHub mirrors it: Issues + Milestones (one per sprint) + Project board https://github.com/users/hwupu/projects/1

| ID | Issue | User story / item | Acceptance criteria | Marks | Pri | SP | Sprint | Status |
|---|---|---|---|---|---|---|---|---|
| PB-00 | [#1](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/1) | *Setup*: repo, Unity project, docs skeleton, CLAUDE.md, ADRs, GitHub board | Project opens in 6000.3.25f1; first commit pushed to GitHub (public, PO decision) | – | Must | 2 | 0 | ✅ |
| PB-01 | [#2](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/2) | As a player I explore a **bounded arena** | Large ground (~100×100 m); cube walls on 4 sides; platform at the center; logical hierarchy & naming | 10 | Must | 2 | 1 | ✅ |
| PB-02 | [#3](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/3) | As a player I **move with W/S (+ A/D strafe) relative to the camera view**, smoothly | W forward / S back along the camera's flattened forward; A/D strafe along the camera's right (PO extra); acceleration smoothing; the character faces the move/camera direction | 8 | Must | 3 | 1 | ✅ |
| PB-03 | [#4](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/4) | As a player I **toggle normal/fast speed** with SPACE | Toggle (not hold); clear difference (e.g. 5 → 10 m/s); HUD indicator | 4 | Must | 1 | 1 | ✅ |
| PB-04 | [#5](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/5) | As a player I **jump with F** under gravity with a projectile arc | Jump only when grounded; gravity brings the player back; horizontal momentum is kept in the air | 8 | Must | 2 | 1 | ✅ |
| PB-05 | [#6](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/6) | As a player I **switch first-/third-person camera** | Free cursor; **hold RMB** to look. FP: camera at head, pitch + yaw (pitch clamped). TP: orbit pitch + yaw, scroll-wheel distance (clamped), wall-clip avoidance. Toggle key V | 8 | Must | 4 | 1 | ✅ |
| PB-06 | [#7](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/7) | As a player I **click objects to select them via ray casting** | LMB → `ScreenPointToRay`; target → throw; non-target → error SFX + UI hint; feedback while on cooldown | 7 | Must | 2 | 2 | ✅ |
| PB-07 | [#8](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/8) | As a player I **throw a boomerang that returns** | Spawns at the hand; suitable initial speed; curves to the selected target; returns to the player; 3 s cooldown (HUD); never lost (timeout fallback) | 12 | Must | 5 | 2 | ✅ |
| PB-08 | [#9](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/9) | As a designer I have a **Spawn Manager** | 200–500 total (default 150 targets + 150 obstacles + 30 collectibles = 330); random; inside bounds; counts configurable | 10 | Must | 3 | 2 | ✅ |
| PB-09 | [#10](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/10) | **No overlap at spawn** | Overlap detection + retry; zero overlaps confirmed by a debug check/log | 5 | Must | 2 | 2 | ✅ |
| PB-10 | [#11](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/11) | **Boomerang hit physics** | Impact pushes the target (impulse); target stays visible 2 s, then disappears; score += N; obstacles are never destroyed | 7 | Must | 2 | 2 | ✅ |
| PB-11 | [#12](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/12) | **Player pushes obstacles** | Obstacles are Rigidbodies; mass tuned so pushing is visible | 1 | Must | 1 | 1 | ✅ |
| PB-12 | [#13](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/13) | **Guardian Platform trigger** | Enter → all targets + obstacles (+ collectibles) disappear, platform stays, SFX. Exit → respawn per `config.json` `respawnMode` (Regenerate / Restore), SFX. Debounced | 8 | Must | 3 | 2 | ✅ |
| PB-13 | [#14](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/14) | **Collectibles** | Trigger pickup → disappears, bonus score, SFX | 3 | Must | 1 | 2 | ✅ |
| PB-14 | [#15](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/15) | **HUD score** | Always visible; updates on hit/collect; also shows targets hit, speed mode, cooldown | 3 | Must | 2 | 2 | ✅ |
| PB-15 | [#16](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/16) | **Minimap** upper-right | Shows the player (with heading) + nearby targets / obstacles / collectibles / platform in distinct colors; follows the player | 2 | Must | 3 | 3 | ✅ |
| PB-16 | [#17](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/17) | **Audio**: 5 required SFX | Throw, hit, invalid selection, enter platform, leave platform (+ optional collect, BGM); AudioManager | 3 | Must | 2 | 5 | ✅ |
| PB-17 | [#18](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/18) | **ESC quits** | `Application.Quit()`; stops Play Mode in the Editor | 1 | Must | 0.5 | 1 | ✅ |
| PB-18 | [#19](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/19) | **Asset integration** (User) | Greybox swapped for theme assets; colliders/scale fixed; credits recorded | quality | Should | 3 | 5 | ✅ |
| PB-19 | [#20](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/20) | **Hero's Journey framing** | Intro panel with story + controls; "Balance Restored" message when all targets are cleared | quality | Should | 2 | 3 | ✅ |
| PB-20 | [#21](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/21) | **Juice**: hit VFX, boomerang spin + trail, camera shake | — | quality | Could | 2 | 5 | ⬜ |
| PB-21 | [#22](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/22) | Build + final rubric test pass | Every row in `00-requirements-traceability.md` verified; standalone build runs; ESC quits | – | Must | 2 | 3 | ✅ |
| PB-22 | [#23](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/23) | **Report PDF** | 1–2 pages, ≥ 500 words, screenshots, asset credits; Claude drafts AI + Scrum sections; User writes self-reflection | +10% | Must | 3 | 5 | ⬜ |
| PB-24 | [#25](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/25) | As the PO I can **tune the demo via `config.json`** without rebuilding | Loaded from beside the app → StreamingAssets → defaults; respawnMode, counts, speeds; validated + clamped; effective values logged; F5 reloads (ADR-0008) | (12) | Must | 1.5 | 2 | ✅ |
| PB-25 | [#26](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/26) | As the designer I can **swap / vary models and restyle the HUD** | Guide: replace a prefab's `Model` child, fix the collider, create variants and add them to `SpawnConfig`, restyle `UI/HUD`; setup scripts keep working | quality | Should | – | 4 | ✅ |
| PB-26 | [#27](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/27) | **Player facing** while the camera rotates (investigate) | PO tests first, then chooses: keep (face camera yaw) / face movement direction / face camera only when moving | quality | Could | – | 4 | ✅ |
| PB-27 | [#28](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/28) | **Perceived spawn delay** (investigate) → loading overlay if needed | PO describes when the delay happens; add timing logs; fix the cause (Editor domain reload vs. respawn hitch vs. first frame); loading overlay if useful | quality | Could | – | 4 | ❌ |
| PB-28 | [#29](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/29) | As a player I control an **animated witch** | Witch model replaces the capsule visuals; Idle/Walk/Run by speed; Jump/Throw clips; hidden in FP; physics unchanged; simple, glitches accepted (ADR-0011) | quality | Should | – | 5 | ✅ |
| PB-29 | [#30](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/30) | As a player I see a **trajectory preview on hover** (ray casting) | Cursor ray each frame (same logic as a click); same Bézier as the flight; **white, 25 % opacity, same for valid and invalid** (PO: don't reveal validity); hidden over UI, while turning the camera, in menus, on sky | (7) | Should | – | 5 | 🟨 |
| PB-30 | [#31](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/31) | As the presenter I can **orbit the camera with the middle mouse button** | Hold MMB: camera orbits, the character's facing and movement are unchanged; release: smooth return to the previous view | demo | Should | – | 5 | ✅ |
| PB-23 | [#24](https://github.com/hwupu/115A-535652-3DGP-PA1/issues/24) | Pause menu / restart | — | — | Won't | – | – | ❌ |

**Must total ≈ 52.5 SP.**

## PO decisions log
| Date | Question | Decision |
|---|---|---|
| 2026-10-01 | Collectible effect | Bonus score only |
| 2026-10-01 | Respawn on platform exit | **Configurable** via `config.json` `respawnMode`: Regenerate / Restore (ADR-0008, PB-24) |
| 2026-10-01 | Camera / cursor | Free cursor, **hold RMB to look**; LMB picks under the cursor |
| 2026-10-01 | A/D keys | **Add A/D strafe** (camera-relative), beyond the spec |
| 2026-10-01 | Repo visibility | Public |
| 2026-10-03 | Folder structure | Keep `Assets/_Project/` (best practice) |
| 2026-10-03 | Scene wiring | Hybrid: Claude's Editor setup scripts + PO art/layout (ADR-0009) |
| 2026-10-03 | Boomerang hits obstacle | Bounce back early; obstacle nudged, never destroyed |
| 2026-10-03 | Invalid selection | Non-target, too far (> 40 m), during cooldown → error sound + message; already-hit target → ignored |
| 2026-10-03 | Scoring | Target +10, collectible +5 (config.json) |
| 2026-10-03 | Sprint 2 dates / SP | Start Oct 3, end Oct 8; story points not tracked further |
| 2026-10-03 | Sprint layout | 5 sprints: S3 feature-complete (Oct 3–5), S4 logic polish + feedback (Oct 6–8), S5 art & audio (Oct 9–12), Oct 13–14 buffer/submit |
| 2026-10-03 | Player facing / spawn delay | **No change yet**: the PO will test more first (PB-26, PB-27) |
| 2026-10-03 | PB-26 facing | After testing: expected = player and movement follow the camera yaw (already the design) → it was a **bug** (missing link), fixed |
| 2026-10-03 | PB-27 spawn delay | False alarm → closed |
| 2026-10-03 | PB-22 report | Hold until the PO has finished everything |
| 2026-10-05 | Theme | **Halloween** (Sketchfab "Halloween graveyard pack", CC-BY); targets are ghosts |
| 2026-10-05 | Asset credits format | List (attribution lines), not a table |
| 2026-10-05 | Asset Store skybox | Git-ignored (Standard EULA vs. public repo); kept locally and in the submission zip; README explains how to re-import |
| 2026-10-05 | Intro text | Keep the assignment's original wording (no Halloween rewrite) |
| 2026-10-05 | Player character (PB-28) | Little Witch with Blender animations from another AI agent's handoff; **keep it simple, glitches are OK** (ADR-0011) |
| 2026-10-06 | PB-20 polish | Ghosts drift around and spin while idle (IdleWander, visual only) |
| 2026-10-06 | New tickets | PB-29 hover trajectory preview (both valid and invalid objects, 50 % opacity curve); PB-30 MMB orbit preview for the TA demo |
| 2026-10-06 | PB-29 preview color | White 25 % for all objects: the preview must not reveal valid/invalid |
| 2026-10-05 | PB-18 asset integration | Accepted by the PO (boomerang, ghost targets, pumpkin collectibles, tombstone/ground, skybox, lighting) |
| 2026-10-05 | PB-20 juice | The asset-swap guide is enough for the PO; polish stays open and is requested case by case (first: the ghost float-and-fade hit reaction, ADR-0010) |

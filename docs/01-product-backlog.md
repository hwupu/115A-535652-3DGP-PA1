# Product Backlog

Owner: User (Product Owner) · Facilitator: Claude (Scrum Master)
Priority: MoSCoW. SP = story points (1 SP ≈ 1–2 h). Marks = rubric points covered.
Created 2026-10-01 (Sprint 0). This is a living document; refine it at each Sprint Planning.

| ID | User story / item | Acceptance criteria | Marks | Pri | SP | Sprint | Status |
|---|---|---|---|---|---|---|---|
| PB-00 | *Setup*: repo, Unity project, docs skeleton, CLAUDE.md, ADRs, GitHub board | Project opens in 6000.3.25f1; first commit pushed to a private repo | – | Must | 2 | 0 | 🟨 |
| PB-01 | As a player I explore a **bounded arena** | Large ground (~100×100 m); cube walls on 4 sides; platform at the center; logical hierarchy & naming | 10 | Must | 2 | 1 | ⬜ |
| PB-02 | As a player I **move with W/S relative to the camera view**, smoothly | W forward / S back along the camera's flattened forward; acceleration smoothing; mouse yaw turns the player | 8 | Must | 3 | 1 | ⬜ |
| PB-03 | As a player I **toggle normal/fast speed** with SPACE | Toggle (not hold); clear difference (e.g. 5 → 10 m/s); HUD indicator | 4 | Must | 1 | 1 | ⬜ |
| PB-04 | As a player I **jump with F** under gravity with a projectile arc | Jump only when grounded; gravity brings the player back; horizontal momentum is kept in the air | 8 | Must | 2 | 1 | ⬜ |
| PB-05 | As a player I **switch first-/third-person camera** | FP: camera at head, pitch + yaw (pitch clamped). TP: orbit pitch + yaw, scroll-wheel distance (clamped), wall-clip avoidance. Toggle key V | 8 | Must | 4 | 1 | ⬜ |
| PB-06 | As a player I **click objects to select them via ray casting** | LMB → `ScreenPointToRay`; target → throw; non-target → error SFX + UI hint; feedback while on cooldown | 7 | Must | 2 | 2 | ⬜ |
| PB-07 | As a player I **throw a boomerang that returns** | Spawns at the hand; suitable initial speed; curves to the selected target; returns to the player; 3 s cooldown (HUD); never lost (timeout fallback) | 12 | Must | 5 | 2 | ⬜ |
| PB-08 | As a designer I have a **Spawn Manager** | 200–500 total (default 150 targets + 150 obstacles + 30 collectibles = 330); random; inside bounds; counts configurable | 10 | Must | 3 | 2 | ⬜ |
| PB-09 | **No overlap at spawn** | Overlap detection + retry; zero overlaps confirmed by a debug check/log | 5 | Must | 2 | 2 | ⬜ |
| PB-10 | **Boomerang hit physics** | Impact pushes the target (impulse); target stays visible 2 s, then disappears; score += N; obstacles are never destroyed | 7 | Must | 2 | 2 | ⬜ |
| PB-11 | **Player pushes obstacles** | Obstacles are Rigidbodies; mass tuned so pushing is visible | 1 | Must | 1 | 1 | ⬜ |
| PB-12 | **Guardian Platform trigger** | Enter → all targets + obstacles (+ collectibles) disappear, platform stays, SFX. Exit → respawn, SFX. Debounced | 8 | Must | 3 | 2 | ⬜ |
| PB-13 | **Collectibles** | Trigger pickup → disappears, bonus score, SFX | 3 | Must | 1 | 2 | ⬜ |
| PB-14 | **HUD score** | Always visible; updates on hit/collect; also shows targets hit, speed mode, cooldown | 3 | Must | 2 | 2 | ⬜ |
| PB-15 | **Minimap** upper-right | Shows the player (with heading) + nearby targets / obstacles / collectibles / platform in distinct colors; follows the player | 2 | Must | 3 | 3 | ⬜ |
| PB-16 | **Audio**: 5 required SFX | Throw, hit, invalid selection, enter platform, leave platform (+ optional collect, BGM); AudioManager | 3 | Must | 2 | 3 | ⬜ |
| PB-17 | **ESC quits** | `Application.Quit()`; stops Play Mode in the Editor | 1 | Must | 0.5 | 1 | ⬜ |
| PB-18 | **Asset integration** (User) | Greybox swapped for theme assets; colliders/scale fixed; credits recorded | quality | Should | 3 | 3 | ⬜ |
| PB-19 | **Hero's Journey framing** | Intro panel with story + controls; "Balance Restored" message when all targets are cleared | quality | Should | 2 | 3 | ⬜ |
| PB-20 | **Juice**: hit VFX, boomerang spin + trail, camera shake | — | quality | Could | 2 | 3 | ⬜ |
| PB-21 | Build + final rubric test pass | Every row in `00-requirements-traceability.md` verified; standalone build runs; ESC quits | – | Must | 2 | 3 | ⬜ |
| PB-22 | **Report PDF** | 1–2 pages, ≥ 500 words, screenshots, asset credits; Claude drafts AI + Scrum sections; User writes self-reflection | +10% | Must | 3 | 3 | ⬜ |
| PB-23 | Pause menu / restart | — | — | Won't | – | – | ❌ |

**Must total ≈ 51 SP.**

## Open PO questions (resolve at Sprint 1 planning)
1. **Respawn on platform exit**: (a) a fresh random layout at full count, including targets that were already destroyed *(recommended)*, or (b) restore the exact hidden objects?
2. **Camera/cursor**: free cursor + hold RMB to rotate, so LMB picks under the mouse *(recommended)*, or a locked cursor + center crosshair?
3. **A/D keys**: none, as specified *(recommended)*, or add strafe/turn as an extra?
4. **Theme**: decide after the asset search (target: before Sprint 3).

# Sprint 1 — Walking Skeleton (2026-10-02 → 2026-10-05)

**Sprint goal:** A greybox arena where the player can walk (W/S + A/D strafe, camera-relative), toggle speed, jump with a projectile arc, push obstacles, switch first/third-person cameras (hold RMB to look, scroll to zoom), and quit with ESC.

**Capacity:** ~13.5 SP. Milestone: [Sprint 1 — Walking Skeleton](https://github.com/hwupu/115A-535652-3DGP-PA1/milestone/2)

## Sprint backlog
| ID | Issue | Item | SP | Status |
|---|---|---|---|---|
| PB-01 | #2 | Bounded arena greybox (ground, cube walls, center platform, hierarchy) | 2 | ⬜ |
| PB-02 | #3 | Camera-relative W/S + A/D movement, smooth | 3 | ⬜ |
| PB-03 | #4 | SPACE speed toggle (HUD indicator comes with PB-14 in Sprint 2) | 1 | ⬜ |
| PB-04 | #5 | F jump + gravity + projectile arc | 2 | ⬜ |
| PB-05 | #6 | First-/third-person camera, RMB look, scroll zoom | 4 | ⬜ |
| PB-11 | #12 | Test obstacles the player can push | 1 | ⬜ |
| PB-17 | #18 | ESC quits | 0.5 | ⬜ |

## Task breakdown
**Claude (code + guide)**
- [ ] C1 `Assets/_Project/Settings/PlayerControls.inputactions`: Move (WASD), Look (mouse delta), LookHold (RMB), ToggleSpeed (Space), Jump (F), SwitchView (V), Zoom (scroll), Select (LMB), Quit (Esc), ReloadConfig (F5). We use our own map instead of the template's `InputSystem_Actions` (Space = Jump there, which conflicts with the spec).
- [ ] C2 `Player/PlayerInputReader`: wraps the actions asset and exposes values/events to other scripts.
- [ ] C3 `Player/PlayerMotor`: Rigidbody movement relative to camera yaw, acceleration smoothing, speed toggle, grounded jump, air momentum (ADR-0004).
- [ ] C4 `Camera/CameraRig`: FP/TP modes, RMB-held pitch/yaw (pitch clamped), scroll distance, wall-clip SphereCast, V toggle.
- [ ] C5 `Core/ApplicationController`: ESC → `Application.Quit()` / exit Play Mode in the Editor.
- [ ] C6 Step-by-step Editor guide `docs/guides/sprint-01-scene-setup.md` (scene, arena, player, camera, physics materials, layers).

**User (Editor / scene)**
- [ ] U1 Create `Assets/_Project/Scenes/Main.unity` and build the greybox arena following the guide.
- [ ] U2 Player capsule placeholder + camera rig wiring.
- [ ] U3 Place ~5 test obstacle cubes with Rigidbodies.
- [ ] U4 Play-test against the checklist below. Delete the template's `TutorialInfo/`, `Readme.asset` and `SampleScene`.
- [ ] U5 (parallel) Asset hunt for theme candidates; shortlist into `docs/asset-credits.md`.

## Sprint 1 test checklist (Review demo)
- [ ] Arena ~100×100 m, closed on all sides; the player can't leave it.
- [ ] W/S/A/D move relative to the camera view in both FP and TP; start/stop feels smooth.
- [ ] SPACE toggles speed (clear difference; logged until the HUD exists).
- [ ] F jumps only when grounded; jumping while running gives a forward arc; the player lands back on the ground.
- [ ] V switches FP ↔ TP; hold RMB to look; pitch is clamped; scroll changes TP distance; the camera doesn't clip through walls.
- [ ] Walking into an obstacle pushes it visibly.
- [ ] ESC quits (exits Play Mode in the Editor).
- [ ] No Console errors.

## Daily log
- **2026-10-01**: Sprint planning. PO decisions: A/D strafe, RMB-hold look, respawn configurable via `config.json` (new PB-24 #25, Sprint 2). Template check: Input System only (`activeInputHandler: 1`), URP 17.3, Input System 1.20.

## Review

## Retro
- Keep:
- Problem:
- Try:

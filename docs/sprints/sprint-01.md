# Sprint 1 — Walking Skeleton (2026-10-02 → 2026-10-05)

**Sprint goal:** A greybox arena where the player can walk (W/S + A/D strafe, camera-relative), toggle speed, jump with a projectile arc, push obstacles, switch first/third-person cameras (hold RMB to look, scroll to zoom), and quit with ESC.

**Capacity:** ~13.5 SP. Milestone: [Sprint 1 — Walking Skeleton](https://github.com/hwupu/115A-535652-3DGP-PA1/milestone/2)

## Sprint backlog
| ID | Issue | Item | SP | Status |
|---|---|---|---|---|
| PB-01 | #2 | Bounded arena greybox (ground, cube walls, center platform, hierarchy) | 2 | ✅ |
| PB-02 | #3 | Camera-relative W/S + A/D movement, smooth | 3 | ✅ |
| PB-03 | #4 | SPACE speed toggle (HUD indicator comes with PB-14 in Sprint 2) | 1 | ✅ |
| PB-04 | #5 | F jump + gravity + projectile arc | 2 | ✅ |
| PB-05 | #6 | First-/third-person camera, RMB look, scroll zoom | 4 | ✅ |
| PB-11 | #12 | Test obstacles the player can push | 1 | ✅ |
| PB-17 | #18 | ESC quits | 0.5 | ✅ |

## Task breakdown
**Claude (code + guide)**
- [x] C1 `Assets/_Project/Settings/PlayerControls.inputactions`: Move (WASD), Look (mouse delta), LookHold (RMB), ToggleSpeed (Space), Jump (F), SwitchView (V), Zoom (scroll), Select (LMB), Quit (Esc), ReloadConfig (F5). We use our own map instead of the template's `InputSystem_Actions` (Space = Jump there, which conflicts with the spec).
- [x] C2 `Player/PlayerInputReader`: wraps the actions asset and exposes values/events to other scripts.
- [x] C3 `Player/PlayerMotor`: Rigidbody movement relative to camera yaw, acceleration smoothing, speed toggle, grounded jump, air momentum (ADR-0004).
- [x] C4 `Cameras/CameraRig`: FP/TP modes, RMB-held pitch/yaw (pitch clamped), scroll distance, wall-clip SphereCast, V toggle.
- [x] C5 `Core/ApplicationController`: ESC → `Application.Quit()` / exit Play Mode in the Editor.
- [x] C6 Step-by-step Editor guide `docs/guides/sprint-01-scene-setup.md` (scene, arena, player, camera, physics materials, layers).

**User (Editor / scene)**
- [x] U1 Create `Assets/_Project/Scenes/Main.unity` and build the greybox arena following the guide.
- [x] U2 Player capsule placeholder + camera rig wiring.
- [x] U3 Place ~5 test obstacle cubes with Rigidbodies.
- [x] U4 Play-test against the checklist below. Deleted `TutorialInfo/` and `Readme.asset`; `SampleScene` is disabled in Build Profiles but the file still exists (carry over: delete it)
- [ ] U5 (parallel, **carried over to Sprint 2**) Asset hunt for theme candidates; shortlist into `docs/asset-credits.md`.

## Sprint 1 test checklist (Review demo)
- [x] Arena ~100×100 m, closed on all sides; the player can't leave it.
- [x] W/S/A/D move relative to the camera view in both FP and TP; start/stop feels smooth.
- [x] SPACE toggles speed (clear difference; logged until the HUD exists).
- [x] F jumps only when grounded; jumping while running gives a forward arc; the player lands back on the ground.
- [x] V switches FP ↔ TP; hold RMB to look; pitch is clamped; scroll changes TP distance; the camera doesn't clip through walls.
- [x] Walking into an obstacle pushes it visibly.
- [x] ESC quits (exits Play Mode in the Editor).
- [x] No Console errors.

## Daily log
- **2026-10-01**: Sprint planning. PO decisions: A/D strafe, RMB-hold look, respawn configurable via `config.json` (new PB-24 #25, Sprint 2). Template check: Input System only (`activeInputHandler: 1`), URP 17.3, Input System 1.20.

- **2026-10-01**: Claude wrote C1–C6. Compile-checked in Unity batch mode: 0 errors, 0 warnings, and the input asset imported. Not yet play-tested; that needs the User's scene (guide: `docs/guides/sprint-01-scene-setup.md`). Namespace `Camera` → `Cameras` to avoid shadowing `UnityEngine.Camera`.

- **2026-10-03**: The User built `Main.unity` following the guide (hierarchy Environment/Gameplay/Cameras/Systems, 100×100 m arena, 4 cube walls, platform, Player layer, 5 test obstacles) and play-tested the full checklist. Everything works.

## Review (2026-10-03)
- **Sprint goal met.** The PO confirmed the whole 14-step play-test checklist in Play Mode.
- Done: PB-01, 02, 03, 04, 05, 11, 17 (13.5 SP). Velocity ≈ 13.5 SP per sprint.
- Not done / carried over: U5 asset hunt (theme still TBD); delete `SampleScene.unity`.
- Backlog impact: none. The PO raised a folder-structure question (`_Project` vs. Assets root) before Sprint 2; see Daily log / ADR-0001.

## Retro (2026-10-03, PO input)
- **Keep:** step-by-step Editor guides with exact transforms and a checklist. The PO built the scene on the first attempt with no errors. Batch-mode compile check before hand-off.
- **Problem:** nothing significant ("everything is perfect"). Manual scene wiring is the slowest part for the PO.
- **Try:** automate scene/prefab creation and component linking with Unity **Editor scripts** (menu / `-executeMethod`), so the PO's time goes to assets and polish. To be decided at Sprint 2 planning (proposed ADR-0009).
- **Folder question:** the PO asked whether `Assets/_Project/` is best practice. Answer: yes (separates our content from Asset Store packs that import at the `Assets/` root). Decision: **keep it** (ADR-0001 unchanged).

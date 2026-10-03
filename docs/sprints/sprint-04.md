# Sprint 4 — Logic Polish (2026-10-03 → 2026-10-08)

**Sprint goal:** Resolve the PO's feedback from the Sprint 2/3 retros. Fix player facing / camera-relative movement, make wiring errors impossible to miss, and give the PO a complete model-swap guide for Sprint 5.

Milestone: [Sprint 4 — Logic Polish](https://github.com/hwupu/115A-535652-3DGP-PA1/milestone/5)

## Planning input (PO, 2026-10-03)
| Item | PO input | Outcome |
|---|---|---|
| PB-26 Facing (#27) | "When I rotate the camera and press W, the camera and player should move forward, not locked to the Z axis." The player didn't rotate | **Bug**, not a design choice: `PlayerMotor.cameraRig` was never linked → fix |
| PB-27 Spawn delay (#28) | "I didn't notice the delay, it is false alarm." | Closed, not planned |
| PB-25 Model swap guide (#26) | "Provide me with steps to swap out meshes for objects." | `docs/guides/asset-swap.md` |
| PB-22 Report (#23) | "Hold on until I finished everything." | Deferred until the PO asks |
| PB-20 Juice (#21) | Not answered yet | Ask the PO |

## Sprint backlog
| ID | Issue | Item | Status |
|---|---|---|---|
| PB-26 | #27 | Fix: player turns with the camera; W/S/A/D camera-relative (missing link + loud fallback + validator) | 🟨 awaiting PO play-test |
| PB-25 | #26 | Asset swap guide (models, variants, boomerang, player, environment, HUD, licensing) | 🟨 awaiting PO review |
| PB-27 | #28 | Spawn delay: false alarm | ❌ closed |
| PB-20 | #21 | Juice (optional) | ⬜ PO to choose |

## Task breakdown
**Claude**
- [x] Root cause via scene YAML audit: only `PlayerMotor.cameraRig` was empty (audio clips are intentionally empty)
- [x] `PlayerMotor`: if the rig is unassigned → auto-find + **warning** (was: silent world-axis fallback)
- [x] `Core/OptionalReferenceAttribute` + `Editor/WiringValidator` (menu **Validate Wiring**; runs after every setup)
- [x] `Sprint3Setup` links `PlayerMotor.cameraRig` (repair on rerun)
- [x] `docs/guides/asset-swap.md`

**User**
- [ ] Rerun **Setup → Sprint 3** (repairs the link), or assign `Player → Player Motor → Camera Rig = Main Camera` by hand; then **Validate Wiring**
- [ ] Play-test: hold RMB + move the mouse, then W → you move where the camera looks and the nose turns, in both views
- [ ] Review the asset-swap guide

## Daily log
- **2026-10-03**: Facing bug traced to a missing reference from Sprint 1. Verified in an APFS clone: the validator reported exactly `Gameplay/Player → PlayerMotor.cameraRig`; rerunning the Sprint 3 setup linked **only** that field, and the validator then passed.

## Review

## Retro
- Keep:
- Problem:
- Try:

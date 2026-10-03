# Sprint 3 — Feature Complete (2026-10-03 → 2026-10-05)

**Sprint goal:** Every rubric feature exists, with greybox art. The minimap in the upper-right shows the player and nearby objects. The Hero's Journey story frames the game (intro, 7 stages, ending). A standalone macOS build runs and reads `config.json` from next to the app.

Milestone: [Sprint 3 — Feature Complete](https://github.com/hwupu/115A-535652-3DGP-PA1/milestone/4)

## Re-plan (Sprint 2 retro, PO 2026-10-03)
Five sprints instead of three. S3 feature-complete → S4 logic polish + PO feedback (PB-20, 25, 26, 27) → S5 art & audio swap, final build, final report (PB-16, 18, 22). Oct 13–14 buffer and submission.

## Sprint backlog
| ID | Issue | Item | Status |
|---|---|---|---|
| PB-15 | #16 | Minimap (upper-right, player + nearby objects) | ✅ |
| PB-19 | #20 | Hero's Journey: intro, 7 stages with hints, "Balance Restored" ending | ✅ |
| PB-21 | #22 | First standalone build + rubric pass (repeated in Sprint 5 with final art) | ✅ |

## Task breakdown
**Claude**
- [x] `Cameras/MinimapCamera`, `UI/MinimapIcon` (level, fixed height, optional yaw), SpawnManager ignores icons in footprints
- [x] `UI/JourneyController` (intro pause, stage tracking, hints, end panel); `PlayerInputReader.SetGameplayEnabled` (menus pause gameplay, ESC/F5 still work)
- [x] `Editor/Sprint3Setup` (layer, icon materials/meshes, prefab icons via `LoadPrefabContents`, static icons, minimap camera + RT, HUD panels, product name, build scene list, template clean-up)
- [x] `Editor/BuildScript` (menu + batch, copies an editable `config.json` next to the `.app`)
- [x] Guide `docs/guides/sprint-03-setup.md`

**User**
- [x] Deleted `Assets/Scenes/SampleScene.unity` (carry-over from Sprints 1–2)
- [x] Run the Sprint 3 setup menu and play-test the checklist (incl. the build)

## Daily log
- **2026-10-03**: Sprint 2 retro → re-plan to 5 sprints, new PB-25/26/27 (#26–#28). The PO asked for **no change yet** to facing / spawn delay until they've tested more. Claude implemented the Sprint 3 code. Verification in an APFS clone: compile 0 errors; setup run 1 created/linked everything (layer 8, 7 icon materials, 4 prefab icons, player arrow, 6 static icons, minimap camera + RT, 4 HUD panels, JourneyController + 16 links); **run 2: 0 changes** (idempotent); spawn smoke test ×3 with icons: 330 objects, 0 failed, 0 overlaps (icons correctly excluded from footprints). **macOS build succeeded** (129.5 MB, 132 s) → `Builds/macOS/`. Ran the built player headless: `Config loaded from …/Builds/macOS/config.json` (beside-app override works) and spawn OK. Not yet verified: on-screen look of the minimap / panels and the stage flow; that needs the PO's play-test.

- **2026-10-03 (later)**: The PO ran the Sprint 3 setup, play-tested the checklist including the macOS build, and confirmed Sprint 3.

## Review (2026-10-03)
- **Sprint goal met** (planned to run until Oct 5). The PO confirmed the minimap, the Hero's Journey flow, and the standalone build.
- Done: PB-15, PB-19, PB-21 (first build; repeated in Sprint 5 with final art).
- Traceability: **all 100 rubric marks are now implemented and verified**, except the real SFX (placeholder tones play for all 5 events; real clips come in Sprint 5).

## Retro
- Keep:
- Problem:
- Try:

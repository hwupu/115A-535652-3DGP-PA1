# Requirements Traceability (Spec + Rubric → Backlog → Status)

Source: `reference/3DGP_PA_01_Instruction.pdf`. Status values: ⬜ todo · 🟨 in progress · ✅ done (verified in Play Mode) · ❌ dropped.

| # | Rubric category / item | Marks | Backlog | Status | Verified how |
|---|---|---|---|---|---|
| 1 | **Scene construction** — ground plane | 2 | PB-01 | ✅ | PO play-test 2026-10-03 (Sprint 1 Review) |
| | boundary construction (cubes) | 2 | PB-01 | ✅ | PO play-test 2026-10-03 (Sprint 1 Review) |
| | playable region appropriately sized | 2 | PB-01 | ✅ | PO play-test 2026-10-03 (Sprint 1 Review) |
| 2 | **Object organization** — required categories present (ground, boundary, targets, obstacles, player, boomerang, platform) | 2 | PB-01, PB-08 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | logical hierarchy & naming | 2 | PB-01 | ✅ | PO play-test 2026-10-03 (Sprint 1 Review) |
| 3 | **Player movement** — W/S forward/backward | 3 | PB-02 | ✅ | PO play-test 2026-10-03 (Sprint 1 Review) |
| | camera-relative movement | 3 | PB-02, PB-26 | ✅ | Corrected 2026-10-03: only worked while the camera faced +Z (`PlayerMotor.cameraRig` unlinked since Sprint 1); fixed + PO re-test in the Sprint 4 Review |
| | smooth movement | 2 | PB-02 | ✅ | PO play-test 2026-10-03 (Sprint 1 Review) |
| 4 | **Speed toggle** — SPACE toggles | 2 | PB-03 | ✅ | PO play-test 2026-10-03 (Sprint 1 Review) |
| | appropriate speed difference | 2 | PB-03 | ✅ | PO play-test 2026-10-03 (Sprint 1 Review) |
| 5 | **Jump/gravity/projectile** — F jump | 3 | PB-04 | ✅ | PO play-test 2026-10-03 (Sprint 1 Review) |
| | gravity | 3 | PB-04 | ✅ | PO play-test 2026-10-03 (Sprint 1 Review) |
| | projectile motion while jumping | 2 | PB-04 | ✅ | PO play-test 2026-10-03 (Sprint 1 Review) |
| 6 | **Camera** — first-person (pitch, yaw) | 3 | PB-05 | ✅ | PO play-test 2026-10-03 (Sprint 1 Review) |
| | third-person | 3 | PB-05 | ✅ | PO play-test 2026-10-03 (Sprint 1 Review) |
| | pitch, yaw, distance control | 2 | PB-05 | ✅ | PO play-test 2026-10-03 (Sprint 1 Review) |
| 7 | **Ray casting** — accurate picking (LMB) | 4 | PB-06 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | wrong selection handling (error SFX) | 3 | PB-06, PB-16 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| 8 | **Boomerang** — throwing system | 3 | PB-07 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | suitable initial speed | 2 | PB-07 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | return to player | 3 | PB-07 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | 3 s cooldown | 2 | PB-07 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | overall stability | 2 | PB-07 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| 9 | **Spawn manager** — random generation | 3 | PB-08 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | 200–500 objects | 2 | PB-08 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | ≥ 100 targets | 2 | PB-08 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | ≥ 100 obstacles | 1 | PB-08 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | placement within region | 2 | PB-08 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| 10 | **Spawn collision avoidance** — overlap detection | 3 | PB-09 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | collision-free placement | 2 | PB-09 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| 11 | **Physics response** — boomerang impact | 3 | PB-10 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | push effect on targets | 2 | PB-10 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | target disappears after 2 s | 2 | PB-10 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | player pushes obstacles | 1 | PB-11 | ✅ | PO play-test 2026-10-03 (Sprint 1 Review) |
| 12 | **Platform & triggers** — trigger detection | 3 | PB-12 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | remove all objects on entry | 2 | PB-12 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | respawn objects on exit | 3 | PB-12 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| 13 | **Collectibles** — implementation | 2 | PB-13 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | correct trigger behaviour | 1 | PB-13 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| 14 | **UI** — score display | 2 | PB-14 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | score updates correctly | 1 | PB-14 | ✅ | PO play-test 2026-10-03 (Sprint 2 Review); spawn log: 330 objects, 0 overlapping pairs |
| | functional minimap (upper-right) | 2 | PB-15 | ✅ | PO play-test 2026-10-03 (Sprint 3 Review) |
| 15 | **Audio & control** — 5 SFX (throw, hit, invalid, enter platform, leave platform) | 3 | PB-16 | ✅ | real CC0 clips for all 5 events + BGM (Sprint 5, PO play-test 2026-10-06) |
| | ESC quits | 1 | PB-17 | ✅ | PO play-test 2026-10-03 (Sprint 1 Review); ESC confirmed in the macOS build (Sprint 3 Review) |
| | **Total** | **100** | | | |
| B | **Report PDF** (1–2 pages, ≥ 500 words, AI usage, Scrum, reflection, asset credits) | +10 / −10 | PB-22 | ⬜ | |

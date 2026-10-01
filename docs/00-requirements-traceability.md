# Requirements Traceability (Spec + Rubric → Backlog → Status)

Source: `reference/3DGP_PA_01_Instruction.pdf`. Status values: ⬜ todo · 🟨 in progress · ✅ done (verified in Play Mode) · ❌ dropped.

| # | Rubric category / item | Marks | Backlog | Status | Verified how |
|---|---|---|---|---|---|
| 1 | **Scene construction** — ground plane | 2 | PB-01 | ⬜ | |
| | boundary construction (cubes) | 2 | PB-01 | ⬜ | |
| | playable region appropriately sized | 2 | PB-01 | ⬜ | |
| 2 | **Object organization** — required categories present (ground, boundary, targets, obstacles, player, boomerang, platform) | 2 | PB-01, PB-08 | ⬜ | |
| | logical hierarchy & naming | 2 | PB-01 | ⬜ | |
| 3 | **Player movement** — W/S forward/backward | 3 | PB-02 | ⬜ | |
| | camera-relative movement | 3 | PB-02 | ⬜ | |
| | smooth movement | 2 | PB-02 | ⬜ | |
| 4 | **Speed toggle** — SPACE toggles | 2 | PB-03 | ⬜ | |
| | appropriate speed difference | 2 | PB-03 | ⬜ | |
| 5 | **Jump/gravity/projectile** — F jump | 3 | PB-04 | ⬜ | |
| | gravity | 3 | PB-04 | ⬜ | |
| | projectile motion while jumping | 2 | PB-04 | ⬜ | |
| 6 | **Camera** — first-person (pitch, yaw) | 3 | PB-05 | ⬜ | |
| | third-person | 3 | PB-05 | ⬜ | |
| | pitch, yaw, distance control | 2 | PB-05 | ⬜ | |
| 7 | **Ray casting** — accurate picking (LMB) | 4 | PB-06 | ⬜ | |
| | wrong selection handling (error SFX) | 3 | PB-06, PB-16 | ⬜ | |
| 8 | **Boomerang** — throwing system | 3 | PB-07 | ⬜ | |
| | suitable initial speed | 2 | PB-07 | ⬜ | |
| | return to player | 3 | PB-07 | ⬜ | |
| | 3 s cooldown | 2 | PB-07 | ⬜ | |
| | overall stability | 2 | PB-07 | ⬜ | |
| 9 | **Spawn manager** — random generation | 3 | PB-08 | ⬜ | |
| | 200–500 objects | 2 | PB-08 | ⬜ | |
| | ≥ 100 targets | 2 | PB-08 | ⬜ | |
| | ≥ 100 obstacles | 1 | PB-08 | ⬜ | |
| | placement within region | 2 | PB-08 | ⬜ | |
| 10 | **Spawn collision avoidance** — overlap detection | 3 | PB-09 | ⬜ | |
| | collision-free placement | 2 | PB-09 | ⬜ | |
| 11 | **Physics response** — boomerang impact | 3 | PB-10 | ⬜ | |
| | push effect on targets | 2 | PB-10 | ⬜ | |
| | target disappears after 2 s | 2 | PB-10 | ⬜ | |
| | player pushes obstacles | 1 | PB-11 | ⬜ | |
| 12 | **Platform & triggers** — trigger detection | 3 | PB-12 | ⬜ | |
| | remove all objects on entry | 2 | PB-12 | ⬜ | |
| | respawn objects on exit | 3 | PB-12 | ⬜ | |
| 13 | **Collectibles** — implementation | 2 | PB-13 | ⬜ | |
| | correct trigger behaviour | 1 | PB-13 | ⬜ | |
| 14 | **UI** — score display | 2 | PB-14 | ⬜ | |
| | score updates correctly | 1 | PB-14 | ⬜ | |
| | functional minimap (upper-right) | 2 | PB-15 | ⬜ | |
| 15 | **Audio & control** — 5 SFX (throw, hit, invalid, enter platform, leave platform) | 3 | PB-16 | ⬜ | |
| | ESC quits | 1 | PB-17 | ⬜ | |
| | **Total** | **100** | | | |
| B | **Report PDF** (1–2 pages, ≥ 500 words, AI usage, Scrum, reflection, asset credits) | +10 / −10 | PB-22 | ⬜ | |

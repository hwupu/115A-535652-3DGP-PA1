# Sprint 2 — Setup Guide (hybrid, ADR-0009)

For: PB-06 picking, PB-07 boomerang, PB-08/09 spawning, PB-10 hit physics, PB-12 platform, PB-13 collectibles, PB-14 HUD, PB-24 config.json (+ PB-16 placeholder audio).
Time: about 10 minutes, plus play-testing.

## Steps
1. **Switch to Unity** and wait for the script compile to finish (spinner at the bottom right).
2. **Import TextMeshPro resources** (one time): **Window → TextMeshPro → Import TMP Essential Resources** → **Import**. This creates `Assets/TextMesh Pro/`. The HUD needs it.
3. **Delete the Sprint 1 test cubes**: `Gameplay/Obstacles_Test`. The spawner now creates real obstacles. The test cubes would only block spawn spots and count as "not a target".
4. Save the scene (Cmd+S), then run **Boomerang Guardian → Setup → Sprint 2 (Core Loop)**.
   - A dialog says "Done". The **Console** lists every item it created (`+`), linked (`~`) or found existing (`=`).
   - It is safe to run again. It only adds what's missing and never overwrites links you set yourself.
5. Press **Play** and go through the checklist below.
6. Tell me the results. Include any red Console messages, or anything that feels off (speed, curve, push force, HUD layout).

## What the setup script created
| Where | What | Why |
|---|---|---|
| `_Project/Prefabs/Greybox/` | `Target_Barrel` (red, Rigidbody 2 kg, `Target`), `Obstacle_Crate` (15 kg), `Obstacle_Pillar` (40 kg), `Collectible_Gem` (trigger, spins), `Boomerang` (kinematic, trigger, spinning arms) | Greybox content. In Sprint 3 you replace the `Model` child with themed art. **Keep the root pivot at the bottom center.** |
| `_Project/Materials/` | 6 greybox materials | Placeholder colors |
| `_Project/Settings/SpawnConfig.asset` | Prefab lists for the spawner | Swap or add prefabs here, with no code changes (ADR-0007) |
| `Assets/StreamingAssets/config.json` | Default runtime config | Unity requires `StreamingAssets` at the `Assets/` root, the one exception to `_Project` |
| `Systems/GameManager` | + `ConfigLoader`, `ScoreManager` | Config (F5 reloads), scoring |
| `Systems/SpawnManager` | `SpawnManager` (region = Ground size, excludes the platform and player) | Random, non-overlapping spawn |
| `Environment/GuardianPlatformTrigger` | Convex cylinder trigger (3 m tall) + `GuardianPlatform` | Enter/leave events (a separate object, so the flat disc scale doesn't distort it) |
| `Gameplay/Spawned` | Empty parent | Spawned objects appear here at runtime, grouped into Targets / Obstacles / Collectibles |
| `Player` | + `Hand` child, `BoomerangThrower`, `TargetSelector` (pick mask excludes the Player layer) | Throwing + ray-cast picking |
| `Systems/AudioManager` | `AudioManager` + `AudioSource` | All sounds. Empty clip slots play synthesized placeholder tones; drop real clips in during Sprint 3 |
| `UI/EventSystem`, `UI/HUD` | Canvas with score panel (top-left), cooldown bar (bottom-center), message line; `HudController` | PB-14. The top-right is kept free for the minimap |

## Play-test checklist (Sprint 2 Review demo)
| # | Do this | Expect | Rubric |
|---|---|---|---|
| 1 | Press Play and look at the Console | `Config loaded from …StreamingAssets/config.json`, then `SpawnManager: 150 targets + 150 obstacles + 30 collectibles = 330 objects … Failed placements: 0. Overlap check: 0 overlapping pairs.` | 9, 10 |
| 2 | Look around | Objects spread everywhere inside the walls; none on the platform or right next to you | 9 |
| 3 | Left-click a red barrel within 40 m | Throw sound; the boomerang curves to it and hits; the barrel is knocked away, turns grey, and disappears after 2 s; HUD **+10**; the boomerang flies back to your hand (catch sound) | 7, 8, 11, 14 |
| 4 | Click another barrel right away | Error sound + "Boomerang not ready (x s)"; the bar at the bottom refills over **3 s** | 8 |
| 5 | Click a crate, pillar, the ground, a wall, the sky | Error sound + "That's an obstacle / not a target / nothing there" | 7 |
| 6 | Click a barrel more than 40 m away | Error sound + "Too far! …" | 7 |
| 7 | Throw at a barrel with a crate in the way | The boomerang bounces off the crate (thud), the crate is nudged, and the boomerang returns. The crate is never destroyed | 8, 11 |
| 8 | Walk into a yellow gem | It disappears, chime, **+5** | 13 |
| 9 | Walk into crates / pillars | Pushed (crates easily, pillars slowly) | 11 |
| 10 | Step onto the platform | Rising tone; **all** barrels, crates, pillars and gems vanish; the platform stays; HUD message | 12, 15 |
| 11 | Step off | Falling tone; objects come back as a **new random layout** (Console shows a new spawn line) | 12, 15 |
| 12 | Open `Assets/StreamingAssets/config.json`, set `"respawnMode": "Restore"`, save, press **F5** in game, then step on and off | The **same** objects come back; barrels you destroyed stay gone | 12 / PB-24 |
| 13 | Throw, then quickly step onto the platform | The boomerang turns back when its target vanishes; no errors | 8 (stability) |
| 14 | Console | No red errors | DoD |

## config.json quick reference
- **Editor:** edit `Assets/StreamingAssets/config.json` and press **F5** in Play Mode. Speeds and the respawn mode apply at once; object counts apply on the next respawn.
- **Build (macOS):** put a `config.json` **next to** `BoomerangGuardian.app`. It overrides the built-in copy without rebuilding (ADR-0008).
- Invalid values are clamped and logged. For example, more than 500 objects in total, or fewer than 100 targets.
- `randomSeed`: 0 gives a new layout each time; any other number repeats the same layout, which is handy for the demo video.

## Tuning (Inspector)
| Feel | Where |
|---|---|
| Boomerang curve / height / push force | `Boomerang` prefab → `BoomerangProjectile` (Curve, Arc Height, Target Impulse) |
| Throw speed, range, cooldown | `config.json` (`boomerangSpeed`, `maxThrowRange`, `boomerangCooldown`) |
| Target stays visible | `Target_Barrel` → `Target` → Disappear Delay (spec: 2 s) |
| Spacing between spawned objects | `SpawnManager` → Spacing / Wall Margin / clear radii |
| HUD layout | Move the panels under `UI/HUD` freely; the links stay |

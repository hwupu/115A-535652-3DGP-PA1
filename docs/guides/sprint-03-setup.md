# Sprint 3 — Setup Guide (Feature Complete)

For: PB-15 minimap, PB-19 Hero's Journey (intro, stages, ending), PB-21 first build.
Time: about 5 minutes, plus play-testing.

## Steps
1. Switch to Unity and wait for the compile to finish.
2. Save the scene, then run **Boomerang Guardian → Setup → Sprint 3 (Feature Complete)**. The Console lists what was created and linked. It is safe to run again (rerun it after adding prefab variants, so they get minimap icons too).
3. Press **Play** and go through the checklist below.
4. Optional: run **Boomerang Guardian → Build → macOS** (about 2 min) → `Builds/macOS/BoomerangGuardian.app`. Claude already made a verified build there from a clone, so you can double-click it right away.

## What the setup script created
| Where | What |
|---|---|
| Project Settings | Layer **Minimap** (8); product name "Boomerang Guardian"; Build Profiles: `Main.unity` only |
| `_Project/Materials/Minimap/` | Unlit icon colors (target red, obstacle grey, gem yellow, you cyan, platform blue, walls white, ground dark green) |
| `_Project/Settings/` | `RT_Minimap` (512² render texture), `MinimapArrow` (player arrow mesh) |
| Every prefab in `SpawnConfig` | A `MinimapIcon` child (flat shape on the Minimap layer, floats at a fixed height, stays level when the object tumbles; hit targets turn grey on the map too) |
| `Player/MinimapIcon` | Arrow that turns with the player (heading) |
| `Environment/MinimapIcons` | Flat ground, wall and platform shapes for the map |
| `Main Camera` | Culling mask excludes Minimap, so icons are invisible in the game view |
| `Cameras/MinimapCamera` | Orthographic top-down camera following the player (60 × 60 m view, north-up), renders only the Minimap layer into `RT_Minimap` |
| `UI/HUD/Panel_Minimap` | Upper-right 320 px map + color legend |
| `UI/HUD/Panel_Journey` | Top-center: current Hero's Journey stage + a hint for what to do next |
| `UI/HUD/Panel_Intro` | Start screen: story, controls, **Begin the Journey** (gameplay paused until clicked; ESC still quits) |
| `UI/HUD/Panel_End` | "Balance Restored!" with score, targets, gems and time, plus **Continue** / **Quit** |
| `UI/HUD` | + `JourneyController` (stage logic) |

## The seven stages (from the assignment story)
| # | Stage | Reached when |
|---|---|---|
| 1 | The Ordinary World | Intro screen |
| 2 | The Call to Adventure | You click **Begin**: objects fill the land |
| 3 | Crossing the Threshold | First boomerang throw |
| 4 | Trials and Challenges | First target hit. The hint shows a checklist: Jump (F), Fast (SPACE), both views (V), 10 hits |
| 5 | Mastery of Skills | All four of those done |
| 6 | The Ordeal | Step onto the Guardian Platform (the world disappears) |
| 7 | The Return | Back in the world with **25** total hits (or every target cleared) → end panel |

The thresholds can be tuned on `UI/HUD → JourneyController` (Mastery Target Hits, Return Target Hits). Untick **Show Intro** to skip the intro while testing.

## Play-test checklist (Sprint 3 Review demo)
| # | Do this | Expect | Rubric |
|---|---|---|---|
| 1 | Press Play | Intro screen with story + controls; WASD / clicks do nothing; ESC still quits | 15, story |
| 2 | Click **Begin the Journey** | Intro closes; top banner "Stage 2/7 · The Call to Adventure"; message line | story |
| 3 | Look at the upper-right | Minimap: you are a cyan arrow in the center pointing where you face; red dots = targets, grey squares = obstacles, yellow diamonds = gems, white walls, blue platform | **14** |
| 4 | Walk and turn | The map scrolls with you (north stays up), the arrow turns | 14 |
| 5 | Hit a target | Its map dot turns grey, then disappears after 2 s | 14 |
| 6 | Do jump / fast / both views / 10 hits | Checklist boxes tick → "Mastery of Skills" | story |
| 7 | Step onto the platform | "The Ordeal"; map shows only walls + platform | 12, 14 |
| 8 | Step off, hit targets until 25 total | "The Return" → **Balance Restored!** panel; Continue resumes, Quit exits | story |
| 9 | Game view | No icons visible in the 3D world (they're map-only) | 14 |
| 10 | Double-click `Builds/macOS/BoomerangGuardian.app` | Runs; ESC quits the app; `Builds/macOS/config.json` changes take effect (F5 or restart) | 15, PB-21 |
| 11 | Console | No red errors | DoD |

## Tuning
| Want | Where |
|---|---|
| Bigger / smaller map area | `Cameras/MinimapCamera → MinimapCamera → View Radius` (default 30 m) |
| Map turns with the player | `MinimapCamera → Rotate With Target` |
| Map size on screen | `UI/HUD/Panel_Minimap` RectTransform |
| Icon size / color | The `MinimapIcon/Shape` child in each prefab, or the `MI_*` materials |

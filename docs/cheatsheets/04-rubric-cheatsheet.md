# Cheat Sheet 4 — The rubric, category by category (what / where / how to demo)

Covers walkthrough **Part 3**. Spec: `docs/reference/3DGP_PA_01_Instruction.pdf` (marking scheme p. 11-12).
Status per item: `docs/00-requirements-traceability.md`. Script paths are relative to `Assets/_Project/Scripts/`.

**Each category below has:** the marks · the spec's words · how it is built · the key code (file:line) · a 20-second demo · what could go wrong / what to say.
Line numbers were taken from the current source; if you edit a file, re-check them.

| # | Category | Marks | # | Category | Marks |
|---|---|---|---|---|---|
| 1 | Scene construction | 6 | 9 | Spawn manager | 10 |
| 2 | Object organization | 4 | 10 | Collision avoidance (spawn) | 5 |
| 3 | Player movement | 8 | 11 | Physics & collision response | 8 |
| 4 | Speed toggle | 4 | 12 | Platform & triggers | 8 |
| 5 | Jump, gravity, projectile | 8 | 13 | Collectibles | 3 |
| 6 | Camera system | 8 | 14 | User interface | 5 |
| 7 | Ray casting & selection | 7 | 15 | Audio & program control | 4 |
| 8 | Boomerang mechanics | 12 | | **Total** | **100** (+10 / −10 report) |

---

## Cat 1 — Scene construction (6)
*Spec: large ground plane; cube boundaries around the playable region; objects stay inside; platform at the centre.*

- **Built in the Editor**, not code: `Environment/Ground`, `Environment/Boundary/Wall_North|South|East|West` (each wall = 14 cube pieces), `Environment/GuardianPlatform`.
- **Region size** is defined twice and must agree: the walls, and `SpawnManager.regionSize` (default 100 × 100 m) + `wallMargin` 2 m. `SpawnManager.OnDrawGizmosSelected` (`:367`) draws the spawn area green when you select it.
- **Demo:** Scene view → select `SpawnManager` → green rectangle inside the walls, cyan circles for platform/player clear zones. Play: walk to a wall; you can't leave.
- **Say:** "All spawn positions are generated inside `regionSize` minus the wall margin minus the object's own radius (`TryFindPosition`, `SpawnManager.cs:193-194`), so objects can't spawn in or beyond the walls."

## Cat 2 — Object organization (4)
*Spec: ground, boundary, targets, obstacles, player, boomerang, platform present; logical hierarchy and naming.*

- Present: ground ✔ boundary ✔ player ✔ platform ✔ — targets/obstacles are spawned at runtime under `Gameplay/Spawned/{Targets,Obstacles,Collectibles}` (`GetGroup`, `SpawnManager.cs:355`) named `Target_Ghost_001…` (`:184`); the **boomerang** exists as a prefab (`Prefabs/Greybox/Boomerang.prefab`) and appears in the Hierarchy as `Boomerang (in flight)` (`BoomerangThrower.cs:55`) while thrown.
- Hierarchy roots: `Gameplay · Systems · UI · Environment · Cameras` (see sheet 3 §2).
- **Demo:** Play, expand `Gameplay/Spawned`; throw a boomerang and point at the new `Boomerang (in flight)` object in the Hierarchy.
- **Say (the boomerang question):** "It's not sitting in the scene at rest: one is instantiated per throw so overlapping flights are possible; the prefab is the model."

## Cat 3 — Player movement (8): W/S, camera-relative, smooth
- **Where:** `Player/PlayerMotor.cs` → `FixedUpdate` (`:144`) → `Move` (`:156-168`).
```csharp
Vector3 wish = yaw * new Vector3(moveInput.x, 0f, moveInput.y);       // :159  rotate keyboard dir by camera yaw
if (wish.sqrMagnitude > 1f) wish.Normalize();                          // :160  no faster diagonals
horizontal = Vector3.MoveTowards(horizontal, wish * CurrentSpeed, acceleration * Time.fixedDeltaTime);  // :165  smoothing
body.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);   // :167  keep vertical (gravity/jump) untouched
```
- `yaw` comes from `cameraRig.Yaw` (`:148`). `moveInput` comes from `PlayerInputReader.Move` (W=+y, S=−y; A/D strafe as a bonus).
- **Smooth** = `groundAcceleration` 40 m/s²: velocity ramps to the target instead of snapping.
- **Demo:** Hold RMB and turn the camera, then press W: the player goes where the camera looks. Release W: it eases to a stop.
- **Say:** quaternion × vector rotates the vector. History: the `cameraRig` Inspector link was once missing, so movement only worked facing +Z; fixed, plus the `Awake` fallback + warning (`:106-114`) and `WiringValidator`.

## Cat 4 — Speed toggle (4)
- `PlayerInputReader` raises `ToggleSpeedPressed` on SPACE → `PlayerMotor.ToggleSpeed` (`:215`): `IsFast = !IsFast`; `CurrentSpeed => IsFast ? fastSpeed : normalSpeed` (`:69`) = **5 vs 10 m/s** (config `normalSpeed/fastSpeed`; `GameConfig.Validate` forces `fast ≥ normal + 1`).
- Feedback: `Debug.Log("Speed mode: …")`, HUD status `Speed: FAST/Normal` (`HudController.cs:84-89`), `SpeedModeChanged` event (also feeds the story's "Mastery" stage).
- **Demo:** SPACE while holding W: visibly faster; HUD text flips. F5 after editing `config.json` shows the speeds are tunable.

## Cat 5 — Jump, gravity, projectile motion (8)
- **Jump (F):** `RequestJump` stores `jumpRequestTime = Time.time` (`:222`); `TryJump` (`:170`) fires if the request is ≤ 0.15 s old **and** grounded:
```csharp
float launchSpeed = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);   // :176   v = √(2·g·h)
body.linearVelocity = new Vector3(velocity.x, launchSpeed, velocity.z);            // :178   only Y changes
```
- **Gravity:** engine gravity on the dynamic Rigidbody (`useGravity`); we never integrate it ourselves.
- **Projectile motion:** in the air `airAcceleration = 0` (`:33`), so horizontal velocity is untouched; constant horizontal + gravity-driven vertical = **parabola**. Jump while running and the arc carries you forward.
- **Grounded:** `CheckGrounded` (`:186`): `SphereCastNonAlloc` downward, ignores own colliders, requires `normal.y ≥ cos(50°)` (not a steep slope). `JumpGroundLockTime` 0.2 s stops re-jumping while still touching the floor.
- **Demo:** jump in place (apex ≈ 1.5 m), then run + jump (forward arc); jump off nothing and watch it land. Fall off? `fallResetY = −20` teleports you back (`:153`).
- **Say:** `v = √(2gh)` is from energy conservation, `½v² = g·h`.

## Cat 6 — Camera system (8): first person, third person, pitch/yaw/distance
- **Where:** `Cameras/CameraRig.cs`. **V** → `SwitchViewPressed` → `ToggleMode` (`:215`) → `SetMode` (`:218`; also hides the player's body renderers in FP via `ShadowCastingMode.ShadowsOnly`).
- **First person:** camera at `target.position + eyeOffset` with the clamped pitch (−80°..80°).
- **Third person:** `pivot + back * distance` (`:138-141`), pitch −20°..70°; **distance** = scroll wheel (`UpdateZoom` `:185`, range 2-15 m, smoothed with `SmoothDamp`); `ResolveCollision` (`:196`) pulls the camera in front of walls (SphereCast; pushable props ignored to avoid jitter).
- **Yaw/pitch:** hold **RMB** + mouse (`UpdateLook` `:145`); cursor is locked while looking, free otherwise (needed for picking).
- **Bonus:** **MMB** orbit preview: camera-only offsets that ease back; gameplay yaw unchanged.
- **Demo:** V (FP ↔ TP), RMB-drag (yaw+pitch), scroll (distance), back against a wall (camera pulls in).
- **Say:** `LateUpdate` so the camera uses the player's final position; the rig owns yaw.

## Cat 7 — Ray casting & object selection (7): accurate picking, wrong-selection handling
- **Where:** `Interaction/TargetSelector.cs`. LMB → `SelectPressed` → `RequestSelect` (flag) → `Update` (`:66`, skips if the pointer is over the HUD) → `Select` (`:75`) → `Evaluate` (`:98`):
```csharp
Ray ray = cameraRig.ViewCamera.ScreenPointToRay(screenPosition);                                   // :101
bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, pickMask,
                                    QueryTriggerInteraction.Ignore);                               // :102
target = hitSomething ? hit.collider.GetComponentInParent<Target>() : null;                        // identify by component
```
- **Results** (`enum SelectionResult`): `Thrown`, `NotATarget` (message: "That's an obstacle, not a target!" / "That's not a target!" / "Nothing there to throw at."), `TooFar` (> 40 m), `OnCooldown` ("Boomerang not ready (x.x s)"), `AlreadyHit` (silently ignored).
- **Wrong selection:** `Reject` → `SelectionRejected(reason, message)` → **error SFX** (`AudioManager.OnSelectionRejected`) + **HUD message** (`HudController`).
- `pickMask` is an Inspector `LayerMask` (its tooltip says to exclude the Player layer; check the actual value on the Player's `TargetSelector` before the demo). `QueryTriggerInteraction.Ignore` means invisible trigger volumes (platform trigger, boomerang) can't be "picked".
- **Bonus:** hover shows the planned flight curve (`TrajectoryPreview`, uses the same `Evaluate`).
- **Demo:** click a ghost (throw) · click a crate (error beep + "obstacle") · click the ground/sky (error) · click a far ghost ("Too far") · click again within 3 s ("not ready").
- **Say:** "Evaluate decides without acting; Select = Evaluate + act, the hover preview = Evaluate + draw."

## Cat 8 — Boomerang mechanics (12)
*throwing system 3 · suitable initial speed 2 · return to player 3 · cooldown 2 · overall stability 2*

- **Throw:** `BoomerangThrower.Throw` (`:40`): needs `IsReady && target != null`; `Instantiate` at the hand (`:54`), `Launch(this, target, Speed)`.
- **Initial speed:** **20 m/s** (`speed`, config `boomerangSpeed`, clamped 5-60).
- **Cooldown:** `lastThrowTime = Time.time` (`:49`); `CooldownRemaining = max(0, lastThrowTime + Cooldown − Time.time)` (`:33`); `IsReady = remaining ≤ 0` (`:36`). **3 s** (config `boomerangCooldown`, clamped 0.5-10). HUD bar: `CooldownProgress` (0 → 1).
- **Outbound:** quadratic Bézier from hand to `target.AimPoint`, control point = midpoint pushed sideways (`curve` 0.3) and up (`arcHeight` 0.5) (`BoomerangProjectile.ControlPoint` `:197`, `Bezier` `:207`). `pathT` advances `speed·dt / pathLength`; `body.MovePosition(Bezier(...))` (`:130-134`). The path follows the target's **live** position if it's pushed.
- **Hit:** trigger contact (`OnTriggerEnter` `:153`) or reaching `pathT ≥ 1` (anti-tunnelling) → `HitTarget` (`:176`) → `Target.Hit(direction * 10 N·s)`; obstacle → `Bump` + bounce back.
- **Return:** `State.Returning`: `MoveTowards(current, hand, speed * 1.2 * dt)`; **caught** within 0.8 m → `Catch()` → `ReportCaught` + `Destroy`.
- **Stability:** `maxFlightTime` 8 s → forced catch (`:116`); target destroyed/disabled/hit-by-another → turn back (`:124`); thrower destroyed → destroy self (`:115`); returning ignores all collisions; one projectile per throw so the 3 s cooldown is the only gate (ADR-0005 amendment).
- **Demo:** throw at a near ghost, watch curve → hit → return; spam-click (cooldown message); throw at a far one (travel time); step on the platform mid-flight (targets vanish; the boomerang turns back and doesn't get lost).
- **Say:** kinematic + trigger + scripted path = deterministic; real physics only for the impact impulse.

## Cat 9 — Spawn manager (10): random, 200-500, ≥100 targets, ≥100 obstacles, within region
- **Where:** `Spawning/SpawnManager.cs`. `Start → SpawnAll` (`:103,106`): `DestroyAll()`, seed (`Random.InitState` if `randomSeed ≠ 0`), then **three** `SpawnGroup<T>` (`:121-123`).
- **Counts:** shipped `config.json`: **150 targets + 150 obstacles + 30 collectibles = 330** ✔ (200-500, ≥100, ≥100). `GameConfig.Validate` **enforces** the rubric limits even if someone edits the JSON (`MinTargets 100`, `MinObstacles 100`, `MaxTotalObjects 500`; `GameConfig.cs:22-24,70-76`).
- **Random:** random prefab per spawn (`Random.Range(0, prefabs.Count)` `:176`), random yaw (`:182`), random XZ inside the region (`:203`).
- **Within region:** half-extent = `regionSize/2 − wallMargin − footprintRadius` (`:193-194`).
- **Proof line** printed at the end of `SpawnAll` (`:128-132`):
  `SpawnManager: 150 targets + 150 obstacles + 30 collectibles = 330 objects in … ms (seed: random). Failed placements: 0. Overlap check: 0 overlapping pairs.`
- **Demo:** open the Console, press Play, show that line. Hierarchy → `Gameplay/Spawned` counts. Edit `config.json` (e.g. `targetCount: 400`), press **F5**, and show it clamped to 300 with a warning ("values were clamped").
- *(F5 reloads the config; the layout changes when `SpawnAll` runs again, e.g. on platform exit in Regenerate mode.)*

## Cat 10 — Collision avoidance during spawn (5): overlap detection, collision-free placement
Four layers of defence (`SpawnManager.cs`):
1. **Footprint** per prefab (`GetFootprint` `:248`): instantiate a temporary probe far away, union its collider + renderer bounds (ignoring minimap icons), compute a **rotation-independent XZ radius** `√(x²+z²)` and a height; cached per prefab.
2. **Exclusion zones** (`IsInside` `:219`): platform (6 m), player start (4 m), walls (margin).
3. **Against already-placed objects** (`OverlapsPlaced` `:226`): our own list of circles + `spacing` 0.5 m. (Needed because freshly instantiated colliders aren't visible to physics queries until the next step.)
4. **Against scene geometry** (`OverlapsScene` `:236`): `Physics.CheckBox` from 5 cm above the ground with the footprint's extents.
- **Retry** up to `maxAttemptsPerObject` = 60 (`:201`); failures are counted, not hidden.
- **Verification** (`CountOverlappingPairs` `:291`): every collider pair → `Bounds.Intersects` → `Physics.ComputePenetration`; counts real penetrations > 1 mm. Logged as "Overlap check: N overlapping pairs" (shipped: **0**, over 3 runs of 330).
- **Say:** rejection sampling = "random candidate, reject if invalid, retry"; the validation pass is an independent check, not the same code that placed them.

## Cat 11 — Physics & collision response (8): impact, push, vanish after 2 s, obstacle pushing
- **Impact + push:** `BoomerangProjectile.HitTarget` (`:176`): direction = flight direction flattened + a lift (`targetLift` 0.3), × `targetImpulse` 10 N·s → `Target.Hit` (`Target.cs:81`):
```csharp
body.AddForceAtPosition(impulse, point, ForceMode.Impulse);   // push + a bit of spin   (Tumble)
AnyHit?.Invoke(this);                                         // score/audio/story
Destroy(gameObject, disappearDelay);                          // :96   2 s later: gone
```
- **Visible for 2 s:** `Destroy(obj, 2f)` is delayed; meanwhile the ghost's reaction runs. `FloatAway` (ghosts): gravity off, drag, upward acceleration in `FixedUpdate` (`:116`), alpha fade in `Update` (`:121`) over exactly `disappearDelay`. The minimap dot turns grey. `IsHit` prevents double scoring.
- **Obstacles can't be destroyed:** `Obstacle` has no destroy path; the boomerang calls `Bump` (small impulse) and returns (`BoomerangProjectile.cs:167-173`).
- **Player pushes obstacles (1):** no script: the player is a dynamic Rigidbody, obstacles are Rigidbodies; zero-friction player material avoids sticking (`PlayerMotor.Awake` `:94-100`).
- **Demo:** hit a ghost: it flies/floats and fades for ~2 s then disappears; walk into a crate: it slides/tips over.
- **Say:** "Two different reactions per prefab (`Tumble` / `FloatAway`, ADR-0010), both satisfy: pushed away, visible 2 s, then removed."

## Cat 12 — Interactive platform & triggers (8): detection, remove all on entry, respawn on exit
- **Where:** `Interaction/GuardianPlatform.cs` on `GuardianPlatformTrigger` (a trigger collider; the visible `GuardianPlatform` mesh stays untouched, spec: "platform remains visible").
- **Detection (3):** `OnTriggerEnter/Exit`; `IsPlayer(other)` = attached Rigidbody has a `PlayerMotor` (ignores boomerangs and others); `playerColliderCount` makes it fire once even with several player colliders.
- **Remove all (2):** `spawnManager.HideAll()` (`SpawnManager.cs:139`): `SetActive(false)` on all targets, obstacles **and** collectibles; `IsHidden = true`.
- **Respawn (3):** `Respawn()` (`:147`): `Regenerate` = `SpawnAll()` (fresh random layout at full count) or `Restore` = `SetActive(true)` (same objects back; destroyed targets stay gone). Chosen in `config.json → respawnMode` (the spec is ambiguous; ADR-0008), F5 switches it live.
- **Feedback:** enter/leave SFX, HUD message ("The Guardian Platform hides the world…"), HUD line "The world has vanished. Leave the platform!"; story stage "The Ordeal".
- **Demo:** walk onto the platform: everything vanishes (platform stays), SFX; step off: world returns, SFX. Then set `Restore` in `config.json`, F5, repeat to show the other interpretation.

## Cat 13 — Collectible items (3): implementation, trigger behaviour
- **Where:** `Interaction/Collectible.cs` (prefab `Collectible_Gem`; spawned 30 per round by the same `SpawnManager`).
- It is a trigger (`Awake` forces `isTrigger` `:53`). `OnTriggerEnter` (`:103`): only the player (`PlayerMotor` check), once (`collected` flag), then `AnyCollected?.Invoke(this)` + `Destroy(gameObject)`.
- **Game logic triggered:** `ScoreManager.OnCollected` → **+5** points; counts toward the end-panel summary ("Gems").
- Visual polish: bob + spin or face the player (`Update` `:76`), moves only the child visual; minimap icon.
- **Demo:** walk into a gem: it disappears, score +5, SFX/HUD "+5".

## Cat 14 — User interface (5): score display, updates correctly, functional minimap
- **Score:** `HudController.Update` writes `scoreText.SetText("Score: {0}", score.Score)` (`:75-76`) every frame, visible during gameplay. **Updates correctly:** `ScoreManager` adds **10 per target hit by a boomerang** (via `Target.AnyHit`; only the first hit, thanks to `IsHit`), 5 per gem. HUD also shows "Targets hit / Remaining", speed/view, the cooldown bar, and "+N" popups.
- **Minimap (upper right):** `Panel_Minimap` with a `RawImage` showing `RT_Minimap`; `MinimapCamera` (orthographic, 60 m above, `viewRadius` 30 → 60 × 60 m around the player, north-up, `LateUpdate` follow). Flat icons on the `Minimap` layer: targets (20 m high, red), obstacles, gems, platform, walls; the player is an arrow (turns with yaw). The main camera culls the Minimap layer; the map camera disables fog during its own render.
- **Demo:** show the corner map while moving: the arrow turns, nearby dots shift; hit a ghost: its dot turns grey, score rises.

## Cat 15 — Audio & program control (4): five SFX, ESC quits
- **Five required SFX** (`Audio/AudioManager.cs`, each wired to an event; real clips in `Assets/_Project/Audio/`):

| Event | Source event | Handler |
|---|---|---|
| Throw | `thrower.Thrown` | `OnThrown` → `throwSfx` |
| Boomerang hits an object | `thrower.TargetHit` (+ `ObstacleHit` separately) | `OnTargetHit` → `hitTargetSfx`, `OnObstacleHit` |
| Invalid selection | `selector.SelectionRejected` | `OnSelectionRejected` → `invalidSfx` |
| Enter platform | `platform.PlayerEntered` | `enterSfx` |
| Leave platform | `platform.PlayerExited` | `leaveSfx` |
(+ extras: catch, collect, BGM.) Any empty clip falls back to a synthesized tone (`PlaceholderTones`) so nothing is silent.
- **ESC:** `Quit` action → `PlayerInputReader.QuitPressed` → `ApplicationController.Quit` (`#if UNITY_EDITOR` stop Play mode, else `Application.Quit()`). **Works even while a menu has paused gameplay** (Quit/F5 bypass `GameplayEnabled`).
- **Demo:** do each of the 5 actions and name the sound; press ESC at the end (in a build: the app closes).

---

## Bonus — Report PDF (+10 if done, −10 if missing)
1-2 pages, ≥ 500 words, AI usage, Scrum, third-party asset credits (`docs/asset-credits.md`, `docs/report/`). The traceability table still shows ⬜ for this row, although the git log shows the report was drafted and rewritten (`docs(PB-22)…`). `docs/report/Report_PA1_BoomerangGuardian.pdf` exists. **Confirm it is the final version, is ≤ 2 pages, and was uploaded to E3.**

---

## One-page demo checklist (in order, ~5 min)

1. Console open → Play → spawn proof line (330 objects, 0 overlaps) *[9, 10]*
2. Intro panel → Begin; point out the HUD: score, cooldown, minimap, stage *[14]*
3. W/S + camera-relative turn; SPACE fast; F jump (run + jump = arc) *[3, 4, 5]*
4. V first/third person; RMB look; scroll distance; wall pull-in *[6]*
5. Click: ghost (throw) / crate (error) / ground (error) / far ghost / cooldown *[7, 8]*
6. Watch curve → hit → 2 s fade → boomerang returns; score +10 *[8, 11, 14]*
7. Push a crate by walking into it *[11]*
8. Collect a gem *[13]*
9. Platform: enter (world vanishes) → leave (respawns); flip `respawnMode` + F5 *[12]*
10. Name the five SFX as they play; ESC *[15]*
11. Hierarchy tour: roots, `Spawned` groups, `Boomerang (in flight)` *[1, 2]*

## Known limits (say them before the TA finds them)
- The boomerang path is **scripted**, not aerodynamic (ADR-0005).
- Animation: some foot sliding, a doubled jump hop, throw pose not synced to the release frame (ADR-0011).
- Spawn is probabilistic: it logs `Failed placements`; the shipped run logs 0.
- A/D strafing exists beyond the spec's W/S; `Restore` vs `Regenerate` is an interpretation choice made switchable on purpose.

# Cheat Sheet 5 — Demo prep: tools, drills, Q&A, and survival tips

Covers walkthrough **Part 4**. The spec says you may be asked "to explain the functionality and design of any part of your project", and "you are responsible for understanding, integrating, testing, and explaining all submitted code". This sheet prepares for that.
Script paths relative to `Assets/_Project/Scripts/`.

---

## 1. Before the demo: 10-minute pre-flight

| ✔ | Check | How |
|---|---|---|
| ☐ | Project opens, **no red errors** in the Console | Unity Hub → open the project folder (Unity **6000.3.26f1**). Console → clear → look for `error CS` |
| ☐ | Scene is `Assets/_Project/Scenes/Main.unity` | double-click it |
| ☐ | Press Play: spawn line shows **330 objects, 0 overlapping pairs** | Console |
| ☐ | **Validate Wiring** is clean | menu *Boomerang Guardian → Validate Wiring* |
| ☐ | Game view is **not** on "Maximize on Play" with the Console hidden | you want to show logs |
| ☐ | `Assets/StreamingAssets/config.json` has `respawnMode: "Regenerate"` and the shipped values | open it; know how to flip to `"Restore"` |
| ☐ | Built app works (if the TA wants a build) | *Boomerang Guardian → Build → macOS* → `Builds/macOS/*.app`; `config.json` is next to the `.app` |
| ☐ | Audio unmuted (Game view speaker icon off-mute), volume up | five SFX must be audible |
| ☐ | Second plan: screen recording of a clean run | in case the Editor misbehaves |
| ☐ | Report PDF final, ≤ 2 pages, uploaded | bonus/penalty row |
| ☐ | `git status` clean; zip excludes `Library/`, `Builds/`, `Temp/` | Unity regenerates `Library/` |

**Headless smoke test of a build** (no window; checks it launches without exceptions):
`"<app>/Contents/MacOS/Boomerang Guardian" -batchmode -nographics -logFile /tmp/run.log`, then `grep -i exception /tmp/run.log`.
**Compile check without the Editor** (only when the Editor is *closed*): `/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -nographics -projectPath "$PWD" -logFile /tmp/unity-compile.log`, then `grep "error CS" /tmp/unity-compile.log`. Never batch-run on the live project while the Editor is open (use an APFS clone, see `CLAUDE.md`).

---

## 2. Unity Editor map (for showing things quickly)

| Panel | What you do with it in the demo |
|---|---|
| **Hierarchy** | the scene tree: show the five roots, `Spawned` groups, `Boomerang (in flight)` |
| **Inspector** | a selected object's components and `[SerializeField]` fields (tooltips on hover). Click `Player` → `PlayerMotor`: speeds, jump height; `BoomerangThrower`: cooldown, speed |
| **Scene view** | edit view; `SpawnManager` selected → green region gizmo |
| **Game view** | what the player sees |
| **Project** | assets; `Scripts/`, `Prefabs/Greybox/`, `Settings/` (SpawnConfig, inputactions) |
| **Console** | `Debug.Log` output: spawn proof line, "Speed mode: FAST", "Selected X → boomerang thrown", "Invalid selection" |

Useful moves: **Play** ▶, **Pause** ⏸, **Step**. **Changes made to values in the Inspector during Play are lost when you stop** (so tweak live to demonstrate, don't expect them to stick). Select an object in the Hierarchy during Play to watch its values change (e.g. `BoomerangThrower`'s state, `Rigidbody` velocity).
Debug aids already in the code: `Debug.DrawRay` of the pick ray (yellow = hit, grey = miss) in the Scene view (`TargetSelector.cs:103-105`), `OnDrawGizmosSelected` on `SpawnManager`.

---

## 3. The 2-minute architecture speech (memorise the shape, not the words)

1. "Unity-6, URP, Input System. **Everything is event-driven.**"
2. "**Input → gameplay → events → presentation.** One `PlayerInputReader`; `PlayerMotor`, `CameraRig`, `TargetSelector` consume it; thrower/platform/targets raise events; `ScoreManager`, `HudController`, `AudioManager`, `JourneyController` only listen."
3. "**Physics choices:** dynamic Rigidbody player (gravity, arc, pushing for free); kinematic scripted-Bézier boomerang (stable) with a real impulse on the target."
4. "**Content:** `SpawnManager` + `SpawnConfig`; rejection sampling with footprints, `Physics.CheckBox`, verification by `ComputePenetration`; 330 objects, 0 overlaps."
5. "**Data-driven:** `config.json` (counts, speeds, cooldown, respawn mode), reloadable with F5; clamped to the rubric limits."
6. "**Process:** Scrum with sprints/backlog/ADRs; AI wrote C# and Editor setup scripts; art, scene and sound decisions were mine; every AI interaction is logged in `docs/ai-log/prompt-log.md`."

---

## 4. "Explain this code" drills

For each, practise saying it out loud in ~45 s. Open the file first.

| File : lines | What to say (skeleton) |
|---|---|
| `PlayerMotor.cs:156-168` `Move` | camera yaw × input = world direction; normalise; `MoveTowards` for smooth accel; keep Y so gravity/jump work |
| `PlayerMotor.cs:170-184` `TryJump` | buffered request + grounded; `v=√(2gh)`; set only Y; ground-lock to avoid double jump |
| `PlayerMotor.cs:186-206` `CheckGrounded` | sphere cast down; ignore own colliders; slope limit from the surface normal |
| `CameraRig.cs:126-143, 145-183` | yaw owned here; FP vs TP placement; collision pull-in; RMB look vs MMB orbit offsets |
| `TargetSelector.cs:98-135` `Evaluate` | ray → collider → `GetComponentInParent<Target>`; enum result; message strings; preview shares it |
| `BoomerangThrower.cs:33-60` | cooldown arithmetic; instantiate + Launch; re-raises projectile events |
| `BoomerangProjectile.cs:112-151` | `FixedUpdate` state machine: outbound Bézier, returning homing, safety timeout |
| `BoomerangProjectile.cs:197-211` | Bézier: `(1−t)²P0 + 2(1−t)t·P1 + t²P2`; control point = midpoint + right·(dist·curve) + up·arc |
| `Target.cs:81-98` `Hit` | once-only; reaction; raise `AnyHit`; delayed `Destroy` |
| `SpawnManager.cs:191-217` `TryFindPosition` | sample, four rejection tests, retry cap |
| `SpawnManager.cs:248-281` `GetFootprint` | probe instance far below, union bounds, rotation-independent radius, cache |
| `SpawnManager.cs:291-324` | independent verification pass |
| `GuardianPlatform.cs:38-61` | trigger enter/exit, `IsPlayer`, collider counter, Hide/Respawn |
| `ScoreManager.cs` | two static-event subscriptions → `Add` → `ScoreChanged` |
| `GameConfig.cs:57-102` `Validate` | clamps to rubric limits; local function `Note<T>` logs changes |
| `ConfigLoader.cs:62-89, 92-101` | candidate paths iterator (priority order), parse with `JsonUtility`, fall back, raise `Changed` |

---

## 5. Live-change drills (a TA may say "change X and show me")

Prefer the **zero-code** route; each is a legitimate "tunable by design".

| Request | Fastest way | Note |
|---|---|---|
| "Make the boomerang slower/faster" | `config.json → boomerangSpeed` then **F5** (clamped 5-60) | or Inspector on `BoomerangThrower` (only if no config is loaded; JSON overrides) |
| "Change the cooldown" | `boomerangCooldown` then F5 (0.5-10) | |
| "Player faster" | `normalSpeed` / `fastSpeed` then F5 | `PlayerMotor` re-reads on `Changed` |
| "Jump higher" | Inspector `PlayerMotor.jumpHeight` (not in JSON) | change during Play to demo; code: `v=√(2gh)` |
| "More/fewer objects" | `targetCount`, `obstacleCount`, `collectibleCount` | needs a respawn (leave platform in Regenerate) or restart; clamped to 100/100/500 |
| "Make respawn restore the same objects" | `respawnMode: "Restore"`, F5, enter + leave the platform | |
| "Repeatable layout" | `randomSeed: 12345` | `0` = random each run |
| "Score per hit" | `targetScore`, `collectibleScore` | |
| "Longer/shorter reaction before vanish" | `Target` prefab `disappearDelay` (spec says 2 s, so explain you'd keep it) | |
| "Throw range" | `maxThrowRange` | "Too far!" message |

**If you must edit C#:** keep the edit tiny (e.g. a constant), save, wait for the spinner (domain reload), Play. Don't edit while in Play mode (it recompiles and resets). If it breaks, **Ctrl/Cmd+Z**, or `git checkout -- <file>` (you'd be using git in the repo, so check `git status` first).

---

## 6. Rapid-fire Q&A (beyond the per-sheet lists)

**Unity basics**
- *GameObject vs Component vs Prefab?* Container / behaviour on it / reusable saved template instantiated at runtime.
- *What is a Rigidbody? Collider? Trigger?* Physics body (mass, velocity, forces) / shape for contacts / a collider that reports overlaps but doesn't block.
- *Dynamic vs kinematic Rigidbody?* Dynamic = physics moves it (player, targets, obstacles); kinematic = script moves it, physics still reports contacts (boomerang).
- *Why does one object need a Rigidbody for triggers?* At least one of the two colliding objects must have one for trigger/collision callbacks to fire (the player has one).
- *Raycast vs SphereCast vs CheckBox?* Thin line / thick line / box overlap test (no movement). Ray = picking; SphereCast = ground + camera collision; CheckBox = spawn overlap.
- *Layers?* Used for the minimap (icons on `Minimap`, main camera culls it) and for pick/collision masks.
- *What is a ScriptableObject?* A data asset not in a scene; `SpawnConfig` holds prefab lists.
- *Why URP?* Unity 6 default modern pipeline (ADR-0002).
- *Time.deltaTime vs fixedDeltaTime?* Variable frame time vs fixed physics step (sheet 2).

**C#**
- *What does `?.Invoke()` / `??` do?* (sheet 1 §4, §7)
- *Struct vs class?* (sheet 1 §2.2)
- *Lambda?* Only used in Editor tools; runtime uses named handlers for unsubscribe. *Coroutine?* Not used (sheet 1 §9).

**Design**
- *How do you know a target is valid?* Ray hits a collider whose parent chain has a `Target` component; not by tag.
- *How is double scoring prevented?* `Target.IsHit` gate; `Hit` returns false the second time.
- *What if the boomerang's target dies mid-flight?* It turns back (`BoomerangProjectile.cs:124`).
- *What if two boomerangs hit the same target?* First wins; second sees `IsHit` and returns.
- *What happens at the platform with a boomerang in flight?* Targets are inactive → the projectile detects `!isActiveAndEnabled` and returns.
- *Why does the camera not shake among 300 obstacles?* Collision pull-in ignores non-kinematic Rigidbodies (pushable props).
- *Why don't objects spawn on the player or platform?* Exclusion circles in `TryFindPosition`.
- *How do you know there are no overlaps?* The verification pass logs 0 pairs.
- *How would you add a new target type?* Make a prefab with `Target` + Rigidbody + collider, add to `SpawnConfig`, rerun Setup (adds the minimap icon). No code.

**Process / AI**
- *What did AI do?* C# scripts, Editor setup/validation tools, ADRs and docs, report draft. *What did you do?* Product decisions, art/sound/scene work, testing and acceptance in Play Mode (see Sprint Reviews), reflection.
- *How did you verify AI output?* Play-test per sprint, spawn log/validation, wiring validator, headless builds; the log records AI slips and corrections.
- *Show me proof of Scrum.* `docs/01-product-backlog.md`, `docs/sprints/`, GitHub issues (issue = PB + 1), commits `type(PB-xx): … (#n)`, DoD `docs/02-definition-of-done.md`.
- *Third-party assets?* `docs/asset-credits.md` and the report; the repo is public, so only redistributable assets are committed.

---

## 7. Glossary (one line each)

**Prefab** reusable object template · **Instantiate** create a copy at runtime · **Component** behaviour on a GameObject · **MonoBehaviour** script base class · **Inspector** property panel · **SerializeField** show a private field there · **Layer / LayerMask** object category / set of categories for queries · **Raycast** line query against colliders · **Trigger** non-blocking collider that fires enter/exit · **Kinematic** moved by script, not forces · **Impulse** instantaneous force (`ForceMode.Impulse`) · **Quaternion** rotation type · **Bézier** curve from control points · **Yaw/Pitch** left-right / up-down rotation · **RenderTexture** camera output stored as a texture (minimap) · **ScriptableObject** data asset · **StreamingAssets** folder copied unchanged into builds (config.json) · **Domain reload** recompile/reset on entering Play Mode · **ADR** architecture decision record · **PB / SP / DoD** product backlog item / story point / definition of done.

---

## 8. If something breaks during the demo

| Symptom | Likely cause → fix |
|---|---|
| Pink/magenta materials | render-pipeline mismatch in that scene/asset → show the recording/build instead |
| Nothing moves / input dead | intro panel is still open (gameplay is paused) → click **Begin**; or Game view isn't focused → click it |
| Can't look around | look is **RMB hold** (cursor free otherwise, by design for picking) |
| Click does nothing | clicked the HUD, or on cooldown (message shows), or the object was already hit |
| "Too far!" | max throw range 40 m; walk closer |
| No sound | Game view mute, or system volume; the placeholders also play if a clip is empty |
| `NullReferenceException` in the Console | an Inspector reference unassigned → run **Validate Wiring** |
| Layout looks the same after F5 | F5 reloads values only; new layout comes from `SpawnAll` (platform exit in Regenerate mode) |
| ESC doesn't close the Editor | in the Editor ESC stops Play Mode (by design); in a build it quits |
| Stuck at `timeScale = 0` after an Editor stop | `JourneyController.OnDestroy` resets it; otherwise Edit → Project Settings → Time |
| Compile error after a live edit | Cmd+Z / `git checkout -- <file>`, wait for recompile |

---

## 9. Mindset

- **Say what you don't know, then reason aloud.** "I'd check `X`; I believe it works like `Y` because `Z`." Better than bluffing.
- **Own the AI usage plainly**: the spec explicitly allows it; what it tests is that you can explain and integrate.
- **Lead with the demo, then the code.** Show behaviour first (20 s), then open the 10-20 lines responsible.
- Know **three files cold**: `PlayerMotor`, `BoomerangProjectile`, `SpawnManager`. Everything else you can navigate to via the checklist in sheet 4.

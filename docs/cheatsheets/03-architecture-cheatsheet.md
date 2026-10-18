# Cheat Sheet 3 — Architecture of "The Boomerang Guardian"

Covers walkthrough **Part 2**. Read after [01](01-csharp-cheatsheet.md) and [02](02-unity-lifecycle-cheatsheet.md).
Script paths are relative to `Assets/_Project/Scripts/`. Snippets are real unless marked *illustrative*.
Decisions are recorded in `docs/decisions/ADR-0001…0011`; cite the ADR number when the TA asks "why?".

---

## 1. The 30-second pitch

> "It is a Unity 6 URP game. A **dynamic-Rigidbody player** moves relative to a **yaw-owning camera rig**. A left click **raycasts**; a valid ghost makes the **thrower** spawn a **kinematic boomerang** that flies a **Bézier path**, hits, and returns. Everything talks through **C# events**, so score, HUD, audio and the story are decoupled listeners. A **SpawnManager** fills the arena with random non-overlapping objects; a **trigger platform** hides and respawns them. Tunables come from **config.json**; scene wiring is done by **idempotent Editor scripts**."

---

## 2. Scene hierarchy (verified from `Scenes/Main.unity`)

```
Gameplay
 ├─ Player          (Rigidbody, CapsuleCollider, PlayerMotor, PlayerInputReader, TargetSelector,
 │   │               BoomerangThrower, PlayerAnimator, ...)
 │   ├─ MinimapIcon   (arrow on the Minimap layer)
 │   ├─ Nose · TrajectoryPreview · Point Light     (+ the witch model/Hand under it)
 └─ Spawned         (SpawnManager fills this: Targets / Obstacles / Collectibles groups)
Systems
 ├─ GameManager     (ScoreManager, ConfigLoader, ApplicationController, JourneyController, ...)
 ├─ SpawnManager · AudioManager · Music
UI
 ├─ EventSystem
 └─ HUD → Panel_Stats, Panel_Cooldown, Text_Message, Panel_Minimap, Panel_Journey, Panel_Intro, Panel_End
Environment
 ├─ Lighting (Directional Light, PostProcess)
 ├─ Ground
 ├─ Boundary → Wall_North/South/East/West  (14 child pieces each)
 ├─ GuardianPlatform  (visible mesh)   +   GuardianPlatformTrigger (invisible trigger volume)
 └─ MinimapIcons (flat icons for ground, walls, platform)
Cameras
 ├─ Main Camera     (CameraRig)
 └─ MinimapCamera   (MinimapCamera → RT_Minimap)
```
Why it matters for the rubric ("logical hierarchy and naming", 2 marks): five clear roots, one concern each. The platform has **two** objects (mesh + trigger) so the mesh stays visible while spawned objects vanish.
*(Which exact components sit on `Player` vs `GameManager` is easiest to confirm by clicking them in the Inspector before the demo.)*

---

## 3. Folder → namespace → responsibility

```
Assets/_Project/
  Scripts/  (namespace BoomerangGuardian.<Folder>)
    Core/         ConfigLoader · GameConfig · ScoreManager · ApplicationController · MaterialUtility · OptionalReferenceAttribute
    Player/       PlayerInputReader · PlayerMotor · PlayerAnimator
    Cameras/      CameraRig · MinimapCamera
    Boomerang/    BoomerangThrower · BoomerangProjectile
    Interaction/  Target · Obstacle · Collectible · TargetSelector · GuardianPlatform · TrajectoryPreview · IdleWander
    Spawning/     SpawnManager · SpawnConfig (ScriptableObject)
    UI/           HudController · JourneyController · MinimapIcon
    Audio/        AudioManager · PlaceholderTones
    Editor/       Sprint2/3/5Setup · AudioSetup · PlayerCharacterSetup · WiringValidator · SetupUtils · BuildScript
  Prefabs/Greybox/   Target_Ghost · Obstacle_Crate · Obstacle_Pillar · Collectible_Gem · Boomerang
  Settings/    PlayerControls.inputactions · SpawnConfig.asset · RT_Minimap.renderTexture · MinimapArrow.asset
  Audio/ · Models/ · Materials/ · Animation/ · Scenes/Main.unity
Assets/StreamingAssets/config.json     ← must sit at Assets/ root (Unity rule)
```
Folder `Cameras` (plural): a namespace `BoomerangGuardian.Camera` would shadow `UnityEngine.Camera`.
`Editor/` is special: Unity compiles it separately and **never ships it in a build**.

---

## 4. The dependency map

```
                    ┌────────────────────────────┐
 Keyboard / Mouse ─▶│ PlayerInputReader  (Player)│   only script that touches the Input System
                    └──────────────┬─────────────┘
   events: Jump · ToggleSpeed · SwitchView · Select(LMB) · Quit(ESC) · ReloadConfig(F5)
   polled: Move · LookDelta · IsLookHeld(RMB) · IsOrbitHeld(MMB) · Zoom · PointerPosition
        │              │               │                │               │
        ▼              ▼               ▼                ▼               ▼
  PlayerMotor     CameraRig      TargetSelector   ApplicationCtl   ConfigLoader(F5)
  move/jump        yaw/pitch/     raycast pick      ESC → quit      reload JSON
        ▲          distance            │ valid target
        │ reads CameraRig.Yaw          ▼
        └───────────────────────  BoomerangThrower ──Instantiate──▶ BoomerangProjectile
                                  (cooldown, hand)                   (Bézier, trigger hit, return)
                                        ▲ ReportTargetHit/ObstacleHit/Caught          │ Target.Hit(), Obstacle.Bump()
                                        └─────────────────────────────────────────────┘
 Target.AnyHit (static) ─────────▶ ScoreManager ── ScoreChanged ──▶ HudController (+10, score text)
 Collectible.AnyCollected (static) ▶     │
                                         └──────▶ JourneyController (story stages)

 GuardianPlatform (trigger) ── HideAll()/Respawn() ──▶ SpawnManager ──▶ Spawned/Targets|Obstacles|Collectibles
        └─ PlayerEntered / PlayerExited ──▶ HudController · AudioManager · JourneyController

 AudioManager ◀── thrower.Thrown/TargetHit/ObstacleHit/Caught · selector.SelectionRejected
                  platform.PlayerEntered/Exited · Collectible.AnyCollected
 ConfigLoader.Current / Changed ──▶ PlayerMotor, BoomerangThrower, TargetSelector, SpawnManager, ScoreManager (read as overrides)
```
Direction of dependency: **input → gameplay → events → presentation (HUD/audio/story)**. Gameplay scripts never reference the HUD or AudioManager; those listen.

---

## 5. Design patterns used (name them for the TA)

| Pattern | Where | One-liner |
|---|---|---|
| **Observer** (C# `event`) | thrower, selector, platform, `Target.AnyHit`, `ScoreManager` | publisher doesn't know subscribers |
| **Facade / single gateway** | `PlayerInputReader` (ADR-0003) | one place for input; `SetGameplayEnabled(false)` mutes it all |
| **State machine** | `BoomerangProjectile.State` (Outbound/Returning/Done); `JourneyController.Stage` (7 forward-only stages) | explicit states, guarded transitions (`if (next <= Stage) return;`) |
| **Data-driven design** | `GameConfig` + config.json (ADR-0008); `SpawnConfig` ScriptableObject (ADR-0007) | change behavior/content without code edits |
| **Rejection sampling** | `SpawnManager.TryFindPosition` (ADR-0006) | random candidate, reject if invalid, retry |
| **Dependency injection via Inspector** | every `[SerializeField] private X` field | references are wired in the editor, not found by name/tag at runtime |
| **Fail loudly** | `Awake` warnings, `WiringValidator`, `[OptionalReference]` (ADR-0009) | a missing link logs/validates instead of silently misbehaving |
| **Template method + callback** | `SetupUtils.SetValue(obj, field, p => ...)` | helper does boilerplate, caller supplies the lambda |

---

## 6. The key architectural decisions (ADRs in one table)

| ADR | Decision | Why (the answer to "why not the obvious thing?") |
|---|---|---|
| 0003 | Input System package + `PlayerControls.inputactions`; one `PlayerInputReader` | legacy `Input.GetKey` is deprecated in Unity 6; bindings in one asset, easy to rebind |
| 0004 | **Dynamic Rigidbody** player, not `CharacterController` | rubric wants gravity, projectile arc and *visible* obstacle pushing: physics gives these for free; costs: tune friction (zero-friction material) |
| 0005 | **Kinematic** Rigidbody + trigger boomerang on a scripted Bézier path | a physical boomerang is unstable and can get lost; scripted = deterministic = "overall stability" marks. Impact on the target is still real physics (`AddForce Impulse`) |
| 0006 | Spawn by **rejection sampling** + footprints + `Physics.CheckBox` + verification pass | guaranteed non-overlap, with proof in the Console |
| 0007 | Minimap = orthographic top-down camera → **RenderTexture** → `RawImage`; icons on a `Minimap` layer; prefabs injected via `SpawnConfig` | readable icons regardless of art; theme can be swapped with no code changes |
| 0008 | `config.json` read by `ConfigLoader`; F5 reloads | TA may interpret "respawn" two ways (new layout vs restore): switch live without rebuilding |
| 0009 | **Idempotent Editor setup scripts** + `WiringValidator` | manual wiring was the slowest, most error-prone step; a missed link (`PlayerMotor.cameraRig`) hid for two sprints → "fail loudly" rule |
| 0010 | Per-prefab hit reaction `Tumble` / `FloatAway`; `M_TargetFade` material | ghosts float up and fade, still "pushed away, visible 2 s, then gone"; template material keeps the transparent shader in builds |
| 0011 | Physics authoritative; Animator only *follows* (Speed blend + Jump/Throw triggers) | animation must not break the rubric's gravity/projectile motion |

---

## 7. Subsystems, one paragraph each

### 7.1 Input: `Player/PlayerInputReader.cs`
Wraps the `Player` action map. Two styles: **polled** values (`Move`, `LookDelta`, `Zoom`, `PointerPosition`) read each frame by whoever needs them, and **events** (`JumpPressed`, `SelectPressed`…) raised from `InputAction.performed` callbacks (`Awake` finds actions by name; `OnEnable` hooks and enables the map; `OnDisable` reverses).
Bindings (from `PlayerControls.inputactions`): **W/A/S/D** move (A/D strafe is an extra beyond the spec's W/S), mouse delta look, **RMB** hold = look, **MMB** hold = orbit preview, scroll = zoom, **SPACE** speed, **F** jump, **V** view, **LMB** select, **ESC** quit, **F5** reload config.
`GameplayEnabled == false` (menus) → `Move/Look/...` return zero and gameplay events are suppressed, but **Quit and ReloadConfig still fire**.

### 7.2 Player: `PlayerMotor` (+ `PlayerAnimator`)
Physics-based (ADR-0004). Per `FixedUpdate`: ground check (SphereCast), set horizontal velocity towards `wish * speed` with `MoveTowards`, buffered jump, rotate to the camera's yaw. Details and code in the rubric sheet (cat 3-5). `PlayerAnimator` only reads `motor.HorizontalSpeed` and listens to `Jumped`/`Thrown`.

### 7.3 Camera: `Cameras/CameraRig.cs`
Lives on the Main Camera. **It owns `Yaw`**; `PlayerMotor` reads it, so movement is camera-relative and the player always faces where you look. `LateUpdate` places the camera: first person at `target + eyeOffset`; third person at `pivot + back * distance`, with `ResolveCollision` (SphereCast) pulling the camera in front of walls. RMB-look turns the rig and player; **MMB-orbit** only adds camera-only `orbitYaw/orbitPitch` offsets that ease back (`SmoothDampAngle`), leaving gameplay yaw untouched.

### 7.4 Selection + boomerang: `TargetSelector`, `BoomerangThrower`, `BoomerangProjectile`
- `TargetSelector.Evaluate` = **decide without acting** (ray → target? hit already? in range? cooldown?) and returns a `SelectionResult` enum. `Select` = Evaluate + act; hover preview = Evaluate + draw. One decision path for click and preview.
- `BoomerangThrower.Throw` checks `IsReady`, stamps `lastThrowTime`, `Instantiate`s a boomerang, calls `Launch`, raises `Thrown`. It is also the *event hub*: the projectile calls `owner.ReportTargetHit/ReportCaught`, which the thrower re-raises for audio/animation/HUD.
- `BoomerangProjectile` = the state machine (§5).

### 7.5 Interaction objects
- `Target`: `Hit(impulse, point)` → once-only (`IsHit`), push, raise `AnyHit`, `Destroy(gameObject, 2f)`; hit reaction per prefab.
- `Obstacle`: `Bump()` small impulse; never destroyed by the boomerang.
- `Collectible`: trigger; `OnTriggerEnter` → `AnyCollected` → `Destroy`.
- `GuardianPlatform`: trigger volume → `SpawnManager.HideAll()/Respawn()`.
- Idle visuals (`IdleWander`, `Collectible.Update`) move only a child *visual*, so physics/picking colliders at the root are unaffected.

### 7.6 Spawning: `Spawning/SpawnManager.cs` + `SpawnConfig`
`SpawnConfig` (ScriptableObject asset in `Settings/`) lists the prefab arrays. `SpawnAll()` → `DestroyAll()` → three `SpawnGroup<T>` calls (targets, obstacles, collectibles) → `CountOverlappingPairs()` validation → `Spawned` event. Spawned objects are parented under `Gameplay/Spawned/<Group>` and named `Target_Ghost_001…`. Detail: rubric sheet cat 9-10.

### 7.7 Score and UI
- `ScoreManager` subscribes to the two static events; `Score`, `TargetsHit`, `CollectiblesCollected`; raises `ScoreChanged(total, added)`. Points from config (10 per target, 5 per gem).
- `HudController` polls state in `Update` (score text, targets remaining, speed/view status, cooldown bar from `thrower.CooldownProgress`) and shows timed messages (`messageTime` stamp + alpha fade). It subscribes to selector/score/platform for message events.
- `JourneyController` = the seven Hero's-Journey stages (`enum JourneyStage`), advanced by events (first throw, first hit, jump + fast + both views + N hits, platform entry, back in the world with M hits). It also owns the intro/end panels and pausing: `SetGameplayPaused` = `input.SetGameplayEnabled(!paused)` + `Time.timeScale`.
- `MinimapCamera` + `MinimapIcon`: ADR-0007. Icons hold a fixed world height (draw order) and stay level when their parent tumbles.

### 7.8 Audio: `Audio/AudioManager.cs`
Pure listener. `Awake` builds each SFX from the assigned clip **or a synthesized placeholder tone** (`PlaceholderTones`), so the game is never silent. `OnEnable/OnDisable` subscribe/unsubscribe; each handler is one line (`Play(clip)` → `AudioSource.PlayOneShot`). Real clips live in `Assets/_Project/Audio/` (throw, hit, invalid, platform enter/leave + BGM).

### 7.9 Configuration: `Core/ConfigLoader.cs`, `GameConfig.cs`
`[DefaultExecutionOrder(-100)]` so it loads first. Lookup order: next to the built app → `StreamingAssets/config.json` → built-in defaults. `GameConfig.Validate()` clamps to the **rubric limits** (≥100 targets, ≥100 obstacles, total ≤500) and sane gameplay ranges, and logs any change. Consumers use the idiom `ConfigLoader.Current?.field ?? inspectorField`, so the game works with or without a loader. Shipped values: 150 targets, 150 obstacles, 30 collectibles (= **330**), speeds 5/10, 10 pts/target, 5 pts/gem, throw range 40 m, boomerang 20 m/s, cooldown 3 s, `respawnMode: Regenerate`.

### 7.10 Editor tooling (not part of the game)
Menu **Boomerang Guardian → Setup → …** (`Sprint2/3/5Setup`, `AudioSetup`, `PlayerCharacterSetup`) creates/links prefabs, managers and HUD **idempotently** (find-by-name, create only what's missing). **Validate Wiring** lists unassigned references except those tagged `[OptionalReference]`. **Build → macOS** (`BuildScript`) produces `Builds/macOS/*.app` + an editable `config.json`.

---

## 8. Cross-cutting rules (state them as "team conventions")

- One MonoBehaviour per file; file name = class name.
- `[SerializeField] private` + `[Tooltip]`; avoid public fields.
- **Identify by component, not tag**: `GetComponentInParent<Target>()`.
- **Theme-agnostic**: gameplay code never names art assets; swap prefabs via `SpawnConfig`/Inspector.
- **Prefab convention**: root pivot at ground contact (bottom centre); visuals in a `Model` child; root has Rigidbody/collider/script.
- Unity 6 API: `Rigidbody.linearVelocity`, `FindFirstObjectByType`, Input System (no `Input.GetKey`).
- Every `+=` in `OnEnable` has a `-=` in `OnDisable`; static events get a `ResetStatics`.
- Fail loudly: missing references log a warning/error and the script disables itself or falls back visibly.

---

## 9. Three request traces (use one in the demo)

**A. "Click on a ghost"** `LMB → PlayerInputReader.OnSelect → SelectPressed → TargetSelector.RequestSelect (flag) → Update: not over HUD → Select → Evaluate(Raycast) → Thrown → BoomerangThrower.Throw → Instantiate + Launch → Thrown event (audio, animation, story)`.

**B. "Boomerang hits"** `FixedUpdate: MovePosition along Bézier → OnTriggerEnter (or pathT reaches 1) → HitTarget → Target.Hit (push, IsHit, AnyHit, Destroy in 2 s) → ScoreManager +10 → ScoreChanged → HUD "+10"; owner.ReportTargetHit → hit SFX; state = Returning → fly to hand → Catch → ReportCaught → Destroy`.

**C. "Step on the platform"** `physics step → GuardianPlatform.OnTriggerEnter → SpawnManager.HideAll (SetActive(false) ×330) → PlayerEntered → SFX + HUD message + story advance; leave → OnTriggerExit → Respawn (Regenerate: SpawnAll / Restore: SetActive(true)) → PlayerExited → SFX + message`.

---

## 10. Honest trade-offs to volunteer (TAs like self-awareness)

- **Per-frame HUD polling** (`HudController.Update`) is simple but updates text every frame; fine at this scale (it uses `SetText` with format args to avoid string garbage).
- **`FindFirstObjectByType` fallbacks** exist (`PlayerMotor`, `Collectible`) as safety nets; the intended path is Inspector wiring, and `WiringValidator` flags gaps.
- **Static events** are convenient (any target → score) but are global state, hence `ResetStatics`.
- **Boomerang is scripted, not physical**: stable but not "real" boomerang aerodynamics, a deliberate trade (ADR-0005).
- **Accepted animation glitches** (foot sliding, doubled jump hop, throw not synced to release frame) are documented in ADR-0011.
- Spawn failure is possible in principle (`maxAttemptsPerObject = 60`); it is counted and logged (`Failed placements: n`), and the shipped run logs 0.

---

## 11. Likely TA questions → short answers

1. **Where is the game loop?** Unity's; we implement `Update/FixedUpdate/LateUpdate` per script (sheet 2).
2. **How do systems communicate?** C# events; publishers (thrower, platform, `Target.AnyHit`) don't know listeners (score, HUD, audio, story).
3. **Why a single input reader?** One place for bindings and one switch to mute gameplay during menus.
4. **Why is the boomerang kinematic?** Deterministic and cannot get lost; impact force is still applied by physics (ADR-0005).
5. **Why config.json?** The "respawn" wording is ambiguous; the TA can choose `Regenerate` or `Restore`, and F5 reloads live (ADR-0008).
6. **How would you switch the theme to robots?** Replace prefabs in `SpawnConfig` (and models in the prefabs); no gameplay code changes.
7. **How do you prevent a missing Inspector link from going unnoticed?** Warn-and-fallback in `Awake`, `[OptionalReference]` + `WiringValidator` (ADR-0009).
8. **Who owns yaw?** `CameraRig`; the player reads it. MMB orbit offsets don't touch it.
9. **How does the minimap work?** Second orthographic camera, only sees the `Minimap` layer's flat icons, renders into `RT_Minimap`, shown by a `RawImage` in the upper right.
10. **What did AI do vs you?** See `docs/ai-log/prompt-log.md` and the report; the Editor scripts and docs were AI-written, art/scene/sound choices were the PO's.

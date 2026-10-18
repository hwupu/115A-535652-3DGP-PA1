# Cheat Sheet 2 — Unity event-function lifecycle, with this project's code

Extends §1.2 of the walkthrough. Companion: [01-csharp-cheatsheet.md](01-csharp-cheatsheet.md).
Paths are relative to `Assets/_Project/Scripts/`. All snippets are real unless marked *illustrative*.

---

## 1. The big idea

You don't write `main()` or a game loop. **Unity owns the loop and calls your methods** ("event functions" / "messages") by *name*. Define a method with the right name and signature and Unity finds it; no `override`, no registration.

```
Python/TS equivalent:  framework.on("update", handler)     Unity: just name the method Update()
```
Event functions can be `private` (the repo's convention) and cost nothing if you don't define them.

---

## 2. Master timeline

```
══ OBJECT BIRTH (scene load, or Instantiate) ══════════════════════════════════
  Awake()        once per object, immediately, even if the component is disabled*
  OnEnable()     each time the component becomes enabled (right after Awake the 1st time)
  Start()        once, just before this object's FIRST Update/FixedUpdate
 (* Awake runs if the GameObject is active; a disabled *component* still gets Awake but no OnEnable/Start)

══ EACH FRAME ═════════════════════════════════════════════════════════════════
  ┌ FixedUpdate()          0..N times, fixed step (0.02 s default). Physics code.
  │   └ physics simulation runs ──▶ OnTriggerEnter/Stay/Exit, OnCollisionEnter/Stay/Exit
  │ (Input System processes the frame's input → its callbacks fire)
  │ Update()               once per frame, Time.deltaTime varies. Input, timers, visuals.
  │ LateUpdate()           once per frame, after ALL Update()s. Cameras, followers, UI that tracks objects.
  │ (rendering; OnDrawGizmos* in the Editor)
  └ end of frame: pending Destroy() calls are executed

══ DEATH ══════════════════════════════════════════════════════════════════════
  OnDisable()    component disabled, GameObject SetActive(false), or about to be destroyed
  OnDestroy()    object is being destroyed (also when the scene unloads)
  OnApplicationQuit()  (not used here)
```

Two timelines run **independently**: *frames* (variable rate, e.g. 60–144 Hz) and *physics steps* (fixed rate, 50 Hz). A slow frame may run `FixedUpdate` several times; a fast frame may run it zero times. That is why the rule is:

| Do this in… | Because |
|---|---|
| `FixedUpdate` | anything touching a `Rigidbody` (forces, velocity, `MovePosition`): physics is stepped at the fixed rate |
| `Update` | polling input, timers, cosmetic motion, UI text |
| `LateUpdate` | things that must follow the *final* position this frame (cameras) |

---

## 3. One method at a time, with the project's code

### 3.1 `Awake()`: "set up *myself*"

Runs once, as soon as the object exists. Grab your own components and set initial state. **Don't assume other objects are initialised yet** (the order between different objects' `Awake` is not defined, see §4).

```csharp
// Player/PlayerMotor.cs:82
private void Awake()
{
    body = GetComponent<Rigidbody>();            // cache: GetComponent is slowish, so never call it per frame
    capsule = GetComponent<CapsuleCollider>();
    body.freezeRotation = true;                  // we steer rotation ourselves
    body.interpolation = RigidbodyInterpolation.Interpolate;
    ...
    if (cameraRig == null)                       // "fail loudly" fallback when the Inspector link is missing
        cameraRig = FindFirstObjectByType<CameraRig>();
}
```
Other uses: `BoomerangProjectile.Awake` makes itself kinematic and every collider a trigger (`:71-78`); `Collectible.Awake` forces `isTrigger` and randomizes the bob phase; `PlayerInputReader.Awake` looks up the input actions and calls `enabled = false` if the asset is missing (`:55-62`).

### 3.2 `OnEnable()` / `OnDisable()`: "connect / disconnect from the outside world"

The **pair** for event subscriptions. Fires every time the component is enabled/disabled, including `SetActive(false/true)`. Mirror every `+=` with a `-=`.

```csharp
// Player/PlayerMotor.cs:117-131
private void OnEnable()
{
    ConfigLoader.Changed += ApplyConfig;
    if (input == null) return;
    input.ToggleSpeedPressed += ToggleSpeed;
    input.JumpPressed += RequestJump;
}
private void OnDisable()
{
    ConfigLoader.Changed -= ApplyConfig;
    if (input == null) return;
    input.ToggleSpeedPressed -= ToggleSpeed;
    input.JumpPressed -= RequestJump;
}
```
Same shape in `ScoreManager` (`Target.AnyHit`, `Collectible.AnyCollected`), `HudController`, `AudioManager`, `CameraRig`, `ApplicationController`, `PlayerAnimator`, `MinimapCamera` (which hooks `RenderPipelineManager.beginCameraRendering`).

Two more things `OnEnable/OnDisable` are used for here:
- `PlayerInputReader.OnEnable` calls `map.Enable()`; `OnDisable` calls `map.Disable()` and unhooks the callbacks (`:79-102`). The input map only listens while the component is alive.
- `CameraRig.OnDisable` calls `SetCursorLocked(false)`, so the mouse cursor is never left hidden/locked if the rig is switched off (`:120-124`).

> `HideAll()` on the platform uses `SetActive(false)`, so every spawned `Target`/`Obstacle`/`Collectible` receives `OnDisable`; `Respawn(Restore)` uses `SetActive(true)`, so they receive `OnEnable` again. They do **not** get a new `Awake`/`Start`; those run once only. (Regenerate mode destroys and re-instantiates instead, so the new copies get the full birth sequence.)

### 3.3 `Start()`: "set up *using other objects*"

Runs once, before the first frame the object updates; by then **every** object's `Awake` and `OnEnable` have run. Use it to read other objects' data.

```csharp
// Player/PlayerMotor.cs:133
private void Start()
{
    if (ConfigLoader.Current != null) ApplyConfig(ConfigLoader.Current);
}
```
Why not in `Awake`? Because `ConfigLoader.Awake` must have run first. This is also why `Start` pulls `Current` instead of waiting for `Changed`: the first `Changed` event fired **before** this script's `OnEnable` subscribed, so it was missed.

```csharp
// Spawning/SpawnManager.cs:103
private void Start() => SpawnAll();        // reads ConfigLoader.Current (counts, seed) → config must exist already
// Cameras/CameraRig.cs:101
private void Start() { ... yaw = target.eulerAngles.y; SetMode(startMode); }   // needs the player's rotation
```

Rule of thumb: **`Awake` = my own stuff, `Start` = stuff that depends on others.**

### 3.4 `FixedUpdate()`: physics tick

```csharp
// Player/PlayerMotor.cs:144
private void FixedUpdate()
{
    IsGrounded = Time.time >= groundLockUntil && CheckGrounded();   // SphereCast down
    Quaternion yaw = Quaternion.Euler(0f, cameraRig.Yaw, 0f);
    Move(yaw);                      // body.linearVelocity = ...  (acceleration × Time.fixedDeltaTime)
    TryJump();
    body.MoveRotation(yaw);
    if (body.position.y < fallResetY) ResetToStart();
}
```
- Inside `FixedUpdate`, `Time.deltaTime` already returns the fixed step, but this project writes `Time.fixedDeltaTime` explicitly for clarity (`PlayerMotor.cs:165`).
- `BoomerangProjectile.FixedUpdate` advances the Bézier parameter and calls `body.MovePosition(next)` (`:112-151`). It's a *kinematic* body: you move it by script and physics still reports its trigger contacts.
- `Target.FixedUpdate` applies the upward `ForceMode.Acceleration` for the *FloatAway* reaction (`Target.cs:116`).

**Input vs physics split** (the classic pitfall): input arrives per *frame*; physics runs per *step*. `PlayerMotor` solves it by *latching*: the `JumpPressed` event just stores `jumpRequestTime = Time.time` (`RequestJump`, `:222`), and `FixedUpdate → TryJump` consumes it within a 0.15 s buffer. If `TryJump` read the key directly in `FixedUpdate`, a press could fall between two steps and be lost.

### 3.5 `Update()`: per-frame logic

```csharp
// Interaction/TargetSelector.cs:60-71  (input event latched, then handled in Update)
private void RequestSelect() => selectRequested = true;     // called from input processing
private void Update()
{
    if (!selectRequested) return;
    selectRequested = false;
    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;  // clicked the HUD
    Select(input.PointerPosition);
}
```
The comment in that file says why: the UI "is the pointer over a HUD element?" test is only reliable in `Update`.

Other `Update` uses: `HudController` writes score/status text each frame (`:73`); `IdleWander` and `Collectible` animate visuals with `Time.time` / `Time.deltaTime` (`IdleWander.cs:44`, `Collectible.cs:76`); `Target.Update` computes the 2-second fade from `Time.time - hitTime` (`Target.cs:121`); `PlayerAnimator.Update` feeds `motor.HorizontalSpeed` to the Animator (`:37`); `BoomerangProjectile.Update` spins the model (`:105`).

`Time.deltaTime` = seconds since the last frame. Continuous motion = `speed * Time.deltaTime`. **Not** for mouse deltas (already per-frame): `CameraRig.UpdateLook`, `// Mouse delta is already per-frame (pixels), so no Time.deltaTime here`.

### 3.6 `LateUpdate()`: "after everyone has moved"

```csharp
// Cameras/CameraRig.cs:126
private void LateUpdate()
{
    UpdateLook(); UpdateZoom();
    Quaternion rotation = Quaternion.Euler(pitch + orbitPitch, yaw + orbitYaw, 0f);
    ...
    transform.SetPositionAndRotation(pivot + back * distance, rotation);
}
```
Camera follows the player. If this ran in `Update`, the player might not have moved yet for that frame → 1-frame lag/jitter. Also used by `MinimapCamera` (follows the player), `MinimapIcon`, and `TrajectoryPreview.LateUpdate` (preview built after the camera has settled, so the ray matches what's on screen).

### 3.7 Physics callbacks: `OnTriggerEnter` / `OnTriggerExit`

Run **right after the physics step** that detected the contact. They need: at least one collider with `Is Trigger`, and at least one of the two objects having a `Rigidbody`.

```csharp
// Interaction/GuardianPlatform.cs:38-61
private void OnTriggerEnter(Collider other)
{
    if (!IsPlayer(other)) return;
    playerColliderCount++;
    if (playerColliderCount != 1) return;
    if (spawnManager != null) spawnManager.HideAll();   // world disappears
    PlayerEntered?.Invoke();                            // HUD message + SFX listen to this
}
private void OnTriggerExit(Collider other) { ... spawnManager.Respawn(); PlayerExited?.Invoke(); }
```
```csharp
// Interaction/Collectible.cs:103
private void OnTriggerEnter(Collider other)
{
    if (collected) return;                                // guard against double-fire
    Rigidbody body = other.attachedRigidbody;
    if (body == null || body.GetComponent<PlayerMotor>() == null) return;   // only the player
    collected = true;
    AnyCollected?.Invoke(this);
    Destroy(gameObject);
}
```
```csharp
// Boomerang/BoomerangProjectile.cs:153
private void OnTriggerEnter(Collider other)
{
    if (state != State.Outbound) return;                  // ignore everything on the way back
    ...
    Target hitTarget = other.GetComponentInParent<Target>();
    if (hitTarget != null) { if (!hitTarget.IsHit) HitTarget(hitTarget, other.ClosestPoint(body.position)); return; }
    Obstacle obstacle = other.GetComponentInParent<Obstacle>(); ...
}
```
Idioms worth knowing:
- `other.attachedRigidbody` finds the Rigidbody owning that collider (a player may have several child colliders); `GetComponentInParent<T>()` climbs from a child collider to the component on the root. This is how objects are **identified by component, not by tag**.
- The `playerColliderCount` in `GuardianPlatform` handles several player colliders entering/exiting, so the platform doesn't fire twice.
- `OnCollisionEnter` is the non-trigger sibling (a solid hit). This project does not need it: the player pushes obstacles purely through physics, with no script.

### 3.8 `OnDestroy()`: "clean up what I created"

```csharp
// Interaction/Target.cs:173
private void OnDestroy()
{
    foreach (Material m in fadeMaterials) Destroy(m);   // runtime material copies would otherwise leak
}
// Core/ConfigLoader.cs:51
private void OnDestroy() { Current = null; }            // don't leave a stale static behind
```
Order on destruction: `OnDisable` → `OnDestroy`.

### 3.9 Editor-only: `OnDrawGizmosSelected`

`SpawnManager.OnDrawGizmosSelected` (`:367`) draws the spawn region in the Scene view when the object is selected; it doesn't run in a build.

### 3.10 Two *static* hooks that run before any scene

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void ResetStatics() => AnyHit = null;     // Target.cs:69
```
Runs once at startup, before scene objects exist. It clears static events/fields: when the Editor has "Enter Play Mode Options → skip domain reload", statics would otherwise keep last session's subscribers. `Collectible` and `ConfigLoader` have the same hook.

---

## 4. Ordering rules & gotchas

| Rule | Consequence in this project |
|---|---|
| Between **different** objects, `Awake`/`OnEnable`/`Start` order is **undefined** unless you set it. | `[DefaultExecutionOrder(-100)]` on `ConfigLoader` (`:18`) makes its `Awake` (which loads config.json) run **before** everyone else's, so `ConfigLoader.Current` is non-null when other scripts' `Awake/Start` read it. |
| `Awake` of **all** objects → then `OnEnable` ↔ `Awake` interleave per object → then `Start` of all. | Cross-object lookups belong in `Start` (`SpawnManager.Start`, `CameraRig.Start`). |
| `Instantiate(prefab)` runs `Awake` + `OnEnable` **immediately, before it returns**; `Start` runs later (before the new object's first `Update`). | `BoomerangThrower.Throw` does `Instantiate(...)` then `boomerang.Launch(...)` two lines later (`:54-56`). It's safe because `Awake` already ran (body is kinematic, `body` is cached). If `Launch` relied on `Start`, it would be too early. |
| `Destroy(obj)` is **deferred to the end of the frame**; `Destroy(obj, 2f)` after 2 s. | `Target.Hit` → `Destroy(gameObject, disappearDelay)` is the spec's "disappears after 2 s". `SpawnManager.DestroyAll` deactivates objects *first*: inactive colliders leave the physics world immediately, so a stale collider can't block the new random layout (comment at `SpawnManager.cs:344`). |
| `SetActive(false)` → `OnDisable` now; the object's `Update`/`FixedUpdate`/triggers stop. `SetActive(true)` → `OnEnable`, **no** new `Awake/Start`. | `HideAll` / `Respawn(Restore)` on the platform. |
| `enabled = false` on a *component* → `OnDisable` + its `Update`/`FixedUpdate` stop; `Awake`/`OnDestroy` still happen. | "fail loudly and stop": `PlayerInputReader.Awake`, `IdleWander.Awake`, `CameraRig.Start`. |
| `Time.timeScale = 0` (pause): `FixedUpdate` stops; `Update` and `LateUpdate` **still run**; `Time.time` and `deltaTime` freeze (`unscaledDeltaTime` doesn't). | Menus pause with `timeScale = 0` **plus** `PlayerInputReader.SetGameplayEnabled(false)`, because `Update`-based input handling would otherwise keep reacting. ESC and F5 still work. |
| An event fired **before** you subscribe is gone. | `PlayerMotor.Start` re-reads `ConfigLoader.Current` instead of relying on `Changed`. |
| Never `new MyBehaviour()`; use `AddComponent<T>()` or a prefab + `Instantiate`. | |
| `GetComponent` / `Find*` are slow: call once (`Awake`/`Start`), cache in a field. | `PlayerMotor`, `Target`, `BoomerangProjectile` all cache in `Awake`. `Collectible.Update` does a lazy one-time `FindFirstObjectByType` and caches it in a static (`:89-94`). |

---

## 5. Walkthroughs: the lifecycle in action

### 5.1 Game start (what runs, in order)

1. `ResetStatics()` hooks clear static events (`Target`, `Collectible`, `ConfigLoader`).
2. All `Awake`s. `ConfigLoader.Awake` goes first (execution order −100): it parses config.json, sets `Current`, raises `Changed`. Others (`PlayerMotor`, `CameraRig`, `PlayerInputReader`, `AudioManager`…) cache their components.
3. All `OnEnable`s: scripts subscribe to events (`ScoreManager` → `Target.AnyHit`; `AudioManager` → `thrower.Thrown`; …); `PlayerInputReader` enables the input map.
4. All `Start`s: `PlayerMotor` pulls config; `CameraRig` reads the player's yaw; **`SpawnManager.Start → SpawnAll()`** spawns the ~330 objects. Each spawn is an `Instantiate` → its own `Awake`/`OnEnable` run immediately.
5. Frame 1 begins → `FixedUpdate`s/`Update`s/`LateUpdate`s.

### 5.2 One throw, end to end

```
frame N     LMB pressed
  input     PlayerInputReader.OnSelect → SelectPressed event → TargetSelector.RequestSelect (flag)
  Update    TargetSelector.Update → Select() → Evaluate(): Physics.Raycast → Target? in range? ready?
            → BoomerangThrower.Throw(): Instantiate(prefab) [Awake runs now] → Launch() → Thrown event
              ├ AudioManager.OnThrown → throw SFX
              ├ PlayerAnimator.OnThrown → trigger "Throw"
              └ JourneyController.OnThrown → story stage advance
frame N+..  BoomerangProjectile.FixedUpdate (Outbound): MovePosition along the Bézier
            Update: spin visual
  physics   trigger contact → OnTriggerEnter → HitTarget() → Target.Hit()
              ├ AddForceAtPosition (push), Destroy(gameObject, 2f)
              └ Target.AnyHit → ScoreManager (score += 10) → ScoreChanged → HudController "+10"
            owner.ReportTargetHit → BoomerangThrower.TargetHit → AudioManager hit SFX
  Update    Target.Update fades alpha over the 2 s   (floating variant)
            BoomerangProjectile Returning: FixedUpdate flies to the hand → Catch() → Destroy
  +2 s      Unity destroys the target → OnDisable → OnDestroy (frees fade materials)
```

### 5.3 Entering and leaving the platform

```
physics step: player collider overlaps platform trigger
  → GuardianPlatform.OnTriggerEnter → SpawnManager.HideAll(): SetActive(false) on all objects
        (each object: OnDisable; triggers/Update stop)
  → PlayerEntered → AudioManager (enter SFX) + HudController message
...player walks off...
  → GuardianPlatform.OnTriggerExit → SpawnManager.Respawn()
        Regenerate: DestroyAll() + SpawnAll() (new objects: Awake/OnEnable/Start)
        Restore:    SetActive(true) (OnEnable again, no new Awake/Start)
  → PlayerExited → leave SFX + message
```

---

## 6. Quick reference: which method for which job?

| I want to… | Use | Example |
|---|---|---|
| cache my own components | `Awake` | `PlayerMotor.Awake` |
| read another object / config | `Start` (+ `[DefaultExecutionOrder]` if needed) | `PlayerMotor.Start` |
| subscribe/unsubscribe to events | `OnEnable` / `OnDisable` | `ScoreManager` |
| move a Rigidbody, apply forces | `FixedUpdate` | `PlayerMotor`, `BoomerangProjectile` |
| react to a key/click | event handler that sets a flag, consumed in `Update`/`FixedUpdate` | `TargetSelector`, `PlayerMotor` jump buffer |
| animate visuals, run timers, update HUD | `Update` | `HudController`, `Collectible` |
| follow something after it moved | `LateUpdate` | `CameraRig`, `MinimapCamera` |
| detect entering a zone / pickup | `OnTriggerEnter/Exit` | `GuardianPlatform`, `Collectible` |
| free what I allocated | `OnDestroy` | `Target` (materials) |
| reset static state | `[RuntimeInitializeOnLoadMethod]` static method | `Target.ResetStatics` |
| do something after a delay | `Destroy(obj, t)`, a `Time.time` stamp, or a coroutine | `Target.Hit`; see cheat sheet 1 §9 |

---

## 7. Likely TA questions → short answers

1. **Awake vs Start?** `Awake` = my own setup, always first; `Start` = once, after every object's `Awake`, safe to depend on others (`ConfigLoader` → `PlayerMotor.Start`).
2. **Why is the player's movement in `FixedUpdate` but the camera in `LateUpdate`?** Physics runs at a fixed step; the camera must read the player's final position for that frame.
3. **How do you stop `FixedUpdate` from missing a jump press?** The press event latches a timestamp; `FixedUpdate` consumes it within a 0.15 s buffer.
4. **How do you guarantee config is loaded before use?** `[DefaultExecutionOrder(-100)]` on `ConfigLoader`, and consumers read it in `Start`.
5. **What happens to the objects when the player steps on the platform?** `SetActive(false)` → `OnDisable` (no destroy); on exit either `SetActive(true)` or regenerate (destroy + instantiate).
6. **Why does the target stay for 2 s?** `Destroy(gameObject, disappearDelay)` schedules deletion 2 s later; meanwhile `Update` fades it.
7. **Why does the boomerang use a trigger collider and `OnTriggerEnter`?** It's kinematic and scripted; triggers report contacts without physics pushing it. The target push is applied by script with `AddForceAtPosition`.
8. **Why unsubscribe in `OnDisable`?** The publisher keeps a reference to your handler; avoids leaks and calls to disabled/destroyed objects.
9. **What does pausing do?** `Time.timeScale = 0` stops `FixedUpdate` and freezes `Time.time`; `Update` still runs, so gameplay input is disabled via `SetGameplayEnabled(false)`.
10. **Why `ResetStatics`?** Static events/fields persist if Play Mode skips domain reload; reset on startup so no stale subscribers remain.

# Cheat Sheet 6: Likely TA questions and live modifications (with snippets)

This sheet adds to sheet 5 (§4 explanation drills, §5 changes made only through config, §6 rapid-fire Q&A).
Here every answer comes with the actual code. Code is located by **file + method name**, not
line number, so the references don't break when comments are added. Every modification snippet
in Part B was compiled against the real project (see the note at the end).

**Rule of thumb for live edits:** first try config.json + **F5**, then the Inspector. Edit C# only
when the request truly needs new behaviour. Stop Play Mode before you edit. Keep the change small.

---

## Part A: "Explain this" questions

### A1. "How does clicking select a target?" (`TargetSelector.Evaluate`)
The camera casts a ray through the cursor pixel. The first non-trigger collider decides. The target is identified **by component**, not by tag.
```csharp
Ray ray = cameraRig.ViewCamera.ScreenPointToRay(screenPosition);
bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, pickMask, QueryTriggerInteraction.Ignore);
target = hitSomething ? hit.collider.GetComponentInParent<Target>() : null;
```
Checks run in order: not a target → already hit → too far → on cooldown → throw.
*Why `GetComponentInParent`?* The collider may be on a child of the prefab, while `Target` sits on the root.
*Why `QueryTriggerInteraction.Ignore`?* So the trigger volumes of the platform and collectibles don't block the click.

### A2. "How is wrong selection handled?" (`TargetSelector.Select` / `Reject`)
It raises the `SelectionRejected(reason, message)` event. The HUD shows the red message and the AudioManager plays the error sound. The selector doesn't know either of them exists (loose coupling via events).

### A3. "How does the cooldown work?" (`BoomerangThrower`)
It stores a time stamp; there is no timer and no coroutine.
```csharp
public float CooldownRemaining => Mathf.Max(0f, lastThrowTime + Cooldown - Time.time);
public bool IsReady => CooldownRemaining <= 0f;
```
Each throw spawns its own projectile, so the cooldown is the only gate. It uses `Time.time`, so it also pauses when `timeScale = 0`.

### A4. "How does the boomerang fly / return?" (`BoomerangProjectile.FixedUpdate`)
- **Kinematic** Rigidbody moved by `MovePosition` in `FixedUpdate` (ADR-0005). The path is scripted, so it is deterministic and never gets lost.
- **Outbound:** a quadratic Bézier curve from the hand to the target. The control point is the midpoint, pushed right and up.
```csharp
// B(t) = (1-t)²·P0 + 2(1-t)t·P1 + t²·P2
Vector3 right = Vector3.Cross(Vector3.up, toTarget.normalized);
return (start + end) * 0.5f + right * (toTarget.magnitude * curve) + Vector3.up * arcHeight;
```
- **Constant speed:** t is not distance, so each step advances t by `speed·dt / approxLength`.
- **Returning:** `MoveTowards(current, hand, step)` toward the hand position each step, so it homes in even while the player moves. It is caught within `catchRadius`.
- **Safety:** after `maxFlightTime` (8 s) it counts as caught. If the target vanishes (platform) or is already hit, it turns back.

### A5. "Why kinematic and not a normal physics object?"
With real physics it could bounce away or get stuck, and the return path would be unpredictable. Kinematic = **we** control the motion. Physics still reports **trigger** contacts, and the impact on targets is real physics (`AddForceAtPosition` with an impulse).

### A6. "How does the target get pushed and vanish after 2 s?" (`Target.Hit`)
```csharp
if (IsHit) return false;            // no double scoring
IsHit = true;
body.AddForceAtPosition(impulse, point, ForceMode.Impulse);   // off-center → also spins
AnyHit?.Invoke(this);               // ScoreManager + JourneyController listen
Destroy(gameObject, disappearDelay); // 2 s, no coroutine needed
```
*Impulse vs Force?* An impulse is an instant change of momentum (a hit). A force acts over time and belongs in FixedUpdate.

### A7. "How is the player moved? Why relative to the camera?" (`PlayerMotor.Move`)
```csharp
Quaternion yaw = Quaternion.Euler(0f, cameraRig.Yaw, 0f);
Vector3 wish = yaw * new Vector3(moveInput.x, 0f, moveInput.y);   // rotate input by the camera's yaw
horizontal = Vector3.MoveTowards(horizontal, wish * CurrentSpeed, acceleration * Time.fixedDeltaTime);
body.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);   // keep Y for gravity / jump
```
Only **yaw** is used, so looking up and down doesn't tilt the movement. `MoveTowards` = constant acceleration, so movement is smooth and never overshoots.

### A8. "Explain the jump and projectile motion." (`PlayerMotor.TryJump`)
`v = √(2gh)` is the launch speed that reaches `jumpHeight`. Only Y is set; gravity bends it into a parabola. In the air `airAcceleration = 0`, so horizontal velocity is constant, which is textbook projectile motion.
```csharp
float launchSpeed = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);
```
*No double jump because:* jumping needs `IsGrounded` (sphere cast down), and a 0.2 s ground lock stops a re-jump while the capsule still touches the floor.

### A9. "How do you check the player is on the ground?" (`PlayerMotor.CheckGrounded`)
A `SphereCastNonAlloc` straight down from the capsule center. It skips the player's own colliders and surfaces steeper than `maxGroundAngle`, using `hit.normal.y >= cos(angle)` (a dot product with up).

### A10. "Explain the camera." (`CameraRig.LateUpdate`)
```csharp
Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
// FP: at the eyes.  TP: step back from a pivot above the player.
transform.SetPositionAndRotation(pivot + (rotation * Vector3.back) * distance, rotation);
```
- **LateUpdate**, so it runs after the player has moved this frame (no jitter).
- **Pitch is clamped** separately for first and third person.
- **Scroll** changes `targetDistance`, and `SmoothDamp` glides toward it.
- **Collision:** a sphere cast pulls the camera in front of walls. It ignores pushable Rigidbodies, so the camera doesn't shake among 300 props.

### A11. "How do you guarantee no overlap at spawn?" (`SpawnManager.TryFindPosition`)
Rejection sampling: random point → four tests → otherwise retry (max 60 attempts).
```csharp
if (IsInside(xz, platform, platformClearRadius + footprint.Radius)) continue;
if (IsInside(xz, player, playerClearRadius + footprint.Radius)) continue;
if (OverlapsPlaced(xz, footprint.Radius)) continue;    // circle vs circle, our own list
if (OverlapsScene(candidate, footprint)) continue;     // Physics.CheckBox vs walls/props
```
- **Footprint radius** = `√(x² + z²)` of the prefab bounds. That circle contains the object at **any** yaw, so a random rotation can't create an overlap.
- **Why our own list?** Freshly instantiated colliders aren't visible to physics queries until the next physics step.
- **Proof:** `CountOverlappingPairs` checks every pair with `Physics.ComputePenetration` and logs `0 overlapping pairs` in the Console.

### A12. "How does the platform hide and respawn everything?" (`GuardianPlatform`)
`OnTriggerEnter` → `spawnManager.HideAll()` (SetActive false). `OnTriggerExit` → `Respawn()`: **Regenerate** builds a new random layout, **Restore** re-activates the old one. A collider **counter** means a player with several colliders enters or exits only once. The player is detected by component:
```csharp
Rigidbody body = other.attachedRigidbody;
return body != null && body.GetComponent<PlayerMotor>() != null;
```

### A13. "Trigger vs collision? Why does OnTriggerEnter fire at all?"
A trigger reports overlaps without blocking. At least one side needs a Rigidbody: the player has a dynamic one, and the boomerang a kinematic one.

### A14. "How does the score update the UI?" (`ScoreManager` → `HudController`)
`Target.AnyHit` / `Collectible.AnyCollected` are **static** events, so the ScoreManager subscribes once for all 300+ spawned objects. `Add()` raises `ScoreChanged`; the HUD's `Update` writes `scoreText.SetText("Score: {0}", score.Score)`.
*Why unsubscribe in OnDisable?* Static events outlive objects. A destroyed listener would still be called and leak.

### A15. "How does the minimap work?"
A second, orthographic camera follows the player from above and renders into a RenderTexture, which the HUD shows in the upper-right corner. Each spawned prefab has a coloured icon on the `Minimap` layer: only the minimap camera renders that layer, and the main camera culls it. Fog is switched off only while the minimap camera renders (URP `beginCameraRendering` / `endCameraRendering`).

### A16. "How do the sounds play?" (`AudioManager`)
It is event-driven: it subscribes to thrower, selector, platform and collectible events, and calls `PlayOneShot` so sounds can overlap. An empty clip slot falls back to a synthesized tone (`PlaceholderTones`).

### A17. "How does ESC quit?" (`ApplicationController.Quit`)
```csharp
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;   // Application.Quit is ignored in the Editor
#else
    Application.Quit();
#endif
```

### A18. "Update vs FixedUpdate vs LateUpdate: where do you use each?"
- **FixedUpdate:** physics (player velocity, boomerang `MovePosition`, the ghost's rising force).
- **Update:** visuals and input follow-up (spin, bob, fade, the HUD, click handling).
- **LateUpdate:** the camera and trajectory preview, after everything has moved.

### A19. "What is config.json for?" (ADR-0008)
Tunables you can change **without rebuilding**: counts, speeds, scores, range, cooldown, respawn mode and seed. `ConfigLoader` loads it in `Awake` at execution order −100, `GameConfig.Validate` clamps values to the rubric limits, and **F5** reloads it at runtime.

### A20. "How did you make sure the scene wiring is correct?"
Idempotent Editor setup scripts (menu **Boomerang Guardian → Setup**) build and wire the scene (ADR-0009). **Validate Wiring** checks every empty reference in the scene and the prefabs. Fields that may legitimately be empty are marked `[OptionalReference]`.

---

## Part B: "Change X and show me" (code snippets)

Each snippet shows **only what changes**. "…" means the existing code stays as is.

### B1. "Add a double jump" (`PlayerMotor`)
```csharp
[Tooltip("Extra jumps allowed in the air.")]
[SerializeField, Min(0)] private int maxAirJumps = 1;
private int airJumpsUsed;

private void TryJump()
{
    if (IsGrounded) airJumpsUsed = 0;                                   // landing refills the air jumps
    bool buffered = Time.time - jumpRequestTime <= jumpBufferTime;
    bool airJump = !IsGrounded && airJumpsUsed < maxAirJumps;
    if (!buffered || (!IsGrounded && !airJump)) return;
    if (airJump) airJumpsUsed++;
    … // the rest unchanged: launchSpeed = √(2gh), set Y velocity, ground lock, Jumped event
}
```
**Say:** setting Y velocity (not adding to it) makes the second jump the same height regardless of falling speed.

### B2. "Let me steer in the air" / "Jump higher"
No code needed: on the `PlayerMotor` Inspector, set `airAcceleration` (e.g. 10) or `jumpHeight`. You can do it during Play Mode.

### B3. "Sprint only while holding a key instead of toggling" (`PlayerMotor`)
Quick version that reads the keyboard directly:
```csharp
using UnityEngine.InputSystem;
…
public float CurrentSpeed => IsFast || (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
    ? fastSpeed : normalSpeed;
```
**Say:** the clean way is a new action in `PlayerControls.inputactions` read through `PlayerInputReader` (see B4). This version is for a quick demo.

### B4. "Add a new key, e.g. R restarts the game" (the proper Input System way)
1. **Editor:** open `Assets/_Project/Settings/PlayerControls.inputactions` → map *Player* → **+** action `Restart` (Button) → binding *Keyboard R* → **Save Asset**.
2. **`PlayerInputReader`:**
```csharp
public event Action RestartPressed;
private InputAction restart;
// Awake:      restart = map.FindAction("Restart", true);
// OnEnable:   restart.performed += OnRestart;
// OnDisable:  restart.performed -= OnRestart;
private void OnRestart(InputAction.CallbackContext _) => RestartPressed?.Invoke();
```
3. **`ApplicationController`:**
```csharp
using UnityEngine.SceneManagement;
…
// OnEnable:  input.RestartPressed += Restart;   OnDisable: input.RestartPressed -= Restart;
private void Restart()
{
    Time.timeScale = 1f;   // in case a panel had paused the game
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}
```
Restarting only the world (without the story/intro) is just `spawnManager.SpawnAll();`.

### B5. "Different targets give different points" (`Target` + `ScoreManager`)
```csharp
// Target.cs
[Tooltip("Points for hitting this target.")]
[SerializeField, Min(0)] private int points = 10;
public int Points => points;

// ScoreManager.cs
private void OnTargetHit(Target target)
{
    TargetsHit++;
    Add(target.Points);   // was: Add(PointsPerTarget)
}
```
Then set `points` per prefab in `_Project/Prefabs`. **Say:** this replaces the config.json `targetScore`.

### B6. "Lose points when the boomerang hits an obstacle" (`ScoreManager`)
```csharp
using BoomerangGuardian.Boomerang;
…
[SerializeField] private BoomerangThrower thrower;      // assign: the Player's thrower
// OnEnable:  if (thrower != null) thrower.ObstacleHit += OnObstacleHit;
// OnDisable: if (thrower != null) thrower.ObstacleHit -= OnObstacleHit;
private void OnObstacleHit(Obstacle _) => Add(-2);
```
The HUD then shows "+-2". To fix that in `HudController.OnScoreChanged`: `ShowMessage(added >= 0 ? $"+{added}" : $"{added}", added >= 0 ? goodColor : errorColor);`

### B7. "Make the boomerang pierce: hit every target on its path" (`BoomerangProjectile.HitTarget`)
```csharp
if (hitTarget.Hit(direction * targetImpulse, point)) owner.ReportTargetHit(hitTarget);
if (hitTarget == target) state = State.Returning;   // was: always return; now only at the selected target
```

### B8. "Make the boomerang fly through obstacles" / "destroy obstacles" (`BoomerangProjectile.OnTriggerEnter`)
In the obstacle branch, delete `state = State.Returning;` to fly through, or add `Destroy(obstacle.gameObject);` to break them. **Say:** the spec asks for a bounce, so the current code returns.

### B9. "Straight throw instead of the curve" / "bigger curve"
No code: on the boomerang prefab set `curve = 0` and `arcHeight = 0` for a straight line, or raise `curve`. The hover preview uses the same values, so it updates too.

### B10. "Targets vanish after 5 s instead of 2"
No code: target prefab → `disappearDelay`. **Say:** the spec says 2 s, so 2 is the shipped value. The ghost fade is computed from the same field, so it stretches automatically.

### B11. "Play a sound when the player jumps" (`AudioManager`)
```csharp
using BoomerangGuardian.Player;
…
[SerializeField] private PlayerMotor motor;                       // Event sources
[SerializeField, OptionalReference] private AudioClip jumpClip;   // Clips
private AudioClip jumpSfx;
// Awake:     jumpSfx = jumpClip != null ? jumpClip : PlaceholderTones.Sweep("Jump", 400f, 600f, 0.1f);
// OnEnable:  if (motor != null) motor.Jumped += OnJumped;
// OnDisable: if (motor != null) motor.Jumped -= OnJumped;
private void OnJumped() => Play(jumpSfx);
```
Then drag the Player into `motor` on the AudioManager. **Say:** gameplay code didn't change at all; the motor already raises `Jumped`.

### B12. "Invert the mouse Y axis" (`CameraRig.UpdateLook`)
```csharp
pitch += delta.y * lookSensitivity;   // was -=
```
Look *speed* is the Inspector field `lookSensitivity`.

### B13. "Change camera limits / distance / FOV"
No code: `CameraRig` → `thirdPersonPitchLimits`, `distanceLimits`, `startDistance`, `eyeOffset`. FOV is on the Main Camera's `Camera` component (Field of View).

### B14. "Spawn in a circle instead of a rectangle" (`SpawnManager.TryFindPosition`)
```csharp
float radius = Mathf.Min(regionSize.x, regionSize.y) * 0.5f - wallMargin - footprint.Radius;
Vector2 p = Random.insideUnitCircle * radius;          // uniform point inside a disc
Vector3 candidate = regionCenter + new Vector3(p.x, 0f, p.y);
```
Replace the `candidate` line inside the loop with these. **Say:** the four rejection tests stay the same, so the no-overlap guarantee holds.

### B15. "Spawn more than 300 targets"
config.json `targetCount`, but `GameConfig.Validate` clamps it with `Mathf.Clamp(targetCount, MinTargets, 300)`, and the total is capped at `MaxTotalObjects = 500` (spec: 200 to 500 objects, at least 100 targets and 100 obstacles). Raise the clamp only if the TA asks. **Say:** the limits come from the rubric.

### B16. "The platform should hide only the targets" (`SpawnManager.SetAllActive`)
```csharp
private void SetAllActive(bool active)
{
    foreach (Target t in targets)
        if (t != null) t.gameObject.SetActive(active);   // was: AllSpawned()
}
```

### B17. "Give points for standing on the platform" / "react when entering"
`GuardianPlatform` already raises `PlayerEntered` / `PlayerExited`. Subscribe to them from any script (same pattern as B6 / B11). No change to the platform itself.

### B18. "Show how many targets are left" / "Show a timer"
Remaining targets is already on the HUD (`SpawnManager.TargetsRemaining`). A timer in `HudController.Update` with an assigned `TMP_Text timerText`:
```csharp
if (timerText != null) timerText.SetText("Time {0:0}", Time.timeSinceLevelLoad);
```

### B19. "Make the player push obstacles harder / lighter"
No code: the obstacle prefab's Rigidbody `Mass` (lighter = pushed further). The boomerang's push is `obstacleImpulse` / `targetImpulse` on the boomerang prefab.

### B20. "Highlight the target under the cursor"
`TrajectoryPreview` already calls `TargetSelector.Evaluate` every frame. Reuse it:
```csharp
SelectionResult r = selector.Evaluate(input.PointerPosition, out Target t, out _, out _, out _);
// if (r == SelectionResult.Thrown) → tint t (e.g. MaterialPropertyBlock, as Target.ApplyTint does)
```

---

## Part C: Tricky "what if" questions (short answers)

| Question | Answer |
|---|---|
| Click a target while the boomerang is flying? | It is allowed if the cooldown is over; each throw is its own projectile. |
| The selected target is hit by another boomerang first? | `target.IsHit` → this boomerang turns back (`FixedUpdate`, outbound check). |
| The player steps on the platform mid-throw? | The targets become inactive → `!target.isActiveAndEnabled` → the boomerang returns. |
| The boomerang passes through a non-selected target? | That target is also hit (`OnTriggerEnter`: any un-hit target counts, ADR-0005 amendment). |
| Can the boomerang miss? | No. If no trigger fires, reaching t = 1 counts as a hit (prevents tunnelling). |
| What if it never comes back? | After `maxFlightTime` it is counted as caught and destroyed. |
| Why does the player not fly when pushing 300 props? | The props are separate dynamic bodies; the player sets its own horizontal velocity each step, while collision response pushes the props. |
| Player falls off the world? | Below `fallResetY` (−20 m) it teleports back to the start. |
| Frame-rate independence? | Movement uses `Time.fixedDeltaTime` in FixedUpdate, and spin/bob use `Time.deltaTime`. The mouse delta is already per frame, so it isn't multiplied by deltaTime. |
| Why not tags? | A typo in a tag string fails silently; a component check is type-safe and survives renames. |
| Why `[SerializeField] private` instead of `public`? | It shows in the Inspector but other scripts can't change it, so state stays encapsulated. |
| What is `?.` / `??` in `ConfigLoader.Current?.boomerangSpeed ?? speed`? | If there is no config, use the Inspector value. |
| Garbage / performance? | `NonAlloc` casts with reused buffers, `SetText` with format args, cached shader IDs, and footprints measured once per prefab. The spawn time is logged in the Console. |

---

*Verification:* the Part B snippets B1, B3 to B7, B11, B12, B14, B16 and B18 were applied in a
copy of the project and compiled with Unity 6000.3.26f1 (0 errors). B4 compiles as is, but at runtime
`FindAction("Restart", true)` throws until the action has been added to the input asset (step 1).

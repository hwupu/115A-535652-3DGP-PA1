# Cheat Sheet 1 — C# for a Python / TypeScript programmer

Audience: you (Python 3, TypeScript, a little C/C++), demoing this project to the TA.
Every example is **real code from this repo** unless it is explicitly labelled *illustrative*.
Paths are relative to `Assets/_Project/Scripts/`.

> Companion: [02-unity-lifecycle-cheatsheet.md](02-unity-lifecycle-cheatsheet.md) (what Unity calls, and when).

---

## 0. The 60-second mental model

| | Python | TypeScript | C# |
|---|---|---|---|
| Typing | dynamic | static, structural, erased at runtime | **static, nominal, real at runtime** |
| Memory | GC | GC | **GC** (no `free`; value types live on the stack/inline) |
| Blocks | indentation | `{}` | `{}` and every statement ends with `;` |
| Everything is in a class? | no | no | **yes** (no free functions; use `static` methods) |
| Compile step | none | `tsc` | **Unity compiles all `.cs` files when you save/focus the Editor**; errors show as `error CS####` in the Console |
| Null | `None` | `null` / `undefined` | `null` (only reference types, or `T?`) |

One file ≈ one class; file name = class name (a Unity requirement for `MonoBehaviour`s).

---

## 1. File skeleton

```csharp
using System;                          // import (like `import` / `from x import y`)
using BoomerangGuardian.Core;
using UnityEngine;

namespace BoomerangGuardian.Boomerang  // like a TS module / Python package: avoids name clashes
{
    public class BoomerangThrower : MonoBehaviour   // ": MonoBehaviour" = "extends"
    {
        ...
    }
}
```

- `using X;` imports a namespace (not a single symbol). Unlike Python there is no `import x as y`; the closest is `using Random = UnityEngine.Random;` (`Spawning/SpawnManager.cs:7`, needed because `System.Random` also exists).
- This repo's rule: folder `Cameras` (plural), because a namespace `BoomerangGuardian.Camera` would hide `UnityEngine.Camera`.

---

## 2. Types

### 2.1 Primitives and `var`

| C# | Python | TS |
|---|---|---|
| `int` (32-bit), `float` (32-bit, written `1.5f`), `double`, `bool`, `string` | `int`, `float`, `bool`, `str` | `number`, `boolean`, `string` |

- Unity uses **`float`**, so literals need the `f` suffix: `5f`, `0.15f`. `float x = 0.15;` is a compile error (a `double` doesn't implicitly narrow to `float`).
- `var watch = new Stopwatch();` = type inference at compile time (like TS `const x = ...`). It is **not** dynamic. Used in `SpawnManager.cs:119`.
- `const` = compile-time constant (`public const string FileName = "config.json";`, `Core/ConfigLoader.cs:21`).
- `readonly` = assignable only in the constructor/initializer (`private readonly List<Material> fadeMaterials = new List<Material>();`, `Interaction/Target.cs:65`). The *reference* is fixed; the list's *contents* can still change.
- `static readonly int SpeedId = Animator.StringToHash("Speed");` (`Player/PlayerAnimator.cs:14`) computes a value once per type, not per instance.

### 2.2 Value types vs reference types (the C/C++ person's key idea)

| | `struct` (value) | `class` (reference) |
|---|---|---|
| Assignment | **copies** the data | copies the *pointer* |
| Can be `null` | no (use `T?`) | yes |
| Examples | `int`, `float`, `bool`, `Vector3`, `Quaternion`, `Color`, `RaycastHit` | `string`, `List<T>`, every `MonoBehaviour`, `Rigidbody` |

```csharp
Vector3 direction = FlightDirection;   // a COPY
direction.y = 0f;                      // does not change whatever FlightDirection came from
```
(`Boomerang/BoomerangProjectile.cs:178-179`.) That is why you can write `direction.y = 0f` freely. In TS/Python, an object would be shared.

Own structs in this repo: `private readonly struct Footprint` and `Placement` (`Spawning/SpawnManager.cs:81,88`): small immutable data bundles. `readonly struct` = fields can't change after construction.

### 2.3 Enums

```csharp
public enum SelectionResult { Thrown, NotATarget, TooFar, OnCooldown, AlreadyHit }   // Interaction/TargetSelector.cs:11
```
A closed set of named values (like a TS string-union, but nominal). Used with `switch`:
```csharp
switch (result)
{
    case SelectionResult.AlreadyHit: return result;
    case SelectionResult.Thrown:     thrower.Throw(target); ...; return result;
    default:                         return Reject(result, message, clickedName);
}
```
`private enum State { Outbound, Returning, Done }` (`BoomerangProjectile.cs:18`) is a tiny **state machine**.

### 2.4 Strings

Interpolation (f-string / template literal equivalent):
```csharp
Debug.Log($"Speed mode: {(IsFast ? "FAST" : "NORMAL")} ({CurrentSpeed} m/s)");   // PlayerMotor.cs:218
$"Too far! {distance:0} m (max {MaxThrowRange:0} m)"                               // :0 = format spec, like f"{x:.0f}"
nameof(PlayerMotor)                                                                // the string "PlayerMotor", refactor-safe
```

---

## 3. Members of a class

### 3.1 Fields, properties, methods

```csharp
private Rigidbody body;                              // field (private is the default if you omit it)

public bool IsHit { get; private set; }              // auto-property: public getter, private setter
public float CurrentSpeed => IsFast ? fastSpeed : normalSpeed;   // read-only computed property (no parentheses to call)
```
A **property** looks like a field but runs code, like a Python `@property` or a TS `get x()`. `IsHit { get; private set; }` is the idiom for "readable by anyone, writable only here" (`Target.cs:47`).

Multi-line getter (`Target.cs:50-58`):
```csharp
public Vector3 AimPoint
{
    get
    {
        if (colliders.Length == 0) return transform.position + Vector3.up * 0.5f;
        Bounds bounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++) bounds.Encapsulate(colliders[i].bounds);
        return bounds.center;
    }
}
```

### 3.2 Access modifiers

`public` (anyone) · `private` (this class only) · `internal` (this assembly only; `BoomerangThrower.ReportTargetHit` is `internal` so only code in the same project can call it) · `protected` (subclasses). **Repo rule: avoid public fields**; expose a property or a `[SerializeField] private` field.

### 3.3 Methods, default / optional / `out` parameters

```csharp
private void Play(AudioClip clip, float scale = 1f)          // default parameter (like Python)
public SelectionResult Evaluate(Vector2 pos, out Target target, out Vector3 aimPoint,
                                out string message, out string clickedName, bool drawDebugRay = false)
```
- `out` = the method **must assign** that parameter; it is how C# returns several values (Python would return a tuple).
- Call site: `Evaluate(pos, out Target t, out Vector3 aim, ...)` declares the variables inline; `out _` **discards** one (`TargetSelector.cs:77`, `TrajectoryPreview.cs:61`).
- `void` = returns nothing. Methods that return `bool` for success (`TryJump`, `TryBuildCurve`) follow the `Try…` naming convention.
- `static` = belongs to the type, not an instance (`BoomerangProjectile.Bezier(...)`, `Mathf.Clamp`). Call as `Type.Method()`.

### 3.4 Expression-bodied members (`=>`): **not a lambda**

```csharp
private void RequestSelect() => selectRequested = true;       // identical to { selectRequested = true; }
public float Speed => ConfigLoader.Current?.boomerangSpeed ?? speed;   // read-only property
```
Here `=>` is just shorthand for a one-line method/property body. It looks like a TS arrow function but is *not* an anonymous function. This repo has ~30 of these (`AudioManager.cs:89-96`).

### 3.5 Constructors

Almost never written here. Unity creates `MonoBehaviour`s itself; **never `new` a MonoBehaviour**, and don't use constructors for setup, use `Awake`. Plain classes/structs (`new GameConfig()`, `new List<Material>()`) use `new` as normal.

---

## 4. Null handling (you'll be asked about this)

```csharp
if (input != null) input.ToggleSpeedPressed += ToggleSpeed;        // plain check
JumpPressed?.Invoke();                                             // ?. : call only if not null (like TS ?.)
public float Cooldown => ConfigLoader.Current?.boomerangCooldown ?? cooldown;
//                                  ?.  → null if Current is null (result type becomes float?)
//                                  ??  → "if null, use this instead" (like TS ?? / Python `or` for None)
```

**Unity gotcha:** Unity overloads `==`/`!=` for `UnityEngine.Object` (anything you can drag in the Inspector). A *destroyed* object compares equal to `null` even though the C# reference isn't really null. So `if (target == null || !target.isActiveAndEnabled)` in `BoomerangProjectile.cs:124` correctly detects a target destroyed mid-flight. Consequence: **don't use `?.` / `??` on `UnityEngine.Object`s** (they bypass Unity's check); this repo uses them only on plain C# objects (`GameConfig`, events).

`T?` on value types: `float?` = nullable float.

---

## 5. Collections and loops

| Python | TS | C# |
|---|---|---|
| `list` | `T[]` / `Array` | `List<T>` (growable) or `T[]` (fixed size) |
| `dict` | `Map` | `Dictionary<K,V>` |
| `for x in xs:` | `for (const x of xs)` | `foreach (Target t in targets)` |
| `for i in range(n)` | `for (let i=0;i<n;i++)` | `for (int i = 0; i < n; i++)` |
| generator (`yield`) | `function*` | **iterator method** returning `IEnumerable<T>` (see §8) |

```csharp
private readonly List<Placement> placements = new List<Placement>();
private readonly Dictionary<GameObject, Footprint> footprints = new Dictionary<GameObject, Footprint>();
```
- `IReadOnlyList<T>` = "you may read and index this but not add": `SpawnConfig.TargetPrefabs` hands out a read-only view of its private array (`Spawning/SpawnConfig.cs:22`).
- **Perf note you can mention:** `Physics.SphereCastNonAlloc(..., groundHits, ...)` writes into a preallocated `RaycastHit[8]` instead of allocating a new array every physics step (`PlayerMotor.cs:76,195`). Avoiding garbage in per-frame code is a Unity habit.

---

## 6. Generics, `where`, attributes

```csharp
private int SpawnGroup<T>(IReadOnlyList<T> prefabs, int count, List<T> spawned, string groupName) where T : Component
```
`<T>` = a type parameter (same idea as TS generics); `where T : Component` is a **constraint**: T must derive from `Component` so the body can call `.gameObject`. One method serves `Target`, `Obstacle` and `Collectible` (`SpawnManager.cs:163`). Unity's own `GetComponent<Rigidbody>()` and `FindFirstObjectByType<CameraRig>()` are generic calls.

**Attributes** `[Like(This)]` are metadata attached to a class/field/method (like Python decorators or TS decorators, but they don't wrap behavior; some other system reads them):

| Attribute | Meaning | Where |
|---|---|---|
| `[SerializeField]` | show/save this *private* field in the Inspector | everywhere |
| `[Tooltip("…")]`, `[Header("…")]`, `[Min(0f)]`, `[Range(0f,1f)]` | Inspector polish/validation | `PlayerMotor.cs:25-45` |
| `[RequireComponent(typeof(Rigidbody))]` | adding this script auto-adds the component | `BoomerangProjectile.cs:15` |
| `[DefaultExecutionOrder(-100)]` | run this script's Awake/OnEnable/Start earlier | `ConfigLoader.cs:18` |
| `[RuntimeInitializeOnLoadMethod(...)]` | run this *static* method at startup | `Target.cs:69` |
| `[CreateAssetMenu(...)]` | adds "Create → Boomerang Guardian → Spawn Config" | `SpawnConfig.cs:12` |
| `[OptionalReference]` | our own attribute: "empty is OK, don't warn" | `Core/OptionalReferenceAttribute.cs` |

Writing your own attribute = a class deriving from `Attribute` (here `PropertyAttribute`).

---

## 7. Events, delegates, and `Action` (the project's glue)

### 7.1 Declare / raise / subscribe

```csharp
// Announcer (Boomerang/BoomerangThrower.cs)
public event Action<Target> TargetHit;                       // Action<T> = "a function taking a Target, returning nothing"
internal void ReportTargetHit(Target t) => TargetHit?.Invoke(t);   // raise (?. = only if someone listens)

// Listener (Audio/AudioManager.cs)
thrower.TargetHit += OnTargetHit;                            // subscribe
thrower.TargetHit -= OnTargetHit;                            // unsubscribe
private void OnTargetHit(Target _) => Play(hitTargetSfx);
```

- **Delegate** = a typed function reference. `Action`, `Action<T1,T2>` (no return value) and `Func<..., TResult>` (returns a value) are the built-in delegate types. TS analogy: `(t: Target) => void`; Python: any callable.
- **`event` keyword** = outside code may only `+=` / `-=`; only the declaring class may invoke. (A plain `Action` field would let anyone overwrite or invoke it.) This is like exposing `subscribe/unsubscribe` but not `emit`.
- A delegate holds an **invocation list**; `+=` appends, `-=` removes *the same method*.
- `_` / `__` as parameter names = "unused" (`OnSelectionRejected(SelectionResult _, string __)`).

### 7.2 Static events (global bus)

```csharp
public static event Action<Target> AnyHit;     // Target.cs:45  -- raised by ANY Target instance
```
`ScoreManager` subscribes with `Target.AnyHit += OnTargetHit;` and never needs a reference to a particular target. Static state survives between Play sessions when the Editor skips domain reload, so each static event/field has a reset:
```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void ResetStatics() => AnyHit = null;     // Target.cs:69-70
```

### 7.3 The rule: every `+=` in `OnEnable` has a `-=` in `OnDisable`

Otherwise a destroyed/disabled object keeps receiving calls (leak or `MissingReferenceException`). Every script that subscribes in this repo mirrors it; see `HudController.cs:46-66`. Full lifecycle context in cheat sheet 2.

### 7.4 Event flow in this game (what to say in the demo)

```
Mouse click → PlayerInputReader.SelectPressed → TargetSelector.RequestSelect (sets a flag)
            → Update(): raycast → thrower.Throw(target)
BoomerangProjectile hits → target.Hit() → Target.AnyHit → ScoreManager.OnTargetHit → ScoreChanged
                                                        └→ HudController shows "+10"
                         → owner.ReportTargetHit → BoomerangThrower.TargetHit → AudioManager plays SFX
```
Publisher never knows its subscribers: score, HUD and audio can change independently.

---

## 8. Lambdas, anonymous functions, and iterators

### 8.1 What a lambda is

An **anonymous function** written inline: `(parameters) => expression` or `(parameters) => { statements }`.
Same idea as TS arrow functions and Python `lambda` (but C# lambdas may contain full statement blocks).

| | Python | TypeScript | C# |
|---|---|---|---|
| one-liner | `lambda p: p.x` | `p => p.x` | `p => p.x` |
| with statements | *(not allowed; need `def`)* | `p => { ...; return v; }` | `p => { ...; return v; }` |
| captures outer variables | yes (by reference) | yes | **yes (the variable itself, not a copy)** |

A lambda must be assigned to a **delegate type**: `Action<SerializedProperty>`, `Func<int,bool>`, or a custom one.

### 8.2 The only real lambdas in this repo (Editor setup scripts)

`Editor/SetupUtils.cs:173` takes a function as a parameter:
```csharp
public static void SetValue(Object owner, string field, System.Action<SerializedProperty> assign)
{
    var so = new SerializedObject(owner);
    SerializedProperty prop = so.FindProperty(field);
    if (prop == null) return;
    assign(prop);                       // call the callback the caller supplied
    so.ApplyModifiedProperties();
}
```
and `Editor/Sprint5Setup.cs:198` passes lambdas:
```csharp
SetupUtils.SetValue(preview, "selector",
    p => p.objectReferenceValue = player.GetComponent<TargetSelector>());
//  ^ parameter p   ^ body: runs with the SerializedProperty SetValue found
```
Why a lambda? `SetValue` does the boring open/find/apply steps, and the caller only supplies "what to assign" (the "template method with a callback" pattern). It also **captures** `player` from the enclosing scope.
`Editor/Sprint2Setup.cs:71` passes a whole block-bodied lambda, `root => { ... }`, to configure a prefab as it is created.

These live in `Editor/` (run only inside the Unity Editor, never in the shipped game).

### 8.3 Runtime code uses *method groups* instead of lambdas, on purpose

```csharp
thrower.Thrown += OnThrown;          // method group: passes an existing named method
```
Because `OnDisable` must later write `thrower.Thrown -= OnThrown;`. With an inline lambda, you cannot unsubscribe:

*Illustrative (not in the project): the bug we avoided*
```csharp
void OnEnable()  { thrower.Thrown += _ => Play(throwSfx); }   // anonymous: no name to remove later
void OnDisable() { thrower.Thrown -= _ => Play(throwSfx); }   // does NOT remove it: this is a *different* lambda object
```
That is why the repo has dozens of tiny named handlers (`OnThrown`, `OnCaught`, ...) rather than lambdas.

### 8.4 Closures: capturing variables (illustrative)

```csharp
int hits = 0;
Action onHit = () => hits++;       // the lambda captures the variable `hits`, not its value
onHit(); onHit();                  // hits == 2
```
The classic trap: capturing a `for` loop variable in a lambda that runs later. Capture a local copy (`int j = i;`) inside the loop. (Same trap as JS `var` / Python late-binding closures.)

### 8.5 Iterator methods (`yield return`), C#'s generators

This is the `yield` that **does** appear in the project, and it is **not** a coroutine (§9):
```csharp
// Spawning/SpawnManager.cs:326
private IEnumerable<Component> AllSpawned()
{
    foreach (Target t in targets) yield return t;
    foreach (Obstacle o in obstacles) yield return o;
    foreach (Collectible c in collectibles) yield return c;
}
// used as:  foreach (Component c in AllSpawned()) ...
```
Exactly Python's `def f(): yield ...` / TS's `function*`: **lazy**, produces one item at a time, no temporary list. It lets `SetAllActive` and `DestroyAll` loop over three differently-typed lists as one sequence. Another one: `ConfigLoader.CandidatePaths()` (`Core/ConfigLoader.cs:92`) yields the config-file locations **in priority order**, and `Load()` stops at the first one that parses (`break`).

---

## 9. Coroutines

### 9.1 What it is

A **coroutine** is a Unity method that can **pause itself and resume later** without blocking the game. It is an iterator method (`IEnumerator`, same `yield` mechanism as §8.5) that *Unity* drives: each `yield return X` tells Unity *when* to resume it.

| Unity | Python / TS analogy |
|---|---|
| `IEnumerator` method + `yield return` | generator / `async` function + `await` |
| `StartCoroutine(Routine())` | `asyncio.create_task(...)` / calling an `async` function |
| `yield return null` | `await nextFrame()` (resume next frame) |
| `yield return new WaitForSeconds(2f)` | `await sleep(2)` |
| `yield return new WaitForFixedUpdate()` | wait for the next physics step |
| `StopCoroutine` / object disabled or destroyed | cancel the task |

It is **single-threaded**: the coroutine runs on the main thread inside Unity's frame loop, so it may touch Unity objects safely; there are no race conditions. (Not the same as C# `async/await` or threads.)

Rules:
- Must live on a `MonoBehaviour`; stopped automatically when that object is destroyed **or its GameObject is deactivated**.
- `WaitForSeconds` uses scaled time: with `Time.timeScale = 0` (this project's pause) it never finishes; use `WaitForSecondsRealtime` to wait through a pause.
- Prefer cached `new WaitForSeconds` objects in hot loops (avoids garbage).

### 9.2 Honest status: this project does not use coroutines

`grep StartCoroutine` over the whole repo returns **zero** hits. If the TA asks, say so: the timed behaviors use cheaper tools:

| Need | What the project does instead | Where |
|---|---|---|
| "Disappear after 2 s" | `Destroy(gameObject, disappearDelay)`: Unity's built-in delayed destroy | `Target.cs:96` |
| "Fade out over 2 s" | `Update()` computes `t = (Time.time - hitTime) / disappearDelay` and sets alpha, i.e. **time-stamp + polling** | `Target.cs:121-133` |
| "3 s boomerang cooldown" | store `lastThrowTime`; `IsReady` = `Time.time >= lastThrowTime + Cooldown` | `BoomerangThrower.cs:33-49` |
| "Safety: give up after 8 s" | `if (Time.time - launchTime > maxFlightTime)` in `FixedUpdate` | `BoomerangProjectile.cs:116` |
| "Ignore jump press older than 0.15 s" | `Time.time - jumpRequestTime <= jumpBufferTime` | `PlayerMotor.cs:172` |
| "HUD message fades" | `messageTime` stamp + `UpdateMessageFade()` | `UI/HudController.cs:44,111` |

Pattern: **remember *when* something started (`Time.time`), then compare in `Update`/`FixedUpdate`**. It is simple, has no hidden state, and needs no cancellation handling.

### 9.3 Same behavior written as a coroutine (*illustrative, not in the repo*)

```csharp
public bool Hit(Vector3 impulse, Vector3 point)
{
    ...
    StartCoroutine(FadeAndDie());          // start; returns immediately
    return true;
}

private IEnumerator FadeAndDie()           // note the return type
{
    float t = 0f;
    while (t < disappearDelay)             // runs once per frame...
    {
        t += Time.deltaTime;
        SetAlpha(1f - t / disappearDelay);
        yield return null;                 // ...pause here, resume next frame
    }
    Destroy(gameObject);
}
```
Trade-off: the coroutine reads top-to-bottom like a script (nice for sequences), but state is hidden in the iterator, and it dies silently if the object is deactivated. For one timer, `Destroy(obj, delay)` is simpler still.

---

## 10. Misc syntax you will see

| Snippet | Meaning |
|---|---|
| `Vector3 a = b * 2f + Vector3.up;` | operator overloading (vectors support `+ - *`). `Quaternion * Vector3` rotates the vector (`PlayerMotor.cs:159`). |
| `a ?? b`, `a?.b` | see §4 |
| `x is null`, `x is Foo f` | pattern matching (rare here) |
| `#if UNITY_EDITOR … #else … #endif` | **conditional compilation**: different code in Editor vs build. `Core/ApplicationController.cs:28-32` (Editor: stop Play mode; build: `Application.Quit()`). |
| `try { } catch (Exception e) { }` | exceptions, like Python `try/except` (`ConfigLoader.cs:70-80`) |
| `/// <summary>…</summary>` | XML doc comments (shown in IDE tooltips) |
| `out _` | discard an `out` value |
| `(float)i / segments` | cast. Without the cast, `int / int` is integer division (like Python `//`). `TrajectoryPreview.cs:72` |
| `Debug.Log / LogWarning / LogError` | Console output (≈ `print`) |
| `foreach (Collider own in ownColliders) if (own == other) return true;` | braces optional for a single statement |
| `enabled = false;` | built-in `MonoBehaviour` property: stops Update/FixedUpdate and fires `OnDisable`. Used for "fail loudly then switch myself off" in `PlayerInputReader.cs:60`. |

---

## 11. Likely TA questions → short answers

1. **Why `[SerializeField] private` rather than `public`?** Encapsulation: still editable in the Inspector, not writable from other scripts.
2. **Value vs reference type: why can I do `direction.y = 0f`?** `Vector3` is a struct; you changed your copy.
3. **What does `?.Invoke()` do?** Raises the event only if there are subscribers (avoids a `NullReferenceException`).
4. **Why unsubscribe in `OnDisable`?** The publisher holds a reference to your method; otherwise a dead/disabled object is still called.
5. **What's the difference between `Action` and `event Action`?** `event` restricts outsiders to `+=`/`-=`.
6. **Do you use coroutines?** No: time-stamps + `Update`/`FixedUpdate` and `Destroy(obj, delay)`. I can explain how it would look as a coroutine (§9.3).
7. **Where did you use lambdas?** Editor setup scripts for callbacks (`SetupUtils.SetValue`); runtime code uses named handlers so they can be unsubscribed.
8. **What is `yield return` in `SpawnManager`?** An iterator (lazy sequence), not a coroutine.
9. **Why do you need `ResetStatics`?** Static fields survive when Play Mode skips the domain reload; the reset avoids stale subscribers.
10. **`Time.deltaTime` vs not?** Multiply by `deltaTime` for continuous per-second quantities; don't for mouse deltas, which are already per-frame (`CameraRig.cs:171`).

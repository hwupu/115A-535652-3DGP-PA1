# Sprint 1 — Scene Setup Guide (Unity 6000.3.25f1)

For: PB-01 arena, PB-02/03/04 player, PB-05 camera, PB-11 obstacles, PB-17 ESC.
Time: about 45–60 min. Parts A–C need no scripts, so they can be done any time. Parts D–G use the Sprint 1 scripts.

> Conventions: our own content goes under `Assets/_Project/`. Name every GameObject clearly; the rubric gives marks for "logical hierarchy & naming".

---

## A. Clean up the template (2 min)
1. In the **Project** window, delete `Assets/TutorialInfo/` and `Assets/Readme.asset`.
2. Leave `Assets/InputSystem_Actions.inputactions` alone. It is the template's project-wide asset, and we don't use it. Our map is `Assets/_Project/Settings/PlayerControls.inputactions`.
3. Create the folders `Assets/_Project/Scenes`, `Assets/_Project/Materials` and `Assets/_Project/Prefabs`.

## B. Create the scene (3 min)
1. **File → New Scene → Basic (URP)** → **Create**.
2. **File → Save As** → `Assets/_Project/Scenes/Main.unity`.
3. **File → Build Profiles** → **Scene List** → **Add Open Scenes**. Remove `SampleScene` from the list, and then delete `Assets/Scenes/SampleScene.unity`.

## C. Build the greybox arena (PB-01, ~15 min)
Create empty GameObjects as **group folders** (right-click in the Hierarchy → **Create Empty**, then **Reset** the Transform). Target hierarchy:

```
Main
├── Environment
│   ├── Lighting            (move Directional Light + Global Volume here)
│   ├── Ground
│   ├── Boundary
│   │   ├── Wall_North
│   │   ├── Wall_South
│   │   ├── Wall_East
│   │   └── Wall_West
│   └── GuardianPlatform
├── Gameplay
│   ├── Player
│   └── Obstacles_Test
│       └── Obstacle_Test_01 … 05
├── Cameras
│   └── Main Camera
└── Systems
    └── GameManager
```

**Ground**: 3D Object → **Plane**, named `Ground`, position (0, 0, 0), scale **(10, 1, 10)**. A Plane is 10 × 10 m, so the ground is **100 × 100 m**.

**Walls** (3D Object → **Cube** each; cubes as required by the spec):

| Name | Position | Scale |
|---|---|---|
| Wall_North | (0, 2.5, 50.5) | (102, 5, 1) |
| Wall_South | (0, 2.5, -50.5) | (102, 5, 1) |
| Wall_East | (50.5, 2.5, 0) | (1, 5, 100) |
| Wall_West | (-50.5, 2.5, 0) | (1, 5, 100) |

**GuardianPlatform**: 3D Object → **Cylinder**, position (0, 0.05, 0), scale **(8, 0.05, 8)**. That makes an 8 m disc, 0.1 m tall, low enough for the player to walk onto.
- The Cylinder primitive comes with a **Capsule Collider**, which has the wrong shape. Remove it, then **Add Component → Mesh Collider** and tick **Convex**.

**Materials** (optional but recommended): in `Assets/_Project/Materials`, Create → **Material** (URP Lit). Make `M_Ground` (green), `M_Wall` (grey), `M_Platform` (blue/cyan), `M_Player` (orange) and `M_Obstacle` (brown). Drag each onto its objects. These are placeholders; real assets come in Sprint 3.

✅ Check: press Play. You should see a closed 100 × 100 m arena with a disc in the middle.

## D. Player (PB-02/03/04, ~10 min)
1. **Tags & Layers**: Edit → Project Settings → **Tags and Layers**. Add a layer `Player` (User Layer 6, for example). We'll need it in Sprint 2 to keep ray casts and spawns off the player.
2. 3D Object → **Capsule**, named `Player`, under `Gameplay`. Position **(0, 1, -40)**, the south side of the arena facing north. Set the **Tag** to `Player` and the **Layer** to `Player`.
3. Add Component → **Rigidbody**:
   - Mass **70**
   - Interpolate and Collision Detection: no need to change; `PlayerMotor` sets them in code.
4. Add Component → **Player Input Reader**. Set **Actions** to `Assets/_Project/Settings/PlayerControls`.
5. Add Component → **Player Motor**. Set **Input** to the `Player` object itself (drag `Player` into the field). Leave **Camera Rig** empty for now; you'll fill it in step E.
6. Optional: make a small child cube `Nose` at (0, 0.5, 0.5), scale (0.3, 0.2, 0.3), and **remove its Box Collider**. It shows which way the player faces. Mark it shadows-only in FP via E-4.

## E. Camera (PB-05, ~5 min)
1. Move `Main Camera` under `Cameras`. Its starting position doesn't matter; the rig positions it.
2. Add Component → **Camera Rig**:
   - **Target** = `Player`
   - **Input** = `Player`
   - **Hide In First Person**: add the Player's **Mesh Renderer** (and `Nose`, if you made it)
3. Go back to `Player` → **Player Motor** → **Camera Rig** = `Main Camera`.

## F. Test obstacles (PB-11, ~5 min)
Under `Gameplay/Obstacles_Test`, make 5 **Cubes** named `Obstacle_Test_01…05`, for example at (-5, 0.5, -30), (0, 0.5, -30), (5, 0.5, -30), (-3, 0.75, -25) with scale 1.5, and (3, 0.5, -25).
- Add Component → **Rigidbody** to each, with **Mass 10**. Leave **Linear Damping** at 0, as friction slows them down.
- Apply `M_Obstacle`.

## G. Game manager (PB-17, ~2 min)
Under `Systems`, create an empty `GameManager` → Add Component → **Application Controller** → set **Input** to `Player`.

**Save the scene** (Cmd+S).

---

## Play-test checklist (Sprint Review demo)
| # | Do this | Expect | Rubric |
|---|---|---|---|
| 1 | Walk to every wall | You can't leave the arena | Cat. 1 |
| 2 | W / S / A / D | Moves forward/back/strafe relative to the **camera view**, with smooth start/stop | Cat. 3 |
| 3 | Hold RMB and move the mouse, then W | Camera turns; W now goes the new way; the player turns with the camera | Cat. 3, 6 |
| 4 | SPACE | Console: `Speed mode: FAST (10 m/s)`; clearly faster. SPACE again → normal | Cat. 4 |
| 5 | F while standing | Jumps ~1.5 m and lands back | Cat. 5 |
| 6 | F while running | Forward **arc** (projectile), keeps momentum in the air | Cat. 5 |
| 7 | F repeatedly in mid-air | No double jump | Cat. 5 |
| 8 | V | Console: `Camera mode: FirstPerson`; view from the eyes; player body hidden | Cat. 6 |
| 9 | In FP: hold RMB and look up/down | Pitch stops at about ±80° | Cat. 6 |
| 10 | V back to TP, then scroll | Distance changes smoothly, 2–15 m | Cat. 6 |
| 11 | TP: back the camera into a wall | Camera moves in front of the wall, no clipping | Cat. 6 |
| 12 | Walk into the test cubes | They slide/tip over visibly | Cat. 11 |
| 13 | ESC | Play Mode stops (in a build: the app quits) | Cat. 15 |
| 14 | Console | No errors | DoD |

## Tuning (Inspector, all have tooltips)
- Feels sluggish or too snappy → **Player Motor → Ground Acceleration** (default 40).
- Want slight steering in the air → **Air Acceleration** 2–5 (0 = pure projectile).
- Mouse too fast or slow → **Camera Rig → Look Sensitivity** (default 0.15).
- Zoom step too big (trackpad) → **Zoom Step**.
- Can't push the cubes → lower the cube **Mass** or raise the Player's mass.

If anything misbehaves, copy the Console message or describe what you see, and I'll debug it.

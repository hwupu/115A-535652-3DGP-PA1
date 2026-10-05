# Unity Handoff: Little Witch Animations (Blender FBX → Unity)

Audience: an AI agent (or developer) setting up the animations in Unity **after** the user exports the FBX from Blender.
Everything marked **[unverified]** was reasoned from Blender/Unity conventions and has **not** been test-imported. Verify it first.

---

## 1. What exists

One skinned character, one rig, **five hand-built animation clips** (generated procedurally in Blender, no mocap).

| Item | Value |
|---|---|
| Blender source | `…/PA1 Assets/the-little-witch/source/The little Witch.blend` |
| Armature object | `metarig` (166 bones, Rigify-style names, **not** Mixamo/Humanoid names) |
| Skinned meshes | `body`, `boots`, `dress`, `hair`, `hat` (all parented to `metarig`, Armature modifier) |
| Rest pose | **T-pose**, baked into mesh and armature (meshes + rest pose already match) |
| Frame rate | **24 fps** |
| Units | Blender metres. Character ≈ **1.07 m** tall including hat tip. Blender ground plane Z=0 = feet |
| Facing | Character faces **-Y in Blender** (toward Blender's default front view). **Verify facing after import** (Unity forward is +Z) **[unverified]** |
| Root bone | `spine` (pelvis, no parent). Hips/legs/torso/skirt all descend from it |

> **Status warning:** the Blender edits (T-pose bake and all five actions) were **not saved** by the assistant. The user must File → Save before exporting. Confirm the FBX actually contains 5 takes.

---

## 2. Blender FBX export settings (for the user, agent should verify what they got)

File → Export → FBX, with `metarig` + the 5 meshes selected (or "Selected Objects" off):

- Object Types: **Armature, Mesh**
- Scale `1.0`, Apply Scalings: **FBX Units Scale**, Forward `-Z`, Up `Y` (defaults)
- Geometry: Apply Modifiers **on** (`boots`, `hair`, `hat` have an "Auto Smooth" geometry-nodes modifier after the Armature modifier; the Armature modifier itself is not applied)
- Armature: **Add Leaf Bones OFF**, Primary Bone Axis `Y`, Secondary `X`, **Only Deform Bones OFF** (hair/skirt/hat bones carry skin weights), Armature FBX Node Type `Null`
- Bake Animation: **ON**, **All Actions ON**, **NLA Strips OFF**, Force Start/End Keying ON, Sampling Rate `1.0`, **Simplify `0.0`** (keys are already dense, one per frame; do not let the exporter thin them)
- Each action has a manual frame range set (see table), which the exporter should respect

Expected result: five takes named after the actions. Unity may show them prefixed, e.g. `metarig|Idle`. Rename/clean as needed.

---

## 3. Clip data (source of truth)

All clips key **the same 27 bones** (quaternion rotation on each; `spine` also has location). This is deliberate so switching clips never leaves residual pose. Unkeyed bones (fingers, toes, hair/hat chains, twist bones) simply inherit from their parents.

Keyed bones: `spine`, `spine.001`, `spine.002`, `spine.003`, `spine.006` (head), `thigh.L/R`, `shin.L/R`, `foot.L/R`, `upper_arm.L/R`, `forearm.L/R`, and 12 skirt panels `thigh.L.001.L/R` … `thigh.L.006.L/R`.

Time convention **[unverified]**: clip time 0 = first key (Blender frame 1). So Unity time = (BlenderFrame − 1) / 24. Confirm clip lengths below after import.

| Clip (Blender action) | Blender frames | Keys | Unity length | Loops? | Purpose |
|---|---|---|---|---|---|
| `Idle` | 1–73 | 73 | **3.000 s** | **Yes** | Breathing, slight weight shift, head sway (2 breaths per loop) |
| `Walk` | 1–25 | 25 | **1.000 s** | **Yes** | In-place walk, one full cycle = 2 steps |
| `Run` | 1–17 | 17 | **0.667 s** | **Yes** | In-place run, one full cycle = 2 strides, has flight phases |
| `Jump` | 1–36 | 36 | **1.458 s** | No | Crouch → takeoff → flight → landing → recover to stand |
| `ThrowBoomerang` | 1–36 | 36 | **1.458 s** | No | Right-handed sidearm throw with a left-foot step |

**Loop clips (Idle/Walk/Run):** the last key intentionally equals the first. Keep the full range (frames 0–72, 0–24, 0–16 at 24 fps), enable **Loop Time**, and do **not** trim the last frame. Unity wraps at the end, so the duplicate pose is never shown twice.

**One-shot clips (Jump/Throw):** first and last pose are the neutral standing stance, so they blend cleanly to/from Idle. Loop Time **off**.

---

## 4. Event times (Blender pose markers do not survive FBX export, so add these as Animation Events manually)

| Clip | Event name | Blender frame | Unity time | Meaning |
|---|---|---|---|---|
| `Jump` | `Takeoff` | 10 | **0.375 s** | Feet leave ground (toes pushing off) |
| `Jump` | `Land` | 23 | **0.917 s** | Feet touch ground again |
| `ThrowBoomerang` | `Release` | 14 | **0.542 s** | Hand releases the boomerang. **Spawn the projectile here** |
| `Walk` | `FootL` | 1 | 0.000 s (and 1.000 s) | Left heel strike |
| `Walk` | `FootR` | 13 | 0.500 s | Right heel strike |
| `Run` | `FootL` | 1 | 0.000 s (and 0.667 s) | Left foot contact |
| `Run` | `FootR` | 9 | 0.333 s | Right foot contact |

Other useful times:
- Jump: crouch bottom 0.250 s (f7), apex ≈ 0.667 s (f17), landing-crouch bottom 1.083 s (f27), fully recovered 1.458 s.
- Throw: wind-up peak 0.333 s (f9), left foot planted 0.458 s (f12), follow-through peak 0.792 s (f20), recovered 1.458 s.
- Run flight phases: ≈ 0.22–0.33 s and ≈ 0.55–0.67 s.

**Boomerang spawn:** at the release event, use the world position of bone **`hand.R`** (character's right hand), launched toward the character's forward direction.

---

## 5. Motion facts the agent needs for gameplay tuning

- **All clips are in place.** No root translation across the ground.
- **Walk speed match:** ≈ **0.43 m/s**. **Run speed match:** ≈ **1.41 m/s** (Blender/Unity metres). Move the character at these speeds to avoid foot sliding, or scale Animator speed by `actualSpeed / clipSpeed`.
- Walk and Run both **start on a left-foot contact**, so their normalized times are phase-aligned. Enable **time sync / "Match phase"** in the blend tree.
- **Jump vertical motion is baked into the `spine` (hips) bone**, not into a root. Hips go from about −0.09 m (crouch) to about **+0.34 m** above standing at apex. Airtime (f10→f23) is **0.542 s**, apex ≈ +0.30 m over the takeoff pose (effective g ≈ 8.2 m/s²).
  **Decision required:** if gameplay code also moves a CharacterController/Rigidbody up for the jump, the visual hop will double up. Either keep the collider grounded and let the clip carry the hop, or drive the physics jump and speed the clip to match (v₀ ≈ 2.66 m/s at g = 9.81 gives ≈ 0.36 m, airtime ≈ 0.54 s).
- **Throw is a sidearm**, deliberately: the hat brim droops to ~0.68 m and an overhand arc intersected it (measured). Arm-to-hat clearance in the final clips: ≥ 70 mm (Throw), ≥ 50 mm (Jump), by vertex distance.
- Foot placement was solved by 2-bone IK. Evaluated ankles matched targets within ~3 mm (Walk/Run/Idle) and ~6 mm (Throw, due to hip yaw). Feet never go below the ground plane.
- **No secondary motion** on hair or hat; only the skirt panels sway with leg motion (and flare in Jump). Hair/hat/skirt bones exist in the rig if you want Unity spring-bone physics layered on top **[optional]**.

---

## 6. Unity import settings (suggested)

**Rig tab**
- Animation Type: **Generic** (the bone names/count are not Humanoid-mappable by default; Humanoid auto-mapping is **[unverified]** and not recommended).
- Root node: the `spine`/`metarig` hierarchy as imported. **Root Motion Node: None**. Clips are in place; do not extract root motion from the hips.
- Optimize Game Objects: **off** (at least initially) so skirt/hair/hat bones stay accessible.

**Model tab**
- Scale Factor / Use File Scale should give a **~1.07 m** tall character at `1 Unity unit = 1 m`.
- Common Blender→Unity quirk **[unverified]**: if the model arrives rotated −90° on X or 100× scaled, fix by enabling Unity's **Bake Axis Conversion** (if available) or re-exporting with Apply Transform. Do not "fix" it by rotating the prefab root.
- Verify the character **faces +Z** in Unity. If it faces away, correct via export Forward/Up axes or a child-transform rotation, not by editing clips.

**Animation tab (per clip)**
- Idle / Walk / Run: **Loop Time ✔**, Loop Pose ✔, full range (see §3).
- Jump / ThrowBoomerang: Loop Time ✘.
- Root Transform rotation/position: irrelevant when Root Motion Node is None. **Apply Root Motion: off** on the Animator.
- Anim. Compression: **Off** (or Keyframe Reduction with very low error). Keys are dense, per-frame, linear.
- Add the Animation Events from §4.

---

## 7. Suggested Animator Controller

Parameters: `Speed` (float, m/s), `Jump` (trigger), `Throw` (trigger).

- **Locomotion blend tree (1D on `Speed`):** `Idle` @ 0, `Walk` @ 0.43, `Run` @ 1.41. Sync Walk/Run phases.
- **Locomotion → Jump** on `Jump` trigger (has exit time, fixed duration ≈ 0.05–0.1 s). **Jump → Locomotion** at ≈ 0.95 normalized time.
- **Locomotion → ThrowBoomerang** on `Throw` trigger; back at ≈ 0.95 normalized time. Best used while standing, since the clip steps forward ≈ 0.16 m with the left foot and shifts the hips in place.
- **Throw while moving (optional):** make a Transform Avatar Mask enabling **everything under `spine.001`** (torso, head, both arms/hands). The skirt panels, thighs and `spine` itself are excluded, so legs keep locomotion. Put the throw on an upper-body layer. Pelvis twist is lost with this mask; acceptable.

---

## 8. Verification checklist (do before building gameplay on top)

1. Import shows **5 clips**, with lengths ≈ 3.000 / 1.000 / 0.667 / 1.458 / 1.458 s.
2. Character height ≈ 1.07 m, upright, facing +Z, not rotated/scaled.
3. Scrub `Walk`: feet plant and roll heel→toe, no sliding in place, arms counter-swing.
4. Scrub `Jump`: feet fully off the ground between 0.375 s and 0.917 s, hips return to standing height by the end.
5. Scrub `ThrowBoomerang`: at 0.542 s the right arm is extended forward at roughly shoulder height; `hand.R` is a sensible spawn point; arm does not clip through the hat.
6. Switch clips back and forth in the Animator: no leftover pose (all five clips key the same bones).
7. Loops (Idle/Walk/Run) show no pop at the wrap point.

---

## 9. Known limitations / assumptions

- Not test-imported in Unity. Items marked **[unverified]** are conventions, not observations.
- Blender changes were unsaved at the time of writing. The FBX must be exported from the saved/edited session.
- Texture/material setup for the witch is **not covered** here.
- Foot contact uses flat-foot stance plus heel/toe pitch with simple ground clearance. No ground-height adaptation.
- Throw/Jump markers (`Release`, `Takeoff`, `Land`) exist in the Blender actions but are not carried by FBX; recreate them as Unity Animation Events with the times above.

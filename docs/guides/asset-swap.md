# Asset Swap Guide: models, variants, HUD (PB-25)

For Sprint 5 (art & audio). Everything here is done in the Unity Editor; **no code changes are needed** (ADR-0007).

---

## 0. Before you import anything
1. **License check.** Note every asset in `docs/asset-credits.md` (name, author, URL, license).
2. **This repo is public.** Unity Asset Store packs (Standard EULA) and many paid or "no redistribution" assets must **not** be pushed to a public repo. Options:
   - put such packs in a git-ignored folder (e.g. `Assets/ThirdParty_Local/`; I'll add the ignore rule when you need it), and note in the README how to get them; or
   - make the repo private before pushing them.

   Sketchfab CC-BY / CC0 models and Mixamo characters are fine to commit (credit CC-BY authors). The **submission zip** to E3 can include everything either way, because it isn't public.
3. **Formats.** Prefer **FBX** (Sketchfab: "Autoconverted format (FBX)"; Mixamo: "FBX for Unity"). For glTF/GLB, first install **glTFast** (Package Manager → `com.unity.cloud.gltfast`).
4. **Keep third-party content in its own folder** (e.g. `Assets/ThirdParty/<PackName>/`), not inside `_Project`.

## 1. Import and fix materials
1. Drag the FBX (with its textures) into `Assets/ThirdParty/<PackName>/`.
2. Select the FBX → Inspector:
   - **Model** tab: check **Scale Factor**. Drag it into the scene next to a default 1 m Cube to compare sizes.
   - **Materials** tab: **Extract Materials…** into the same folder, so you can edit them.
3. **Pink materials?** The pack uses the Built-in pipeline. Run **Window → Rendering → Render Pipeline Converter → Built-in to URP → Material Upgrade → Initialize And Convert**, or set each material's shader to **Universal Render Pipeline/Lit**.

## 2. Replace the look of an existing prefab (e.g. the target barrel)
Our prefabs are built as **root (physics + gameplay) → `Model` (looks) + `MinimapIcon` (map)**. You only change `Model`.

1. Double-click `Assets/_Project/Prefabs/Greybox/Target_Barrel.prefab` to open Prefab Mode.
2. Select the **`Model`** child. In the Inspector, **remove its Mesh Filter and Mesh Renderer** (⋮ → Remove Component). `Model` is now an empty container. Keeping it preserves references to it (e.g. the gem's spin uses `Model`).
   - Delete any other greybox-only children (the barrel's `Band`).
   - **Don't touch** `MinimapIcon`.
3. Drag your imported model **onto `Model`** to make it a child. Then:
   - Set its Position to (0, 0, 0) first. Then adjust it so the **bottom of the model sits at y = 0** and it's centered on X/Z. **Pivot = bottom center** is the spawner's rule (ADR-0006).
   - Scale and rotate until it matches the old size (barrel ≈ 0.8 × 1.2 m, crate 1.2 m, pillar 1 × 2 m, gem ≈ 0.4 m at 0.9 m height).
   - If the model came with its own **colliders, remove them**. Physics lives on the root.
4. Select the **root** and fit its collider to the new shape:
   - Use **Edit Collider** (the button on the collider) or type Center/Size/Radius.
   - **Use primitive colliders** (Box / Capsule / Sphere), or several on the root. A **non-convex Mesh Collider does not work on a moving Rigidbody**.
   - The gem's Sphere Collider must stay **Is Trigger**.
5. Adjust **Rigidbody → Mass** if the new object feels too light or heavy (target 2, crate 15, pillar 40).
6. **Save** (the arrow at the top-left of the Hierarchy, or Cmd+S), then press Play:
   - The Console spawn line should still say **0 overlapping pairs**. Footprints are measured automatically from the new collider and model.
   - Hitting a target should still tint it grey. That works for URP Lit materials; other shaders may not tint.

## 3. Add variants (several barrels, rocks, crates…)
1. Right-click the base prefab (e.g. `Obstacle_Crate`) → **Create → Prefab Variant**, and name it e.g. `Obstacle_Rock_A`.
   - A variant keeps all components and the minimap icon, and only overrides what you change.
   - Use a variant of the **closest** base: target → `Target_Barrel`, obstacle → `Obstacle_Crate` / `Obstacle_Pillar`, gem → `Collectible_Gem`.
2. Open the variant and do **section 2** steps 2–6 with a different model.
3. Open `Assets/_Project/Settings/SpawnConfig.asset` and add the variant to **Target / Obstacle / Collectible Prefabs** (the + button). The spawner picks one at random for each object.
   - Remove the greybox prefab from the list if you no longer want it to appear.
4. Optional: rerun **Boomerang Guardian → Setup → Sprint 3**. It adds a minimap icon to any listed prefab that lacks one; variants already have it.

## 4. Boomerang
Open `Prefabs/Greybox/Boomerang.prefab`. Replace the children of **`Spinner`** (Arm_A / Arm_B) with your model, lying **flat** (spin axis = local Y) and about 0.6 m across. Keep the root's Sphere Collider (trigger) and the `BoomerangProjectile` component.

## 5. Player character (e.g. Mixamo)
1. On `Gameplay/Player`, **untick the Mesh Renderer** (keep the Capsule Collider, Rigidbody and scripts). Delete or disable `Nose`.
2. Drag the character in as a child named `Model`. The Player pivot is at the capsule center, so set the model's **Y to -1** (feet on the ground) and rotation (0, 0, 0), facing +Z.
3. `Main Camera → Camera Rig → Hide In First Person`: replace the old renderer with the character's **Skinned Mesh Renderer(s)**.
4. Animations (idle / run / jump) need a small Animator controller script. Ask me when you get there.

## 6. Environment
- **Ground:** change the `Ground` material, or use a tiling texture (material → Base Map + Tiling).
- **Walls: keep the cubes** (the spec says "use cube objects to create boundaries"). Change only their material. Decorative models can sit outside or on top of them.
- **Platform:** keep the `GuardianPlatform` disc and the `GuardianPlatformTrigger`. Decoration can go on top as children without colliders, or with colliders lower than ~0.1 m.

## 7. Restyle the HUD
Everything is under `UI/HUD`, and script links survive moving or resizing anything.
- **Panels:** select `Panel_*` → Image → Color, or assign a **Source Image** sprite (e.g. a UI pack's frame).
- **Text:** select any `Text_*` → TextMeshPro: font size, color, style. For a **custom font**, use **Window → TextMeshPro → Font Asset Creator** (pick a .ttf → Generate Font Atlas → Save), then drag the font asset into the text's **Font Asset** field.
- **Buttons:** `Button_*` → Image color / sprite, and Button → Colors for hover and press.
- **Minimap:** size via `Panel_Minimap`; the zoom is on `Cameras/MinimapCamera → View Radius`.
- **Layout safety:** the stats panel is top-left, the journey banner top-center, the minimap top-right, the cooldown bar bottom-center, and messages at the center. Keep these areas from overlapping at 16:9.

## 8. After any swap: check
1. **Boomerang Guardian → Validate Wiring** → "All required references are set".
2. Press Play. Spawn line: 0 failed, 0 overlaps. Click targets, bounce off obstacles, pick up a gem, use the platform.
3. Add the asset to `docs/asset-credits.md`, then tell me and I'll commit.

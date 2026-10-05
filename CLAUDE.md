# CLAUDE.md — AI context for "The Boomerang Guardian" (3DGP PA1)

## Project
- NYCU 535652 3D Game Programming, Programming Assignment 1 (single-person).
- Spec: `docs/reference/3DGP_PA_01_Instruction.pdf`. Rubric → backlog mapping: `docs/00-requirements-traceability.md`.
- Engine: **Unity 6000.3.25f1 LTS**, URP (Universal 3D template). This folder **is** the Unity project root.
- Target finish ≈ 2026-10-14 (2 weeks starting 2026-10-01).

## Roles
- **User**: Product Owner, then asset / 3D / sound designer and scene builder (works in the Unity Editor).
- **Claude**: Scrum Master, then programming assistant. Claude writes C# scripts and gives step-by-step Editor instructions when asked. Claude does **not** edit scenes/prefabs (`.unity`/`.prefab` YAML) unless explicitly asked.

## Code conventions
- All our own content lives in `Assets/_Project/`; third-party packs stay in their own folders.
- Scripts: `Assets/_Project/Scripts/{Core,Player,Cameras,Boomerang,Spawning,Interaction,UI,Audio,Editor}`; namespace `BoomerangGuardian.<Folder>`. The folder is `Cameras` (plural) because a `BoomerangGuardian.Camera` namespace would shadow `UnityEngine.Camera`.
- Compile check without opening the Editor (only when the Editor is closed): `/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -nographics -projectPath "$PWD" -logFile /tmp/unity-compile.log`, then grep for `error CS`.
- Step-by-step Editor guides for the User live in `docs/guides/`.
- Scene wiring is automated with idempotent Editor setup scripts in `Scripts/Editor/` (namespace `BoomerangGuardian.EditorTools`, menu **Boomerang Guardian → Setup**), per ADR-0009. Open the scene before creating assets in a setup run (`OpenScene` unloads in-memory assets).
- If the Editor is open, verify in an APFS clone: `cp -cR Assets Packages ProjectSettings Library /tmp/bg-verify/`, then batch-run there (`-executeMethod …`). Never batch-run on the real project while the Editor is open.
- Prefab convention: the root pivot sits at the ground contact (bottom center); visuals go in a `Model` child; root holds Rigidbody/collider/gameplay component.
- `Assets/StreamingAssets/config.json` (Unity requires it at the Assets root) holds runtime tunables (ADR-0008).
- Build: menu **Boomerang Guardian → Build → macOS**, or batch `-executeMethod BoomerangGuardian.EditorTools.BuildScript.BuildMacBatch -buildOutput <path>.app` (in a clone if the Editor is open). Output in `Builds/macOS/` (git-ignored) plus an editable `config.json` next to the `.app`. Headless smoke run: `"<app>/Contents/MacOS/Boomerang Guardian" -batchmode -nographics -logFile <log>`.
- Theme: **Halloween** (ghost targets). Per-prefab target hit reaction `Tumble` / `FloatAway`, with the fade material template `M_TargetFade` keeping transparent variants in builds (ADR-0010, `Setup → Sprint 5`).
- `WiringValidator` (menu **Validate Wiring**) checks the scene **and** prefabs under `_Project/Prefabs`; mark legitimately empty fields `[OptionalReference]`.
- Menus/panels pause gameplay via `PlayerInputReader.SetGameplayEnabled(false)` + `Time.timeScale = 0`; ESC and F5 keep working.
- One MonoBehaviour per file, file name = class name. Use `[SerializeField] private` fields with `[Tooltip]` for tunables. Avoid public fields.
- Unity 6 APIs: `Rigidbody.linearVelocity` (not `velocity`), `FindObjectsByType`, **Input System package** (no legacy `Input.GetKey`).
- Theme-agnostic: gameplay code never references specific art assets. Prefabs are injected through ScriptableObjects or the Inspector.
- Identify objects by component (`Target`, `Obstacle`, `Collectible`), not by tag strings.

## Process
- Backlog: `docs/01-product-backlog.md`. Current sprint: `docs/sprints/`. Decisions: `docs/decisions/ADR-*.md`.
- Commit messages: `type(PB-xx): summary (#issue)` (types: feat, fix, docs, chore, refactor, asset). GitHub issue number = PB number + 1 (see the backlog's Issue column). Use `Closes #n` when an item meets the DoD.
- GitHub repo `hwupu/115A-535652-3DGP-PA1` is **public** (PO decision), so never commit assets whose license forbids redistribution.
- After each significant AI interaction, append an entry to `docs/ai-log/prompt-log.md`.
- Record reflection observations as they happen in `docs/report/reflection-notes.md`.
- Definition of Done: `docs/02-definition-of-done.md`.

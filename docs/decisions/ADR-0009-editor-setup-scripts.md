# ADR-0009: Hybrid scene setup with idempotent Editor scripts

- **Status:** Accepted
- **Date:** 2026-10-03
- **Deciders:** User (PO), proposed by Claude (from the Sprint 1 retro)

## Context
In Sprint 1 the PO wired the scene by hand following a guide. It worked first try, but manual wiring is the slowest part of the PO's work. Sprint 2 adds many objects and links (prefabs, managers, HUD, minimap). Hand-editing `.unity` / `.prefab` YAML is fragile, and CLAUDE.md forbids it.

## Decision
- Claude writes **Editor scripts** in `Assets/_Project/Scripts/Editor/` (namespace `BoomerangGuardian.EditorTools`, an Editor-only assembly folder). They use the Unity API (`PrefabUtility`, `EditorSceneManager`, `SerializedObject`) to create and link objects.
- Each script runs from the menu **Boomerang Guardian → Setup → …**, or from batch mode with `-executeMethod` when the Editor is closed.
- Scripts are **idempotent**: they find objects by name or path and create only what's missing, so they never duplicate. Existing user-tuned values are not overwritten unless the script says so. Each script logs what it created or skipped.
- Each script creates **greybox** visuals only. Art, layout and polish stay with the PO, who later swaps visuals inside the generated prefabs.
- Each sprint guide lists "what the setup script created", so the PO can explain the scene.

## Consequences
- This saves PO time and makes setup reproducible. The scripts are also evidence of AI-assisted tooling for the report.
- It costs about 1–2 SP of Claude time per sprint.
- Batch-mode runs need the Editor to be closed. The menu route works with it open, and Cmd+Z undoes changes.

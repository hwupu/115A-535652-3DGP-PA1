# ADR-0008: Runtime `config.json` for demo-time toggles

- **Status:** Accepted
- **Date:** 2026-10-01
- **Deciders:** User (PO), proposed by Claude

## Context
The spec is ambiguous about "respawn" on platform exit. It could mean (a) a fresh random layout, or (b) restoring the hidden objects. The PO wants to switch between them depending on how the TA interprets it, **without rebuilding**. Other demo tunables (object counts, speeds) also benefit from being editable.

## Decision
- A plain-C# `GameConfig` class (serializable, read with `JsonUtility`) is loaded once at startup by `ConfigLoader` (Core).
- **Lookup order:**
  1. `config.json` **next to the built app**. On macOS that is the folder containing `BoomerangGuardian.app`. In the Editor it is the project root.
  2. `Application.streamingAssetsPath/config.json`, the default shipped with the build.
  3. Built-in defaults, with a warning logged.
- Example contents:
  ```json
  {
    "respawnMode": "Regenerate",
    "targetCount": 150,
    "obstacleCount": 150,
    "collectibleCount": 30,
    "normalSpeed": 5.0,
    "fastSpeed": 10.0
  }
  ```
  `respawnMode` is `"Regenerate"` (fresh random layout at full count) or `"Restore"` (re-show the same hidden objects).
- Values are validated and clamped. For example, the total is clamped to 200–500, with at least 100 targets and 100 obstacles. The effective config is logged to the Console at startup.
- A debug key (**F5**) reloads the config at runtime, so the PO can change the mode mid-demo.

## Consequences
- The TA can see either behaviour on request. Scene-wiring complexity doesn't change.
- Inspector values become fallbacks, and the JSON overrides them. This is documented in the README.

## Amendment (2026-10-03, Sprint 2)
- The shipped default lives in `Assets/StreamingAssets/config.json`. Unity requires `StreamingAssets` at the `Assets/` root, the one exception to the `_Project` convention.
- Keys added: `randomSeed`, `targetScore`, `collectibleScore`, `maxThrowRange`, `boomerangSpeed`, `boomerangCooldown`.
- When no `ConfigLoader` is in the scene, `ConfigLoader.Current` is null and components fall back to their Inspector values.

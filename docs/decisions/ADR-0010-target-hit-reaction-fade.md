# ADR-0010: Per-prefab target hit reaction, and a fade material template

- **Status:** Accepted
- **Date:** 2026-10-05
- **Deciders:** User (PO), proposed by Claude

## Context
With the Halloween theme, targets are ghosts. The PO wants a hit ghost to "flow upward and fade away" instead of bouncing on the ground. The spec still requires that the target is **pushed away**, **stays visible for 2 s**, then **disappears**. Imported FBX materials are opaque URP Lit, which can't fade. Switching a material to transparent at runtime works in the Editor, but in a **build** URP strips shader variants that no included material uses, so the fade could silently render opaque.

## Decision
- `Target` gets a per-prefab **Hit Reaction**: `Tumble` (gravity, grey tint, as before) or `FloatAway`.
- **FloatAway:** an impulse push (horizontal + up, never down), gravity off, drag, constant upward acceleration, an upright spin, and a smooth fade of base color alpha and emission over `disappearDelay` (2 s); then destroy, as before. The minimap dot still turns grey.
- **Fade material:** `M_TargetFade.mat` (URP Lit, Transparent, `_NORMALMAP` + `_EMISSION` enabled) is referenced by each FloatAway target prefab (`fadeMaterialTemplate`). At hit time each visual renderer gets a runtime copy, with the original textures and colors copied over (`MaterialUtility.CopyLitProperties`). Referencing the asset from a prefab used in the scene keeps the transparent variants in builds. Without a template: runtime conversion plus a warning.
- `Sprint5Setup` creates the material and links it into every target prefab in `SpawnConfig` (first run: also sets FloatAway).

## Consequences
- Theme-agnostic: barrels can keep `Tumble`, ghosts use `FloatAway`, with no code change.
- Runtime material copies are destroyed with the target (no leaks). Fading ghosts don't cast shadows.
- Only URP Lit-style properties are copied. A custom-shader model may lose some look while fading.

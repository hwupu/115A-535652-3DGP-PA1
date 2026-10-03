# ADR-0006: Spawn with rejection sampling and overlap checks

- **Status:** Accepted
- **Date:** 2026-10-01
- **Deciders:** User (PO), proposed by Claude

## Context
We need 200–500 random objects, inside bounds, with no initial overlap.

## Decision
`SpawnManager` samples random XZ positions inside the arena (minus a wall margin), skipping exclusion circles (platform, player start). Each candidate's bounds are tested with **`Physics.CheckBox`** against the existing scene, plus our own list of already-placed bounds (newly instantiated colliders are not visible to queries until physics syncs). Up to N attempts per object; failures are logged. A debug validator counts overlaps after spawning.

## Consequences
Guaranteed non-overlap with proof in the Console. Respawn behavior on platform exit is pending a PO decision.

## Amendment (2026-10-03, Sprint 2)
- **Footprints:** each prefab is measured once (temporary instance → collider + renderer bounds) as a rotation-independent XZ radius around its pivot, plus a height. Placement checks circle distance against our own list (+ `spacing`), plus `Physics.CheckBox` from 5 cm above the ground against existing scene geometry.
- **Prefab convention:** the root pivot sits at the ground contact point (bottom center). The spawner places roots at ground height.
- **Validation:** after each spawn, all collider pairs are checked with `Physics.ComputePenetration` and the count is logged (smoke test: 0 overlapping pairs in 3 runs of 330 objects).
- **Regenerate:** old objects are deactivated before `Destroy`, so their colliders don't block the new layout in the same frame.
- **Respawn mode:** `Regenerate` / `Restore`, chosen in `config.json` (ADR-0008).

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

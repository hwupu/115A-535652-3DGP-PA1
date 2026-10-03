# ADR-0007: Render-texture minimap and theme-agnostic prefabs

- **Status:** Accepted
- **Date:** 2026-10-01
- **Deciders:** User (PO), proposed by Claude

## Context
We need a minimap in the upper-right showing the player and nearby objects. The theme is undecided.

## Decision
An orthographic top-down **minimap camera** follows the player and renders to a RenderTexture shown in a RawImage at the upper-right. Each spawnable prefab has a flat colored **icon quad on a `Minimap` layer**. The main camera excludes that layer, and the minimap camera renders only icons + ground. Gameplay code references prefabs through a `SpawnConfig` ScriptableObject and identifies objects by component (`Target`, `Obstacle`, `Collectible`).

## Consequences
Icons stay readable regardless of art. The User can swap greybox prefabs for themed models without code changes.

## Amendment (2026-10-03, Sprint 3)
- Icons are `MinimapIcon` children (root keeps level and a fixed world height; a flat `Shape` child on layer **Minimap** = 8). Heights set the draw order: ground 10, walls/platform 11, obstacles 19, targets 20, gems 21, player 25. The camera is at 60.
- Icon renderers cast and receive no shadows. The main camera's culling mask excludes the Minimap layer.
- The map is north-up by default, with the player arrow turning (`MinimapCamera.rotateWithTarget` toggles that). The view is 60 × 60 m around the player ("nearby objects").
- `SpawnManager` ignores `MinimapIcon` renderers when measuring footprints. Otherwise icons floating 20 m up would inflate the footprint height.
- New prefab variants get icons by rerunning the Sprint 3 setup (it iterates `SpawnConfig`).

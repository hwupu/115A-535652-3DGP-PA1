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

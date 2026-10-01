# ADR-0004: Dynamic Rigidbody player instead of CharacterController

- **Status:** Accepted
- **Date:** 2026-10-01
- **Deciders:** User (PO), proposed by Claude

## Context
The rubric needs gravity, projectile motion during jumps, and visible pushing of obstacles.

## Decision
The player is a **dynamic Rigidbody** (rotation frozen, interpolation on, continuous collision). Movement sets the horizontal `linearVelocity` toward a target velocity (smoothed). A jump applies a vertical impulse. In the air, the horizontal velocity is preserved, which gives a parabolic projectile arc. Ground check uses `Physics.SphereCast`.

## Consequences
Pushing obstacles and gravity come from the physics engine with no special code. We must tune friction (a zero-friction PhysicsMaterial on the player) and obstacle mass.

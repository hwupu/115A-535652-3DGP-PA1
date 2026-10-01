# ADR-0005: Kinematic boomerang following a scripted path

- **Status:** Accepted
- **Date:** 2026-10-01
- **Deciders:** User (PO), proposed by Claude

## Context
The boomerang must fly to the selected target, hit it, and always return. A purely physical boomerang is unstable and can get lost.

## Decision
The boomerang is a **kinematic Rigidbody + trigger collider**, moved in `FixedUpdate` via `MovePosition`. Outbound: a quadratic Bezier curve (side-offset control point) to the target at a set speed. On hitting the target (or reaching it): it applies an `AddForce(Impulse)` to the target's Rigidbody, then returns by homing to the player's hand and is caught within a radius. A max-flight-time fallback forces the return. Cooldown is 3 s from the throw.

## Consequences
Deterministic and stable, which is graded under "overall stability". The impact force on targets is still real physics.

# ADR-0011: Simple animation layer for the player character

- **Status:** Accepted
- **Date:** 2026-10-05
- **Deciders:** User (PO), proposed by Claude

## Context
The PO made a "Little Witch" character (Sketchfab, CC-BY) with five in-place clips in Blender, built with another AI agent. They came with a handoff document (`docs/reference/Unity_Animation_Handoff.md`). The handoff raises open questions: physics jump vs. baked hop, speed matching (clips 0.43 / 1.41 m/s vs. game 5 / 10 m/s), throw release timing via animation events, and an upper-body layer. **PO direction: "keep implementation simple. It is okay to have glitches."**

## Decision
- **Physics stays authoritative.** `PlayerMotor` keeps moving and jumping the Rigidbody (the rubric's gravity / projectile motion). The Animator only follows: **Apply Root Motion off**.
- **Animator `AC_Player`:** a 1D blend on `Speed` (Idle 0, Walk 2.5, Run 6 m/s, with Walk ×1.5 and Run ×1.8 playback) plus `Jump` / `Throw` triggers via Any State, returning at 90 %. No layers, masks or events.
- `PlayerAnimator` sets `Speed` from the horizontal velocity, and fires `Jump` on `PlayerMotor.Jumped` and `Throw` on `BoomerangThrower.Thrown`.
- **Import:** Generic rig, Optimize Game Objects off, compression off, clips renamed (`metarig|Idle` → `Idle`), loop on Idle/Walk/Run. Model scaled ×1.5 (1.33 m → ~2 m) with its feet at the capsule bottom. The `Hand` spawn point is parented to the `hand.R` bone. The witch's renderers are hidden in first person. The capsule renderer is disabled (collider kept).
- All of this is done by `PlayerCharacterSetup` (idempotent menu / batch).

## Accepted glitches
Foot sliding at game speeds; a slightly doubled jump hop (the clip lifts the hips +0.34 m on top of the physics jump); the boomerang leaves at click time, not at the clip's release frame (0.542 s); a throw/jump trigger pressed mid-clip may replay.

## Verification
The handoff checklist was run headless before implementation: 5 clips with the expected lengths; faces +Z; `hand.R` present; height 1.33 m (the handoff said 1.07 m). Setup idempotent; clone build + headless run without exceptions.

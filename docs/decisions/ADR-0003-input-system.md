# ADR-0003: New Input System

- **Status:** Accepted
- **Date:** 2026-10-01
- **Deciders:** User (PO), proposed by Claude

## Context
Unity 6 projects default to the Input System package. Legacy `Input.GetKey` raises deprecation warnings.

## Decision
Use the **Input System package** with an `InputActionAsset` (Player map: Move(W/S), Look(mouse delta), ToggleSpeed(Space), Jump(F), SwitchCamera(V), Zoom(scroll), Select(LMB), Quit(Esc)).

## Consequences
Bindings live in one place, and rebinding is easy. This is slightly more setup than legacy input.

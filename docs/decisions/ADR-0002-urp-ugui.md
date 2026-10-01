# ADR-0002: URP template, uGUI + TextMeshPro for the HUD

- **Status:** Accepted
- **Date:** 2026-10-01
- **Deciders:** User (PO), proposed by Claude

## Context
We need a HUD (score, cooldown) and a minimap image. Unity 6 provides both UI Toolkit and uGUI.

## Decision
Use the **Universal 3D (URP)** template. The HUD uses **uGUI Canvas + TextMeshPro**, and the minimap uses a **RawImage**.

## Consequences
uGUI is the most documented option and is simple to wire in the Inspector. URP gives good-looking lighting for free assets.

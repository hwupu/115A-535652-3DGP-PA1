# The Boomerang Guardian: A Hero's Journey

NYCU 535652 3D Game Programming — Programming Assignment 1.
Unity **6000.3.25f1 LTS** (URP).

## Open & play
1. Unity Hub → **Add → Add project from disk** → select this folder.
2. Open `Assets/_Project/Scenes/Main.unity` and press Play.

## Build
**Boomerang Guardian → Build → macOS** → `Builds/macOS/BoomerangGuardian.app`, with an editable `config.json` next to it.

## Controls
| Key | Action |
|---|---|
| W / S | Move forward / backward (relative to the camera view) |
| A / D | Strafe left / right (extra) |
| Hold right mouse | Look around (pitch / yaw) |
| SPACE | Toggle normal / fast speed |
| F | Jump |
| V | Switch first-person / third-person camera |
| Mouse wheel | Third-person camera distance |
| Left click | Select a target → throw the boomerang (3 s cooldown, max 40 m) |
| ESC | Quit |
| F5 | Reload `config.json` |

## Runtime config (`config.json`)
The default is in `Assets/StreamingAssets/config.json`. In a build, a `config.json` placed **next to the app** overrides it, and F5 reloads it in game.
Keys: `respawnMode` (`Regenerate` / `Restore`), `randomSeed`, `targetCount`, `obstacleCount`, `collectibleCount`, `normalSpeed`, `fastSpeed`, `targetScore`, `collectibleScore`, `maxThrowRange`, `boomerangSpeed`, `boomerangCooldown`. Out-of-range values are clamped (200–500 objects, ≥ 100 targets and obstacles).

## Repository map
```
Assets/_Project/        our scripts, prefabs, scenes, materials, audio
docs/
  reference/            assignment instruction PDF
  00-requirements-traceability.md   spec + rubric → backlog → status
  01-product-backlog.md
  02-definition-of-done.md
  sprints/              sprint plans, daily logs, reviews, retros
  decisions/            Architecture Decision Records (ADRs)
  ai-log/               AI prompt log (evidence for the report)
  asset-credits.md      third-party asset acknowledgements
  report/               report notes, draft, final PDF
CLAUDE.md               context file for the AI assistant
```

## Process links
- Sprint board: https://github.com/users/hwupu/projects/1
- Issues / milestones: https://github.com/hwupu/115A-535652-3DGP-PA1/issues

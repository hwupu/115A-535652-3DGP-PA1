# Sprint 0 — Foundation (2026-10-01 → 2026-10-02)

**Sprint goal:** The project, repository and process artifacts are ready, so Sprint 1 can start coding right away.

## Sprint backlog
| Item | Owner | Status |
|---|---|---|
| Read the spec, ask clarifying questions | Claude (SM) | ✅ |
| Product backlog + rubric traceability | Claude drafts, User (PO) approves | ✅ |
| Docs skeleton, CLAUDE.md, ADR-0001…0007, DoD | Claude | ✅ |
| `.gitignore` (Unity/macOS/builds) + `.gitattributes` (Git LFS) | Claude | ✅ |
| Create Unity 6000.3.25f1 URP project in this folder | User (created in `My project/`), Claude moved it to the root | ✅ |
| `git init` + `git lfs install` + remote (User); first commit (Claude) | User + Claude | ✅ |
| Push to GitHub. PO decided to keep the repo **public** | User + Claude | ✅ |
| GitHub Issues #1–#24 + 4 sprint milestones + labels | Claude | ✅ |
| GitHub Project board ([users/hwupu/projects/1](https://github.com/users/hwupu/projects/1)): 25 items, Status + Story Points fields | User authorized the `project` scope, Claude created the board | ✅ |

## Daily log
- **2026-10-01**: Spec read (14 pages). PO decisions: ~2-week timeline, theme TBD after the asset search, Git + private GitHub, collectibles = bonus score only. Backlog (24 items, ~51 Must SP) and a 3-sprint plan approved. The PO decided that this folder is the Unity root, so the instruction PDF moved to `docs/reference/`. The PO added Git LFS as a requirement, with macOS as the development platform. An AI session was interrupted mid-write; files were checked and recreated (lesson: verify the file state after interruptions).
- **2026-10-01 (later)**: The User renamed the folder to `115A-535652-3DGP-PA1` (no spaces), ran `git init` with LFS, added remote `hwupu/115A-535652-3DGP-PA1`, and created the URP project in `My project/`. Claude moved `Assets/`, `Packages/`, `ProjectSettings/` (+ the ignored `Library/`, `Logs/`, `UserSettings/`) to the root, then verified Force Text serialization and Visible Meta Files, and that LFS picks up the PNG and PDF. Made the first commit. Not pushed, because the GitHub repo is public and the plan says private.

## Review (2026-10-01)
- **Sprint goal met.** Unity project at the repo root, Git + LFS, pushed to GitHub. Docs skeleton, 8 ADRs, backlog mirrored as issues #1–#25, 4 sprint milestones, Project board.
- Backlog changes during the sprint: + PB-24 runtime `config.json` (from the respawn decision).

## Retro (drafted by SM, confirmed by PO)
- **Keep:** asking clarifying questions before planning; recording every PO decision immediately (backlog decisions log + ADR).
- **Problem:** environment friction cost time. The interrupted AI write, the folder renamed mid-session, and the `gh` auth scope refresh (3 attempts) all happened.
- **Try:** at the end of each AI session, verify the file/git state (`git status`) before moving on. Do environment changes (renames, auth) between sessions, not during one.
- **PO comment:** the folder rename and re-login were deliberate setup steps; nothing further to reflect on.

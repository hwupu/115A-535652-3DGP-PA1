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
| GitHub Project (board view): needs the `project` token scope. The first refresh attempt didn't apply | User refreshes auth, Claude creates it | ⬜ |

## Daily log
- **2026-10-01**: Spec read (14 pages). PO decisions: ~2-week timeline, theme TBD after the asset search, Git + private GitHub, collectibles = bonus score only. Backlog (24 items, ~51 Must SP) and a 3-sprint plan approved. The PO decided that this folder is the Unity root, so the instruction PDF moved to `docs/reference/`. The PO added Git LFS as a requirement, with macOS as the development platform. An AI session was interrupted mid-write; files were checked and recreated (lesson: verify the file state after interruptions).
- **2026-10-01 (later)**: The User renamed the folder to `115A-535652-3DGP-PA1` (no spaces), ran `git init` with LFS, added remote `hwupu/115A-535652-3DGP-PA1`, and created the URP project in `My project/`. Claude moved `Assets/`, `Packages/`, `ProjectSettings/` (+ the ignored `Library/`, `Logs/`, `UserSettings/`) to the root, then verified Force Text serialization and Visible Meta Files, and that LFS picks up the PNG and PDF. Made the first commit. Not pushed, because the GitHub repo is public and the plan says private.

## Review
_(at sprint end)_

## Retro
- Keep:
- Problem:
- Try:

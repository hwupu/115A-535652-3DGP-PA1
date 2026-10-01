# AI Prompt Log

Evidence for the report section "Use of AI Tools". One entry per significant interaction.
Tool: **Claude Code** (model Claude Opus 5.5) in the terminal, unless noted otherwise.

Template:
```
## YYYY-MM-DD · #NN · <topic> · Sprint N · PB-xx
- Intent:
- Prompt (summary or key quote):
- AI output:
- Accepted / changed / rejected — and why:
- Verification:
```

---

## 2026-10-01 · #01 · Project kickoff & backlog · Sprint 0 · PB-00
- **Intent:** Set up an AI-native, Scrum-style workflow for a solo Unity assignment.
- **Prompt (key quote):** "Please follow best AI-native development approach and keep the major decisions, prompt logs, etc. … you will act as Scrum Master first, then programming assistant. I'll be Product Owner … plan the backlog for me to review."
- **AI output:** Read the 14-page spec PDF. Asked 4 clarifying questions (timeline, theme, version control, collectible logic). Produced a 24-item product backlog with acceptance criteria mapped to all 100 rubric marks, 7 proposed technical decisions (ADRs), and a 3-sprint plan + Sprint 0.
- **Decisions by User:** 2-week timeline; theme deferred until assets are found (so the code must be theme-agnostic); Git + private GitHub; collectibles give bonus score only; the current folder becomes the Unity root, so the PDF moved to `docs/reference/`.
- **Verification:** The User reviewed and approved the plan. Each rubric line was cross-checked in `00-requirements-traceability.md`.

## 2026-10-01 · #02 · Git LFS, macOS ignores, interrupted session recovery · Sprint 0 · PB-00
- **Intent:** Add Git LFS, make sure `.gitignore` covers macOS files and Unity builds/temp, and check for leftovers after a confirmation dialog was closed by mistake.
- **AI output:** Checked running processes (none left over; Unity Hub was the User's own). Found that the interrupted batch had written **none** of the ADR, sprint, log, credits or reflection files, and recreated them. Rewrote `.gitignore` (Unity cache/temp, builds incl. `*.app`/`*.dSYM`/Burst/IL2CPP backup folders, IDE, macOS, Windows) and `.gitattributes` (LFS for images, models, audio/video, fonts, native libs, PDF).
- **Lesson:** AI tool calls can be interrupted silently, so always verify the actual file state rather than assuming.

## 2026-10-01 · #03 · Move Unity project to repo root + first commit · Sprint 0 · PB-00
- **Intent:** Move the Hub-created project (`My project/`) to the repo root and make the first commit.
- **AI output:** First found the working folder "missing". The User had renamed it (spaces → hyphens) while the AI session was open. The AI stopped and asked instead of recreating the old path. After the restart: checked for path conflicts, moved the project folders (same-volume move, so the 2.9 GB `Library/` didn't need a re-import), verified the Unity VCS settings and LFS tracking, and committed.
- **Caught by AI:** the GitHub remote is **public**, contrary to the agreed plan (private). The push was held for a PO decision (academic integrity: a public solution repo).
- **Verification:** `git status` showed only Assets/Packages/ProjectSettings/docs plus root files staged. `git lfs ls-files` showed the PNG and PDF.

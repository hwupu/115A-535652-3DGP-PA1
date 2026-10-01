# ADR-0001: Repository layout: Unity project root = repo root, docs beside Assets, Git LFS for binaries

- **Status:** Accepted
- **Date:** 2026-10-01
- **Deciders:** User (PO), proposed by Claude

## Context
The submission is a zipped Unity project, and we want process evidence (backlog, sprints, AI log) kept with the code. Third-party models, textures and audio are large binaries. Development happens on macOS.

## Decision
The assignment folder is the Unity project root and the git root. `docs/` sits beside `Assets/`, which Unity ignores. Our content goes in `Assets/_Project/`, and third-party packs keep their own folders. The instruction PDF lives in `docs/reference/`. **Git LFS** tracks binary assets (images, models, audio, video, fonts, native libs, PDF) via `.gitattributes`. `.gitignore` excludes Unity cache/temp (`Library/`, `Temp/`, `Logs/`, `UserSettings/`…), builds (`Build/`, `Builds/`, `*.app`), IDE files, and macOS/Windows OS files.

## Consequences
The zip is just the folder minus the ignored directories. Unity Hub can't create a project in a non-empty folder, so the project is created elsewhere and its contents moved in (see Sprint 0 log). GitHub LFS free quota: 10 GiB storage and bandwidth (Git LFS on GitHub Free), which is plenty for this project.

## Amendment (2026-10-01)
The PO chose to keep the GitHub repo **public**. Consequence: every committed third-party asset must have a license that permits redistribution. Otherwise, keep it out of git (e.g. import locally, and document how to obtain it).

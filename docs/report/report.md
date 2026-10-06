# The Boomerang Guardian: AI Tools & Scrum Report

Pu-Hsuan Wu · NYCU 535652 3D Game Programming · PA1 · Unity 6000.3.25f1 LTS (URP) · Repo: github.com/hwupu/115A-535652-3DGP-PA1

## 1. Use of AI Tools

**Tools.** The main tool was **Claude Code** (Claude Opus 5.5), an AI agent running in the terminal with access to the project folder, git, the GitHub CLI and Unity's command line. A second AI agent helped me rig and animate the witch character in Blender; it handed over a written "handoff" document. I also used ffmpeg (run by the agent) to compress the audio.

**How they were used.** I gave the AI two roles: *Scrum Master* first, then *programming assistant*, while I acted as Product Owner and as the asset, scene and sound designer. The AI turned the 100-mark rubric into a traceable backlog, recorded every major decision as an Architecture Decision Record (11 ADRs), and kept a prompt log (24 entries). It wrote all C# code (35 files, about 3,000 lines of gameplay code and 2,000 lines of Editor tooling) and step-by-step Editor guides. After Sprint 1, my retro feedback led to a **hybrid workflow**: the AI wrote idempotent *Editor setup scripts* (menu "Boomerang Guardian → Setup") that create prefabs, link components and build the HUD and minimap. Wiring that took me about 45–60 minutes by hand in Sprint 1 took about 10 minutes in Sprint 2.

**Benefits.** Speed (the whole core loop of spawn, pick, throw, hit, score and platform was built and play-tested in one day), consistent architecture, and documentation that was written *while* working instead of afterwards. The AI also caught process risks: it noticed the GitHub repo was public and kept a Unity Asset Store skybox out of it for licensing reasons, and it asked clarifying questions instead of guessing (e.g. the ambiguous "respawn" rule became a runtime `config.json` switch, so I can show either behaviour to the TA).

**Limitations.** The AI cannot *see* or *play* the game, so every feel and visual question needed my play-test. Its own design caused my worst bug: a "helpful" silent fallback hid a missing camera reference for two sprints, so W always moved along the world Z axis. Model swaps twice broke references to replaced objects, and one field was wrongly marked optional, which hid an error from its own checker. A handoff claim from the Blender agent (character height 1.07 m) turned out to be wrong (1.33 m).

**Verification.** I never accepted AI output on trust. (1) Every change was compiled by Unity in batch mode, inside an APFS copy-on-write clone so it never touched my open Editor. (2) Setup scripts were run twice to prove they changed nothing the second time. (3) Automated checks: a spawn test (330 objects, 0 overlapping pairs, measured with `Physics.ComputePenetration`), a headless run of the built macOS app, and a "Validate Wiring" tool added after the camera bug, which now checks the scene and all prefabs. (4) The other agent's animation handoff was tested on a real import before any code was built on it. (5) Each sprint ended with my own play-test against a checklist mapped to the rubric.

## 2. Adoption of Scrum

**Setup.** Solo Scrum: I was Product Owner, the AI was Scrum Master and developer. Artifacts lived in the repo (`docs/`): a product backlog with MoSCoW priority and rubric marks per item, a Definition of Done, a rubric traceability matrix, and one file per sprint with plan, daily log, review and retro. GitHub Issues, milestones and a Project board mirrored the backlog (see the figure).

**Sprints.** *Sprint 0* (Oct 1): repo, Git LFS, backlog, ADRs, board. *Sprint 1*: walking skeleton (arena, camera-relative movement, speed toggle, jump, first/third-person camera, ESC). *Sprint 2*: core loop (spawn manager, ray-cast picking, boomerang, hit physics, platform trigger, collectibles, HUD, `config.json`). *Sprint 3*: minimap and the Hero's Journey story (intro, seven stages, ending) plus the first macOS build. *Sprint 4*: polish from my feedback, including the facing bug and the wiring validator. *Sprint 5*: Halloween art, animated witch, real sound, ghost effects, hover trajectory preview and a middle-click orbit camera for the demo.

**How Scrum helped.** Retrospectives produced real process changes: Sprint 1's retro introduced the setup scripts, and Sprint 3's retro introduced "fail loudly" code and automated wiring checks. Reviews against the traceability matrix showed exactly which rubric marks were still open. When Sprint 2 finished on its first day, we re-planned from three to five sprints so logic polish came *before* art. **Challenges:** story points were meaningless with an AI developer, so we dropped them; requirements kept emerging (Git LFS, licensing, theme); and a checklist item was once marked as passed while the bug was still there.

## 3. Reflection

⟦**What I learned:** your own words. Suggested points: Unity physics vs. scripted motion (boomerang), ray casting and triggers, URP rendering details such as shader variants in builds, working with FBX/Blender assets.⟧

⟦**Effectiveness of AI-assisted development:** your verdict. E.g. where it saved the most time, where you had to correct it, how much you trusted it by the end.⟧

⟦**Effectiveness of Scrum (solo):** your verdict. E.g. were the retros and the board worth the overhead for one person?⟧

⟦**What I would improve next time:** e.g. automate checks from day 1, settle the theme and assets earlier, keep the repo private if using store assets.⟧

**Asset credits.** *Models:* "Halloween graveyard pack" by rudolfs (skfb.ly/pr9W6, CC BY 4.0); "Boomerang" by Muhammad Ari Kurniawan (skfb.ly/6tvtu, CC BY 4.0); "The little Witch" by Antropik (skfb.ly/oQxwO, CC BY 4.0), rigged and animated by me in Blender with AI assistance. *Skybox:* "Free Stylized Hand-Painted Skybox" ⟦publisher⟧, Unity Asset Store (Standard EULA; not in the public repo). *Sound (freesound.org, CC0):* Egg Shaker - 1 Throw by connermusician (#199823); Obscure SFX - Hit #1 by HxcPotato (#160883); Error by Kastenfrosch (#521973); UI Interaction Error Beep - Ghost Metro Ticket by TommasoMotteran (#859155); invalid1.wav by mmaal (#663859); Halloween 8-bit by batmetal (#391434).

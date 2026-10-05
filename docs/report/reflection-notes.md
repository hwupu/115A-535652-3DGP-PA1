# Reflection Notes (raw material for the report)

Capture observations **as they happen**. The report must show "genuine reflection", so these notes matter more than polish.
Report outline (1–2 pages, ≥ 500 words): 1) Use of AI tools · 2) Adoption of Scrum · 3) Reflection · Asset credits.

## AI-assisted development — observations
- (2026-10-01) Planning with AI: the AI turned the 100-mark rubric into a traceable backlog in one session, and asked clarifying questions before assuming a theme or timeline.
- (2026-10-01) An AI session was interrupted and silently failed to write several files. The human had to notice, and the AI had to verify the real state. Lesson: trust, but verify.

- (2026-10-01) The AI noticed the GitHub repo was public, against the plan. AI as a checklist enforcer catches small but important process slips.

- (2026-10-01) For the respawn ambiguity the AI offered A/B options; the PO chose a third way (a runtime config toggle). AI options are a starting point, not the boundary.

- (2026-10-03) AI-written step-by-step Editor guides with exact values let the PO set up the scene first try. Precise, verifiable instructions beat high-level ones.

- (2026-10-03) AI verification loop: running the AI's own Editor script in a disposable clone caught a real bug (stale asset reference) before the PO ever saw it. "AI writes, AI tests in a sandbox, human play-tests" worked well.
- (2026-10-03) Limitation: the AI can compile and run editor-time checks, but can't feel the game (flight curve, sounds, HUD readability), so human play-testing stays essential.

- (2026-10-03) The AI asked clarifying questions about vague feedback ("keep player face front", "takes a while to spawn") rather than implementing a guess. The PO chose to test more first. Asking before acting avoided rework.
- (2026-10-03) End-to-end automation: the AI could build the game and run it headless to prove a requirement (config next to the app) that would otherwise need a manual test.

- (2026-10-03) **AI-introduced risk:** a "helpful" silent fallback in AI-written code hid a missed manual wiring step for two sprints, and a checklist item was marked as passed. Lesson: AI code should fail loudly, and verification should be automated where possible (wiring validator) rather than relying only on manual checklists.

- (2026-10-05) The AI's guide said to "replace the children of Spinner", but the PO's natural workflow replaced Spinner itself. Guides need to anticipate how a human actually works in the Editor, and code should be robust to it (import rotations, lost links).

- (2026-10-05) Recurring pattern: every model swap so far (boomerang, pumpkin) broke a reference to the replaced child. AI-built tooling (validator, loud fallbacks) turned a confusing "it doesn't move" into a one-line diagnosis. But one field was wrongly marked optional by the AI, which hid the second case from the validator. Tool rules need review too.

- (2026-10-05) **Multi-agent handoff:** a different AI agent animated the witch in Blender and wrote a handoff doc that marked its own unverified assumptions. This AI tested those claims on a headless import first; most held, one didn't (height). Explicit "[unverified]" markers plus automated checks made agent-to-agent collaboration safe.
- (2026-10-05) The PO cut scope mid-task ("simple, glitches OK"). With a deadline, "good enough and documented" beat a perfect animation system. ADR-0011 lists the accepted glitches, so they are conscious trade-offs, not bugs.

## Scrum — observations
- (2026-10-01) Solo Scrum: one person holds the PO role, and the AI acts as Scrum Master, keeping artifacts honest (backlog, DoD, sprint files).
- (2026-10-01) Requirements kept emerging mid-sprint (Unity root location, Git LFS). These were small and absorbed into Sprint 0, which shows why the backlog is a living document.

- (2026-10-03) Sprint 1 retro produced a concrete process improvement (automate scene wiring with Editor scripts). The retro is useful even solo.

- (2026-10-03) The PO dropped story-point tracking for a solo, 2-week project. Adapting Scrum ceremonies to the context rather than following them dogmatically.

- (2026-10-03) Sprint 2 finished on day 1, so the PO re-planned into 5 sprints (logic polish before art). Scrum's inspect-and-adapt applied to the plan itself.

- (2026-10-03) The PO's "I want to do more testing before I decide" turned a vague request into a precise bug report a sprint later. Inspection before adaptation.

## User's self-reflection (to be written by the User)
- What did I learn?
- Where did AI help most, and where did it mislead me?
- Did Scrum help a solo project, or was it overhead?
- What would I do differently next time?

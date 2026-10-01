# Definition of Done

A backlog item is **Done** when all of these are true:

1. All acceptance criteria in `01-product-backlog.md` are met in **Play Mode** and the PO (User) has seen it working.
2. No errors, and no new warnings, in the Unity Console.
3. Code follows `CLAUDE.md` conventions (namespace, folder, serialized private fields with tooltips).
4. The User can explain the code. For AI-generated code, Claude walks through it on request (required by the spec: "you may be asked to explain").
5. Hierarchy objects and assets have clear names (rubric: logical hierarchy & naming).
6. The traceability row in `00-requirements-traceability.md` is updated to ✅ with a note on how it was verified.
7. Committed with `type(PB-xx): …` and pushed.
8. Any significant AI interaction is logged in `ai-log/prompt-log.md`. Any third-party asset used is added to `asset-credits.md`.

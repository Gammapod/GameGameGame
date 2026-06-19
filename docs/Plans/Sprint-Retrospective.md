# Sprint Retrospective

Status: Living retrospective notes from the current wrap-up.

## What went well

- TDD workflow helped keep Core, Content, Editor, and Agent API changes aligned.
- The behavior-chain GUI is now much clearer and no longer presents legacy low-level authoring as the default path.
- Trace formatting and preview commands gave us better inspection tools before expanding primitives.
- The first utility Action Step batch was implemented with editor/API discovery and tests, not just Core runtime behavior.
- Delegating the generated-content exercise to the content-editor perspective surfaced design gaps quickly.

## What was difficult

- Scenario testing is still too ad hoc. Temporary tests work, but there is no ergonomic workflow for authoring a scenario, simulating turns, and reviewing compact traces.
- Direction/movement design questions emerged immediately once richer scenarios were tried.
- Some desired scenario behavior needs sequenced state changes after a successful step, while canonical behavior chains currently stop on first success.
- `CreateFacing` is useful as a prototype but too limited for realistic content because it cannot select a template.

## Process improvements

- Perform sprint wrap-up explicitly using `docs/Source of Truth/sprint-wrapup-process.md`.
- Archive completed planning documents promptly so `docs/Plans/` only contains active planning work.
- Before adding more primitives, run generated scenario exercises that use preview and trace formatting.
- For each new primitive batch, record deferred brainstorm items in the roadmap rather than keeping them implicit in chat.
- When tests become slow or flaky under normal parallel build/test execution, prefer targeted filters during development and a final relevant broader run before handoff.

## Open retrospective questions for team discussion

- Should scenario exercise helpers become production editor/agent features or remain test infrastructure?
- How detailed should generated scenario reports be before they become too heavy for sprint work?
- What threshold should decide whether a design gap becomes planned work versus conceptualized backlog?

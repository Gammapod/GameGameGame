# Sprint Retrospective

Status: Living retrospective notes from the current wrap-up.

## What went well

- Sprint 16 moved smoothly because each showcase had a clear content goal, a small primitive request, immediate content proof, headless validation, and a recorded GIF artifact.
- Requesting both strafing primitives together was more efficient than one-at-a-time requests because their semantics, tests, docs, and catalog support were nearly identical.
- Keeping action plans linear while embedding distance/directional checks inside primitives worked well for authoring. The final kiting-orbiter chain stayed readable despite expressing several fallback behaviors.
- The Sprint 15 recorder paid off immediately in Sprint 16: every showcase could produce a visual artifact without adding bespoke inspection tooling.
- The content-owner/core-owner cadence remained effective: concise primitive requests with state reads/writes, tie-breaks, fallback behavior, and scenario intent gave the core-owner enough context to implement complete vertical slices.

- Sprint 10 promoted scenario feedback into a production editor/agent API instead of leaving it as test-only infrastructure.
- The scenario-root inventory model matched existing engine/content concepts and avoided inventing a separate scenario language.
- Asking the content-editor perspective to author a scenario without prescribed Action Steps provided useful documentation/tooling validation.
- TDD workflow helped keep Core, Content, Editor, and Agent API changes aligned.
- The behavior-chain GUI is now much clearer and no longer presents legacy low-level authoring as the default path.
- Trace formatting and preview commands gave us better inspection tools before expanding primitives.
- The first utility Action Step batch was implemented with editor/API discovery and tests, not just Core runtime behavior.
- Delegating the generated-content exercise to the content-editor perspective surfaced design gaps quickly.

## What was difficult

- Sprint 16 still had to work around unfiltered `AcquireNearestTarget`. Sparse layouts, far-away player starts, and the `beta-kiting-orbiter-fallback-lane` companion scenario were needed so helper/blocker entities did not become unintended targets.
- The deepest kiting-orbiter fallback proof took more design effort than the single-primitive showcases. Demonstrating clockwise fallback, anticlockwise fallback, flee fallback, and seek fallback in one readable room was awkward with unfiltered target acquisition, so the proof was split into a main room plus a one-row fallback lane.
- Some fixture assertions had to be adjusted to match actual compact trace wording. The strafing tests initially expected a separate `strafe=...` line in the report text, while the scenario report exposed the human summary `moved ... strafing ...`; lower-level tests still cover the structured trace details.
- Generated debug artifacts can easily appear in the workspace if an output path is not kept outside the repository. During wrap-up, stray `debug/` artifacts had to be removed from the worktree.

- Scenario reports improved, but content-authoring agents still need more concise report text/state summaries to avoid inspecting structured data manually.
- Initiative ordering is intentionally temporary and deterministic for scenario-root inventory spaces, but future local/global initiative semantics remain undesigned.
- The older test-local `MinimalScenarioRunner` now overlaps with `AgentContentEditorApi.RunScenario` and needs a cleanup/replacement decision.
- Scenario testing is still too ad hoc. Temporary tests work, but there is no ergonomic workflow for authoring a scenario, simulating turns, and reviewing compact traces.
- Direction/movement design questions emerged immediately once richer scenarios were tried.
- Some desired scenario behavior needs sequenced state changes after a successful step, while canonical behavior chains currently stop on first success.
- `CreateFacing` is useful as a prototype but too limited for realistic content because it cannot select a template.

## Process improvements

- For future showcase sprints, keep using the pattern: primitive request -> authored YAML -> fixture test -> `record-scenario` GIF -> sprint-plan note.
- When two primitives are symmetric variants, request them together to reduce duplicated coordination and documentation effort.
- For complex fallback-chain showcases, consider planning both a player-facing/readable scenario and a small mechanical proof scenario from the start.
- Always record scenario GIFs to an external temp/artifact directory, not a repository-local `debug/` folder, unless the artifact is intentionally being checked in.
- Scenario fixture tests should assert report wording at the same level the scenario report actually exposes, and leave lower-level trace-detail assertions to Core tests.

- Continue naming sprint plans sequentially, starting with Sprint 10, and archive completed sprint plans under `docs/Archived/` during wrap-up.
- Prefer scenario exercises that ask the content-editor perspective for intent-level behavior, not implementation-specific Action Step instructions, when validating authoring/documentation quality.
- Perform sprint wrap-up explicitly using `docs/Source of Truth/sprint-wrapup-process.md`.
- Archive completed planning documents promptly so `docs/Plans/` only contains active planning work.
- Before adding more primitives, run generated scenario exercises that use preview and trace formatting.
- For each new primitive batch, record deferred brainstorm items in the roadmap rather than keeping them implicit in chat.
- When tests become slow or flaky under normal parallel build/test execution, prefer targeted filters during development and a final relevant broader run before handoff.

## Open retrospective questions for team discussion

- How polished should `RunScenario` text reports be before saved runlogs/golden runlog tests become worthwhile?
- Should Sprint 11 finish scenario report/API polish first, or use the scenario runner immediately to drive direction/movement semantics?
- What threshold should decide whether a design gap becomes planned work versus conceptualized backlog?

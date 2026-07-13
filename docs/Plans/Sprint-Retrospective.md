# Sprint Retrospective

Status: Retrospective reference. No active sprint plan is selected during roadmap reset.

## What went well

- Runtime action-plan override MVP landed as a tight Core/Content/Editor vertical slice: one-turn pre/main/post slots, canonical producer Action Steps, YAML/editor/API validation, and a manual-test scenario all share the same simple `targetSlot` + `planId` model.
- Splitting the work into runtime spine, authorable pre-plan slice, content exercise, then symmetric main/post producers kept the sprint easy to validate and avoided prematurely designing passive items, possession, or dynamic target-plan copying.
- The unified history/log/rollback sprint worked well as a sequence of thin vertical slices. Starting with `WorldState` snapshots, then history frames, then rollback, successful submissions, failed entries, interval actor logs, projection, SadConsole consumption, and undo kept each step testable and prevented a large risky rewrite.
- Coordinating Core-owner and frontend-owner at ownership boundaries was effective: Core established shared history/projection/rollback semantics, then frontend-owner wired SadConsole presentation and input without inventing frontend-only simulation rules.
- Calling content-editor to review the migrated headless scenario reports caught the correct acceptance question: preserve authoring-facing report usefulness while changing the backing model.
- Treating the old PNG/GIF scenario recorder as legacy rather than forcing a deep migration avoided over-investing in superseded tooling and produced a better backlog direction: saved runlogs/history playback/SadConsole-rendered export.

- Moving targeting and post-move facing into shared state services simplified canonical behavior chains without losing fallback composition. Plans can now consume target slots while target selection happens before the plan, and facing follows successful movement direction afterward.
- Keeping target slots numeric with content-authored hints avoided baking semantic roles like enemy, food, or ally into Core while still giving authors enough structure for different entities to interpret slots differently.
- Demoting legacy acquisition/turn-only steps instead of deleting runtime support preserved old content compatibility while making new editor/API authoring safer.

- Sprint 21's ad-hoc Console scenario catalog request still mapped cleanly onto existing roadmap pressure around scenario/content package ergonomics, manual scenario selection, and beta vignette playability. Treating it first as a viability/backlog comparison kept the work from becoming an unbounded frontend rewrite.
- The implementation stayed aligned with the longer-term frontend direction by putting discovery/manifest behavior in a shared content-facing catalog service instead of burying the contract in Console UI code.
- Keeping Console UI testing light while testing the catalog/launch contract matched Console's prototype role and avoided over-investing in replaceable frontend behavior.

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

- The main-plan override semantics had a subtle trap: scheduled entity plans were being materialized before the override chain was composed. A targeted test made clear that `Main` override should avoid planning the default entity plan at all for that turn.
- Scenario recording/folder discovery can still mutate `Beta/Manifest.yaml` as a side effect. During wrap-up this had to be reverted to preserve the no-modify-existing-content rule while keeping the new showcase file.
- The initial spike vocabulary described behavior-provider entity overrides, while the MVP implemented referenced plan-slot overrides. Naming and docs needed careful cleanup so future readers do not assume dynamic target-plan copying or passive inventory overrides are already supported.
- The roadmap had accumulated completed implementation sequences inline. During cleanup, active planning had to be separated from completed evidence. Future sprints should archive completed plans immediately instead of leaving long completed checklists in active roadmaps.
- Some documentation lagged behind implementation quickly: frontend standards still said global logs were controlled-only after history projection had autonomous rows. Wrap-up should include stale-statement greps for old capability limitations.
- The term “scenario recording” now refers to legacy tooling, while future desired work is history playback / visual export. Keeping those separate in docs is important to avoid accidentally extending the wrong surface.

- Targeting touched several layers at once: runtime state, content templates, YAML/editable DTOs, validation, scenario runners, Console, editor/API helpers, and tests. Small seams such as `TurnService` pre-plan hooks were important to keep Core content-agnostic.
- Existing beta fixture expectations had encoded old facing and target-acquisition behavior. Updating those tests required separating behavior we still want to guarantee from brittle beta-era implementation details.
- Parallel test execution can still hit transient build output locks in this workspace; sequential final verification remains safer for wrap-up.

- Sprint 21 began as an ad-hoc request outside the currently assumed next mechanics gate. Although it proved roadmap-adjacent, it exposed a prioritization question: ad-hoc workflow/friction fixes may be legitimate sprint candidates, but they should be checked explicitly against roadmap buckets before consuming implementation time.
- The request blurred the line between a small Console workflow improvement and broader content-package architecture. We had to repeatedly constrain scope to folder discovery, cached manifests, and manifest-only descriptions so the work did not prematurely become package/import semantics.
- Auto-generating a manifest inside `src/GameGameGame.Content/Beta` creates a content-adjacent artifact even though existing content YAML files remain untouched. Future generated/cache artifacts need an explicit decision about whether they are checked in, ignored, or treated as author-maintained content indexes.

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

- For override/status-like features, explicitly distinguish “destination model supports this” from “content can author this now.” That distinction helped keep mimic target-plan copying, passive inventory contribution, and player possession out of the MVP.
- When validating content scenarios with tooling that scans/discovers folders, check for generated manifest/cache edits before handoff.
- For cross-layer refactors, keep using a numbered-slice checklist, but archive the checklist once complete and leave only follow-ups in active roadmaps.
- When a legacy tool is intentionally not migrated, document both parts in the same cleanup: current legacy support status and the preferred replacement backlog item.
- Use content-facing review for report/API migrations even when automated tests preserve shape; the key acceptance criterion is whether the report still answers authoring questions.
- Prefer “small shared seam first” refactors, such as `ActorTurnResolver` and `ActionStepAttemptProjection`, before migrating larger loops or frontends.

- For state-system refactors that replace action-plan scripting, explicitly list which old steps remain runtime-compatible but non-canonical before editing docs/tests. That prevents accidental preservation of old authoring patterns.
- When adding generic slots, document both the engine invariant and the content-authoring convention in the same sprint so future work does not infer semantic slot names from examples.

- For ad-hoc feature requests, keep using a short intake step before implementation: compare against roadmap buckets, identify what the request unblocks, classify it as roadmap-aligned / reprioritization signal / distraction, and only then decide whether to plan a sprint slice.
- If an ad-hoc request is accepted, write or update a small plan with explicit in-scope/out-of-scope boundaries before coding. This is especially important when a small workflow feature could expand into a larger architecture change.
- Add a lightweight prioritization check during wrap-up: did the ad-hoc work indicate the roadmap is stale, or did it merely surface a tactical bottleneck inside an existing bucket?
- Generated/cache files under content directories should have an explicit policy before broad use: checked-in curated artifact, local cache ignored by git, or generated output outside source content.

- For future showcase sprints, replace the old `record-scenario` GIF step with saved runlog/history playback or SadConsole-rendered export; the Console command is removed, while the legacy recorder service should only remain as long as tests or explicit adapters still need it.
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

- Does Sprint 21 indicate that scenario/content package ergonomics should move above Gate 5 template spawning, or was it a one-off tactical bottleneck now resolved?
- Should `src/GameGameGame.Content/Beta/Manifest.yaml` be checked in as a curated scenario index with descriptions, or treated as a generated local cache and ignored?
- What threshold should allow an ad-hoc user-experience/tooling request to interrupt the currently planned mechanics/content gate?

- How polished should `RunScenario` text reports be before saved runlogs/golden runlog tests become worthwhile?
- Should Sprint 11 finish scenario report/API polish first, or use the scenario runner immediately to drive direction/movement semantics?
- What threshold should decide whether a design gap becomes planned work versus conceptualized backlog?

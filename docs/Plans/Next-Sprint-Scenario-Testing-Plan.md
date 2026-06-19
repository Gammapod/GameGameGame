# Next Sprint Scenario Testing Plan

Status: Draft / likely next sprint pickup.

Read when:

- implementing or refining the next scenario-feedback sprint;
- deciding which generated scenario feedback features are in or out of scope;
- recording handoff notes from scenario exercise work.

Related:

- `docs/Source of Truth/Engine-Editor-Capabilities.md`
- `docs/Source of Truth/planning-index.md`
- `docs/Plans/High-Level-Roadmap.md`

## Goal

Make generated scenario authoring and simulation feedback comfortable enough to evaluate behavior-system design choices before adding another large batch of movement/direction primitives.

## Current priority rationale

The sprint delivered canonical behavior-chain GUI clarity, safe legacy hiding, trace formatting, plan preview, and the first utility Action Step batch. The content-editor exercise showed that the next bottleneck is testing workflow: we can implement primitives, but we need faster feedback on whether authored scenarios behave as intended.

## Loose scope

- Build a headless scenario exercise workflow around existing editor/content services and Core simulation.
- Keep generated exercises out of checked-in content unless they become deliberate fixtures.
- Reuse `ContentEditorService`, `AgentContentEditorApi`, `PreviewActionPlan`, and `BehaviorChainTraceFormatter` rather than bypassing editor parity.
- Support multi-turn simulation reports with compact traces.
- Make unsupported capability gaps explicit in generated exercise output.
- Treat the current Avalonia GUI as out of scope unless explicitly selected; scenario feedback work should prioritize editor services, agent/headless APIs, tests, and future frontend readiness.

## Candidate testable outcomes

- A test/helper can create temporary entities, carried inventory, action plans, behavior chains, initial facing, and default plan assignments through editor-facing APIs.
- A test/helper can materialize a small world, run N turns/actions, and return compact trace summaries.
- A generated scenario can exercise `DropFacing`, `PushFacing`, `DestroyTarget`, and `CreateFacing` without manual YAML inspection.
- Scenario reports distinguish engine bugs from unsupported authoring/design requests.

## Design decisions to discuss before implementation

- What minimum scenario DSL/API shape is useful without becoming an editor-only content language?
- Should generated scenario helpers live in tests only, or become an editor service / agent API feature?
- What is the expected relationship between content templates and runtime entities in generated simulation exercises?
- How should failed/all-fallback-exhausted behavior-chain turn consumption be displayed in reports?

## Out of scope for this next sprint unless explicitly selected

- Resolving the full movement/direction philosophy.
- Implementing `CreateFacing(templateId)` or template spawning.
- Adding scheduler/speed/multiple-action systems.
- Adding behavior templates.
- Refactoring or extending the current Avalonia GUI.

## Promotion triggers for follow-up backlog items

- Promote compact world/state summaries if raw traces are not enough to understand scenario outcomes quickly.
- Promote plan preview + simulation in one API command if scenario exercises repeatedly require separate preview, simulation, trace, and state-diff calls to answer one authoring question.
- Promote capability-gap reporting if unsupported requests recur, especially missing Action Steps, direction overrides, template spawning, state slots, or inventory-transfer semantics.
- Promote saved runlogs or golden runlog tests only after the scenario report format is stable enough that expected-output fixtures will not churn constantly.
- Promote scenario documents only if temporary/generated setup becomes valuable enough to preserve as reusable checked-in fixtures or selectable scenarios.

## Handoff notes for future sessions

Update this section during or at the end of scenario-feedback work. Keep it concise and operational.

Record:

- implementation files or tests that became central;
- commands/tests that were useful for verification;
- scenario shapes that were exercised;
- capability gaps encountered;
- items intentionally deferred back to the roadmap;
- whether the active plan should be archived, extended, or replaced.

Current handoff state:

- No implementation has started from this plan yet.
- The likely first slice remains a headless scenario exercise workflow using editor/content services and Core simulation, with compact trace output and explicit unsupported-gap notes.

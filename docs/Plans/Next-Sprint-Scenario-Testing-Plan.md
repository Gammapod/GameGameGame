# Next Sprint Scenario Testing Plan

Status: Draft / likely next sprint pickup.

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

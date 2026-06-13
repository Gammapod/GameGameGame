# High-Level Roadmap

This roadmap tracks upcoming engine/editor parity work at a planning level. The agent editor API is the next planned item. Later items are intentionally listed as conceptualized, not yet planned until the agent API has been used to generate and validate test content.

## Planned

### Agent editor API layer

Status: Planned.

Supporting documents:

- [Agent API Wishlist](Agent-API-Wishlist.md)
- [Agent Editor API Plan](Agent-Editor-API-Plan.md)

Goal: create a stable, constrained agent-facing API over the editor/content services so agents can author content through the same capability model as the editor GUI.

Initial scope:

- Add an in-process API facade over existing editor/content services before choosing an external protocol.
- Support document/session operations, validation, YAML preview/diff, entity template authoring, actor initial facing, action plans/steps, supported checks, and supported effects.
- Keep authoring canonical: do not expose legacy arbitrary variables, legacy variable fields, or `SetVariable` as new authoring commands.
- Return structured results and actionable diagnostics from mutating operations.
- Add tests that use the API to generate movement-capable test content, validate it, and inspect the resulting YAML.

Planned follow-up: after using the API to generate test content, revise this roadmap based on gaps found in content authoring, validation, diagnostics, and generated YAML quality.

## Conceptualized, not yet planned

### Weight mechanics simplification

Status: Conceptualized, not yet planned.

Concept: remove carrying capacity as a primary mechanic and replace it with a simpler containment rule where an entity may exist inside another entity when the contained entity's weight is less than or equal to the container entity's weight. This likely treats weight more like bulk or volume than physical mass.

Planning deferred until the agent API can author and validate inventory/weight test content.

### Runtime entity indexing and simulation efficiency

Status: Conceptualized, not yet planned.

Concept: add runtime indexes so entity interactions can resolve more quickly while many entities are simulated. Likely indexes include entity ID, plane/world location, container ownership, and eventually relationship or template/tag lookups.

Planning deferred until current authored scenarios and generated test content provide clearer performance targets.

### Diegetic action-plan entities

Status: Conceptualized, not yet planned.

Concept: represent action plans diegetically as entities during gameplay. Each entity would have an action-plan stack that can be inspected as its own inventory-like space, and rearranging plans would change runtime behavior.

Planning deferred because this depends on stable action-plan authoring, inventory/containment semantics, and likely runtime indexing.

### New action primitives and runtime states

Status: Conceptualized, not yet planned.

Concept: add new primitives and state needed for basic gameplay scenarios, potentially including entity creation, entity destruction, player/screen messages, per-action-plan cooldowns, moving toward arbitrary targets, and friendly/hostile entity lists.

Planning deferred until concrete generated test content or gameplay scenarios require these capabilities.

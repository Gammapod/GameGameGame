# Behavior Primitive Action Plans

Status: Archived / superseded.

The behavior-primitive/fallback foundation work has been archived at:

- [Behavior Primitive/Fallback Foundation Archive](../Archived/Behavior-Primitive-Fallback-Foundation.md)

The active direction is now to build a separate canonical behavior system beside the legacy low-level action-plan model:

- [Behavior Model Consolidation Plan](Behavior-Model-Consolidation-Plan.md)

Summary of completed foundation work:

- persistent entity action state for `Facing` and `Target`;
- primitive-backed descriptor support for `MoveFacing` and `PickupTarget`;
- linked fallback references between primitive-backed plans;
- validation for required primitive state and missing fallback references;
- editor service and agent API support for primitive-backed plans and a `MoveFacing -> PickupTarget` helper.

This linked-plan foundation remains useful compatibility/prototype work, but it is not the desired editor-facing model. The desired canonical model is an entity Action Plan represented as an ordered Fallback Chain of engine-defined Action Steps.

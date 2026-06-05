# GameGameGame Invariants

This document records minimal functional requirements that should influence tests. Keep this list small and stable.

## Entity And Space

- Every meaningful game object is an entity with a stable ID.
- Entity locations are represented by occupancy of nodes in planes.
- At most one entity may occupy a node at a time.
- Traversals through containment or inventory relationships must be cycle-safe.

## Inventory And Weight

- An entity has no usable inventory space if its inventory width or height is `0`.
- Missing or zero weight contributes `0` weight.
- An entity's own weight does not count against its own carrying capacity.
- Recursive carried weight includes all entities inside the entity's inventory space, plus anything those entities recursively carry.
- Pickup must fail if `current carried weight + target total weight > carrying capacity`.

## Actions And Turns

- An entity is an actor only if it has a decidable action plan or decision trigger.
- Actions must produce structured traces for failed checks and resolutions.
- Ranked action plans must distinguish failure that continues to the next action from failure that consumes the turn.
- Spatial recursion may exist, but temporal recursion must be explicitly guarded.

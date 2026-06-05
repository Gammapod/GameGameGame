# GameGameGame Design Notes

## Core Premise

GameGameGame is a recursive entity-space simulation.

Every object that can matter to the rules is an entity. Every contained, equipped, configured, or nested context is represented as a plane or graph space. Inventory, equipment, levels, menus, save files, behaviors, and settings are not separate categories; they are entity-owned spaces with different rules attached.

Rules should operate on spatial relationships, capabilities, and effects rather than hardcoded object classes.

## Guiding Principles

### Everything Is An Entity

The player, a sword, a slime, a chest, a dungeon, a save file, a behavior module, a menu, and a settings object should all be represented as entities.

Entities are not divided into fundamentally different base types such as item, monster, level, or menu. Those are roles expressed through components, capabilities, rules, and spatial relationships.

Example capabilities include:

- Can act
- Can move
- Can be picked up
- Can pick things up
- Can be equipped
- Can be used as a weapon
- Can own or expose a plane
- Can simulate a contained plane
- Can modify another entity or action

### Spaces Are Real

Inventory spaces, equipment spaces, dungeons, behavior spaces, save files, settings screens, and menus can all be represented as planes or graph spaces.

This means a chest interior, a sword socket, a dungeon inside an overworld, or a behavior space inside a skeleton are not just UI abstractions. They are actual topology in the simulation.

### Equipment Is Containment With Semantics

Equipping an entity should usually mean placing it into a specific functional space owned or exposed by another entity.

For example, a player may have an equipment plane with nodes representing right hand, body, head, or other slots. A sword placed in the right-hand node modifies the player because rules inspect that spatial relationship.

The architecture may provide convenience queries such as `GetEquipped`, but the underlying representation should remain spatial rather than a direct field like `EquippedWeapon`.

### Properties Are Derived When Possible

Entity properties should often be computed from base properties plus contextual modifiers.

For example, a magic gem should not permanently mutate a sword into a different kind of sword. Instead, the effective properties of the sword are derived from the sword, the gem, their relationship, and the current context.

The same object may manifest differently depending on where it is placed:

- Gem in a sword socket: modifies attacks
- Gem equipped to the player: modifies the player
- Gem inside a slime: modifies the slime
- Gem inside a machine: modifies the machine

### Behavior Can Be Data And Space

Entity behavior should be replaceable and eventually representable diegetically.

For example, a skeleton's hostile behavior might be represented by an entity or structure inside a behavior space. Replacing that behavior object with a friendly one should be a plausible gameplay action.

This does not require every behavior to be visual or spatial immediately, but the architecture should not prevent behavior from becoming entity-space data later.

### No Item Versus Creature Distinction

The same entity may be a creature, an item, a weapon, a container, and an actor depending on its capabilities and context.

Examples the architecture should support:

- A player can pick up and equip a sword.
- A slime can move around and pick up objects.
- A player can pick up and equip the slime.
- A player can hit things with the equipped slime.
- A troll can pick up the player and throw them.

### Containment Is Not Ownership

An entity's current location, the owner of a plane, control permissions, and simulation permissions are related but distinct.

If a troll picks up the player, the player is located in the troll's space. That does not necessarily mean the troll permanently owns the player or controls all of the player's capabilities.

### The World Is Not A Tree

Recursive inventory and nested spaces mean containment can be cyclic.

Examples:

- Two entities may contain each other through inventory spaces.
- A save file may contain a world that contains another save file.
- A menu may contain a settings object that affects the menu.

The implementation must use stable IDs and references rather than nested object ownership. Traversals through containment or adjacency must be cycle-safe.

### Spatial Recursion Is Data; Temporal Recursion Is Controlled

Spatial topology may be recursive or cyclic. Turn simulation must not recurse infinitely.

Plane simulation should use explicit scheduling context, stack tracking, and visited sets so a plane cannot take unlimited nested turns through recursive containment.

### UI Is A View Of The Simulation

The simulation core should be headless and independent from any renderer.

ASCII roguelike rendering, inspection panes, menus, config panes, inventory views, and debug graph views should all consume the same underlying model. A main menu or save file may be represented diegetically, but the UI can choose the most useful projection for the player.

## Example Goals

The architecture should eventually be able to support scenarios such as:

- Pick up and equip a sword.
- A slime moves around and picks up objects it finds.
- Pick up and equip the slime, then use it to hit things.
- A slime keeps acting inside a chest while a spike trap inside that chest can attack it.
- Equip a magic gem to a sword to change the sword's properties.
- Equip the same magic gem to yourself for a different manifestation of the same effect.
- Get an enemy troll to pick you up and throw you across a ravine.
- Build Rube Goldberg scenarios inside your own inventory that cause effects outside.
- Treat levels or dungeons as inventory spaces within larger overworld entities.
- Treat overworlds as spaces inside a game file object, which is itself inside a main menu object.
- Represent settings as a diegetic `Sys.Config` object equipped by the main menu.
- Bring a representation of one save file into another and enter it to retrieve tools collected there.
- Treat enemy behaviors and decision trees as nested play spaces that can be modified during gameplay.

## Architectural Direction

The architecture should prefer general systems over specialized categories.

Prefer systems like:

- Containment system
- Spatial query system
- Action system
- Effect system
- Behavior system
- Simulation scheduler

Avoid prematurely splitting the model into unrelated systems like:

- Inventory system
- Equipment system
- Dungeon system
- Menu system
- AI system

Those concepts may exist as rule packages or convenience APIs, but they should be implemented on top of the same entity-space machinery.

## Near-Term Implementation Priorities

Before adding complex gameplay, the next architectural plumbing should likely include:

- Component storage on entities
- Entity-owned planes with purpose labels
- Generic pickup and drop actions
- Equipment represented as planes or nodes, not hardcoded fields
- Capability/rule queries such as `CanPickUp`, `CanEquip`, `CanAct`, and `CanSimulateContainedPlane`
- Cycle-safe traversal helpers
- A deterministic turn scheduler with explicit recursion guards

# GameGameGame

GameGameGame is an experimental turn-based game about exploring nested worlds, manipulating creatures and objects, and learning the consequences of a simulation in which **every inventory is a real place**.

Creatures, items, rooms, equipment, and the player all use the same underlying entity model. What distinguishes them is what they contain, which actions they can take, their physical properties, and how other entities respond to them.

[Play the current systems prototype on itch.io](https://gammapod.itch.io/gamegamegame)

> **Project status:** Active prototype. The game, engine, authoring model, APIs, and content formats are still under development and may change.

![A player moves rats into its own inventory, then enters the inventory of a crate](readme-example.gif)

## What makes GameGameGame unusual

- **Inventories are places.** Entities can own spatial interiors that other entities enter, traverse, and rearrange.
- **Everything follows the same entity model.** A creature, item, room, piece of equipment, or player character differs by capabilities and relationships rather than by a fundamental object category.
- **Creatures act from priorities and relationships.** Authored action plans fall through from one intention to the next, while associations such as love, fear, and hatred determine possible targets.
- **Actions are shared world transformations.** Movement, containment, transfer, creation, destruction, transformation, and behavioral changes are assembled from reusable rules.
- **Conflict is about changing the situation.** The design emphasizes positioning, containment, transformation, theft, escape, and behavioral manipulation rather than conventional health-bar attrition.

## Inventories are places

### In the game

An entity's inventory is not an abstract list or a separate menu. It is a space that other entities can enter, move through, and occupy.

A creature may swallow the player. The player can then explore the creature's interior, interact with whatever else it contains, and look for a way out. Inventories can be nested indefinitely, and interiors can connect to form larger environments. A route may enter through one entity and leave through another.

Because containment is part of the world's topology, unusual arrangements are valid rather than exceptional: nested spaces, loops, self-connected regions, and even an entity ending up inside its own inventory.

### In the engine

Every entity may own zero or one rectangular `Space`, and that space is also its inventory. Each occupiable cell is represented as a node in the simulation graph. Adjacent cells are connected by directional edges, while entering and exiting an entity introduces an additional in/out relationship.

Spaces can be joined topologically into larger layers. Movement, pickup, drop, enter, exit, and transfer therefore operate on related relocation rules instead of separate inventory and map systems.

This model requires stronger ownership, containment, and topology invariants than a conventional flat inventory, but it allows the engine to represent recursive and non-Euclidean arrangements without introducing one-off exceptions for each puzzle or scenario. It is possible for a creature to end up inside its own inventory, and the engine is fine with that.

## Everything follows the same entity model

### In the game

The world does not have separate fundamental categories for creatures, items, terrain, equipment, or the player. Any entity may contain other entities, take actions, be carried, block movement, transform, or be treated differently by something else.

A creature can be picked up. A tool can act. A room can have behavior. This means that familiar roles are consequences of an entity's properties and behavior rather than fixed assumptions built into the game.

### In the engine

All runtime objects share the same entity representation. Their differences emerge from authored properties, owned spaces, action plans, physical values, presentation data, and associations—not from separate creature, item, equipment, and terrain inheritance trees.

Entity lifecycle actions can create, destroy, or polymorph entities. Polymorphing changes template-derived behavior and physical properties while preserving the entity's identity, inventory, and associations. This allows transformation to serve both as a player-facing mechanic and as a lightweight way to author state changes such as growth, metamorphosis, upgrades, or temporary forms.

The uniform model also keeps engine services general: the same topology, targeting, relocation, action, and presentation systems can operate on any entity that satisfies their rules.

## Creatures act from priorities and relationships

### In the game

Creatures do not choose behavior from a single fixed command. They work through an ordered list of intentions and take the first action that is currently possible.

A goblin might use a plan like this:

```text
1. Flee what it fears
2. Pick up what it loves
3. Seek what it loves
4. Wander
```

And separately, the goblin would have associations like:

```text
Goblin fears Trolls
Goblin loves Gems
```

It will then flee Trolls, even if gems are nearby. When no troll can be found, it can instead collect or search for gems. Changing what the goblin fears or loves changes its behavior without replacing the whole creature.

This makes creature behavior inspectable and manipulable. The player can reason about what an entity is trying to do, then change its surroundings, relationships, or available actions to alter the result.

### In the engine

Each acting entity has an authored action plan made from ranked action steps. On its turn, the simulation evaluates the first step. If the step cannot resolve a required target, fails a check, or cannot pay its cost, execution falls through to the next step. The first successful action ends the turn.

Steps that require targets refer to associations rather than hard-coded entity identities. Associations can express roles such as a thing the actor loves, fears, or hates, while runtime targeting resolves an available example of that association within the permitted range. The specific labels like "fears" are arbitrary labels defined by Content.

Action plans and associations can also be overridden or extended. This provides a common foundation for commands, panic, attraction, hostility, temporary conditions, and state-like behavior without requiring a separate AI implementation for every creature.

## Actions are shared world transformations

### In the game

The player and other entities manipulate the world through the same families of actions.

Moving, pushing, picking something up, entering a creature, giving an item away, or forcing something out of a container are all ways of changing where an entity is located. Other actions can create entities, destroy them, transform them, or change how they will behave on later turns.

Actions may also consume physical resources. A machine might create a drone from scrap, a creature might transform after gathering enough biomass, or a wizard might spend stored mana to teleport.

Because these actions share the same world model, authored scenarios can combine them in ways that were not implemented specifically for one puzzle or creature.

### In the engine

Actions are assembled from shared targeting rules, checks, costs, and effects rather than implemented as bespoke command methods on individual entity classes.

Relocation services handle movement between cells, spaces, owners, and containment relationships. Lifecycle effects create, destroy, or polymorph entities. Other effects can replace or append action plans, or temporarily change an entity's associations.

Optional costs are satisfied by matching entities in the actor's inventory and are destroyed when the action succeeds. Resources therefore remain ordinary participants in the same entity and containment model as the rest of the simulation.

The tradeoff is that authored actions require strong validation and structured diagnostics: mistakes that might otherwise be compiler errors must be detected when content is loaded or edited.

## Conflict is about changing the situation

### In the game

GameGameGame is not primarily designed around reducing an opponent's resource pool until it dies. Conflict can instead come from being pushed, trapped, swallowed, robbed, transformed, commanded, separated from an important resource, or placed somewhere dangerous.

The intended challenge is to inspect the world, understand what entities are likely to do, and find a way to change the situation before the consequence occurs. Positioning, containment, escape, transfer, transformation, and behavioral manipulation can all become forms of attack or defense.

### In the engine

Conflict uses ordinary simulation actions and state changes rather than a separate combat subsystem. Threats can be expressed through movement, containment, transfer, lifecycle changes, action-plan overrides, and association changes.

Action evaluation produces structured outcomes and logs that the frontend can eventually project into player-facing choices, explanations, and warnings. Because behavior is represented as explicit plans operating over explicit world state, the same architecture can support intention displays and future predictive tools without inventing a second set of rules for the interface.

## Why I built this

GameGameGame began with a game-design premise: if the player and creatures exist on a grid, and an inventory can also be shown as a grid, why not combine the two spaces?

Following that premise breaks many assumptions that conventional game code can take for granted. A location can belong to a creature. An entity can contain another entity that contains it in return. The player, a room, a tool, and an enemy may all need to participate in the same actions. Transforming an object may need to preserve its contents and relationships.

The architecture grew in response to those problems. The project is an ongoing exercise in domain modelling, simulation design, test-driven development, content authoring, and maintaining clear boundaries between engine rules, tools, and presentation—but those technical concerns serve the game rather than replacing it.

## Current experience

The SadConsole application currently provides two complementary ways to explore the project:

- **Play mode** presents authored scenarios through a player-controlled, turn-based interface.
- **Debug and browser tools** expose scenario state, entities, action choices, diagnostics, and authored content for development and testing.

The repository also contains curated YAML scenarios that exercise individual engine capabilities and larger vertical slices. The current build is best understood as a playable systems prototype and feedback surface rather than a finished game campaign.

## Architecture

```text
YAML scenarios and templates
            │
            ▼
GameGameGame.Content
  loading · validation · editing · materialization
            │
            ▼
GameGameGame.Core
  world state · topology · actions · turns · outcomes
       ┌────┴───────────────┐
       ▼                    ▼
GameGameGame.SadConsole   GameGameGame.Headless
interactive frontend      scenario/report tooling
```

The primary boundary is deliberate: `GameGameGame.Core` does not depend on SadConsole. Content is loaded into runtime models and acted on through shared services; the frontend projects engine state into interaction and presentation models rather than reimplementing game rules.

### Solution layout

| Project | Responsibility |
| --- | --- |
| `src/GameGameGame.Core` | Runtime model, topology, containment, actions, turn execution, history, and gameplay services |
| `src/GameGameGame.Content` | YAML loading, validation, editable documents, scenario materialization, and authoring services |
| `src/GameGameGame.Content.Tools` | Local tooling over the shared content-editing APIs |
| `src/GameGameGame.SadConsole` | Interactive play, debug, and content-browser frontend |
| `src/GameGameGame.Headless` | UI-independent scenario recording and rendering support |
| `src/GameGameGame.Documentation` | Documentation discovery and graph compilation |
| `tests/*` | Engine, content, tooling, documentation, and frontend tests |

### Shared projections and services

The SadConsole layer owns rendering and interaction flow, but action legality and simulation changes remain in Core. Shared projections expose choices, outcomes, logs, entity details, and scenario state.

This adds application-layer types, but it prevents UI code from becoming a second implementation of the game rules and allows other frontends or tools to consume the same simulation services.

## Development approach

Current work is organized as vertical slices. A gameplay concept is promoted through the simulation core, content schema, editor services, tests, authored scenarios, logs, player-facing choices, and frontend presentation together.

The repository includes automated coverage across engine behavior, topology and containment invariants, content loading and validation, authoring services, documentation tooling, scenario support, and frontend interaction logic.

Design and implementation records are connected through source-of-truth documents and a compiled documentation graph. These records make it possible to trace important invariants, current capabilities, tests, and active planning without treating every historical note as equally authoritative.

None of the code has been touched by a human. I define the architecture, requirements, constraints, review process, and development workflow, while coding agents perform the source edits. To see how responsibilities and workflows have been engineered, try the agent definitions in `.opencode/agent` as a starting place.

## Run locally

### Prerequisites

- [.NET SDK 10.0.300](https://dotnet.microsoft.com/)
- A desktop environment supported by MonoGame DesktopGL

The SDK version is pinned in `global.json`.

### Start the application

```bash
dotnet run --project src/GameGameGame.SadConsole/GameGameGame.SadConsole.csproj
```

On Windows, `Start.cmd` provides a convenience launcher.

### Run a specific scenario

```bash
dotnet run --project src/GameGameGame.SadConsole/GameGameGame.SadConsole.csproj -- <content-file> <scenario-id>
```

Example:

```bash
dotnet run --project src/GameGameGame.SadConsole/GameGameGame.SadConsole.csproj -- \
  src/GameGameGame.Content/AlphaScenarioContent.yaml alpha-smoke
```

Curated scenario fixtures live primarily under `src/GameGameGame.Content/Beta`.

## Build and test

```bash
dotnet restore GameGameGame.sln
dotnet build GameGameGame.sln --configuration Release --no-restore
dotnet test GameGameGame.sln --configuration Release --no-build
```

GitHub Actions runs the same restore, Release build, and test sequence on pushes and pull requests to `main`.

## Useful places to start reading

A reviewer interested in the engine can begin with:

- `src/GameGameGame.Core/WorldState.cs` — runtime state ownership
- `src/GameGameGame.Core/ActionPlanInterpreter.cs` — authored-plan execution boundary
- `src/GameGameGame.Core/MovementService.cs` — movement and relocation rules
- `src/GameGameGame.Core/TopologyService.cs` — spatial relationships
- `src/GameGameGame.Core/ActionChoiceService.cs` — legal player-facing choices
- `src/GameGameGame.Core/SimulationHistorySession.cs` — simulation history and rollback boundary

For content and frontend integration:

- `src/GameGameGame.Content` — YAML documents, validation, and materialization
- `src/GameGameGame.SadConsole/Ui` — presentation and interaction components
- `tests/GameGameGame.Tests` — behavioral examples and invariants
- `docs/Source of Truth/invariants.md` — maintained behavior contracts and test traces
- `docs/Source of Truth/vertical-slice-map.md` — cross-layer navigation

## Current limitations

- This is an evolving engine rather than a finished game.
- The current frontend is primarily a development, demonstration, and feedback surface.
- The content schema and public APIs are not yet stable.
- Setup and packaging are presently oriented toward desktop Windows builds.
- Some player-facing ideas described above remain design direction rather than implemented features.
- Some documentation records active design work and may be more detailed than external readers need.

## Roadmap

Near-term work focuses on turning individual simulation capabilities into complete, player-facing vertical slices. Current directions include:

- expanding connected and merged spaces;
- promoting more interactions through the canonical action and action-choice pipeline;
- improving the presentation of available actions, entity intentions, outcomes, and risks;
- extending authoring and validation tools;
- combining isolated mechanics into more complete playable scenarios.

Detailed priorities and implementation plans are documented in:

- `docs/Source of Truth/Current-Goals.md`
- `docs/Plans/High-Level-Roadmap.md`
- `docs/Source of Truth/planning-index.md`

## Attribution

GameGameGame is a personal project designed and coordinated by Ben (`Gammapod`). It uses:

- [SadConsole](https://sadconsole.com/) and MonoGame for the desktop frontend
- [YamlDotNet](https://github.com/aaubry/YamlDotNet) for YAML serialization
- [xUnit](https://xunit.net/) for automated tests
- [ImageSharp](https://sixlabors.com/products/imagesharp/) for image-related tooling

## License

No open-source license has been selected yet. The source is publicly viewable, but reuse rights are not granted unless a license is added.

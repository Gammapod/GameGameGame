# GameGameGame

GameGameGame is a .NET game prototype with a shared engine, YAML-backed content model, console gameplay shell, and Avalonia content editor.

## Projects

- `src/GameGameGame.Core` — engine/runtime model and gameplay services.
- `src/GameGameGame.Content` — content loading, editable content documents, validation, prototype content, and editor service operations.
- `src/GameGameGame.Console` — console gameplay shell for exercising the prototype runtime.
- `src/GameGameGame.Editor` — Avalonia editor for authoring and validating content.
- `tests/GameGameGame.Tests` — xUnit coverage for core behavior, content loading/validation, editor services, and editor view-model behavior.

## Current engine capabilities

The engine currently supports:

- entity templates and runtime entities;
- world planes, grid coordinates, inventory planes, and carried entities;
- entity presentation data such as display names, glyphs, and colors;
- inventory dimensions, entity weight, carrying capacity, and carried entity layout;
- movement and relocation through `Move`, `Pickup`, `Drop`, and `Teleport` behavior;
- turn execution through action plans;
- action-plan checks including `CanMove`, `BlockingEntity`, and `CanPickup`;
- action-plan effects including `Wait`, `Move`, `Pickup`, `ReverseDirection`, `CallPlan`, `Teleport`, and `Drop`;
- canonical actor state defaults such as initial facing;
- trace records for action evaluation and failures.

## Current content system

Content is YAML-backed and lives primarily in `src/GameGameGame.Content`.

Current content tooling includes:

- loading prototype content from YAML;
- editable content documents;
- content editor sessions;
- entity template creation, duplication, update, and deletion;
- action-plan creation, duplication, update, and deletion;
- action-plan step, check, and effect editing;
- inventory placement and validation helpers;
- YAML preview, save, reload, and validation support;
- structured diagnostics for many content validation issues.

## Current editor

The Avalonia editor can currently:

- create, open, save, and reload content documents;
- edit entity templates and presentation fields;
- edit inventory dimensions, weight, carrying capacity, and carried entities;
- assign and clear default action plans;
- edit initial actor facing;
- create, edit, delete, and reorder action plans and steps;
- author supported checks and effects;
- edit movement target/destination fields for advanced movement effects;
- surface validation diagnostics.

## Current console prototype

The console app runs the current prototype content and provides keyboard-driven gameplay for movement, pickup, drop, and inspection.

Basic controls shown by the app include:

- arrow keys to move;
- `P` to pick up;
- `D` to drop;
- `I` to inspect;
- `Q` or `Esc` to quit.

## Run and author scenarios

Run the default console prototype:

```bash
dotnet run --project src/GameGameGame.Console/GameGameGame.Console.csproj
```

Run a specific authored scenario from a content YAML file:

```bash
dotnet run --project src/GameGameGame.Console/GameGameGame.Console.csproj -- <content-file> <scenario-id>
```

Example:

```bash
dotnet run --project src/GameGameGame.Console/GameGameGame.Console.csproj -- src/GameGameGame.Content/AlphaScenarioContent.yaml alpha-smoke
```

Record a scenario to PNG frames and a GIF:

```bash
dotnet run --project src/GameGameGame.Console/GameGameGame.Console.csproj -- record-scenario <content-file> <scenario-id> --turns <N> --output <directory>
```

Example:

```bash
dotnet run --project src/GameGameGame.Console/GameGameGame.Console.csproj -- record-scenario src/GameGameGame.Content/AlphaScenarioContent.yaml alpha-smoke --turns 5 --output artifacts/scenario-recordings/alpha-smoke
```

Open the current Avalonia editor for content and scenario authoring:

```bash
dotnet run --project src/GameGameGame.Editor/GameGameGame.Editor.csproj
```

Content is YAML-backed and primarily lives under `src/GameGameGame.Content`. Current beta scenario fixtures are organized under `src/GameGameGame.Content/Beta`.

## Build and test

This repository targets .NET 10. Build the main runnable projects directly:

```bash
dotnet build src/GameGameGame.Console/GameGameGame.Console.csproj
dotnet build src/GameGameGame.Editor/GameGameGame.Editor.csproj
```

Run the normal non-Editor test project for Core, Content, Console, and Headless coverage:

```bash
dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj
```

Run legacy/current Avalonia editor-specific tests separately:

```bash
dotnet test tests/GameGameGame.Editor.Tests/GameGameGame.Editor.Tests.csproj
```

## Documentation

The current engine/editor capability reference is maintained at:

- `docs/Source of Truth/Engine-Editor-Capabilities.md`

Other design notes, invariants, testing guidance, and implementation plans are under `docs/`.

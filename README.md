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

## Build and test

This repository uses a `.slnx` solution file and targets .NET 10.

```bash
dotnet build GameGameGame.slnx
dotnet test GameGameGame.slnx
```

Run the console prototype:

```bash
dotnet run --project src/GameGameGame.Console/GameGameGame.Console.csproj
```

Run the editor:

```bash
dotnet run --project src/GameGameGame.Editor/GameGameGame.Editor.csproj
```

## Documentation

The current engine/editor capability reference is maintained at:

- `docs/Source of Truth/Engine-Editor-Capabilities.md`

Other design notes, invariants, testing guidance, and implementation plans are under `docs/`.

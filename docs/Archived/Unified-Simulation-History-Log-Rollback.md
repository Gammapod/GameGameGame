# Unified Simulation History / Log / Rollback Sprint

Status: Completed / archived during sprint cleanup.

This sprint centralized SadConsole/player-facing game history enough that durable state, rollback, and logs no longer live in frontend-owned controlled-command lists.

## Completed scope

1. Added `WorldState` clone/restore support for entities, planes, nodes, occupancy, inventory planes, action states, coordinate lookup integrity, turn number, and visible trace/turn-report context.
2. Added `SimulationHistorySession` with frame 0 snapshots, successful controlled-command intervals, failed current-frame command entries, non-controlled/headless actor intervals, previous-frame rollback, and future-frame/interval/log discard behavior.
3. Routed SadConsole controlled actions through shared history and made SadConsole rebuild logs from `ActionLogProjection.FromHistory(...)`.
4. Added SadConsole `U undo` over shared Core rollback while keeping frontend state presentation-only.
5. Centralized per-actor plan resolution in Core through `ActorTurnResolver`, used by `TurnService`, headless scenario runs, and scenario recording.
6. Added structured action-step attempt projection from canonical behavior-chain traces.
7. Generalized history-backed structured outcome/log projection to include controlled outcomes, failed current-frame commands, and conservative autonomous actor rows with consumed-turn facts and extracted action-step attempts.
8. Updated SadConsole global/local action-log presentation to show controlled and autonomous success/failure rows from shared projection.
9. Migrated headless scenario run reports to record shared history intervals while preserving the content-editor-facing report shape.
10. Deprecated deeper investment in the legacy PNG/GIF `ScenarioRecordingService`; the preferred future path is history playback / SadConsole-rendered recording export.

## Validation and review

- Main tests passed after the sprint: 380.
- SadConsole tests passed after the sprint: 24.
- Editor tests passed after the sprint: 72.
- Content-editor reviewed the migrated scenario reports and approved with no blockers.

## Remaining follow-ups

- Richer autonomous target/source/destination anchors for structured outcomes.
- Saved runlog / playback artifact design.
- History playback / SadConsole-rendered recording export.
- Optional report polish: expose structured action-step attempts/outcomes to editor/API consumers without requiring trace-string parsing.

# Sprint 19: Gate 4 Peer Transfer Showcases

Status: Completed / archived during Sprint 19 wrap-up.

Related source of truth and active plans:

- `docs/Source of Truth/Content-Authoring-Manual.md`
- `docs/Source of Truth/Engine-Editor-Capabilities.md`
- `docs/Source of Truth/invariants.md`
- `docs/Plans/High-Level-Roadmap.md`
- `docs/Plans/Beta-Content-Exploration-Plan.md`
- `docs/Source of Truth/Capability-Gap-Log.md`
- `docs/Source of Truth/Design-Quirks-and-Gotchas.md`

## Completion summary

Sprint 19 completed the first Gate 4 peer-transfer slice and authored four transfer-focused beta showcases.

Implemented canonical Action Steps:

- `GiveTarget`
- `TakeTarget`

Authoring semantics now supported:

- transfer the actor's first carried entity into the current `Target` inventory;
- transfer the current `Target`'s first carried entity into actor inventory;
- deterministic first-item/source selection;
- deterministic row-major destination placement;
- success consumes the turn;
- failure falls through without consuming the transfer step;
- ordinary carried entities, including player/actor entities, can be transferred when existing inventory rules allow it.

Deferred transfer follow-ups:

- authorable item/source selection rules;
- target filters;
- transfer permissions/restrictions;
- barter/trade semantics;
- richer inventory/containment report summaries.

## Showcase content completed

Sprint 19 added beta transfer content under:

- `src/GameGameGame.Content/Beta/Transfer/`

Completed showcases:

1. `beta-passive-chest-transfer`
   - File: `src/GameGameGame.Content/Beta/Transfer/PassiveChestTransferShowcase.yaml`
   - Demonstrates a runner giving an offering to a passive chest and then taking a coin from it.

2. `beta-stealing-actor`
   - File: `src/GameGameGame.Content/Beta/Transfer/StealingActorShowcase.yaml`
   - Demonstrates an actor acquiring a target, approaching it, and taking a carried item.

3. `beta-feeding-offering`
   - File: `src/GameGameGame.Content/Beta/Transfer/FeedingOfferingShowcase.yaml`
   - Demonstrates an offering bearer giving food to a recipient entity as a precursor to future reaction/state-change work.

4. `beta-collector-trader-handoff`
   - File: `src/GameGameGame.Content/Beta/Transfer/CollectorTraderHandoffShowcase.yaml`
   - Demonstrates the flagship composition: Collector picks up the player, gives the player to Trader, and Trader drops the player.

Deferred showcase:

- `beta-trade-transfer`
  - Deferred because the completed showcase set already proves the first transfer slice. True trade/barter semantics remain future work; a simple peer exchange vignette can be authored later if needed.

## Validation and artifacts

Targeted validation performed during the sprint:

```text
dotnet test tests\GameGameGame.Tests\GameGameGame.Tests.csproj --filter "FullyQualifiedName~BetaContentFixtureTests" -m:1 --no-restore
```

Result at wrap-up:

```text
Passed: 24/24
```

Scenario recordings were generated for completed transfer showcases under:

- `artifacts/scenario-recordings/beta-passive-chest-transfer/`
- `artifacts/scenario-recordings/beta-stealing-actor/`
- `artifacts/scenario-recordings/beta-feeding-offering/`
- `artifacts/scenario-recordings/beta-collector-trader-handoff/`

## Findings

- Gate 4 transfer primitives compose cleanly with existing targeting, seeking, pickup, and drop behavior.
- Sparse layouts remain the preferred workaround while `AcquireNearestTarget` has no authorable filters.
- Existing final-state reports still do not summarize nested inventory/containment richly; tests often verify transfer outcomes through trace lines or direct materialized-world inspection.
- The interactive Console renderer can require a taller terminal for taller scenario roots because the local turn-order report is drawn below the inventory grid. Enlarging the terminal is an acceptable workaround for now.
- Continuing `beta-collector-trader-handoff` after the intended endpoint produced surprising recursive/continued transfer behavior. This was recorded as a design quirk, not a gap, in `docs/Source of Truth/Design-Quirks-and-Gotchas.md`.

## Follow-up candidates

- Revisit `beta-trade-transfer` only if a simple exchange vignette adds new evidence beyond the completed transfer set.
- Consider inventory/containment report summaries if future transfer or containment-heavy scenarios remain hard to inspect.
- Consider action-specific transfer guardrails later for ordinary verbs, without globally forbidding recursive spaces.
- Continue toward Gate 5 template spawning when beta content needs authored spawned entities rather than placeholder `CreateFacing` output.

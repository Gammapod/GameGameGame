# Ecology Baseline Metrics

Status: Archived baseline planning artifact captured after the engine refactor, before selecting the next ecology sprint. Active ecology follow-up is tracked through the Content rolling board.

Source content: `src/GameGameGame.Content/Beta/Ecology/EcologyVignettes.yaml`

Regression test: `tests/GameGameGame.Tests/EcologyVignetteBaselineMetricsTests.cs`

## Method

The current gallery rooms include an `Ecology Observer` with `controller: Player` for manual/SadConsole play. Persisted headless runs stop at the first player-choice actor, so the baseline test clears those observer controllers in memory only and then runs the existing persisted scenarios for 10, 25, and 50 turns.

Counts include visible scenario-plane entities plus any entities carried in top-level inventories. Runs reported no validation diagnostics, runtime failures, or capability gaps.

## Baseline counts

### `ecology-glowcap-grubarium`

| Entity | Turn 10 | Turn 25 | Turn 50 |
|---|---:|---:|---:|
| Ecology Observer | 1 | 1 | 1 |
| Glowcap Fungus | 3 | 3 | 3 |
| Glowcap Spore | 15 | 15 | 15 |
| Cave Grub | 3 | 3 | 3 |
| Duskwing Bat | 1 | 1 | 1 |

Classification: **clogged/static plateau**. The producers fill available space quickly, after which counts remain unchanged and runtime observations accumulate from failed/falling-through actions. The system remains populated but is not demonstrating active closed-loop ecology at 25/50 turns.

### `ecology-mana-crystal-automata`

| Entity | Turn 10 | Turn 25 | Turn 50 |
|---|---:|---:|---:|
| Ecology Observer | 1 | 1 | 1 |
| Mana Crystal | 2 | 2 | 2 |
| Mana Spark | 14 | 14 | 14 |
| Tiny Construct | 2 | 2 | 2 |
| Mana Leech | 2 | 2 | 2 |

Classification: **clogged/static plateau**. Mana sparks saturate the room and no later population movement is visible in aggregate counts. This is currently a resource-filled exhibit rather than a live regulator loop.

### `ecology-goblin-coin-table`

| Entity | Turn 10 | Turn 25 | Turn 50 |
|---|---:|---:|---:|
| Ecology Observer | 1 | 1 | 1 |
| Coin Fountain | 2 | 2 | 2 |
| Coin | 13 | 13 | 13 |
| Coin Goblin | 2 | 2 | 2 |
| Goblin Thief | 1 | 1 | 1 |

Classification: **clogged/static plateau**. Coin production reaches the room-space limit and the social/economic loop does not produce visible aggregate change after saturation.

## Planning implications

- The existing ecology gallery is valid and runnable, but the current autonomous baseline is mostly space-saturation, not stable active ecology.
- Future ecology planning should treat "stays populated for 50 turns" as insufficient; a useful ecology metric should also ask whether material is still being converted, moved, consumed, or recycled after saturation.
- The baseline strengthens the case for authorable pacing and local-density controls before adding larger biomes: cooldowns would slow sources, and density/count gates would let producers or reproducers stop before filling all open cells.

# Beta Design Quirks and Gotchas

Status: Active reference log for unexpected-but-not-necessarily-wrong beta behavior, not an active implementation plan. Use as design context only while the SadConsole frontend roadmap is the implementation priority.

Read when:

- reviewing surprising beta scenario behavior that may be useful design material;
- deciding whether an observed behavior is a capability gap, a bug, or an undocumented emergent capability;
- collecting future gameplay seeds discovered during authored vignette testing.

Related plans and source of truth:

- `docs/Source of Truth/Content-Authoring-Manual.md` remains the source of truth for currently authorable content workflows.
- `docs/Plans/Beta-Capability-Gap-Log.md` records missing capabilities, blockers, reporting needs, and authoring friction.
- `docs/Plans/Beta-Content-Exploration-Plan.md` records paused beta vignette ordering and gate context.

## Purpose

This log is the sister document to the beta capability gap log. Use it for behavior that is surprising, emergent, undocumented, or potentially confusing, but not necessarily wrong and not necessarily a missing capability.

Prefer this log over the gap log when the observation is better framed as:

- an emergent behavior worth preserving;
- a potential author/player gotcha;
- a currently-undocumented capability;
- a gameplay seed for later design;
- a behavior that may need action-specific guardrails later without becoming a global engine prohibition.

Do not use this log for clear blockers, missing authoring support, crashes, or required reporting/tooling improvements. Those belong in `Beta-Capability-Gap-Log.md`.

## Observations

### QUIRK-001: Recursive containment can emerge from peer transfer chains

- **Discovered in:** Sprint 19 Gate 4 transfer showcase testing.
- **Scenario/content:** `src/GameGameGame.Content/Beta/Transfer/CollectorTraderHandoffShowcase.yaml`, scenario `beta-collector-trader-handoff`.
- **Observed behavior:** The intended turn-6 sequence works: Collector picks up the player, gives the player to Trader, and Trader drops the player. Continuing the simulation produces additional emergent behavior: the Collector can pick up the Trader, hand the Trader to the player, and later produce a surprising recursive/self-containing result involving previous target state and transfer behavior.
- **Why this is not a gap:** The simulation remains stable, and recursive spaces are not intended to be globally forbidden at the engine level.
- **Design interpretation:** Recursive or self-containing spaces may be useful as weird spatial topology rather than invalid state. Ordinary `GiveTarget`/`TakeTarget`-style verbs may eventually need action-specific guardrails so common interactions make sense, but the engine should not necessarily prevent all containment cycles.
- **Potential gameplay seeds:** Pocket dimensions, impossible containers, bonus rooms, stores inside objects, one-way exits, recursive dungeons, and non-Euclidean inventory spaces.
- **Gotcha for authors:** Current first-pass transfer primitives have no role filters, target filters, transfer restrictions, or objective-completion stop condition. Linear chains may continue past the intended vignette endpoint and create surprising additional transfers.

### QUIRK-002: Inventory spaces can be treated as disconnected spatial graph regions until explicit links exist

- **Discovered in:** Design discussion following `beta-collector-trader-handoff`.
- **Observed behavior/design framing:** Inventory spaces are currently separate planes disconnected from the rest of the spatial graph except through containment/action semantics. Future portal-like capabilities could add explicit graph edges between planes, giving actors alternate means to travel between inventory spaces.
- **Why this is not a gap:** Disconnected inventory spaces are a useful baseline. The interesting future capability is not simply “make spaces connected,” but author explicit cross-space topology when a scenario wants it.
- **Design interpretation:** Pocket dimensions can be modeled as inventory spaces plus explicit travel links. Portals, one-way exits, doors between inventory planes, or other plane-to-plane edges can turn containment into navigable topology.
- **Potential gameplay seeds:** Player-created pocket dimensions, hidden shops, bonus rooms in objects, containers with one-way exits, portable rooms, recursive stores, and authored shortcuts between otherwise disconnected spaces.
- **Future discussion:** Portal/travel tools should distinguish normal containment transfer from navigable graph edges. This may let content authors choose when an inventory is merely storage, when it is a room, and when it is part of a larger non-Euclidean map.

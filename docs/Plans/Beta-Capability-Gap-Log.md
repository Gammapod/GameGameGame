# Beta Capability Gap Log

Status: Active beta exploration log.

Read when:

- deciding whether a beta vignette should request engine, editor/API, reporting, or Console/frontend work;
- reviewing why a showcase is headless-only, partially playable, or intentionally blocked;
- promoting repeated scenario pressure into an implementation plan.

Related plans:

- `docs/Plans/Beta-Content-Exploration-Plan.md`
- `docs/Plans/Sprint-12-Beta-Primitive-Showcases.md`
- `docs/Source of Truth/Engine-Editor-Capabilities.md`

## Open gaps

### GAP-001: `CreateFacing` placeholder entities lack content-template/presentation assignment

- **Discovered in:** Sprint 12 `beta-create-showcase`.
- **Scenario/content:** `src/GameGameGame.Content/Beta/CurrentTools/CreateFacingShowcase.yaml`, scenario `beta-create-showcase`.
- **Desired behavior:** A successful `CreateFacing` action should create an entity that remains renderable/inspectable in Console and future frontends.
- **Current behavior:** Headless scenario reports can observe the created `Placeholder Rock`, but Console rendering/inspection can crash after the action because the runtime-created `placeholderRock` entity has no content-template assignment in `PrototypeContentRegistry`.
- **Current workaround:** Treat successful `CreateFacing` as headless-only for Sprint 12 manual testing. Console-safe creation demos must avoid successful creation, which does not showcase the primitive.
- **Missing capability:** Runtime-created entities need content presentation/template binding, or `CreateFacing` should become template-backed, such as `CreateFacing(templateId)` / `SpawnTemplateFacing`.
- **Unlocks:** Console-playable create/spawner showcases; authored builders; authored traps/bombs/projectiles; summons/clones; future template-spawning vignettes.
- **Classification:** New engine/content integration capability; also a Console/frontend stability gap.
- **Priority:** Medium-high for beta if creation/spawning remains in the current-tool showcase set; otherwise promote at the template-spawning gate.

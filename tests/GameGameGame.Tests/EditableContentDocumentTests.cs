using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class EditableContentDocumentTests
{
    [Fact]
    public void EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              rock:
                name: Rock
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 3
                carryingCapacity: 3
                material: stone
            presentations:
              rock:
                glyph: '*'
                color: Earth
            actionPlans:
              wait:
                id: wait
                steps:
                  - label: wait
                    checks: []
                    onSuccess:
                      kind: Wait
            """);

        var registry = document.ToRegistry();
        Assert.True(registry.Validate().IsValid);
        Assert.Equal("Rock", registry.EntityTemplates[new EntityTemplateId("rock")].Name);

        var saved = document.SaveYaml();
        var reloaded = EditableContentDocument.LoadYaml(saved).ToRegistry();

        Assert.True(reloaded.Validate().IsValid);
        Assert.Equal("Rock", reloaded.EntityTemplates[new EntityTemplateId("rock")].Name);
        Assert.Equal(new EntityMaterial("stone"), reloaded.EntityTemplates[new EntityTemplateId("rock")].Material);
        Assert.Contains("material: stone", saved);
        Assert.Equal('*', reloaded.Presentations[new EntityTemplateId("rock")].Glyph);
        Assert.Equal(PlanEffectKind.Wait, reloaded.ActionPlanDescriptors[new ActionPlanTemplateId("wait")].Steps.Single().OnSuccess!.Kind);
    }

    [Fact]
    public void EditableContentDocumentLoadYamlRemainsPermissiveForUnknownProperties()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              rock:
                name: Rock
                inventoryWidht: 99
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 3
                aperture: 3
            presentations:
              rock:
                glyph: '*'
                color: Earth
            actionPlans: {}
            """);

        var registry = document.ToRegistry();

        Assert.Equal(0, registry.EntityTemplates[new EntityTemplateId("rock")].InventoryWidth);
    }

    [Fact]
    public void CanonicalAuthoringValidationReportsUnknownYamlPropertiesWithSuggestionsAndCollectsAll()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              actor:
                name: Actor
                inventoryWidht: 2
                inventoryWidth: 1
                inventoryHeight: 1
                bulk: 1
                aperture: 1
                targeting:
                  range: 4
                  rules:
                  - slot: 1
                    label: prey
                    targetCapabilites: [DestroyTarget]
                    targetCapabilities: [DestroyTarget]
            presentations:
              actor: { glyph: A, color: Cyan }
            actionPlans:
              hunt:
                behavior:
                  steps:
                  - kind: TargetPathMove
                    targetLable: prey
                    targetLabel: prey
                    pathMode: SeekAdjacency
            scenarios:
              typoStart:
                name: Typo Start
                scenarioRootEntityTemplateId: actor
                playerStart: { x: 0, yy: 0 }
            """);

        var diagnostics = document.ValidateCanonicalAuthoring().Diagnostics
            .Where(diagnostic => diagnostic.Code == ContentDiagnosticCode.UnknownYamlProperty)
            .ToList();

        Assert.Contains(diagnostics, diagnostic => diagnostic.Message.Contains("entityTemplates.actor.inventoryWidht") && diagnostic.Message.Contains("inventoryWidth"));
        Assert.Contains(diagnostics, diagnostic => diagnostic.Message.Contains("entityTemplates.actor.targeting.rules[0].targetCapabilites") && diagnostic.Message.Contains("targetCapabilities"));
        Assert.Contains(diagnostics, diagnostic => diagnostic.Message.Contains("actionPlans.hunt.behavior.steps[0].targetLable") && diagnostic.Message.Contains("targetLabel"));
        Assert.Contains(diagnostics, diagnostic => diagnostic.Message.Contains("scenarios.typoStart.playerStart.yy") && diagnostic.Message.Contains("y"));
        Assert.Equal(4, diagnostics.Count);
    }

    [Fact]
    public void EditableContentDocumentRoundTripsMergedInventoryLayers()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              room:
                name: Room
                inventoryWidth: 3
                inventoryHeight: 3
                bulk: 10
                aperture: 10
                carriedEntities:
                - entityId: entityA
                  templateId: spaceA
                  coord: { x: 0, y: 0 }
                - entityId: entityB
                  templateId: spaceB
                  coord: { x: 1, y: 0 }
              spaceA:
                name: Space A
                inventoryWidth: 3
                inventoryHeight: 3
                bulk: 1
                aperture: 10
              spaceB:
                name: Space B
                inventoryWidth: 2
                inventoryHeight: 2
                bulk: 1
                aperture: 10
            presentations:
              room: { glyph: R, color: Gray }
              spaceA: { glyph: A, color: Cyan }
              spaceB: { glyph: B, color: Green }
            mergedLayers:
              sharedInterior:
                spaces:
                - owner: entityA
                  origin: { x: 0, y: 0 }
                - owner: entityB
                  origin: { x: 3, y: 0 }
            actionPlans: {}
            """);

        var saved = document.SaveYaml();
        var reloaded = EditableContentDocument.LoadYaml(saved).ToRegistry();

        Assert.Contains("mergedLayers:", saved);
        var layer = Assert.Single(reloaded.MergedInventoryLayers).Value;
        Assert.Equal(new MergedInventoryLayerId("sharedInterior"), layer.Id);
        Assert.Equal(new GridCoord(3, 0), layer.Spaces[1].Origin);
    }

    [Fact]
    public void MergedInventoryLayerDocumentMapperRoundTripsCurrentTopologyDtoShape()
    {
        var layer = new MergedInventoryLayerDefinition(
            new MergedInventoryLayerId("sharedInterior"),
            [
                new MergedInventorySpaceContribution(new EntityId("entityA"), new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(new EntityId("entityB"), new GridCoord(3, 1))
            ],
            [
                new MergedInventoryAlignedJoin(
                    new MergedInventoryJoinEndpoint(new EntityId("entityA"), Direction.East),
                    new MergedInventoryJoinEndpoint(new EntityId("entityB"), Direction.West),
                    MergedInventoryJoinAlignment.Center)
            ]);

        var dto = MergedInventoryLayerDocumentMapper.ToDto(layer);
        var roundTripped = MergedInventoryLayerDocumentMapper.ToDefinition(layer.Id, dto);

        Assert.Equal("entityA", dto.Spaces![0].Owner);
        Assert.Equal(3, dto.Spaces[1].Origin!.X);
        Assert.Equal(1, dto.Spaces[1].Origin!.Y);
        Assert.Equal(layer.Id, roundTripped.Id);
        Assert.Equal(layer.Spaces, roundTripped.Spaces);
        var dtoJoin = Assert.Single(dto.Joins!);
        Assert.Equal("entityA", dtoJoin.From!.Owner);
        Assert.Equal(Direction.East, dtoJoin.From.Edge);
        Assert.Equal("entityB", dtoJoin.To!.Owner);
        Assert.Equal(Direction.West, dtoJoin.To.Edge);
        Assert.Equal(MergedInventoryJoinAlignment.Center, dtoJoin.Align);
        Assert.Equal(layer.Joins, roundTripped.Joins);
    }

    [Fact]
    public void EditableContentDocumentCanCreateEntityTemplateWithGeneratedStableId()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              rock:
                name: Rock
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 3
                carryingCapacity: 3
            presentations:
              rock:
                glyph: '*'
                color: Earth
            actionPlans: {}
            """);

        var id = document.AddEntityTemplate(
            "Giant Slime",
            new EntityTemplate(
                "Giant Slime",
                InventoryWidth: 3,
                InventoryHeight: 3,
                Bulk: 20,
                Aperture: 20),
            new EntityPresentation('S', PresentationColor.DarkGreen));

        var registry = EditableContentDocument.LoadYaml(document.SaveYaml()).ToRegistry();

        Assert.Equal(new EntityTemplateId("giantSlime"), id);
        Assert.Equal("Giant Slime", registry.EntityTemplates[id].Name);
        Assert.Equal('S', registry.Presentations[id].Glyph);
    }

    [Fact]
    public void EditableContentDocumentRoundTripsEnterAndExitPolicies()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              room:
                name: Room
                inventoryWidth: 3
                inventoryHeight: 3
                bulk: 1
                aperture: 10
                enterPolicy: FarthestFromOccupied
                exitPolicy: EdgeAlignedWithExitDirection
            presentations:
              room:
                glyph: R
                color: Cyan
            actionPlans: {}
            """);

        var reloaded = EditableContentDocument.LoadYaml(document.SaveYaml()).ToRegistry();
        var template = reloaded.EntityTemplates[new EntityTemplateId("room")];

        Assert.Equal(EntityEnterPolicy.FarthestFromOccupied, template.EnterPolicy);
        Assert.Equal(EntityExitPolicy.EdgeAlignedWithExitDirection, template.ExitPolicy);
    }

    [Fact]
    public void EditableContentDocumentRoundTripsPresentationAndPaletteCatalogs()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            presentationCatalog:
              creature.moth:
                name: Moth
                fallbackText: m
                tags: [creature, insect]
            palettes:
              creature.moth.default:
                name: Moth Default
                roles:
                  primary: Gray
                  accent: Yellow
            entityTemplates:
              moth:
                name: Moth
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 1
            presentations:
              moth:
                presentationId: creature.moth
                paletteId: creature.moth.default
                glyph: m
                color: Gray
            actionPlans: {}
            """);

        var reloaded = EditableContentDocument.LoadYaml(document.SaveYaml()).ToRegistry();

        Assert.True(reloaded.Validate().IsValid, string.Join(Environment.NewLine, reloaded.Validate().Errors));
        Assert.Contains(new PresentationId("creature.moth"), reloaded.PresentationCatalog.Keys);
        Assert.Contains(new PaletteId("creature.moth.default"), reloaded.PaletteCatalog.Keys);
    }

    [Fact]
    public void EditableContentDocumentRoundTripsCreateEntityAndPolymorphTargetFields()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              rat:
                name: Rat
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 1
              egg:
                name: Egg
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 1
            presentations:
              rat:
                glyph: r
                color: Gray
              egg:
                glyph: e
                color: Yellow
            actionPlans:
              lifecycle:
                id: lifecycle
                behavior:
                  steps:
                    - kind: CreateEntity
                      templateId: rat
                      createPlacement: Facing
                      directionMode: East
                    - kind: PolymorphTarget
                      targetSelf: true
                      templateId: egg
            """);

        var reloaded = EditableContentDocument.LoadYaml(document.SaveYaml()).ToRegistry();
        var steps = reloaded.ActionPlanDescriptors[new ActionPlanTemplateId("lifecycle")].Behavior!.Steps;

        Assert.Equal("rat", steps[0].TemplateId);
        Assert.Equal(CreateEntityPlacement.Facing, steps[0].CreatePlacement);
        Assert.Equal(ActionPlanMoveDirectionMode.East, steps[0].DirectionMode);
        Assert.Equal("egg", steps[1].TemplateId);
        Assert.True(steps[1].TargetSelf);
    }

    [Fact]
    public void EditableContentDocumentRoundTripsBehaviorStepCosts()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              scrap:
                name: Scrap
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 1
            presentations:
              scrap:
                glyph: s
                color: Gray
            actionPlans:
              costlyMove:
                id: costlyMove
                behavior:
                  steps:
                    - kind: MoveFacing
                      costs:
                        - templateId: scrap
                          quantity: 3
            """);

        var saved = document.SaveYaml();
        var reloaded = EditableContentDocument.LoadYaml(saved).ToRegistry();
        var cost = Assert.Single(reloaded.ActionPlanDescriptors[new ActionPlanTemplateId("costlyMove")].Behavior!.Steps.Single().Costs);

        Assert.Contains("costs:", saved);
        Assert.Equal("scrap", cost.TemplateId);
        Assert.Equal(3, cost.Quantity);
    }

    [Fact]
    public void EditableContentDocumentRoundTripsTargetPathMoveFields()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              orbitPlan:
                id: orbitPlan
                behavior:
                  steps:
                    - kind: TargetPathMove
                      targetLabel: enemy
                      pathMode: Orbit
                      desiredDistance: 6
                      orbitDirection: Anticlockwise
            """);

        var saved = document.SaveYaml();
        var reloaded = EditableContentDocument.LoadYaml(saved).ToRegistry();
        var step = reloaded.ActionPlanDescriptors[new ActionPlanTemplateId("orbitPlan")].Behavior!.Steps.Single();

        Assert.Contains("pathMode: Orbit", saved);
        Assert.Contains("desiredDistance: 6", saved);
        Assert.Contains("orbitDirection: Anticlockwise", saved);
        Assert.Equal(ActionPlanTargetPathMode.Orbit, step.PathMode);
        Assert.Equal(6, step.DesiredDistance);
        Assert.Equal(ActionPlanOrbitDirection.Anticlockwise, step.OrbitDirection);
    }

    [Fact]
    public void EditableContentDocumentCanonicalizesLegacyActionPlanVariableFieldsOnSave()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: wandering
                defaultPlanVariables:
                  facing:
                    kind: Direction
                    directionValue: West
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              wandering:
                id: wandering
                steps:
                  - label: move facing
                    checks:
                      - kind: CanMove
                        directionVariable: facing
                    onSuccess:
                      kind: Move
                      directionVariable: facing
                  - label: handle blocker
                    checks:
                      - kind: BlockingEntity
                        directionVariable: facing
                        targetVariable: target
                    onSuccess:
                      kind: Pickup
                      targetVariable: target
                      inventoryCoord:
                        x: 0
                        y: 0
                    onFailure:
                      kind: ReverseDirection
                      directionVariable: facing
                      consumesTurn: false
                      continuePlan: true
            """);

        var saved = document.SaveYaml();
        var reloaded = EditableContentDocument.LoadYaml(saved).ToRegistry();
        var descriptor = reloaded.GetActionPlanDescriptor(new ActionPlanTemplateId("wandering"));

        Assert.DoesNotContain("directionVariable", saved);
        Assert.DoesNotContain("targetVariable", saved);
        Assert.True(reloaded.Validate().IsValid);
        Assert.Null(descriptor.Steps[0].Checks.Single().DirectionVariable);
        Assert.Null(descriptor.Steps[0].OnSuccess!.DirectionVariable);
        Assert.Null(descriptor.Steps[1].Checks.Single().DirectionVariable);
        Assert.Null(descriptor.Steps[1].Checks.Single().TargetVariable);
        Assert.Null(descriptor.Steps[1].OnSuccess!.TargetVariable);
        Assert.Null(descriptor.Steps[1].OnFailure!.DirectionVariable);
    }

    [Fact]
    public void EditableContentDocumentCanonicalizesLegacyFacingDefaultOnSave()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultPlanVariables:
                  facing:
                    kind: Direction
                    directionValue: West
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans: {}
            """);

        var saved = document.SaveYaml();
        var reloaded = EditableContentDocument.LoadYaml(saved).ToRegistry();
        var template = reloaded.GetEntityTemplate(new EntityTemplateId("slime"));

        Assert.DoesNotContain("defaultPlanVariables", saved);
        Assert.Contains("actionStateDefaults", saved);
        Assert.Contains("facing: West", saved);
        Assert.Equal(Direction.West, template.ActionStateDefaults!.Facing);
        Assert.Null(template.DefaultPlanVariables);
    }

    [Fact]
    public void EditableContentDocumentCanonicalAuthoringValidationReportsArbitraryVariableFields()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              wandering:
                id: wandering
                steps:
                  - label: move turn direction
                    checks:
                      - kind: CanMove
                        directionVariable: turnDirection
                    onSuccess:
                      kind: Move
                      directionVariable: turnDirection
            """);

        var result = document.ValidateCanonicalAuthoring();

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == ContentDiagnosticCode.ArbitraryPlanVariableField
            && diagnostic.ActionPlanTemplateId == new ActionPlanTemplateId("wandering")
            && diagnostic.StepIndex == 0
            && diagnostic.VariableName == "turnDirection");
    }

    [Fact]
    public void EditableContentDocumentCanonicalAuthoringValidationReportsDefaultPlanVariables()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultPlanVariables:
                  mood:
                    kind: Int
                    intValue: 1
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans: {}
            """);

        var result = document.ValidateCanonicalAuthoring();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.ArbitraryPlanVariableField);
        Assert.Equal(new EntityTemplateId("slime"), diagnostic.EntityTemplateId);
        Assert.Equal("mood", diagnostic.VariableName);
    }

    [Fact]
    public void EditableContentDocumentSavesMovementPrimitiveDescriptors()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              movement:
                id: movement
                steps:
                  - label: teleport rock
                    checks: []
                    onSuccess:
                      kind: Teleport
                      movementTarget:
                        kind: Entity
                        entityId: rock
                      movementDestination:
                        kind: PlaneCoord
                        planeCoord:
                          planeId: world
                          coord:
                            x: 4
                            y: 2
            """);

        var saved = document.SaveYaml();
        var reloaded = EditableContentDocument.LoadYaml(saved).ActionPlans["movement"]
            .Steps!.Single().OnSuccess!;

        Assert.Contains("kind: Teleport", saved);
        Assert.Contains("movementTarget", saved);
        Assert.Contains("movementDestination", saved);
        Assert.Equal(PlanEffectKind.Teleport, reloaded.Kind);
        Assert.Equal(MovementTargetKind.Entity, reloaded.MovementTarget!.Kind);
        Assert.Equal("rock", reloaded.MovementTarget.EntityId);
        Assert.Equal("world", reloaded.MovementDestination!.PlaneCoord!.PlaneId);
        Assert.Equal(4, reloaded.MovementDestination.PlaneCoord.Coord!.X);
        Assert.Equal(2, reloaded.MovementDestination.PlaneCoord.Coord.Y);
    }
}

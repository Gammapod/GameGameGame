using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class YamlContentLoaderTests
{
    [Fact]
    public void YamlContentLoaderCreatesRegistryFromDeclarativeContent()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              rock:
                name: Rock
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 3
                carryingCapacity: 3
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 3
                defaultActionPlanId: wait
                defaultPlanVariables:
                  facing:
                    kind: Direction
                    directionValue: West
            presentations:
              rock:
                glyph: '*'
                color: Earth
              slime:
                glyph: s
                color: Green
            actionPlans:
              wait:
                id: wait
                steps:
                  - label: wait
                    checks: []
                    onSuccess:
                      kind: Wait
            """);

        var result = registry.Validate();

        Assert.True(result.IsValid);
        Assert.Equal("Slime", registry.GetEntityTemplate(new EntityTemplateId("slime")).Name);
        Assert.Equal('s', registry.GetPresentation(new EntityTemplateId("slime")).Glyph);
        Assert.Equal(PlanEffectKind.Wait, registry.GetActionPlanDescriptor(new ActionPlanTemplateId("wait")).Steps.Single().OnSuccess!.Kind);
        var variables = registry.GetEntityTemplate(new EntityTemplateId("slime")).DefaultPlanVariables!;
        Assert.Equal(Direction.West, variables["facing"].DirectionValue);
    }

    [Fact]
    public void YamlContentLoaderRemainsPermissiveForUnknownProperties()
    {
        var registry = YamlContentLoader.LoadRegistry(
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

        Assert.Equal(0, registry.GetEntityTemplate(new EntityTemplateId("rock")).InventoryWidth);
    }

    [Fact]
    public void EditableDocumentAndYamlContentLoaderInterpretTargetingLocalityAliasIdentically()
    {
        const string yaml = """
            entityTemplates:
              hunter:
                name: Hunter
                inventoryWidth: 1
                inventoryHeight: 1
                bulk: 1
                aperture: 1
                targeting:
                  range: 5
                  locality:
                    origins: [CurrentPlace, OwnInventory]
                  rules:
                  - slot: 1
                    label: prey
                    targetTemplateId: prey
                    targetCapabilities: [DestroyTarget]
                    range: 3
              prey:
                name: Prey
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
            presentations:
              hunter: { glyph: H, color: Cyan }
              prey: { glyph: p, color: Gray }
            actionPlans:
              hunt:
                behavior:
                  steps:
                  - kind: DestroyTarget
                    targetLabel: prey
            """;

        var direct = YamlContentLoader.LoadRegistry(yaml);
        var editable = EditableContentDocument.LoadYaml(yaml);
        var fromEditable = editable.ToRegistry();

        Assert.DoesNotContain(editable.ValidateCanonicalAuthoring().Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.UnknownYamlProperty);
        Assert.Equal(
            direct.GetEntityTemplate(new EntityTemplateId("hunter")).Targeting!.DefaultLocality!.Origins,
            fromEditable.GetEntityTemplate(new EntityTemplateId("hunter")).Targeting!.DefaultLocality!.Origins);
    }

    [Fact]
    public void EditableDocumentAndYamlContentLoaderInterpretActionPlanKeyFallbackIdentically()
    {
        const string yaml = """
            entityTemplates:
              actor:
                name: Actor
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 1
                defaultActionPlanId: wait
            presentations:
              actor: { glyph: A, color: Cyan }
            actionPlans:
              wait:
                behavior:
                  steps:
                  - kind: Move
                    directionMode: North
                    costs:
                    - templateId: actor
                      quantity: 2
            scenarios:
              ignoredByRegistry:
                id: ignoredByRegistry
                name: Ignored By Registry
                scenarioRootEntityTemplateId: actor
            """;

        var direct = YamlContentLoader.LoadRegistry(yaml);
        var fromEditable = EditableContentDocument.LoadYaml(yaml).ToRegistry();

        var directPlan = direct.GetActionPlanDescriptor(new ActionPlanTemplateId("wait"));
        var editablePlan = fromEditable.GetActionPlanDescriptor(new ActionPlanTemplateId("wait"));
        Assert.Equal(new ActionPlanId("wait"), directPlan.Id);
        Assert.Equal(directPlan.Id, editablePlan.Id);
        Assert.Equal(directPlan.Behavior!.Steps.Single().Costs.Single(), editablePlan.Behavior!.Steps.Single().Costs.Single());
    }

    [Fact]
    public void EditableDocumentAndYamlContentLoaderInterpretBlankActionPlanIdFallbackIdentically()
    {
        const string yaml = """
            entityTemplates:
              actor:
                name: Actor
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 1
                defaultActionPlanId: wait
            presentations:
              actor: { glyph: A, color: Cyan }
            actionPlans:
              wait:
                id: ""
                behavior:
                  steps:
                  - kind: Move
                    directionMode: North
            """;

        var direct = YamlContentLoader.LoadRegistry(yaml).GetActionPlanDescriptor(new ActionPlanTemplateId("wait"));
        var editable = EditableContentDocument.LoadYaml(yaml).ToRegistry().GetActionPlanDescriptor(new ActionPlanTemplateId("wait"));
        var descriptor = EditableContentDocument.LoadYaml(yaml).ActionPlans["wait"].ToDescriptor("wait");

        Assert.Equal(new ActionPlanId("wait"), direct.Id);
        Assert.Equal(direct.Id, editable.Id);
        Assert.Equal(direct.Id, descriptor.Id);
    }

    [Fact]
    public void EditableDocumentAndYamlContentLoaderInterpretRepresentativeSchemaFixtureIdentically()
    {
        const string yaml = """
            presentationCatalog:
              creature.bat:
                name: Bat
                fallbackText: b
                tags: [creature, flying]
            palettes:
              creature.bat.default:
                name: Bat Default
                roles:
                  primary: Gray
                  accent: Yellow
            entityTemplates:
              room:
                name: Room
                inventoryWidth: 3
                inventoryHeight: 3
                bulk: 100
                aperture: 100
                carriedEntities:
                - entityId: hunterEntity
                  templateId: hunter
                  coord: { x: 1, y: 1 }
                  controller: Player
              hunter:
                name: Hunter
                inventoryWidth: 2
                inventoryHeight: 1
                weight: 9
                carryingCapacity: 8
                bulk: 3
                aperture: 4
                material: wood
                defaultActionPlanId: hunt
                actionStateDefaults:
                  facing: East
                targeting:
                  range: 5
                  defaultLocality:
                    origins: [CurrentPlace]
                  rules:
                  - slot: 1
                    label: prey
                    targetTemplateId: prey
                    targetCapabilities: [DestroyTarget]
                    range: 3
                    locality:
                      origins: [CurrentPlace, PeerInventories]
                targetingRules:
                - slot: 2
                  label: carried
                  targetTemplateId: prey
                  targetCapabilities: [PickupTarget]
                  range: 2
              prey:
                name: Prey
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
            presentations:
              room: { glyph: R, color: Gray }
              hunter:
                presentationId: creature.bat
                paletteId: creature.bat.default
                color: Cyan
              prey: { glyph: p, color: Earth }
            actionPlans:
              hunt:
                behavior:
                  steps:
                  - kind: TargetPathMove
                    targetLabel: prey
                    pathMode: MaintainDistance
                    desiredDistance: 2
                    costs:
                    - templateId: prey
                      quantity: 1
                  - kind: Transfer
                    targetLabel: carried
                    counterpartyTargetLabel: prey
                    directionMode: Forward
                    transferDirection: ActorToTarget
            mergedLayers:
              shared:
                spaces:
                - owner: hunterEntity
                  origin: { x: 0, y: 0 }
                joins:
                - from: { owner: hunterEntity, edge: East }
                  to: { owner: hunterEntity, edge: West }
                  align: Center
            scenarios:
              ignoredByRegistry:
                id: ignoredByRegistry
                name: Ignored By Registry
                scenarioRootEntityTemplateId: room
            """;

        var direct = YamlContentLoader.LoadRegistry(yaml);
        var editable = EditableContentDocument.LoadYaml(yaml).ToRegistry();

        var directHunter = direct.GetEntityTemplate(new EntityTemplateId("hunter"));
        var editableHunter = editable.GetEntityTemplate(new EntityTemplateId("hunter"));
        Assert.Equal(directHunter.InventoryWidth, editableHunter.InventoryWidth);
        Assert.Equal(directHunter.InventoryHeight, editableHunter.InventoryHeight);
        Assert.Equal(directHunter.Bulk, editableHunter.Bulk);
        Assert.Equal(directHunter.Aperture, editableHunter.Aperture);
        Assert.Equal(directHunter.Material, editableHunter.Material);
        Assert.Equal(directHunter.ActionStateDefaults, editableHunter.ActionStateDefaults);
        Assert.Equal(directHunter.Targeting!.DefaultLocality!.Origins, editableHunter.Targeting!.DefaultLocality!.Origins);
        Assert.Equal(directHunter.Targeting.Rules.Single().Locality!.Origins, editableHunter.Targeting.Rules.Single().Locality!.Origins);
        Assert.Equal(directHunter.TargetingRules!.Single().TargetCapabilities, editableHunter.TargetingRules!.Single().TargetCapabilities);

        Assert.Equal(direct.GetPresentation(new EntityTemplateId("hunter")), editable.GetPresentation(new EntityTemplateId("hunter")));
        var directPresentationDefinition = direct.PresentationCatalog[new PresentationId("creature.bat")];
        var editablePresentationDefinition = editable.PresentationCatalog[new PresentationId("creature.bat")];
        Assert.Equal(directPresentationDefinition.Id, editablePresentationDefinition.Id);
        Assert.Equal(directPresentationDefinition.Name, editablePresentationDefinition.Name);
        Assert.Equal(directPresentationDefinition.FallbackText, editablePresentationDefinition.FallbackText);
        Assert.Equal(directPresentationDefinition.Tags, editablePresentationDefinition.Tags);
        var directPalette = direct.PaletteCatalog[new PaletteId("creature.bat.default")];
        var editablePalette = editable.PaletteCatalog[new PaletteId("creature.bat.default")];
        Assert.Equal(directPalette.Id, editablePalette.Id);
        Assert.Equal(directPalette.Name, editablePalette.Name);
        Assert.Equal(directPalette.Roles, editablePalette.Roles);

        var directSteps = direct.GetActionPlanDescriptor(new ActionPlanTemplateId("hunt")).Behavior!.Steps;
        var editableSteps = editable.GetActionPlanDescriptor(new ActionPlanTemplateId("hunt")).Behavior!.Steps;
        Assert.Equal(directSteps[0].Kind, editableSteps[0].Kind);
        Assert.Equal(directSteps[0].TargetLabel, editableSteps[0].TargetLabel);
        Assert.Equal(directSteps[0].DirectionMode, editableSteps[0].DirectionMode);
        Assert.Equal(directSteps[0].PathMode, editableSteps[0].PathMode);
        Assert.Equal(directSteps[0].DesiredDistance, editableSteps[0].DesiredDistance);
        Assert.Equal(directSteps[0].Costs.Single(), editableSteps[0].Costs.Single());
        Assert.Equal(directSteps[1].TransferDirection, editableSteps[1].TransferDirection);
        Assert.Equal(directSteps[1].CounterpartyTargetLabel, editableSteps[1].CounterpartyTargetLabel);

        var directLayer = direct.MergedInventoryLayers[new MergedInventoryLayerId("shared")];
        var editableLayer = editable.MergedInventoryLayers[new MergedInventoryLayerId("shared")];
        Assert.Equal(directLayer.Id, editableLayer.Id);
        Assert.Equal(directLayer.Spaces, editableLayer.Spaces);
        Assert.Equal(directLayer.Joins, editableLayer.Joins);
    }

    [Fact]
    public void YamlContentLoaderCanLoadRegistryFromFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"game-content-{Guid.NewGuid():N}.yaml");

        try
        {
            File.WriteAllText(
                path,
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

            var registry = YamlContentLoader.LoadRegistryFile(path);

            Assert.True(registry.Validate().IsValid);
            Assert.Equal("Rock", registry.GetEntityTemplate(new EntityTemplateId("rock")).Name);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void YamlContentLoaderLoadsEntityEnterAndExitPolicies()
    {
        var registry = YamlContentLoader.LoadRegistry(
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
                topologyPolicy: ConnectsInwardAndOutward
            presentations:
              room:
                glyph: R
                color: Cyan
            actionPlans: {}
            """);

        var template = registry.GetEntityTemplate(new EntityTemplateId("room"));

        Assert.Equal(EntityEnterPolicy.FarthestFromOccupied, template.EnterPolicy);
        Assert.Equal(EntityExitPolicy.EdgeAlignedWithExitDirection, template.ExitPolicy);
        Assert.Equal(EntityTopologyPolicy.ConnectsInwardAndOutward, template.TopologyPolicy);
    }

    [Fact]
    public void YamlContentLoaderLoadsTemplateMaterialAndValidationRejectsUnknownMaterial()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              chest:
                name: Chest
                inventoryWidth: 2
                inventoryHeight: 1
                bulk: 3
                aperture: 3
                material: wood
              ore:
                name: Ore
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
                material: metal
              mystery:
                name: Mystery
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
                material: glass
            presentations:
              chest: { glyph: C, color: Earth }
              ore: { glyph: o, color: Gray }
              mystery: { glyph: '?', color: Gray }
            actionPlans: {}
            """);

        var result = registry.Validate();

        Assert.Equal(new EntityMaterial("wood"), registry.GetEntityTemplate(new EntityTemplateId("chest")).Material);
        Assert.Equal(new EntityMaterial("metal"), registry.GetEntityTemplate(new EntityTemplateId("ore")).Material);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == ContentDiagnosticCode.InvalidEntityMaterial &&
            diagnostic.EntityTemplateId == new EntityTemplateId("mystery") &&
            diagnostic.Message.Contains("glass", StringComparison.Ordinal));
    }

    [Fact]
    public void YamlContentLoaderLoadsMergedInventoryLayerPlacements()
    {
        var registry = YamlContentLoader.LoadRegistry(
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

        var layer = Assert.Single(registry.MergedInventoryLayers).Value;

        Assert.True(registry.Validate().IsValid);
        Assert.Equal(new MergedInventoryLayerId("sharedInterior"), layer.Id);
        Assert.Equal([TestWorldEntity("entityA"), TestWorldEntity("entityB")], layer.Spaces.Select(space => space.OwnerId).ToArray());
        Assert.Equal(new GridCoord(3, 0), layer.Spaces[1].Origin);
    }

    [Fact]
    public void ContentValidationAllowsMergedLayerOverlapAsProjectionMetadata()
    {
        var registry = YamlContentLoader.LoadRegistry(
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
              overlapping:
                spaces:
                - owner: entityA
                  origin: { x: 0, y: 0 }
                - owner: entityB
                  origin: { x: 2, y: 0 }
            actionPlans: {}
            """);

        var validation = registry.Validate();

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        var layer = Assert.Single(registry.MergedInventoryLayers).Value;
        Assert.Equal(new MergedInventoryLayerId("overlapping"), layer.Id);
    }

    [Fact]
    public void OverlappingMergedLayerContentMaterializesDistinctGraphNodesWithSharedLayoutProjection()
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
              overlapping:
                spaces:
                - owner: entityA
                  origin: { x: 0, y: 0 }
                - owner: entityB
                  origin: { x: 2, y: 0 }
            actionPlans: {}
            scenarios:
              overlap:
                name: Overlap
                scenarioRootEntityTemplateId: room
                playerControls: {}
            """);

        var materialization = ScenarioMaterializer.Materialize(document, "overlap");
        var world = materialization.World;
        var entityAPlane = world.GetRegisteredInventoryPlaneId(new EntityId("entityA"));
        var entityBPlane = world.GetRegisteredInventoryPlaneId(new EntityId("entityB"));
        Assert.NotNull(entityAPlane);
        Assert.NotNull(entityBPlane);
        var sourceA = new PlaneCoord(entityAPlane!.Value, new GridCoord(2, 0));
        var sourceB = new PlaneCoord(entityBPlane!.Value, new GridCoord(0, 0));
        var graph = TopologyGraphMaterializer.Materialize(world);

        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.True(graph.TryGetNode(new TopologyCellRef(sourceA), out var nodeA));
        Assert.True(graph.TryGetNode(new TopologyCellRef(sourceB), out var nodeB));
        Assert.NotEqual(nodeA.Id, nodeB.Id);
        Assert.Equal(new TopologyLayoutCoord(new GridCoord(2, 0)), nodeA.LayoutCoord);
        Assert.Equal(nodeA.LayoutCoord, nodeB.LayoutCoord);
    }

    [Fact]
    public void OverlapLoopScenarioMovesThroughExplicitGraphEdgesWithoutCollapsingLayoutProjection()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              root:
                name: Root
                inventoryWidth: 3
                inventoryHeight: 3
                bulk: 10
                aperture: 10
                carriedEntities:
                - entityId: nodeA
                  templateId: oneCell
                  coord: { x: 0, y: 0 }
                - entityId: nodeB
                  templateId: oneCell
                  coord: { x: 1, y: 0 }
                - entityId: nodeC
                  templateId: oneCell
                  coord: { x: 2, y: 0 }
              oneCell:
                name: One Cell
                inventoryWidth: 1
                inventoryHeight: 1
                bulk: 1
                aperture: 10
            presentations:
              root: { glyph: R, color: Gray }
              oneCell: { glyph: o, color: Cyan }
            mergedLayers:
              overlapLoop:
                spaces:
                - owner: nodeA
                  origin: { x: 0, y: 0 }
                - owner: nodeB
                  origin: { x: 0, y: 0 }
                - owner: nodeC
                  origin: { x: 0, y: 0 }
                joins:
                - from: { owner: nodeA, edge: East }
                  to: { owner: nodeB, edge: West }
                  align: Center
                - from: { owner: nodeB, edge: South }
                  to: { owner: nodeC, edge: North }
                  align: Center
                - from: { owner: nodeC, edge: West }
                  to: { owner: nodeA, edge: North }
                  align: Center
            actionPlans: {}
            scenarios:
              overlap-loop:
                name: Overlap Loop
                scenarioRootEntityTemplateId: root
                playerControls: {}
            """);

        var materialization = ScenarioMaterializer.Materialize(document, "overlap-loop");
        var world = materialization.World;
        var nodeAPlane = world.GetRegisteredInventoryPlaneId(new EntityId("nodeA"));
        var nodeBPlane = world.GetRegisteredInventoryPlaneId(new EntityId("nodeB"));
        var nodeCPlane = world.GetRegisteredInventoryPlaneId(new EntityId("nodeC"));
        Assert.NotNull(nodeAPlane);
        Assert.NotNull(nodeBPlane);
        Assert.NotNull(nodeCPlane);
        var sourceA = new PlaneCoord(nodeAPlane!.Value, new GridCoord(0, 0));
        var sourceB = new PlaneCoord(nodeBPlane!.Value, new GridCoord(0, 0));
        var sourceC = new PlaneCoord(nodeCPlane!.Value, new GridCoord(0, 0));
        var travelerId = new EntityId("traveler");
        AddRuntimeEntity(world, travelerId, "Traveler", sourceA);
        var movement = new MovementService();

        Assert.Empty(materialization.ValidationDiagnostics);
        var graph = TopologyGraphMaterializer.Materialize(world);
        var overlappedNodes = new[] { sourceA, sourceB, sourceC }
            .Select(source =>
            {
                Assert.True(graph.TryGetNode(new TopologyCellRef(source), out var node));
                return node;
            })
            .ToList();
        Assert.Single(overlappedNodes.Select(node => node.LayoutCoord).Distinct());
        Assert.Equal(3, overlappedNodes.Select(node => node.Id).Distinct().Count());

        Assert.True(movement.TryMove(world, travelerId, Direction.East));
        Assert.Equal(sourceB, world.GetEntityLocation(travelerId));
        Assert.True(movement.TryMove(world, travelerId, Direction.South));
        Assert.Equal(sourceC, world.GetEntityLocation(travelerId));
        Assert.True(movement.TryMove(world, travelerId, Direction.West));
        Assert.Equal(sourceA, world.GetEntityLocation(travelerId));
    }

    [Fact]
    public void ContentValidationRejectsMergedLayerDisconnectedOrInvalidOwner()
    {
        var registry = YamlContentLoader.LoadRegistry(
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
              disconnected:
                spaces:
                - owner: entityA
                  origin: { x: 0, y: 0 }
                - owner: entityB
                  origin: { x: 10, y: 10 }
              invalidOwner:
                spaces:
                - owner: entityA
                  origin: { x: 0, y: 0 }
                - owner: missingEntity
                  origin: { x: 3, y: 0 }
            actionPlans: {}
            """);

        var validation = registry.Validate();

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.Contains("disconnected", StringComparison.Ordinal) && error.Contains("disconnected", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(validation.Errors, error => error.Contains("invalidOwner", StringComparison.Ordinal) && error.Contains("missingEntity", StringComparison.Ordinal));
    }

    [Fact]
    public void ContentValidationAllowsMergedLayerWithThreeContributors()
    {
        var registry = YamlContentLoader.LoadRegistry(
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
                - entityId: entityC
                  templateId: spaceC
                  coord: { x: 2, y: 0 }
              spaceA:
                name: Space A
                inventoryWidth: 1
                inventoryHeight: 1
                bulk: 1
                aperture: 10
              spaceB:
                name: Space B
                inventoryWidth: 1
                inventoryHeight: 1
                bulk: 1
                aperture: 10
              spaceC:
                name: Space C
                inventoryWidth: 1
                inventoryHeight: 1
                bulk: 1
                aperture: 10
            presentations:
              room: { glyph: R, color: Gray }
              spaceA: { glyph: A, color: Cyan }
              spaceB: { glyph: B, color: Green }
              spaceC: { glyph: C, color: Yellow }
            mergedLayers:
              sharedInterior:
                spaces:
                - owner: entityA
                  origin: { x: 0, y: 0 }
                - owner: entityB
                  origin: { x: 1, y: 0 }
                - owner: entityC
                  origin: { x: 2, y: 0 }
            actionPlans: {}
            """);

        var validation = registry.Validate();
        var layer = Assert.Single(registry.MergedInventoryLayers).Value;

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        Assert.Equal(3, layer.Spaces.Count);
    }

    [Fact]
    public void YamlContentLoaderLoadsAlignedMergedLayerJoin()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              root:
                name: Root
                inventoryWidth: 3
                inventoryHeight: 3
                bulk: 10
                aperture: 10
                carriedEntities:
                - entityId: roomA
                  templateId: roomSpace
                  coord: { x: 0, y: 0 }
                - entityId: hallAB
                  templateId: hallSpace
                  coord: { x: 1, y: 0 }
              roomSpace:
                name: Room Space
                inventoryWidth: 3
                inventoryHeight: 3
                bulk: 1
                aperture: 10
              hallSpace:
                name: Hall Space
                inventoryWidth: 5
                inventoryHeight: 1
                bulk: 1
                aperture: 10
            presentations:
              root: { glyph: R, color: Gray }
              roomSpace: { glyph: A, color: Cyan }
              hallSpace: { glyph: H, color: Green }
            mergedLayers:
              roomHall:
                spaces:
                - owner: roomA
                  origin: { x: 0, y: 0 }
                - owner: hallAB
                  origin: { x: 10, y: 0 }
                joins:
                - from: { owner: roomA, edge: East }
                  to: { owner: hallAB, edge: West }
                  align: Center
            actionPlans: {}
            """);

        var layer = Assert.Single(registry.MergedInventoryLayers).Value;
        var join = Assert.Single(layer.Joins!);

        Assert.True(registry.Validate().IsValid, string.Join(Environment.NewLine, registry.Validate().Errors));
        Assert.Equal(new EntityId("roomA"), join.From.OwnerId);
        Assert.Equal(Direction.East, join.From.Edge);
        Assert.Equal(new EntityId("hallAB"), join.To.OwnerId);
        Assert.Equal(Direction.West, join.To.Edge);
        Assert.Equal(MergedInventoryJoinAlignment.Center, join.Align);
    }

    [Fact]
    public void ContentValidationRejectsMergedLayerJoinDirectionalConflict()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              root:
                name: Root
                inventoryWidth: 3
                inventoryHeight: 3
                bulk: 10
                aperture: 10
                carriedEntities:
                - entityId: roomA
                  templateId: roomSpace
                  coord: { x: 0, y: 0 }
                - entityId: hallAB
                  templateId: hallSpace
                  coord: { x: 1, y: 0 }
                - entityId: hallAC
                  templateId: hallSpace
                  coord: { x: 2, y: 0 }
              roomSpace:
                name: Room Space
                inventoryWidth: 3
                inventoryHeight: 3
                bulk: 1
                aperture: 10
              hallSpace:
                name: Hall Space
                inventoryWidth: 5
                inventoryHeight: 1
                bulk: 1
                aperture: 10
            presentations:
              root: { glyph: R, color: Gray }
              roomSpace: { glyph: A, color: Cyan }
              hallSpace: { glyph: H, color: Green }
            mergedLayers:
              roomHallConflict:
                spaces:
                - owner: roomA
                  origin: { x: 0, y: 0 }
                - owner: hallAB
                  origin: { x: 10, y: 0 }
                - owner: hallAC
                  origin: { x: 20, y: 0 }
                joins:
                - from: { owner: roomA, edge: East }
                  to: { owner: hallAB, edge: West }
                  align: Center
                - from: { owner: roomA, edge: East }
                  to: { owner: hallAC, edge: West }
                  align: Center
            actionPlans: {}
            """);

        var validation = registry.Validate();

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.Contains("roomHallConflict", StringComparison.Ordinal) && error.Contains("directional conflict", StringComparison.OrdinalIgnoreCase));
        var diagnostic = Assert.Single(validation.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidMergedInventoryLayer);
        Assert.Equal(new MergedInventoryLayerId("roomHallConflict"), diagnostic.MergedInventoryLayerId);
        Assert.Contains("directional conflict", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ContentValidationReportsMergedLayerUnknownOwnerAsStructuredDiagnostic()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              root:
                name: Root
                inventoryWidth: 2
                inventoryHeight: 1
                bulk: 10
                aperture: 10
                carriedEntities:
                - entityId: roomA
                  templateId: roomSpace
                  coord: { x: 0, y: 0 }
              roomSpace:
                name: Room Space
                inventoryWidth: 1
                inventoryHeight: 1
                bulk: 1
                aperture: 10
            presentations:
              root: { glyph: R, color: Gray }
              roomSpace: { glyph: A, color: Cyan }
            mergedLayers:
              missingOwnerLayer:
                spaces:
                - owner: roomA
                  origin: { x: 0, y: 0 }
                - owner: missingRoom
                  origin: { x: 2, y: 0 }
            actionPlans: {}
            """);

        var validation = registry.Validate();

        Assert.False(validation.IsValid);
        var diagnostic = Assert.Single(validation.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidMergedInventoryLayer);
        Assert.Equal(new MergedInventoryLayerId("missingOwnerLayer"), diagnostic.MergedInventoryLayerId);
        Assert.Equal(new EntityId("missingRoom"), diagnostic.RelatedEntityId);
        Assert.Contains("unknown owner entity missingRoom", diagnostic.Message, StringComparison.Ordinal);
    }

    private static EntityId TestWorldEntity(string id) => new(id);

    [Fact]
    public void YamlContentLoaderDefaultsMissingEntityPoliciesToNullWithEffectiveSimplePolicies()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              room:
                name: Room
                inventoryWidth: 3
                inventoryHeight: 3
                bulk: 1
                aperture: 10
            presentations:
              room:
                glyph: R
                color: Cyan
            actionPlans: {}
            """);

        var template = registry.GetEntityTemplate(new EntityTemplateId("room"));

        Assert.Null(template.EnterPolicy);
        Assert.Null(template.ExitPolicy);
        Assert.Equal(EntityEnterPolicy.FirstUnoccupiedRowMajor, template.EffectiveEnterPolicy);
        Assert.Equal(EntityExitPolicy.AnyCell, template.EffectiveExitPolicy);
        Assert.Equal(EntityTopologyPolicy.None, template.TopologyPolicy);
    }

    [Fact]
    public void YamlContentLoaderLoadsSemanticPresentationAndPaletteIdsWhilePreservingLegacyGlyphFallback()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              rat:
                name: Rat
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 1
              player:
                name: Player
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 1
              rock:
                name: Rock
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 1
            presentations:
              rat:
                presentationId: creature.rat
                paletteId: creature.rat.default
                glyph: r
                color: Gray
              player:
                presentationId: actor.player
                paletteId: actor.player.default
                glyph: '@'
                color: Yellow
              rock:
                glyph: '*'
                color: Earth
            actionPlans: {}
            """);

        var rat = registry.GetPresentation(new EntityTemplateId("rat"));
        var player = registry.GetPresentation(new EntityTemplateId("player"));
        var rock = registry.GetPresentation(new EntityTemplateId("rock"));

        Assert.Equal(new PresentationId("creature.rat"), rat.PresentationId);
        Assert.Equal(new PaletteId("creature.rat.default"), rat.PaletteId);
        Assert.Equal('r', rat.Glyph);
        Assert.Equal('r', rat.ToInspectionAppearance().Glyph);
        Assert.Equal(PresentationColor.Gray, rat.Color);
        Assert.Equal('@', player.ToInspectionAppearance().Glyph);
        Assert.Equal(new PresentationId("legacy.glyph.*"), rock.PresentationId);
        Assert.Equal(new PaletteId("legacy.color.Earth"), rock.PaletteId);
    }

    [Fact]
    public void YamlContentLoaderLoadsCreateEntityAndPolymorphTargetTemplateFields()
    {
        var registry = YamlContentLoader.LoadRegistry(
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
                      directionMode: Forward
                    - kind: PolymorphTarget
                      targetSelf: true
                      templateId: egg
            """);

        var steps = registry.GetActionPlanDescriptor(new ActionPlanTemplateId("lifecycle")).Behavior!.Steps;

        Assert.Equal(ActionPlanBehaviorStepKind.CreateEntity, steps[0].Kind);
        Assert.Equal("rat", steps[0].TemplateId);
        Assert.Equal(CreateEntityPlacement.Facing, steps[0].CreatePlacement);
        Assert.Equal(ActionPlanMoveDirectionMode.Forward, steps[0].DirectionMode);
        Assert.Equal(ActionPlanBehaviorStepKind.PolymorphTarget, steps[1].Kind);
        Assert.True(steps[1].TargetSelf);
        Assert.Equal("egg", steps[1].TemplateId);
    }

    [Fact]
    public void YamlContentLoaderLoadsBehaviorStepCostEntries()
    {
        var registry = YamlContentLoader.LoadRegistry(
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

        var step = registry.GetActionPlanDescriptor(new ActionPlanTemplateId("costlyMove")).Behavior!.Steps.Single();
        var cost = Assert.Single(step.Costs);
        Assert.Equal("scrap", cost.TemplateId);
        Assert.Equal(3, cost.Quantity);
    }

    [Fact]
    public void YamlContentLoaderMaterializesBulkAndApertureMetadata()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              satchel:
                name: Satchel
                inventoryWidth: 1
                inventoryHeight: 1
                bulk: 2
                aperture: 3
            presentations:
              satchel:
                glyph: b
                color: Earth
            actionPlans: {}
            """);

        var template = registry.GetEntityTemplate(new EntityTemplateId("satchel"));

        Assert.Equal(2, template.Bulk);
        Assert.Equal(3, template.Aperture);
    }

    [Fact]
    public void YamlContentLoaderLoadsCanonicalActionPlanDescriptorsWithoutVariableNames()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: canonical
                defaultPlanVariables:
                  facing:
                    kind: Direction
                    directionValue: South
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              canonical:
                id: canonical
                steps:
                  - label: move facing
                    checks:
                      - kind: CanMove
                    onSuccess:
                      kind: Move
                  - label: reverse
                    checks: []
                    onSuccess:
                      kind: ReverseDirection
                      consumesTurn: false
                      continuePlan: false
            """);

        var result = registry.Validate();
        var descriptor = registry.GetActionPlanDescriptor(new ActionPlanTemplateId("canonical"));

        Assert.True(result.IsValid);
        Assert.Null(descriptor.Steps[0].Checks.Single().DirectionVariable);
        Assert.Null(descriptor.Steps[0].OnSuccess!.DirectionVariable);
        Assert.Null(descriptor.Steps[1].OnSuccess!.DirectionVariable);
        Assert.IsType<CanMoveCheck>(descriptor.Steps[0].Checks.Single().Materialize());
        Assert.IsType<MoveEffect>(descriptor.Steps[0].OnSuccess!.Materialize());
    }

    [Fact]
    public void YamlContentLoaderRoundTripsPrimitiveBackedActionPlanDescriptor()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              primitiveMove:
                id: primitiveMove
                primitive:
                  kind: MoveFacing
                  fallbackPlanId: wait
              wait:
                id: wait
                steps:
                  - label: wait
                    checks: []
                    onSuccess:
                      kind: Wait
            """);

        var registry = document.ToRegistry();
        var saved = document.SaveYaml();
        var reloaded = EditableContentDocument.LoadYaml(saved).ToRegistry();

        var descriptor = registry.GetActionPlanDescriptor(new ActionPlanTemplateId("primitiveMove"));
        Assert.Equal(ActionPlanPrimitiveKind.MoveFacing, descriptor.Primitive!.Kind);
        Assert.Equal(new ActionPlanId("wait"), descriptor.Primitive.FallbackPlanId);
        Assert.Contains("primitive:", saved);
        Assert.Equal(ActionPlanPrimitiveKind.MoveFacing, reloaded.GetActionPlanDescriptor(new ActionPlanTemplateId("primitiveMove")).Primitive!.Kind);
    }

    [Fact]
    public void YamlContentLoaderRoundTripsCanonicalBehaviorChainDescriptor()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              behaviorChain:
                id: behaviorChain
                behavior:
                  steps:
                    - kind: MoveFacing
                    - kind: Backstep
                    - kind: PickupTarget
                    - kind: TurnLeft
                    - kind: TurnRight
                    - kind: ReverseFacing
                    - kind: FleeTarget
                      targetSlot: 2
                    - kind: MaintainChebyshevDistanceTwo
                    - kind: StrafeClockwise
                    - kind: StrafeAnticlockwise
                    - kind: ApplyPrePlan
                      targetSlot: 3
                      planId: behaviorChain
            """);

        var registry = document.ToRegistry();
        var saved = document.SaveYaml();
        var reloaded = EditableContentDocument.LoadYaml(saved).ToRegistry();

        var descriptor = registry.GetActionPlanDescriptor(new ActionPlanTemplateId("behaviorChain"));
        Assert.Equal(
            [
                ActionPlanBehaviorStepKind.MoveFacing,
                ActionPlanBehaviorStepKind.Backstep,
                ActionPlanBehaviorStepKind.PickupTarget,
                ActionPlanBehaviorStepKind.TurnLeft,
                ActionPlanBehaviorStepKind.TurnRight,
                ActionPlanBehaviorStepKind.ReverseFacing,
                ActionPlanBehaviorStepKind.FleeTarget,
                ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo,
                ActionPlanBehaviorStepKind.StrafeClockwise,
                ActionPlanBehaviorStepKind.StrafeAnticlockwise,
                ActionPlanBehaviorStepKind.ApplyPrePlan
            ],
            descriptor.Behavior!.Steps.Select(step => step.Kind).ToArray());
        Assert.Contains("behavior:", saved);
        Assert.Contains("kind: MoveFacing", saved);
        Assert.Contains("kind: Backstep", saved);
        Assert.Contains("kind: ReverseFacing", saved);
        Assert.Contains("kind: FleeTarget", saved);
        Assert.Contains("targetSlot: 2", saved);
        Assert.Contains("kind: MaintainChebyshevDistanceTwo", saved);
        Assert.Contains("kind: StrafeClockwise", saved);
        Assert.Contains("kind: StrafeAnticlockwise", saved);
        Assert.Contains("kind: ApplyPrePlan", saved);
        Assert.Contains("planId: behaviorChain", saved);
        Assert.Equal(ActionPlanBehaviorStepKind.FleeTarget, reloaded.GetActionPlanDescriptor(new ActionPlanTemplateId("behaviorChain")).Behavior!.Steps[6].Kind);
        Assert.Equal(2, reloaded.GetActionPlanDescriptor(new ActionPlanTemplateId("behaviorChain")).Behavior!.Steps[6].TargetSlot);
        Assert.Equal(ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo, reloaded.GetActionPlanDescriptor(new ActionPlanTemplateId("behaviorChain")).Behavior!.Steps[7].Kind);
        Assert.Equal(ActionPlanBehaviorStepKind.StrafeClockwise, reloaded.GetActionPlanDescriptor(new ActionPlanTemplateId("behaviorChain")).Behavior!.Steps[8].Kind);
        Assert.Equal(ActionPlanBehaviorStepKind.StrafeAnticlockwise, reloaded.GetActionPlanDescriptor(new ActionPlanTemplateId("behaviorChain")).Behavior!.Steps[9].Kind);
        Assert.Equal(ActionPlanBehaviorStepKind.ApplyPrePlan, reloaded.GetActionPlanDescriptor(new ActionPlanTemplateId("behaviorChain")).Behavior!.Steps[10].Kind);
        Assert.Equal(3, reloaded.GetActionPlanDescriptor(new ActionPlanTemplateId("behaviorChain")).Behavior!.Steps[10].TargetSlot);
        Assert.Equal(new ActionPlanId("behaviorChain"), reloaded.GetActionPlanDescriptor(new ActionPlanTemplateId("behaviorChain")).Behavior!.Steps[10].PlanId);
    }

    [Fact]
    public void CanonicalMoveDescriptorRoundTripsDirectionMode()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              canonicalMove:
                id: canonicalMove
                behavior:
                  steps:
                    - kind: Move
                      directionMode: BackLeft
            """);

        var registry = document.ToRegistry();
        var saved = document.SaveYaml();
        var reloaded = EditableContentDocument.LoadYaml(saved).ToRegistry();

        var step = registry.GetActionPlanDescriptor(new ActionPlanTemplateId("canonicalMove")).Behavior!.Steps[0];
        Assert.Equal(ActionPlanBehaviorStepKind.Move, step.Kind);
        Assert.Equal(ActionPlanMoveDirectionMode.BackLeft, step.DirectionMode);
        Assert.Contains("kind: Move", saved);
        Assert.Contains("directionMode: BackLeft", saved);
        Assert.Equal(ActionPlanMoveDirectionMode.BackLeft, reloaded.GetActionPlanDescriptor(new ActionPlanTemplateId("canonicalMove")).Behavior!.Steps[0].DirectionMode);
    }

    [Fact]
    public void CanonicalTransferDescriptorRoundTripsDirectionModeAndTransferDirection()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              transferPlan:
                id: transferPlan
                behavior:
                  steps:
                    - kind: Transfer
                      targetLabel: offers
                      directionMode: Forward
                      transferDirection: ActorToTarget
            """);

        var registry = document.ToRegistry();
        var saved = document.SaveYaml();
        var reloaded = EditableContentDocument.LoadYaml(saved).ToRegistry();

        var step = registry.GetActionPlanDescriptor(new ActionPlanTemplateId("transferPlan")).Behavior!.Steps[0];
        Assert.Equal(ActionPlanBehaviorStepKind.Transfer, step.Kind);
        Assert.Equal("offers", step.TargetLabel);
        Assert.Equal(ActionPlanMoveDirectionMode.Forward, step.DirectionMode);
        Assert.Equal(TransferDirection.ActorToTarget, step.TransferDirection);
        Assert.Contains("kind: Transfer", saved);
        Assert.Contains("targetLabel: offers", saved);
        Assert.Contains("directionMode: Forward", saved);
        Assert.Contains("transferDirection: ActorToTarget", saved);
        Assert.Equal(TransferDirection.ActorToTarget, reloaded.GetActionPlanDescriptor(new ActionPlanTemplateId("transferPlan")).Behavior!.Steps[0].TransferDirection);
    }

    [Fact]
    public void YamlContentLoaderLoadsCanonicalPushBehavior()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              pushPlan:
                id: pushPlan
                behavior:
                  steps:
                    - kind: Push
                      targetLabel: foe
                      directionMode: SouthEast
            """);

        var registry = document.ToRegistry();
        var saved = document.SaveYaml();
        var reloaded = EditableContentDocument.LoadYaml(saved).ToRegistry();

        var step = registry.GetActionPlanDescriptor(new ActionPlanTemplateId("pushPlan")).Behavior!.Steps[0];
        Assert.Equal(ActionPlanBehaviorStepKind.Push, step.Kind);
        Assert.Equal("foe", step.TargetLabel);
        Assert.Equal(ActionPlanMoveDirectionMode.SouthEast, step.DirectionMode);
        Assert.Contains("kind: Push", saved);
        Assert.Contains("targetLabel: foe", saved);
        Assert.Contains("directionMode: SouthEast", saved);
        Assert.Equal(ActionPlanMoveDirectionMode.SouthEast, reloaded.GetActionPlanDescriptor(new ActionPlanTemplateId("pushPlan")).Behavior!.Steps[0].DirectionMode);
    }

    [Fact]
    public void YamlContentLoaderLoadsCanonicalActorActionStateDefaults()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                actionStateDefaults:
                  facing: East
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans: {}
            """);

        var template = registry.GetEntityTemplate(new EntityTemplateId("slime"));

        Assert.Equal(Direction.East, template.ActionStateDefaults!.Facing);
    }

    [Fact]
    public void YamlContentLoaderLoadsTargetPathMoveBehavior()
    {
        var registry = YamlContentLoader.LoadRegistry(
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
                      orbitDirection: Clockwise
            """);

        var step = registry.GetActionPlanDescriptor(new ActionPlanTemplateId("orbitPlan")).Behavior!.Steps.Single();

        Assert.Equal(ActionPlanBehaviorStepKind.TargetPathMove, step.Kind);
        Assert.Equal("enemy", step.TargetLabel);
        Assert.Equal(ActionPlanTargetPathMode.Orbit, step.PathMode);
        Assert.Equal(6, step.DesiredDistance);
        Assert.Equal(ActionPlanOrbitDirection.Clockwise, step.OrbitDirection);
    }

    [Fact]
    public void YamlContentLoaderLoadsTemplateTargetingRules()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              cat:
                name: Cat
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 3
                carryingCapacity: 0
              mouse:
                name: Mouse
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
                targetingRules:
                  - slot: 1
                    label: danger
                    hint: Danger
                    targetTemplateId: cat
                    range: 6
            presentations:
              cat:
                glyph: c
                color: Earth
              mouse:
                glyph: m
                color: Gray
            actionPlans: {}
            """);

        var template = registry.GetEntityTemplate(new EntityTemplateId("mouse"));
        var rule = Assert.Single(template.TargetingRules!);

        Assert.Equal(1, rule.Slot);
        Assert.Equal("danger", rule.Label);
        Assert.Equal("Danger", rule.Hint);
        Assert.Equal(new EntityTemplateId("cat"), rule.TargetTemplateId);
        Assert.Equal(6, rule.Range);
        Assert.True(registry.Validate().IsValid);
    }

    [Fact]
    public void EditableContentDocumentRoundTripsTemplateTargetingRules()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              cat:
                name: Cat
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 3
                carryingCapacity: 0
              mouse:
                name: Mouse
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
                targetingRules:
                  - slot: 2
                    label: home
                    hint: Home
                    targetTemplateId: cat
                    range: 4
            presentations:
              cat:
                glyph: c
                color: Earth
              mouse:
                glyph: m
                color: Gray
            actionPlans: {}
            """);

        var saved = document.SaveYaml();
        var reloaded = EditableContentDocument.LoadYaml(saved).ToRegistry();
        var rule = Assert.Single(reloaded.GetEntityTemplate(new EntityTemplateId("mouse")).TargetingRules!);

        Assert.Contains("targetingRules:", saved);
        Assert.Contains("label: home", saved);
        Assert.Contains("hint: Home", saved);
        Assert.Equal(2, rule.Slot);
        Assert.Equal("home", rule.Label);
        Assert.Equal(new EntityTemplateId("cat"), rule.TargetTemplateId);
    }

    [Fact]
    public void YamlContentLoaderLoadsMovementPrimitiveDescriptors()
    {
        var registry = YamlContentLoader.LoadRegistry(
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
                  - label: drop carried
                    checks: []
                    onSuccess:
                      kind: Drop
                      movementTarget:
                        kind: CarriedInventoryCoord
                        inventoryCoord:
                          x: 0
                          y: 1
                      movementDestination:
                        kind: AdjacentToSelf
                        direction: East
            """);

        var descriptor = registry.GetActionPlanDescriptor(new ActionPlanTemplateId("movement"));
        var teleport = descriptor.Steps[0].OnSuccess!;
        var drop = descriptor.Steps[1].OnSuccess!;

        Assert.Equal(PlanEffectKind.Teleport, teleport.Kind);
        Assert.Equal(MovementTargetKind.Entity, teleport.MovementTarget!.Kind);
        Assert.Equal(new EntityId("rock"), teleport.MovementTarget.EntityId);
        Assert.Equal(MovementDestinationKind.PlaneCoord, teleport.MovementDestination!.Kind);
        Assert.Equal(new PlaneCoord(new PlaneId("world"), new GridCoord(4, 2)), teleport.MovementDestination.PlaneCoord);
        Assert.Equal(PlanEffectKind.Drop, drop.Kind);
        Assert.Equal(MovementTargetKind.CarriedInventoryCoord, drop.MovementTarget!.Kind);
        Assert.Equal(new GridCoord(0, 1), drop.MovementTarget.InventoryCoord);
        Assert.Equal(MovementDestinationKind.AdjacentToSelf, drop.MovementDestination!.Kind);
        Assert.Equal(Direction.East, drop.MovementDestination.Direction);
    }

    [Fact]
    public void SpawnedActionPlanUsesCanonicalInitialFacingDefault()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              actor:
                name: Actor
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
                defaultActionPlanId: moveFacing
                actionStateDefaults:
                  facing: South
            presentations:
              actor:
                glyph: a
                color: Green
            actionPlans:
              moveFacing:
                id: moveFacing
                steps:
                  - label: move facing
                    checks:
                      - kind: CanMove
                    onSuccess:
                      kind: Move
            """);
        var world = TestWorld.CreateWorld();
        var actorId = new EntityId("actor");
        var spawn = registry.SpawnEntity(
            world,
            new EntityTemplateId("actor"),
            new EntitySpawnOptions(actorId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(3, 1))));
        var turns = new TurnService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());

        Assert.Equal(Direction.South, world.GetActionFacing(actorId));

        var acted = turns.ResolvePlan(world, actorId, spawn.ActionPlan!.PlanTurn(world, actorId, new MovementService()));

        Assert.True(acted);
        Assert.Equal("Actor@world(3,2)", world.FormatEntityAddress(actorId));
        Assert.True(TraceContains(world.LastTrace!, "Read slot Facing"));
    }

    private static bool TraceContains(TraceNode trace, string label) =>
        trace.Label == label || trace.Children.Any(child => TraceContains(child, label));

    private static void AddRuntimeEntity(WorldState world, EntityId entityId, string name, PlaneCoord location)
    {
        var nodeId = world.GetNodeId(location);
        world.Entities.Add(entityId, new Entity(entityId, name, nodeId, InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 1));
        world.Occupancy.Add(nodeId, entityId);
    }
}

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
}

using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

public sealed class ContentRegistryTests
{
    [Fact]
    public void InspectionDtosAreOwnedByContentAssembly()
    {
        Assert.Equal("GameGameGame.Content", typeof(EntityInspectionPanel).Assembly.GetName().Name);
        Assert.Equal("GameGameGame.Content", typeof(EntityInspectionService).Assembly.GetName().Name);
        Assert.Equal("GameGameGame.Content", typeof(PresentationColor).Assembly.GetName().Name);
    }

    [Fact]
    public void PrototypeRegistryResolvesEntityTemplatesByStableId()
    {
        var registry = PrototypeContent.CreateRegistry();

        Assert.Equal("Rock", registry.GetEntityTemplate(PrototypeContent.RockTemplateId).Name);
        Assert.Equal("Slime", registry.GetEntityTemplate(PrototypeContent.SlimeTemplateId).Name);
    }

    [Fact]
    public void PrototypeRegistryResolvesPresentationByTemplateId()
    {
        var registry = PrototypeContent.CreateRegistry();

        var rock = registry.GetPresentation(PrototypeContent.RockTemplateId);
        var slime = registry.GetPresentation(PrototypeContent.SlimeTemplateId);

        Assert.Equal('*', rock.Glyph);
        Assert.Equal(PresentationColor.Earth, rock.Color);
        Assert.Equal('s', slime.Glyph);
        Assert.Equal(PresentationColor.Green, slime.Color);
    }

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
    public void InspectionCanUseContentPresentationInsteadOfEntityPresentationFields()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithPresentation(PrototypeContent.RockTemplateId, new EntityPresentation('R', PresentationColor.White));
        var world = PrototypeContent.CreateFirstSlice().World;
        var rockId = new EntityId("inspectedRegistryRock");
        registry.SpawnEntity(
            world,
            PrototypeContent.RockTemplateId,
            new EntitySpawnOptions(rockId, new PlaneCoord(PrototypeContent.GameInventoryPlaneId, new GridCoord(0, 0))));
        var inspector = new EntityInspectionService(entityId => registry.GetPresentationForEntity(entityId).ToInspectionAppearance());

        var panel = inspector.Inspect(world, rockId);

        Assert.Equal('R', panel.Glyph);
        Assert.Equal(PresentationColor.White, panel.Color);
    }

    [Fact]
    public void RegistrySpawnTracksTemplateIdForPresentationLookup()
    {
        var registry = PrototypeContent.CreateRegistry();
        var world = PrototypeContent.CreateFirstSlice().World;
        var spawnId = new EntityId("presentedRock");

        registry.SpawnEntity(
            world,
            PrototypeContent.RockTemplateId,
            new EntitySpawnOptions(spawnId, new PlaneCoord(PrototypeContent.GameInventoryPlaneId, new GridCoord(0, 0))));

        Assert.Equal(PrototypeContent.RockTemplateId, registry.GetTemplateIdForEntity(spawnId));
        Assert.Equal('*', registry.GetPresentationForEntity(spawnId).Glyph);
    }

    [Fact]
    public void PrototypeRegistryCreatesActionPlansByStableId()
    {
        var registry = PrototypeContent.CreateRegistry();

        var plan = registry.CreateActionPlan(PrototypeContent.WanderingActionPlanTemplateId);

        Assert.IsType<InterpretedEntityActionPlan>(plan);
    }

    [Fact]
    public void PrototypeRegistryValidationPassesForBuiltInContent()
    {
        var registry = PrototypeContent.CreateRegistry();

        var result = registry.Validate();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsMissingTemplateDefaultPlan()
    {
        var missingPlanId = new ActionPlanTemplateId("missingPlan");
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                PrototypeContent.RockTemplateId,
                PrototypeContent.CreateRockTemplate() with { DefaultActionPlanId = missingPlanId });

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("missingPlan") && error.Contains("Rock"));
    }

    [Fact]
    public void PrototypeRegistryValidationReportsMissingCalledPlan()
    {
        var missingPlanId = new ActionPlanId("missingNestedPlan");
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("invalidWandering"),
                    [
                        new ActionPlanStepDescriptor(
                            "call missing",
                            [],
                            PlanEffectDescriptor.CallPlan(missingPlanId),
                            OnFailure: null)
                    ]));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("missingNestedPlan") && error.Contains("invalidWandering"));
    }

    [Fact]
    public void PrototypeRegistryResolvesReusableActionPlanDescriptorsByStableId()
    {
        var registry = PrototypeContent.CreateRegistry();

        var wandering = registry.GetActionPlanDescriptor(PrototypeContent.WanderingActionPlanTemplateId);
        var handleBlocker = registry.GetActionPlanDescriptor(PrototypeContent.HandleBlockerActionPlanTemplateId);

        Assert.Equal("wandering", wandering.Id.Value);
        Assert.Equal("handleBlocker", handleBlocker.Id.Value);
        Assert.Contains(wandering.Steps, step => step.OnSuccess?.Kind == PlanEffectKind.CallPlan && step.OnSuccess.PlanId == handleBlocker.Id);
    }

    [Fact]
    public void PrototypeActionPlanDescriptorsUseDataInputs()
    {
        var registry = PrototypeContent.CreateRegistry();
        var wandering = registry.GetActionPlanDescriptor(PrototypeContent.WanderingActionPlanTemplateId);
        var handleBlocker = registry.GetActionPlanDescriptor(PrototypeContent.HandleBlockerActionPlanTemplateId);

        var moveStep = wandering.Steps.Single(step => step.Label == "move facing");
        var canMove = Assert.Single(moveStep.Checks);
        Assert.Equal(PlanCheckKind.CanMove, canMove.Kind);
        Assert.Equal("facing", canMove.DirectionVariable);
        Assert.Equal(PlanEffectKind.Move, moveStep.OnSuccess!.Kind);
        Assert.Equal("facing", moveStep.OnSuccess.DirectionVariable);

        var blockerStep = wandering.Steps.Single(step => step.Label == "handle blocker");
        var blocker = Assert.Single(blockerStep.Checks);
        Assert.Equal(PlanCheckKind.BlockingEntity, blocker.Kind);
        Assert.Equal("facing", blocker.DirectionVariable);
        Assert.Equal("target", blocker.TargetVariable);
        Assert.Equal(PlanEffectKind.ReverseDirection, blockerStep.OnFailure!.Kind);
        Assert.Equal("facing", blockerStep.OnFailure.DirectionVariable);

        var pickupStep = handleBlocker.Steps.Single(step => step.Label == "pickup blocker");
        var canPickup = Assert.Single(pickupStep.Checks);
        Assert.Equal(PlanCheckKind.CanPickup, canPickup.Kind);
        Assert.Equal("target", canPickup.TargetVariable);
        Assert.Equal(new GridCoord(0, 0), canPickup.InventoryCoord);
        Assert.Equal(PlanEffectKind.Pickup, pickupStep.OnSuccess!.Kind);
        Assert.Equal("target", pickupStep.OnSuccess.TargetVariable);
        Assert.Equal(new GridCoord(0, 0), pickupStep.OnSuccess.InventoryCoord);
    }

    [Fact]
    public void SlimeTemplateDefinesDefaultPlanIdAndVariables()
    {
        var template = PrototypeContent.CreateSlimeTemplate();

        Assert.Equal(PrototypeContent.WanderingActionPlanTemplateId, template.DefaultActionPlanId);
        var variables = template.DefaultPlanVariables ?? throw new InvalidOperationException("Slime template should define default plan variables.");
        Assert.True(variables.TryGetValue("facing", out var facing));
        Assert.Equal(PlanValueKind.Direction, facing.Kind);
        Assert.Equal(Direction.West, facing.DirectionValue);
    }

    [Fact]
    public void SpawnEntityFromTemplateIdCreatesExpectedInstance()
    {
        var registry = PrototypeContent.CreateRegistry();
        var world = PrototypeContent.CreateFirstSlice().World;
        var spawnId = new EntityId("registryRock");

        var result = registry.SpawnEntity(
            world,
            PrototypeContent.RockTemplateId,
            new EntitySpawnOptions(spawnId, new PlaneCoord(PrototypeContent.GameInventoryPlaneId, new GridCoord(0, 0))));

        Assert.Equal(spawnId, result.EntityId);
        Assert.Null(result.ActionPlan);
        Assert.Equal("Rock@world(0,0)", world.FormatEntityAddress(spawnId));
    }

    [Fact]
    public void SpawnEntityFromTemplateIdUsesTemplateDefaultActionPlan()
    {
        var registry = PrototypeContent.CreateRegistry();
        var world = PrototypeContent.CreateFirstSlice().World;
        var spawnId = new EntityId("registrySlime");

        var result = registry.SpawnEntity(
            world,
            PrototypeContent.SlimeTemplateId,
            new EntitySpawnOptions(spawnId, new PlaneCoord(PrototypeContent.GameInventoryPlaneId, new GridCoord(4, 4))));
        var turns = new TurnService(
            new MovementService(),
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [spawnId] = result.ActionPlan!
            });

        turns.AdvanceAfterPlayerTurn(world);

        Assert.NotNull(result.ActionPlan);
        Assert.Equal("Slime@world(3,4)", world.FormatEntityAddress(spawnId));
    }

    [Fact]
    public void SpawnEntityFromTemplateIdCanOverrideDefaultPlanVariables()
    {
        var registry = PrototypeContent.CreateRegistry();
        var world = PrototypeContent.CreateFirstSlice().World;
        var spawnId = new EntityId("eastFacingSlime");

        var result = registry.SpawnEntity(
            world,
            PrototypeContent.SlimeTemplateId,
            new EntitySpawnOptions(
                spawnId,
                new PlaneCoord(PrototypeContent.GameInventoryPlaneId, new GridCoord(0, 4)),
                PlanVariableOverrides: new Dictionary<string, PlanValueDescriptor>
                {
                    ["facing"] = PlanValueDescriptor.Direction(Direction.East)
                }));
        var turns = new TurnService(
            new MovementService(),
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [spawnId] = result.ActionPlan!
            });

        turns.AdvanceAfterPlayerTurn(world);

        Assert.NotNull(result.ActionPlan);
        Assert.Equal("Slime@world(1,4)", world.FormatEntityAddress(spawnId));
    }

    [Fact]
    public void RegistrySpawnCanOverrideTemplateActionPlanByDescriptorId()
    {
        var registry = PrototypeContent.CreateRegistry();
        var world = PrototypeContent.CreateFirstSlice().World;
        var spawnId = new EntityId("wanderingRegistryRock");

        var result = registry.SpawnEntity(
            world,
            PrototypeContent.RockTemplateId,
            new EntitySpawnOptions(
                spawnId,
                new PlaneCoord(PrototypeContent.GameInventoryPlaneId, new GridCoord(4, 4)),
                ActionPlanOverrideId: PrototypeContent.WanderingActionPlanTemplateId,
                PlanVariableOverrides: new Dictionary<string, PlanValueDescriptor>
                {
                    ["facing"] = PlanValueDescriptor.Direction(Direction.West)
                }));
        var turns = new TurnService(
            new MovementService(),
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [spawnId] = result.ActionPlan!
            });

        turns.AdvanceAfterPlayerTurn(world);

        Assert.NotNull(result.ActionPlan);
        Assert.Equal("Rock@world(3,4)", world.FormatEntityAddress(spawnId));
    }

    [Fact]
    public void CreateFirstSliceCollectsActionPlansFromRegistryDrivenSpawns()
    {
        var slice = PrototypeContent.CreateFirstSlice();

        Assert.Contains(PrototypeContent.SlimeId, slice.ActionPlans.Keys);
        Assert.Contains(PrototypeContent.GiantSlimeId, slice.ActionPlans.Keys);
        Assert.DoesNotContain(PrototypeContent.RockId, slice.ActionPlans.Keys);
    }

    [Fact]
    public void RegistrySpawnResolvesCarriedEntitiesByTemplateId()
    {
        var registry = PrototypeContent.CreateRegistry();
        var world = PrototypeContent.CreateFirstSlice().World;
        var bagId = new EntityId("registryBag");
        var carriedRockId = new EntityId("registryCarriedRock");
        var template = new EntityTemplate(
            "Registry Bag",
            InventoryWidth: 2,
            InventoryHeight: 1,
            Weight: 1,
            CarryingCapacity: 10,
            CarriedEntities:
            [
                new CarriedEntityTemplate(carriedRockId, PrototypeContent.RockTemplateId, new GridCoord(1, 0))
            ]);
        var templateId = new EntityTemplateId("registryBag");
        registry = registry.WithEntityTemplate(templateId, template);

        registry.SpawnEntity(
            world,
            templateId,
            new EntitySpawnOptions(bagId, new PlaneCoord(PrototypeContent.GameInventoryPlaneId, new GridCoord(0, 0))));

        Assert.Equal("Registry Bag@world(0,0)", world.FormatEntityAddress(bagId));
        Assert.Equal("Rock@registryBag(1,0)", world.FormatEntityAddress(carriedRockId));
    }

    [Fact]
    public void RegistrySpawnCollectsActionPlansFromCarriedEntities()
    {
        var registry = PrototypeContent.CreateRegistry();
        var world = PrototypeContent.CreateFirstSlice().World;
        var bagId = new EntityId("slimeBag");
        var carriedSlimeId = new EntityId("carriedSlime");
        var template = new EntityTemplate(
            "Slime Bag",
            InventoryWidth: 2,
            InventoryHeight: 1,
            Weight: 1,
            CarryingCapacity: 10,
            CarriedEntities:
            [
                new CarriedEntityTemplate(carriedSlimeId, PrototypeContent.SlimeTemplateId, new GridCoord(0, 0))
            ]);
        var templateId = new EntityTemplateId("slimeBag");
        registry = registry.WithEntityTemplate(templateId, template);

        var result = registry.SpawnEntity(
            world,
            templateId,
            new EntitySpawnOptions(bagId, new PlaneCoord(PrototypeContent.GameInventoryPlaneId, new GridCoord(0, 0))));

        Assert.Null(result.ActionPlan);
        Assert.Contains(carriedSlimeId, result.ActionPlans.Keys);
        Assert.IsType<InterpretedEntityActionPlan>(result.ActionPlans[carriedSlimeId]);
    }
}

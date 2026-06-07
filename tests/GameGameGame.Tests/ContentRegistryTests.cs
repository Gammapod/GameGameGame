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
    public void PrototypeRegistryExposesEntityTemplatesForEditorEnumeration()
    {
        var registry = PrototypeContent.CreateRegistry();

        Assert.Contains(PrototypeContent.RockTemplateId, registry.EntityTemplates.Keys);
        Assert.Equal("Rock", registry.EntityTemplates[PrototypeContent.RockTemplateId].Name);
        Assert.Contains(PrototypeContent.SlimeTemplateId, registry.EntityTemplates.Keys);
        Assert.Equal("Slime", registry.EntityTemplates[PrototypeContent.SlimeTemplateId].Name);
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
    public void PrototypeRegistryExposesPresentationsForEditorEnumeration()
    {
        var registry = PrototypeContent.CreateRegistry();

        Assert.Contains(PrototypeContent.RockTemplateId, registry.Presentations.Keys);
        Assert.Equal('*', registry.Presentations[PrototypeContent.RockTemplateId].Glyph);
        Assert.Contains(PrototypeContent.SlimeTemplateId, registry.Presentations.Keys);
        Assert.Equal(PresentationColor.Green, registry.Presentations[PrototypeContent.SlimeTemplateId].Color);
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
        Assert.Equal('*', reloaded.Presentations[new EntityTemplateId("rock")].Glyph);
        Assert.Equal(PlanEffectKind.Wait, reloaded.ActionPlanDescriptors[new ActionPlanTemplateId("wait")].Steps.Single().OnSuccess!.Kind);
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
                Weight: 20,
                CarryingCapacity: 20),
            new EntityPresentation('S', PresentationColor.DarkGreen));

        var registry = EditableContentDocument.LoadYaml(document.SaveYaml()).ToRegistry();

        Assert.Equal(new EntityTemplateId("giantSlime"), id);
        Assert.Equal("Giant Slime", registry.EntityTemplates[id].Name);
        Assert.Equal('S', registry.Presentations[id].Glyph);
    }

    [Fact]
    public void ContentEditorServiceListsJoinedEntityPresets()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
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
            """));

        var preset = Assert.Single(editor.ListEntityPresets());

        Assert.Equal(new EntityTemplateId("rock"), preset.Id);
        Assert.Equal("Rock", preset.Template.Name);
        Assert.Equal('*', preset.Presentation.Glyph);
        Assert.Equal(PresentationColor.Earth, preset.Presentation.Color);
    }

    [Fact]
    public void ContentEditorServiceUpdatesEntityPresetAndPresentation()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
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
            """));
        var id = new EntityTemplateId("rock");

        editor.UpdateEntityPreset(
            id,
            editor.GetEntityPreset(id).Template with
            {
                Name = "Heavy Rock",
                Weight = 5
            },
            new EntityPresentation('R', PresentationColor.Gray));
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.Equal("Heavy Rock", registry.EntityTemplates[id].Name);
        Assert.Equal(5, registry.EntityTemplates[id].Weight);
        Assert.Equal('R', registry.Presentations[id].Glyph);
        Assert.Equal(PresentationColor.Gray, registry.Presentations[id].Color);
    }

    [Fact]
    public void ContentEditorServicePlacesAndMovesCarriedEntityInInventoryLayout()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              bag:
                name: Bag
                inventoryWidth: 2
                inventoryHeight: 1
                weight: 1
                carryingCapacity: 10
              rock:
                name: Rock
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 3
                carryingCapacity: 3
            presentations:
              bag:
                glyph: b
                color: Gray
              rock:
                glyph: '*'
                color: Earth
            actionPlans: {}
            """));
        var bagId = new EntityTemplateId("bag");
        var carriedId = new EntityId("carriedRock");

        editor.PlaceCarriedEntity(bagId, carriedId, new EntityTemplateId("rock"), new GridCoord(0, 0));
        editor.MoveCarriedEntity(bagId, carriedId, new GridCoord(1, 0));
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.True(registry.Validate().IsValid);
        var carried = Assert.Single(registry.EntityTemplates[bagId].CarriedEntities!);
        Assert.Equal(carriedId, carried.EntityId);
        Assert.Equal(new EntityTemplateId("rock"), carried.TemplateId);
        Assert.Equal(new GridCoord(1, 0), carried.Coord);
    }

    [Fact]
    public void ContentEditorServiceListsActionPlans()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              wait:
                id: wait
                steps:
                  - label: wait
                    checks: []
                    onSuccess:
                      kind: Wait
            """));

        var plan = Assert.Single(editor.ListActionPlans());

        Assert.Equal(new ActionPlanTemplateId("wait"), plan.TemplateId);
        Assert.Equal(new ActionPlanId("wait"), plan.Descriptor.Id);
        Assert.Equal("wait", Assert.Single(plan.Descriptor.Steps).Label);
    }

    [Fact]
    public void ContentEditorServiceAddsReordersAndRemovesActionPlanSteps()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              simple:
                id: simple
                steps: []
            """));
        var planId = new ActionPlanTemplateId("simple");

        editor.AddActionPlanStep(planId, new ActionPlanStepDescriptor("first", [], PlanEffectDescriptor.Wait(), OnFailure: null));
        editor.AddActionPlanStep(planId, new ActionPlanStepDescriptor("second", [], PlanEffectDescriptor.Wait(), OnFailure: null));
        editor.MoveActionPlanStep(planId, fromIndex: 1, toIndex: 0);
        editor.RemoveActionPlanStep(planId, index: 1);
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        var step = Assert.Single(registry.ActionPlanDescriptors[planId].Steps);
        Assert.Equal("second", step.Label);
    }

    [Fact]
    public void ContentEditorServiceSetsActionPlanStepChecksAndEffects()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
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
                  - label: move
                    checks: []
                    onSuccess:
                      kind: Wait
            """));
        var planId = new ActionPlanTemplateId("wandering");

        editor.UpdateActionPlanStep(
            planId,
            index: 0,
            new ActionPlanStepDescriptor(
                "move facing",
                [PlanCheckDescriptor.CanMove("facing")],
                PlanEffectDescriptor.Move("facing"),
                OnFailure: null));
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.True(registry.Validate().IsValid);
        var step = Assert.Single(registry.ActionPlanDescriptors[planId].Steps);
        Assert.Equal(PlanCheckKind.CanMove, Assert.Single(step.Checks).Kind);
        Assert.Equal(PlanEffectKind.Move, step.OnSuccess!.Kind);
    }

    [Fact]
    public void ContentEditorServiceEditsTemplateDefaultPlanVariables()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: wandering
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
            """));
        var slimeId = new EntityTemplateId("slime");

        editor.SetDefaultPlanVariable(slimeId, "facing", PlanValueDescriptor.Direction(Direction.East));
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.True(registry.Validate().IsValid);
        Assert.Equal(Direction.East, registry.EntityTemplates[slimeId].DefaultPlanVariables!["facing"].DirectionValue);
    }

    [Fact]
    public void ContentEditorServiceValidatesCurrentDocumentAfterEdits()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
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
            """));
        var id = new EntityTemplateId("rock");

        editor.UpdateEntityPreset(
            id,
            editor.GetEntityPreset(id).Template with { Name = "Edited Rock" },
            new EntityPresentation('R', PresentationColor.White));
        var result = editor.Validate();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ContentEditorServiceValidationReportsCurrentDocumentErrors()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              bag:
                name: Bag
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 1
                carryingCapacity: 10
              rock:
                name: Rock
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 3
                carryingCapacity: 3
            presentations:
              bag:
                glyph: b
                color: Gray
              rock:
                glyph: '*'
                color: Earth
            actionPlans: {}
            """));

        editor.PlaceCarriedEntity(new EntityTemplateId("bag"), new EntityId("outsideRock"), new EntityTemplateId("rock"), new GridCoord(2, 0));
        var result = editor.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("outsideRock") && error.Contains("outside inventory bounds"));
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
    public void PrototypeRegistryExposesActionPlanDescriptorsForEditorEnumeration()
    {
        var registry = PrototypeContent.CreateRegistry();

        Assert.Contains(PrototypeContent.WanderingActionPlanTemplateId, registry.ActionPlanDescriptors.Keys);
        Assert.Equal("wandering", registry.ActionPlanDescriptors[PrototypeContent.WanderingActionPlanTemplateId].Id.Value);
        Assert.Contains(PrototypeContent.HandleBlockerActionPlanTemplateId, registry.ActionPlanDescriptors.Keys);
        Assert.Equal("handleBlocker", registry.ActionPlanDescriptors[PrototypeContent.HandleBlockerActionPlanTemplateId].Id.Value);
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
    public void PrototypeRegistryValidationReportsCarriedEntityOutsideInventoryBounds()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                new EntityTemplateId("badBag"),
                new EntityTemplate(
                    "Bad Bag",
                    InventoryWidth: 1,
                    InventoryHeight: 1,
                    Weight: 1,
                    CarryingCapacity: 10,
                    CarriedEntities:
                    [
                        new CarriedEntityTemplate(new EntityId("outsideRock"), PrototypeContent.RockTemplateId, new GridCoord(1, 0))
                    ]));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("badBag") && error.Contains("outsideRock") && error.Contains("outside inventory bounds"));
    }

    [Fact]
    public void PrototypeRegistryValidationReportsOverlappingCarriedEntities()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                new EntityTemplateId("crowdedBag"),
                new EntityTemplate(
                    "Crowded Bag",
                    InventoryWidth: 2,
                    InventoryHeight: 1,
                    Weight: 1,
                    CarryingCapacity: 10,
                    CarriedEntities:
                    [
                        new CarriedEntityTemplate(new EntityId("firstRock"), PrototypeContent.RockTemplateId, new GridCoord(0, 0)),
                        new CarriedEntityTemplate(new EntityId("secondRock"), PrototypeContent.RockTemplateId, new GridCoord(0, 0))
                    ]));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("crowdedBag") && error.Contains("firstRock") && error.Contains("secondRock") && error.Contains("overlap"));
    }

    [Fact]
    public void PrototypeRegistryValidationReportsDuplicateCarriedEntityIds()
    {
        var duplicateId = new EntityId("duplicateRock");
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                new EntityTemplateId("duplicateBag"),
                new EntityTemplate(
                    "Duplicate Bag",
                    InventoryWidth: 2,
                    InventoryHeight: 1,
                    Weight: 1,
                    CarryingCapacity: 10,
                    CarriedEntities:
                    [
                        new CarriedEntityTemplate(duplicateId, PrototypeContent.RockTemplateId, new GridCoord(0, 0)),
                        new CarriedEntityTemplate(duplicateId, PrototypeContent.RockTemplateId, new GridCoord(1, 0))
                    ]));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("duplicateBag") && error.Contains("duplicateRock") && error.Contains("duplicate carried entity ID"));
    }

    [Fact]
    public void PrototypeRegistryValidationReportsCarriedEntitiesOnTemplateWithoutUsableInventory()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                new EntityTemplateId("pocketlessBag"),
                new EntityTemplate(
                    "Pocketless Bag",
                    InventoryWidth: 0,
                    InventoryHeight: 0,
                    Weight: 1,
                    CarryingCapacity: 10,
                    CarriedEntities:
                    [
                        new CarriedEntityTemplate(new EntityId("trappedRock"), PrototypeContent.RockTemplateId, new GridCoord(0, 0))
                    ]));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("pocketlessBag") && error.Contains("trappedRock") && error.Contains("no usable inventory"));
    }

    [Fact]
    public void PrototypeRegistryValidationReportsMissingRequiredPlanVariable()
    {
        var planTemplateId = new ActionPlanTemplateId("needsDirection");
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                PrototypeContent.RockTemplateId,
                PrototypeContent.CreateRockTemplate() with { DefaultActionPlanId = planTemplateId })
            .WithActionPlanDescriptor(
                planTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("needsDirection"),
                    [
                        new ActionPlanStepDescriptor(
                            "move missing variable",
                            [PlanCheckDescriptor.CanMove("facing")],
                            PlanEffectDescriptor.Move("facing"),
                            OnFailure: null)
                    ]));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Rock") && error.Contains("facing") && error.Contains("missing required variable"));
    }

    [Fact]
    public void PrototypeRegistryValidationReportsPlanVariableTypeMismatch()
    {
        var planTemplateId = new ActionPlanTemplateId("wrongDirectionType");
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                PrototypeContent.RockTemplateId,
                PrototypeContent.CreateRockTemplate() with
                {
                    DefaultActionPlanId = planTemplateId,
                    DefaultPlanVariables = new Dictionary<string, PlanValueDescriptor>
                    {
                        ["facing"] = PlanValueDescriptor.Int(1)
                    }
                })
            .WithActionPlanDescriptor(
                planTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("wrongDirectionType"),
                    [
                        new ActionPlanStepDescriptor(
                            "move wrong variable type",
                            [PlanCheckDescriptor.CanMove("facing")],
                            PlanEffectDescriptor.Move("facing"),
                            OnFailure: null)
                    ]));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Rock") && error.Contains("facing") && error.Contains("expected Direction") && error.Contains("found Int"));
    }

    [Fact]
    public void PrototypeRegistryValidationAcceptsVariablesWrittenByChecksBeforeLaterReads()
    {
        var planTemplateId = new ActionPlanTemplateId("writeThenRead");
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                PrototypeContent.RockTemplateId,
                PrototypeContent.CreateRockTemplate() with
                {
                    DefaultActionPlanId = planTemplateId,
                    DefaultPlanVariables = new Dictionary<string, PlanValueDescriptor>
                    {
                        ["facing"] = PlanValueDescriptor.Direction(Direction.West)
                    }
                })
            .WithActionPlanDescriptor(
                planTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("writeThenRead"),
                    [
                        new ActionPlanStepDescriptor(
                            "find target",
                            [PlanCheckDescriptor.BlockingEntity("facing", "target")],
                            PlanEffectDescriptor.Pickup("target", new GridCoord(0, 0)),
                            OnFailure: null)
                    ]));

        var result = registry.Validate();

        Assert.DoesNotContain(result.Errors, error => error.Contains("target") && error.Contains("missing required variable"));
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

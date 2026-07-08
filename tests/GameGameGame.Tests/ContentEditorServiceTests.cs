using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class ContentEditorServiceTests
{
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
                Bulk = 5
            },
            new EntityPresentation('R', PresentationColor.Gray));
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.Equal("Heavy Rock", registry.EntityTemplates[id].Name);
        Assert.Equal(5, registry.EntityTemplates[id].Bulk);
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
    public void ContentEditorServiceRejectsCarriedPlacementOutsideInventoryBounds()
    {
        var editor = CreateInventoryEditor();

        var result = editor.ValidateCarriedEntityPlacement(new EntityTemplateId("bag"), new GridCoord(2, 0));

        Assert.False(result.IsSuccess);
        Assert.Equal("Cannot place carried entity at 2,0; it is outside inventory bounds 2x1 for bag.", result.ErrorMessage);
    }

    [Fact]
    public void ContentEditorServiceRejectsCarriedPlacementInOccupiedCellWithoutMutatingDocument()
    {
        var editor = CreateInventoryEditor();
        var bagId = new EntityTemplateId("bag");
        editor.PlaceCarriedEntity(bagId, new EntityId("carriedRock"), new EntityTemplateId("rock"), new GridCoord(0, 0));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            editor.PlaceCarriedEntity(bagId, new EntityId("secondRock"), new EntityTemplateId("rock"), new GridCoord(0, 0)));

        Assert.Equal("Cannot place carried entity at 0,0; cell is already occupied by carriedRock.", exception.Message);
        var carried = Assert.Single(EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry().EntityTemplates[bagId].CarriedEntities!);
        Assert.Equal(new EntityId("carriedRock"), carried.EntityId);
    }

    [Fact]
    public void ContentEditorServiceRejectsMoveToOccupiedCellWithoutMutatingDocument()
    {
        var editor = CreateInventoryEditor(width: 2, height: 1);
        var bagId = new EntityTemplateId("bag");
        editor.PlaceCarriedEntity(bagId, new EntityId("firstRock"), new EntityTemplateId("rock"), new GridCoord(0, 0));
        editor.PlaceCarriedEntity(bagId, new EntityId("secondRock"), new EntityTemplateId("rock"), new GridCoord(1, 0));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            editor.MoveCarriedEntity(bagId, new EntityId("firstRock"), new GridCoord(1, 0)));

        Assert.Equal("Cannot place carried entity at 1,0; cell is already occupied by secondRock.", exception.Message);
        var carried = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry().EntityTemplates[bagId].CarriedEntities!;
        Assert.Equal(new GridCoord(0, 0), carried.Single(item => item.EntityId == new EntityId("firstRock")).Coord);
        Assert.Equal(new GridCoord(1, 0), carried.Single(item => item.EntityId == new EntityId("secondRock")).Coord);
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
    public void ContentEditorServiceCreatesActionPlanWithGeneratedIdAndWaitStep()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans: {}
            """));

        var id = editor.CreateActionPlan("New Plan");
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.Equal(new ActionPlanTemplateId("newPlan"), id);
        var plan = registry.ActionPlanDescriptors[id];
        Assert.Equal(new ActionPlanId("newPlan"), plan.Id);
        var step = Assert.Single(plan.Steps);
        Assert.Equal("wait", step.Label);
        Assert.Equal(PlanEffectKind.Wait, step.OnSuccess!.Kind);
        Assert.True(registry.Validate().IsValid);
    }

    [Fact]
    public void ContentEditorServiceDuplicatesActionPlanWithGeneratedId()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              wander:
                id: wander
                steps:
                  - label: move
                    checks:
                      - kind: CanMove
                        directionVariable: facing
                    onSuccess:
                      kind: Move
                      directionVariable: facing
            """));

        var id = editor.DuplicateActionPlan(new ActionPlanTemplateId("wander"), "Wander Copy");
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.Equal(new ActionPlanTemplateId("wanderCopy"), id);
        var plan = registry.ActionPlanDescriptors[id];
        Assert.Equal(new ActionPlanId("wanderCopy"), plan.Id);
        var step = Assert.Single(plan.Steps);
        Assert.Equal("move", step.Label);
        Assert.Equal(PlanCheckKind.CanMove, Assert.Single(step.Checks).Kind);
        Assert.Equal(PlanEffectKind.Move, step.OnSuccess!.Kind);
    }

    [Fact]
    public void ContentEditorServiceDeletesUnreferencedActionPlan()
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

        var result = editor.DeleteActionPlan(new ActionPlanTemplateId("wait"));

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Empty(editor.Document.ActionPlans);
    }

    [Fact]
    public void ContentEditorServiceBlocksDeletingReferencedActionPlan()
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
                defaultActionPlanId: wait
            presentations:
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
              caller:
                id: caller
                steps:
                  - label: call
                    checks: []
                    onSuccess:
                      kind: CallPlan
                      planId: wait
            """));

        var references = editor.ListActionPlanReferences(new ActionPlanTemplateId("wait"));
        var result = editor.DeleteActionPlan(new ActionPlanTemplateId("wait"));

        Assert.Contains(references, reference => reference.EntityTemplateId == new EntityTemplateId("slime"));
        Assert.Contains(references, reference => reference.ActionPlanTemplateId == new ActionPlanTemplateId("caller"));
        Assert.False(result.IsSuccess);
        Assert.Contains("slime", result.ErrorMessage);
        Assert.Contains("caller", result.ErrorMessage);
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
        Assert.Equal(Direction.East, registry.EntityTemplates[slimeId].ActionStateDefaults!.Facing);
        Assert.Null(registry.EntityTemplates[slimeId].DefaultPlanVariables);
    }

    [Fact]
    public void ContentEditorServiceEditsCanonicalActorActionStateDefaults()
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
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans: {}
            """));
        var slimeId = new EntityTemplateId("slime");

        editor.SetInitialFacing(slimeId, Direction.East);
        var defaults = editor.GetActionStateDefaults(slimeId);
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.Equal(Direction.East, defaults.Facing);
        Assert.Equal(Direction.East, registry.EntityTemplates[slimeId].ActionStateDefaults!.Facing);
        Assert.Null(registry.EntityTemplates[slimeId].DefaultPlanVariables);

        editor.ClearInitialFacing(slimeId);
        var cleared = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.Null(cleared.EntityTemplates[slimeId].ActionStateDefaults?.Facing);
    }

    [Fact]
    public void ContentEditorServiceAddsAndUpdatesCanonicalActionPlanChecks()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              wandering:
                id: wandering
                steps:
                  - label: wait
                    checks: []
                    onSuccess:
                      kind: Wait
            """));
        var planId = new ActionPlanTemplateId("wandering");

        editor.AddActionPlanCheck(planId, stepIndex: 0, PlanCheckKind.CanMove);
        editor.UpdateActionPlanCheck(planId, stepIndex: 0, checkIndex: 0, PlanCheckKind.BlockingEntity);
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();
        var check = Assert.Single(registry.ActionPlanDescriptors[planId].Steps.Single().Checks);

        Assert.Equal(PlanCheckKind.BlockingEntity, check.Kind);
        Assert.Null(check.DirectionVariable);
        Assert.Null(check.TargetVariable);
    }

    [Fact]
    public void ContentEditorServiceSetsCanonicalActionPlanStepEffects()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              wandering:
                id: wandering
                steps:
                  - label: wait
                    checks: []
                    onSuccess:
                      kind: Wait
            """));
        var planId = new ActionPlanTemplateId("wandering");

        editor.SetActionPlanStepSuccessEffect(planId, stepIndex: 0, PlanEffectKind.Move);
        editor.SetActionPlanStepFailureEffect(planId, stepIndex: 0, PlanEffectKind.ReverseDirection);
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();
        var step = registry.ActionPlanDescriptors[planId].Steps.Single();

        Assert.Equal(PlanEffectKind.Move, step.OnSuccess!.Kind);
        Assert.Null(step.OnSuccess.DirectionVariable);
        Assert.Equal(PlanEffectKind.ReverseDirection, step.OnFailure!.Kind);
        Assert.Null(step.OnFailure.DirectionVariable);
    }

    [Fact]
    public void ContentEditorServiceSetsPrimitiveBackedActionPlan()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              moveFacing:
                id: moveFacing
                steps:
                  - label: wait
                    checks: []
                    onSuccess:
                      kind: Wait
              fallback:
                id: fallback
                steps:
                  - label: wait
                    checks: []
                    onSuccess:
                      kind: Wait
            """));
        var planId = new ActionPlanTemplateId("moveFacing");

        editor.SetActionPlanPrimitive(planId, ActionPlanPrimitiveKind.MoveFacing, new ActionPlanId("fallback"));
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();
        var descriptor = registry.ActionPlanDescriptors[planId];

        Assert.Empty(descriptor.Steps);
        Assert.Equal(ActionPlanPrimitiveKind.MoveFacing, descriptor.Primitive!.Kind);
        Assert.Equal(new ActionPlanId("fallback"), descriptor.Primitive.FallbackPlanId);
    }

    [Fact]
    public void ContentEditorServiceCreatesMoveFacingPickupTargetChain()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans: {}
            """));

        var chain = editor.CreateMoveFacingPickupTargetChain("Rat Wander", "Rat Pickup");
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();
        var move = registry.ActionPlanDescriptors[chain.MoveFacingPlanId];
        var pickup = registry.ActionPlanDescriptors[chain.PickupTargetPlanId];

        Assert.Equal(ActionPlanPrimitiveKind.MoveFacing, move.Primitive!.Kind);
        Assert.Equal(new ActionPlanId(chain.PickupTargetPlanId.Value), move.Primitive.FallbackPlanId);
        Assert.Equal(ActionPlanPrimitiveKind.PickupTarget, pickup.Primitive!.Kind);
        Assert.Empty(move.Steps);
        Assert.Empty(pickup.Steps);
    }

    [Fact]
    public void ContentEditorServiceCreatesCanonicalMoveFacingPickupTargetBehavior()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans: {}
            """));

        var planId = editor.CreateMoveFacingPickupTargetBehavior("Rat Behavior");
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();
        var descriptor = registry.ActionPlanDescriptors[planId];

        Assert.Equal(new ActionPlanTemplateId("ratBehavior"), planId);
        Assert.Null(descriptor.Primitive);
        Assert.Empty(descriptor.Steps);
        Assert.Equal(
            [ActionPlanBehaviorStepKind.MoveFacing, ActionPlanBehaviorStepKind.PickupTarget],
            descriptor.Behavior!.Steps.Select(step => step.Kind).ToArray());
    }

    [Fact]
    public void ContentEditorServiceSetsBehaviorStepTargetSlot()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              behavior:
                id: behavior
                behavior:
                  steps:
                    - kind: SeekTarget
            """));

        editor.SetActionPlanBehaviorStepTargetSlot(new ActionPlanTemplateId("behavior"), stepIndex: 0, targetSlot: 2);
        var descriptor = editor.ListActionPlans().Single(plan => plan.TemplateId == new ActionPlanTemplateId("behavior")).Descriptor;

        Assert.Equal(2, descriptor.Behavior!.Steps[0].TargetSlot);
        Assert.Contains("targetSlot: 2", editor.Document.SaveYaml());
    }

    [Fact]
    public void ContentEditorServiceSetsBehaviorStepTargetLabel()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              behavior:
                id: behavior
                behavior:
                  steps:
                    - kind: SeekTarget
                      targetSlot: 2
            """));

        editor.SetActionPlanBehaviorStepTargetLabel(new ActionPlanTemplateId("behavior"), stepIndex: 0, targetLabel: "fears");
        var descriptor = editor.ListActionPlans().Single(plan => plan.TemplateId == new ActionPlanTemplateId("behavior")).Descriptor;

        Assert.Equal("fears", descriptor.Behavior!.Steps[0].TargetLabel);
        Assert.Null(descriptor.Behavior!.Steps[0].TargetSlot);
        Assert.Contains("targetLabel: fears", editor.Document.SaveYaml());
        Assert.DoesNotContain("targetSlot: 2", editor.Document.SaveYaml());
    }

    [Fact]
    public void ContentEditorServiceSetsBehaviorStepPlanId()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              fear:
                id: fear
                behavior:
                  steps:
                    - kind: Backstep
              caster:
                id: caster
                behavior:
                  steps:
                    - kind: ApplyPrePlan
            """));

        editor.SetActionPlanBehaviorStepPlanId(new ActionPlanTemplateId("caster"), stepIndex: 0, new ActionPlanId("fear"));
        var descriptor = editor.ListActionPlans().Single(plan => plan.TemplateId == new ActionPlanTemplateId("caster")).Descriptor;

        Assert.Equal(new ActionPlanId("fear"), descriptor.Behavior!.Steps[0].PlanId);
        Assert.Contains("planId: fear", editor.Document.SaveYaml());
    }

    [Fact]
    public void ContentEditorServiceListsCanonicalActionStepMetadata()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans: {}
            """));

        var steps = editor.ListActionSteps();

        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.MoveFacing && step.DisplayName == "Move Facing");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.Backstep && step.DisplayName == "Backstep");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.PickupTarget && step.DisplayName == "Pickup Target");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.DropFacing && step.DisplayName == "Drop Facing");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.PushFacing && step.DisplayName == "Push Facing");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.DestroyTarget && step.DisplayName == "Destroy Target");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.CreateFacing && step.DisplayName == "Create Facing");
        Assert.DoesNotContain(steps, step => step.Kind == ActionPlanBehaviorStepKind.AcquireNearestTarget);
        Assert.DoesNotContain(steps, step => step.Kind == ActionPlanBehaviorStepKind.TurnLeft);
        Assert.DoesNotContain(steps, step => step.Kind == ActionPlanBehaviorStepKind.TurnRight);
        Assert.DoesNotContain(steps, step => step.Kind == ActionPlanBehaviorStepKind.ReverseFacing);
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo && step.DisplayName == "Maintain Chebyshev Distance Two");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.StrafeClockwise && step.DisplayName == "Strafe Clockwise");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.StrafeAnticlockwise && step.DisplayName == "Strafe Anticlockwise");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.ApplyPrePlan && step.DisplayName == "Apply Pre-Plan");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.ApplyMainPlan && step.DisplayName == "Apply Main Plan");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.ApplyPostPlan && step.DisplayName == "Apply Post-Plan");
    }

    [Fact]
    public void ContentEditorServiceRejectsLegacyMetadataSettingActionStepsForCanonicalAuthoring()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              behavior:
                id: behavior
            """));

        Assert.Throws<InvalidOperationException>(() => editor.AddActionPlanBehaviorStep(new ActionPlanTemplateId("behavior"), ActionPlanBehaviorStepKind.AcquireNearestTarget));
        Assert.Throws<InvalidOperationException>(() => editor.AddActionPlanBehaviorStep(new ActionPlanTemplateId("behavior"), ActionPlanBehaviorStepKind.TurnLeft));
    }

    [Fact]
    public void ContentEditorServicePreviewsCanonicalBehaviorPlan()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              rat:
                name: Rat
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 1
                carryingCapacity: 3
                defaultActionPlanId: ratBehavior
                actionStateDefaults:
                  facing: West
            presentations:
              rat:
                glyph: r
                color: Green
            actionPlans:
              ratBehavior:
                id: ratBehavior
                behavior:
                  steps:
                    - kind: MoveFacing
                    - kind: PickupTarget
            """));

        var preview = editor.PreviewActionPlan(new ActionPlanTemplateId("ratBehavior"), new EntityTemplateId("rat"));

        Assert.Equal("Canonical Behavior Chain", preview.Shape);
        Assert.Equal(["Move Facing", "Pickup Target"], preview.ActionSteps.Select(step => step.DisplayName).ToArray());
        Assert.Contains("Facing=West", preview.StateHints);
        Assert.Contains("Target=Self (defaultable)", preview.StateHints);
        Assert.Contains("Attempts to move", preview.ActionSteps[0].Hint);
        Assert.True(preview.ValidationDiagnostics.Count == 0, string.Join(Environment.NewLine, preview.ValidationDiagnostics));
        Assert.Contains("behavior:", preview.YamlPreview);
        Assert.Contains(preview.Guidance, line => line.Contains("canonical behavior chain", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ContentEditorServiceSetsCanonicalBehaviorChainAndClearsLegacyPlanShapes()
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
                defaultActionPlanId: wander
                actionStateDefaults:
                  facing: West
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              wander:
                id: wander
                primitive:
                  kind: MoveFacing
                  fallbackPlanId: pickup
              pickup:
                id: pickup
                primitive:
                  kind: PickupTarget
            """));
        var planId = new ActionPlanTemplateId("wander");

        editor.SetActionPlanBehavior(
            planId,
            [ActionPlanBehaviorStepKind.MoveFacing, ActionPlanBehaviorStepKind.PickupTarget]);
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();
        var descriptor = registry.ActionPlanDescriptors[planId];

        Assert.True(registry.Validate().IsValid, string.Join(Environment.NewLine, registry.Validate().Errors));
        Assert.Null(descriptor.Primitive);
        Assert.Empty(descriptor.Steps);
        Assert.Equal(
            [ActionPlanBehaviorStepKind.MoveFacing, ActionPlanBehaviorStepKind.PickupTarget],
            descriptor.Behavior!.Steps.Select(step => step.Kind).ToArray());
        Assert.Equal(Direction.West, registry.EntityTemplates[new EntityTemplateId("slime")].ActionStateDefaults!.Facing);
    }

    [Fact]
    public void ContentEditorServiceMaterializesDefaultFacingWhenAssigningBehaviorPlan()
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
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              wander:
                id: wander
                behavior:
                  steps:
                    - kind: MoveFacing
            """));
        var slimeId = new EntityTemplateId("slime");

        editor.SetDefaultActionPlan(slimeId, new ActionPlanTemplateId("wander"));
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.Equal(Direction.West, registry.EntityTemplates[slimeId].ActionStateDefaults!.Facing);
        Assert.True(registry.Validate().IsValid, string.Join(Environment.NewLine, registry.Validate().Errors));
    }

    [Fact]
    public void ContentEditorServiceClearsBehaviorWhenLastActionStepIsRemoved()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              wander:
                id: wander
                behavior:
                  steps:
                    - kind: MoveFacing
            """));
        var planId = new ActionPlanTemplateId("wander");

        editor.RemoveActionPlanBehaviorStep(planId, index: 0);
        var saved = editor.Document.SaveYaml();
        var descriptor = EditableContentDocument.LoadYaml(saved).ToRegistry().ActionPlanDescriptors[planId];

        Assert.Null(descriptor.Behavior);
        Assert.DoesNotContain("behavior:", saved);
    }

    [Fact]
    public void ContentEditorServiceAddsReordersAndRemovesCanonicalBehaviorSteps()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              wander:
                id: wander
                steps:
                  - label: wait
                    checks: []
                    onSuccess:
                      kind: Wait
            """));
        var planId = new ActionPlanTemplateId("wander");

        editor.AddActionPlanBehaviorStep(planId, ActionPlanBehaviorStepKind.PickupTarget);
        editor.AddActionPlanBehaviorStep(planId, ActionPlanBehaviorStepKind.MoveFacing);
        editor.MoveActionPlanBehaviorStep(planId, fromIndex: 1, toIndex: 0);
        editor.RemoveActionPlanBehaviorStep(planId, index: 1);
        var descriptor = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry().ActionPlanDescriptors[planId];

        var step = Assert.Single(descriptor.Behavior!.Steps);
        Assert.Equal(ActionPlanBehaviorStepKind.MoveFacing, step.Kind);
        Assert.Null(descriptor.Primitive);
        Assert.Empty(descriptor.Steps);
    }

    [Fact]
    public void ContentEditorServiceSetsTeleportActionPlanStepEffectDescriptor()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              movement:
                id: movement
                steps:
                  - label: wait
                    checks: []
                    onSuccess:
                      kind: Wait
            """));
        var planId = new ActionPlanTemplateId("movement");

        editor.SetActionPlanStepSuccessEffect(
            planId,
            stepIndex: 0,
            PlanEffectDescriptor.Teleport(
                MovementTargetDescriptor.Entity(new EntityId("rock")),
                MovementDestinationDescriptor.Plane(new PlaneCoord(new PlaneId("world"), new GridCoord(4, 2)))));
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();
        var effect = registry.ActionPlanDescriptors[planId].Steps.Single().OnSuccess!;

        Assert.Equal(PlanEffectKind.Teleport, effect.Kind);
        Assert.Equal(MovementTargetKind.Entity, effect.MovementTarget!.Kind);
        Assert.Equal(new EntityId("rock"), effect.MovementTarget.EntityId);
        Assert.Equal(new PlaneCoord(new PlaneId("world"), new GridCoord(4, 2)), effect.MovementDestination!.PlaneCoord);
    }

    [Fact]
    public void ContentEditorServiceSetsDropActionPlanStepEffectDescriptor()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              movement:
                id: movement
                steps:
                  - label: wait
                    checks: []
                    onSuccess:
                      kind: Wait
            """));
        var planId = new ActionPlanTemplateId("movement");

        editor.SetActionPlanStepSuccessEffect(
            planId,
            stepIndex: 0,
            PlanEffectDescriptor.Drop(
                MovementTargetDescriptor.CarriedInventoryCoord(new GridCoord(0, 0)),
                MovementDestinationDescriptor.AdjacentToSelf(Direction.West)));
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();
        var effect = registry.ActionPlanDescriptors[planId].Steps.Single().OnSuccess!;

        Assert.Equal(PlanEffectKind.Drop, effect.Kind);
        Assert.Equal(MovementTargetKind.CarriedInventoryCoord, effect.MovementTarget!.Kind);
        Assert.Equal(new GridCoord(0, 0), effect.MovementTarget.InventoryCoord);
        Assert.Equal(MovementDestinationKind.AdjacentToSelf, effect.MovementDestination!.Kind);
        Assert.Equal(Direction.West, effect.MovementDestination.Direction);
    }

    [Fact]
    public void ContentEditorServiceUpdatesMovementEffectTargetAndDestinationFields()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              movement:
                id: movement
                steps:
                  - label: teleport
                    checks: []
                    onSuccess:
                      kind: Teleport
                      movementTarget:
                        kind: Self
                      movementDestination:
                        kind: AdjacentToSelf
                        direction: East
            """));
        var planId = new ActionPlanTemplateId("movement");

        editor.SetActionPlanStepSuccessEffectMovementTarget(
            planId,
            stepIndex: 0,
            MovementTargetDescriptor.Entity(new EntityId("rock")));
        editor.SetActionPlanStepSuccessEffectMovementDestination(
            planId,
            stepIndex: 0,
            MovementDestinationDescriptor.Plane(new PlaneCoord(new PlaneId("world"), new GridCoord(4, 2))));

        var effect = EditableContentDocument.LoadYaml(editor.Document.SaveYaml())
            .ToRegistry()
            .ActionPlanDescriptors[planId]
            .Steps.Single().OnSuccess!;

        Assert.Equal(MovementTargetKind.Entity, effect.MovementTarget!.Kind);
        Assert.Equal(new EntityId("rock"), effect.MovementTarget.EntityId);
        Assert.Equal(MovementDestinationKind.PlaneCoord, effect.MovementDestination!.Kind);
        Assert.Equal(new PlaneCoord(new PlaneId("world"), new GridCoord(4, 2)), effect.MovementDestination.PlaneCoord);
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
                carriedEntities:
                  - entityId: outsideRock
                    templateId: rock
                    coord:
                      x: 2
                      y: 0
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

        var result = editor.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("outsideRock") && error.Contains("outside inventory bounds"));
    }

    [Fact]
    public void ContentEditorServiceCreatesEntityPresetWithGeneratedIdAndDefaultPresentation()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans: {}
            """));

        var id = editor.CreateEntityPreset("New Rock");
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.Equal(new EntityTemplateId("newRock"), id);
        Assert.Equal("New Rock", registry.EntityTemplates[id].Name);
        Assert.Equal('?', registry.Presentations[id].Glyph);
        Assert.Equal(PresentationColor.Gray, registry.Presentations[id].Color);
        Assert.True(registry.Validate().IsValid);
    }

    [Fact]
    public void ContentEditorServiceDuplicatesEntityPresetWithSafeIds()
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
                carriedEntities:
                  - entityId: carriedRock
                    templateId: rock
                    coord:
                      x: 0
                      y: 0
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

        var duplicateId = editor.DuplicateEntityPreset(new EntityTemplateId("bag"), "Bag Copy");
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.Equal(new EntityTemplateId("bagCopy"), duplicateId);
        Assert.Equal("Bag Copy", registry.EntityTemplates[duplicateId].Name);
        Assert.Equal('b', registry.Presentations[duplicateId].Glyph);
        var carried = Assert.Single(registry.EntityTemplates[duplicateId].CarriedEntities!);
        Assert.Equal(new EntityId("bagCopyCarriedRock"), carried.EntityId);
        Assert.Equal(new EntityTemplateId("rock"), carried.TemplateId);
    }

    [Fact]
    public void ContentEditorServiceReportsTemplateReferencesBeforeDeletion()
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
                carriedEntities:
                  - entityId: carriedRock
                    templateId: rock
                    coord:
                      x: 0
                      y: 0
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

        var references = editor.ListEntityTemplateReferences(new EntityTemplateId("rock"));
        var deleteResult = editor.DeleteEntityPreset(new EntityTemplateId("rock"));

        var reference = Assert.Single(references);
        Assert.Equal(new EntityTemplateId("bag"), reference.SourceTemplateId);
        Assert.Equal(new EntityId("carriedRock"), reference.CarriedEntityId);
        Assert.False(deleteResult.IsSuccess);
        Assert.Contains("carriedRock", deleteResult.ErrorMessage);
    }

    [Fact]
    public void ContentEditorServiceDeletesUnreferencedEntityPreset()
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

        var result = editor.DeleteEntityPreset(id);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.DoesNotContain(id.Value, editor.Document.EntityTemplates.Keys);
        Assert.DoesNotContain(id.Value, editor.Document.Presentations.Keys);
    }

    [Fact]
    public void ContentEditorServiceAssignsAndClearsDefaultActionPlan()
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
            presentations:
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
            """));
        var slimeId = new EntityTemplateId("slime");

        editor.SetDefaultActionPlan(slimeId, new ActionPlanTemplateId("wait"));
        var assigned = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();
        editor.ClearDefaultActionPlan(slimeId);
        var cleared = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.Equal(new ActionPlanTemplateId("wait"), assigned.EntityTemplates[slimeId].DefaultActionPlanId);
        Assert.Null(cleared.EntityTemplates[slimeId].DefaultActionPlanId);
    }

    [Fact]
    public void ContentEditorServiceListsCarriedEntitiesWithPresentationData()
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
                carriedEntities:
                  - entityId: carriedRock
                    templateId: rock
                    coord:
                      x: 1
                      y: 0
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

        var carried = Assert.Single(editor.ListCarriedEntities(new EntityTemplateId("bag")));

        Assert.Equal(new EntityId("carriedRock"), carried.EntityId);
        Assert.Equal(new EntityTemplateId("rock"), carried.TemplateId);
        Assert.Equal(new GridCoord(1, 0), carried.Coord);
        Assert.Equal("Rock", carried.Template.Name);
        Assert.Equal('*', carried.Presentation.Glyph);
    }

    [Fact]
    public void ContentEditorServicePlacesCarriedEntityWithGeneratedIdInFirstOpenCell()
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

        var carriedId = editor.PlaceCarriedEntity(bagId, new EntityTemplateId("rock"));
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.Equal(new EntityId("bagRock"), carriedId);
        var carried = Assert.Single(registry.EntityTemplates[bagId].CarriedEntities!);
        Assert.Equal(carriedId, carried.EntityId);
        Assert.Equal(new GridCoord(0, 0), carried.Coord);
    }

    [Fact]
    public void ContentEditorServiceFindsFirstOpenInventoryCell()
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
                carriedEntities:
                  - entityId: carriedRock
                    templateId: rock
                    coord:
                      x: 0
                      y: 0
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

        Assert.Equal(new GridCoord(1, 0), editor.FindFirstOpenInventoryCell(new EntityTemplateId("bag")));
    }

    [Fact]
    public void ContentEditorServiceRemovesCarriedEntity()
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
                carriedEntities:
                  - entityId: carriedRock
                    templateId: rock
                    coord:
                      x: 0
                      y: 0
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

        editor.RemoveCarriedEntity(bagId, new EntityId("carriedRock"));
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.Null(registry.EntityTemplates[bagId].CarriedEntities);
    }

    [Fact]
    public void ContentEditorServiceReplacesCarriedEntityTemplateReference()
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
                carriedEntities:
                  - entityId: carriedRock
                    templateId: rock
                    coord:
                      x: 0
                      y: 0
              rock:
                name: Rock
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 3
                carryingCapacity: 3
              gem:
                name: Gem
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
            presentations:
              bag:
                glyph: b
                color: Gray
              rock:
                glyph: '*'
                color: Earth
              gem:
                glyph: g
                color: Green
            actionPlans: {}
            """));
        var bagId = new EntityTemplateId("bag");

        editor.ReplaceCarriedEntityTemplate(bagId, new EntityId("carriedRock"), new EntityTemplateId("gem"));
        var carried = Assert.Single(EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry().EntityTemplates[bagId].CarriedEntities!);

        Assert.Equal(new EntityTemplateId("gem"), carried.TemplateId);
    }

    private static ContentEditorService CreateInventoryEditor(int width = 2, int height = 1) =>
        new(EditableContentDocument.LoadYaml(
            $$"""
            entityTemplates:
              bag:
                name: Bag
                inventoryWidth: {{width}}
                inventoryHeight: {{height}}
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
}

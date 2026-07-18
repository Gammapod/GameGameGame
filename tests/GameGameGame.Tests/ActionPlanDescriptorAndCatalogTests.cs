using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class ActionPlanDescriptorAndCatalogTests
{
    [Theory]
    [InlineData(ActionPlanShape.EmptyPassive)]
    [InlineData(ActionPlanShape.CanonicalBehaviorChain)]
    [InlineData(ActionPlanShape.TransitionalPrimitivePlan)]
    [InlineData(ActionPlanShape.LegacyLowLevelSteps)]
    [InlineData(ActionPlanShape.InvalidMixedShape)]
    [InlineData(ActionPlanShape.InvalidEmptyBehaviorChain)]
    public void ActionPlanShapeClassifierIdentifiesPlanShape(ActionPlanShape expectedShape)
    {
        var descriptor = expectedShape switch
        {
            ActionPlanShape.EmptyPassive => new ActionPlanDescriptor(new ActionPlanId("empty"), []),
            ActionPlanShape.CanonicalBehaviorChain => new ActionPlanDescriptor(
                new ActionPlanId("behavior"),
                [],
                Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing)])),
            ActionPlanShape.TransitionalPrimitivePlan => new ActionPlanDescriptor(
                new ActionPlanId("primitive"),
                [],
                Primitive: new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing)),
            ActionPlanShape.LegacyLowLevelSteps => new ActionPlanDescriptor(
                new ActionPlanId("legacy"),
                [new ActionPlanStepDescriptor("wait", [], PlanEffectDescriptor.Wait(), OnFailure: null)]),
            ActionPlanShape.InvalidMixedShape => new ActionPlanDescriptor(
                new ActionPlanId("mixed"),
                [new ActionPlanStepDescriptor("wait", [], PlanEffectDescriptor.Wait(), OnFailure: null)],
                Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing)])),
            ActionPlanShape.InvalidEmptyBehaviorChain => new ActionPlanDescriptor(
                new ActionPlanId("empty-behavior"),
                [],
                Behavior: new ActionPlanBehaviorDescriptor([])),
            _ => throw new ArgumentOutOfRangeException(nameof(expectedShape))
        };

        Assert.Equal(expectedShape, ActionPlanShapeClassifier.Classify(descriptor));
    }

    [Fact]
    public void PlanVariableRefReadsTypedVariableFromContext()
    {
        var context = new ActionPlanContext();
        var facingRef = new PlanVariableRef<DirectionPlanValue>("facing");
        context.Set("facing", new DirectionPlanValue(Direction.West));

        Assert.True(facingRef.TryRead(context, out var value));
        Assert.Equal(Direction.West, value.Value);
        Assert.Equal("facing", facingRef.Name);
    }

    [Fact]
    public void LiteralCoordValueSourceExposesSerializableLiteralValue()
    {
        var source = new LiteralCoordValueSource(new GridCoord(1, 0));

        Assert.Equal(new GridCoord(1, 0), source.Value);
        Assert.Equal("(1,0)", source.ToString());
    }

    [Fact]
    public void PlanValueDescriptorKeepsInitialVariablesAsData()
    {
        var descriptor = PlanValueDescriptor.Direction(Direction.West);

        Assert.Equal(PlanValueKind.Direction, descriptor.Kind);
        Assert.Equal(Direction.West, descriptor.DirectionValue);
        Assert.Equal(new DirectionPlanValue(Direction.West), descriptor.Materialize());
    }

    [Fact]
    public void ActionPlanDescriptorKeepsBuiltInInputsAsData()
    {
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("descriptor-test"),
            [
                new ActionPlanStepDescriptor(
                    "move facing",
                    [PlanCheckDescriptor.CanMove("facing")],
                    PlanEffectDescriptor.Move("facing"),
                    PlanEffectDescriptor.ReverseDirection("facing", consumesTurn: false, continuePlan: true))
            ]);

        var step = Assert.Single(descriptor.Steps);
        var check = Assert.Single(step.Checks);

        Assert.Equal(PlanCheckKind.CanMove, check.Kind);
        Assert.Equal("facing", check.DirectionVariable);
        Assert.Equal(PlanEffectKind.Move, step.OnSuccess!.Kind);
        Assert.Equal("facing", step.OnSuccess.DirectionVariable);
        Assert.Equal(PlanEffectKind.ReverseDirection, step.OnFailure!.Kind);
        Assert.False(step.OnFailure.ConsumesTurn);
        Assert.True(step.OnFailure.ContinuePlan);
    }

    [Fact]
    public void PlanPrimitiveCatalogExposesAllCheckEffectAndValueKinds()
    {
        Assert.Equal(Enum.GetValues<PlanCheckKind>().Order(), PlanPrimitiveCatalog.Checks.Select(check => check.Kind).Order());
        Assert.Equal(Enum.GetValues<PlanEffectKind>().Order(), PlanPrimitiveCatalog.Effects.Select(effect => effect.Kind).Order());
        Assert.Equal(Enum.GetValues<PlanValueKind>().Order(), PlanPrimitiveCatalog.ValueKinds.Select(value => value.Kind).Order());
    }

    [Fact]
    public void PlanPrimitiveCatalogDescribesCheckFieldsAndVariableContracts()
    {
        var canMove = PlanPrimitiveCatalog.GetCheck(PlanCheckKind.CanMove);
        var blockingEntity = PlanPrimitiveCatalog.GetCheck(PlanCheckKind.BlockingEntity);

        var canMoveField = Assert.Single(canMove.Fields);
        Assert.Equal("directionVariable", canMoveField.Name);
        Assert.Equal(PlanPrimitiveFieldKind.VariableRead, canMoveField.Kind);
        Assert.Equal(PlanValueKind.Direction, canMoveField.ValueKind);
        Assert.True(canMoveField.IsRequired);

        Assert.Contains(blockingEntity.Fields, field =>
            field.Name == "directionVariable"
            && field.Kind == PlanPrimitiveFieldKind.VariableRead
            && field.ValueKind == PlanValueKind.Direction);
        Assert.Contains(blockingEntity.Fields, field =>
            field.Name == "targetVariable"
            && field.Kind == PlanPrimitiveFieldKind.VariableWrite
            && field.ValueKind == PlanValueKind.Entity);
    }

    [Fact]
    public void PlanPrimitiveCatalogDescribesEffectFieldsAndReferences()
    {
        var pickup = PlanPrimitiveCatalog.GetEffect(PlanEffectKind.Pickup);
        var callPlan = PlanPrimitiveCatalog.GetEffect(PlanEffectKind.CallPlan);
        var setVariable = PlanPrimitiveCatalog.GetEffect(PlanEffectKind.SetVariable);

        Assert.Contains(pickup.Fields, field =>
            field.Name == "targetVariable"
            && field.Kind == PlanPrimitiveFieldKind.VariableRead
            && field.ValueKind == PlanValueKind.Entity);
        Assert.Contains(pickup.Fields, field =>
            field.Name == "inventoryCoord"
            && field.Kind == PlanPrimitiveFieldKind.CoordLiteral);
        Assert.Contains(callPlan.Fields, field =>
            field.Name == "planId"
            && field.Kind == PlanPrimitiveFieldKind.ActionPlanReference);
        Assert.Contains(setVariable.Fields, field =>
            field.Name == "variableName"
            && field.Kind == PlanPrimitiveFieldKind.VariableWrite);
        Assert.Contains(setVariable.Fields, field =>
            field.Name == "value"
            && field.Kind == PlanPrimitiveFieldKind.PlanValueLiteral);
    }

    [Fact]
    public void PlanPrimitiveCatalogDescribesCanonicalSlotUsage()
    {
        var canMove = PlanPrimitiveCatalog.GetCheck(PlanCheckKind.CanMove);
        var blockingEntity = PlanPrimitiveCatalog.GetCheck(PlanCheckKind.BlockingEntity);
        var pickup = PlanPrimitiveCatalog.GetEffect(PlanEffectKind.Pickup);
        var reverse = PlanPrimitiveCatalog.GetEffect(PlanEffectKind.ReverseDirection);

        Assert.Contains(canMove.SlotReads, slot => slot.Slot == ActionPlanSlot.Facing && slot.ValueKind == PlanValueKind.Direction);
        Assert.Contains(blockingEntity.SlotReads, slot => slot.Slot == ActionPlanSlot.Facing && slot.ValueKind == PlanValueKind.Direction);
        Assert.Contains(blockingEntity.SlotWrites, slot => slot.Slot == ActionPlanSlot.Target && slot.ValueKind == PlanValueKind.Entity);
        Assert.Contains(pickup.SlotReads, slot => slot.Slot == ActionPlanSlot.Target && slot.ValueKind == PlanValueKind.Entity);
        Assert.Contains(reverse.SlotReads, slot => slot.Slot == ActionPlanSlot.Facing && slot.ValueKind == PlanValueKind.Direction);
        Assert.Contains(reverse.SlotWrites, slot => slot.Slot == ActionPlanSlot.Facing && slot.ValueKind == PlanValueKind.Direction);
    }

    [Fact]
    public void ActionStepCatalogExposesAllCanonicalActionStepKinds()
    {
        Assert.Equal(
            Enum.GetValues<ActionPlanBehaviorStepKind>().Order(),
            ActionStepCatalog.Steps.Select(step => step.Kind).Order());
    }

    [Fact]
    public void ActionStepCatalogDescribesMoveFacingMetadata()
    {
        var moveFacing = ActionStepCatalog.Get(ActionPlanBehaviorStepKind.MoveFacing);

        Assert.Equal("Move Facing", moveFacing.DisplayName);
        Assert.Equal(ActionStepAuthoringTier.Stable, moveFacing.Tier);
        Assert.Contains("blocked", moveFacing.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(moveFacing.RequiredState, state => state.Slot == ActionPlanSlot.Facing && state.ValueKind == PlanValueKind.Direction);
        Assert.Contains(moveFacing.DefaultableState, state => state.Slot == ActionPlanSlot.Facing && state.ValueKind == PlanValueKind.Direction);
        Assert.Contains(moveFacing.StateWrites, state => state.Slot == ActionPlanSlot.Target && state.ValueKind == PlanValueKind.Entity);
    }

    [Fact]
    public void ActionStepCatalogDescribesBackstepMetadata()
    {
        var backstep = ActionStepCatalog.Get(ActionPlanBehaviorStepKind.Backstep);

        Assert.Equal("Backstep", backstep.DisplayName);
        Assert.Equal(ActionStepAuthoringTier.Stable, backstep.Tier);
        Assert.Contains("opposite", backstep.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("preserving", backstep.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(backstep.RequiredState, state => state.Slot == ActionPlanSlot.Facing && state.ValueKind == PlanValueKind.Direction);
        Assert.Contains(backstep.DefaultableState, state => state.Slot == ActionPlanSlot.Facing && state.ValueKind == PlanValueKind.Direction);
        Assert.Contains(backstep.StateWrites, state => state.Slot == ActionPlanSlot.Target && state.ValueKind == PlanValueKind.Entity);
        Assert.DoesNotContain(backstep.StateWrites, state => state.Slot == ActionPlanSlot.Facing);
    }

    [Fact]
    public void ActionStepCatalogDescribesPickupTargetMetadata()
    {
        var pickupTarget = ActionStepCatalog.Get(ActionPlanBehaviorStepKind.PickupTarget);

        Assert.Equal("Pickup Target", pickupTarget.DisplayName);
        Assert.Equal(ActionStepAuthoringTier.Stable, pickupTarget.Tier);
        Assert.Contains("pick up", pickupTarget.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(pickupTarget.RequiredState, state => state.Slot == ActionPlanSlot.Target && state.ValueKind == PlanValueKind.Entity);
        Assert.Contains(pickupTarget.DefaultableState, state => state.Slot == ActionPlanSlot.Target && state.ValueKind == PlanValueKind.Entity);
        Assert.Empty(pickupTarget.StateWrites);
        Assert.Equal(ActionPlanBehaviorStepKind.PickupTarget, pickupTarget.TargetCapability);
    }

    [Fact]
    public void ActionStepCatalogDescribesPreferredTransformAliasesForPickupAndDrop()
    {
        var adjacentToInventory = ActionStepCatalog.Get(ActionPlanBehaviorStepKind.TransformAdjacentToInventory);
        var inventoryToAdjacent = ActionStepCatalog.Get(ActionPlanBehaviorStepKind.TransformInventoryToAdjacent);

        Assert.Equal("Transform Adjacent To Inventory", adjacentToInventory.DisplayName);
        Assert.Contains("adjacent", adjacentToInventory.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("inventory", adjacentToInventory.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(adjacentToInventory.RequiredState, state => state.Slot == ActionPlanSlot.Target && state.ValueKind == PlanValueKind.Entity);
        Assert.Equal(ActionPlanBehaviorStepKind.TransformAdjacentToInventory, adjacentToInventory.TargetCapability);

        Assert.Equal("Transform Inventory To Adjacent", inventoryToAdjacent.DisplayName);
        Assert.Contains("inventory", inventoryToAdjacent.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("adjacent", inventoryToAdjacent.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(inventoryToAdjacent.RequiredState, state => state.Slot == ActionPlanSlot.Facing && state.ValueKind == PlanValueKind.Direction);
    }

    [Theory]
    [InlineData(ActionPlanBehaviorStepKind.PickupTarget)]
    [InlineData(ActionPlanBehaviorStepKind.TransformAdjacentToInventory)]
    [InlineData(ActionPlanBehaviorStepKind.EnterTarget)]
    [InlineData(ActionPlanBehaviorStepKind.GiveTarget)]
    [InlineData(ActionPlanBehaviorStepKind.TakeTarget)]
    [InlineData(ActionPlanBehaviorStepKind.DestroyTarget)]
    [InlineData(ActionPlanBehaviorStepKind.PushFacing)]
    public void ActionStepCatalogDeclaresTargetCapabilitiesForAffordanceTargeting(ActionPlanBehaviorStepKind kind)
    {
        var step = ActionStepCatalog.Get(kind);

        Assert.Equal(kind, step.TargetCapability);
        Assert.True(EntityInteractionAffordanceService.IsSupportedTargetCapability(kind));
    }

    [Theory]
    [InlineData(ActionPlanBehaviorStepKind.DropFacing, "Drop Facing")]
    [InlineData(ActionPlanBehaviorStepKind.PushFacing, "Push Facing")]
    [InlineData(ActionPlanBehaviorStepKind.DestroyTarget, "Destroy Target")]
    [InlineData(ActionPlanBehaviorStepKind.CreateFacing, "Create Facing")]
    [InlineData(ActionPlanBehaviorStepKind.SeekTarget, "Seek Target")]
    [InlineData(ActionPlanBehaviorStepKind.FleeTarget, "Flee Target")]
    [InlineData(ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo, "Maintain Chebyshev Distance Two")]
    [InlineData(ActionPlanBehaviorStepKind.StrafeClockwise, "Strafe Clockwise")]
    [InlineData(ActionPlanBehaviorStepKind.StrafeAnticlockwise, "Strafe Anticlockwise")]
    [InlineData(ActionPlanBehaviorStepKind.GiveTarget, "Give Target")]
    [InlineData(ActionPlanBehaviorStepKind.TakeTarget, "Take Target")]
    [InlineData(ActionPlanBehaviorStepKind.EnterTarget, "Enter Target")]
    [InlineData(ActionPlanBehaviorStepKind.ExitFacing, "Exit Facing")]
    [InlineData(ActionPlanBehaviorStepKind.ApplyPrePlan, "Apply Pre-Plan")]
    [InlineData(ActionPlanBehaviorStepKind.ApplyMainPlan, "Apply Main Plan")]
    [InlineData(ActionPlanBehaviorStepKind.ApplyPostPlan, "Apply Post-Plan")]
    public void ActionStepCatalogDescribesFirstUtilityBatch(ActionPlanBehaviorStepKind kind, string displayName)
    {
        var step = ActionStepCatalog.Get(kind);

        Assert.Equal(displayName, step.DisplayName);
        Assert.Equal(ActionStepAuthoringTier.Stable, step.Tier);
        Assert.NotEmpty(step.Description);
    }
}

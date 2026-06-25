using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class ContentEditorAuthoringTests
{
    [Fact]
    public void ContentEditorAuthorsPrimitiveBackedPlan()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var actorId = editor.CreateEntityPreset("Primitive Actor");
        var fallbackId = editor.CreateActionPlan("Fallback Wait");
        var planId = editor.CreateActionPlan("Primitive Move");

        editor.SetInitialFacing(actorId, Direction.North);
        editor.SetActionPlanPrimitive(planId, ActionPlanPrimitiveKind.MoveFacing, new ActionPlanId(fallbackId.Value));
        editor.SetDefaultActionPlan(actorId, planId);

        var yaml = document.SaveYaml();

        Assert.True(editor.Validate().IsValid, string.Join(Environment.NewLine, editor.Validate().Errors));
        Assert.Contains("primitive:", yaml);
        Assert.Contains("kind: MoveFacing", yaml);
        Assert.Contains("fallbackPlanId: fallbackWait", yaml);
    }

    [Fact]
    public void ContentEditorAuthorsMoveFacingToPickupTargetChain()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var actorId = editor.CreateEntityPreset("Primitive Collector");
        var pickupId = editor.CreateActionPlan("Pickup Target");
        var moveId = editor.CreateActionPlan("Move Facing");

        editor.SetInitialFacing(actorId, Direction.North);
        editor.SetActionPlanPrimitive(pickupId, ActionPlanPrimitiveKind.PickupTarget);
        editor.SetActionPlanPrimitive(moveId, ActionPlanPrimitiveKind.MoveFacing, new ActionPlanId(pickupId.Value));
        editor.SetDefaultActionPlan(actorId, moveId);

        var yaml = document.SaveYaml();

        Assert.True(editor.Validate().IsValid, string.Join(Environment.NewLine, editor.Validate().Errors));
        Assert.True(document.ValidateCanonicalAuthoring().IsValid, string.Join(Environment.NewLine, document.ValidateCanonicalAuthoring().Errors));
        Assert.Contains("kind: MoveFacing", yaml);
        Assert.Contains("fallbackPlanId: pickupTarget", yaml);
        Assert.Contains("kind: PickupTarget", yaml);
    }

    [Fact]
    public void ContentEditorListsCanonicalActionStepMetadata()
    {
        var editor = new ContentEditorService(new EditableContentDocument());

        var steps = editor.ListActionSteps();

        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.MoveFacing && step.DisplayName == "Move Facing");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.Backstep && step.DisplayName == "Backstep");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.PickupTarget && step.DisplayName == "Pickup Target");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.DropFacing && step.DisplayName == "Drop Facing");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.PushFacing && step.DisplayName == "Push Facing");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.DestroyTarget && step.DisplayName == "Destroy Target");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.CreateFacing && step.DisplayName == "Create Facing");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.TurnLeft && step.DisplayName == "Turn Left");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.TurnRight && step.DisplayName == "Turn Right");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.ReverseFacing && step.DisplayName == "Reverse Facing");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo && step.DisplayName == "Maintain Chebyshev Distance Two");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.StrafeClockwise && step.DisplayName == "Strafe Clockwise");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.StrafeAnticlockwise && step.DisplayName == "Strafe Anticlockwise");
    }

    [Fact]
    public void ContentEditorAuthorsCanonicalBehaviorChain()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var ratId = editor.CreateEntityPreset("Behavior Rat");
        editor.UpdateEntityPreset(
            ratId,
            new EntityTemplate("Behavior Rat", InventoryWidth: 1, InventoryHeight: 1, Weight: 1, CarryingCapacity: 3),
            new EntityPresentation('r', PresentationColor.Green));
        editor.SetInitialFacing(ratId, Direction.West);

        var planId = editor.CreateActionPlan("Rat Behavior");
        editor.SetActionPlanBehavior(planId, [ActionPlanBehaviorStepKind.MoveFacing, ActionPlanBehaviorStepKind.PickupTarget]);
        editor.SetDefaultActionPlan(ratId, planId);

        var yaml = document.SaveYaml();

        Assert.True(editor.Validate().IsValid, string.Join(Environment.NewLine, editor.Validate().Errors));
        Assert.True(document.ValidateCanonicalAuthoring().IsValid, string.Join(Environment.NewLine, document.ValidateCanonicalAuthoring().Errors));
        Assert.Contains("behavior:", yaml);
        Assert.Contains("kind: MoveFacing", yaml);
        Assert.Contains("kind: PickupTarget", yaml);
        Assert.DoesNotContain("primitive:", yaml);
        Assert.DoesNotContain("fallbackPlanId:", yaml);
        Assert.DoesNotContain("kind: CanMove", yaml);
        Assert.DoesNotContain("kind: CallPlan", yaml);
        Assert.DoesNotContain("kind: SetVariable", yaml);
    }

    [Fact]
    public void ContentEditorPreviewsCanonicalBehaviorPlan()
    {
        var editor = new ContentEditorService(new EditableContentDocument());
        var ratId = editor.CreateEntityPreset("Preview Rat");
        editor.UpdateEntityPreset(
            ratId,
            new EntityTemplate("Preview Rat", InventoryWidth: 1, InventoryHeight: 1, Weight: 1, CarryingCapacity: 3),
            new EntityPresentation('r', PresentationColor.Green));
        var planId = editor.CreateMoveFacingPickupTargetBehavior("Preview Rat Behavior");
        editor.SetDefaultActionPlan(ratId, planId);

        var preview = editor.PreviewActionPlan(planId, ratId);

        Assert.Equal("Canonical Behavior Chain", preview.Shape);
        Assert.Equal([ActionPlanBehaviorStepKind.MoveFacing, ActionPlanBehaviorStepKind.PickupTarget], preview.ActionSteps.Select(step => step.Kind).ToArray());
        Assert.Contains("Facing=West", preview.StateHints);
        Assert.Contains("Target=Self (defaultable)", preview.StateHints);
        Assert.Empty(preview.ValidationDiagnostics);
    }

    [Fact]
    public void ContentEditorAuthorsCanonicalBehaviorChainWithHelper()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var ratId = editor.CreateEntityPreset("Helper Rat");
        editor.UpdateEntityPreset(
            ratId,
            new EntityTemplate("Helper Rat", InventoryWidth: 1, InventoryHeight: 1, Weight: 1, CarryingCapacity: 3),
            new EntityPresentation('r', PresentationColor.Green));

        var planId = editor.CreateMoveFacingPickupTargetBehavior("Helper Rat Behavior");
        editor.SetDefaultActionPlan(ratId, planId);

        var yaml = document.SaveYaml();

        Assert.True(editor.Validate().IsValid, string.Join(Environment.NewLine, editor.Validate().Errors));
        Assert.True(document.ValidateCanonicalAuthoring().IsValid, string.Join(Environment.NewLine, document.ValidateCanonicalAuthoring().Errors));
        Assert.Contains("behavior:", yaml);
        Assert.Contains("kind: MoveFacing", yaml);
        Assert.Contains("kind: PickupTarget", yaml);
        Assert.Contains("facing: West", yaml);
        Assert.DoesNotContain("primitive:", yaml);
        Assert.DoesNotContain("fallbackPlanId:", yaml);
    }
}

using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class LegacyCoordinateTargetMovementRetirementTests
{
    [Theory]
    [InlineData(ActionPlanBehaviorStepKind.AcquireNearestTarget)]
    [InlineData(ActionPlanBehaviorStepKind.SeekTarget)]
    [InlineData(ActionPlanBehaviorStepKind.FleeTarget)]
    [InlineData(ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo)]
    [InlineData(ActionPlanBehaviorStepKind.StrafeClockwise)]
    [InlineData(ActionPlanBehaviorStepKind.StrafeAnticlockwise)]
    public void RuntimeRejectsRetiredLegacyTargetingAndCoordinateTargetMovementSteps(ActionPlanBehaviorStepKind kind)
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId($"retired-{kind}"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(kind)]));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext()));

        Assert.Contains("retired", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TargetPathMove", exception.Message, StringComparison.Ordinal);
    }
}

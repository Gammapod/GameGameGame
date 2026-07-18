using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class PlanOverrideActionStepTests
{
    [Fact]
    public void ApplyPrePlanBehaviorStepInstallsReferencedPlanOnTargetPreSlot()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var fearPlan = new ActionPlanDefinition(
            new ActionPlanId("fear"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Backstep)
            ]));
        var casterPlan = new ActionPlanDefinition(
            new ActionPlanId("caster"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.ApplyPrePlan, PlanId: fearPlan.Id)
            ]));

        var result = new ActionPlanInterpreter(
            new MovementService(),
            new Dictionary<ActionPlanId, ActionPlanDefinition>
            {
                [fearPlan.Id] = fearPlan,
                [casterPlan.Id] = casterPlan
            }).Execute(world, TestWorld.PlayerId, casterPlan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        var overridePlan = world.GetActionPlanOverride(TestWorld.SlimeId, ActionPlanOverrideSlot.Pre);
        Assert.NotNull(overridePlan);
        Assert.IsType<InterpretedPlanIntent>(Assert.Single(overridePlan!.Options));
        Assert.True(TraceContains(result.Trace, "Primitive ApplyPrePlan"));
    }

    [Theory]
    [InlineData(ActionPlanBehaviorStepKind.ApplyMainPlan, ActionPlanOverrideSlot.Main)]
    [InlineData(ActionPlanBehaviorStepKind.ApplyPostPlan, ActionPlanOverrideSlot.Post)]
    public void ApplyPlanBehaviorStepInstallsReferencedPlanOnTargetSlot(ActionPlanBehaviorStepKind stepKind, ActionPlanOverrideSlot slot)
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var overridePlanDefinition = new ActionPlanDefinition(
            new ActionPlanId("override"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Backstep)
            ]));
        var casterPlan = new ActionPlanDefinition(
            new ActionPlanId("caster"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(stepKind, PlanId: overridePlanDefinition.Id)
            ]));

        var result = new ActionPlanInterpreter(
            new MovementService(),
            new Dictionary<ActionPlanId, ActionPlanDefinition>
            {
                [overridePlanDefinition.Id] = overridePlanDefinition,
                [casterPlan.Id] = casterPlan
            }).Execute(world, TestWorld.PlayerId, casterPlan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.NotNull(world.GetActionPlanOverride(TestWorld.SlimeId, slot));
        Assert.True(TraceContains(result.Trace, $"Primitive {stepKind}"));
    }

    [Fact]
    public void ApplyPrePlanBehaviorStepFailsWhenReferencedPlanIsMissing()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var casterPlan = new ActionPlanDefinition(
            new ActionPlanId("caster"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.ApplyPrePlan, PlanId: new ActionPlanId("missing"))
            ]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.PlayerId,
            casterPlan,
            new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.Null(world.GetActionPlanOverride(TestWorld.SlimeId, ActionPlanOverrideSlot.Pre));
        Assert.True(TraceContains(result.Trace, "Primitive ApplyPrePlan"));
    }

    private static bool TraceContains(TraceNode trace, string label)
    {
        return trace.Label == label || trace.Children.Any(child => TraceContains(child, label));
    }
}

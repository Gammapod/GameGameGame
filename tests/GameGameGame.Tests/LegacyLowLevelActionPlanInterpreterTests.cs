using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class LegacyLowLevelActionPlanInterpreterTests
{
    [Fact]
    public void PlanInterpreterUsesFirstSuccessfulConsumingRankedStep()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        var executed = new List<string>();
        var plan = new ActionPlanDefinition(
            new ActionPlanId("test"),
            [
                new ActionPlanStep(
                    "blocked first step",
                    [new TestPlanCheck("first check", passed: false)],
                    new RecordingPlanEffect("first effect", executed, consumesTurn: true),
                    onFailure: null),
                new ActionPlanStep(
                    "fallback step",
                    [new TestPlanCheck("second check", passed: true)],
                    new RecordingPlanEffect("fallback effect", executed, consumesTurn: true),
                    onFailure: null),
                new ActionPlanStep(
                    "unreached step",
                    [new TestPlanCheck("third check", passed: true)],
                    new RecordingPlanEffect("unreached effect", executed, consumesTurn: true),
                    onFailure: null)
            ]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(["fallback effect"], executed);
        Assert.Equal(TraceStatus.Success, result.Trace.Status);
        Assert.Contains(result.Trace.Children, child => child.Label == "Step blocked first step" && child.Status == TraceStatus.Failure);
        Assert.Contains(result.Trace.Children, child => child.Label == "Step fallback step" && child.Status == TraceStatus.Success);
        Assert.DoesNotContain(result.Trace.Children, child => child.Label == "Step unreached step");
    }

    [Fact]
    public void PlanInterpreterCommitsCheckVariableWritesBeforeEffect()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        Direction? effectFacing = null;
        var plan = new ActionPlanDefinition(
            new ActionPlanId("variable-test"),
            [
                new ActionPlanStep(
                    "bind facing",
                    [new TestPlanCheck("bind east", passed: true, new Dictionary<string, PlanValue>
                    {
                        ["facing"] = new DirectionPlanValue(Direction.East)
                    })],
                    new ReadDirectionEffect("read facing", "facing", value => effectFacing = value),
                    onFailure: null)
            ]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.True(result.Succeeded);
        Assert.Equal(Direction.East, effectFacing);
        Assert.True(context.TryGet<DirectionPlanValue>("facing", out var storedFacing));
        Assert.Equal(Direction.East, storedFacing.Value);
        Assert.True(TraceContains(result.Trace, "Set variable facing"));
    }

    [Fact]
    public void PlanInterpreterReturnsFailureWhenNoStepConsumesOrStops()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        var plan = new ActionPlanDefinition(
            new ActionPlanId("all-fail"),
            [
                new ActionPlanStep("first", [new TestPlanCheck("first check", passed: false)], onSuccess: null, onFailure: null),
                new ActionPlanStep("second", [new TestPlanCheck("second check", passed: false)], onSuccess: null, onFailure: null)
            ]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.False(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.Equal(TraceStatus.Failure, result.Trace.Status);
        Assert.Contains("no step", result.Trace.Detail);
    }

    private static bool TraceContains(TraceNode trace, string label)
    {
        return trace.Label == label || trace.Children.Any(child => TraceContains(child, label));
    }

    private sealed record TestPlanCheck(
        string Label,
        bool passed,
        IReadOnlyDictionary<string, PlanValue>? Writes = null) : IPlanCheck
    {
        public PlanCheckResult Evaluate(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement) =>
            new(passed, Writes ?? new Dictionary<string, PlanValue>(), new TraceNode(Label, passed ? TraceStatus.Success : TraceStatus.Failure));
    }

    private sealed class RecordingPlanEffect(string label, List<string> executed, bool consumesTurn) : IPlanEffect
    {
        public PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
        {
            executed.Add(label);

            return new PlanEffectResult(
                Succeeded: true,
                ConsumesTurn: consumesTurn,
                ContinuePlan: !consumesTurn,
                TraceNode.Success(label));
        }
    }

    private sealed class ReadDirectionEffect(string label, string variableName, Action<Direction> read) : IPlanEffect
    {
        public PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
        {
            if (!context.TryGet<DirectionPlanValue>(variableName, out var value))
            {
                return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, TraceNode.Failure(label, FailureReason.None, $"missing {variableName}"));
            }

            read(value.Value);

            return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, TraceNode.Success(label, value.Value.ToString()));
        }
    }
}

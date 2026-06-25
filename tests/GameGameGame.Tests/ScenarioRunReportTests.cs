using System.Text;
using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.Headless;

namespace GameGameGame.Tests;

public sealed class ScenarioRunReportTests
{
    [Fact]
    public void ScenarioRunServiceCanUseEditorAuthoredTemporaryContentForReport()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Scenario Room");
        var actorTemplateId = editor.CreateEntityPreset("Scenario Actor");
        var rockTemplateId = editor.CreateEntityPreset("Scenario Rock");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Scenario Room", InventoryWidth: 4, InventoryHeight: 5, Weight: 100, CarryingCapacity: 100),
            new EntityPresentation('#', PresentationColor.Gray));
        editor.UpdateEntityPreset(
            actorTemplateId,
            new EntityTemplate("Scenario Actor", InventoryWidth: 3, InventoryHeight: 2, Weight: 10, CarryingCapacity: 5),
            new EntityPresentation('@', PresentationColor.White));
        editor.UpdateEntityPreset(
            rockTemplateId,
            new EntityTemplate("Scenario Rock", InventoryWidth: 0, InventoryHeight: 0, Weight: 3, CarryingCapacity: 3),
            new EntityPresentation('*', PresentationColor.Gray));
        editor.SetInitialFacing(actorTemplateId, Direction.East);
        editor.PlaceCarriedEntity(actorTemplateId, new EntityId("carriedRock"), rockTemplateId, new GridCoord(0, 0));
        var planTemplateId = editor.CreateActionPlan("Drop Facing");
        editor.SetActionPlanBehavior(planTemplateId, [ActionPlanBehaviorStepKind.DropFacing]);
        editor.SetDefaultActionPlan(actorTemplateId, planTemplateId);
        editor.PlaceCarriedEntity(scenarioRootId, new EntityId("actor"), actorTemplateId, new GridCoord(1, 2));
        var validation = editor.Validate();
        var canonicalValidation = document.ValidateCanonicalAuthoring();
        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        Assert.True(canonicalValidation.IsValid, string.Join(Environment.NewLine, canonicalValidation.Errors));

        var report = ScenarioRunService.Run(document, new ScenarioRunRequest(scenarioRootId, TurnCount: 1));

        Assert.Equal([new EntityId("actor")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        var turn = Assert.Single(report.Turns);
        Assert.Equal("Scenario Actor", turn.ActorName);
        Assert.Contains("1. DropFacing: Success; fallback=stopped", turn.TraceLines);
        Assert.Contains("   reads: Facing=East", turn.TraceLines);
        Assert.Contains("Scenario Actor: scenarioRoot(1,2), facing East, target none", report.FinalStateLines);
        Assert.Contains("Scenario Rock: scenarioRoot(2,2), facing none, target none", report.FinalStateLines);
        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeObservations);
        Assert.Empty(report.RuntimeFailures);
        Assert.Empty(report.CapabilityGaps);
    }

    [Fact]
    public void ScenarioRunServiceReportsMultiTurnMoveFacingScenario()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Scenario Room");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Scenario Room", InventoryWidth: 5, InventoryHeight: 5, Weight: 100, CarryingCapacity: 100),
            new EntityPresentation('#', PresentationColor.Gray));
        var actorTemplateId = editor.CreateEntityPreset("Player");
        editor.UpdateEntityPreset(
            actorTemplateId,
            new EntityTemplate("Player", InventoryWidth: 0, InventoryHeight: 0, Weight: 1, CarryingCapacity: 1),
            new EntityPresentation('@', PresentationColor.White));
        editor.SetInitialFacing(actorTemplateId, Direction.East);
        var planTemplateId = editor.CreateActionPlan("Move Facing");
        editor.SetActionPlanBehavior(planTemplateId, [ActionPlanBehaviorStepKind.MoveFacing]);
        editor.SetDefaultActionPlan(actorTemplateId, planTemplateId);
        editor.PlaceCarriedEntity(scenarioRootId, new EntityId("player"), actorTemplateId, new GridCoord(1, 2));

        var report = ScenarioRunService.Run(document, new ScenarioRunRequest(scenarioRootId, TurnCount: 2));

        Assert.Equal([1, 2], report.Turns.Select(turn => turn.TurnNumber).ToArray());
        Assert.All(report.Turns, turn => Assert.Equal("Player", turn.ActorName));
        Assert.All(report.Turns, turn => Assert.Contains("1. MoveFacing: Success; fallback=stopped", turn.TraceLines));
        Assert.Contains("Player: scenarioRoot(3,2), facing East, target none", report.FinalStateLines);
        Assert.Empty(report.RuntimeObservations);
    }

    [Fact]
    public void ScenarioRunnerReportsPickupTargetScenario()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.RockId);
        var scenario = new HeadlessScenario(
            "actor_picks_up_target_rock",
            world,
            TestWorld.PlayerId,
            CreateBehaviorPlan("pickup-target", ActionPlanBehaviorStepKind.PickupTarget),
            [TestWorld.PlayerId, TestWorld.RockId]);

        var report = MinimalScenarioRunner.Run(scenario, turnCount: 1).FormatText();

        Assert.Contains("- 1. PickupTarget: Success; fallback=stopped", report, StringComparison.Ordinal);
        Assert.Contains("-    reads: Target=rock", report, StringComparison.Ordinal);
        Assert.Contains("- Rock: player(0,0), facing none, target none", report, StringComparison.Ordinal);
        Assert.Contains("Diagnostics:\n- none", report, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioRunnerReportsPushFacingScenario()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var scenario = new HeadlessScenario(
            "actor_pushes_blocker_north",
            world,
            TestWorld.PlayerId,
            CreateBehaviorPlan("push-facing", ActionPlanBehaviorStepKind.PushFacing),
            [TestWorld.PlayerId, TestWorld.SlimeId]);

        var report = MinimalScenarioRunner.Run(scenario, turnCount: 1).FormatText();

        Assert.Contains("- 1. PushFacing: Success; fallback=stopped", report, StringComparison.Ordinal);
        Assert.Contains("-    reads: Facing=North", report, StringComparison.Ordinal);
        Assert.Contains("- Player: world(1,1), facing North, target none", report, StringComparison.Ordinal);
        Assert.Contains("- Slime: world(1,0), facing none, target none", report, StringComparison.Ordinal);
        Assert.Contains("Diagnostics:\n- none", report, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioRunnerReportsDestroyTargetScenario()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var scenario = new HeadlessScenario(
            "actor_destroys_target_and_inventory",
            world,
            TestWorld.PlayerId,
            CreateBehaviorPlan("destroy-target", ActionPlanBehaviorStepKind.DestroyTarget),
            [TestWorld.PlayerId, TestWorld.SlimeId, TestWorld.RockId]);

        var report = MinimalScenarioRunner.Run(scenario, turnCount: 1).FormatText();

        Assert.Contains("- 1. DestroyTarget: Success; fallback=stopped", report, StringComparison.Ordinal);
        Assert.Contains("-    reads: Target=slime", report, StringComparison.Ordinal);
        Assert.Contains("- Player: world(1,2), facing none, target slime", report, StringComparison.Ordinal);
        Assert.Contains("- slime: destroyed", report, StringComparison.Ordinal);
        Assert.Contains("- rock: destroyed", report, StringComparison.Ordinal);
        Assert.Contains("Diagnostics:\n- none", report, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioRunnerReportsCreateFacingScenario()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.East);
        var scenario = new HeadlessScenario(
            "actor_creates_placeholder_rock_facing_east",
            world,
            TestWorld.PlayerId,
            CreateBehaviorPlan("create-facing", ActionPlanBehaviorStepKind.CreateFacing),
            [TestWorld.PlayerId, new EntityId("placeholderRock")]);

        var report = MinimalScenarioRunner.Run(scenario, turnCount: 1).FormatText();

        Assert.Contains("- 1. CreateFacing: Success; fallback=stopped", report, StringComparison.Ordinal);
        Assert.Contains("-    reads: Facing=East", report, StringComparison.Ordinal);
        Assert.Contains("- Player: world(1,2), facing East, target none", report, StringComparison.Ordinal);
        Assert.Contains("- Placeholder Rock: world(2,2), facing none, target none", report, StringComparison.Ordinal);
        Assert.Contains("Diagnostics:\n- none", report, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioRunnerReportsContentAuthoringValidationFailure()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var invalidActorId = editor.CreateEntityPreset("Invalid Actor");
        document.EntityTemplates[invalidActorId.Value].DefaultActionPlanId = "missingPlan";
        var world = TestWorld.CreateWorld();
        var scenario = new HeadlessScenario(
            "invalid_content_missing_default_plan",
            world,
            TestWorld.PlayerId,
            CreateBehaviorPlan("unused-plan", ActionPlanBehaviorStepKind.MoveFacing),
            [TestWorld.PlayerId],
            Diagnostics:
            [
                $"content authoring: Entity template {invalidActorId} (Invalid Actor) references missing defaultActionPlanId missingPlan."
            ]);

        var report = MinimalScenarioRunner.Run(scenario, turnCount: 0).FormatText();

        Assert.Contains("Diagnostics:\n- content authoring: Entity template invalidActor (Invalid Actor) references missing defaultActionPlanId missingPlan.", report, StringComparison.Ordinal);
        Assert.Contains("Capability Gaps:\n- none", report, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioRunnerReportsRuntimeExecutionFailure()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.SetActionFacing(TestWorld.PlayerId, Direction.East);
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2))));
        var scenario = new HeadlessScenario(
            "actor_cannot_create_into_occupied_facing_cell",
            world,
            TestWorld.PlayerId,
            CreateBehaviorPlan("create-facing", ActionPlanBehaviorStepKind.CreateFacing),
            [TestWorld.PlayerId, TestWorld.RockId]);

        var report = MinimalScenarioRunner.Run(scenario, turnCount: 1).FormatText();

        Assert.Contains("- 1. CreateFacing: Failure; reason=InvalidPlacement; fallback=stopped", report, StringComparison.Ordinal);
        Assert.Contains("- Terminal: failed; consumed turn", report, StringComparison.Ordinal);
        Assert.Contains("Diagnostics:\n- runtime execution: Turn 1: plan create-facing failed (cannot create placeholder entity at world(2,2))", report, StringComparison.Ordinal);
        Assert.Contains("- Rock: world(2,2), facing none, target none", report, StringComparison.Ordinal);
        Assert.Contains("Capability Gaps:\n- none", report, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioRunnerReportsUnsupportedCapabilityGap()
    {
        var world = TestWorld.CreateWorld();
        var scenario = new HeadlessScenario(
            "request_create_facing_specific_template",
            world,
            TestWorld.PlayerId,
            CreateBehaviorPlan("not-run", ActionPlanBehaviorStepKind.CreateFacing),
            [TestWorld.PlayerId],
            CapabilityGaps:
            [
                "unsupported capability: CreateFacing(templateId) is not available; current CreateFacing creates placeholder rocks only"
            ]);

        var report = MinimalScenarioRunner.Run(scenario, turnCount: 0).FormatText();

        Assert.Contains("Diagnostics:\n- none", report, StringComparison.Ordinal);
        Assert.Contains("Capability Gaps:\n- unsupported capability: CreateFacing(templateId) is not available; current CreateFacing creates placeholder rocks only", report, StringComparison.Ordinal);
    }

    private static ActionPlanDefinition CreateBehaviorPlan(string id, ActionPlanBehaviorStepKind stepKind) =>
        new(
            new ActionPlanId(id),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(stepKind)]));

}

internal sealed record HeadlessScenario(
    string Name,
    WorldState World,
    EntityId ActorId,
    ActionPlanDefinition Plan,
    IReadOnlyList<EntityId> WatchedEntityIds,
    IReadOnlyList<string>? Diagnostics = null,
    IReadOnlyList<string>? CapabilityGaps = null);

internal sealed record ScenarioRunReport(
    string ScenarioName,
    IReadOnlyList<string> SetupLines,
    IReadOnlyList<ScenarioTurnReport> Turns,
    IReadOnlyList<string> FinalStateLines,
    IReadOnlyList<string> Diagnostics,
    IReadOnlyList<string> CapabilityGaps)
{
    public string FormatText()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Scenario: {ScenarioName}");
        builder.AppendLine();
        AppendSection(builder, "Setup", SetupLines);
        builder.AppendLine();
        builder.AppendLine("Run:");

        foreach (var turn in Turns)
        {
            builder.AppendLine($"Turn {turn.TurnNumber}: {turn.ActorName} executes {turn.PlanId}");

            foreach (var line in turn.TraceLines)
            {
                builder.AppendLine($"- {line}");
            }
        }

        builder.AppendLine();
        AppendSection(builder, "Final State", FinalStateLines);
        builder.AppendLine();
        AppendSection(builder, "Diagnostics", Diagnostics.Count == 0 ? ["none"] : Diagnostics);
        builder.AppendLine();
        AppendSection(builder, "Capability Gaps", CapabilityGaps.Count == 0 ? ["none"] : CapabilityGaps);
        return builder.ToString().Replace(Environment.NewLine, "\n", StringComparison.Ordinal).TrimEnd();
    }

    private static void AppendSection(StringBuilder builder, string label, IReadOnlyList<string> lines)
    {
        builder.AppendLine($"{label}:");

        foreach (var line in lines)
        {
            builder.AppendLine(line.StartsWith("  - ", StringComparison.Ordinal) ? line : $"- {line}");
        }
    }
}

internal sealed record ScenarioTurnReport(
    int TurnNumber,
    string ActorName,
    ActionPlanId PlanId,
    IReadOnlyList<string> TraceLines);

internal static class MinimalScenarioRunner
{
    public static ScenarioRunReport Run(HeadlessScenario scenario, int turnCount)
    {
        var interpreter = new ActionPlanInterpreter(new MovementService());
        var setupLines = SummarizeSetup(scenario);
        var turns = new List<ScenarioTurnReport>();
        var diagnostics = scenario.Diagnostics?.ToList() ?? [];
        var capabilityGaps = scenario.CapabilityGaps?.ToList() ?? [];

        for (var turn = 1; turn <= turnCount; turn++)
        {
            var result = interpreter.Execute(scenario.World, scenario.ActorId, scenario.Plan, new ActionPlanContext());
            scenario.World.RecordTrace(result.Trace);

            if (result.ConsumesTurn)
            {
                scenario.World.AdvanceTurn();
            }

            turns.Add(new ScenarioTurnReport(
                turn,
                scenario.World.Entities[scenario.ActorId].Name,
                scenario.Plan.Id,
                BehaviorChainTraceFormatter.Format(result)));

            if (!result.Succeeded)
            {
                diagnostics.Add($"runtime execution: Turn {turn}: plan {scenario.Plan.Id} failed ({FindFailureDetail(result.Trace)})");
            }
        }

        return new ScenarioRunReport(
            scenario.Name,
            setupLines,
            turns,
            SummarizeEntities(scenario.World, scenario.WatchedEntityIds),
            diagnostics,
            capabilityGaps);
    }

    private static IReadOnlyList<string> SummarizeSetup(HeadlessScenario scenario)
    {
        var actor = scenario.World.Entities[scenario.ActorId];
        var planSteps = scenario.Plan.Behavior is null
            ? "non-behavior plan"
            : string.Join(", ", scenario.Plan.Behavior.Steps.Select(step => step.Kind));

        var lines = new List<string>
        {
            $"Actor: {actor.Name} at {scenario.World.GetEntityLocation(scenario.ActorId)}, {FormatActionState(scenario.World, scenario.ActorId)}",
            $"Plan: {scenario.Plan.Id} [{planSteps}]",
            "Watched entities:"
        };

        lines.AddRange(SummarizeEntities(scenario.World, scenario.WatchedEntityIds).Select(line => $"  - {line}"));
        return lines;
    }

    private static IReadOnlyList<string> SummarizeEntities(WorldState world, IReadOnlyList<EntityId> entityIds) =>
        entityIds
            .Select(entityId => world.Entities.TryGetValue(entityId, out var entity)
                ? $"{entity.Name}: {world.GetEntityLocation(entityId)}, {FormatActionState(world, entityId)}"
                : $"{entityId}: destroyed")
            .ToList();

    private static string FindFailureDetail(TraceNode trace) =>
        DescendantsAndSelf(trace)
            .Where(node => node.Status == TraceStatus.Failure && !string.IsNullOrWhiteSpace(node.Detail))
            .Select(node => node.Detail!)
            .LastOrDefault()
        ?? trace.Detail
        ?? "no detail";

    private static IEnumerable<TraceNode> DescendantsAndSelf(TraceNode node)
    {
        yield return node;

        foreach (var child in node.Children)
        {
            foreach (var descendant in DescendantsAndSelf(child))
            {
                yield return descendant;
            }
        }
    }

    private static string FormatActionState(WorldState world, EntityId entityId)
    {
        var facing = world.GetActionFacing(entityId)?.ToString() ?? "none";
        var target = world.GetActionTarget(entityId)?.ToString() ?? "none";
        return $"facing {facing}, target {target}";
    }
}

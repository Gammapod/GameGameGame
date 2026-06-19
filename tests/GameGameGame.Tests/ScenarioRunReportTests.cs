using System.Text;
using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.Editor;

namespace GameGameGame.Tests;

public sealed class ScenarioRunReportTests
{
    [Fact]
    public void ScenarioRunnerCanUseEditorAuthoredTemporaryContentForReport()
    {
        var api = AgentContentEditorApi.CreateNew();
        var actorTemplateId = AssertSuccess(api.CreateEntityTemplate("Scenario Actor"));
        var rockTemplateId = AssertSuccess(api.CreateEntityTemplate("Scenario Rock"));
        AssertSuccess(api.UpdateEntityTemplate(
            actorTemplateId,
            new AgentEntityTemplateUpdate(
                InventoryWidth: 3,
                InventoryHeight: 2,
                Weight: 10,
                CarryingCapacity: 5,
                Glyph: '@',
                Color: PresentationColor.White)));
        AssertSuccess(api.UpdateEntityTemplate(
            rockTemplateId,
            new AgentEntityTemplateUpdate(
                Weight: 3,
                CarryingCapacity: 3,
                Glyph: '*',
                Color: PresentationColor.Gray)));
        AssertSuccess(api.SetInitialFacing(actorTemplateId, Direction.East));
        AssertSuccess(api.PlaceCarriedEntity(actorTemplateId, new EntityId("carriedRock"), rockTemplateId, new GridCoord(0, 0)));
        var planTemplateId = AssertSuccess(api.CreateActionPlan("Drop Facing"));
        AssertSuccess(api.SetActionPlanBehavior(planTemplateId, [ActionPlanBehaviorStepKind.DropFacing]));
        AssertSuccess(api.SetDefaultActionPlan(actorTemplateId, planTemplateId));
        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var registry = api.Session.Document.ToRegistry();
        var world = CreateScenarioWorld();
        var actorId = new EntityId("actor");
        registry.SpawnEntity(
            world,
            actorTemplateId,
            new EntitySpawnOptions(
                actorId,
                new PlaneCoord(new PlaneId("scenarioWorld"), new GridCoord(1, 2)),
                InventoryPlaneId: new PlaneId("actorInventory"),
                InventoryPlaneName: "Actor Inventory"));
        var plan = registry.GetActionPlanDescriptor(planTemplateId).Materialize();
        var scenario = new HeadlessScenario(
            "editor_authored_actor_drops_carried_rock_facing_east",
            world,
            actorId,
            plan,
            [actorId, new EntityId("carriedRock")]);

        var report = MinimalScenarioRunner.Run(scenario, turnCount: 1).FormatText();

        Assert.Equal(
            """
            Scenario: editor_authored_actor_drops_carried_rock_facing_east

            Setup:
            - Actor: Scenario Actor at scenarioWorld(1,2), facing East, target none
            - Plan: dropFacing [DropFacing]
            - Watched entities:
              - Scenario Actor: scenarioWorld(1,2), facing East, target none
              - Scenario Rock: actorInventory(0,0), facing none, target none

            Run:
            Turn 1: Scenario Actor executes dropFacing
            - Plan dropFacing: Success; consumedTurn=True; continuePlan=False
            - 1. DropFacing: Success; fallback=stopped
            -    reads: Facing=East
            - Terminal: succeeded; consumed turn

            Final State:
            - Scenario Actor: scenarioWorld(1,2), facing East, target none
            - Scenario Rock: scenarioWorld(2,2), facing none, target none

            Diagnostics:
            - none

            Capability Gaps:
            - none
            """,
            report);
    }

    [Fact]
    public void MinimalScenarioRunnerReportsCompletedDropFacingScenario()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.SetActionFacing(TestWorld.PlayerId, Direction.East);
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("drop-facing"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DropFacing)]));
        var scenario = new HeadlessScenario(
            "actor_drops_carried_rock_facing_east",
            world,
            TestWorld.PlayerId,
            plan,
            [TestWorld.PlayerId, TestWorld.RockId]);

        var report = MinimalScenarioRunner.Run(scenario, turnCount: 1).FormatText();

        Assert.Equal(
            """
            Scenario: actor_drops_carried_rock_facing_east

            Setup:
            - Actor: Player at world(1,2), facing East, target none
            - Plan: drop-facing [DropFacing]
            - Watched entities:
              - Player: world(1,2), facing East, target none
              - Rock: player(0,0), facing none, target none

            Run:
            Turn 1: Player executes drop-facing
            - Plan drop-facing: Success; consumedTurn=True; continuePlan=False
            - 1. DropFacing: Success; fallback=stopped
            -    reads: Facing=East
            - Terminal: succeeded; consumed turn

            Final State:
            - Player: world(1,2), facing East, target none
            - Rock: world(2,2), facing none, target none

            Diagnostics:
            - none

            Capability Gaps:
            - none
            """,
            report);
    }

    [Fact]
    public void ScenarioRunnerReportsMultiTurnMoveFacingScenario()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.East);
        var scenario = new HeadlessScenario(
            "actor_moves_facing_east_for_two_turns",
            world,
            TestWorld.PlayerId,
            CreateBehaviorPlan("move-facing", ActionPlanBehaviorStepKind.MoveFacing),
            [TestWorld.PlayerId]);

        var report = MinimalScenarioRunner.Run(scenario, turnCount: 2).FormatText();

        Assert.Contains("Turn 1: Player executes move-facing", report, StringComparison.Ordinal);
        Assert.Contains("Turn 2: Player executes move-facing", report, StringComparison.Ordinal);
        Assert.Contains("- 1. MoveFacing: Success; fallback=stopped", report, StringComparison.Ordinal);
        Assert.Contains("- Player: world(3,2), facing East, target none", report, StringComparison.Ordinal);
        Assert.DoesNotContain("Turn 1: plan move-facing failed", report, StringComparison.Ordinal);
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
        var api = AgentContentEditorApi.CreateNew();
        var invalidActorId = AssertSuccess(api.CreateEntityTemplate("Invalid Actor"));
        api.Session.Document.EntityTemplates[invalidActorId.Value].DefaultActionPlanId = "missingPlan";
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

    private static WorldState CreateScenarioWorld()
    {
        var world = new WorldState();
        AddRectangularPlane(world, new Plane(new PlaneId("scenarioWorld"), "Scenario World", 5, 5));
        return world;
    }

    private static void AddRectangularPlane(WorldState world, Plane plane)
    {
        world.Planes.Add(plane.Id, plane);

        for (var y = 0; y < plane.Height; y++)
        {
            for (var x = 0; x < plane.Width; x++)
            {
                world.AddNode(plane.Id, new GridCoord(x, y));
            }
        }
    }

    private static ActionPlanDefinition CreateBehaviorPlan(string id, ActionPlanBehaviorStepKind stepKind) =>
        new(
            new ActionPlanId(id),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(stepKind)]));

    private static void AssertSuccess(AgentApiResult result)
    {
        Assert.True(result.IsSuccess, result.Error?.Message);
    }

    private static T AssertSuccess<T>(AgentApiResult<T> result)
    {
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value!;
    }
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

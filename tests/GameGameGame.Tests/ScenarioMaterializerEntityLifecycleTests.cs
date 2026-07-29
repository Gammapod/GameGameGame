using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class ScenarioMaterializerEntityLifecycleTests
{
    [Fact]
    public void ScenarioMaterializerPopulatesRuntimeTemplatesForCreateAndPolymorph()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              room:
                name: Room
                inventoryWidth: 3
                inventoryHeight: 3
                bulk: 100
                aperture: 100
                carriedEntities:
                  - entityId: ratking
                    templateId: ratking
                    coord:
                      x: 1
                      y: 1
              ratking:
                name: Rat King
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 5
                aperture: 5
                defaultActionPlanId: createRat
                actionStateDefaults:
                  facing: East
              rat:
                name: Rat
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 1
                defaultActionPlanId: ratPlan
                actionStateDefaults:
                  facing: West
            presentations:
              room:
                glyph: R
                color: Gray
              ratking:
                glyph: K
                color: DarkGreen
              rat:
                glyph: r
                color: Gray
            actionPlans:
              createRat:
                id: createRat
                behavior:
                  steps:
                    - kind: CreateEntity
                      templateId: rat
              ratPlan:
                id: ratPlan
                behavior:
                  steps:
                    - kind: Move
                      directionMode: Forward
            scenarios:
              lifecycle:
                id: lifecycle
                name: Lifecycle
                scenarioRootEntityTemplateId: room
            """);

        var materialization = ScenarioMaterializer.Materialize(document, "lifecycle");

        Assert.True(materialization.ValidationDiagnostics.Count == 0, string.Join(Environment.NewLine, materialization.ValidationDiagnostics));
        Assert.True(materialization.World.RuntimeEntityTemplates.ContainsKey("rat"));
        Assert.Equal("ratking", materialization.World.Entities[new EntityId("ratking")].TemplateId);
        var ratTemplate = materialization.World.RuntimeEntityTemplates["rat"];
        Assert.Equal("Rat", ratTemplate.Name);
        Assert.Equal(new ActionPlanId("ratPlan"), ratTemplate.DefaultActionPlanId);
        Assert.Equal(Direction.West, ratTemplate.InitialFacing);
    }

    [Fact]
    public void ScenarioRunServiceSchedulesTemplateBackedCreatedActorsOnLaterTurns()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              room:
                name: Room
                inventoryWidth: 5
                inventoryHeight: 3
                bulk: 100
                aperture: 100
                carriedEntities:
                  - entityId: ratking
                    templateId: ratking
                    coord:
                      x: 2
                      y: 1
              ratking:
                name: Rat King
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 5
                aperture: 5
                defaultActionPlanId: createRat
              rat:
                name: Rat
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 1
                defaultActionPlanId: ratPlan
                actionStateDefaults:
                  facing: East
            presentations:
              room:
                glyph: R
                color: Gray
              ratking:
                glyph: K
                color: DarkGreen
              rat:
                glyph: r
                color: Gray
            actionPlans:
              createRat:
                id: createRat
                behavior:
                  steps:
                    - kind: CreateEntity
                      templateId: rat
              ratPlan:
                id: ratPlan
                behavior:
                  steps:
                    - kind: Move
                      directionMode: Forward
            """);

        var report = ScenarioRunService.Run(document, new ScenarioRunRequest(new EntityTemplateId("room"), TurnCount: 2));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Contains(report.Turns, turn => turn.ActorId == new EntityId("ratking")
            && turn.TraceLines.Any(line => line.StartsWith("1. CreateEntity: Success", StringComparison.Ordinal)));
        Assert.Contains(report.Turns, turn => turn.ActorId == new EntityId("rat-1")
            && turn.TraceLines.Any(line => line.StartsWith("1. Move: Success", StringComparison.Ordinal)));
        Assert.Contains(report.FinalStateLines, line => line.StartsWith("Rat: scenarioRoot", StringComparison.Ordinal));
    }

    [Fact]
    public void DynamicScenarioActionPlanSynchronizerRefreshesCreatedPolymorphedAndDestroyedActors()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              room:
                name: Room
                inventoryWidth: 5
                inventoryHeight: 3
                bulk: 100
                aperture: 100
                carriedEntities:
                  - entityId: ratking
                    templateId: ratking
                    coord:
                      x: 2
                      y: 1
              ratking:
                name: Rat King
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 5
                aperture: 5
                defaultActionPlanId: createRat
              rat:
                name: Rat
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 1
                defaultActionPlanId: ratPlan
                actionStateDefaults:
                  facing: East
            presentations:
              room:
                glyph: R
                color: Gray
              ratking:
                glyph: K
                color: DarkGreen
              rat:
                glyph: r
                color: Gray
            actionPlans:
              createRat:
                id: createRat
                behavior:
                  steps:
                    - kind: CreateEntity
                      templateId: rat
              ratPlan:
                id: ratPlan
                behavior:
                  steps:
                    - kind: Move
                      directionMode: Forward
            scenarios:
              lifecycle:
                id: lifecycle
                name: Lifecycle
                scenarioRootEntityTemplateId: room
            """);
        var materialization = ScenarioMaterializer.Materialize(document, "lifecycle");
        var world = materialization.World;
        var actionPlans = new Dictionary<EntityId, IEntityActionPlan>(materialization.ActionPlans);
        var ratId = new EntityId("rat-1");

        var created = ActorTurnResolver.ResolvePlan(
            world,
            new EntityId("ratking"),
            actionPlans[new EntityId("ratking")].PlanTurn(world, new EntityId("ratking"), new MovementService()),
            new MovementService());
        Assert.True(created.Succeeded);
        Assert.True(world.Entities.ContainsKey(ratId));
        Assert.False(actionPlans.ContainsKey(ratId));

        var synchronizer = new DynamicScenarioActionPlanSynchronizer();
        synchronizer.SynchronizeInPlace(world, materialization.Registry, actionPlans);
        Assert.True(actionPlans.ContainsKey(ratId));
        var originalRatPlan = actionPlans[ratId];

        world.SetDefaultActionPlanId(ratId, new ActionPlanId("createRat"));
        synchronizer.SynchronizeInPlace(world, materialization.Registry, actionPlans);
        Assert.NotSame(originalRatPlan, actionPlans[ratId]);

        world.DestroyEntityRecursive(ratId);
        synchronizer.SynchronizeInPlace(world, materialization.Registry, actionPlans);
        Assert.False(actionPlans.ContainsKey(ratId));
    }

    [Fact]
    public void FlagshipLifecycleScenarioRunsCreateDestroyAndPolymorphLoop()
    {
        var contentPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "GameGameGame.Content",
            "Beta",
            "EntityLifecycle",
            "CreateDestroyPolymorphShowcase.yaml");
        var document = EditableContentDocument.LoadYaml(File.ReadAllText(contentPath));

        var report = ScenarioRunService.Run(
            document,
            new PersistedScenarioRunRequest("delta-create-destroy-polymorph-flagship-room", TurnCount: 8));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Empty(report.CapabilityGaps);
        Assert.Contains(report.Turns, turn => turn.ActorId == new EntityId("ratking")
            && turn.TraceLines.Any(line => line.StartsWith("1. CreateEntity: Success", StringComparison.Ordinal)));
        Assert.Contains(report.Turns, turn => turn.ActorName == "Rat"
            && turn.TraceLines.Any(line => line.StartsWith("1. MoveFacing: Success", StringComparison.Ordinal)));
        Assert.Contains(report.Turns, turn => turn.ActorName == "Rat"
            && turn.TraceLines.Any(line => line.StartsWith("2. Backstep: Success", StringComparison.Ordinal)));
        Assert.Contains(report.Turns, turn => turn.ActorId == new EntityId("snake")
            && turn.TraceLines.Any(line => line.StartsWith("1. DestroyTarget: Success", StringComparison.Ordinal)));
        Assert.Contains(report.Turns, turn => turn.ActorId == new EntityId("lifecycleEgg")
            && turn.ActorName == "Egg"
            && turn.TraceLines.Any(line => line.StartsWith("1. PolymorphTarget: Success", StringComparison.Ordinal)));
        Assert.Contains(report.Turns, turn => turn.ActorId == new EntityId("lifecycleEgg")
            && turn.ActorName == "Caterpillar"
            && turn.TraceLines.Any(line => line.StartsWith("1. PolymorphTarget: Success", StringComparison.Ordinal)));
        Assert.Contains(report.Turns, turn => turn.ActorId == new EntityId("lifecycleEgg")
            && turn.ActorName == "Cocoon"
            && turn.TraceLines.Any(line => line.StartsWith("1. PolymorphTarget: Success", StringComparison.Ordinal)));
        Assert.Contains(report.Turns, turn => turn.ActorId == new EntityId("lifecycleEgg")
            && turn.ActorName == "Butterfly"
            && turn.TraceLines.Any(line => line.StartsWith("1. PolymorphTarget: Success", StringComparison.Ordinal)));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "src", "GameGameGame.Content")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test working directory.");
    }
}

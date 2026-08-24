using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class TargetingServiceTests
{
    [Fact]
    public void RefreshTargetsSelectsNearestMatchingTemplateInRange()
    {
        var registry = CreateTargetingRegistry();
        var world = CreateTargetingWorld();
        Spawn(registry, world, "mouse", "mouse", new GridCoord(2, 2));
        Spawn(registry, world, "cat", "farCat", new GridCoord(4, 4));
        Spawn(registry, world, "cat", "nearCat", new GridCoord(2, 4));
        Spawn(registry, world, "cheese", "cheese", new GridCoord(2, 3));

        TargetingService.RefreshTargets(world, registry, new EntityId("mouse"));

        Assert.Equal(new EntityId("nearCat"), world.GetActionTarget(new EntityId("mouse"), slot: 1));
        Assert.Equal(new EntityId("nearCat"), world.GetActionTarget(new EntityId("mouse"), label: "danger"));
        Assert.Equal(new EntityId("nearCat"), world.GetActionTarget(new EntityId("mouse")));
    }

    [Fact]
    public void RefreshTargetsUsesOctagonalDistanceForCandidateOrdering()
    {
        var registry = CreateTargetingRegistry();
        var world = CreateTargetingWorld();
        var mouse = new EntityId("mouse");
        Spawn(registry, world, "mouse", mouse.Value, new GridCoord(2, 2));
        Spawn(registry, world, "cat", "cardinalTwoCat", new GridCoord(4, 2));
        Spawn(registry, world, "cat", "diagonalAdjacentCat", new GridCoord(3, 3));

        TargetingService.RefreshTargets(world, registry, mouse);

        Assert.Equal(new EntityId("diagonalAdjacentCat"), world.GetActionTarget(mouse, label: "danger"));
    }

    [Fact]
    public void RefreshTargetsClearsStaleTargetWhenNoMatchIsInRange()
    {
        var registry = CreateTargetingRegistry();
        var world = CreateTargetingWorld();
        var mouse = new EntityId("mouse");
        Spawn(registry, world, "mouse", mouse.Value, new GridCoord(0, 0));
        Spawn(registry, world, "cat", "cat", new GridCoord(4, 4));
        world.SetActionTarget(mouse, slot: 1, new EntityId("staleCat"));

        TargetingService.RefreshTargets(world, registry, mouse);

        Assert.Null(world.GetActionTarget(mouse, slot: 1));
        Assert.Null(world.GetActionTarget(mouse, label: "danger"));
        Assert.Null(world.GetActionTarget(mouse));
    }

    [Fact]
    public void TurnServiceRefreshesTargetsBeforeResolvingActorPlan()
    {
        var registry = CreateTargetingRegistry();
        var world = CreateTargetingWorld();
        var mouse = new EntityId("mouse");
        var cat = new EntityId("cat");
        Spawn(registry, world, "mouse", mouse.Value, new GridCoord(2, 2));
        Spawn(registry, world, "cat", cat.Value, new GridCoord(2, 3));
        var actionPlans = new Dictionary<EntityId, IEntityActionPlan>
        {
            [mouse] = new FixedEntityActionPlan(new AssertTargetAction(cat))
        };
        var turns = new TurnService(
            new MovementService(),
            actionPlans,
            (turnWorld, entityId) => TargetingService.RefreshTargets(turnWorld, registry, entityId));

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal(cat, world.GetActionTarget(mouse, slot: 1));
    }

    [Fact]
    public void RefreshTargetsCanFilterMatchingTemplateByPickupCapabilityAdjective()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              thief:
                name: Thief
                inventoryWidth: 1
                inventoryHeight: 1
                bulk: 1
                aperture: 2
                defaultActionPlanId: thiefPlan
                targetingRules:
                  - slot: 1
                    label: loves
                    targetTemplateId: gold
                    targetCapabilities:
                      - PickupTarget
                    range: 4
              gold:
                name: Gold
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
            presentations:
              thief:
                glyph: t
                color: Gray
              gold:
                glyph: '$'
                color: Yellow
            actionPlans:
              thiefPlan:
                id: thiefPlan
                behavior:
                  steps:
                    - kind: SeekTarget
                      targetLabel: loves
                    - kind: PickupTarget
                      targetLabel: loves
            """);
        var world = CreateTargetingWorld();
        var thief = new EntityId("thief");
        Spawn(registry, world, "thief", thief.Value, new GridCoord(2, 2));
        Spawn(registry, world, "gold", "heavyGold", new GridCoord(2, 3));
        world.Entities[new EntityId("heavyGold")] = world.Entities[new EntityId("heavyGold")] with { Bulk = 9 };
        Spawn(registry, world, "gold", "portableGold", new GridCoord(2, 4));

        TargetingService.RefreshTargets(world, registry, thief);

        Assert.Equal(new EntityId("portableGold"), world.GetActionTarget(thief, label: "loves"));
    }

    [Fact]
    public void RefreshTargetsCanSelectNounlessPickupCapabilityAdjective()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              thief:
                name: Thief
                inventoryWidth: 1
                inventoryHeight: 1
                bulk: 1
                aperture: 2
                defaultActionPlanId: thiefPlan
                targetingRules:
                  - slot: 1
                    label: loves
                    targetCapabilities:
                      - PickupTarget
                    range: 4
              gem:
                name: Gem
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
              chest:
                name: Chest
                inventoryWidth: 1
                inventoryHeight: 1
                bulk: 9
                aperture: 3
            presentations:
              thief:
                glyph: t
                color: Gray
              gem:
                glyph: '*'
                color: Yellow
              chest:
                glyph: C
                color: Earth
            actionPlans:
              thiefPlan:
                id: thiefPlan
                behavior:
                  steps:
                    - kind: SeekTarget
                      targetLabel: loves
                    - kind: PickupTarget
                      targetLabel: loves
            """);
        var world = CreateTargetingWorld();
        var thief = new EntityId("thief");
        Spawn(registry, world, "thief", thief.Value, new GridCoord(2, 2));
        Spawn(registry, world, "chest", "heavyChest", new GridCoord(2, 3));
        Spawn(registry, world, "gem", "portableGem", new GridCoord(2, 4));

        TargetingService.RefreshTargets(world, registry, thief);

        Assert.Equal(new EntityId("portableGem"), world.GetActionTarget(thief, label: "loves"));
    }

    [Fact]
    public void RefreshTargetsUsesTemplateLevelRangeForAllTargetingRules()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              mouse:
                name: Mouse
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
                targeting:
                  range: 3
                  rules:
                    - slot: 1
                      label: danger
                      targetTemplateId: cat
              cat:
                name: Cat
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
            presentations: {}
            actionPlans: {}
            """);
        var world = CreateTargetingWorld();
        var mouse = new EntityId("mouse");
        Spawn(registry, world, "mouse", mouse.Value, new GridCoord(2, 2));
        Spawn(registry, world, "cat", "nearCat", new GridCoord(2, 4));
        Spawn(registry, world, "cat", "farCat", new GridCoord(4, 4));

        TargetingService.RefreshTargets(world, registry, mouse);

        Assert.Equal(new EntityId("nearCat"), world.GetActionTarget(mouse, label: "danger"));
    }

    [Fact]
    public void RefreshTargetsCurrentPlaceLocalityIgnoresPeersInDifferentContainingPlacesOnSamePlane()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              room:
                name: Room
                inventoryWidth: 5
                inventoryHeight: 5
                bulk: 100
                aperture: 100
              goblin:
                name: Goblin
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
                targeting:
                  range: 10
                  rules:
                    - slot: 1
                      label: hates
                      targetTemplateId: slime
                      locality:
                        origins:
                          - CurrentPlace
              slime:
                name: Slime
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
            presentations: {}
            actionPlans: {}
            """);
        var world = CreateContainmentWorld();
        var roomAInventory = SpawnRoom(registry, world, "roomA", new GridCoord(0, 0));
        var roomBInventory = SpawnRoom(registry, world, "roomB", new GridCoord(1, 0));
        var goblin = SpawnInPlane(registry, world, "goblin", "goblin", roomAInventory, new GridCoord(1, 1));
        SpawnInPlane(registry, world, "slime", "sameRoomSlime", roomAInventory, new GridCoord(1, 2));
        SpawnInPlane(registry, world, "slime", "otherRoomSlime", roomBInventory, new GridCoord(1, 1));

        TargetingService.RefreshTargets(world, registry, goblin);

        Assert.Equal(new EntityId("sameRoomSlime"), world.GetActionTarget(goblin, label: "hates"));
    }

    [Fact]
    public void RefreshTargetsRuleLocalityCanSearchCurrentPlaceAndPeerInventories()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              room:
                name: Room
                inventoryWidth: 5
                inventoryHeight: 5
                bulk: 100
                aperture: 100
              goblin:
                name: Goblin
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
                targeting:
                  range: 10
                  rules:
                    - slot: 1
                      label: hates
                      targetTemplateId: slime
                      locality:
                        origins:
                          - CurrentPlace
                    - slot: 2
                      label: wants
                      targetTemplateId: gold
                      locality:
                        origins:
                          - CurrentPlace
                          - PeerInventories
              slime:
                name: Slime
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
              chest:
                name: Chest
                inventoryWidth: 1
                inventoryHeight: 1
                bulk: 3
                aperture: 3
              gold:
                name: Gold
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
            presentations: {}
            actionPlans: {}
            """);
        var world = CreateContainmentWorld();
        var roomInventory = SpawnRoom(registry, world, "room", new GridCoord(0, 0));
        var goblin = SpawnInPlane(registry, world, "goblin", "goblin", roomInventory, new GridCoord(1, 1));
        SpawnInPlane(registry, world, "slime", "slime", roomInventory, new GridCoord(1, 2));
        var chest = SpawnInPlane(registry, world, "chest", "chest", roomInventory, new GridCoord(3, 1));
        var chestInventory = world.GetInventoryPlaneId(chest) ?? throw new InvalidOperationException("Chest inventory missing.");
        SpawnInPlane(registry, world, "gold", "hiddenGold", chestInventory, new GridCoord(0, 0));

        TargetingService.RefreshTargets(world, registry, goblin);

        Assert.Equal(new EntityId("slime"), world.GetActionTarget(goblin, label: "hates"));
        Assert.Equal(new EntityId("hiddenGold"), world.GetActionTarget(goblin, label: "wants"));
    }

    [Fact]
    public void RefreshTargetsRuleLocalityCanSearchOwnInventoryWithinActorRange()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              room:
                name: Room
                inventoryWidth: 5
                inventoryHeight: 5
                bulk: 100
                aperture: 100
              goblin:
                name: Goblin
                inventoryWidth: 1
                inventoryHeight: 1
                bulk: 2
                aperture: 2
                targeting:
                  range: 0
                  rules:
                    - slot: 1
                      label: carriedGold
                      targetTemplateId: gold
                      locality:
                        origins:
                          - OwnInventory
              gold:
                name: Gold
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
            presentations: {}
            actionPlans: {}
            """);
        var world = CreateContainmentWorld();
        var roomInventory = SpawnRoom(registry, world, "room", new GridCoord(0, 0));
        var goblin = SpawnInPlane(registry, world, "goblin", "goblin", roomInventory, new GridCoord(1, 1));
        var goblinInventory = world.GetInventoryPlaneId(goblin) ?? throw new InvalidOperationException("Goblin inventory missing.");
        SpawnInPlane(registry, world, "gold", "carriedGold", goblinInventory, new GridCoord(0, 0));

        TargetingService.RefreshTargets(world, registry, goblin);

        Assert.Equal(new EntityId("carriedGold"), world.GetActionTarget(goblin, label: "carriedGold"));
    }

    [Fact]
    public void TargetingCandidatePreviewReportsRuleCandidatesWithoutMutatingTargets()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              room:
                name: Room
                inventoryWidth: 5
                inventoryHeight: 5
                bulk: 100
                aperture: 100
              goblin:
                name: Goblin
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
                targeting:
                  range: 10
                  rules:
                    - slot: 1
                      label: wants
                      targetTemplateId: gold
                      locality:
                        origins:
                          - CurrentPlace
                          - PeerInventories
              chest:
                name: Chest
                inventoryWidth: 1
                inventoryHeight: 1
                bulk: 3
                aperture: 3
              gold:
                name: Gold
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
            presentations: {}
            actionPlans: {}
            """);
        var world = CreateContainmentWorld();
        var roomInventory = SpawnRoom(registry, world, "room", new GridCoord(0, 0));
        var goblin = SpawnInPlane(registry, world, "goblin", "goblin", roomInventory, new GridCoord(1, 1));
        var chest = SpawnInPlane(registry, world, "chest", "chest", roomInventory, new GridCoord(3, 1));
        var chestInventory = world.GetInventoryPlaneId(chest) ?? throw new InvalidOperationException("Chest inventory missing.");
        SpawnInPlane(registry, world, "gold", "hiddenGold", chestInventory, new GridCoord(0, 0));

        var preview = TargetingCandidatePreviewService.Preview(world, registry, goblin);

        var rule = Assert.Single(preview.Rules);
        Assert.Equal("wants", rule.Label);
        var candidate = Assert.Single(rule.Candidates);
        Assert.Equal(new EntityId("hiddenGold"), candidate.EntityId);
        Assert.Equal(TargetingLocalityOrigin.PeerInventories, candidate.Origin);
        Assert.Equal(chest, candidate.ReferenceEntityId);
        Assert.Null(world.GetActionTarget(goblin, label: "wants"));
    }

    [Fact]
    public void TargetingCandidatePreviewReportsOctagonalDistances()
    {
        var registry = CreateTargetingRegistry();
        var world = CreateTargetingWorld();
        var mouse = new EntityId("mouse");
        Spawn(registry, world, "mouse", mouse.Value, new GridCoord(2, 2));
        Spawn(registry, world, "cat", "cardinalTwoCat", new GridCoord(4, 2));
        Spawn(registry, world, "cat", "diagonalAdjacentCat", new GridCoord(3, 3));

        var preview = TargetingCandidatePreviewService.Preview(world, registry, mouse);

        var rule = Assert.Single(preview.Rules);
        Assert.Collection(
            rule.Candidates,
            candidate =>
            {
                Assert.Equal(new EntityId("diagonalAdjacentCat"), candidate.EntityId);
                Assert.Equal(1, candidate.Distance);
            },
            candidate =>
            {
                Assert.Equal(new EntityId("cardinalTwoCat"), candidate.EntityId);
                Assert.Equal(2, candidate.Distance);
            });
    }

    private static PrototypeContentRegistry CreateTargetingRegistry() =>
        YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              mouse:
                name: Mouse
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
                targetingRules:
                  - slot: 1
                    label: danger
                    hint: Danger
                    targetTemplateId: cat
                    range: 3
              cat:
                name: Cat
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 3
                carryingCapacity: 0
              cheese:
                name: Cheese
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
            presentations:
              mouse:
                glyph: m
                color: Gray
              cat:
                glyph: c
                color: Earth
              cheese:
                glyph: '*'
                color: Yellow
            actionPlans: {}
            """);

    private static WorldState CreateTargetingWorld()
    {
        var world = new WorldState();
        var plane = new Plane(TestWorld.WorldPlaneId, "World", 5, 5);
        world.Planes.Add(plane.Id, plane);
        for (var y = 0; y < plane.Height; y++)
        {
            for (var x = 0; x < plane.Width; x++)
            {
                world.AddNode(plane.Id, new GridCoord(x, y));
            }
        }

        return world;
    }

    private static WorldState CreateContainmentWorld()
    {
        var world = new WorldState();
        var plane = new Plane(new PlaneId("root"), "Root", 3, 1);
        world.Planes.Add(plane.Id, plane);
        for (var x = 0; x < plane.Width; x++)
        {
            world.AddNode(plane.Id, new GridCoord(x, 0));
        }

        return world;
    }

    private static PlaneId SpawnRoom(PrototypeContentRegistry registry, WorldState world, string entityId, GridCoord coord)
    {
        var roomId = new EntityId(entityId);
        registry.SpawnEntity(world, new EntityTemplateId("room"), new EntitySpawnOptions(roomId, new PlaneCoord(new PlaneId("root"), coord)));
        return world.GetInventoryPlaneId(roomId) ?? throw new InvalidOperationException($"Room {entityId} inventory missing.");
    }

    private static EntityId SpawnInPlane(PrototypeContentRegistry registry, WorldState world, string templateId, string entityId, PlaneId planeId, GridCoord coord)
    {
        var id = new EntityId(entityId);
        registry.SpawnEntity(world, new EntityTemplateId(templateId), new EntitySpawnOptions(id, new PlaneCoord(planeId, coord)));
        return id;
    }

    private static void Spawn(PrototypeContentRegistry registry, WorldState world, string templateId, string entityId, GridCoord coord, EntityTemplateId? spawnTemplateId = null) =>
        registry.SpawnEntity(
            world,
            spawnTemplateId ?? new EntityTemplateId(templateId),
            new EntitySpawnOptions(new EntityId(entityId), new PlaneCoord(TestWorld.WorldPlaneId, coord)));

    private sealed class FixedEntityActionPlan(IActionIntent action) : IEntityActionPlan
    {
        public PlannedActionPlan PlanTurn(WorldState world, EntityId entityId, MovementService movement) =>
            PlannedActionPlan.Single(action);
    }

    private sealed class AssertTargetAction(EntityId expectedTarget) : IActionIntent
    {
        public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement) =>
            new(true, TraceNode.Success("Assert target available"));

        public void Execute(WorldState world, EntityId actorId, MovementService movement)
        {
        }

        public ActionResolution Resolve(WorldState world, EntityId actorId, MovementService movement)
        {
            var actual = world.GetActionTarget(actorId, slot: 1);
            var succeeded = actual == expectedTarget;
            return new ActionResolution(
                succeeded,
                ConsumesTurn: true,
                ContinuePlan: false,
                succeeded
                    ? TraceNode.Success("Assert target", expectedTarget.ToString())
                    : TraceNode.Failure("Assert target", FailureReason.None, $"expected {expectedTarget}, actual {actual}"));
        }
    }
}

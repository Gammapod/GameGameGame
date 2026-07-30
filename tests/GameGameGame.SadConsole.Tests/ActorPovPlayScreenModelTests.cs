using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Screens;

namespace GameGameGame.SadConsole.Tests;

public sealed class ActorPovPlayScreenModelTests
{
    [Fact]
    public void BuildComposesProjectionFactsIntoScreenModel()
    {
        var fixture = ActorPovFixture.Create();

        var model = ActorPovPlayScreenModelBuilder.Build(
            fixture.World,
            fixture.ActorId,
            fixture.ActionPlans,
            SadConsoleRect.FromSize(1, 1, 118, 38),
            fixture.Appearance,
            fixture.ActionPlanDescriptor);

        Assert.Equal(fixture.ActorId, model.ControlledActor.EntityId);
        Assert.NotNull(model.CurrentPlace);
        Assert.Equal(fixture.RoomId, model.CurrentPlace.EntityId);
        Assert.NotNull(model.ActorInventory);
        Assert.Equal(fixture.ActorId, model.ActorInventory.EntityId);
        Assert.Equal(fixture.ChestId, model.PresentationState.SelectedWorldInspectionEntityId);
        Assert.Equal(fixture.BackpackId, model.PresentationState.SelectedCarriedInspectionEntityId);
        Assert.Equal(ActorPovPlayRegionIds.CurrentPlace, model.PresentationState.FocusedRegionId);
        Assert.Contains(model.Viewports, viewport => viewport.RegionId == ActorPovPlayRegionIds.CurrentPlace && viewport.EntityId == fixture.RoomId);
        Assert.Contains(model.Viewports, viewport => viewport.RegionId == ActorPovPlayRegionIds.ActorInventory && viewport.EntityId == fixture.ActorId);
    }

    [Fact]
    public void BuildPreservesRequestedCandidateSelectionWhenValid()
    {
        var fixture = ActorPovFixture.Create(includeSecondCandidates: true);
        var requested = new ActorPovPlayPresentationState(
            fixture.SecondChestId,
            fixture.SecondBackpackId,
            ActorPovPlayRegionIds.ActorInventory);

        var model = ActorPovPlayScreenModelBuilder.Build(
            fixture.World,
            fixture.ActorId,
            fixture.ActionPlans,
            SadConsoleRect.FromSize(1, 1, 118, 38),
            fixture.Appearance,
            fixture.ActionPlanDescriptor,
            requested);

        Assert.Equal(fixture.SecondChestId, model.PresentationState.SelectedWorldInspectionEntityId);
        Assert.Equal(fixture.SecondBackpackId, model.PresentationState.SelectedCarriedInspectionEntityId);
        Assert.Equal(ActorPovPlayRegionIds.ActorInventory, model.PresentationState.FocusedRegionId);
        Assert.Equal(fixture.SecondChestId, model.SelectedWorldInspectionCandidate?.Entity.EntityId);
        Assert.Equal(fixture.SecondBackpackId, model.SelectedCarriedInspectionCandidate?.Entity.EntityId);
    }

    [Fact]
    public void BuildFallsBackToFirstCandidateWhenRequestedSelectionIsNotProjected()
    {
        var fixture = ActorPovFixture.Create(includeSecondCandidates: true);
        var requested = new ActorPovPlayPresentationState(
            new EntityId("missingWorldCandidate"),
            new EntityId("missingCarriedCandidate"));

        var model = ActorPovPlayScreenModelBuilder.Build(
            fixture.World,
            fixture.ActorId,
            fixture.ActionPlans,
            SadConsoleRect.FromSize(1, 1, 118, 38),
            fixture.Appearance,
            fixture.ActionPlanDescriptor,
            requested);

        Assert.Equal(fixture.ChestId, model.PresentationState.SelectedWorldInspectionEntityId);
        Assert.Equal(fixture.BackpackId, model.PresentationState.SelectedCarriedInspectionEntityId);
    }

    [Fact]
    public void BuildPassesThroughProjectionAndLayoutDiagnosticsWithoutGuessing()
    {
        var (world, actorId) = CurrentPlaceMissingFixture();

        var model = ActorPovPlayScreenModelBuilder.Build(
            world,
            actorId,
            new Dictionary<EntityId, IEntityActionPlan>(),
            SadConsoleRect.FromSize(1, 1, 20, 10));

        Assert.Null(model.CurrentPlace);
        Assert.Contains(model.Diagnostics, diagnostic => diagnostic.Source == "projection" && diagnostic.Code == PointOfViewDiagnosticCode.CurrentPlaceNotFound.ToString());
        Assert.Contains(model.Diagnostics, diagnostic => diagnostic.Source == "layout" && diagnostic.Code == "actor-pov.layout.too-small");
    }

    [Fact]
    public void BuildFromPlayableScenarioSessionUsesSessionPlayerAndDrawableBounds()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var drawable = SadConsoleRect.FromSize(1, 1, 78, 43);

        var model = ActorPovPlayScreenModelBuilder.Build(session, drawable);

        Assert.Equal(session.PlayerEntityId, model.ControlledActor.EntityId);
        Assert.Equal(drawable, model.Layout.DrawableBounds);
        Assert.NotNull(model.CurrentPlace);
    }

    [Fact]
    public void CurrentPlaceComponentRendersCurrentPovInsideLayoutRegion()
    {
        var fixture = ActorPovFixture.Create();
        var model = ActorPovPlayScreenModelBuilder.Build(
            fixture.World,
            fixture.ActorId,
            fixture.ActionPlans,
            SadConsoleRect.FromSize(1, 1, 118, 38),
            fixture.Appearance,
            fixture.ActionPlanDescriptor);

        var component = Assert.IsType<InventorySpaceComponent>(ActorPovPlayComponentFactory.CurrentPlaceComponent(model));

        Assert.Equal("actor-pov-current-place-grid", component.Id);
        Assert.Equal(UiComponentState.Focused, component.State);
        Assert.Same(InventorySpaceRenderOptions.Bare, component.Options);
        Assert.Equal("Room", component.View.Title);
        AssertInside(model.Layout.CurrentPlace.Bounds, component.Bounds);
    }

    [Fact]
    public void CurrentPlaceComponentCanUseDebugLabelsWithoutLeavingLayoutRegion()
    {
        var fixture = ActorPovFixture.Create();
        var model = ActorPovPlayScreenModelBuilder.Build(
            fixture.World,
            fixture.ActorId,
            fixture.ActionPlans,
            SadConsoleRect.FromSize(1, 1, 118, 38),
            fixture.Appearance,
            fixture.ActionPlanDescriptor);

        var component = Assert.IsType<InventorySpaceComponent>(ActorPovPlayComponentFactory.CurrentPlaceComponent(model, showDebugLabels: true));

        Assert.Same(InventorySpaceRenderOptions.Labeled, component.Options);
        AssertInside(model.Layout.CurrentPlace.Bounds, component.Bounds);
    }

    [Fact]
    public void CurrentPlaceComponentShowsHonestEmptyPanelWhenCurrentPlaceIsUnavailable()
    {
        var (world, actorId) = CurrentPlaceMissingFixture();
        var model = ActorPovPlayScreenModelBuilder.Build(
            world,
            actorId,
            new Dictionary<EntityId, IEntityActionPlan>(),
            SadConsoleRect.FromSize(1, 1, 118, 38));

        var component = Assert.IsType<PanelComponent>(ActorPovPlayComponentFactory.CurrentPlaceComponent(model));

        Assert.Equal("actor-pov-current-place-empty", component.Id);
        Assert.Contains(component.BodyRows, row => row.Contains("did not resolve", StringComparison.OrdinalIgnoreCase));
        AssertInside(model.Layout.CurrentPlace.Bounds, component.Bounds);
    }

    [Fact]
    public void ActorInventoryComponentRendersControlledActorInventoryInsideLayoutRegion()
    {
        var fixture = ActorPovFixture.Create();
        var model = ActorPovPlayScreenModelBuilder.Build(
            fixture.World,
            fixture.ActorId,
            fixture.ActionPlans,
            SadConsoleRect.FromSize(1, 1, 118, 38),
            fixture.Appearance,
            fixture.ActionPlanDescriptor);

        var component = Assert.IsType<InventorySpaceComponent>(ActorPovPlayComponentFactory.ActorInventoryComponent(model));

        Assert.Equal("actor-pov-actor-inventory-grid", component.Id);
        Assert.Equal(UiComponentState.Selected, component.State);
        Assert.Same(InventorySpaceRenderOptions.Bare, component.Options);
        Assert.Equal("Actor", component.View.Title);
        AssertInside(model.Layout.ActorInventory.Bounds, component.Bounds);
    }

    [Fact]
    public void ActorInventoryComponentCanUseDebugLabelsWithoutLeavingLayoutRegion()
    {
        var fixture = ActorPovFixture.Create();
        var model = ActorPovPlayScreenModelBuilder.Build(
            fixture.World,
            fixture.ActorId,
            fixture.ActionPlans,
            SadConsoleRect.FromSize(1, 1, 118, 38),
            fixture.Appearance,
            fixture.ActionPlanDescriptor);

        var component = Assert.IsType<InventorySpaceComponent>(ActorPovPlayComponentFactory.ActorInventoryComponent(model, showDebugLabels: true));

        Assert.Same(InventorySpaceRenderOptions.Labeled, component.Options);
        AssertInside(model.Layout.ActorInventory.Bounds, component.Bounds);
    }

    [Fact]
    public void ActorInventoryComponentShowsHonestEmptyPanelWhenActorInventoryIsUnavailable()
    {
        var (world, actorId) = CurrentPlaceMissingFixture();
        var model = ActorPovPlayScreenModelBuilder.Build(
            world,
            actorId,
            new Dictionary<EntityId, IEntityActionPlan>(),
            SadConsoleRect.FromSize(1, 1, 118, 38));

        var component = Assert.IsType<PanelComponent>(ActorPovPlayComponentFactory.ActorInventoryComponent(model));

        Assert.Equal("actor-pov-actor-inventory-empty", component.Id);
        Assert.Contains(component.BodyRows, row => row.Contains("no drawable inventory", StringComparison.OrdinalIgnoreCase));
        AssertInside(model.Layout.ActorInventory.Bounds, component.Bounds);
    }

    [Fact]
    public void ParentChainComponentRendersProjectedAncestorsInOrderInsideLayoutRegion()
    {
        var fixture = ActorPovFixture.Create();
        var model = ActorPovPlayScreenModelBuilder.Build(
            fixture.World,
            fixture.ActorId,
            fixture.ActionPlans,
            SadConsoleRect.FromSize(1, 1, 118, 38),
            fixture.Appearance,
            fixture.ActionPlanDescriptor);

        var components = ActorPovPlayComponentFactory.ParentChainComponents(model);
        var grids = components.OfType<InventorySpaceComponent>().ToList();

        var grid = Assert.Single(grids);
        Assert.Equal("Scenario Host", grid.View.Title);
        Assert.DoesNotContain(components, component => component is ConnectorLineComponent);
        Assert.All(grids, grid => AssertInside(model.Layout.ParentChain.Bounds, grid.Bounds));
    }

    [Fact]
    public void ParentChainComponentShowsHonestEmptyTextWhenNoParentChainExists()
    {
        var (world, actorId) = CurrentPlaceMissingFixture();
        var model = ActorPovPlayScreenModelBuilder.Build(
            world,
            actorId,
            new Dictionary<EntityId, IEntityActionPlan>(),
            SadConsoleRect.FromSize(1, 1, 118, 38));

        var component = Assert.IsType<PanelComponent>(Assert.Single(ActorPovPlayComponentFactory.ParentChainComponents(model)));

        Assert.Equal("actor-pov-parent-chain", component.Id);
        Assert.Contains(component.BodyRows, row => row.Contains("No visible parent", StringComparison.OrdinalIgnoreCase));
        AssertInside(model.Layout.ParentChain.Bounds, component.Bounds);
    }

    [Fact]
    public void ParentChainComponentOmitsDeterministicallyWhenRegionCannotShowEveryAncestor()
    {
        var fixture = ActorPovFixture.CreateDeepParentChain();
        var model = ActorPovPlayScreenModelBuilder.Build(
            fixture.World,
            fixture.ActorId,
            fixture.ActionPlans,
            SadConsoleRect.FromSize(1, 1, 118, 12),
            fixture.Appearance,
            fixture.ActionPlanDescriptor);

        var components = ActorPovPlayComponentFactory.ParentChainComponents(model);
        var grids = components.OfType<InventorySpaceComponent>().ToList();
        var connector = Assert.Single(components.OfType<ConnectorLineComponent>());

        Assert.Equal(3, grids.Count);
        Assert.Contains(connector.View.Segments, segment => segment.Id == "parent-chain-more-ancestors-offscreen");
        Assert.All(grids, grid => AssertInside(model.Layout.ParentChain.Bounds, grid.Bounds));
    }

    [Fact]
    public void ParentChainComponentsPlaceImmediateParentAtBottomGrandparentMiddleGreatGrandparentTop()
    {
        var fixture = ActorPovFixture.CreateDeepParentChain();
        var model = ActorPovPlayScreenModelBuilder.Build(
            fixture.World,
            fixture.ActorId,
            fixture.ActionPlans,
            SadConsoleRect.FromSize(1, 1, 118, 38),
            fixture.Appearance,
            fixture.ActionPlanDescriptor);

        var grids = ActorPovPlayComponentFactory.ParentChainComponents(model)
            .OfType<InventorySpaceComponent>()
            .ToList();

        Assert.Equal(3, grids.Count);
        Assert.Equal("Ancestor 6", grids[0].View.Title);
        Assert.Equal("Ancestor 8", grids[2].View.Title);
        Assert.True(grids[0].Bounds.Top < grids[1].Bounds.Top);
        Assert.True(grids[1].Bounds.Top < grids[2].Bounds.Top);
    }

    [Fact]
    public void WorldInspectionComponentRendersSelectedWorldCandidateInsideLayoutRegion()
    {
        var fixture = ActorPovFixture.Create();
        var model = ActorPovPlayScreenModelBuilder.Build(
            fixture.World,
            fixture.ActorId,
            fixture.ActionPlans,
            SadConsoleRect.FromSize(1, 1, 118, 38),
            fixture.Appearance,
            fixture.ActionPlanDescriptor);

        var component = Assert.IsType<InventorySpaceComponent>(ActorPovPlayComponentFactory.WorldInspectionComponents(model)
            .Single(component => component.Id.Contains("-east-", StringComparison.Ordinal)));

        Assert.Equal("actor-pov-world-inspection-east-grid", component.Id);
        Assert.Equal(UiComponentState.Selected, component.State);
        Assert.Equal("Chest", component.View.Title);
        AssertInside(model.Layout.WorldInspection.Bounds, component.Bounds);
    }

    [Fact]
    public void ActorInventoryInspectionComponentRendersSelectedCarriedCandidateInsideLayoutRegion()
    {
        var fixture = ActorPovFixture.Create();
        var model = ActorPovPlayScreenModelBuilder.Build(
            fixture.World,
            fixture.ActorId,
            fixture.ActionPlans,
            SadConsoleRect.FromSize(1, 1, 118, 38),
            fixture.Appearance,
            fixture.ActionPlanDescriptor);

        var component = Assert.IsType<InventorySpaceComponent>(ActorPovPlayComponentFactory.ActorInventoryInspectionComponent(model));

        Assert.Equal("actor-pov-actor-inventory-inspection-grid", component.Id);
        Assert.Equal(UiComponentState.Selected, component.State);
        Assert.Equal("Backpack", component.View.Title);
        AssertInside(model.Layout.ActorInventoryInspection.Bounds, component.Bounds);
    }

    [Fact]
    public void InspectionComponentsShowHonestEmptyPanelsWhenNoCandidatesExist()
    {
        var (world, actorId) = CurrentPlaceMissingFixture();
        var model = ActorPovPlayScreenModelBuilder.Build(
            world,
            actorId,
            new Dictionary<EntityId, IEntityActionPlan>(),
            SadConsoleRect.FromSize(1, 1, 118, 38));

        var worldInspectionComponents = ActorPovPlayComponentFactory.WorldInspectionComponents(model);
        var worldInspection = Assert.IsType<PanelComponent>(worldInspectionComponents.First());
        var carriedInspection = Assert.IsType<PanelComponent>(ActorPovPlayComponentFactory.ActorInventoryInspectionComponent(model));

        Assert.Equal(8, worldInspectionComponents.Count);
        Assert.Equal("actor-pov-world-inspection-northwest-empty", worldInspection.Id);
        Assert.Equal("empty", worldInspection.Status);
        Assert.Equal("actor-pov-actor-inventory-inspection-empty", carriedInspection.Id);
        Assert.Equal("no candidates", carriedInspection.Status);
        AssertInside(model.Layout.WorldInspection.Bounds, worldInspection.Bounds);
        AssertInside(model.Layout.ActorInventoryInspection.Bounds, carriedInspection.Bounds);
    }

    [Fact]
    public void DiagnosticsChromeComponentSummarizesSelectionsRegionsAndReadyStatus()
    {
        var fixture = ActorPovFixture.Create();
        var model = ActorPovPlayScreenModelBuilder.Build(
            fixture.World,
            fixture.ActorId,
            fixture.ActionPlans,
            SadConsoleRect.FromSize(1, 1, 118, 38),
            fixture.Appearance,
            fixture.ActionPlanDescriptor);

        var component = Assert.IsType<PanelComponent>(ActorPovPlayComponentFactory.DiagnosticsChromeComponent(model));

        Assert.Equal("actor-pov-diagnostics-chrome", component.Id);
        Assert.Equal("ready", component.Status);
        Assert.Equal(UiComponentState.Unselected, component.State);
        Assert.Contains(component.BodyRows, row => row.Contains($"world inspect: {fixture.ChestId.Value}", StringComparison.Ordinal));
        Assert.Contains(component.BodyRows, row => row.Contains($"carried inspect: {fixture.BackpackId.Value}", StringComparison.Ordinal));
        Assert.Contains(component.BodyRows, row => row.Contains("omitted regions: none", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(component.BodyRows, row => row.StartsWith("current-place:", StringComparison.Ordinal));
        AssertInside(model.Layout.DiagnosticsRegion.Bounds, component.Bounds);
    }

    [Fact]
    public void DiagnosticsChromeComponentReportsLayoutAndProjectionDiagnostics()
    {
        var (world, actorId) = CurrentPlaceMissingFixture();
        var model = ActorPovPlayScreenModelBuilder.Build(
            world,
            actorId,
            new Dictionary<EntityId, IEntityActionPlan>(),
            SadConsoleRect.FromSize(1, 1, 20, 10));

        var component = Assert.IsType<PanelComponent>(ActorPovPlayComponentFactory.DiagnosticsChromeComponent(model));

        Assert.Equal(UiComponentState.Error, component.State);
        Assert.Contains("diagnostic", component.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(component.BodyRows, row => row.Contains("omitted regions:", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(component.BodyRows, row => row.Contains("layout:actor-pov.layout.too-small", StringComparison.Ordinal));
        Assert.Contains(component.BodyRows, row => row.Contains($"projection:{PointOfViewDiagnosticCode.CurrentPlaceNotFound}", StringComparison.Ordinal));
        AssertInside(model.Layout.DiagnosticsRegion.Bounds, component.Bounds);
    }

    [Fact]
    public void MainComponentsIncludesCurrentPovAndActorInventoryOnlyForThisSlice()
    {
        var fixture = ActorPovFixture.Create();
        var model = ActorPovPlayScreenModelBuilder.Build(
            fixture.World,
            fixture.ActorId,
            fixture.ActionPlans,
            SadConsoleRect.FromSize(1, 1, 118, 38),
            fixture.Appearance,
            fixture.ActionPlanDescriptor);

        var components = ActorPovPlayComponentFactory.MainComponents(model);

        Assert.Equal(
            [
                "actor-pov-parent-chain-0-grid",
                "actor-pov-parent-chain-connectors",
                "actor-pov-current-place-grid",
                "actor-pov-world-inspection-northwest-empty",
                "actor-pov-world-inspection-north-empty",
                "actor-pov-world-inspection-northeast-empty",
                "actor-pov-world-inspection-east-grid",
                "actor-pov-world-inspection-southeast-empty",
                "actor-pov-world-inspection-south-empty",
                "actor-pov-world-inspection-southwest-empty",
                "actor-pov-world-inspection-west-empty",
                "actor-pov-world-inspection-connectors",
                "actor-pov-actor-inventory-grid",
                "actor-pov-actor-inventory-inspection-grid"
            ],
            components.Select(component => component.Id));
    }

    [Fact]
    public void MainComponentsConnectImmediateParentOwningCellToCurrentPlace()
    {
        var fixture = ActorPovFixture.Create();
        var model = ActorPovPlayScreenModelBuilder.Build(
            fixture.World,
            fixture.ActorId,
            fixture.ActionPlans,
            SadConsoleRect.FromSize(1, 1, 118, 38),
            fixture.Appearance,
            fixture.ActionPlanDescriptor);

        var components = ActorPovPlayComponentFactory.MainComponents(model);
        var connector = Assert.Single(components.OfType<ConnectorLineComponent>(), component => component.Id == "actor-pov-parent-chain-connectors");
        var currentPlace = Assert.Single(components.OfType<InventorySpaceComponent>(), component => component.Id == "actor-pov-current-place-grid");

        var segment = Assert.Single(connector.View.Segments, segment => segment.Id == "parent-chain-0-to-current-place");
        Assert.True(segment.Start.CellX < segment.End.CellX);
        Assert.Equal(currentPlace.Bounds.Left, segment.End.CellX);
    }

    [Fact]
    public void MainComponentsConnectAdjacentWorldEntityCellToInspectionPanel()
    {
        var fixture = ActorPovFixture.Create();
        var model = ActorPovPlayScreenModelBuilder.Build(
            fixture.World,
            fixture.ActorId,
            fixture.ActionPlans,
            SadConsoleRect.FromSize(1, 1, 118, 38),
            fixture.Appearance,
            fixture.ActionPlanDescriptor);

        var components = ActorPovPlayComponentFactory.MainComponents(model);
        var connector = Assert.Single(components.OfType<ConnectorLineComponent>(), component => component.Id == "actor-pov-world-inspection-connectors");
        var currentPlace = Assert.Single(components.OfType<InventorySpaceComponent>(), component => component.Id == "actor-pov-current-place-grid");
        var inspected = Assert.Single(components.OfType<InventorySpaceComponent>(), component => component.Id == "actor-pov-world-inspection-east-grid");

        var segment = Assert.Single(connector.View.Segments, segment => segment.Id == "current-place-east-to-world-inspection-east");
        var sourceCell = currentPlace.CellBounds(new GridCoord(2, 0));
        Assert.Equal(sourceCell.Left + (sourceCell.Width / 2), segment.Start.CellX);
        Assert.Equal(sourceCell.Top + (sourceCell.Height / 2), segment.Start.CellY);
        Assert.Equal(inspected.Bounds.Left, segment.End.CellX);
        Assert.Equal(inspected.Bounds.Top + (inspected.Bounds.Height / 2), segment.End.CellY);
    }

    private static (WorldState World, EntityId ActorId) CurrentPlaceMissingFixture()
    {
        var world = new WorldState();
        var planeId = new PlaneId("orphanPlane");
        var actorId = new EntityId("orphanActor");
        world.Planes.Add(planeId, new Plane(planeId, "Orphan Plane", 1, 1));
        world.AddNode(planeId, new GridCoord(0, 0));
        var nodeId = world.GetNodeId(new PlaneCoord(planeId, new GridCoord(0, 0)));
        world.Entities.Add(actorId, new Entity(actorId, "Orphan Actor", nodeId, 1, 1, 1, 1));
        world.Occupancy.Add(nodeId, actorId);
        return (world, actorId);
    }

    private static void AssertInside(SadConsoleRect outer, SadConsoleRect inner)
    {
        Assert.True(inner.Left >= outer.Left);
        Assert.True(inner.Top >= outer.Top);
        Assert.True(inner.Left + inner.Width <= outer.Left + outer.Width);
        Assert.True(inner.Bottom <= outer.Bottom);
    }

    private sealed record ActorPovFixture(
        WorldState World,
        EntityId ScenarioHostId,
        EntityId RoomId,
        EntityId ActorId,
        EntityId ChestId,
        EntityId BackpackId,
        EntityId? SecondChestId,
        EntityId? SecondBackpackId,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> ActionPlans,
        Func<EntityId, ActionPlanDescriptor?> ActionPlanDescriptor,
        Func<EntityId, EntityInspectionAppearance> Appearance)
    {
        public static ActorPovFixture Create(bool includeSecondCandidates = false)
        {
            var world = new WorldState();
            var hostId = new EntityId("scenarioHost");
            var roomId = new EntityId("room");
            var actorId = new EntityId("actor");
            var chestId = new EntityId("chest");
            var backpackId = new EntityId("backpack");
            var secondChestId = includeSecondCandidates ? new EntityId("chest2") : (EntityId?)null;
            var secondBackpackId = includeSecondCandidates ? new EntityId("backpack2") : (EntityId?)null;
            var outerPlane = new PlaneId("outerPlane");
            var hostPlane = new PlaneId("hostPlane");
            var roomPlane = new PlaneId("roomPlane");
            var actorInventoryPlane = new PlaneId("actorInventory");
            var chestPlane = new PlaneId("chestInventory");
            var backpackPlane = new PlaneId("backpackInventory");

            AddPlane(world, outerPlane, 1, 1);
            AddPlane(world, hostPlane, 1, 1);
            AddPlane(world, roomPlane, 5, 2);
            AddPlane(world, actorInventoryPlane, 3, 1);
            AddPlane(world, chestPlane, 1, 1);
            AddPlane(world, backpackPlane, 1, 1);
            if (includeSecondCandidates)
            {
                AddPlane(world, new PlaneId("chest2Inventory"), 1, 1);
                AddPlane(world, new PlaneId("backpack2Inventory"), 1, 1);
            }

            AddEntity(world, hostId, "Scenario Host", new PlaneCoord(outerPlane, new GridCoord(0, 0)), 1, 1, 100, 100);
            AddEntity(world, roomId, "Room", new PlaneCoord(hostPlane, new GridCoord(0, 0)), 5, 2, 50, 50);
            AddEntity(world, actorId, "Actor", new PlaneCoord(roomPlane, new GridCoord(1, 0)), 3, 1, 1, 10);
            AddEntity(world, chestId, "Chest", new PlaneCoord(roomPlane, new GridCoord(2, 0)), 1, 1, 5, 5);
            AddEntity(world, backpackId, "Backpack", new PlaneCoord(actorInventoryPlane, new GridCoord(0, 0)), 1, 1, 1, 5);
            if (secondChestId is { } chest2 && secondBackpackId is { } backpack2)
            {
                AddEntity(world, chest2, "Chest 2", new PlaneCoord(roomPlane, new GridCoord(3, 0)), 1, 1, 5, 5);
                AddEntity(world, backpack2, "Backpack 2", new PlaneCoord(actorInventoryPlane, new GridCoord(1, 0)), 1, 1, 1, 5);
            }

            world.RegisterInventoryPlane(hostId, hostPlane);
            world.RegisterInventoryPlane(roomId, roomPlane);
            world.RegisterInventoryPlane(actorId, actorInventoryPlane);
            world.RegisterInventoryPlane(chestId, chestPlane);
            world.RegisterInventoryPlane(backpackId, backpackPlane);
            if (secondChestId is { } registeredChest2 && secondBackpackId is { } registeredBackpack2)
            {
                world.RegisterInventoryPlane(registeredChest2, new PlaneId("chest2Inventory"));
                world.RegisterInventoryPlane(registeredBackpack2, new PlaneId("backpack2Inventory"));
            }

            var plan = new ActionPlanDescriptor(
                new ActionPlanId("actorPlan"),
                [],
                Behavior: new ActionPlanBehaviorDescriptor([
                    new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.EnterTarget)
                ]));
            IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans = new Dictionary<EntityId, IEntityActionPlan>();
            var appearances = new Dictionary<EntityId, EntityInspectionAppearance>
            {
                [hostId] = new('H', PresentationColor.Gray),
                [roomId] = new('#', PresentationColor.Gray),
                [actorId] = new('@', PresentationColor.Yellow),
                [chestId] = new('c', PresentationColor.Earth),
                [backpackId] = new('b', PresentationColor.Cyan)
            };
            if (secondChestId is { } appearanceChest2 && secondBackpackId is { } appearanceBackpack2)
            {
                appearances[appearanceChest2] = new('C', PresentationColor.Earth);
                appearances[appearanceBackpack2] = new('B', PresentationColor.Cyan);
            }

            return new ActorPovFixture(
                world,
                hostId,
                roomId,
                actorId,
                chestId,
                backpackId,
                secondChestId,
                secondBackpackId,
                actionPlans,
                entityId => entityId == actorId ? plan : null,
                entityId => appearances.TryGetValue(entityId, out var appearance) ? appearance : new EntityInspectionAppearance('?', PresentationColor.Gray));
        }

        public static ActorPovFixture CreateDeepParentChain()
        {
            var world = new WorldState();
            var hostId = new EntityId("scenarioHost");
            var roomId = new EntityId("room");
            var actorId = new EntityId("actor");
            var chestId = new EntityId("chest");
            var backpackId = new EntityId("backpack");
            var ancestorIds = new[]
            {
                hostId,
                new EntityId("ancestor1"),
                new EntityId("ancestor2"),
                new EntityId("ancestor3"),
                new EntityId("ancestor4"),
                new EntityId("ancestor5"),
                new EntityId("ancestor6"),
                new EntityId("ancestor7"),
                new EntityId("ancestor8"),
                roomId
            };
            var planes = Enumerable.Range(0, ancestorIds.Length + 1)
                .Select(index => new PlaneId($"deepPlane{index}"))
                .ToList();
            var actorInventoryPlane = new PlaneId("actorInventory");
            var chestPlane = new PlaneId("chestInventory");
            var backpackPlane = new PlaneId("backpackInventory");

            foreach (var plane in planes)
            {
                AddPlane(world, plane, 4, 2);
            }

            AddPlane(world, actorInventoryPlane, 2, 1);
            AddPlane(world, chestPlane, 1, 1);
            AddPlane(world, backpackPlane, 1, 1);

            for (var index = 0; index < ancestorIds.Length; index++)
            {
                var inventoryWidth = ancestorIds[index] == roomId ? 4 : 1;
                var inventoryHeight = ancestorIds[index] == roomId ? 2 : 1;
                AddEntity(world, ancestorIds[index], index == 0 ? "Scenario Host" : ancestorIds[index] == roomId ? "Room" : $"Ancestor {index}", new PlaneCoord(planes[index], new GridCoord(0, 0)), inventoryWidth, inventoryHeight, 100 - index, 100);
                world.RegisterInventoryPlane(ancestorIds[index], planes[index + 1]);
            }

            AddEntity(world, actorId, "Actor", new PlaneCoord(planes[^1], new GridCoord(1, 0)), 2, 1, 1, 10);
            AddEntity(world, chestId, "Chest", new PlaneCoord(planes[^1], new GridCoord(2, 0)), 1, 1, 5, 5);
            AddEntity(world, backpackId, "Backpack", new PlaneCoord(actorInventoryPlane, new GridCoord(0, 0)), 1, 1, 1, 5);
            world.RegisterInventoryPlane(actorId, actorInventoryPlane);
            world.RegisterInventoryPlane(chestId, chestPlane);
            world.RegisterInventoryPlane(backpackId, backpackPlane);

            var plan = new ActionPlanDescriptor(
                new ActionPlanId("actorPlan"),
                [],
                Behavior: new ActionPlanBehaviorDescriptor([
                    new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.EnterTarget)
                ]));
            IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans = new Dictionary<EntityId, IEntityActionPlan>();
            var appearances = ancestorIds.ToDictionary(entityId => entityId, _ => new EntityInspectionAppearance('#', PresentationColor.Gray));
            appearances[actorId] = new EntityInspectionAppearance('@', PresentationColor.Yellow);
            appearances[chestId] = new EntityInspectionAppearance('c', PresentationColor.Earth);
            appearances[backpackId] = new EntityInspectionAppearance('b', PresentationColor.Cyan);

            return new ActorPovFixture(
                world,
                hostId,
                roomId,
                actorId,
                chestId,
                backpackId,
                null,
                null,
                actionPlans,
                entityId => entityId == actorId ? plan : null,
                entityId => appearances.TryGetValue(entityId, out var appearance) ? appearance : new EntityInspectionAppearance('?', PresentationColor.Gray));
        }

        private static void AddPlane(WorldState world, PlaneId planeId, int width, int height)
        {
            world.Planes.Add(planeId, new Plane(planeId, planeId.Value, width, height));
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    world.AddNode(planeId, new GridCoord(x, y));
                }
            }
        }

        private static void AddEntity(WorldState world, EntityId entityId, string name, PlaneCoord location, int inventoryWidth, int inventoryHeight, int bulk, int aperture)
        {
            var nodeId = world.GetNodeId(location);
            world.Entities.Add(entityId, new Entity(entityId, name, nodeId, inventoryWidth, inventoryHeight, bulk, aperture));
            world.Occupancy.Add(nodeId, entityId);
        }
    }
}

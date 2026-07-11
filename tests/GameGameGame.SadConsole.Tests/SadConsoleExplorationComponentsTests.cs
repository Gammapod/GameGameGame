using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp;

namespace GameGameGame.SadConsole.Tests;

public sealed class SadConsoleExplorationComponentsTests
{
    [Fact]
    public void ScenarioSelectionOpensPlayEditSubPanelBeforeNavigating()
    {
        var model = SadConsoleScenarioSelectionModel.FromCatalog(new ScenarioCatalogResult(
            [new ScenarioCatalogEntry("demo.yaml", "demo", "Demo Scenario", "A useful test scenario.")],
            []));

        var initial = model.BuildScreen();

        Assert.Equal(SadConsoleExplorationScreenKind.ScenarioSelection, initial.Kind);
        Assert.Single(initial.Components);
        Assert.Equal(SadConsoleExplorationComponentState.Focused, initial.Components[0].State);

        var openResult = model.Handle(SadConsoleExplorationCommand.Activate);
        var withCommands = model.BuildScreen();

        Assert.Equal(SadConsoleExplorationScreenKind.ScenarioSelection, openResult.NextScreen);
        Assert.True(model.CommandChoiceOpen);
        Assert.Equal(2, withCommands.Components.Count);
        Assert.Equal(SadConsoleExplorationComponentKind.ScenarioCommandList, withCommands.Components[1].Kind);
        Assert.Equal(SadConsoleExplorationComponentState.Focused, withCommands.Components[1].State);

        var playResult = model.Handle(SadConsoleExplorationCommand.Activate);

        Assert.Equal(SadConsoleExplorationScreenKind.SimulationPlay, playResult.NextScreen);
        Assert.Equal("demo", playResult.Scenario?.ScenarioId);
    }

    [Fact]
    public void ComponentBorderPaletteDistinguishesUnselectedSelectedAndFocused()
    {
        var palette = new SadConsoleExplorationBorderPalette("dim", "selected", "focused");

        var unselected = new SadConsoleExplorationComponent("a", SadConsoleExplorationComponentKind.EntityTemplateList, "A", [], new SadConsoleRect(0, 0, 10, 5), SadConsoleExplorationComponentState.Unselected);
        var selected = unselected with { State = SadConsoleExplorationComponentState.Selected };
        var focused = unselected with { State = SadConsoleExplorationComponentState.Focused };

        Assert.Equal("dim", unselected.BorderColor(palette));
        Assert.Equal("selected", selected.BorderColor(palette));
        Assert.Equal("focused", focused.BorderColor(palette));
    }

    [Fact]
    public void ScenarioEditScreenRoutesEntityAndActionPlanSelections()
    {
        var scenario = DemoScenario();
        var model = new SadConsoleScenarioEditScreenModel(scenario);

        Assert.Equal(SadConsoleExplorationComponentKind.ScenarioPreviewList, model.BuildScreen().Components[0].Kind);

        model.Handle(SadConsoleExplorationCommand.MoveNextComponent);
        model.Handle(SadConsoleExplorationCommand.MoveNextComponent);
        model.Handle(SadConsoleExplorationCommand.Activate);
        var entityResult = model.Handle(SadConsoleExplorationCommand.Activate);

        Assert.Equal(SadConsoleExplorationScreenKind.EntityTemplateEdit, entityResult.NextScreen);
        Assert.Equal("player", entityResult.EntityTemplate?.TemplateId);

        model = new SadConsoleScenarioEditScreenModel(scenario);
        model.Handle(SadConsoleExplorationCommand.MoveNextComponent);
        model.Handle(SadConsoleExplorationCommand.MoveNextComponent);
        model.Handle(SadConsoleExplorationCommand.MoveNextComponent);
        model.Handle(SadConsoleExplorationCommand.Activate);
        var actionResult = model.Handle(SadConsoleExplorationCommand.Activate);

        Assert.Equal(SadConsoleExplorationScreenKind.ActionPlanEdit, actionResult.NextScreen);
        Assert.Equal("wander", actionResult.ActionPlan?.ActionPlanId);
    }

    [Fact]
    public void EntityScreenCanJumpToReferencedActionPlanAndCancelBackToScenarioEdit()
    {
        var scenario = DemoScenario();
        var model = new SadConsoleEntityTemplateEditScreenModel(scenario.EntityTemplates[0]);

        var screen = model.BuildScreen();
        Assert.Contains(screen.Components, component => component.Kind == SadConsoleExplorationComponentKind.PresentationFields);
        Assert.Contains(screen.Components, component => component.Kind == SadConsoleExplorationComponentKind.TargetingFields);
        Assert.Contains(screen.Components, component => component.Kind == SadConsoleExplorationComponentKind.InventoryFields);

        var jump = model.JumpToActionPlan(scenario.ActionPlans);
        Assert.Equal(SadConsoleExplorationScreenKind.ActionPlanEdit, jump.NextScreen);
        Assert.Equal("wander", jump.ActionPlan?.ActionPlanId);

        var cancel = model.Handle(SadConsoleExplorationCommand.Cancel);
        Assert.Equal(SadConsoleExplorationScreenKind.ScenarioEdit, cancel.NextScreen);
    }

    [Fact]
    public void ActionPlanCancelReturnsToOriginScreen()
    {
        var actionPlan = DemoScenario().ActionPlans[0];
        var fromEntity = new SadConsoleActionPlanEditScreenModel(actionPlan, SadConsoleExplorationScreenKind.EntityTemplateEdit);
        var fromScenario = new SadConsoleActionPlanEditScreenModel(actionPlan, SadConsoleExplorationScreenKind.ScenarioEdit);

        Assert.Equal(SadConsoleExplorationScreenKind.EntityTemplateEdit, fromEntity.Handle(SadConsoleExplorationCommand.Cancel).NextScreen);
        Assert.Equal(SadConsoleExplorationScreenKind.ScenarioEdit, fromScenario.Handle(SadConsoleExplorationCommand.Cancel).NextScreen);
    }

    [Fact]
    public void ScenarioEditItemCanBeProjectedFromEditorSnapshot()
    {
        var snapshot = new FrontendEditorSnapshot(
            "demo.yaml",
            false,
            [new FrontendEditorScenarioSummary("demo", "Demo", "root", "player", "player-1", new GridCoord(2, 3))],
            [new FrontendEditorEntityTemplateSummary(
                "player",
                "Player",
                '@',
                PresentationColor.Yellow,
                5,
                4,
                1,
                9,
                "wander",
                new FrontendEditorActionStateDefaultsSummary(Direction.East, null),
                [new FrontendEditorTargetingRuleSummary(0, "target", null, "rock", "Rock", 3)],
                [new FrontendEditorCarriedEntitySummary("rock-1", "rock", "Rock", '*', PresentationColor.Gray, new GridCoord(1, 2), [])],
                [])],
            [new FrontendEditorActionPlanSummary("wander", "canonical", [new FrontendEditorActionPlanStepSummary(0, default, "Move")], ["Move"])],
            [],
            [],
            "yaml",
            []);

        var scenario = SadConsoleScenarioEditScreenModel.FromSnapshot("demo.yaml", snapshot, "demo");

        Assert.Equal("root", scenario.ScenarioRootEntityTemplateId);
        Assert.Equal(2, scenario.PlayerX);
        Assert.Equal("Player", scenario.EntityTemplates[0].Name);
        Assert.Equal("wander", scenario.EntityTemplates[0].ActionPlanId);
        Assert.Equal("0: Move", scenario.ActionPlans[0].Steps[0]);
    }

    private static SadConsoleScenarioEditItem DemoScenario() => new(
        "demo.yaml",
        "demo",
        "Demo Scenario",
        "root",
        "player",
        2,
        3,
        [new SadConsoleEntityTemplateEditItem(
            "player",
            "Player",
            '@',
            "Yellow",
            "wander",
            [new SadConsoleTargetingSlotEditItem(0, "target", "Rock", 3)],
            [new SadConsoleInventoryItemEditItem("rock-1", 1, 2, "Rock", "9", "1")],
            5,
            4,
            9,
            1)],
        [new SadConsoleActionPlanEditItem("wander", ["0: Move", "1: Wait"])],
        ["turn-0 preview", "player"]);
}

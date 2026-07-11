using GameGameGame.Content;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Navigation;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed class EntityTemplateEditScreen
{
    private readonly FrontendEditorEntityTemplateSummary _template;
    private readonly List<FrontendEditorActionPlanSummary> _actionPlans;
    private readonly FocusRouter _focusRouter;
    private int _selectedTargetingSlotIndex;
    private int _selectedInventoryItemIndex;
    private bool _targetingSlotPanelOpen;

    private EntityTemplateEditScreen(FrontendEditorEntityTemplateSummary template, IReadOnlyList<FrontendEditorActionPlanSummary> actionPlans)
    {
        _template = template;
        _actionPlans = actionPlans.ToList();
        _focusRouter = new FocusRouter([
            new FocusTarget("presentation"),
            new FocusTarget("targeting"),
            new FocusTarget("inventory")
        ]);
    }

    public string TemplateId => _template.TemplateId;
    public string Title => $"Edit Entity Template: {_template.Name}";
    public string Purpose => "Review authored presentation, targeting, and inventory information. Edits will be enabled field-by-field after layout review.";
    public string? FocusedComponentId => _focusRouter.FocusedComponentId;
    public string? SelectedComponentId => _focusRouter.SelectedComponentId;
    public int SelectedTargetingSlotIndex => _selectedTargetingSlotIndex;
    public int SelectedInventoryItemIndex => _selectedInventoryItemIndex;

    public static EntityTemplateEditScreen FromSnapshot(FrontendEditorSnapshot snapshot, string templateId)
    {
        var template = snapshot.EntityTemplates.First(template => template.TemplateId == templateId);
        return new EntityTemplateEditScreen(template, snapshot.ActionPlans);
    }

    public IReadOnlyList<IUiComponent> Components() =>
    [
        PresentationFields(),
        TargetingPanel(),
        InventoryPanel()
    ];

    public string FooterText()
    {
        if (FocusedComponentId is null)
        {
            return "No component focused: arrows choose component. Enter focuses. Esc returns to Scenario Edit.";
        }

        return FocusedComponentId switch
        {
            "presentation" => "Presentation focused: Enter jumps to default action plan when one exists. Esc releases focus.",
            "targeting" => _targetingSlotPanelOpen
                ? "Targeting slot detail focused: review editable fields. Esc closes 3.2.1 detail panel."
                : "Targeting focused: Up/Down chooses targeting slot. Enter opens 3.2.1 slot details. Esc releases focus.",
            "inventory" => "Inventory focused: Up/Down chooses carried inventory row. Esc releases focus.",
            _ => "Esc releases focus."
        };
    }

    public EntityTemplateEditResult Handle(UiComponentCommand command)
    {
        if (_targetingSlotPanelOpen)
        {
            if (command == UiComponentCommand.Cancel)
            {
                _targetingSlotPanelOpen = false;
                return EntityTemplateEditResult.Stay("Closed targeting slot detail panel.");
            }

            return EntityTemplateEditResult.Stay("Targeting slot detail fields are read-only in this shell pass.");
        }

        if (FocusedComponentId is { } focused)
        {
            return HandleFocused(focused, command);
        }

        var result = _focusRouter.Handle(command);
        return result.Kind switch
        {
            FocusRouterResultKind.CancelScreen => EntityTemplateEditResult.ReturnToScenarioEdit("Returned to Scenario Edit."),
            FocusRouterResultKind.SelectedComponent => EntityTemplateEditResult.Stay($"Selected component: {result.ComponentId}."),
            FocusRouterResultKind.FocusedComponent => EntityTemplateEditResult.Stay($"Focused component: {result.ComponentId}."),
            _ => EntityTemplateEditResult.Stay("Use arrows to choose a component, Enter to focus, Esc to return.")
        };
    }

    private EntityTemplateEditResult HandleFocused(string focused, UiComponentCommand command)
    {
        if (command == UiComponentCommand.Cancel)
        {
            _focusRouter.Handle(UiComponentCommand.Cancel);
            return EntityTemplateEditResult.Stay($"Released focus from {focused}.");
        }

        if (command is UiComponentCommand.Up or UiComponentCommand.Left)
        {
            MoveFocusedSelection(focused, -1);
            return EntityTemplateEditResult.Stay(FocusedSelectionMessage(focused));
        }

        if (command is UiComponentCommand.Down or UiComponentCommand.Right)
        {
            MoveFocusedSelection(focused, 1);
            return EntityTemplateEditResult.Stay(FocusedSelectionMessage(focused));
        }

        if (command == UiComponentCommand.Select && focused == "presentation")
        {
            return JumpToActionPlan();
        }

        if (command == UiComponentCommand.Select && focused == "targeting")
        {
            if (_template.TargetingRules.Count == 0)
            {
                return EntityTemplateEditResult.Stay("No targeting slot is available to inspect.");
            }

            _targetingSlotPanelOpen = true;
            return EntityTemplateEditResult.Stay($"Opened 3.2.1 details for targeting slot {DisplaySlotNumber(_template.TargetingRules[_selectedTargetingSlotIndex])}.");
        }

        return EntityTemplateEditResult.Stay("This component is read-only in the first template-edit shell pass.");
    }

    private EntityTemplateEditResult JumpToActionPlan()
    {
        if (string.IsNullOrWhiteSpace(_template.DefaultActionPlanId))
        {
            return EntityTemplateEditResult.Stay("Template has no default action plan to jump to.");
        }

        var plan = _actionPlans.FirstOrDefault(plan => plan.ActionPlanId == _template.DefaultActionPlanId);
        return plan is null
            ? EntityTemplateEditResult.Stay($"Default action plan '{_template.DefaultActionPlanId}' was not found.")
            : EntityTemplateEditResult.OpenActionPlan(plan.ActionPlanId, $"Action Plan screen next: {plan.ActionPlanId}.");
    }

    private void MoveFocusedSelection(string focused, int delta)
    {
        if (focused == "targeting" && _template.TargetingRules.Count > 0)
        {
            _selectedTargetingSlotIndex = Math.Clamp(_selectedTargetingSlotIndex + delta, 0, _template.TargetingRules.Count - 1);
        }
        else if (focused == "inventory" && _template.CarriedEntities.Count > 0)
        {
            _selectedInventoryItemIndex = Math.Clamp(_selectedInventoryItemIndex + delta, 0, _template.CarriedEntities.Count - 1);
        }
    }

    private string FocusedSelectionMessage(string focused) => focused switch
    {
        "targeting" => _template.TargetingRules.Count == 0 ? "No targeting slots defined." : $"Selected targeting slot: {DisplaySlotNumber(_template.TargetingRules[_selectedTargetingSlotIndex])}.",
        "inventory" => _template.CarriedEntities.Count == 0 ? "No carried inventory entries defined." : $"Selected inventory entry: {_template.CarriedEntities[_selectedInventoryItemIndex].EntityId}.",
        _ => "Selection unchanged."
    };

    private FieldGroupComponent PresentationFields() => new(
        "presentation",
        "3.1 Presentation information",
        new SadConsoleRect(1, 4, 44, 16),
        [
            new EditableFieldComponent("name", "name", _template.Name, EditableFieldMode.Editable),
            new EditableFieldComponent("glyph", "glyph", _template.Glyph.ToString(), EditableFieldMode.Editable),
            new EditableFieldComponent("color", "color", _template.Color.ToString(), EditableFieldMode.Editable),
            new EditableFieldComponent("action-plan", "action plan", _template.DefaultActionPlanId ?? "(none)", EditableFieldMode.Editable)
        ],
        _focusRouter.StateFor("presentation"));

    private PanelComponent TargetingPanel()
    {
        var rows = _template.TargetingRules.Count == 0
            ? new List<string> { "No targeting slots defined." }
            : _template.TargetingRules.Select((slot, index) =>
                $"{(index == _selectedTargetingSlotIndex ? ">" : " ")} slot {DisplaySlotNumber(slot)}: {FormatTargetingSlotSummary(slot)}").ToList();

        return new PanelComponent(
            "targeting",
            "3.2 Targeting information",
            new SadConsoleRect(48, 4, 69, 16),
            rows,
            _focusRouter.StateFor("targeting"));
    }

    public IUiComponent? OverlayComponent() => _targetingSlotPanelOpen ? TargetingSlotDetailPanel() : null;

    private FieldGroupComponent TargetingSlotDetailPanel()
    {
        var slot = _template.TargetingRules[_selectedTargetingSlotIndex];
        return new FieldGroupComponent(
            "targeting-slot-detail",
            $"3.2.1 Targeting slot {DisplaySlotNumber(slot)}",
            new SadConsoleRect(60, 8, 45, 11),
            [
                new EditableFieldComponent("target-label", "target label", slot.Label ?? "", EditableFieldMode.Editable),
                new EditableFieldComponent("target-template", "target template/criteria", slot.TargetTemplateName ?? slot.TargetTemplateId, EditableFieldMode.Editable),
                new EditableFieldComponent("target-range", "target range", slot.Range.ToString(), EditableFieldMode.Editable)
            ],
            UiComponentState.Focused);
    }

    private static string FormatTargetingSlotSummary(FrontendEditorTargetingRuleSummary slot)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(slot.Label))
        {
            parts.Add(slot.Label!);
        }

        var target = slot.TargetTemplateName ?? slot.TargetTemplateId;
        if (!string.IsNullOrWhiteSpace(target))
        {
            parts.Add(target);
        }

        return parts.Count == 0 ? "(unconfigured)" : string.Join(' ', parts);
    }

    private static int DisplaySlotNumber(FrontendEditorTargetingRuleSummary slot) => slot.Slot + 1;

    private PanelComponent InventoryPanel()
    {
        var rows = new List<string>
        {
            $"inventory space X: {_template.InventoryWidth}",
            $"inventory space Y: {_template.InventoryHeight}",
            $"Aperture: {_template.Aperture}",
            $"Bulk: {_template.Bulk}",
            $"brush selection: {(_template.CarriedEntities.Count == 0 ? "(none)" : _template.CarriedEntities[_selectedInventoryItemIndex].TemplateName ?? _template.CarriedEntities[_selectedInventoryItemIndex].TemplateId ?? "unbound")}",
            "3.3.2 inventory-drawing panel: placeholder"
        };

        if (_template.CarriedEntities.Count > 0)
        {
            rows.Add("3.3.1 carried entries:");
            rows.AddRange(_template.CarriedEntities.Select((item, index) =>
                $"{(index == _selectedInventoryItemIndex ? ">" : " ")} ({item.Coord.X},{item.Coord.Y}) {item.Glyph?.ToString() ?? "?"} {item.TemplateName ?? item.TemplateId ?? item.EntityId}"));
        }

        return new PanelComponent(
            "inventory",
            "3.3 Inventory information",
            new SadConsoleRect(1, 18, 116, 36),
            rows,
            _focusRouter.StateFor("inventory"));
    }
}

internal sealed record EntityTemplateEditResult(EntityTemplateEditResultKind Kind, string Message, string? ActionPlanId = null)
{
    public static EntityTemplateEditResult Stay(string message) => new(EntityTemplateEditResultKind.Stay, message);
    public static EntityTemplateEditResult ReturnToScenarioEdit(string message) => new(EntityTemplateEditResultKind.ReturnToScenarioEdit, message);
    public static EntityTemplateEditResult OpenActionPlan(string actionPlanId, string message) => new(EntityTemplateEditResultKind.OpenActionPlan, message, actionPlanId);
}

internal enum EntityTemplateEditResultKind
{
    Stay,
    ReturnToScenarioEdit,
    OpenActionPlan
}

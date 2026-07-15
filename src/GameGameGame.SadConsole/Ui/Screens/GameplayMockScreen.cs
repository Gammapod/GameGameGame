using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Styling;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed record GameplayMockFrame(
    string Title,
    EntityPanelProjection PlayerProjection,
    EntityPanelProjection? CurrentPlaceProjection,
    EntityPanelProjection? InspectedProjection,
    string CurrentRoomSizeLabel,
    IReadOnlyList<string> CurrentPlaceEntityRows,
    IReadOnlyList<string> InspectedTargetingRows,
    IReadOnlyList<string> InspectedActionPlanRows,
    SadConsoleRect CurrentPlaceBounds,
    SadConsoleRect HudBounds,
    SadConsoleRect InspectionBounds,
    IReadOnlyList<IUiComponent> Components,
    IReadOnlyList<string> HudRows,
    IReadOnlyList<string> CurrentPlacePlayerLogRows,
    IReadOnlyList<string> Diagnostics);

internal sealed class GameplayMockScreen
{
    private readonly PlayableScenarioSession _session;
    private readonly EntityPanelProjectionService _panelProjection;
    private readonly ControlledActorCommandService _commands;
    private readonly SimulationHistorySession _history;
    private readonly MovementService _movement = new();
    private ActionLogProjection? _actionLog;
    private EntityId? _inspectedEntityId;
    private int _selectedActionStepIndex;

    public GameplayMockScreen(PlayableScenarioSession session)
    {
        _session = session;
        _panelProjection = new EntityPanelProjectionService(
            entityId => session.Registry.GetPresentationForEntity(entityId).ToInspectionAppearance(),
            GetActionPlanDescriptorForEntity);
        _commands = new ControlledActorCommandService(
            _movement,
            // Temporary debug wait uses direct-control compatibility: the controlled actor's authored plan
            // must not also resolve autonomously while it is acting as the player-controlled entity.
            session.ActionPlans
                .Where(entry => entry.Key != session.PlayerEntityId)
                .ToDictionary(entry => entry.Key, entry => entry.Value),
            (world, entityId) => TargetingService.RefreshTargets(world, session.Registry, entityId));
        _history = SimulationHistorySession.Start(
            session.World,
            session.PlayerEntityId,
            session.ActivePlaneId,
            session.ActiveContainerEntityId);
        _actionLog = ActionLogProjection.FromHistory(_history);
    }

    public EntityId PlayerEntityId => _session.PlayerEntityId;
    public EntityId? InspectedEntityId => _inspectedEntityId;
    public int FrameIndex => _history.CurrentFrame.FrameIndex;
    public int SelectedActionStepIndex => _selectedActionStepIndex;
    private WorldState World => _session.World;
    private IReadOnlyDictionary<EntityId, IEntityActionPlan> ProjectionActionPlans => _session.ActionPlans;

    public GameplayMockFrame BuildFrame(int width, int height)
    {
        var safeWidth = Math.Max(40, width);
        var safeHeight = Math.Max(18, height);
        RefreshDisplayTargets();
        var projectionActionPlans = ProjectionActionPlans;
        var playerProjection = _panelProjection.Project(World, _session.PlayerEntityId, projectionActionPlans, _session.PlayerEntityId, _actionLog);
        var diagnostics = new List<string>();
        diagnostics.AddRange(_session.ValidationDiagnostics);
        diagnostics.AddRange(_session.RuntimeFailures);
        diagnostics.AddRange(_session.CapabilityGaps);

        if (playerProjection.PointOfView is null)
        {
            diagnostics.Add("Player POV projection is unavailable.");
        }
        else
        {
            diagnostics.AddRange(playerProjection.PointOfView.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
        }

        var currentPlaceProjection = playerProjection.PointOfView?.CurrentPlace is { } currentPlace
            ? _panelProjection.Project(World, currentPlace.EntityId, projectionActionPlans, _session.PlayerEntityId, _actionLog)
            : null;

        var hudWidth = Math.Clamp(safeWidth / 5, 20, Math.Max(20, safeWidth - 42));
        var hudBounds = SadConsoleRect.FromSize(0, 0, hudWidth, safeHeight);
        var contentLeft = hudBounds.Left + hudBounds.Width + 1;
        var contentWidth = Math.Max(20, safeWidth - contentLeft - 1);
        var inspectionHeight = Math.Max(8, safeHeight / 3);
        var inspectionTop = Math.Max(8, safeHeight - inspectionHeight);
        var currentPlaceBounds = SadConsoleRect.FromSize(contentLeft, 0, contentWidth, inspectionTop);
        var inspectionBounds = SadConsoleRect.FromSize(
            contentLeft,
            inspectionTop,
            contentWidth,
            Math.Max(0, safeHeight - inspectionTop));

        var components = new List<IUiComponent>();
        components.Add(BuildCurrentPlaceComponent(currentPlaceProjection, currentPlaceBounds));
        EntityPanelProjection? inspectedProjection = null;
        if (_inspectedEntityId is { } inspectedEntityId && World.Entities.ContainsKey(inspectedEntityId))
        {
            inspectedProjection = _panelProjection.Project(World, inspectedEntityId, projectionActionPlans, _session.PlayerEntityId, _actionLog);
            components.Add(BuildInspectionComponent(inspectedProjection, inspectionBounds));
        }

        if (diagnostics.Count > 0)
        {
            components.Add(new PanelComponent(
                "play-mock-diagnostics",
                "POV / setup diagnostics",
                SadConsoleRect.FromSize(contentLeft, Math.Max(1, currentPlaceBounds.Bottom - 7), Math.Min(60, contentWidth), Math.Min(6, currentPlaceBounds.Height - 2)),
                diagnostics.Take(4).ToList(),
                UiComponentState.Error));
        }

        return new GameplayMockFrame(
            $"Play UX Mock | {_session.Name} | frame {FrameIndex} | world turn {World.TurnNumber}",
            playerProjection,
            currentPlaceProjection,
            inspectedProjection,
            DescribeCurrentRoomSize(playerProjection.PointOfView?.CurrentPlace),
            BuildCurrentPlaceEntityRows(
                currentPlaceProjection,
                playerProjection.PointOfView?.TargetAdjectives ?? [],
                playerProjection.PointOfView?.ReciprocalAdjectives ?? []),
            inspectedProjection is null ? [] : BuildTargetingRuleRows(inspectedProjection.EntityId),
            inspectedProjection is null ? [] : BuildActionPlanRows(inspectedProjection.EntityId),
            currentPlaceBounds,
            hudBounds,
            inspectionBounds,
            components,
            BuildHudRows(playerProjection, currentPlaceProjection, AvailablePlayerActionSteps(), _selectedActionStepIndex),
            BuildCurrentPlacePlayerLogRows(currentPlaceProjection),
            diagnostics);
    }

    public string InspectNextEntity()
    {
        var frame = BuildFrame(SadConsoleScreenMetrics.ScreenWidth, SadConsoleScreenMetrics.ScreenHeight);
        var candidates = frame.CurrentPlaceProjection?.InventoryGrid?.Cells
            .Select(cell => cell.EntityId)
            .Where(entityId => entityId is not null && entityId != _session.PlayerEntityId)
            .Select(entityId => entityId!.Value)
            .Distinct()
            .OrderBy(entityId => entityId.Value, StringComparer.Ordinal)
            .ToList() ?? [];

        if (candidates.Count == 0)
        {
            _inspectedEntityId = null;
            return "No non-player entity is visible in the current POV place.";
        }

        var nextIndex = _inspectedEntityId is null ? 0 : (candidates.IndexOf(_inspectedEntityId.Value) + 1) % candidates.Count;
        _inspectedEntityId = candidates[Math.Max(0, nextIndex)];
        return $"Inspecting {World.Entities[_inspectedEntityId.Value].Name}.";
    }

    public void ClearInspection() => _inspectedEntityId = null;

    public string DebugAdvanceOneControlledTurn()
    {
        var result = _history.SubmitControlledCommand(_commands, ControlledActorCommand.Wait());
        _actionLog = ActionLogProjection.FromHistory(_history);
        RefreshDisplayTargets();
        return result.Succeeded
            ? $"Debug wait advanced to frame {FrameIndex}; world turn {World.TurnNumber}."
            : $"Debug wait failed: {result.FailureReason?.ToString() ?? "unknown"}.";
    }

    public string SelectPreviousActionStep() => SelectActionStep(-1);

    public string SelectNextActionStep() => SelectActionStep(1);

    public string ExecuteSelectedActionStep()
    {
        var steps = AvailablePlayerActionSteps();
        if (steps.Count == 0)
        {
            return "No authored action steps are available for the controlled entity.";
        }

        _selectedActionStepIndex = Math.Clamp(_selectedActionStepIndex, 0, steps.Count - 1);
        var step = steps[_selectedActionStepIndex];
        var plan = new ActionPlanDefinition(
            new ActionPlanId($"play-choice-{step.Kind}"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([step]));
        var result = new ActionPlanInterpreter(_movement).Execute(World, _session.PlayerEntityId, plan, new ActionPlanContext());
        PostActionStateUpdater.ApplyFacingFromMovement(World, _session.PlayerEntityId, result.ActorMovementDirection);
        World.RecordTrace(result.Trace);
        if (result.ConsumesTurn)
        {
            World.AdvanceTurn();
        }

        _history.RecordActorInterval([
            new SimulationHistoryActorLog(
                0,
                _session.PlayerEntityId,
                World.Entities[_session.PlayerEntityId].Name,
                result.Succeeded,
                result.ConsumesTurn,
                result.ContinuePlan,
                step.Kind.ToString(),
                result.Trace)
        ], _session.ActivePlaneId, _session.ActiveContainerEntityId);
        _actionLog = ActionLogProjection.FromHistory(_history);
        RefreshDisplayTargets();

        var status = result.Succeeded ? "succeeded" : "failed";
        return $"Selected action {step.Kind} {status}; frame {FrameIndex}, world turn {World.TurnNumber}.";
    }

    private string SelectActionStep(int delta)
    {
        var steps = AvailablePlayerActionSteps();
        if (steps.Count == 0)
        {
            _selectedActionStepIndex = 0;
            return "No authored action steps are available for the controlled entity.";
        }

        _selectedActionStepIndex = (_selectedActionStepIndex + delta + steps.Count) % steps.Count;
        return $"Selected action step {_selectedActionStepIndex + 1}/{steps.Count}: {steps[_selectedActionStepIndex].Kind}.";
    }

    private IReadOnlyList<ActionPlanBehaviorStepDescriptor> AvailablePlayerActionSteps() =>
        GetActionPlanDescriptorForEntity(_session.PlayerEntityId)?.Behavior?.Steps ?? [];

    private void RefreshDisplayTargets()
    {
        foreach (var entityId in _session.ActionPlans.Keys)
        {
            TargetingService.RefreshTargets(World, _session.Registry, entityId);
        }
    }

    private ActionPlanDescriptor? GetActionPlanDescriptorForEntity(EntityId entityId)
    {
        if (!_session.Registry.TryGetTemplateIdForEntity(entityId, out var templateId))
        {
            return null;
        }

        var template = _session.Registry.GetEntityTemplate(templateId);
        return template.DefaultActionPlanId is { } planId
            && _session.Registry.ActionPlanDescriptors.TryGetValue(planId, out var descriptor)
                ? descriptor
                : null;
    }

    private IReadOnlyList<string> BuildCurrentPlaceEntityRows(
        EntityPanelProjection? currentPlaceProjection,
        IReadOnlyList<EntityPointOfViewTargetAdjectiveProjection> targetAdjectives,
        IReadOnlyList<EntityPointOfViewTargetAdjectiveProjection> reciprocalAdjectives)
    {
        var entityIds = currentPlaceProjection?.InventoryGrid?.Cells
            .Select(cell => cell.EntityId)
            .Where(entityId => entityId is not null)
            .Select(entityId => entityId!.Value)
            .Distinct()
            .OrderBy(entityId => World.GetEntityLocation(entityId).Coord.Y)
            .ThenBy(entityId => World.GetEntityLocation(entityId).Coord.X)
            .ThenBy(entityId => entityId.Value, StringComparer.Ordinal)
            .ToList() ?? [];

        var adjectivesByEntity = GroupAdjectivesByEntity(targetAdjectives);
        var reciprocalAdjectivesByEntity = GroupAdjectivesByEntity(reciprocalAdjectives);

        return entityIds.Select(entityId => BuildDisplayedEntityRow(entityId, adjectivesByEntity, reciprocalAdjectivesByEntity)).ToList();
    }

    private static IReadOnlyDictionary<EntityId, IReadOnlyList<string>> GroupAdjectivesByEntity(
        IReadOnlyList<EntityPointOfViewTargetAdjectiveProjection> adjectives) =>
        adjectives
            .GroupBy(adjective => adjective.EntityId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .Select(adjective => adjective.Adjective)
                    .Where(adjective => !string.IsNullOrWhiteSpace(adjective))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList());

    private string BuildDisplayedEntityRow(
        EntityId entityId,
        IReadOnlyDictionary<EntityId, IReadOnlyList<string>> adjectivesByEntity,
        IReadOnlyDictionary<EntityId, IReadOnlyList<string>> reciprocalAdjectivesByEntity)
    {
        var entity = World.Entities[entityId];
        var facing = World.GetActionFacing(entityId)?.ToString() ?? "none";
        var target = FormatCurrentTarget(entityId);
        var adjectives = adjectivesByEntity.TryGetValue(entityId, out var labels) && labels.Count > 0
            ? $"; adjectives {string.Join(", ", labels)}"
            : string.Empty;
        var reciprocalAdjectives = reciprocalAdjectivesByEntity.TryGetValue(entityId, out var reciprocalLabels) && reciprocalLabels.Count > 0
            ? $"; reciprocal {string.Join(", ", reciprocalLabels)}"
            : string.Empty;
        return $"{entity.Name}: facing {facing}; target {target}{adjectives}{reciprocalAdjectives}";
    }

    private string FormatCurrentTarget(EntityId entityId)
    {
        if (!World.ActionStates.TryGetValue(entityId, out var state))
        {
            return "none";
        }

        var labeledTarget = state.LabeledTargets
            .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(labeledTarget.Key))
        {
            return $"{labeledTarget.Key} -> {FormatEntityName(labeledTarget.Value)}";
        }

        if (state.Target is { } target)
        {
            return $"target -> {FormatEntityName(target)}";
        }

        var slottedTarget = state.Targets.OrderBy(entry => entry.Key).FirstOrDefault();
        return slottedTarget.Value.Value is null
            ? "none"
            : $"slot {slottedTarget.Key} -> {FormatEntityName(slottedTarget.Value)}";
    }

    private IReadOnlyList<string> BuildTargetingRuleRows(EntityId entityId)
    {
        if (!_session.Registry.TryGetTemplateIdForEntity(entityId, out var templateId))
        {
            return ["targeting rules: template unavailable"];
        }

        var rules = _session.Registry.GetEntityTemplate(templateId).TargetingRules ?? [];
        if (rules.Count == 0)
        {
            return ["targeting rules: none"];
        }

        return rules.Select(rule =>
        {
            var label = string.IsNullOrWhiteSpace(rule.Label) ? $"slot {rule.Slot}" : rule.Label;
            var target = World.GetActionTarget(entityId, rule.Slot) is { } targetId
                ? FormatEntityName(targetId)
                : "none";
            var template = rule.TargetTemplateId?.Value ?? "any";
            var capabilities = rule.TargetCapabilities.Count == 0 ? "" : $" [{string.Join(",", rule.TargetCapabilities)}]";
            return $"rule {label}: {template} r{rule.Range}{capabilities} -> {target}";
        }).ToList();
    }

    private IReadOnlyList<string> BuildActionPlanRows(EntityId entityId)
    {
        if (!_session.Registry.TryGetTemplateIdForEntity(entityId, out var templateId))
        {
            return ["action plan: template unavailable"];
        }

        var template = _session.Registry.GetEntityTemplate(templateId);
        if (template.DefaultActionPlanId is not { } planId || !_session.Registry.ActionPlanDescriptors.TryGetValue(planId, out var plan))
        {
            return ["action plan: none"];
        }

        var rows = new List<string> { $"action plan: {planId}" };
        if (plan.Behavior?.Steps.Count > 0)
        {
            rows.AddRange(plan.Behavior.Steps.Select((step, index) => $"{index + 1}. {FormatBehaviorStep(step)}"));
        }
        else if (plan.Steps.Count > 0)
        {
            rows.AddRange(plan.Steps.Select((step, index) => $"{index + 1}. {step.Label}"));
        }
        else if (plan.Primitive is { } primitive)
        {
            rows.Add($"1. {primitive.Kind}");
        }
        else
        {
            rows.Add("steps: none");
        }

        return rows;
    }

    private string FormatEntityName(EntityId entityId) =>
        World.Entities.TryGetValue(entityId, out var entity) ? entity.Name : entityId.Value;

    private static string FormatBehaviorStep(ActionPlanBehaviorStepDescriptor step)
    {
        var target = step.TargetLabel is { Length: > 0 }
            ? $" {step.TargetLabel}"
            : step.TargetSlot is { } slot
                ? $" slot {slot}"
                : string.Empty;
        var plan = step.PlanId is { } planId ? $" -> {planId}" : string.Empty;
        return $"{step.Kind}{target}{plan}";
    }

    private static IUiComponent BuildCurrentPlaceComponent(EntityPanelProjection? currentPlaceProjection, SadConsoleRect bounds)
    {
        if (currentPlaceProjection?.InventoryGrid is not { } grid)
        {
            return new PanelComponent(
                "current-place",
                "Current place viewport",
                bounds,
                ["Player POV did not resolve to a usable current-place inventory."],
                UiComponentState.Error);
        }

        return new InventoryGridComponent(
            "current-place",
            $"Current place: {currentPlaceProjection.Name}",
            bounds,
            [$"plane: {grid.PlaneId}", $"inventory: {grid.Width}x{grid.Height}", "centered from player POV"],
            UiComponentState.Selected,
            grid.Width,
            grid.Height,
            new GridCoord(0, 0),
            grid.Cells.Select(cell => new InventoryGridCell(cell.Coord, cell.Glyph, cell.Color)).ToList());
    }

    private static IUiComponent BuildInspectionComponent(EntityPanelProjection projection, SadConsoleRect bounds)
    {
        var rows = new List<string>
        {
            $"{projection.Glyph} {projection.Name}",
            $"id: {projection.EntityId}",
            $"path: {FormatBreadcrumbFromCurrentPlace(projection)}"
        };
        rows.AddRange(projection.Properties.Take(4).Select(property => $"{property.Name}: {property.Value}"));
        if (projection.InventoryGrid is { } grid)
        {
            rows.Add($"inventory: {grid.Width}x{grid.Height} {grid.PlaneId}");
        }

        return new PanelComponent("inspected-entity", "Inspected entity panel", bounds, rows, UiComponentState.Focused);
    }

    private static IReadOnlyList<string> BuildHudRows(
        EntityPanelProjection playerProjection,
        EntityPanelProjection? currentPlaceProjection,
        IReadOnlyList<ActionPlanBehaviorStepDescriptor> actionSteps,
        int selectedActionStepIndex)
    {
        var rows = new List<string>
        {
            $"Player: {playerProjection.Glyph} {playerProjection.Name} ({playerProjection.EntityId})",
            $"Facing: {playerProjection.ActionState.Facing?.ToString() ?? "none"} | Target: {playerProjection.ActionState.Target?.ToString() ?? "none"}",
            $"Current place: {currentPlaceProjection?.Name ?? "none"}"
        };

        if (playerProjection.PointOfView?.CurrentPlace is { } place)
        {
            rows.Add($"Bulk/aperture: {place.ObserverBulk}/{place.PlaceAperture} ratio {place.BulkToApertureRatio?.ToString() ?? "n/a"}");
            rows.Add($"POV rule: {place.SelectionRule}");
        }
        else
        {
            rows.Add("POV rule: unresolved");
        }

        if (actionSteps.Count == 0)
        {
            rows.Add("Actions: none authored");
        }
        else
        {
            var clampedIndex = Math.Clamp(selectedActionStepIndex, 0, actionSteps.Count - 1);
            rows.Add($"Action: {clampedIndex + 1}/{actionSteps.Count} {actionSteps[clampedIndex].Kind}");
        }

        rows.Add("Controls: Left/Right choose | Enter acts | Space debug wait | I inspect | Esc returns");
        return rows;
    }

    private static IReadOnlyList<string> BuildCurrentPlacePlayerLogRows(EntityPanelProjection? currentPlaceProjection)
    {
        if (currentPlaceProjection is null)
        {
            return ["player-log: current place unavailable"];
        }

        if (currentPlaceProjection.LocalLog.Count == 0)
        {
            return ["player-log: no local player-facing ids yet"];
        }

        return currentPlaceProjection.LocalLog
            .SelectMany(ProjectPlayerFacingIds)
            .Take(5)
            .ToList();
    }

    private static IReadOnlyList<string> ProjectPlayerFacingIds(ActionOutcome outcome)
    {
        if (outcome.ActionStepAttempts.Count == 0)
        {
            return [FormatPlayerFacingLogRow(outcome, outcome.ActionKind)];
        }

        return outcome.ActionStepAttempts
            .Select(attempt => FormatPlayerFacingLogRow(outcome, attempt.StepKind))
            .ToList();
    }

    private static string FormatPlayerFacingLogRow(ActionOutcome outcome, string? actionKind)
    {
        var result = outcome.Succeeded ? "success" : "failure";
        var messageId = $"action.{ToSnakeCase(string.IsNullOrWhiteSpace(actionKind) ? "turn" : actionKind!)}.{result}";
        return $"player-log: {messageId} actor={outcome.ActorName} result={result}";
    }

    private static string ToSnakeCase(string value)
    {
        var chars = new List<char>(value.Length + 4);
        for (var index = 0; index < value.Length; index++)
        {
            var c = value[index];
            if (char.IsUpper(c) && index > 0 && value[index - 1] != '_')
            {
                chars.Add('_');
            }

            chars.Add(char.IsWhiteSpace(c) || c == '-' ? '_' : char.ToLowerInvariant(c));
        }

        return new string(chars.ToArray());
    }

    private static string DescribeCurrentRoomSize(EntityPointOfViewCurrentPlaceProjection? currentPlace)
    {
        if (currentPlace?.BulkToApertureRatio is not { } ratio)
        {
            return "Unknown";
        }

        if (ratio >= 0.9m)
        {
            return "Small";
        }

        return ratio < 0.1m ? "Large" : "Medium";
    }

    private static string FormatBreadcrumbFromCurrentPlace(EntityPanelProjection projection) =>
        string.Join(" > ", projection.Breadcrumb.Segments.Select(segment => segment.EntityId.Value));
}

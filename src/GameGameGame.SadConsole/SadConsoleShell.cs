using GameGameGame.Content;
using GameGameGame.Core;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using Console = SadConsole.Console;
using GggDirection = GameGameGame.Core.Direction;
using GggColor = GameGameGame.Content.PresentationColor;

namespace GameGameGame.SadConsoleApp;

// Legacy/deprecated SadConsole shell. Keep as reference only while the new
// componentized exploration architecture is built.
internal sealed class SadConsoleShell : Console
{
    public const int ScreenWidth = 120;
    public const int ScreenHeight = 42;
    private readonly MovementService _movement = new();
    private readonly EntityPanelProjectionService _panelProjection;
    private readonly ControlledActorAffordanceService _affordances;
    private readonly SadConsoleSessionViewBuilder _sessionViewBuilder;
    private readonly ScenarioCatalogResult? _catalog;
    private SadConsoleEditorContext? _editorContext;
    private PlayableScenarioSession? _session;
    private ControlledActorCommandService? _commands;
    private SimulationHistorySession? _history;
    private ActionLogProjection? _actionLog;
    private ShellMode _mode;
    private int _selectedScenarioIndex;
    private GridCoord _worldCursor = new(0, 0);
    private GridCoord _inventoryCursor = new(0, 0);
    private EntityId? _selectedEntity;
    private EntityId? _inspectedEntity;
    private GggDirection? _selectedExitDirection;
    private string _message;

    public SadConsoleShell(SadConsoleStartup startup) : base(ScreenWidth, ScreenHeight)
    {
        UseKeyboard = true;
        IsFocused = true;
        FocusedMode = FocusBehavior.Set;

        _catalog = startup.Catalog;
        _message = startup.Error ?? "Enter launches. Up/Down selects. Esc quits.";
        _panelProjection = new EntityPanelProjectionService(entityId =>
            _session?.Registry.GetPresentationForEntity(entityId).ToInspectionAppearance()
            ?? new EntityInspectionAppearance('?', GggColor.Gray));
        _affordances = new ControlledActorAffordanceService(_movement);
        _sessionViewBuilder = new SadConsoleSessionViewBuilder(_panelProjection, _affordances);

        if (!string.IsNullOrWhiteSpace(startup.DirectContentPath))
        {
            OpenEditorContext(startup.DirectContentPath, startup.DirectScenarioId, launchSimulation: startup.LaunchDirectSimulation);
        }
        else if (startup.DirectSession is { } direct)
        {
            StartSession(direct);
        }
        else if (startup.Error is null && (_catalog?.Entries.Count ?? 0) == 0)
        {
            _message = "No scenarios found. Esc quits.";
        }

        Redraw();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyReleased(Keys.Escape))
        {
            if (_mode == ShellMode.Menu)
            {
                SadConsole.Game.Instance.MonoGameInstance.Exit();
            }
            else if (_mode == ShellMode.Editor)
            {
                if (_editorContext?.IsCommandMenuOpen == true)
                {
                    var result = _editorContext.CancelCommandMenu();
                    _message = result.Message;
                }
                else if (_editorContext?.IsTemplateEditInputActive == true)
                {
                    var result = _editorContext.CancelEdit();
                    _message = result.Message;
                }
                else
                {
                    ReturnToMenuOrExit();
                }
            }
            else if (_mode == ShellMode.Play)
            {
                ReturnToEditorMenuOrExit();
            }
            else
            {
                _mode = ShellMode.Play;
                _selectedEntity = null;
                _message = "Selection cancelled.";
            }

            Redraw();
            return true;
        }

        if (_mode == ShellMode.Menu)
        {
            HandleMenuInput(keyboard);
        }
        else if (_mode == ShellMode.Editor)
        {
            HandleEditorInput(keyboard);
        }
        else
        {
            HandleSessionInput(keyboard);
        }

        Redraw();
        return true;
    }

    private void HandleMenuInput(Keyboard keyboard)
    {
        if (_catalog is null || _catalog.Entries.Count == 0)
        {
            return;
        }

        if (keyboard.IsKeyReleased(Keys.Up))
        {
            _selectedScenarioIndex = Math.Max(0, _selectedScenarioIndex - 1);
        }
        else if (keyboard.IsKeyReleased(Keys.Down))
        {
            _selectedScenarioIndex = Math.Min(_catalog.Entries.Count - 1, _selectedScenarioIndex + 1);
        }
        else if (keyboard.IsKeyReleased(Keys.Enter))
        {
            var entry = _catalog.Entries[_selectedScenarioIndex];
            OpenEditorContext(entry.ContentPath, entry.ScenarioId, launchSimulation: true);
        }
        else if (keyboard.IsKeyReleased(Keys.O))
        {
            var entry = _catalog.Entries[_selectedScenarioIndex];
            OpenEditorContext(entry.ContentPath, entry.ScenarioId, launchSimulation: false);
        }
    }

    private void HandleEditorInput(Keyboard keyboard)
    {
        if (_editorContext is null)
        {
            ReturnToMenuOrExit();
            return;
        }

        if (_editorContext.IsEditingTemplatePresentation)
        {
            HandleEditorTemplateTextEditInput(keyboard);
            return;
        }

        if (_editorContext.IsPickingTemplateInitialFacing)
        {
            HandleEditorTemplateInitialFacingPickerInput(keyboard);
            return;
        }

        if (_editorContext.IsPickingTemplateDefaultActionPlan)
        {
            HandleEditorTemplateDefaultActionPlanPickerInput(keyboard);
            return;
        }

        if (_editorContext.IsEditingActionPlanSteps)
        {
            HandleEditorActionPlanStepInput(keyboard);
            return;
        }

        if (_editorContext.IsEditingTemplateTargetingRule)
        {
            HandleEditorTemplateTargetingRuleInput(keyboard);
            return;
        }

        if (_editorContext.IsTemplateInventoryBrushActive)
        {
            HandleEditorTemplateInventoryBrushInput(keyboard);
            return;
        }

        if (_editorContext.IsCommandMenuOpen)
        {
            HandleEditorCommandMenuInput(keyboard);
            return;
        }

        if (keyboard.IsKeyReleased(Keys.Up))
        {
            _editorContext.MoveSelection(-1);
            _message = _editorContext.Section == SadConsoleEditorSection.Templates
                ? $"Template editor focus: {SadConsoleEditorContext.TemplateFocusLabel(_editorContext.TemplateFocus)}. Select/Enter activates."
                : "Editor browser selection moved. Template presentation edits apply only in Templates section.";
        }
        else if (keyboard.IsKeyReleased(Keys.Down))
        {
            _editorContext.MoveSelection(1);
            _message = _editorContext.Section == SadConsoleEditorSection.Templates
                ? $"Template editor focus: {SadConsoleEditorContext.TemplateFocusLabel(_editorContext.TemplateFocus)}. Select/Enter activates."
                : "Editor browser selection moved. Template presentation edits apply only in Templates section.";
        }
        else if (keyboard.IsKeyReleased(Keys.Left))
        {
            if (_editorContext.Section == SadConsoleEditorSection.Templates && _editorContext.TemplateFocus != SadConsoleEditorTemplateFocus.TemplateSelector)
            {
                var result = _editorContext.MoveTemplateFocus(-1, 0);
                _message = result.Message;
            }
            else
            {
                _editorContext.MoveSection(-1);
                _message = "Editor browser section changed.";
            }
        }
        else if (keyboard.IsKeyReleased(Keys.Right))
        {
            if (_editorContext.Section == SadConsoleEditorSection.Templates)
            {
                var result = _editorContext.MoveTemplateFocus(1, 0);
                _message = result.Message;
            }
            else
            {
                _editorContext.MoveSection(1);
                _message = "Editor browser section changed.";
            }
        }
        else if (keyboard.IsKeyReleased(Keys.T))
        {
            _editorContext.ToggleTextSurface();
            _editorContext.SelectSection(SadConsoleEditorSection.YamlAndDiff);
            _message = "Toggled read-only YAML/diff inspection surface.";
        }
        else if (keyboard.IsKeyReleased(Keys.R))
        {
            var result = _editorContext.RefreshSnapshot();
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.P))
        {
            _editorContext.SelectSection(SadConsoleEditorSection.Preview);
            var preview = _editorContext.RefreshSelectedScenarioPreview();
            _message = preview is null
                ? "No authored scenario is selected for preview."
                : $"Refreshed turn-0 derived runtime preview for {preview.ScenarioId}. Preview is not authored source.";
        }
        else if (keyboard.IsKeyReleased(Keys.S))
        {
            var result = _editorContext.Save();
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.J))
        {
            var result = _editorContext.JumpSelectedPreviewEntityToSourceTemplate();
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.N))
        {
            var result = _editorContext.BeginTemplateNameEdit();
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.G))
        {
            var result = _editorContext.BeginTemplateGlyphEdit();
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.C))
        {
            var result = _editorContext.CycleSelectedTemplateColor();
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.A))
        {
            var result = _editorContext.Section == SadConsoleEditorSection.ActionPlans
                ? _editorContext.BeginActionPlanStepEditor()
                : _editorContext.BeginTemplateDefaultActionPlanPicker();
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.Y))
        {
            var result = _editorContext.BeginTemplateTargetingRuleEditor();
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.B))
        {
            var result = _editorContext.ToggleTemplateInventoryBrush();
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.Enter))
        {
            var result = _editorContext.Section == SadConsoleEditorSection.Templates
                ? _editorContext.ActivateTemplateFocus()
                : _editorContext.OpenCommandMenu();
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.M))
        {
            LaunchSelectedEditorScenario();
        }
    }

    private void HandleEditorCommandMenuInput(Keyboard keyboard)
    {
        if (_editorContext is null)
        {
            return;
        }

        if (keyboard.IsKeyReleased(Keys.Up))
        {
            _editorContext.MoveCommandMenuSelection(-1);
            _message = "Editor command menu selection moved. Enter activates; Esc cancels.";
        }
        else if (keyboard.IsKeyReleased(Keys.Down))
        {
            _editorContext.MoveCommandMenuSelection(1);
            _message = "Editor command menu selection moved. Enter activates; Esc cancels.";
        }
        else if (keyboard.IsKeyReleased(Keys.Enter))
        {
            var result = _editorContext.ActivateSelectedCommand();
            _message = result.Message;
            if (result.RequestsSimulationLaunch)
            {
                LaunchSelectedEditorScenario();
            }
        }
    }

    private void HandleEditorTemplateInventoryBrushInput(Keyboard keyboard)
    {
        if (_editorContext is null)
        {
            return;
        }

        if (keyboard.IsKeyReleased(Keys.Up))
        {
            var result = _editorContext.MoveTemplateInventoryBrushCursor(0, -1);
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.Down))
        {
            var result = _editorContext.MoveTemplateInventoryBrushCursor(0, 1);
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.Left))
        {
            var result = _editorContext.MoveTemplateInventoryBrushCursor(-1, 0);
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.Right))
        {
            var result = _editorContext.MoveTemplateInventoryBrushCursor(1, 0);
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.Tab) || keyboard.IsKeyReleased(Keys.E))
        {
            var result = _editorContext.CycleTemplateInventoryBrush();
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.Enter))
        {
            var result = _editorContext.PlaceTemplateInventoryBrush();
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.B))
        {
            var result = _editorContext.ToggleTemplateInventoryBrush();
            _message = result.Message;
        }
    }

    private void HandleEditorActionPlanStepInput(Keyboard keyboard)
    {
        if (_editorContext is null)
        {
            return;
        }

        if (keyboard.IsKeyReleased(Keys.Up))
        {
            _editorContext.MoveSelection(-1);
            _message = "Action-plan step editor moved. R replaces existing row; I inserts at selected position; Esc exits.";
        }
        else if (keyboard.IsKeyReleased(Keys.Down))
        {
            _editorContext.MoveSelection(1);
            _message = "Action-plan step editor moved. R replaces existing row; I inserts at selected position; Esc exits.";
        }
        else if (keyboard.IsKeyReleased(Keys.Tab) || keyboard.IsKeyReleased(Keys.Right))
        {
            var result = _editorContext.CycleActionStepEditorAvailable(1);
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.Left))
        {
            var result = _editorContext.CycleActionStepEditorAvailable(-1);
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.R))
        {
            var result = _editorContext.ReplaceSelectedActionPlanStep();
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.I))
        {
            var result = _editorContext.InsertSelectedActionPlanStep();
            _message = result.Message;
        }
    }

    private void HandleEditorTemplateTargetingRuleInput(Keyboard keyboard)
    {
        if (_editorContext is null)
        {
            return;
        }

        if (_editorContext.IsEditingTargetingRuleLabel)
        {
            if (keyboard.IsKeyReleased(Keys.Enter))
            {
                var result = _editorContext.ConfirmTargetingRuleLabelEdit();
                _message = result.Message;
                return;
            }

            if (keyboard.IsKeyReleased(Keys.Back))
            {
                var result = _editorContext.BackspaceTargetingRuleLabelText();
                _message = result.Message;
                return;
            }

            var typed = ReadTypedCharacters(keyboard);
            if (typed.Length > 0)
            {
                var result = _editorContext.TypeTargetingRuleLabelText(typed);
                _message = result.Message;
            }

            return;
        }

        if (keyboard.IsKeyReleased(Keys.Up))
        {
            _editorContext.MoveSelection(-1);
            _message = "Targeting rule editor moved to previous slot. Left/Right field; Enter activates focused field; Esc exits.";
        }
        else if (keyboard.IsKeyReleased(Keys.Down))
        {
            _editorContext.MoveSelection(1);
            _message = "Targeting rule editor moved to next slot. Left/Right field; Enter activates focused field; Esc exits.";
        }
        else if (keyboard.IsKeyReleased(Keys.Left))
        {
            var result = _editorContext.MoveTargetingRuleField(-1);
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.Right))
        {
            var result = _editorContext.MoveTargetingRuleField(1);
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.L))
        {
            var result = _editorContext.BeginTargetingRuleLabelEdit();
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.E))
        {
            var result = _editorContext.CycleTargetingRuleTarget();
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.Enter))
        {
            var result = _editorContext.ActivateTargetingRuleField();
            _message = result.Message;
        }
        else if (keyboard.IsKeyReleased(Keys.X) || keyboard.IsKeyReleased(Keys.Delete) || keyboard.IsKeyReleased(Keys.Back))
        {
            var result = _editorContext.ClearTemplateTargetingRuleSlot();
            _message = result.Message;
        }
        else
        {
            var typed = ReadTypedCharacters(keyboard);
            if (typed.Contains('+') || typed.Contains(']'))
            {
                var result = _editorContext.AdjustTargetingRuleRange(1);
                _message = result.Message;
            }
            else if (typed.Contains('-') || typed.Contains('['))
            {
                var result = _editorContext.AdjustTargetingRuleRange(-1);
                _message = result.Message;
            }
        }
    }

    private void HandleEditorTemplateDefaultActionPlanPickerInput(Keyboard keyboard)
    {
        if (_editorContext is null)
        {
            return;
        }

        if (keyboard.IsKeyReleased(Keys.Up))
        {
            _editorContext.MoveSelection(-1);
            _message = "Default action plan picker moved. Enter applies; Esc cancels.";
        }
        else if (keyboard.IsKeyReleased(Keys.Down))
        {
            _editorContext.MoveSelection(1);
            _message = "Default action plan picker moved. Enter applies; Esc cancels.";
        }
        else if (keyboard.IsKeyReleased(Keys.Enter))
        {
            var result = _editorContext.ConfirmTemplateDefaultActionPlanPicker();
            _message = result.Message;
        }
    }

    private void HandleEditorTemplateInitialFacingPickerInput(Keyboard keyboard)
    {
        if (_editorContext is null)
        {
            return;
        }

        if (keyboard.IsKeyReleased(Keys.Up))
        {
            _editorContext.MoveSelection(-1);
            _message = "Initial facing picker moved. Enter applies; Esc cancels.";
        }
        else if (keyboard.IsKeyReleased(Keys.Down))
        {
            _editorContext.MoveSelection(1);
            _message = "Initial facing picker moved. Enter applies; Esc cancels.";
        }
        else if (keyboard.IsKeyReleased(Keys.Enter))
        {
            var result = _editorContext.ConfirmTemplateInitialFacingPicker();
            _message = result.Message;
        }
    }

    private void HandleEditorTemplateTextEditInput(Keyboard keyboard)
    {
        if (_editorContext is null)
        {
            return;
        }

        if (keyboard.IsKeyReleased(Keys.Enter))
        {
            var result = _editorContext.ConfirmEdit();
            _message = result.Message;
            return;
        }

        if (keyboard.IsKeyReleased(Keys.Back))
        {
            var result = _editorContext.BackspaceEditText();
            _message = result.Message;
            return;
        }

        var typed = ReadTypedCharacters(keyboard);
        if (typed.Length > 0)
        {
            var result = _editorContext.TypeEditText(typed);
            _message = result.Message;
        }
    }

    private void HandleSessionInput(Keyboard keyboard)
    {
        if (_session is null || _commands is null || _history is null)
        {
            return;
        }

        switch (_mode)
        {
            case ShellMode.Play:
                HandlePlayInput(keyboard);
                break;
            case ShellMode.InspectSource:
                HandleInspectSourceInput(keyboard);
                break;
            case ShellMode.PickupSource:
                HandlePickupSourceInput(keyboard);
                break;
            case ShellMode.PickupDestination:
                HandlePickupDestinationInput(keyboard);
                break;
            case ShellMode.DropSource:
                HandleDropSourceInput(keyboard);
                break;
            case ShellMode.DropDestination:
                HandleDropDestinationInput(keyboard);
                break;
            case ShellMode.EnterSource:
                HandleEnterSourceInput(keyboard);
                break;
            case ShellMode.ExitDirection:
                HandleExitDirectionInput(keyboard);
                break;
        }
    }

    private void HandlePlayInput(Keyboard keyboard)
    {
        var direction = ReadDirection(keyboard);
        if (direction is { } moveDirection)
        {
            Execute(ControlledActorCommand.Move(moveDirection), "Player acted.");
            return;
        }

        if (keyboard.IsKeyReleased(Keys.Space))
        {
            Execute(ControlledActorCommand.Wait(), "Player waited.");
        }
        else if (keyboard.IsKeyReleased(Keys.I))
        {
            _worldCursor = PlayerLocation().Coord;
            _mode = ShellMode.InspectSource;
            MoveWorldCursorToFirstValid(InspectCandidates(PlayerLocation().PlaneId));
            _message = "Inspect mode: Tab cycles visible entities, arrows move cursor, Enter inspects.";
        }
        else if (keyboard.IsKeyReleased(Keys.U))
        {
            UndoPreviousFrame();
        }
        else if (keyboard.IsKeyReleased(Keys.P))
        {
            _worldCursor = PlayerLocation().Coord;
            MoveWorldCursorToFirstValid(_affordances.Query(_session!.World, _session.PlayerEntityId).PickupSources.Select(source => source.Source));
            _mode = ShellMode.PickupSource;
            _message = "Pickup mode: Tab cycles valid sources, arrows move cursor, Enter selects.";
        }
        else if (keyboard.IsKeyReleased(Keys.D))
        {
            _inventoryCursor = new GridCoord(0, 0);
            _inspectedEntity = _session!.PlayerEntityId;
            MoveInventoryCursorToFirstValid(_affordances.Query(_session.World, _session.PlayerEntityId).DropSources.Select(source => source.Source));
            _mode = ShellMode.DropSource;
            _message = "Drop mode: Tab cycles carried items, arrows move cursor, Enter selects.";
        }
        else if (keyboard.IsKeyReleased(Keys.E))
        {
            _worldCursor = PlayerLocation().Coord;
            MoveWorldCursorToFirstValid(_affordances.Query(_session!.World, _session.PlayerEntityId).EnterTargets.Select(source => source.Source));
            _mode = ShellMode.EnterSource;
            _message = "Enter mode: Tab cycles valid targets, arrows move cursor, Enter enters.";
        }
        else if (keyboard.IsKeyReleased(Keys.X))
        {
            var exits = _affordances.Query(_session!.World, _session.PlayerEntityId).ExitDirections;
            _selectedExitDirection = exits.FirstOrDefault(exit => exit.CanExecute)?.Direction;
            MoveWorldCursorToExitDestination(exits, _selectedExitDirection);
            _mode = ShellMode.ExitDirection;
            _message = "Exit mode: Tab cycles valid exits, Enter exits, arrows still choose a direction.";
        }
    }

    private void HandleInspectSourceInput(Keyboard keyboard)
    {
        var planeId = _session!.World.GetInventoryPlaneId(CurrentContainerEntityId());
        if (planeId is null)
        {
            _mode = ShellMode.Play;
            _message = "Current container has no inspectable inventory.";
            return;
        }

        if (CycleWorldCursor(keyboard, InspectCandidates(planeId.Value), planeId.Value, "Inspect target")) return;

        MoveCursor(keyboard, planeId.Value, ref _worldCursor);
        if (!keyboard.IsKeyReleased(Keys.Enter)) return;

        var target = _session.World.GetOccupant(new PlaneCoord(planeId.Value, _worldCursor));
        if (target is null)
        {
            _message = "No entity at that cell.";
            return;
        }

        _inspectedEntity = target;
        _mode = ShellMode.Play;
        _message = $"Inspecting {_session.World.Entities[target.Value].Name}.";
    }

    private void HandlePickupSourceInput(Keyboard keyboard)
    {
        var playerPlaneId = PlayerLocation().PlaneId;
        var affordances = _affordances.Query(_session!.World, _session.PlayerEntityId);
        if (CycleWorldCursor(keyboard, affordances.PickupSources.Where(source => source.CanExecute).Select(source => source.Source), playerPlaneId, "Pickup source")) return;

        MoveCursor(keyboard, playerPlaneId, ref _worldCursor);
        if (!keyboard.IsKeyReleased(Keys.Enter)) return;

        var target = _session!.World.GetOccupant(new PlaneCoord(playerPlaneId, _worldCursor));
        if (target is null || target == _session.PlayerEntityId)
        {
            _message = "No pickup target at that cell.";
            return;
        }

        _selectedEntity = target;
        _inventoryCursor = new GridCoord(0, 0);
        _inspectedEntity = _session.PlayerEntityId;
        MoveInventoryCursorToFirstValid(affordances.PickupDestinations(target.Value).Where(destination => destination.CanExecute).Select(destination => (PlaneCoord?)destination.Destination));
        _mode = ShellMode.PickupDestination;
        _message = $"Choose inventory destination for {_session.World.Entities[target.Value].Name}. Tab cycles valid cells.";
    }

    private void HandlePickupDestinationInput(Keyboard keyboard)
    {
        var inventoryPlaneId = _session!.World.GetInventoryPlaneId(_session.PlayerEntityId);
        if (inventoryPlaneId is null)
        {
            _mode = ShellMode.Play;
            _message = "Player has no inventory.";
            return;
        }

        if (_selectedEntity is { } pickupTarget && CycleInventoryCursor(keyboard, _affordances.Query(_session.World, _session.PlayerEntityId).PickupDestinations(pickupTarget).Where(destination => destination.CanExecute).Select(destination => (PlaneCoord?)destination.Destination), inventoryPlaneId.Value, "Pickup destination")) return;

        MoveCursor(keyboard, inventoryPlaneId.Value, ref _inventoryCursor);
        if (!keyboard.IsKeyReleased(Keys.Enter) || _selectedEntity is not { } target) return;
        Execute(ControlledActorCommand.Pickup(target, new PlaneCoord(inventoryPlaneId.Value, _inventoryCursor)), "Picked up entity.");
    }

    private void HandleDropSourceInput(Keyboard keyboard)
    {
        var inventoryPlaneId = _session!.World.GetInventoryPlaneId(_session.PlayerEntityId);
        if (inventoryPlaneId is null)
        {
            _mode = ShellMode.Play;
            _message = "Player has no inventory.";
            return;
        }

        var affordances = _affordances.Query(_session!.World, _session.PlayerEntityId);
        if (CycleInventoryCursor(keyboard, affordances.DropSources.Where(source => source.CanExecute).Select(source => source.Source), inventoryPlaneId.Value, "Drop source")) return;

        MoveCursor(keyboard, inventoryPlaneId.Value, ref _inventoryCursor);
        if (!keyboard.IsKeyReleased(Keys.Enter)) return;

        var target = _session.World.GetOccupant(new PlaneCoord(inventoryPlaneId.Value, _inventoryCursor));
        if (target is null)
        {
            _message = "No carried entity at that inventory cell.";
            return;
        }

        _selectedEntity = target;
        _worldCursor = PlayerLocation().Coord;
        MoveWorldCursorToFirstValid(affordances.DropDestinations(target.Value).Where(destination => destination.CanExecute).Select(destination => (PlaneCoord?)destination.Destination));
        _mode = ShellMode.DropDestination;
        _message = $"Choose world destination for {_session.World.Entities[target.Value].Name}. Tab cycles valid cells.";
    }

    private void HandleDropDestinationInput(Keyboard keyboard)
    {
        var playerPlaneId = PlayerLocation().PlaneId;
        if (_selectedEntity is { } dropTarget && CycleWorldCursor(keyboard, _affordances.Query(_session!.World, _session.PlayerEntityId).DropDestinations(dropTarget).Where(destination => destination.CanExecute).Select(destination => (PlaneCoord?)destination.Destination), playerPlaneId, "Drop destination")) return;

        MoveCursor(keyboard, playerPlaneId, ref _worldCursor);
        if (!keyboard.IsKeyReleased(Keys.Enter) || _selectedEntity is not { } target) return;
        Execute(ControlledActorCommand.Drop(target, new PlaneCoord(playerPlaneId, _worldCursor)), "Dropped entity.");
    }

    private void HandleEnterSourceInput(Keyboard keyboard)
    {
        var playerPlaneId = PlayerLocation().PlaneId;
        var affordances = _affordances.Query(_session!.World, _session.PlayerEntityId);
        if (CycleWorldCursor(keyboard, affordances.EnterTargets.Where(source => source.CanExecute).Select(source => source.Source), playerPlaneId, "Enter target")) return;

        MoveCursor(keyboard, playerPlaneId, ref _worldCursor);
        if (!keyboard.IsKeyReleased(Keys.Enter)) return;

        var target = _session!.World.GetOccupant(new PlaneCoord(playerPlaneId, _worldCursor));
        if (target is null || target == _session.PlayerEntityId)
        {
            _message = "No enter target at that cell.";
            return;
        }

        Execute(ControlledActorCommand.Enter(target.Value), "Entered entity.");
        _inspectedEntity = _session.PlayerEntityId;
    }

    private void HandleExitDirectionInput(Keyboard keyboard)
    {
        var exits = _affordances.Query(_session!.World, _session.PlayerEntityId).ExitDirections;
        if (keyboard.IsKeyReleased(Keys.Tab))
        {
            CycleExitDirection(exits);
            return;
        }

        if (keyboard.IsKeyReleased(Keys.Enter) && _selectedExitDirection is { } selectedExit)
        {
            Execute(ControlledActorCommand.Exit(selectedExit), "Exited entity.");
            _inspectedEntity = _session.PlayerEntityId;
            _selectedExitDirection = null;
            return;
        }

        var direction = ReadDirection(keyboard);
        if (direction is { } exitDirection)
        {
            Execute(ControlledActorCommand.Exit(exitDirection), "Exited entity.");
            _inspectedEntity = _session!.PlayerEntityId;
            _selectedExitDirection = null;
        }
    }

    private void Execute(ControlledActorCommand command, string successMessage)
    {
        var result = _history!.SubmitControlledCommand(_commands!, command);
        _actionLog = ActionLogProjection.FromHistory(_history);
        _mode = ShellMode.Play;
        _selectedEntity = null;
        _selectedExitDirection = null;
        _worldCursor = PlayerLocation().Coord;
        _message = result.Succeeded ? successMessage : FormatFailure(result);
    }

    private void UndoPreviousFrame()
    {
        if (_history is null || _session is null)
        {
            return;
        }

        if (!_history.RollbackPreviousFrame())
        {
            _message = "Nothing to undo.";
            return;
        }

        _actionLog = ActionLogProjection.FromHistory(_history);
        _mode = ShellMode.Play;
        _selectedEntity = null;
        _selectedExitDirection = null;
        _inspectedEntity = _session.PlayerEntityId;
        _worldCursor = PlayerLocation().Coord;
        _inventoryCursor = new GridCoord(0, 0);
        _message = "Undid previous frame.";
    }

    private void StartSession(PlayableScenarioSession session)
    {
        _session = session;
        _commands = new ControlledActorCommandService(_movement, session.ActionPlans, (world, entityId) => TargetingService.RefreshTargets(world, session.Registry, entityId));
        _history = SimulationHistorySession.Start(
            session.World,
            session.PlayerEntityId,
            session.ActivePlaneId,
            session.ActiveContainerEntityId);
        _mode = ShellMode.Play;
        _selectedEntity = null;
        _selectedExitDirection = null;
        _inspectedEntity = session.PlayerEntityId;
        _worldCursor = PlayerLocation().Coord;
        _inventoryCursor = new GridCoord(0, 0);
        _actionLog = ActionLogProjection.FromHistory(_history);
        _message = session.ValidationDiagnostics.Count == 0 && session.RuntimeFailures.Count == 0
            ? $"Scenario {session.ScenarioId}. Arrows move. Space wait. I inspect. P pickup. D drop. E enter. X exit. U undo (unavailable at frame 0). Esc returns."
            : $"Scenario {session.ScenarioId} diagnostics: {string.Join(" | ", session.ValidationDiagnostics.Concat(session.RuntimeFailures))}";
    }

    private void OpenEditorContext(string contentPath, string? scenarioId, bool launchSimulation)
    {
        try
        {
            var result = SadConsoleEditorContext.Open(contentPath, scenarioId);
            if (!result.IsSuccess || result.Context is null)
            {
                _message = result.ErrorMessage ?? $"Could not open content file {contentPath}.";
                return;
            }

            _editorContext = result.Context;
            _mode = ShellMode.Editor;
            _session = null;
            _commands = null;
            _history = null;
            _actionLog = null;
            _message = launchSimulation
                ? $"Opened authored content for {scenarioId}; launching derived Simulation."
                : "Editor context opened. Template presentation edits are available; press P to materialize Preview manually.";

            if (launchSimulation)
            {
                LaunchSelectedEditorScenario();
            }
        }
        catch (Exception ex)
        {
            _message = $"Could not open editor context for {contentPath}: {ex.Message}";
        }
    }

    private void LaunchSelectedEditorScenario()
    {
        if (_editorContext is null)
        {
            _message = "No editor context is open.";
            return;
        }

        try
        {
            var preview = _editorContext.MaterializeSelectedScenarioForSimulation();
            if (preview is null)
            {
                _message = "No authored scenario is selected.";
                return;
            }

            StartSession(preview.Session);
            _message = preview.CanPlay
                ? $"Simulation mode: derived runtime session for {preview.ScenarioId}. Esc returns to Editor; runtime state is not written back."
                : $"Derived Simulation for {preview.ScenarioId} has diagnostics: {string.Join(" | ", preview.ValidationDiagnostics.Concat(preview.RuntimeFailures).Concat(preview.CapabilityGaps))}";
        }
        catch (Exception ex)
        {
            _message = $"Could not launch selected authored scenario: {ex.Message}";
        }
    }

    private void ReturnToMenuOrExit()
    {
        if (_catalog is null)
        {
            SadConsole.Game.Instance.MonoGameInstance.Exit();
            return;
        }

        _session = null;
        _commands = null;
        _history = null;
        _actionLog = null;
        _mode = ShellMode.Menu;
        _message = "Returned to scenario list. Enter launches. Esc quits.";
    }

    private void ReturnToEditorMenuOrExit()
    {
        if (_editorContext is not null)
        {
            _session = null;
            _commands = null;
            _history = null;
            _actionLog = null;
            _mode = ShellMode.Editor;
            _message = "Returned to Editor context. Selected authored scenario was preserved; runtime state was discarded.";
            return;
        }

        ReturnToMenuOrExit();
    }

    private void Redraw()
    {
        ClearSurface();
        if (_mode == ShellMode.Menu)
        {
            DrawMenu();
        }
        else if (_mode == ShellMode.Editor)
        {
            DrawEditor();
        }
        else
        {
            DrawSession();
        }
        Surface.IsDirty = true;
    }

    private void DrawMenu()
    {
        PrintText(1, 0, "GameGameGame SadConsole debug/editor browser", Color.Yellow);
        PrintClipped(1, 1, Width - 2, _message, Color.White);
        PrintClipped(1, 2, Width - 2, "Enter: Play Scenario from Catalog (opens backing Editor context first). O: Open selected content file in Editor. Esc quits.", Color.DarkGray);

        if (_catalog is null)
        {
            return;
        }

        var maxEntries = Height - 6;
        var first = Math.Max(0, Math.Min(_selectedScenarioIndex - maxEntries / 2, Math.Max(0, _catalog.Entries.Count - maxEntries)));
        for (var index = first; index < _catalog.Entries.Count && index < first + maxEntries; index++)
        {
            var entry = _catalog.Entries[index];
            var y = 5 + index - first;
            var selected = index == _selectedScenarioIndex;
            PrintClipped(2, y, Width - 4, $"{(selected ? '>' : ' ')} {entry.Name} ({entry.ScenarioId}) - {entry.ContentPath}", selected ? Color.Yellow : Color.White);
            if (!string.IsNullOrWhiteSpace(entry.Description) && y + 1 < Height)
            {
                PrintClipped(6, ++y, Width - 8, entry.Description, Color.Gray);
            }
        }

        var diagnosticY = Height - Math.Min(3, _catalog.Diagnostics.Count);
        foreach (var diagnostic in _catalog.Diagnostics.Take(3))
        {
            PrintClipped(1, diagnosticY++, Width - 2, $"Catalog diagnostic: {diagnostic}", Color.Orange);
        }
    }

    private void DrawEditor()
    {
        if (_editorContext is null)
        {
            return;
        }

        var view = SadConsoleEditorViewBuilder.Build(_editorContext, _message);
        PrintText(1, 0, view.Header, Color.Yellow);
        PrintClipped(1, 1, Width - 2, view.Message, Color.White);
        PrintClipped(1, 3, Width - 2, view.FileLine, Color.Cyan);
        PrintClipped(1, 4, Width - 2, view.DirtyLine, Color.Gray);
        PrintClipped(1, 5, Width - 2, view.CountLine, Color.Gray);
        PrintClipped(1, 6, Width - 2, view.SelectedScenarioLine, Color.White);
        PrintClipped(1, 7, Width - 2, view.PromptHint, Color.DarkGray);
        PrintClipped(1, 8, Width - 2, view.SectionLine, Color.Cyan);

        PrintText(1, 10, "Authored scenarios (quick launch list)", Color.Yellow);
        var y = 11;
        foreach (var row in view.ScenarioRows)
        {
            PrintClipped(2, y++, Width - 4, row, row.StartsWith('>') ? Color.Yellow : Color.White);
        }

        y = 26;
        PrintText(1, y++, view.DetailHeader, Color.Yellow);
        foreach (var row in view.DetailRows)
        {
            PrintClipped(2, y++, Width - 4, row, row.StartsWith('>') ? Color.Yellow : Color.White);
        }

        y = Math.Max(y + 1, 45);
        if (y < Height)
        {
            PrintText(1, y++, "Editor-service diagnostics", Color.Yellow);
            foreach (var row in view.DiagnosticRows)
            {
                if (y >= Height)
                {
                    break;
                }

                PrintClipped(2, y++, Width - 4, row, row.StartsWith("Error", StringComparison.OrdinalIgnoreCase) ? Color.Orange : Color.Gray);
            }
        }
    }

    private void DrawSession()
    {
        if (_session is null)
        {
            return;
        }

        var view = BuildSessionView();
        PrintText(1, 0, view.Header, Color.Yellow);
        PrintClipped(1, 1, Width - 2, view.Message, Color.White);
        PrintClipped(1, 2, Width - 2, view.AffordanceSummary, Color.Gray);
        PrintClipped(1, 3, Width - 2, view.SelectedSummary, Color.Gray);
        PrintClipped(1, 4, Width - 2, view.PromptHint, Color.DarkGray);

        foreach (var panel in view.Panels)
        {
            DrawPanel(panel, view.Affordances);
        }

        DrawGlobalLog(view.GlobalLog);
    }

    private SadConsoleSessionView BuildSessionView()
    {
        return _sessionViewBuilder.Build(
            _session!,
            new SadConsoleSessionViewBuilderState(
                _mode,
                _message,
                _selectedEntity,
                _inspectedEntity,
                _worldCursor,
                _inventoryCursor,
                _actionLog,
                _history?.CanRollback ?? false));
    }

    private void DrawPanel(SadConsolePanelView view, ControlledActorAffordances affordances)
    {
        if (view.IsCollapsed)
        {
            DrawCollapsedPanel(view);
            return;
        }

        DrawPanel(view.Projection, view.Bounds, view.Title, view.Cursor, affordances);
    }

    private void DrawCollapsedPanel(SadConsolePanelView view)
    {
        var panel = view.Projection;
        var bounds = view.Bounds;
        PrintClipped(bounds.Left, bounds.Top, bounds.Width, view.Title, Color.Yellow);
        PrintClipped(bounds.Left, bounds.Top + 1, bounds.Width, $"{panel.Glyph} {panel.Name}", Color.White);
        PrintClipped(bounds.Left, bounds.Top + 2, bounds.Width, panel.EntityId.Value, Color.Gray);
        PrintClipped(bounds.Left, bounds.Top + 3, bounds.Width, "collapsed", Color.DarkGray);
    }

    private void DrawPanel(
        EntityPanelProjection panel,
        SadConsoleRect bounds,
        string title,
        GridCoord? cursor,
        ControlledActorAffordances affordances)
    {
        var left = bounds.Left;
        var top = bounds.Top;
        var width = bounds.Width;
        var bottom = bounds.Bottom;

        PrintClipped(left, top, width, $"{title}: {panel.Glyph} {panel.Name} {panel.EntityId}", Color.Yellow);
        PrintClipped(left, top + 1, width, $"Path: {FormatBreadcrumb(panel.Breadcrumb)}", Color.Gray);
        PrintClipped(left, top + 2, width, $"Location: {panel.Location} | Facing: {panel.ActionState.Facing?.ToString() ?? "none"} | Target: {panel.ActionState.Target?.ToString() ?? "none"}", Color.Gray);

        var y = top + 3;
        foreach (var property in panel.Properties.Take(4))
        {
            PrintClipped(left, y++, width, $"{property.Name}: {property.Value}", Color.White);
        }

        if (panel.ActionPlanSummary is { } actionPlan)
        {
            PrintClipped(left, y++, width, $"Plan: {actionPlan}", Color.White);
        }

        if (panel.InventoryGrid is not { } grid)
        {
            PrintClipped(left, y, width, "Inventory: none", Color.Gray);
            return;
        }

        PrintClipped(left, y++, width, $"Inventory: {grid.PlaneId} ({grid.Width}x{grid.Height})", Color.White);
        var highlights = BuildHighlights(grid.PlaneId, affordances);
        for (var row = 0; row < grid.Height && y < bottom - 8; row++, y++)
        {
            for (var x = 0; x < grid.Width && x < width - 1; x++)
            {
                var coord = new GridCoord(x, row);
                var cell = grid.Cells.Single(cell => cell.Coord == coord);
                var occupant = cell.EntityId;
                var foreground = ToSadColor(cell.Color);
                var background = BackgroundForCell(grid.PlaneId, coord, occupant, cursor, highlights);
                SetCell(left + x, y, cell.Glyph, foreground, background);
            }
        }

        if (y >= bottom)
        {
            return;
        }

        DrawLocalActivity(panel, left, width, bottom, ref y);
    }

    private void DrawLocalActivity(EntityPanelProjection panel, int left, int width, int bottom, ref int y)
    {
        foreach (var row in LocalActivityViewBuilder.Build(panel, bottom - y))
        {
            var isDetailRow = IsLocalActivityDetailRow(row.Text);
            var x = row.IsHeader ? left : isDetailRow ? left + 2 : left;
            var rowWidth = isDetailRow ? Math.Max(0, width - 2) : width;
            var text = row.Text;
            if (!row.IsHeader && !isDetailRow)
            {
                var content = panel.Contents.FirstOrDefault(contentRow => row.Text.StartsWith($"{contentRow.Order}. {contentRow.Glyph} {contentRow.EntityName}"));
                if (content is not null)
                {
                    text = $"{content.Order}. {content.Glyph} {content.EntityName}{FormatEntityStateSuffix(content.EntityId)} [{content.Participation}]";
                }
            }

            var color = row.IsHeader ? Color.Yellow : row.IsPositive ? Color.LightGreen : row.IsWarning ? Color.Orange : row.IsMuted ? Color.DarkGray : Color.White;
            PrintClipped(x, y++, rowWidth, text, color);
        }
    }

    private static bool IsLocalActivityDetailRow(string text) =>
        text.StartsWith('└') || text.StartsWith('├');

    private void DrawGlobalLog(SadConsoleLogView log)
    {
        PrintText(log.Bounds.Left, log.Bounds.Top, log.Title, Color.Yellow);
        if (log.Rows.Count == 0)
        {
            PrintClipped(log.Bounds.Left, log.Bounds.Top + 1, log.Bounds.Width, log.EmptyText, Color.DarkGray);
            return;
        }

        var y = log.Bounds.Top + 1;
        foreach (var outcome in log.Rows.TakeLast(log.Bounds.Height - 1))
        {
            PrintClipped(log.Bounds.Left, y++, log.Bounds.Width, ActionOutcomeTextFormatter.FormatGlobal(outcome), outcome.Succeeded ? Color.LightGreen : Color.Orange);
        }
    }

    private IReadOnlyDictionary<GridCoord, CellHighlight> BuildHighlights(PlaneId planeId, ControlledActorAffordances affordances)
    {
        var highlights = new Dictionary<GridCoord, CellHighlight>();

        switch (_mode)
        {
            case ShellMode.Play:
                foreach (var movement in affordances.MovementDirections.Where(affordance => affordance.Destination?.PlaneId == planeId))
                {
                    AddHighlight(highlights, movement.Destination!.Value.Coord, movement.CanExecute ? CellHighlight.Valid : CellHighlight.Invalid);
                }

                foreach (var source in affordances.PickupSources.Concat(affordances.EnterTargets).Where(affordance => affordance.Source?.PlaneId == planeId && affordance.CanExecute))
                {
                    AddHighlight(highlights, source.Source!.Value.Coord, CellHighlight.Valid);
                }
                break;

            case ShellMode.InspectSource:
                foreach (var coord in InspectCandidates(planeId).Where(candidate => candidate is not null).Select(candidate => candidate!.Value.Coord))
                {
                    AddHighlight(highlights, coord, CellHighlight.Valid);
                }
                break;

            case ShellMode.PickupSource:
                foreach (var source in affordances.PickupSources.Where(affordance => affordance.Source?.PlaneId == planeId))
                {
                    AddHighlight(highlights, source.Source!.Value.Coord, source.CanExecute ? CellHighlight.Valid : CellHighlight.Invalid);
                }
                break;

            case ShellMode.PickupDestination when _selectedEntity is { } pickupTarget:
                foreach (var destination in affordances.PickupDestinations(pickupTarget).Where(affordance => affordance.Destination.PlaneId == planeId))
                {
                    AddHighlight(highlights, destination.Destination.Coord, destination.CanExecute ? CellHighlight.Valid : CellHighlight.Invalid);
                }
                break;

            case ShellMode.DropSource:
                foreach (var source in affordances.DropSources.Where(affordance => affordance.Source?.PlaneId == planeId))
                {
                    AddHighlight(highlights, source.Source!.Value.Coord, source.CanExecute ? CellHighlight.Valid : CellHighlight.Invalid);
                }
                break;

            case ShellMode.DropDestination when _selectedEntity is { } dropTarget:
                foreach (var destination in affordances.DropDestinations(dropTarget).Where(affordance => affordance.Destination.PlaneId == planeId))
                {
                    AddHighlight(highlights, destination.Destination.Coord, destination.CanExecute ? CellHighlight.Valid : CellHighlight.Invalid);
                }
                break;

            case ShellMode.EnterSource:
                foreach (var source in affordances.EnterTargets.Where(affordance => affordance.Source?.PlaneId == planeId))
                {
                    AddHighlight(highlights, source.Source!.Value.Coord, source.CanExecute ? CellHighlight.Valid : CellHighlight.Invalid);
                }
                break;

            case ShellMode.ExitDirection:
                foreach (var exit in affordances.ExitDirections.Where(affordance => affordance.Destination?.PlaneId == planeId))
                {
                    AddHighlight(highlights, exit.Destination!.Value.Coord, exit.CanExecute ? CellHighlight.Valid : CellHighlight.Invalid);
                }
                break;
        }

        return highlights;
    }

    private static void AddHighlight(Dictionary<GridCoord, CellHighlight> highlights, GridCoord coord, CellHighlight highlight)
    {
        if (!highlights.TryGetValue(coord, out var existing) || existing != CellHighlight.Valid)
        {
            highlights[coord] = highlight;
        }
    }

    private Color BackgroundForCell(
        PlaneId planeId,
        GridCoord coord,
        EntityId? occupant,
        GridCoord? cursor,
        IReadOnlyDictionary<GridCoord, CellHighlight> highlights)
    {
        if (cursor == coord)
        {
            return Color.DarkGoldenrod;
        }

        if (occupant == _session?.PlayerEntityId)
        {
            return Color.DarkBlue;
        }

        if (occupant is { } entityId && entityId == _selectedEntity)
        {
            return Color.DarkMagenta;
        }

        if (occupant is { } targetId && _session?.World.GetActionTarget(_session.PlayerEntityId) == targetId)
        {
            return Color.Purple;
        }

        if (highlights.TryGetValue(coord, out var highlight))
        {
            return highlight == CellHighlight.Valid ? Color.DarkGreen : Color.DarkRed;
        }

        return Color.Black;
    }

    private string FormatEntityStateSuffix(EntityId entityId)
    {
        if (_session is null)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        if (_session.World.GetActionFacing(entityId) is { } facing)
        {
            parts.Add($"F:{FacingArrow(facing)}");
        }

        if (_session.World.GetActionTarget(entityId) is { } target)
        {
            parts.Add($"T:{FormatEntityShortName(target)}");
        }

        return parts.Count == 0 ? string.Empty : $" ({string.Join(' ', parts)})";
    }

    private string FormatEntityShortName(EntityId entityId) =>
        _session is { } session && session.World.Entities.TryGetValue(entityId, out var entity)
            ? entity.Name
            : entityId.Value;

    private static string FacingArrow(GggDirection direction) => direction switch
    {
        GggDirection.North => "N↑",
        GggDirection.South => "S↓",
        GggDirection.West => "W←",
        GggDirection.East => "E→",
        _ => direction.ToString()
    };

    private EntityId CurrentContainerEntityId()
    {
        var playerPlaneId = PlayerLocation().PlaneId;
        return InventoryPlaneOwnership.TryFindOwner(_session!.World, playerPlaneId, out var containerId)
            ? containerId
            : _session.PlayerEntityId;
    }

    private PlaneCoord PlayerLocation() => _session!.World.GetEntityLocation(_session.PlayerEntityId);

    private IReadOnlyList<PlaneCoord?> InspectCandidates(PlaneId planeId)
    {
        if (_session is null)
        {
            return [];
        }

        return _session.World.Occupancy
            .Where(entry => _session.World.Nodes.TryGetValue(entry.Key, out var node) && node.PlaneId == planeId)
            .Select(entry => (PlaneCoord?)_session.World.GetEntityLocation(entry.Value))
            .OrderBy(coord => coord!.Value.Coord.Y)
            .ThenBy(coord => coord!.Value.Coord.X)
            .ToList();
    }

    private bool CycleWorldCursor(Keyboard keyboard, IEnumerable<PlaneCoord?> candidates, PlaneId planeId, string label)
    {
        if (!keyboard.IsKeyReleased(Keys.Tab))
        {
            return false;
        }

        return CycleCursor(candidates, planeId, ref _worldCursor, label);
    }

    private bool CycleInventoryCursor(Keyboard keyboard, IEnumerable<PlaneCoord?> candidates, PlaneId planeId, string label)
    {
        if (!keyboard.IsKeyReleased(Keys.Tab))
        {
            return false;
        }

        return CycleCursor(candidates, planeId, ref _inventoryCursor, label);
    }

    private bool CycleCursor(IEnumerable<PlaneCoord?> candidates, PlaneId planeId, ref GridCoord cursor, string label)
    {
        var result = PromptChoiceCycler.Cycle(candidates, planeId, cursor, label);
        cursor = result.Cursor;
        _message = result.Message;
        return true;
    }

    private void MoveWorldCursorToFirstValid(IEnumerable<PlaneCoord?> candidates)
    {
        if (FirstValidCoord(candidates, PlayerLocation().PlaneId) is { } coord)
        {
            _worldCursor = coord;
        }
    }

    private void MoveInventoryCursorToFirstValid(IEnumerable<PlaneCoord?> candidates)
    {
        if (_session?.World.GetInventoryPlaneId(_session.PlayerEntityId) is { } inventoryPlaneId && FirstValidCoord(candidates, inventoryPlaneId) is { } coord)
        {
            _inventoryCursor = coord;
        }
    }

    private static GridCoord? FirstValidCoord(IEnumerable<PlaneCoord?> candidates, PlaneId planeId) =>
        PromptChoiceCycler.FirstValidCoord(candidates, planeId);

    private void CycleExitDirection(IReadOnlyList<ControlledActorDirectionAffordance> exits)
    {
        var validExits = exits.Where(exit => exit.CanExecute).ToList();
        if (validExits.Count == 0)
        {
            _message = "Exit: no valid exits.";
            return;
        }

        var index = _selectedExitDirection is { } current
            ? validExits.FindIndex(exit => exit.Direction == current)
            : -1;
        var selected = validExits[(index + 1 + validExits.Count) % validExits.Count];
        _selectedExitDirection = selected.Direction;
        MoveWorldCursorToExitDestination(exits, _selectedExitDirection);
        _message = $"Exit: selected {selected.Direction}. Tab cycles, Enter exits.";
    }

    private void MoveWorldCursorToExitDestination(IReadOnlyList<ControlledActorDirectionAffordance> exits, GggDirection? direction)
    {
        if (direction is null)
        {
            return;
        }

        var destination = exits.FirstOrDefault(exit => exit.Direction == direction)?.Destination;
        if (destination?.PlaneId == PlayerLocation().PlaneId)
        {
            _worldCursor = destination.Value.Coord;
        }
    }

    private void MoveCursor(Keyboard keyboard, PlaneId planeId, ref GridCoord cursor)
    {
        if (ReadDirection(keyboard) is not { } direction || !_session!.World.Planes.TryGetValue(planeId, out var plane)) return;
        var next = cursor.Offset(direction);
        if (plane.Contains(next)) cursor = next;
    }

    private static GggDirection? ReadDirection(Keyboard keyboard) =>
        keyboard.IsKeyReleased(Keys.Up) ? GggDirection.North :
        keyboard.IsKeyReleased(Keys.Down) ? GggDirection.South :
        keyboard.IsKeyReleased(Keys.Left) ? GggDirection.West :
        keyboard.IsKeyReleased(Keys.Right) ? GggDirection.East :
        null;

    private static string ReadTypedCharacters(Keyboard keyboard)
    {
        var chars = new List<char>();
        foreach (var key in keyboard.KeysPressed)
        {
            if (key.Character != 0 && !char.IsControl(key.Character))
            {
                chars.Add(key.Character);
            }
        }

        return new string(chars.ToArray());
    }

    private void PrintClipped(int x, int y, int width, string text, Color color)
    {
        if (y < 0 || y >= Height || x >= Width) return;
        var clipped = text.Length <= width ? text : text[..Math.Max(0, width - 1)];
        PrintText(x, y, clipped.PadRight(Math.Max(0, width)), color);
    }

    private void ClearSurface()
    {
        for (var y = 0; y < ScreenHeight; y++)
        {
            for (var x = 0; x < ScreenWidth; x++)
            {
                SetCell(x, y, ' ', Color.White, Color.Black);
            }
        }
    }

    private void PrintText(int x, int y, string text, Color foreground)
    {
        for (var index = 0; index < text.Length && x + index < ScreenWidth; index++)
        {
            SetCell(x + index, y, text[index], foreground, Color.Black);
        }
    }

    private void SetCell(int x, int y, int glyph, Color foreground, Color background)
    {
        if (x < 0 || y < 0 || x >= ScreenWidth || y >= ScreenHeight)
        {
            return;
        }

        var cell = Surface[x, y];
        cell.Glyph = glyph;
        cell.Foreground = foreground;
        cell.Background = background;
        cell.IsDirty = true;
    }

    private static string FormatBreadcrumb(EntityContainmentPath path)
    {
        var text = path.Segments.Count == 0
            ? path.RequestedEntityId.ToString()
            : string.Join(" > ", path.Segments.Select(segment => segment.EntityId.Value));
        return path.Status == EntityContainmentPathStatus.Complete ? text : $"{text} [{path.Status}]";
    }

    private static string FormatFailure(ControlledActorCommandResult result) =>
        string.IsNullOrWhiteSpace(result.FailureDetail)
            ? $"Action failed: {result.FailureReason?.ToString() ?? "failed"}."
            : $"Action failed: {result.FailureReason?.ToString() ?? "failed"}. {result.FailureDetail}";

    private static Color ToSadColor(GggColor color) => color switch
    {
        GggColor.White => Color.White,
        GggColor.Yellow => Color.Yellow,
        GggColor.Cyan => Color.Cyan,
        GggColor.Green => Color.Green,
        GggColor.DarkGreen => Color.DarkGreen,
        GggColor.Earth => Color.SaddleBrown,
        GggColor.Gray => Color.Gray,
        _ => Color.White
    };
}

internal enum ShellMode
{
    Menu,
    Editor,
    Play,
    InspectSource,
    PickupSource,
    PickupDestination,
    DropSource,
    DropDestination,
    EnterSource,
    ExitDirection
}

internal enum CellHighlight
{
    Invalid,
    Valid
}

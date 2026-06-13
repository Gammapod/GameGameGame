using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using GameGameGame.Content;
using GameGameGame.Core;
using System.Collections.Specialized;
using System.ComponentModel;

namespace GameGameGame.Editor;

public sealed class MainWindow : Window
{
    private readonly TextBox _pathTextBox = new() { Watermark = "Path to content YAML" };
    private readonly StackPanel _inventoryGridRows = new() { Spacing = 4 };
    private MainEditorViewModel? _boundViewModel;

    public MainWindow()
    {
        Title = "GameGameGame Content Editor";
        Width = 1200;
        Height = 800;
        Content = BuildContent();
        DataContextChanged += (_, _) => BindInventoryGridViewModel();
    }

    private Control BuildContent()
    {
        var root = new DockPanel { Margin = new Thickness(12) };
        var toolbar = BuildToolbar();
        DockPanel.SetDock(toolbar, Dock.Top);
        root.Children.Add(toolbar);

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("260,16,*,16,*"),
            RowDefinitions = new RowDefinitions("*,180")
        };
        root.Children.Add(grid);

        var presetList = new ListBox();
        presetList.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.EntityPresets)));
        presetList.Bind(ListBox.SelectedItemProperty, new Binding(nameof(MainEditorViewModel.SelectedPreset)) { Mode = BindingMode.TwoWay });
        var presetListPanel = Wrap("Entity Presets", BuildEntityPresetListPanel(presetList));
        Grid.SetColumn(presetListPanel, 0);
        Grid.SetRowSpan(presetListPanel, 2);
        grid.Children.Add(presetListPanel);

        var editorTabs = BuildEditorTabs();
        Grid.SetColumn(editorTabs, 2);
        grid.Children.Add(editorTabs);

        var yamlPreview = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
            FontFamily = "Consolas"
        };
        yamlPreview.Bind(TextBox.TextProperty, new Binding(nameof(MainEditorViewModel.YamlPreview)));
        var yamlPreviewPanel = Wrap("YAML Preview", yamlPreview);
        Grid.SetColumn(yamlPreviewPanel, 4);
        grid.Children.Add(yamlPreviewPanel);

        var lowerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,16,*")
        };
        Grid.SetColumn(lowerGrid, 2);
        Grid.SetColumnSpan(lowerGrid, 3);
        Grid.SetRow(lowerGrid, 1);
        grid.Children.Add(lowerGrid);

        var validationList = new ListBox();
        validationList.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.ValidationMessages)));
        lowerGrid.Children.Add(Wrap("Validation", validationList));

        var diffList = new ListBox();
        diffList.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.YamlDiffLines)));
        var diffPanel = Wrap("YAML Diff", diffList);
        Grid.SetColumn(diffPanel, 2);
        lowerGrid.Children.Add(diffPanel);

        return root;
    }

    private Control BuildToolbar()
    {
        var panel = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
        var status = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };
        status.Bind(TextBlock.TextProperty, new Binding(nameof(MainEditorViewModel.StatusMessage)));
        DockPanel.SetDock(status, Dock.Right);
        panel.Children.Add(status);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var createNew = new Button { Content = "New" };
        createNew.Click += (_, _) => ViewModel?.CreateNewDocument();
        var open = new Button { Content = "Open" };
        open.Click += async (_, _) => await OpenContentFileAsync();
        var save = new Button { Content = "Save" };
        save.Click += (_, _) => ViewModel?.Save();
        var saveAs = new Button { Content = "Save As" };
        saveAs.Click += (_, _) => ViewModel?.SaveAs(_pathTextBox.Text ?? string.Empty);
        var reload = new Button { Content = "Reload" };
        reload.Click += (_, _) => ViewModel?.Reload();
        buttons.Children.Add(_pathTextBox);
        buttons.Children.Add(createNew);
        buttons.Children.Add(open);
        buttons.Children.Add(save);
        buttons.Children.Add(saveAs);
        buttons.Children.Add(reload);
        panel.Children.Add(buttons);

        return panel;
    }

    private async Task OpenContentFileAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Content YAML",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("YAML content files")
                {
                    Patterns = ["*.yaml", "*.yml"]
                },
                FilePickerFileTypes.All
            ]
        });

        var path = files.Count > 0 ? files[0].Path.LocalPath : null;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        _pathTextBox.Text = path;
        ViewModel?.OpenFile(path);
    }

    private Control BuildEntityPresetListPanel(ListBox presetList)
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(presetList);

        var nameInput = new TextBox { Watermark = "New or duplicate name" };
        nameInput.Bind(TextBox.TextProperty, new Binding(nameof(MainEditorViewModel.EntityPresetNameInput))
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        panel.Children.Add(nameInput);

        var create = new Button
        {
            Content = "Create Preset",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        create.Click += (_, _) => ViewModel?.CreateEntityPreset();
        panel.Children.Add(create);

        var duplicate = new Button
        {
            Content = "Duplicate Selected",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        duplicate.Click += (_, _) => ViewModel?.DuplicateSelectedEntityPreset();
        panel.Children.Add(duplicate);

        var delete = new Button
        {
            Content = "Delete Selected",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        delete.Click += (_, _) => ViewModel?.DeleteSelectedEntityPreset();
        panel.Children.Add(delete);

        return panel;
    }

    private Control BuildPresetForm()
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(BoundTextBox("Name", nameof(MainEditorViewModel.SelectedName)));
        panel.Children.Add(BoundNumeric("Inventory Width", nameof(MainEditorViewModel.SelectedInventoryWidth)));
        panel.Children.Add(BoundNumeric("Inventory Height", nameof(MainEditorViewModel.SelectedInventoryHeight)));
        panel.Children.Add(BoundNumeric("Weight", nameof(MainEditorViewModel.SelectedWeight)));
        panel.Children.Add(BoundNumeric("Carrying Capacity", nameof(MainEditorViewModel.SelectedCarryingCapacity)));
        panel.Children.Add(BoundTextBox("Glyph", nameof(MainEditorViewModel.SelectedGlyph)));

        var colors = new ComboBox
        {
            ItemsSource = Enum.GetValues<PresentationColor>()
        };
        colors.Bind(ComboBox.SelectedItemProperty, new Binding(nameof(MainEditorViewModel.SelectedColor)) { Mode = BindingMode.TwoWay });
        panel.Children.Add(Wrap("Color", colors));

        var apply = new Button
        {
            Content = "Apply Preset Edits",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        apply.Click += (_, _) => ViewModel?.ApplySelectedEntityPresetEdits();
        panel.Children.Add(apply);

        panel.Children.Add(BuildDefaultActionPlanEditor());

        var diagnostics = new ListBox { MinHeight = 80 };
        diagnostics.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.SelectedPresetDiagnostics)));
        panel.Children.Add(Wrap("Selected Preset Diagnostics", diagnostics));

        return Wrap("Selected Preset", panel);
    }

    private Control BuildEditorTabs() =>
        new TabControl
        {
            ItemsSource = new[]
            {
                new TabItem
                {
                    Header = "Entity",
                    Content = new ScrollViewer { Content = BuildPresetForm() }
                },
                new TabItem
                {
                    Header = "Inventory",
                    Content = new ScrollViewer { Content = BuildInventoryEditor() }
                },
                new TabItem
                {
                    Header = "Action Plans",
                    Content = new ScrollViewer { Content = BuildActionPlanBrowser() }
                },
                new TabItem
                {
                    Header = "Actor State",
                    Content = new ScrollViewer { Content = BuildActorStateEditor() }
                }
            }
        };

    private Control BuildDefaultActionPlanEditor()
    {
        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 16, 0, 0) };

        var actionPlans = new ComboBox();
        actionPlans.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.ActionPlans)));
        actionPlans.Bind(ComboBox.SelectedItemProperty, new Binding(nameof(MainEditorViewModel.SelectedDefaultActionPlan)) { Mode = BindingMode.TwoWay });
        panel.Children.Add(Wrap("Default Action Plan", actionPlans));

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var assign = new Button { Content = "Assign Plan" };
        assign.Click += (_, _) => ViewModel?.AssignSelectedDefaultActionPlan();
        var clear = new Button { Content = "Clear Plan" };
        clear.Click += (_, _) => ViewModel?.ClearSelectedDefaultActionPlan();
        buttons.Children.Add(assign);
        buttons.Children.Add(clear);
        panel.Children.Add(buttons);

        return Wrap("Action Plan Assignment", panel);
    }

    private Control BuildActionPlanBrowser()
    {
        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 16, 0, 0) };

        var nameInput = new TextBox { Watermark = "New or duplicate plan name" };
        nameInput.Bind(TextBox.TextProperty, new Binding(nameof(MainEditorViewModel.ActionPlanNameInput))
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        panel.Children.Add(nameInput);

        var planButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var create = new Button { Content = "Create Plan" };
        create.Click += (_, _) => ViewModel?.CreateActionPlan();
        var duplicate = new Button { Content = "Duplicate Selected" };
        duplicate.Click += (_, _) => ViewModel?.DuplicateSelectedActionPlan();
        var delete = new Button { Content = "Delete Selected" };
        delete.Click += (_, _) => ViewModel?.DeleteSelectedActionPlan();
        planButtons.Children.Add(create);
        planButtons.Children.Add(duplicate);
        planButtons.Children.Add(delete);
        panel.Children.Add(planButtons);

        var actionPlans = new ComboBox();
        actionPlans.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.ActionPlans)));
        actionPlans.Bind(ComboBox.SelectedItemProperty, new Binding(nameof(MainEditorViewModel.SelectedActionPlan)) { Mode = BindingMode.TwoWay });
        panel.Children.Add(Wrap("Action Plan", actionPlans));

        var planDiagnostics = new ListBox { MinHeight = 80 };
        planDiagnostics.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.SelectedActionPlanDiagnostics)));
        panel.Children.Add(Wrap("Selected Plan Diagnostics", planDiagnostics));

        var steps = new ListBox { MinHeight = 140 };
        steps.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.ActionPlanSteps)));
        steps.Bind(ListBox.SelectedItemProperty, new Binding(nameof(MainEditorViewModel.SelectedActionPlanStep)) { Mode = BindingMode.TwoWay });
        panel.Children.Add(Wrap("Steps", steps));

        var stepDiagnostics = new ListBox { MinHeight = 80 };
        stepDiagnostics.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.SelectedActionPlanStepDiagnostics)));
        panel.Children.Add(Wrap("Selected Step Diagnostics", stepDiagnostics));

        panel.Children.Add(BoundTextBox("Step Label", nameof(MainEditorViewModel.ActionPlanStepLabelInput)));

        var stepButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var applyLabel = new Button { Content = "Apply Label" };
        applyLabel.Click += (_, _) => ViewModel?.ApplySelectedActionPlanStepLabel();
        var addWait = new Button { Content = "Add Wait Step" };
        addWait.Click += (_, _) => ViewModel?.AddWaitStepToSelectedActionPlan();
        var moveUp = new Button { Content = "Move Up" };
        moveUp.Click += (_, _) => ViewModel?.MoveSelectedActionPlanStepUp();
        var moveDown = new Button { Content = "Move Down" };
        moveDown.Click += (_, _) => ViewModel?.MoveSelectedActionPlanStepDown();
        var remove = new Button { Content = "Remove Step" };
        remove.Click += (_, _) => ViewModel?.RemoveSelectedActionPlanStep();
        stepButtons.Children.Add(applyLabel);
        stepButtons.Children.Add(addWait);
        stepButtons.Children.Add(moveUp);
        stepButtons.Children.Add(moveDown);
        stepButtons.Children.Add(remove);
        panel.Children.Add(stepButtons);

        panel.Children.Add(BuildStepCheckEditor());

        panel.Children.Add(BuildStepEffectEditor());

        return Wrap("Action Plan Browser", panel);
    }

    private Control BuildStepCheckEditor()
    {
        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };

        var checks = new ListBox { MinHeight = 80 };
        checks.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.ActionPlanStepChecks)));
        checks.Bind(ListBox.SelectedItemProperty, new Binding(nameof(MainEditorViewModel.SelectedActionPlanStepCheck)) { Mode = BindingMode.TwoWay });
        panel.Children.Add(Wrap("Checks", checks));

        var kind = new ComboBox();
        kind.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.CheckKinds)));
        kind.Bind(ComboBox.SelectedItemProperty, new Binding(nameof(MainEditorViewModel.SelectedCheckKind)) { Mode = BindingMode.TwoWay });
        panel.Children.Add(Wrap("Kind", kind));

        var checkCoordX = BoundNumeric("Inventory X", nameof(MainEditorViewModel.CheckInventoryCoordX));
        checkCoordX.Bind(IsVisibleProperty, new Binding(nameof(MainEditorViewModel.IsCheckInventoryCoordVisible)));
        panel.Children.Add(checkCoordX);

        var checkCoordY = BoundNumeric("Inventory Y", nameof(MainEditorViewModel.CheckInventoryCoordY));
        checkCoordY.Bind(IsVisibleProperty, new Binding(nameof(MainEditorViewModel.IsCheckInventoryCoordVisible)));
        panel.Children.Add(checkCoordY);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var add = new Button { Content = "Add Check" };
        add.Click += (_, _) => ViewModel?.AddSelectedCheckToSelectedStep();
        var update = new Button { Content = "Update Check" };
        update.Click += (_, _) => ViewModel?.UpdateSelectedStepCheck();
        var moveUp = new Button { Content = "Move Up" };
        moveUp.Click += (_, _) => ViewModel?.MoveSelectedStepCheckUp();
        var moveDown = new Button { Content = "Move Down" };
        moveDown.Click += (_, _) => ViewModel?.MoveSelectedStepCheckDown();
        var remove = new Button { Content = "Remove Check" };
        remove.Click += (_, _) => ViewModel?.RemoveSelectedStepCheck();
        buttons.Children.Add(add);
        buttons.Children.Add(update);
        buttons.Children.Add(moveUp);
        buttons.Children.Add(moveDown);
        buttons.Children.Add(remove);
        panel.Children.Add(buttons);

        return Wrap("Selected Step Checks", panel);
    }

    private Control BuildStepEffectEditor()
    {
        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };

        panel.Children.Add(BuildEffectInputs(
            "Success Effect",
            nameof(MainEditorViewModel.SelectedSuccessEffectKind),
            nameof(MainEditorViewModel.SuccessInventoryCoordX),
            nameof(MainEditorViewModel.SuccessInventoryCoordY),
            nameof(MainEditorViewModel.SelectedSuccessCallPlan),
            nameof(MainEditorViewModel.IsSuccessInventoryCoordVisible),
            nameof(MainEditorViewModel.IsSuccessCallPlanVisible),
            nameof(MainEditorViewModel.IsSuccessMovementVisible),
            nameof(MainEditorViewModel.SelectedSuccessMovementTargetKind),
            nameof(MainEditorViewModel.SuccessMovementTargetEntityIdInput),
            nameof(MainEditorViewModel.SuccessMovementTargetCoordX),
            nameof(MainEditorViewModel.SuccessMovementTargetCoordY),
            nameof(MainEditorViewModel.SelectedSuccessMovementDestinationKind),
            nameof(MainEditorViewModel.SuccessMovementDestinationPlaneIdInput),
            nameof(MainEditorViewModel.SuccessMovementDestinationCoordX),
            nameof(MainEditorViewModel.SuccessMovementDestinationCoordY),
            nameof(MainEditorViewModel.SuccessMovementDestinationOwnerIdInput),
            nameof(MainEditorViewModel.SuccessMovementDestinationAnchorEntityIdInput),
            nameof(MainEditorViewModel.SuccessMovementDestinationDirection)));

        var setSuccess = new Button { Content = "Set Success Effect", HorizontalAlignment = HorizontalAlignment.Left };
        setSuccess.Click += (_, _) => ViewModel?.SetSelectedStepSuccessEffect();
        panel.Children.Add(setSuccess);

        panel.Children.Add(BuildEffectInputs(
            "Failure Effect",
            nameof(MainEditorViewModel.SelectedFailureEffectKind),
            nameof(MainEditorViewModel.FailureInventoryCoordX),
            nameof(MainEditorViewModel.FailureInventoryCoordY),
            nameof(MainEditorViewModel.SelectedFailureCallPlan),
            nameof(MainEditorViewModel.IsFailureInventoryCoordVisible),
            nameof(MainEditorViewModel.IsFailureCallPlanVisible),
            nameof(MainEditorViewModel.IsFailureMovementVisible),
            nameof(MainEditorViewModel.SelectedFailureMovementTargetKind),
            nameof(MainEditorViewModel.FailureMovementTargetEntityIdInput),
            nameof(MainEditorViewModel.FailureMovementTargetCoordX),
            nameof(MainEditorViewModel.FailureMovementTargetCoordY),
            nameof(MainEditorViewModel.SelectedFailureMovementDestinationKind),
            nameof(MainEditorViewModel.FailureMovementDestinationPlaneIdInput),
            nameof(MainEditorViewModel.FailureMovementDestinationCoordX),
            nameof(MainEditorViewModel.FailureMovementDestinationCoordY),
            nameof(MainEditorViewModel.FailureMovementDestinationOwnerIdInput),
            nameof(MainEditorViewModel.FailureMovementDestinationAnchorEntityIdInput),
            nameof(MainEditorViewModel.FailureMovementDestinationDirection)));

        var failureButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var setFailure = new Button { Content = "Set Failure Effect" };
        setFailure.Click += (_, _) => ViewModel?.SetSelectedStepFailureEffect();
        var clearFailure = new Button { Content = "Clear Failure Effect" };
        clearFailure.Click += (_, _) => ViewModel?.ClearSelectedStepFailureEffect();
        failureButtons.Children.Add(setFailure);
        failureButtons.Children.Add(clearFailure);
        panel.Children.Add(failureButtons);

        return Wrap("Selected Step Effects", panel);
    }

    private Control BuildEffectInputs(
        string header,
        string kindProperty,
        string coordXProperty,
        string coordYProperty,
        string callPlanProperty,
        string coordVisibleProperty,
        string callPlanVisibleProperty,
        string movementVisibleProperty,
        string movementTargetKindProperty,
        string movementTargetEntityProperty,
        string movementTargetCoordXProperty,
        string movementTargetCoordYProperty,
        string movementDestinationKindProperty,
        string movementDestinationPlaneIdProperty,
        string movementDestinationCoordXProperty,
        string movementDestinationCoordYProperty,
        string movementDestinationOwnerIdProperty,
        string movementDestinationAnchorEntityIdProperty,
        string movementDestinationDirectionProperty)
    {
        var panel = new StackPanel { Spacing = 8 };

        var kind = new ComboBox();
        kind.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.EffectKinds)));
        kind.Bind(ComboBox.SelectedItemProperty, new Binding(kindProperty) { Mode = BindingMode.TwoWay });
        panel.Children.Add(Wrap("Kind", kind));

        var coordX = BoundNumeric("Inventory X", coordXProperty);
        coordX.Bind(IsVisibleProperty, new Binding(coordVisibleProperty));
        panel.Children.Add(coordX);

        var coordY = BoundNumeric("Inventory Y", coordYProperty);
        coordY.Bind(IsVisibleProperty, new Binding(coordVisibleProperty));
        panel.Children.Add(coordY);

        var callPlan = new ComboBox();
        callPlan.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.ActionPlans)));
        callPlan.Bind(ComboBox.SelectedItemProperty, new Binding(callPlanProperty) { Mode = BindingMode.TwoWay });
        var callPlanPanel = Wrap("Call Plan", callPlan);
        callPlanPanel.Bind(IsVisibleProperty, new Binding(callPlanVisibleProperty));
        panel.Children.Add(callPlanPanel);

        var movementPanel = new StackPanel { Spacing = 8 };
        var targetKind = new ComboBox();
        targetKind.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.MovementTargetKinds)));
        targetKind.Bind(ComboBox.SelectedItemProperty, new Binding(movementTargetKindProperty) { Mode = BindingMode.TwoWay });
        movementPanel.Children.Add(Wrap("Target Kind", targetKind));
        movementPanel.Children.Add(BoundTextBox("Target Entity ID", movementTargetEntityProperty));
        movementPanel.Children.Add(BoundNumeric("Target Inventory X", movementTargetCoordXProperty));
        movementPanel.Children.Add(BoundNumeric("Target Inventory Y", movementTargetCoordYProperty));
        var destinationKind = new ComboBox();
        destinationKind.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.MovementDestinationKinds)));
        destinationKind.Bind(ComboBox.SelectedItemProperty, new Binding(movementDestinationKindProperty) { Mode = BindingMode.TwoWay });
        movementPanel.Children.Add(Wrap("Destination Kind", destinationKind));
        movementPanel.Children.Add(BoundTextBox("Destination Plane ID", movementDestinationPlaneIdProperty));
        movementPanel.Children.Add(BoundNumeric("Destination X", movementDestinationCoordXProperty));
        movementPanel.Children.Add(BoundNumeric("Destination Y", movementDestinationCoordYProperty));
        movementPanel.Children.Add(BoundTextBox("Destination Owner ID", movementDestinationOwnerIdProperty));
        movementPanel.Children.Add(BoundTextBox("Destination Anchor Entity ID", movementDestinationAnchorEntityIdProperty));
        var direction = new ComboBox();
        direction.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.Directions)));
        direction.Bind(ComboBox.SelectedItemProperty, new Binding(movementDestinationDirectionProperty) { Mode = BindingMode.TwoWay });
        movementPanel.Children.Add(Wrap("Destination Direction", direction));
        var movementWrap = Wrap("Movement", movementPanel);
        movementWrap.Bind(IsVisibleProperty, new Binding(movementVisibleProperty));
        panel.Children.Add(movementWrap);

        return Wrap(header, panel);
    }

    private Control BuildActorStateEditor()
    {
        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 16, 0, 0) };

        var directions = new ComboBox();
        directions.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.Directions)));
        directions.Bind(ComboBox.SelectedItemProperty, new Binding(nameof(MainEditorViewModel.SelectedInitialFacing)) { Mode = BindingMode.TwoWay });
        panel.Children.Add(Wrap("Initial Facing", directions));

        var hasFacing = new TextBlock();
        hasFacing.Bind(TextBlock.TextProperty, new Binding(nameof(MainEditorViewModel.HasInitialFacing)) { StringFormat = "Initial facing set: {0}" });
        panel.Children.Add(hasFacing);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var set = new Button { Content = "Set Initial Facing" };
        set.Click += (_, _) => ViewModel?.SetInitialFacing();
        var clear = new Button { Content = "Clear Initial Facing" };
        clear.Click += (_, _) => ViewModel?.ClearInitialFacing();
        buttons.Children.Add(set);
        buttons.Children.Add(clear);
        panel.Children.Add(buttons);

        return Wrap("Actor State", panel);
    }

    private Control BuildInventoryEditor()
    {
        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 16, 0, 0) };

        panel.Children.Add(new TextBlock
        {
            Text = "Click an occupied cell to select it. Click an empty cell to move the selected carried entity, or place the selected template.",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        panel.Children.Add(Wrap("Inventory Grid", _inventoryGridRows));

        var carriedList = new ListBox { MinHeight = 120 };
        carriedList.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.CarriedEntities)));
        carriedList.Bind(ListBox.SelectedItemProperty, new Binding(nameof(MainEditorViewModel.SelectedCarriedEntity)) { Mode = BindingMode.TwoWay });
        panel.Children.Add(Wrap("Carried Entities", carriedList));

        var templateToPlace = new ComboBox();
        templateToPlace.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.EntityPresets)));
        templateToPlace.Bind(ComboBox.SelectedItemProperty, new Binding(nameof(MainEditorViewModel.SelectedTemplateToPlace)) { Mode = BindingMode.TwoWay });
        panel.Children.Add(Wrap("Template To Place", templateToPlace));

        var place = new Button
        {
            Content = "Place In First Open Cell",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        place.Click += (_, _) => ViewModel?.PlaceSelectedTemplateInInventory();
        panel.Children.Add(place);

        var replacementTemplate = new ComboBox();
        replacementTemplate.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainEditorViewModel.EntityPresets)));
        replacementTemplate.Bind(ComboBox.SelectedItemProperty, new Binding(nameof(MainEditorViewModel.SelectedReplacementTemplate)) { Mode = BindingMode.TwoWay });
        panel.Children.Add(Wrap("Replacement Template", replacementTemplate));

        var replacementButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var replace = new Button { Content = "Replace Selected" };
        replace.Click += (_, _) => ViewModel?.ReplaceSelectedCarriedEntityTemplate();
        var remove = new Button { Content = "Remove Selected" };
        remove.Click += (_, _) => ViewModel?.RemoveSelectedCarriedEntity();
        replacementButtons.Children.Add(replace);
        replacementButtons.Children.Add(remove);
        panel.Children.Add(replacementButtons);

        return Wrap("Inventory Layout", panel);
    }

    private static Control BoundTextBox(string label, string propertyName)
    {
        var textBox = new TextBox();
        textBox.Bind(TextBox.TextProperty, new Binding(propertyName)
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        return Wrap(label, textBox);
    }

    private static Control BoundNumeric(string label, string propertyName)
    {
        var numeric = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 100000
        };
        numeric.Bind(NumericUpDown.ValueProperty, new Binding(propertyName) { Mode = BindingMode.TwoWay });
        return Wrap(label, numeric);
    }

    private static Control BoundCheckBox(string label, string propertyName)
    {
        var checkBox = new CheckBox { Content = label };
        checkBox.Bind(CheckBox.IsCheckedProperty, new Binding(propertyName) { Mode = BindingMode.TwoWay });
        return checkBox;
    }

    private static Control Wrap(string header, Control content) =>
        new StackPanel
        {
            Spacing = 4,
            Margin = new Thickness(0, 0, 0, 8),
            Children =
            {
                new TextBlock { Text = header, FontWeight = Avalonia.Media.FontWeight.SemiBold },
                content
            }
        };

    private void BindInventoryGridViewModel()
    {
        if (_boundViewModel is not null)
        {
            _boundViewModel.InventoryGridCells.CollectionChanged -= InventoryGridCellsChanged;
            _boundViewModel.PropertyChanged -= ViewModelPropertyChanged;
        }

        _boundViewModel = ViewModel;
        if (_boundViewModel is not null)
        {
            _boundViewModel.InventoryGridCells.CollectionChanged += InventoryGridCellsChanged;
            _boundViewModel.PropertyChanged += ViewModelPropertyChanged;
        }

        RebuildInventoryGrid();
    }

    private void InventoryGridCellsChanged(object? sender, NotifyCollectionChangedEventArgs args) =>
        RebuildInventoryGrid();

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(MainEditorViewModel.SelectedInventoryWidth)
            || args.PropertyName == nameof(MainEditorViewModel.SelectedInventoryHeight))
        {
            RebuildInventoryGrid();
        }
    }

    private void RebuildInventoryGrid()
    {
        _inventoryGridRows.Children.Clear();

        if (ViewModel is not { } viewModel || viewModel.InventoryGridCells.Count == 0)
        {
            _inventoryGridRows.Children.Add(new TextBlock { Text = "No usable inventory grid." });
            return;
        }

        foreach (var row in viewModel.InventoryGridCells.GroupBy(cell => cell.Coord.Y).OrderBy(group => group.Key))
        {
            var rowPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            foreach (var cell in row.OrderBy(cell => cell.Coord.X))
            {
                var button = new Button
                {
                    Content = cell.DisplayText,
                    MinWidth = 72,
                    MinHeight = 44,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                button.Click += (_, _) => ViewModel?.ClickInventoryGridCell(cell);
                rowPanel.Children.Add(button);
            }

            _inventoryGridRows.Children.Add(rowPanel);
        }
    }

    private MainEditorViewModel? ViewModel => DataContext as MainEditorViewModel;
}

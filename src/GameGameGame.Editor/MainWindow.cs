using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using GameGameGame.Content;
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

        var form = new ScrollViewer { Content = BuildPresetForm() };
        Grid.SetColumn(form, 2);
        grid.Children.Add(form);

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

        panel.Children.Add(BuildInventoryEditor());

        return Wrap("Selected Preset", panel);
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

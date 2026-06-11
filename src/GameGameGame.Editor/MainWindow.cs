using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using GameGameGame.Content;

namespace GameGameGame.Editor;

public sealed class MainWindow : Window
{
    private readonly TextBox _pathTextBox = new() { Watermark = "Path to content YAML" };

    public MainWindow()
    {
        Title = "GameGameGame Content Editor";
        Width = 1200;
        Height = 800;
        Content = BuildContent();
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
        var presetListPanel = Wrap("Entity Presets", presetList);
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
        var open = new Button { Content = "Open" };
        open.Click += (_, _) => ViewModel?.OpenFile(_pathTextBox.Text ?? string.Empty);
        var save = new Button { Content = "Save" };
        save.Click += (_, _) => ViewModel?.Save();
        buttons.Children.Add(_pathTextBox);
        buttons.Children.Add(open);
        buttons.Children.Add(save);
        panel.Children.Add(buttons);

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

    private MainEditorViewModel? ViewModel => DataContext as MainEditorViewModel;
}

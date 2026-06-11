using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.Editor;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Editor)]
public sealed class EditorViewModelTests
{
    [Fact]
    public void EditorViewModelOpensContentFileAndListsEntityPresets()
    {
        var path = WriteTempContentFile(BasicContentYaml);

        try
        {
            var editor = new MainEditorViewModel();

            var result = editor.OpenFile(path);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(path, editor.FilePath);
            Assert.False(editor.IsDirty);
            var preset = Assert.Single(editor.EntityPresets);
            Assert.Equal(new EntityTemplateId("rock"), preset.Id);
            Assert.Equal("Rock", preset.Name);
            Assert.Equal('*', preset.Glyph);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelEditsSelectedPresetAndUpdatesPreviewDiffAndValidation()
    {
        var path = WriteTempContentFile(BasicContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);

            editor.SelectEntityPreset(new EntityTemplateId("rock"));
            editor.SelectedName = "Editor Rock";
            editor.SelectedGlyph = "R";
            editor.SelectedColor = PresentationColor.White;
            editor.SelectedWeight = 5;
            editor.ApplySelectedEntityPresetEdits();

            Assert.True(editor.IsDirty);
            Assert.Empty(editor.ValidationMessages);
            Assert.Contains("Editor Rock", editor.YamlPreview);
            Assert.Contains(editor.YamlDiffLines, line => line.StartsWith("+") && line.Contains("Editor Rock"));
            Assert.Equal("Editor Rock", editor.EntityPresets.Single().Name);
            Assert.Equal('R', editor.EntityPresets.Single().Glyph);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelApplyKeepsSelectedPresetWhenUiClearsSelectionDuringRefresh()
    {
        var path = WriteTempContentFile(BasicContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("rock"));
            editor.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainEditorViewModel.IsDirty))
                {
                    editor.SelectedPreset = null;
                }
            };

            editor.ApplySelectedEntityPresetEdits();

            Assert.NotNull(editor.SelectedPreset);
            Assert.Equal(new EntityTemplateId("rock"), editor.SelectedPreset.Id);
            Assert.Equal("Applied edits to Rock.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelSavesAndClearsDirtyStateAndDiff()
    {
        var path = WriteTempContentFile(BasicContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("rock"));
            editor.SelectedName = "Saved Editor Rock";
            editor.ApplySelectedEntityPresetEdits();

            var result = editor.Save();

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.False(editor.IsDirty);
            Assert.Empty(editor.YamlDiffLines);
            Assert.Equal("Saved Editor Rock", YamlContentLoader.LoadRegistryFile(path).EntityTemplates[new EntityTemplateId("rock")].Name);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelSaveAsWritesNewPathAndClearsDirtyStateAndDiff()
    {
        var path = WriteTempContentFile(BasicContentYaml);
        var saveAsPath = Path.Combine(Path.GetTempPath(), $"game-editor-viewmodel-save-as-{Guid.NewGuid():N}.yaml");

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("rock"));
            editor.SelectedName = "Saved As Rock";
            editor.ApplySelectedEntityPresetEdits();

            var result = editor.SaveAs(saveAsPath);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(saveAsPath, editor.FilePath);
            Assert.False(editor.IsDirty);
            Assert.Empty(editor.YamlDiffLines);
            Assert.Equal("Saved As Rock", YamlContentLoader.LoadRegistryFile(saveAsPath).EntityTemplates[new EntityTemplateId("rock")].Name);
            Assert.Equal("Saved as.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
            DeleteIfExists(saveAsPath);
        }
    }

    [Fact]
    public void EditorViewModelReloadDiscardsUnsavedEditsAndRefreshesUi()
    {
        var path = WriteTempContentFile(BasicContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("rock"));
            editor.SelectedName = "Unsaved Rock";
            editor.SelectedGlyph = "R";
            editor.ApplySelectedEntityPresetEdits();

            var result = editor.Reload();

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.False(editor.IsDirty);
            Assert.Empty(editor.YamlDiffLines);
            Assert.Equal(new EntityTemplateId("rock"), editor.SelectedPreset?.Id);
            Assert.Equal("Rock", editor.SelectedName);
            Assert.Equal("*", editor.SelectedGlyph);
            Assert.Equal("Reloaded.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelCreatesNewDocumentWithoutPath()
    {
        var editor = new MainEditorViewModel();

        editor.CreateNewDocument();

        Assert.Null(editor.FilePath);
        Assert.False(editor.IsDirty);
        Assert.Empty(editor.EntityPresets);
        Assert.Empty(editor.ValidationMessages);
        Assert.Equal("Created new content document.", editor.StatusMessage);
    }

    [Fact]
    public void EditorViewModelCreatesEntityPresetAndSelectsIt()
    {
        var path = WriteTempContentFile(BasicContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);

            editor.EntityPresetNameInput = "New Bag";
            editor.CreateEntityPreset();

            var created = editor.EntityPresets.Single(item => item.Id == new EntityTemplateId("newBag"));
            Assert.Equal(created, editor.SelectedPreset);
            Assert.Equal("New Bag", editor.SelectedName);
            Assert.Equal("Created New Bag.", editor.StatusMessage);
            Assert.True(editor.IsDirty);
            Assert.Contains("newBag", editor.YamlPreview);
            Assert.Contains(editor.YamlDiffLines, line => line.StartsWith("+") && line.Contains("newBag"));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelDuplicatesSelectedEntityPresetAndSelectsCopy()
    {
        var path = WriteTempContentFile(InventoryContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));

            editor.EntityPresetNameInput = "Bag Copy";
            editor.DuplicateSelectedEntityPreset();

            var duplicate = editor.EntityPresets.Single(item => item.Id == new EntityTemplateId("bagCopy"));
            Assert.Equal(duplicate, editor.SelectedPreset);
            Assert.Equal("Bag Copy", editor.SelectedName);
            Assert.Single(editor.CarriedEntities);
            Assert.Equal("Duplicated Bag as Bag Copy.", editor.StatusMessage);
            Assert.Contains("bagCopy", editor.YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelDeletesUnreferencedSelectedEntityPreset()
    {
        var path = WriteTempContentFile(BasicContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("rock"));

            editor.DeleteSelectedEntityPreset();

            Assert.Empty(editor.EntityPresets);
            Assert.Null(editor.SelectedPreset);
            Assert.Equal("Deleted Rock.", editor.StatusMessage);
            Assert.True(editor.IsDirty);
            Assert.DoesNotContain("rock:", editor.YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelDoesNotDeleteReferencedSelectedEntityPreset()
    {
        var path = WriteTempContentFile(InventoryContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("rock"));

            editor.DeleteSelectedEntityPreset();

            Assert.Contains(editor.EntityPresets, item => item.Id == new EntityTemplateId("rock"));
            Assert.Equal(new EntityTemplateId("rock"), editor.SelectedPreset?.Id);
            Assert.Contains("Cannot delete entity template rock", editor.StatusMessage);
            Assert.Contains("carriedRock", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelListsCarriedEntitiesForSelectedPreset()
    {
        var path = WriteTempContentFile(InventoryContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);

            editor.SelectEntityPreset(new EntityTemplateId("bag"));

            var carried = Assert.Single(editor.CarriedEntities);
            Assert.Equal(new EntityId("carriedRock"), carried.EntityId);
            Assert.Equal(new EntityTemplateId("rock"), carried.TemplateId);
            Assert.Equal(new GridCoord(0, 0), carried.Coord);
            Assert.Equal("Rock", carried.TemplateName);
            Assert.Equal('*', carried.Glyph);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelBuildsInventoryGridCellsForSelectedPreset()
    {
        var path = WriteTempContentFile(InventoryContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));

            Assert.Collection(
                editor.InventoryGridCells,
                cell =>
                {
                    Assert.Equal(new GridCoord(0, 0), cell.Coord);
                    Assert.True(cell.IsOccupied);
                    Assert.Equal(new EntityId("carriedRock"), cell.CarriedEntityId);
                    Assert.Equal("* Rock", cell.DisplayText);
                },
                cell =>
                {
                    Assert.Equal(new GridCoord(1, 0), cell.Coord);
                    Assert.False(cell.IsOccupied);
                    Assert.Equal(".", cell.DisplayText);
                });
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelClickingOccupiedGridCellSelectsCarriedEntity()
    {
        var path = WriteTempContentFile(InventoryContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));

            editor.ClickInventoryGridCell(editor.InventoryGridCells.Single(cell => cell.Coord == new GridCoord(0, 0)));

            Assert.NotNull(editor.SelectedCarriedEntity);
            Assert.Equal(new EntityId("carriedRock"), editor.SelectedCarriedEntity.EntityId);
            Assert.Equal("Selected Rock at 0,0.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelClickingEmptyGridCellPlacesSelectedTemplateThere()
    {
        var path = WriteTempContentFile(InventoryContentYamlWithoutCarriedEntity);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));
            editor.SelectedTemplateToPlace = editor.EntityPresets.Single(item => item.Id == new EntityTemplateId("rock"));

            editor.ClickInventoryGridCell(editor.InventoryGridCells.Single(cell => cell.Coord == new GridCoord(1, 0)));

            var carried = Assert.Single(editor.CarriedEntities);
            Assert.Equal(new GridCoord(1, 0), carried.Coord);
            Assert.Equal(carried.EntityId, editor.SelectedCarriedEntity?.EntityId);
            Assert.Contains("x: 1", editor.YamlPreview);
            Assert.Contains(editor.YamlDiffLines, line => line.StartsWith("+") && line.Contains("bagRock"));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelGridPlacementKeepsTemplateWhenUiClearsSelectionDuringRefresh()
    {
        var path = WriteTempContentFile(InventoryContentYamlWithoutCarriedEntity);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));
            editor.SelectedTemplateToPlace = editor.EntityPresets.Single(item => item.Id == new EntityTemplateId("rock"));
            editor.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainEditorViewModel.IsDirty))
                {
                    editor.SelectedTemplateToPlace = null;
                }
            };

            editor.ClickInventoryGridCell(editor.InventoryGridCells.Single(cell => cell.Coord == new GridCoord(1, 0)));

            Assert.Equal("Placed Rock at 1,0.", editor.StatusMessage);
            var carried = Assert.Single(editor.CarriedEntities);
            Assert.Equal(new GridCoord(1, 0), carried.Coord);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelClickingEmptyGridCellMovesSelectedCarriedEntityThere()
    {
        var path = WriteTempContentFile(InventoryContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));
            editor.ClickInventoryGridCell(editor.InventoryGridCells.Single(cell => cell.Coord == new GridCoord(0, 0)));

            editor.ClickInventoryGridCell(editor.InventoryGridCells.Single(cell => cell.Coord == new GridCoord(1, 0)));

            var carried = Assert.Single(editor.CarriedEntities);
            Assert.Equal(new GridCoord(1, 0), carried.Coord);
            Assert.Equal(carried.EntityId, editor.SelectedCarriedEntity?.EntityId);
            Assert.Contains("Moved Rock to 1,0.", editor.StatusMessage);
            Assert.Contains("x: 1", editor.YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelPlacesCarriedEntityAndRefreshesPreviewDiffAndValidation()
    {
        var path = WriteTempContentFile(InventoryContentYamlWithoutCarriedEntity);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));

            editor.SelectedTemplateToPlace = editor.EntityPresets.Single(item => item.Id == new EntityTemplateId("rock"));
            editor.PlaceSelectedTemplateInInventory();

            var carried = Assert.Single(editor.CarriedEntities);
            Assert.Equal(new EntityId("bagRock"), carried.EntityId);
            Assert.Equal(new GridCoord(0, 0), carried.Coord);
            Assert.Empty(editor.ValidationMessages);
            Assert.Contains("bagRock", editor.YamlPreview);
            Assert.Contains(editor.YamlDiffLines, line => line.StartsWith("+") && line.Contains("bagRock"));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelFirstOpenPlacementKeepsTemplateWhenUiClearsSelectionDuringRefresh()
    {
        var path = WriteTempContentFile(InventoryContentYamlWithoutCarriedEntity);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));
            editor.SelectedTemplateToPlace = editor.EntityPresets.Single(item => item.Id == new EntityTemplateId("rock"));
            editor.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainEditorViewModel.IsDirty))
                {
                    editor.SelectedTemplateToPlace = null;
                }
            };

            editor.PlaceSelectedTemplateInInventory();

            Assert.Equal("Placed Rock.", editor.StatusMessage);
            Assert.Single(editor.CarriedEntities);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelReplacesSelectedCarriedEntityTemplate()
    {
        var path = WriteTempContentFile(InventoryContentYamlWithGem);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));
            editor.SelectedCarriedEntity = editor.CarriedEntities.Single();
            editor.SelectedReplacementTemplate = editor.EntityPresets.Single(item => item.Id == new EntityTemplateId("gem"));

            editor.ReplaceSelectedCarriedEntityTemplate();

            var carried = Assert.Single(editor.CarriedEntities);
            Assert.Equal(new EntityTemplateId("gem"), carried.TemplateId);
            Assert.Equal("Gem", carried.TemplateName);
            Assert.Contains("templateId: gem", editor.YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelRemovesSelectedCarriedEntity()
    {
        var path = WriteTempContentFile(InventoryContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));
            editor.SelectedCarriedEntity = editor.CarriedEntities.Single();

            editor.RemoveSelectedCarriedEntity();

            Assert.Empty(editor.CarriedEntities);
            Assert.DoesNotContain("carriedRock", editor.YamlPreview);
            Assert.Contains(editor.YamlDiffLines, line => line.StartsWith("-") && line.Contains("carriedRock"));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    private const string BasicContentYaml =
        """
        entityTemplates:
          rock:
            name: Rock
            inventoryWidth: 0
            inventoryHeight: 0
            weight: 3
            carryingCapacity: 3
        presentations:
          rock:
            glyph: '*'
            color: Earth
        actionPlans: {}
        """;

    private const string InventoryContentYaml =
        """
        entityTemplates:
          bag:
            name: Bag
            inventoryWidth: 2
            inventoryHeight: 1
            weight: 1
            carryingCapacity: 10
            carriedEntities:
              - entityId: carriedRock
                templateId: rock
                coord:
                  x: 0
                  y: 0
          rock:
            name: Rock
            inventoryWidth: 0
            inventoryHeight: 0
            weight: 3
            carryingCapacity: 3
        presentations:
          bag:
            glyph: b
            color: Gray
          rock:
            glyph: '*'
            color: Earth
        actionPlans: {}
        """;

    private const string InventoryContentYamlWithoutCarriedEntity =
        """
        entityTemplates:
          bag:
            name: Bag
            inventoryWidth: 2
            inventoryHeight: 1
            weight: 1
            carryingCapacity: 10
          rock:
            name: Rock
            inventoryWidth: 0
            inventoryHeight: 0
            weight: 3
            carryingCapacity: 3
        presentations:
          bag:
            glyph: b
            color: Gray
          rock:
            glyph: '*'
            color: Earth
        actionPlans: {}
        """;

    private const string InventoryContentYamlWithGem =
        """
        entityTemplates:
          bag:
            name: Bag
            inventoryWidth: 2
            inventoryHeight: 1
            weight: 1
            carryingCapacity: 10
            carriedEntities:
              - entityId: carriedRock
                templateId: rock
                coord:
                  x: 0
                  y: 0
          rock:
            name: Rock
            inventoryWidth: 0
            inventoryHeight: 0
            weight: 3
            carryingCapacity: 3
          gem:
            name: Gem
            inventoryWidth: 0
            inventoryHeight: 0
            weight: 1
            carryingCapacity: 0
        presentations:
          bag:
            glyph: b
            color: Gray
          rock:
            glyph: '*'
            color: Earth
          gem:
            glyph: g
            color: Green
        actionPlans: {}
        """;

    private static string WriteTempContentFile(string yaml)
    {
        var path = Path.Combine(Path.GetTempPath(), $"game-editor-viewmodel-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(path, yaml);

        return path;
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

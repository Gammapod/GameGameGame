using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Navigation;
using GameGameGame.SadConsoleApp.Ui.Rendering;
using GameGameGame.SadConsoleApp.Ui.Screens;
using GameGameGame.SadConsoleApp.Ui.Styling;
using GameGameGame.SadConsoleApp.Ui.Tiles;
using SadRogue.Primitives;

namespace GameGameGame.SadConsole.Tests;

public sealed class SadConsoleComponentGalleryTests
{
    [Fact]
    public void GalleryContainsPhaseOneReviewComponents()
    {
        var gallery = ComponentGalleryScreen.CreateDefault();

        var components = gallery.Components();

        Assert.Collection(
            components,
            component => Assert.Equal("panel-states", component.Id),
            component => Assert.Equal("lists", component.Id),
            component => Assert.Equal("fields", component.Id),
            component => Assert.Equal("text-entry-overlay", component.Id),
            component => Assert.Equal("int-setter-overlay", component.Id),
            component => Assert.Equal("choice-picker-overlay", component.Id),
            component => Assert.Equal("confirm-overlay", component.Id),
            component => Assert.Equal("candii-tileset", component.Id),
            component => Assert.Equal("inventory-space-scale-probe", component.Id),
            component => Assert.Equal("connector-line", component.Id),
            component => Assert.Equal("play-entity-tooltip", component.Id),
            component => Assert.Equal("play-mode-components", component.Id),
            component => Assert.Equal("inventory-space", component.Id),
            component => Assert.Equal("footer", component.Id));
    }

    [Fact]
    public void GalleryShowsAllPanelBorderTokensForUserReview()
    {
        var theme = SadConsoleTheme.Default;
        var rows = ComponentGalleryScreen.CreateDefault(theme).RenderReviewRows();

        Assert.Contains(rows, row => row.Contains(theme.Panel.BorderUnselected));
        Assert.Contains(rows, row => row.Contains(theme.Panel.BorderSelected));
        Assert.Contains(rows, row => row.Contains(theme.Panel.BorderFocused));
        Assert.Contains(rows, row => row.Contains(theme.Panel.BorderDisabled));
        Assert.Contains(rows, row => row.Contains(theme.Panel.BorderError));
    }

    [Fact]
    public void GalleryShowsSelectableListFieldAndFooterStateExamples()
    {
        var rows = ComponentGalleryScreen.CreateDefault().RenderReviewRows();

        Assert.Contains(rows, row => row.Contains("Selectable lists"));
        Assert.Contains(rows, row => row.Contains("Scenario row"));
        Assert.Contains(rows, row => row.Contains("opens Play/Debug/Edit; Esc cancels/back"));
        Assert.Contains(rows, row => row.Contains("Editable fields"));
        Assert.Contains(rows, row => row.Contains("scenario root"));
        Assert.Contains(rows, row => row.Contains("range must be 0-10"));
        Assert.Contains(rows, row => row.Contains("Text entry overlay"));
        Assert.Contains(rows, row => row.Contains("Int setter overlay"));
        Assert.Contains(rows, row => row.Contains("Choice picker overlay"));
        Assert.Contains(rows, row => row.Contains("■ Yellow"));
        Assert.Contains(rows, row => row.Contains("Confirm overlay"));
        Assert.Contains(rows, row => row.Contains("Candii 8x8 tileset preview"));
        Assert.Contains(rows, row => row.Contains("square 8x8 cells"));
        Assert.Contains(rows, row => row.Contains("Inventory Space Zoom probe"));
        Assert.Contains(rows, row => row.Contains("Mixed Space Zoom probe"));
        Assert.Contains(rows, row => row.Contains("Connector-line pattern"));
        Assert.Contains(rows, row => row.Contains("Accepted connector-line pattern"));
        Assert.Contains(rows, row => row.Contains("Big Slime Moved North"));
        Assert.Contains(rows, row => row.Contains("Play mode component map"));
        Assert.Contains(rows, row => row.Contains("0.2.1 Action selector"));
        Assert.Contains(rows, row => row.Contains("Inventory-space component"));
        Assert.Contains(rows, row => row.Contains("backdrop glyph 160"));
        Assert.Contains(rows, row => row.Contains("layers: backdrop"));
        Assert.DoesNotContain(rows, row => row.Contains("Command palette overlay"));
        Assert.Contains(rows, row => row.Contains("Context footer"));
        Assert.Contains(rows, row => row.Contains("arrows select a component"));
    }

    [Fact]
    public void GalleryUsesFocusRouterToDemonstrateSelectedAndFocusedControls()
    {
        var gallery = ComponentGalleryScreen.CreateDefault();

        Assert.Equal("panel-states", gallery.SelectedComponentId);
        Assert.Null(gallery.FocusedComponentId);

        var moved = gallery.Handle(UiComponentCommand.Right);
        Assert.Equal(FocusRouterResultKind.SelectedComponent, moved.Kind);
        Assert.Equal("lists", gallery.SelectedComponentId);

        var focused = gallery.Handle(UiComponentCommand.Select);
        Assert.Equal(FocusRouterResultKind.FocusedComponent, focused.Kind);
        Assert.Equal("lists", gallery.FocusedComponentId);
        Assert.Contains(gallery.RenderReviewRows(), row => row.Contains("Component focused: arrows route to component"));

        var released = gallery.Handle(UiComponentCommand.Cancel);
        Assert.Equal(FocusRouterResultKind.ReleasedFocus, released.Kind);
        Assert.Null(gallery.FocusedComponentId);
    }

    [Fact]
    public void GalleryCanUseCustomThemeWithoutChangingComponents()
    {
        var theme = SadConsoleTheme.Default with
        {
            Name = "Review",
            Panel = SadConsoleTheme.Default.Panel with
            {
                BorderUnselected = "ReviewUnselected",
                BorderSelected = "ReviewSelected",
                BorderFocused = "ReviewFocused",
                BorderGlyphs = new BorderGlyphTheme('[', ']', '[', ']', '~', ':')
            },
            Footer = SadConsoleTheme.Default.Footer with
            {
                Background = "ReviewFooterBackground"
            }
        };

        var rows = ComponentGalleryScreen.CreateDefault(theme).RenderReviewRows();

        Assert.Contains(rows, row => row.Contains("ReviewUnselected"));
        Assert.Contains(rows, row => row.Contains("ReviewSelected"));
        Assert.Contains(rows, row => row.Contains("ReviewFocused"));
        Assert.Contains(rows, row => row.Contains("ReviewFooterBackground"));
        Assert.Contains(rows, row => row.Contains("[~] : [~]"));
    }

    [Fact]
    public void BuiltInThemesProvideDifferentColorsAndBorderGlyphs()
    {
        Assert.Contains(SadConsoleTheme.BuiltInThemes, theme => theme.Name == "Default");
        Assert.Contains(SadConsoleTheme.BuiltInThemes, theme => theme.Name == "Blueprint");
        Assert.NotEqual(SadConsoleTheme.Default.Panel.BorderSelected, SadConsoleTheme.Blueprint.Panel.BorderSelected);
        Assert.NotEqual(SadConsoleTheme.Default.Panel.BorderGlyphs, SadConsoleTheme.Blueprint.Panel.BorderGlyphs);
    }

    [Fact]
    public void GallerySurfacesSelectedThemeAndBorderGlyphPreview()
    {
        var rows = ComponentGalleryScreen.CreateDefault(SadConsoleTheme.Blueprint).RenderReviewRows();

        Assert.Contains(rows, row => row.Contains("theme: Blueprint"));
        Assert.Contains(rows, row => row.Contains("#=# ! #=#"));
    }

    [Fact]
    public void StartupRecognizesGalleryModeWithoutLoadingScenarioCatalog()
    {
        var startup = SadConsoleStartup.FromArgs(["--gallery"]);

        Assert.True(startup.LaunchGallery);
        Assert.Null(startup.Catalog);
        Assert.Null(startup.Error);
    }

    [Fact]
    public void GalleryRendererStripsStyleTokensForVisualRows()
    {
        var stripped = ComponentGalleryConsole.StripStyleTokens("(LightGray) name: (Gold) Player");

        Assert.Equal("name: Player", stripped);
    }

    [Fact]
    public void GalleryRendererMapsThemeTokensToSadConsoleColors()
    {
        Assert.Equal(SadRogue.Primitives.Color.Gold, ComponentGalleryConsole.ColorFromToken("Gold"));
        Assert.Equal(SadRogue.Primitives.Color.HotPink, ComponentGalleryConsole.ColorFromToken("HotPink"));
        Assert.Equal(SadRogue.Primitives.Color.Black, ComponentGalleryConsole.ColorFromToken("Black"));
        Assert.Equal(SadRogue.Primitives.Color.White, ComponentGalleryConsole.ColorFromToken("Default"));
        Assert.Equal(SadRogue.Primitives.Color.Green, ComponentGalleryConsole.ColorFromToken("Green"));
        Assert.Equal(SadRogue.Primitives.Color.DarkGreen, ComponentGalleryConsole.ColorFromToken("DarkGreen"));
        Assert.Equal(SadRogue.Primitives.Color.Yellow, ComponentGalleryConsole.ColorFromToken("Yellow"));
        Assert.Equal(SadRogue.Primitives.Color.SaddleBrown, ComponentGalleryConsole.ColorFromToken("Earth"));
        Assert.Equal(SadRogue.Primitives.Color.White, ComponentGalleryConsole.ColorFromToken("unknown-token"));
    }

    [Fact]
    public void GalleryRendererDetectsColorSampleTokensForBlockGlyphPreview()
    {
        var row = "> (HotPink) (Green) ■ Green";

        Assert.Equal("Green", ComponentGalleryConsole.SampleColorTokenForRow(row));
        Assert.Equal("> ■ Green", ComponentGalleryConsole.StripStyleTokens(row));
    }

    [Fact]
    public void GalleryRendererCanPreviewBorderGlyphTheme()
    {
        Assert.Equal("#=# ! #=#", ComponentGalleryConsole.BorderGlyphPreview(BorderGlyphTheme.DoubleAscii));
    }

    [Fact]
    public void CandiiTilesetProfileDefinesSquareTileRoles()
    {
        var profile = TilesetProfileLoader.Load(Path.Combine("assets", "Candii.tileset.json"));

        Assert.Equal("candii-8x8", profile.Id);
        Assert.Equal("Candii", profile.FontName);
        Assert.Equal(8, profile.TileWidth);
        Assert.Equal(8, profile.TileHeight);
        Assert.Equal(8, profile.BaseUnit);
        Assert.Equal(0, profile.Blank);
        Assert.Equal(0, profile.ResolveTextGlyph(' '));
        Assert.Equal('&', profile.ResolveTextGlyph('&'));
        Assert.Equal(181, profile.Roles.ColorSample);
        Assert.Equal(180, profile.Roles.PanelBorder.TopLeft);
        Assert.Equal(153, profile.Roles.PanelBorder.TopRight);
        Assert.Equal(154, profile.Roles.PanelBorder.BottomLeft);
        Assert.Equal(179, profile.Roles.PanelBorder.BottomRight);
        Assert.Equal(158, profile.Roles.PanelBorder.Horizontal);
        Assert.Equal(141, profile.Roles.PanelBorder.Vertical);
        Assert.Empty(profile.Validate());
    }

    [Fact]
    public void GalleryIncludesInventorySpaceComponentAsExecutablePatternReference()
    {
        var gallery = ComponentGalleryScreen.CreateDefault();

        var inventorySpace = Assert.IsType<InventorySpaceComponent>(gallery.Components().Single(component => component.Id == "inventory-space"));

        Assert.Equal(160, inventorySpace.View.Backdrop.Tile.Glyph);
        Assert.Equal(0x808080, inventorySpace.View.Backdrop.Tile.ForegroundRgb);
        Assert.Equal(0x404040, inventorySpace.View.Backdrop.Tile.BackgroundRgb);
        Assert.Equal(5, inventorySpace.View.Viewport.Width);
        Assert.Equal(4, inventorySpace.View.Viewport.Height);
        Assert.Equal(2, inventorySpace.View.Entities.Count);
        Assert.Contains(inventorySpace.View.Decorators, decorator => decorator.Role == InventorySpaceDecoratorRole.Controlled);
        Assert.Same(InventorySpaceRenderOptions.FramedDebug, inventorySpace.Options);
        Assert.True(inventorySpace.Bounds.Height >= inventorySpace.RequiredHeight);
    }

    [Fact]
    public void GalleryIncludesConnectorLinePatternAsExecutablePatternReference()
    {
        var gallery = ComponentGalleryScreen.CreateDefault();

        var connector = Assert.IsType<ConnectorLineComponent>(gallery.Components().Single(component => component.Id == "connector-line"));

        Assert.Equal("Connector-line pattern", connector.Title);
        Assert.Equal(2, connector.View.Segments.Count);
        Assert.All(connector.View.Segments, segment => Assert.Equal(1, segment.Layer));
        Assert.Equal('-', connector.View.FallbackGlyphs.Horizontal);
        Assert.Equal('|', connector.View.FallbackGlyphs.Vertical);
        Assert.Equal('+', connector.View.FallbackGlyphs.Junction);
        Assert.Contains(connector.RenderRows(SadConsoleTheme.Default), row => row.Contains("below prompts/debug"));
    }

    [Fact]
    public void GalleryIncludesInventorySpaceScaleProbeAsExecutablePatternReference()
    {
        var gallery = ComponentGalleryScreen.CreateDefault();

        var probe = Assert.IsType<InventorySpaceScaleProbeComponent>(gallery.Components().Single(component => component.Id == "inventory-space-scale-probe"));

        Assert.Collection(
            probe.Samples,
            sample =>
            {
                Assert.Equal(InventorySpaceRelationshipTier.CurrentLocation, sample.Profile.RelationshipTier);
                Assert.Equal(InventorySpaceZoom.Huge32, sample.Profile.SpaceZoom);
                Assert.Equal(32, sample.Profile.CellPixelSize);
            },
            sample =>
            {
                Assert.Equal(InventorySpaceRelationshipTier.PlayerInventory, sample.Profile.RelationshipTier);
                Assert.Equal(InventorySpaceZoom.Large24, sample.Profile.SpaceZoom);
                Assert.Equal(24, sample.Profile.CellPixelSize);
                Assert.Equal(1, sample.Profile.CellGapPixels);
            },
            sample => Assert.Equal(InventorySpaceZoom.Normal16, sample.Profile.SpaceZoom),
            sample => Assert.Equal(InventorySpaceZoom.Small8, sample.Profile.SpaceZoom),
            sample =>
            {
                Assert.Equal(InventorySpaceZoom.Micro4, sample.Profile.SpaceZoom);
                Assert.False(sample.Profile.UsesCandiiFont);
            });
        Assert.All(probe.Samples, sample => Assert.Equal(2, sample.View.Entities.Count));
    }

    [Fact]
    public void GalleryIncludesPlayEntityTooltipPatternAsExecutablePatternReference()
    {
        var gallery = ComponentGalleryScreen.CreateDefault();

        var tooltip = Assert.IsType<PlayEntityTooltipComponent>(gallery.Components().Single(component => component.Id == "play-entity-tooltip"));

        Assert.Equal("Play entity tooltip pattern", tooltip.Title);
        Assert.Equal(["Big Slime Moved North"], tooltip.BodyRows);
        Assert.InRange(tooltip.BackgroundAlpha, (byte)1, (byte)254);
        Assert.Equal(1, tooltip.Bounds.Height);
    }

    [Fact]
    public void TilesetTextRendererConvertsTextThroughProfileMapping()
    {
        var profile = TilesetProfileLoader.Load(Path.Combine("assets", "Candii.tileset.json"));
        var renderer = new TilesetTextRenderer(profile);

        var glyphs = renderer.ToGlyphs("A B");

        Assert.Equal(new[] { (int)'A', 0, (int)'B' }, glyphs);
    }

    [Fact]
    public void TileLayoutConvertsParentCellsToActiveTileCells()
    {
        var rect = TileLayoutRect.FromParentCells(
            left: 10,
            top: 4,
            width: 20,
            height: 6,
            parentCellSize: new Point(8, 16),
            tileSize: new Point(8, 8));

        Assert.Equal(10, rect.Left);
        Assert.Equal(4, rect.Top);
        Assert.Equal(20, rect.Width);
        Assert.Equal(12, rect.Height);
    }

    [Fact]
    public void DisplaySettingsUseIntegerScaledCandiiViewportPixels()
    {
        var settings = SadConsoleDisplaySettings.Default.WithUiScale(2);

        Assert.Equal(2, settings.UiScale);
        Assert.Equal(16, settings.ScaledTileWidth);
        Assert.Equal(16, settings.ScaledTileHeight);
        Assert.Equal(1920, settings.WindowWidthPixels);
        Assert.Equal(672, settings.WindowHeightPixels);
        Assert.Contains("Scale: 2x", settings.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void StartupParsesUiScaleArgument()
    {
        var startup = SadConsoleStartup.FromArgs(["--gallery", "--ui-scale", "3"]);

        Assert.True(startup.LaunchGallery);
        Assert.Equal(3, startup.ActiveDisplaySettings.UiScale);
        Assert.Equal(24, startup.ActiveDisplaySettings.ScaledTileWidth);
    }
}

using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Navigation;
using GameGameGame.SadConsoleApp.Ui.Rendering;
using GameGameGame.SadConsoleApp.Ui.Screens;
using GameGameGame.SadConsoleApp.Ui.Styling;
using GameGameGame.SadConsoleApp.Ui.Tiles;

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
            component => Assert.Equal("play-mode-components", component.Id),
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
        Assert.Contains(rows, row => row.Contains("opens Play/Edit; Esc cancels/back"));
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
        Assert.Contains(rows, row => row.Contains("Play mode component map"));
        Assert.Contains(rows, row => row.Contains("0.2.1 Action selector"));
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
        Assert.Equal(180, profile.Roles.PanelBorder.TopLeft);
        Assert.Equal(153, profile.Roles.PanelBorder.TopRight);
        Assert.Equal(154, profile.Roles.PanelBorder.BottomLeft);
        Assert.Equal(179, profile.Roles.PanelBorder.BottomRight);
        Assert.Equal(158, profile.Roles.PanelBorder.Horizontal);
        Assert.Equal(141, profile.Roles.PanelBorder.Vertical);
        Assert.Empty(profile.Validate());
    }
}

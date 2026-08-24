using System.Text.Json;
using System.Text.Json.Serialization;
using GameGameGame.Core;

namespace GameGameGame.Frontend.SadConsole;

internal sealed record TilesetProfile(
    string Id,
    string FontName,
    string FontFile,
    string ImageFile,
    int TileWidth,
    int TileHeight,
    int BaseUnit,
    int Blank,
    bool AsciiCodepointMapping,
    TilesetPresentationMappings PresentationMappings,
    TilesetRoles Roles)
{
    public int ResolveTextGlyph(char character) => character == ' '
        ? Blank
        : AsciiCodepointMapping
            ? character
            : throw new InvalidOperationException($"Tileset '{Id}' does not define text glyph mapping for '{character}'.");
}

internal sealed record TilesetRoles(
    int GridDotted,
    int GridCave,
    int GridWood,
    int GridMetal,
    int FacingDiag,
    int FacingNS,
    int FacingWE,
    int MoveHighlight,
    int EntityHighlight,
    int PickupHighlight,
    int PushHighlight,
    int DropHighlight,
    int EnterHighlight,
    int ExitHighlight,
    int TransferHighlight,
    int NoActionHighlight,
    TileBorderGlyphSet PanelBorder)
{
    public int DefaultBackdrop => GridDotted;

    public int BackdropForMaterial(EntityMaterial? material) => material?.Value switch
    {
        "metal" => GridMetal,
        "stone" => GridCave,
        "wood" => GridWood,
        _ => DefaultBackdrop
    };

    public (int Glyph, global::SadConsole.Mirror Mirror) FacingGlyph(GameGameGame.Core.Direction direction) => direction switch
    {
        GameGameGame.Core.Direction.North => (FacingNS, global::SadConsole.Mirror.None),
        GameGameGame.Core.Direction.South => (FacingNS, global::SadConsole.Mirror.Vertical),
        GameGameGame.Core.Direction.East => (FacingWE, global::SadConsole.Mirror.None),
        GameGameGame.Core.Direction.West => (FacingWE, global::SadConsole.Mirror.Horizontal),
        GameGameGame.Core.Direction.NorthWest => (FacingDiag, global::SadConsole.Mirror.None),
        GameGameGame.Core.Direction.NorthEast => (FacingDiag, global::SadConsole.Mirror.Horizontal),
        GameGameGame.Core.Direction.SouthWest => (FacingDiag, global::SadConsole.Mirror.Vertical),
        GameGameGame.Core.Direction.SouthEast => (FacingDiag, global::SadConsole.Mirror.Horizontal | global::SadConsole.Mirror.Vertical),
        _ => (FacingNS, global::SadConsole.Mirror.None)
    };
}

internal sealed record TilesetPresentationMappings(IReadOnlyDictionary<string, int> GlyphsByPresentationId);

internal sealed record TileBorderGlyphSet(
    int TopLeft,
    int TopRight,
    int BottomLeft,
    int BottomRight,
    int Horizontal,
    int Vertical,
    int HorizontalWithSouthVertical,
    int HorizontalWithNorthVertical,
    int VerticalWithEastHorizontal,
    int VerticalWithWestHorizontal);

internal static class TilesetProfileLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static TilesetProfile Load(string path) =>
        JsonSerializer.Deserialize<TilesetProfile>(File.ReadAllText(path), Options)
        ?? throw new InvalidOperationException($"Tileset profile '{path}' did not deserialize.");

    public static TilesetProfile LoadCandii() => Load(ResolveAssetPath("Candii.tileset.json"));

    public static string ResolveAssetPath(string fileName)
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, "assets", fileName);
        if (File.Exists(outputPath)) return outputPath;

        var workingPath = Path.Combine(Environment.CurrentDirectory, "assets", fileName);
        if (File.Exists(workingPath)) return workingPath;

        return outputPath;
    }
}

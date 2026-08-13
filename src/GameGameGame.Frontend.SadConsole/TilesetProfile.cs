using System.Text.Json;
using System.Text.Json.Serialization;

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
    TilesetRoles Roles)
{
    public int ResolveTextGlyph(char character) => character == ' '
        ? Blank
        : AsciiCodepointMapping
            ? character
            : throw new InvalidOperationException($"Tileset '{Id}' does not define text glyph mapping for '{character}'.");
}

internal sealed record TilesetRoles(TileBorderGlyphSet PanelBorder);

internal sealed record TileBorderGlyphSet(
    int TopLeft,
    int TopRight,
    int BottomLeft,
    int BottomRight,
    int Horizontal,
    int Vertical);

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

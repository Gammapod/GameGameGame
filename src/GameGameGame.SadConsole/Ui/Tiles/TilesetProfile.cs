using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameGameGame.SadConsoleApp.Ui.Tiles;

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

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Id)) errors.Add("Tileset id is required.");
        if (string.IsNullOrWhiteSpace(FontName)) errors.Add("Tileset fontName is required.");
        if (TileWidth <= 0) errors.Add("tileWidth must be positive.");
        if (TileHeight <= 0) errors.Add("tileHeight must be positive.");
        if (BaseUnit <= 0) errors.Add("baseUnit must be positive.");
        if (TileWidth > 0 && BaseUnit > 0 && TileWidth % BaseUnit != 0) errors.Add("tileWidth must be a multiple of baseUnit.");
        if (TileHeight > 0 && BaseUnit > 0 && TileHeight % BaseUnit != 0) errors.Add("tileHeight must be a multiple of baseUnit.");
        if (Blank < 0) errors.Add("blank glyph index must be non-negative.");
        errors.AddRange(Roles.Validate("roles"));
        return errors;
    }
}

internal sealed record TilesetRoles(TileBorderGlyphSet PanelBorder)
{
    public IReadOnlyList<string> Validate(string path)
    {
        var errors = new List<string>();
        if (PanelBorder is null)
        {
            errors.Add($"{path}.panelBorder is required.");
        }
        else
        {
            errors.AddRange(PanelBorder.Validate($"{path}.panelBorder"));
        }

        return errors;
    }
}

internal sealed record TileBorderGlyphSet(
    int TopLeft,
    int TopRight,
    int BottomLeft,
    int BottomRight,
    int Horizontal,
    int Vertical)
{
    public IReadOnlyList<string> Validate(string path)
    {
        var errors = new List<string>();
        if (TopLeft < 0) errors.Add($"{path}.topLeft must be non-negative.");
        if (TopRight < 0) errors.Add($"{path}.topRight must be non-negative.");
        if (BottomLeft < 0) errors.Add($"{path}.bottomLeft must be non-negative.");
        if (BottomRight < 0) errors.Add($"{path}.bottomRight must be non-negative.");
        if (Horizontal < 0) errors.Add($"{path}.horizontal must be non-negative.");
        if (Vertical < 0) errors.Add($"{path}.vertical must be non-negative.");
        return errors;
    }
}

internal static class TilesetProfileLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static TilesetProfile Load(string path)
    {
        var profile = JsonSerializer.Deserialize<TilesetProfile>(File.ReadAllText(path), Options)
            ?? throw new InvalidOperationException($"Tileset profile '{path}' did not deserialize.");
        var errors = profile.Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Tileset profile '{path}' is invalid: {string.Join("; ", errors)}");
        }

        return profile;
    }
}

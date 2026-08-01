using GameGameGame.Content;
using GameGameGame.SadConsoleApp.Ui.Tiles;

namespace GameGameGame.SadConsoleApp.Ui.Presentation;

internal sealed class SadConsolePresentationResolver
{
    private readonly IReadOnlyDictionary<string, int> _glyphsByPresentationId;

    public SadConsolePresentationResolver(TilesetProfile tilesetProfile)
    {
        ArgumentNullException.ThrowIfNull(tilesetProfile);
        _glyphsByPresentationId = new Dictionary<string, int>(
            tilesetProfile.PresentationMappings.GlyphsByPresentationId,
            StringComparer.Ordinal);
    }

    public static SadConsolePresentationResolver Default { get; } = new(TilesetProfileLoader.LoadCandii());

    public int ResolveGlyph(InventoryInspectionCell cell) => ResolveGlyph(cell.PresentationId, cell.Glyph);

    public int ResolveGlyph(EntityInspectionAppearance appearance) => ResolveGlyph(appearance.PresentationId, appearance.Glyph);

    public int ResolveGlyph(PresentationId? presentationId, char fallbackGlyph)
    {
        if (presentationId is { } id && _glyphsByPresentationId.TryGetValue(id.Value, out var glyph))
        {
            return glyph;
        }

        return fallbackGlyph;
    }

    public EntityInspectionAppearance ResolveAppearance(EntityInspectionAppearance appearance)
    {
        var glyph = ResolveGlyph(appearance);
        return glyph == appearance.Glyph
            ? appearance
            : appearance with { Glyph = (char)glyph };
    }
}

namespace Forzion.Game;

/// <summary>
/// Where the bottom of the HUD goes, in pixels: the selection panel along the bottom edge and
/// the minimap's frame in the bottom right corner beside it, both as tall.
/// </summary>
public static class HudLayout
{
    /// <summary>Height of the bottom of the HUD: the selection panel and the minimap's frame.</summary>
    public const int BottomHeight = 210;

    /// <summary>Width of the box the minimap draws the map in. A 64 by 48 map fills it at 4 pixels per Cell.</summary>
    public const int MinimapBoxWidth = 256;

    /// <summary>Height of the box the minimap draws the map in.</summary>
    public const int MinimapBoxHeight = 192;

    /// <summary>Room around the minimap's box inside its frame, the same on every side.</summary>
    public const int MinimapMargin = (BottomHeight - MinimapBoxHeight) / 2;

    /// <summary>Width of the minimap's frame; the selection panel stops short of it.</summary>
    public const int MinimapWidth = MinimapBoxWidth + (2 * MinimapMargin);
}

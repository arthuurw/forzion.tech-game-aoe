using Forzion.Simulation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// The colours that mean the same wherever they show: on the 3D map, on the minimap and in
/// the HUD and the screens over the match. A Resource or a Player reads alike in all of them.
/// </summary>
public static class Palette
{
    /// <summary>The ground of the map.</summary>
    public static readonly Color Ground = new(0.36f, 0.52f, 0.25f);

    /// <summary>A Forest.</summary>
    public static readonly Color Forest = new(0.1f, 0.3f, 0.12f);

    /// <summary>Water.</summary>
    public static readonly Color Water = new(0.2f, 0.42f, 0.75f);

    /// <summary>The background of the HUD's bars and panels, and of the minimap's frame.</summary>
    public static readonly Color Panel = new(0.08f, 0.09f, 0.11f, 0.88f);

    /// <summary>The colour of a heading, and of the Faction and Age in the top bar.</summary>
    public static readonly Color Heading = new(1f, 0.95f, 0.8f);

    /// <summary>What belongs to no Player of the palette.</summary>
    private static readonly Color Neutral = new(0.6f, 0.6f, 0.6f);

    // Violet and orange: neither is the blue of Water, the greens of the ground and Forests,
    // nor the colour of a Resource.
    private static readonly Color[] PlayerColours =
    [
        new(0.6f, 0.3f, 0.9f),
        new(0.95f, 0.5f, 0.1f),
    ];

    /// <summary>The colour that marks what a Player owns.</summary>
    public static Color ColourOf(PlayerId player) =>
        player.Value >= 1 && player.Value <= PlayerColours.Length ? PlayerColours[player.Value - 1] : Neutral;

    /// <summary>
    /// The colour of a Resource: its amount in the top bar, its sources on the minimap, and
    /// the berries, logs and nuggets of its sources on the map.
    /// </summary>
    public static Color ColourOf(ResourceKind kind) => kind switch
    {
        ResourceKind.Food => new Color(0.9f, 0.2f, 0.28f),
        ResourceKind.Wood => new Color(0.7f, 0.47f, 0.24f),
        ResourceKind.Gold => new Color(1f, 0.82f, 0.2f),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown Resource."),
    };
}

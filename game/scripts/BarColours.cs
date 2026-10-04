using Godot;

namespace Forzion.Game;

/// <summary>
/// The colours of the bars drawn over the map and in the HUD's panel, shared so that a bar
/// reads alike in both places.
/// </summary>
public static class BarColours
{
    /// <summary>The colour of a construction bar.</summary>
    public static readonly Color Construction = new(0.95f, 0.7f, 0.25f);

    /// <summary>The colour of a hit point bar: green when whole, through yellow, to red when nearly dead.</summary>
    /// <param name="fraction">The share of its hit points the entity has left, from 0 to 1.</param>
    public static Color HitPoints(double fraction) =>
        fraction > 0.5
            ? new Color(0.35f, 0.85f, 0.35f).Lerp(new Color(0.95f, 0.85f, 0.25f), (float)((1 - fraction) * 2))
            : new Color(0.95f, 0.85f, 0.25f).Lerp(new Color(0.9f, 0.2f, 0.15f), (float)((0.5 - fraction) * 2));
}

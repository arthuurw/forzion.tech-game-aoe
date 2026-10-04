using Forzion.Presentation;
using Forzion.Simulation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// Draws the bars over the entities on the map that <see cref="WorldBars"/> names: hit points
/// over selected and wounded units and buildings, construction over construction sites. Each bar is drawn
/// on the screen above where the entity stands, so it keeps its size at any zoom.
/// </summary>
public partial class WorldBarsOverlay : Control
{
    private const float UnitBarWidth = 30;
    private const float BuildingBarWidth = 56;
    private const float BarHeight = 5;

    // How high above the ground each bar hangs, in world units: just over the placeholder shapes.
    private const float AboveUnit = 1.25f;
    private const float AboveBuilding = 1.6f;

    private static readonly Color Background = new(0, 0, 0, 0.7f);

    /// <summary>The match whose entities get bars.</summary>
    [Export]
    public MatchView MatchView { get; set; } = null!;

    /// <summary>The input whose selected entities show their hit points.</summary>
    [Export]
    public SelectionInput SelectionInput { get; set; } = null!;

    /// <summary>The camera the person looks through.</summary>
    [Export]
    public Camera3D Camera { get; set; } = null!;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        var state = MatchView.State;

        // Bars of one entity stack upward: hit points at the bottom, construction above.
        var stacked = new Dictionary<EntityId, int>();

        foreach (var bar in WorldBars.Of(state, SelectionInput.PlayerControl.Selected))
        {
            var (anchor, width) = AnchorOf(state, bar.Entity);

            if (anchor is not { } world || Camera.IsPositionBehind(world))
            {
                continue;
            }

            var row = stacked.GetValueOrDefault(bar.Entity);
            stacked[bar.Entity] = row + 1;

            var centre = Camera.UnprojectPosition(world) - new Vector2(0, row * (BarHeight + 2));
            var whole = new Rect2(centre.X - (width / 2), centre.Y - (BarHeight / 2), width, BarHeight);
            var fill = (float)bar.Fill;
            var colour = bar.Kind switch
            {
                WorldBarKind.HitPoints => BarColours.HitPoints(bar.Fill),
                WorldBarKind.Construction => BarColours.Construction,
                _ => throw new ArgumentOutOfRangeException(nameof(bar), bar.Kind, "Unknown bar."),
            };

            DrawRect(whole.Grow(1), Background);
            DrawRect(new Rect2(whole.Position, new Vector2(whole.Size.X * fill, whole.Size.Y)), colour);
        }
    }

    /// <summary>The world point the entity's bars hang from and how wide they are; no point when the entity is gone.</summary>
    private (Vector3? Anchor, float Width) AnchorOf(MatchState state, EntityId id)
    {
        return state.FindUnitOrBuilding(id) switch
        {
            ({ } unit, _) => (WorldSpace.ToWorld(MatchView.Driver.PositionOf(unit), AboveUnit), UnitBarWidth),
            (_, { } building) => (WorldSpace.CentreOf(building, AboveBuilding), BuildingBarWidth),
            _ => (null, 0),
        };
    }
}

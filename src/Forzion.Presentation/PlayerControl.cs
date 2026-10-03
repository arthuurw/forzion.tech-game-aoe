using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// What the person at the screen does with the mouse, turned into a selection of their
/// Player's entities and into commands for the simulation. It decides no game rule
/// (ADR 0001): whether a command is valid is for the match to say when it applies it.
/// </summary>
public sealed class PlayerControl
{
    /// <summary>
    /// How far, in pixels, the mouse moves with the button down before a click becomes a box.
    /// A hand never holds the mouse perfectly still.
    /// </summary>
    public const double DragThreshold = 6;

    private readonly PlayerId player;
    private readonly Func<ScreenPoint, SightLine?> sightThrough;
    private readonly Picker picker;
    private readonly List<EntityId> selected = [];

    /// <param name="driver">Drives the match: where its units are drawn and where commands go.</param>
    /// <param name="player">The Player the person at the screen controls.</param>
    /// <param name="sightThrough">
    /// The camera's line of sight through a point of the screen, or null when that point looks
    /// above the horizon and never meets the ground.
    /// </param>
    /// <param name="sizes">The size of the shapes drawn for the entities.</param>
    public PlayerControl(MatchDriver driver, PlayerId player, Func<ScreenPoint, SightLine?> sightThrough, PickSizes sizes)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(sightThrough);
        ArgumentNullException.ThrowIfNull(sizes);

        this.player = player;
        this.sightThrough = sightThrough;
        picker = new Picker(driver, sizes);
    }

    /// <summary>The selected entities, in ascending ID order.</summary>
    public IReadOnlyList<EntityId> Selected => selected;

    /// <summary>
    /// Whether the mouse, pressed at one point and now at the other, makes a click rather than
    /// a box: it has moved less than <see cref="DragThreshold"/> pixels either way.
    /// </summary>
    public bool IsClick(ScreenPoint pressedAt, ScreenPoint currentlyAt) =>
        Math.Abs(currentlyAt.X - pressedAt.X) < DragThreshold && Math.Abs(currentlyAt.Y - pressedAt.Y) < DragThreshold;

    /// <summary>
    /// Replaces the selection with what the mouse picks between press and release. A click
    /// selects the Player's unit or building where the button went down; a box drawn on
    /// screen selects every unit of the Player inside it and no building, as in other strategy
    /// games. Picking nothing of the Player's leaves the selection empty.
    /// </summary>
    public void Select(ScreenPoint pressedAt, ScreenPoint releasedAt)
    {
        selected.Clear();

        if (IsClick(pressedAt, releasedAt))
        {
            SelectAt(pressedAt);
        }
        else
        {
            SelectInBox(pressedAt, releasedAt);
        }
    }

    private void SelectInBox(ScreenPoint corner, ScreenPoint opposite)
    {
        ScreenPoint[] corners =
        [
            corner,
            new(opposite.X, corner.Y),
            opposite,
            new(corner.X, opposite.Y),
        ];

        var sights = corners.Select(sightThrough).ToList();

        // A corner above the horizon marks out no area on the ground.
        if (sights.Any(sight => sight is null))
        {
            return;
        }

        selected.AddRange(picker
            .UnitsInside(sights.Select(sight => sight!.Value).ToList())
            .Where(unit => unit.Owner == player)
            .Select(unit => unit.Id));
    }

    private void SelectAt(ScreenPoint point)
    {
        var sight = sightThrough(point);

        if (sight is null)
        {
            return;
        }

        // Only what the Player owns can be selected: the selection is what the Player commands.
        switch (picker.At(sight.Value))
        {
            case UnitState unit when unit.Owner == player:
                selected.Add(unit.Id);
                break;
            case BuildingState building when building.Owner == player:
                selected.Add(building.Id);
                break;
        }
    }
}

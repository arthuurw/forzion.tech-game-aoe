using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// What the person at the screen does with the mouse, turned into a selection of their
/// Player's entities and into commands for the simulation. It decides no game rule
/// (ADR 0001): whether a command is valid is for the match to say when it applies it.
/// </summary>
public sealed class PlayerControl
{
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

    /// <summary>Selects what the mouse pressed and released at.</summary>
    public void Select(ScreenPoint pressedAt, ScreenPoint releasedAt)
    {
        selected.Clear();

        var sight = sightThrough(releasedAt);

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

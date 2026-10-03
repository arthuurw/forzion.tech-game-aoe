using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// What the person at the screen does with the mouse and the HUD's buttons, turned into a
/// selection of their Player's entities and into commands for the simulation. It decides no game rule
/// (ADR 0001): whether a command is valid is for the match to say when it applies it.
/// </summary>
public sealed class PlayerControl
{
    /// <summary>
    /// How far, in pixels, the mouse moves with the button down before a click becomes a box.
    /// A hand never holds the mouse perfectly still.
    /// </summary>
    public const double DragThreshold = 6;

    private readonly MatchDriver driver;
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

        this.driver = driver;
        this.player = player;
        this.sightThrough = sightThrough;
        picker = new Picker(driver, sizes);
    }

    /// <summary>
    /// The selected entities, in ascending ID order. A selected unit that dies, or building
    /// that is destroyed, leaves the selection for good.
    /// </summary>
    public IReadOnlyList<EntityId> Selected
    {
        get
        {
            var state = driver.Match.State;

            selected.RemoveAll(id =>
                !state.Units.Any(unit => unit.Id == id) && !state.Buildings.Any(building => building.Id == id));

            return selected;
        }
    }

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

    /// <summary>
    /// Sends the selected units of the Player the order that fits what the mouse points at,
    /// as a command the next tick applies: gather from a resource source, attack a unit or
    /// building of another Player, build an unfinished building of the Player, otherwise walk
    /// to the Cell under the mouse. With a building of the Player selected instead, the Cell
    /// under the mouse becomes its rally point. Nothing is sent while nothing is selected. The
    /// command goes out even when the match will refuse it; the refusal comes back as a
    /// <see cref="CommandRejected"/> event.
    /// </summary>
    public void OrderAt(ScreenPoint point)
    {
        if (sightThrough(point) is not { } sight)
        {
            return;
        }

        var units = selected.Where(IsUnitOfPlayer).ToList();
        var ground = CellUnder(sight.Ground);

        if (units.Count == 0)
        {
            SendToSelectedBuilding(building => new SetRallyPointCommand(player, building, ground));

            return;
        }

        driver.Match.Enqueue(OrderFor(units, picker.At(sight), ground));
    }

    /// <summary>
    /// The kind of building the person has chosen to place and is now pointing where, or null
    /// while no building is being placed.
    /// </summary>
    public BuildingKind? PlacingBuilding { get; private set; }

    /// <summary>
    /// Puts a unit of the given kind at the end of the training queue of the selected building.
    /// Nothing is sent while no building of the Player is selected.
    /// </summary>
    public void Train(UnitKind kind) => SendToSelectedBuilding(building => new TrainCommand(player, building, kind));

    /// <summary>
    /// Takes the unit at <paramref name="position"/> off the training queue of the selected
    /// building: 0 for the one in training. Nothing is sent while no building of the Player is selected.
    /// </summary>
    public void CancelTraining(int position) =>
        SendToSelectedBuilding(building => new CancelTrainingCommand(player, building, position));

    /// <summary>
    /// Orders the Age Advance at the selected building, the Town Center. Nothing is sent while
    /// no building of the Player is selected.
    /// </summary>
    public void AdvanceAge() => SendToSelectedBuilding(building => new AgeAdvanceCommand(player, building));

    /// <summary>
    /// Starts placing a building of the given kind: from now on <see cref="PlacementAt"/> tells
    /// where it would go, until <see cref="PlaceAt"/> places it or <see cref="CancelPlacement"/> gives up.
    /// </summary>
    public void ChooseBuilding(BuildingKind kind) => PlacingBuilding = kind;

    /// <summary>Gives up placing the chosen building.</summary>
    public void CancelPlacement() => PlacingBuilding = null;

    /// <summary>
    /// Where the chosen building would go with the mouse at <paramref name="point"/>: its
    /// footprint centred on the point, and whether the match would place it there now. Null
    /// while no building is chosen or when the point looks above the horizon.
    /// </summary>
    public BuildingPlacement? PlacementAt(ScreenPoint point)
    {
        if (PlacingBuilding is not { } kind || sightThrough(point) is not { } sight)
        {
            return null;
        }

        var size = Match.BuildingSize(kind);

        // The footprint whose centre is nearest the point: half a footprint back, rounded to the nearest Cell.
        var origin = new CellPosition(
            (int)Math.Floor(sight.Ground.X - (size / 2.0) + 0.5),
            (int)Math.Floor(sight.Ground.Y - (size / 2.0) + 0.5));

        return new BuildingPlacement(kind, origin, size, driver.Match.CanPlace(kind, origin));
    }

    /// <summary>
    /// Places the chosen building where <see cref="PlacementAt"/> shows it, with the selected
    /// units as builders, and stops placing. The command goes out even when the preview is
    /// invalid: the match refuses it and says why. Nothing is sent while no building is chosen.
    /// </summary>
    public void PlaceAt(ScreenPoint point)
    {
        if (PlacementAt(point) is not { } placement)
        {
            return;
        }

        // The match sends the Villagers among the builders and leaves the others be.
        var builders = selected.Where(IsUnitOfPlayer).ToList();

        driver.Match.Enqueue(new PlaceBuildingCommand(player, placement.Kind, placement.Origin, builders));
        PlacingBuilding = null;
    }

    private void SendToSelectedBuilding(Func<EntityId, Command> commandFor)
    {
        var state = driver.Match.State;
        var building = Selected
            .Where(id => state.Buildings.Any(each => each.Id == id && each.Owner == player))
            .Select(id => (EntityId?)id)
            .FirstOrDefault();

        if (building is { } id)
        {
            driver.Match.Enqueue(commandFor(id));
        }
    }

    /// <summary>The command a right-click on <paramref name="target"/> gives.</summary>
    private Command OrderFor(IReadOnlyList<EntityId> units, object? target, CellPosition ground) => target switch
    {
        ResourceSourceState source => new GatherCommand(player, units, source.Id),
        UnitState enemy when enemy.Owner != player => new AttackCommand(player, units, enemy.Id),
        BuildingState enemy when enemy.Owner != player => new AttackCommand(player, units, enemy.Id),
        BuildingState site when !site.IsComplete => new BuildCommand(player, units, site.Id),
        _ => new MoveCommand(player, units, ground),
    };

    private bool IsUnitOfPlayer(EntityId id) =>
        driver.Match.State.Units.Any(unit => unit.Id == id && unit.Owner == player);

    private static CellPosition CellUnder(MapPoint point) => new((int)Math.Floor(point.X), (int)Math.Floor(point.Y));

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

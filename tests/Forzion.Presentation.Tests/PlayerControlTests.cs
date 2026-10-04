using Forzion.Simulation;
using static Forzion.Presentation.Tests.TestMatches;

namespace Forzion.Presentation.Tests;

public class PlayerControlTests
{
    // Units 0.4 Cell across and 1 tall, buildings and resource sources 1.2 tall.
    private static readonly PickSizes Sizes = new(UnitRadius: 0.4, UnitHeight: 1, BuildingAndSourceHeight: 1.2);

    private MatchDriver driver = null!;

    [Fact]
    public void Clicking_a_unit_of_the_Player_selects_it()
    {
        var control = NewControl(out var match);
        var villager = UnitsOf(match, FirstPlayer)[0];

        control.Select(Over(villager.Position), Over(villager.Position));

        Assert.Equal([villager.Id], control.Selected);
    }

    // Seen from a slanted camera, the line of sight through the head of a unit meets the
    // ground well behind its feet.
    [Fact]
    public void Clicking_the_head_of_a_unit_seen_from_a_slanted_camera_selects_it()
    {
        var control = NewControl(out var match, Slanted);
        var villager = UnitsOf(match, FirstPlayer)[0];
        var behindItsFeet = new ScreenPoint(villager.Position.X.ToDouble(), villager.Position.Y.ToDouble() - 0.6);

        control.Select(behindItsFeet, behindItsFeet);

        Assert.Equal([villager.Id], control.Selected);
    }

    [Fact]
    public void Clicking_a_unit_of_another_Player_selects_nothing()
    {
        var control = NewControl(out var match);
        var enemy = UnitsOf(match, SecondPlayer)[0];

        control.Select(Over(enemy.Position), Over(enemy.Position));

        Assert.Empty(control.Selected);
    }

    [Fact]
    public void Clicking_bare_ground_clears_the_selection()
    {
        var control = NewControl(out var match);
        var villager = UnitsOf(match, FirstPlayer)[0];
        control.Select(Over(villager.Position), Over(villager.Position));
        var bareGround = Over(MapPosition.CentreOf(FreeCellAwayFromUnits(match)));

        control.Select(bareGround, bareGround);

        Assert.Empty(control.Selected);
    }

    [Fact]
    public void Clicking_a_building_of_the_Player_selects_it()
    {
        var control = NewControl(out var match);
        var townCenter = match.State.Buildings.First(building => building.Owner == FirstPlayer);
        var middle = new ScreenPoint(townCenter.Origin.X + (townCenter.Width / 2.0), townCenter.Origin.Y + (townCenter.Height / 2.0));

        control.Select(middle, middle);

        Assert.Equal([townCenter.Id], control.Selected);
    }

    // Seen from a slanted camera, the line of sight through the top of a building meets the
    // ground in the Cell behind it.
    [Fact]
    public void Clicking_the_top_of_a_building_seen_from_a_slanted_camera_selects_it()
    {
        var control = NewControl(out var match, Slanted);
        var townCenter = match.State.Buildings.First(building => building.Owner == FirstPlayer);
        var behindIt = new ScreenPoint(townCenter.Origin.X + 1.5, townCenter.Origin.Y - 0.5);

        control.Select(behindIt, behindIt);

        Assert.Equal([townCenter.Id], control.Selected);
    }

    [Fact]
    public void A_unit_standing_in_front_of_a_building_is_picked_before_it()
    {
        var control = NewControl(out var match, Slanted);
        var villager = UnitsOf(match, FirstPlayer)[0];
        var townCenter = match.State.Buildings.First(building => building.Owner == FirstPlayer);
        // The Villagers start on the row just past the Town Center's side facing the camera, so
        // the line of sight through a Villager's head goes on to meet the building.
        var throughBoth = new ScreenPoint(villager.Position.X.ToDouble(), townCenter.Origin.Y + townCenter.Height - 0.1);

        control.Select(throughBoth, throughBoth);

        Assert.Equal([villager.Id], control.Selected);
    }

    [Fact]
    public void Dragging_a_box_selects_the_units_of_the_Player_inside_it_and_no_building()
    {
        var control = NewControl(out var match);
        var villagers = UnitsOf(match, FirstPlayer);
        var townCenter = match.State.Buildings.First(building => building.Owner == FirstPlayer);
        var (from, to) = BoxAround(villagers.Select(unit => unit.Position).Append(MapPosition.CentreOf(townCenter.Origin)));

        control.Select(from, to);

        Assert.Equal(villagers.Select(unit => unit.Id), control.Selected);
    }

    [Fact]
    public void Dragging_a_box_from_any_corner_selects_the_same_units()
    {
        var control = NewControl(out var match);
        var villagers = UnitsOf(match, FirstPlayer);
        var (from, to) = BoxAround(villagers.Select(unit => unit.Position));

        control.Select(new ScreenPoint(to.X, from.Y), new ScreenPoint(from.X, to.Y));

        Assert.Equal(villagers.Select(unit => unit.Id), control.Selected);
    }

    [Fact]
    public void Dragging_a_box_around_units_of_another_Player_clears_the_selection()
    {
        var control = NewControl(out var match);
        var villager = UnitsOf(match, FirstPlayer)[0];
        control.Select(Over(villager.Position), Over(villager.Position));
        var (from, to) = BoxAround(UnitsOf(match, SecondPlayer).Select(unit => unit.Position));

        control.Select(from, to);

        Assert.Empty(control.Selected);
    }

    // Seen from a slanted camera the units' bodies stand out over the ground: a box drawn
    // around them on screen ends short of their feet.
    [Fact]
    public void A_box_drawn_around_the_bodies_of_units_seen_from_a_slanted_camera_selects_them()
    {
        var control = NewControl(out var match, Slanted);
        var villagers = UnitsOf(match, FirstPlayer);
        var (from, to) = BoxAround(villagers.Select(unit => unit.Position));
        // The Villagers start side by side on one row. The middle of their bodies, half a unit
        // up, shows 0.35 Cell short of their feet: the box spans from 0.6 short of the feet,
        // short of their heads, to 0.1 short of the feet.
        var feet = villagers[0].Position.Y.ToDouble();

        control.Select(new ScreenPoint(from.X, feet - 0.6), new ScreenPoint(to.X, feet - 0.1));

        Assert.Equal(villagers.Select(unit => unit.Id), control.Selected);
    }

    [Fact]
    public void A_mouse_that_barely_moves_between_press_and_release_clicks()
    {
        var control = NewControl(out var match);
        var villager = UnitsOf(match, FirstPlayer)[0];
        var pressedAt = Over(villager.Position);

        control.Select(pressedAt, new ScreenPoint(pressedAt.X + 2, pressedAt.Y + 2));

        Assert.True(control.IsClick(pressedAt, new ScreenPoint(pressedAt.X + 2, pressedAt.Y + 2)));
        Assert.Equal([villager.Id], control.Selected);
    }

    [Fact]
    public void Right_clicking_bare_ground_sends_the_selected_units_walking_to_that_Cell()
    {
        var control = NewControl(out var match);
        var villagers = UnitsOf(match, FirstPlayer);
        var (from, to) = BoxAround(villagers.Select(unit => unit.Position));
        control.Select(from, to);
        var destination = FreeCellAwayFromUnits(match);

        control.OrderAt(Over(MapPosition.CentreOf(destination)));
        Tick(match);

        Assert.All(villagers, villager => Assert.Equal(destination, villager.Path[^1]));
    }

    [Fact]
    public void Right_clicking_a_resource_source_sends_the_selected_Villagers_to_gather_from_it()
    {
        var control = NewControl(out var match);
        var villagers = UnitsOf(match, FirstPlayer);
        var (from, to) = BoxAround(villagers.Select(unit => unit.Position));
        control.Select(from, to);
        var source = match.State.ResourceSources[0];

        control.OrderAt(Over(MapPosition.CentreOf(source.Cell)));
        Tick(match);

        Assert.All(villagers, villager => Assert.Equal(source.Id, villager.GatherSource));
    }

    // Seen from a slanted camera, the line of sight through the top of a resource source
    // meets the ground in the Cell behind it.
    [Fact]
    public void Right_clicking_the_top_of_a_resource_source_seen_from_a_slanted_camera_gathers_from_it()
    {
        var control = NewControl(out var match, Slanted);
        var villager = UnitsOf(match, FirstPlayer)[0];
        control.Select(Over(villager.Position), Over(villager.Position));
        var source = match.State.ResourceSources.First(source =>
            source.Cell.Y > 0 && match.State.Map[source.Cell with { Y = source.Cell.Y - 1 }] == CellKind.Free);

        control.OrderAt(new ScreenPoint(source.Cell.X + 0.5, source.Cell.Y - 0.2));
        Tick(match);

        Assert.Equal(source.Id, villager.GatherSource);
    }

    [Fact]
    public void Right_clicking_with_no_unit_selected_gives_no_order()
    {
        var control = NewControl(out var match);
        var townCenter = match.State.Buildings.First(building => building.Owner == FirstPlayer);
        var middle = new ScreenPoint(townCenter.Origin.X + 1.5, townCenter.Origin.Y + 1.5);
        control.Select(middle, middle);

        control.OrderAt(new ScreenPoint(-5, -5));
        var events = Tick(match);

        Assert.Empty(events);
        Assert.DoesNotContain(match.State.Units, unit => unit.IsMoving);
    }

    // Whether an order can be carried out is for the match to say, not for the controls.
    [Fact]
    public void An_order_the_match_refuses_is_still_sent_and_comes_back_rejected()
    {
        var control = NewControl(out var match);
        var villager = UnitsOf(match, FirstPlayer)[0];
        control.Select(Over(villager.Position), Over(villager.Position));

        control.OrderAt(new ScreenPoint(-5, -5));
        var events = Tick(match);

        var rejection = Assert.IsType<CommandRejected>(Assert.Single(events));
        Assert.Equal(RejectionReason.DestinationOutsideMap, rejection.Reason);
    }

    [Fact]
    public void Right_clicking_an_enemy_unit_sends_the_selected_soldiers_to_attack_it()
    {
        var control = NewControl(out var match, config: WithSoldier());
        var soldier = SoldierOf(match);
        control.Select(Over(soldier.Position), Over(soldier.Position));
        var enemy = UnitsOf(match, SecondPlayer)[0];

        control.OrderAt(Over(enemy.Position));
        Tick(match);

        Assert.Equal(enemy.Id, soldier.Target);
    }

    [Fact]
    public void Right_clicking_an_enemy_unit_with_Villagers_and_soldiers_selected_sends_the_soldiers_to_attack_it()
    {
        var control = NewControl(out var match, config: WithSoldier());
        var units = UnitsOf(match, FirstPlayer);
        var soldier = SoldierOf(match);
        var (from, to) = BoxAround(units.Select(unit => unit.Position));
        control.Select(from, to);
        Assert.Equal(units.Select(unit => unit.Id), control.Selected);
        var enemy = UnitsOf(match, SecondPlayer)[0];

        control.OrderAt(Over(enemy.Position));
        var events = Tick(match);

        Assert.DoesNotContain(events, matchEvent => matchEvent is CommandRejected);
        Assert.Equal(enemy.Id, soldier.Target);
        Assert.All(units.Where(unit => unit.Kind == UnitKind.Villager), villager => Assert.Null(villager.Target));
    }

    [Fact]
    public void Right_clicking_an_enemy_building_sends_the_selected_soldiers_to_attack_it()
    {
        var control = NewControl(out var match, config: WithSoldier());
        var soldier = SoldierOf(match);
        control.Select(Over(soldier.Position), Over(soldier.Position));
        var townCenter = match.State.Buildings.First(building => building.Owner == SecondPlayer);

        control.OrderAt(new ScreenPoint(townCenter.Origin.X + 1.5, townCenter.Origin.Y + 1.5));
        Tick(match);

        Assert.Equal(townCenter.Id, soldier.Target);
    }

    [Fact]
    public void Right_clicking_an_enemy_unit_with_only_Villagers_selected_sends_them_walking_to_it()
    {
        var control = NewControl(out var match);
        var villager = UnitsOf(match, FirstPlayer)[0];
        control.Select(Over(villager.Position), Over(villager.Position));
        var enemy = UnitsOf(match, SecondPlayer)[0];

        control.OrderAt(Over(enemy.Position));
        var events = Tick(match);

        Assert.DoesNotContain(events, matchEvent => matchEvent is CommandRejected);
        Assert.Equal(enemy.Position.Cell, villager.Path[^1]);
    }

    [Fact]
    public void Right_clicking_an_enemy_building_with_only_Villagers_selected_sends_them_walking_up_to_it()
    {
        var control = NewControl(out var match);
        var villager = UnitsOf(match, FirstPlayer)[0];
        control.Select(Over(villager.Position), Over(villager.Position));
        var townCenter = match.State.Buildings.First(building => building.Owner == SecondPlayer);

        control.OrderAt(new ScreenPoint(townCenter.Origin.X + 1.5, townCenter.Origin.Y + 1.5));
        var events = Tick(match);

        Assert.DoesNotContain(events, matchEvent => matchEvent is CommandRejected);
        Assert.True(villager.IsMoving);
        Assert.Null(villager.Target);
    }

    [Fact]
    public void Right_clicking_an_enemy_site_with_only_Villagers_selected_sends_them_walking_up_to_it()
    {
        var control = NewControl(out var match);
        var site = PlaceHouse(match, SecondPlayer);
        var villager = UnitsOf(match, FirstPlayer)[0];
        control.Select(Over(villager.Position), Over(villager.Position));

        control.OrderAt(new ScreenPoint(site.Origin.X + (site.Width / 2.0), site.Origin.Y + (site.Height / 2.0)));
        var events = Tick(match);

        Assert.DoesNotContain(events, matchEvent => matchEvent is CommandRejected);
        Assert.True(villager.IsMoving);
        Assert.Null(villager.ConstructionSite);
    }

    [Fact]
    public void Right_clicking_an_unfinished_building_of_the_Player_sends_the_selected_Villagers_to_build_it()
    {
        var control = NewControl(out var match);
        var site = PlaceHouse(match);
        var villager = UnitsOf(match, FirstPlayer)[0];
        control.Select(Over(villager.Position), Over(villager.Position));

        control.OrderAt(new ScreenPoint(site.Origin.X + (site.Width / 2.0), site.Origin.Y + (site.Height / 2.0)));
        Tick(match);

        Assert.Equal(site.Id, villager.ConstructionSite);
    }

    [Fact]
    public void A_selected_unit_that_dies_leaves_the_selection()
    {
        var control = NewControl(out var match, config: WithEnemySoldierAtHome());
        var villagers = UnitsOf(match, FirstPlayer);
        var (from, to) = BoxAround(villagers.Select(unit => unit.Position));
        control.Select(from, to);
        Assert.Equal(villagers.Select(unit => unit.Id), control.Selected);

        while (UnitsOf(match, FirstPlayer).Count == villagers.Count)
        {
            Tick(match);
        }

        Assert.Equal(UnitsOf(match, FirstPlayer).Select(unit => unit.Id), control.Selected);
    }

    /// <summary>
    /// Has the Player's Villagers gather the Wood for a House, stops them, and places the House
    /// with no builder on the free spot nearest its Town Center. Returns the unfinished House.
    /// </summary>
    private BuildingState PlaceHouse(Match match, PlayerId? owner = null)
    {
        var player = owner ?? FirstPlayer;
        var state = match.State;
        var villagers = UnitsOf(match, player);
        var townCenter = state.Buildings.First(building => building.Owner == player);
        var wood = state.ResourceSources
            .Where(source => source.Kind == ResourceKind.Wood)
            .OrderBy(source => Math.Abs(source.Cell.X - townCenter.Origin.X) + Math.Abs(source.Cell.Y - townCenter.Origin.Y))
            .First();
        match.Enqueue(new GatherCommand(player, villagers.Select(unit => unit.Id).ToList(), wood.Id));

        while (state.Players.First(each => each.Id == player).AmountOf(ResourceKind.Wood) < Match.BuildingCost(BuildingKind.House).Wood)
        {
            Tick(match);
        }

        match.Enqueue(new MoveCommand(player, villagers.Select(unit => unit.Id).ToList(), villagers[0].Position.Cell));

        while (villagers.Any(unit => unit.IsMoving))
        {
            Tick(match);
        }

        var origin = Enumerable.Range(0, state.Map.Height)
            .SelectMany(y => Enumerable.Range(0, state.Map.Width).Select(x => new CellPosition(x, y)))
            .Where(cell => match.CanPlace(BuildingKind.House, cell))
            .OrderBy(cell => Math.Abs(cell.X - townCenter.Origin.X) + Math.Abs(cell.Y - townCenter.Origin.Y))
            .ThenBy(cell => cell.Y)
            .ThenBy(cell => cell.X)
            .First();
        match.Enqueue(new PlaceBuildingCommand(player, BuildingKind.House, origin, []));
        Tick(match);

        var house = state.Buildings[^1];
        Assert.Equal(BuildingKind.House, house.Kind);
        Assert.False(house.IsComplete);

        return house;
    }

    private PlayerControl NewControl(out Match match, Func<ScreenPoint, SightLine?>? camera = null, MatchConfig? config = null)
    {
        driver = NewDriver(config);
        match = driver.Match;

        return new PlayerControl(driver, FirstPlayer, camera ?? TopDown, Sizes);
    }

    /// <summary>The plain match, with the first Player starting with a melee soldier beside its Town Center.</summary>
    private static MatchConfig WithSoldier() => PlainConfig(firstExtras: [new StartingUnit(UnitKind.MeleeSoldier, BesideFirstHome())]);

    private static UnitState SoldierOf(Match match) =>
        match.State.Units.Single(unit => unit.Owner == FirstPlayer && unit.Kind == UnitKind.MeleeSoldier);

    /// <summary>Runs one tick of the match, applying the commands sent so far, and returns its events.</summary>
    private IReadOnlyList<MatchEvent> Tick(Match match)
    {
        Assert.Same(driver.Match, match);

        return driver.Advance(1.0 / Match.TicksPerSecond);
    }

    // A camera looking straight down: one pixel of the screen is one Cell of the map.
    private static SightLine? TopDown(ScreenPoint point) => new SightLine(new MapPoint(point.X, point.Y), new MapPoint(0, 0));

    // A camera looking down towards decreasing Y, like the game's: one pixel of the screen is
    // one Cell of the ground, and the line of sight moves 0.7 Cell towards the camera for each
    // unit it rises.
    private static SightLine? Slanted(ScreenPoint point) => new SightLine(new MapPoint(point.X, point.Y), new MapPoint(0, 0.7));

    private static ScreenPoint Over(MapPosition position) => new(position.X.ToDouble(), position.Y.ToDouble());

    /// <summary>
    /// The corners of a box on the top-down screen around the positions, with 3 Cells to spare
    /// across and 1 up and down: wide enough to count as a drag, not a click.
    /// </summary>
    private static (ScreenPoint From, ScreenPoint To) BoxAround(IEnumerable<MapPosition> positions)
    {
        var points = positions.Select(Over).ToList();

        return (new ScreenPoint(points.Min(point => point.X) - 3, points.Min(point => point.Y) - 1),
                new ScreenPoint(points.Max(point => point.X) + 3, points.Max(point => point.Y) + 1));
    }

    /// <summary>A free Cell with nothing beside it and no unit within 3 Cells.</summary>
    private static CellPosition FreeCellAwayFromUnits(Match match)
    {
        var map = match.State.Map;

        for (var y = 1; y < map.Height - 1; y++)
        {
            for (var x = 1; x < map.Width - 1; x++)
            {
                var cell = new CellPosition(x, y);
                var clear = Neighbourhood(cell).All(near => map[near] == CellKind.Free);
                var farFromUnits = match.State.Units.All(unit =>
                    Math.Abs(unit.Position.Cell.X - x) > 3 || Math.Abs(unit.Position.Cell.Y - y) > 3);

                if (clear && farFromUnits)
                {
                    return cell;
                }
            }
        }

        throw new InvalidOperationException("The map has no free Cell away from units.");
    }

    private static IEnumerable<CellPosition> Neighbourhood(CellPosition cell)
    {
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                yield return new CellPosition(cell.X + dx, cell.Y + dy);
            }
        }
    }
}

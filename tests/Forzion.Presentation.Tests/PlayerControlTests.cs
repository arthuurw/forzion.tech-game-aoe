using Forzion.Simulation;

namespace Forzion.Presentation.Tests;

public class PlayerControlTests
{
    private static readonly PlayerId FirstPlayer = new(1);
    private static readonly PlayerId SecondPlayer = new(2);

    // Units 0.4 Cell across and 1 tall, buildings and resource sources 1.2 tall.
    private static readonly PickSizes Sizes = new(UnitRadius: 0.4, UnitHeight: 1, StructureHeight: 1.2);

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

    private static PlayerControl NewControl(out Match match, Func<ScreenPoint, SightLine?>? camera = null)
    {
        var faction = new FactionId(1);
        var config = new MatchConfig(42, new MapConfig(64, 48), [new PlayerConfig(faction), new PlayerConfig(faction)]);
        var driver = new MatchDriver(Match.Create(config), new TickClock(Match.TicksPerSecond));
        match = driver.Match;

        return new PlayerControl(driver, FirstPlayer, camera ?? TopDown, Sizes);
    }

    // A camera looking straight down: one pixel of the screen is one Cell of the map.
    private static SightLine? TopDown(ScreenPoint point) => new SightLine(new MapPoint(point.X, point.Y), new MapPoint(0, 0));

    // A camera looking down towards decreasing Y, like the game's: one pixel of the screen is
    // one Cell of the ground, and the line of sight moves 0.7 Cell towards the camera for each
    // unit it rises.
    private static SightLine? Slanted(ScreenPoint point) => new SightLine(new MapPoint(point.X, point.Y), new MapPoint(0, 0.7));

    private static ScreenPoint Over(MapPosition position) => new(position.X.ToDouble(), position.Y.ToDouble());

    private static List<UnitState> UnitsOf(Match match, PlayerId player) =>
        match.State.Units.Where(unit => unit.Owner == player).ToList();

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

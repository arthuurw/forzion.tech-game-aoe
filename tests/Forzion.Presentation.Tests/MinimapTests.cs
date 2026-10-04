using Forzion.Simulation;
using static Forzion.Presentation.Tests.TestMatches;

namespace Forzion.Presentation.Tests;

public class MinimapTests
{
    // The plain match's 64 by 48 map in a 256 pixel square box: 4 pixels per Cell, and the
    // 256 - 192 = 64 pixels spare split above and below.
    private const double BoxSize = 256;
    private const double PixelsPerCell = 4;
    private const double SpareAbove = 32;

    [Fact]
    public void The_whole_map_fits_the_box_as_large_as_it_can_centred_across_the_spare_room()
    {
        var match = Match.Create(PlainConfig());

        var minimap = NewMinimap(match);

        Assert.Equal(new ScreenPoint(0, 32), minimap.ToMinimap(new MapPoint(0, 0)));
        Assert.Equal(new ScreenPoint(256, 224), minimap.ToMinimap(new MapPoint(64, 48)));
    }

    [Fact]
    public void A_click_on_the_minimap_points_at_the_place_of_the_map_drawn_under_it()
    {
        var minimap = NewMinimap(Match.Create(PlainConfig()));

        Assert.Equal(new MapPoint(32, 24), minimap.ToMap(new ScreenPoint(128, 128)));
        Assert.Equal(new MapPoint(10.5, 3), minimap.ToMap(new ScreenPoint(42, 44)));
    }

    [Fact]
    public void A_click_in_the_room_beside_the_map_points_at_the_nearest_place_on_its_edge()
    {
        var minimap = NewMinimap(Match.Create(PlainConfig()));

        Assert.Equal(new MapPoint(2.5, 0), minimap.ToMap(new ScreenPoint(10, 5)));
        Assert.Equal(new MapPoint(64, 48), minimap.ToMap(new ScreenPoint(300, 250)));
    }

    [Fact]
    public void Every_building_of_either_Player_is_marked_over_its_footprint_with_its_owner()
    {
        var match = Match.Create(PlainConfig());
        var minimap = NewMinimap(match);

        var marks = minimap.MarksOf(match.State, unit => PointOf(unit.Position));

        foreach (var player in new[] { FirstPlayer, SecondPlayer })
        {
            var townCenter = TownCenterOf(match, player);
            var corner = InBox(townCenter.Origin.X, townCenter.Origin.Y);

            Assert.Contains(new PlayerMark(corner, townCenter.Width * PixelsPerCell, townCenter.Height * PixelsPerCell, player), marks);
        }
    }

    [Fact]
    public void Every_unit_of_either_Player_is_marked_a_Cell_across_centred_where_it_is_drawn_and_over_the_buildings()
    {
        var match = Match.Create(PlainConfig());
        var minimap = NewMinimap(match);

        // Each unit drawn at its own made-up place, so that a mark shows which one it is for.
        var marks = minimap.MarksOf(match.State, unit => new MapPoint(unit.Id.Value, 10));

        // The mark's corner is half a Cell up and left of where the unit is drawn.
        var expected = match.State.Units.Select(unit =>
            new PlayerMark(InBox(unit.Id.Value - 0.5, 10 - 0.5), PixelsPerCell, PixelsPerCell, unit.Owner));
        Assert.Equal(expected, marks.TakeLast(match.State.Units.Count));
    }

    [Fact]
    public void Every_resource_source_is_marked_over_its_Cell_with_its_Resource_under_the_buildings()
    {
        var match = Match.Create(PlainConfig());
        var minimap = NewMinimap(match);

        var marks = minimap.MarksOf(match.State, unit => PointOf(unit.Position));

        var expected = match.State.ResourceSources.Select(source =>
            new ResourceSourceMark(InBox(source.Cell.X, source.Cell.Y), PixelsPerCell, PixelsPerCell, source.Kind));
        Assert.Equal(expected, marks.Take(match.State.ResourceSources.Count));
    }

    private static Minimap NewMinimap(Match match) => new(match.State.Map, BoxSize, BoxSize);

    /// <summary>Where a point of the map falls in the box, worked out from the numbers above.</summary>
    private static ScreenPoint InBox(double x, double y) => new(x * PixelsPerCell, SpareAbove + (y * PixelsPerCell));
}

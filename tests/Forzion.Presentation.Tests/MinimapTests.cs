using Forzion.Simulation;
using static Forzion.Presentation.Tests.TestMatches;

namespace Forzion.Presentation.Tests;

public class MinimapTests
{
    [Fact]
    public void The_whole_map_fits_the_box_as_large_as_it_can_centred_across_the_spare_room()
    {
        var match = Match.Create(PlainConfig());

        // A 64 by 48 map in a 256 pixel square: 4 pixels per Cell, 32 pixels spare above and below.
        var minimap = new Minimap(match.State.Map, 256, 256);

        Assert.Equal(new ScreenPoint(0, 32), minimap.ToMinimap(new MapPoint(0, 0)));
        Assert.Equal(new ScreenPoint(256, 224), minimap.ToMinimap(new MapPoint(64, 48)));
    }

    [Fact]
    public void A_click_on_the_minimap_points_at_the_place_of_the_map_drawn_under_it()
    {
        var minimap = new Minimap(Match.Create(PlainConfig()).State.Map, 256, 256);

        Assert.Equal(new MapPoint(32, 24), minimap.ToMap(new ScreenPoint(128, 128)));
        Assert.Equal(new MapPoint(10.5, 3), minimap.ToMap(new ScreenPoint(42, 44)));
    }

    [Fact]
    public void A_click_in_the_room_beside_the_map_points_at_the_nearest_place_on_its_edge()
    {
        var minimap = new Minimap(Match.Create(PlainConfig()).State.Map, 256, 256);

        Assert.Equal(new MapPoint(2.5, 0), minimap.ToMap(new ScreenPoint(10, 5)));
        Assert.Equal(new MapPoint(64, 48), minimap.ToMap(new ScreenPoint(300, 250)));
    }
}

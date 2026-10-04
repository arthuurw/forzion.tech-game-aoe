namespace Forzion.Presentation.Tests;

public class SightLineTests
{
    [Fact]
    public void A_ray_looking_straight_down_meets_the_ground_below_its_origin()
    {
        var sight = SightLine.FromRay(new WorldVector(3, 10, 4), new WorldVector(0, -1, 0));

        Assert.Equal(new SightLine(new MapPoint(3, 4), new MapPoint(0, 0)), sight);
    }

    [Fact]
    public void A_slanted_ray_shifts_towards_its_origin_as_it_rises()
    {
        var down = -Math.Sqrt(0.5);

        var sight = SightLine.FromRay(new WorldVector(0, 10, 10), new WorldVector(0, down, down));

        Assert.NotNull(sight);
        Assert.Equal(0, sight.Value.Ground.X, 9);
        Assert.Equal(0, sight.Value.Ground.Y, 9);
        Assert.Equal(new MapPoint(0, 10), Round(sight.Value.At(10)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    public void A_ray_looking_at_or_above_the_horizon_never_meets_the_ground(double up)
    {
        Assert.Null(SightLine.FromRay(new WorldVector(0, 10, 0), new WorldVector(1, up, 0)));
    }

    private static MapPoint Round(MapPoint point) => new(Math.Round(point.X, 9), Math.Round(point.Y, 9));
}

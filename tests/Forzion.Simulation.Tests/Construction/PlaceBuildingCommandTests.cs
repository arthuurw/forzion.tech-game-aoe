using Forzion.Simulation.Tests.Maps;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Construction;

public class PlaceBuildingCommandTests
{
    [Fact]
    public void Placing_a_House_on_free_Cells_pays_its_cost_and_leaves_an_unfinished_House_on_them()
    {
        var match = TestMatches.TwoPlayerMatch();
        var player = match.State.Players[0];
        var cost = Match.BuildingCost(BuildingKind.House);
        Site.Stockpile(match, TestMatches.FirstPlayer, cost.Wood);
        var before = Enum.GetValues<ResourceKind>().Select(player.AmountOf).ToList();
        var origin = Site.FreeOriginNear(match.State, match.State.Buildings[0].Origin, Match.BuildingSize(BuildingKind.House));
        match.Enqueue(new PlaceBuildingCommand(TestMatches.FirstPlayer, BuildingKind.House, origin));

        match.Tick();

        var house = match.State.Buildings.Single(building => building.Kind == BuildingKind.House);
        Assert.Equal(TestMatches.FirstPlayer, house.Owner);
        Assert.Equal(origin, house.Origin);
        Assert.Equal(Match.BuildingSize(BuildingKind.House), house.Width);
        Assert.Equal(Match.BuildingSize(BuildingKind.House), house.Height);
        Assert.False(house.IsComplete);
        Assert.Equal(0, house.BuildProgress);
        Assert.All(MapProbe.Footprint(house), cell => Assert.Equal(CellKind.Building, match.State.Map[cell]));
        Assert.Equal(
            [before[0] - cost.Food, before[1] - cost.Wood, before[2] - cost.Gold],
            Enum.GetValues<ResourceKind>().Select(player.AmountOf));
        Assert.True(cost.Wood > 0);
    }
}

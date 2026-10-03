using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Ai;

public class AiConstructionTests
{
    private static readonly PlayerId Ai = TestMatches.SecondPlayer;

    [Fact]
    public void An_AI_Player_builds_a_House_before_its_population_reaches_the_limit()
    {
        var match = Match.Create(AiMatches.Config(firstIsAi: false, secondIsAi: true));

        AiMatches.TickUntil(match, () => AiMatches.Buildings(match, Ai, BuildingKind.House).Any(house => house.IsComplete), 1500);

        Assert.True(match.State.PopulationLimitOf(Ai) > match.State.PopulationOf(Ai));
    }

    [Fact]
    public void An_AI_Player_builds_a_single_Barracks()
    {
        var match = Match.Create(AiMatches.Config(firstIsAi: false, secondIsAi: true));

        AiMatches.TickUntil(match, () => AiMatches.Buildings(match, Ai, BuildingKind.Barracks).Any(barracks => barracks.IsComplete), 5000);
        AiMatches.Run(match, 200);

        Assert.Single(AiMatches.Buildings(match, Ai, BuildingKind.Barracks));
    }

    [Fact]
    public void An_AI_Player_builds_a_Storehouse_by_the_sources_it_gathers_once_those_near_its_Town_Center_run_out()
    {
        var match = Match.Create(AiMatches.Config(firstIsAi: false, secondIsAi: true));
        var townCenter = AiMatches.TownCenter(match, Ai);

        AiMatches.TickUntil(match, () => AiMatches.Buildings(match, Ai, BuildingKind.Storehouse).Any(storehouse => storehouse.IsComplete), 12000);

        var storehouse = AiMatches.Buildings(match, Ai, BuildingKind.Storehouse).First();
        var gatheredFrom = match.State.UnitsOf(Ai)
            .Select(unit => unit.GatherSource)
            .OfType<EntityId>()
            .Select(id => match.State.ResourceSources.Single(source => source.Id == id).Cell);
        Assert.Contains(gatheredFrom, cell => Distance(storehouse, cell) < Distance(townCenter, cell));
    }

    /// <summary>Distance in king's moves from the Cell to the nearest Cell of the building's footprint.</summary>
    private static int Distance(BuildingState building, CellPosition cell) =>
        Math.Max(
            Math.Max(Math.Max(building.Origin.X - cell.X, cell.X - (building.Origin.X + building.Width - 1)), 0),
            Math.Max(Math.Max(building.Origin.Y - cell.Y, cell.Y - (building.Origin.Y + building.Height - 1)), 0));
}

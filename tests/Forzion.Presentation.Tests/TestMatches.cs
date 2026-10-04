using Forzion.Simulation;

namespace Forzion.Presentation.Tests;

/// <summary>Matches shared by the presentation tests, built on the simulation's public interface.</summary>
internal static class TestMatches
{
    public static readonly PlayerId FirstPlayer = new(1);

    public static readonly PlayerId SecondPlayer = new(2);

    /// <summary>The default two-Player match, with extra units for either Player.</summary>
    public static MatchConfig PlainConfig(
        IReadOnlyList<StartingUnit>? firstExtras = null, IReadOnlyList<StartingUnit>? secondExtras = null)
    {
        var faction = new FactionId(1);

        return new MatchConfig(
            42, new MapConfig(64, 48), [new PlayerConfig(faction, firstExtras), new PlayerConfig(faction, secondExtras)]);
    }

    /// <summary>
    /// The plain match, with the second Player starting with a melee soldier beside the first
    /// Player's Town Center, close enough to the first Player's Villagers to attack them on its own.
    /// </summary>
    public static MatchConfig WithEnemySoldierAtHome() =>
        PlainConfig(secondExtras: [new StartingUnit(UnitKind.MeleeSoldier, BesideFirstHome())]);

    /// <summary>
    /// The Cell two left of the centre of the first Player's Town Center: free, and away from
    /// the Villagers' row.
    /// </summary>
    public static CellPosition BesideFirstHome()
    {
        var townCenter = Match.Create(PlainConfig()).State.Buildings.First(building => building.Owner == FirstPlayer);

        return new CellPosition(townCenter.Origin.X + (townCenter.Width / 2) - 2, townCenter.Origin.Y + (townCenter.Height / 2));
    }

    /// <summary>The Player's units, in ascending ID order.</summary>
    public static List<UnitState> UnitsOf(Match match, PlayerId player) =>
        match.State.Units.Where(unit => unit.Owner == player).ToList();
}

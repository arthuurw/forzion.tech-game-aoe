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

    /// <summary>A driver of a new match of the configuration, the plain match by default.</summary>
    public static MatchDriver NewDriver(MatchConfig? config = null) => NewDriver(out _, config);

    /// <summary>A driver of a new match of the configuration, and the match it drives, for the test to read and command.</summary>
    public static MatchDriver NewDriver(out Match match, MatchConfig? config = null)
    {
        match = Match.Create(config ?? PlainConfig());

        return new MatchDriver(match, new TickClock(Match.TicksPerSecond));
    }

    /// <summary>Advances the driver one tick at a time until the condition holds.</summary>
    public static void TickUntil(MatchDriver driver, Func<bool> condition)
    {
        for (var tick = 0; tick < 2_000; tick++)
        {
            driver.Advance(1.0 / Match.TicksPerSecond);

            if (condition())
            {
                return;
            }
        }

        throw new InvalidOperationException("The condition still did not hold after 2000 ticks.");
    }

    /// <summary>The Player's units, in ascending ID order.</summary>
    public static List<UnitState> UnitsOf(Match match, PlayerId player) =>
        match.State.Units.Where(unit => unit.Owner == player).ToList();
}

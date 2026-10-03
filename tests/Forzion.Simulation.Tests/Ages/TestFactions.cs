using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Ages;

/// <summary>Factions made up for the tests, to show that the rules follow whatever a Faction's data says.</summary>
internal static class TestFactions
{
    /// <summary>
    /// A Faction of three Ages, each cheap enough to reach from the starting Resources: Age I
    /// unlocks Villagers, Houses and Storehouses, Age II the Barracks and the melee and ranged
    /// soldiers, and Age III the heavy soldier.
    /// </summary>
    public static readonly Faction ThreeAges = new(
        new FactionId(7),
        "TEST_THREE_AGES",
        [
            new FactionAge("TEST_AGE_1", new Cost(0, 0, 0), 0, [UnitKind.Villager], [BuildingKind.House, BuildingKind.Storehouse]),
            new FactionAge("TEST_AGE_2", new Cost(50, 0, 0), 10, [UnitKind.MeleeSoldier, UnitKind.RangedSoldier], [BuildingKind.Barracks]),
            new FactionAge("TEST_AGE_3", new Cost(50, 0, 50), 20, [UnitKind.HeavySoldier], []),
        ],
        new Dictionary<UnitKind, string>());

    /// <summary>A match of the first Player alone, controlling <see cref="ThreeAges"/>.</summary>
    public static Match ThreeAgeMatch() => Match.Create(TestMatches.SinglePlayerConfig() with
    {
        Players = [new PlayerConfig(ThreeAges.Id)],
        Factions = [ThreeAges],
    });

    /// <summary>A Faction with the given ID and a single Age that unlocks nothing.</summary>
    public static Faction OneAge(int id) => new(
        new FactionId(id),
        $"TEST_ONE_AGE_{id}",
        [new FactionAge("TEST_AGE_1", new Cost(0, 0, 0), 0, [], [])],
        new Dictionary<UnitKind, string>());
}

namespace Forzion.Simulation;

/// <summary>The Factions of the game, as data. A match whose configuration names no Factions has these.</summary>
public static class Factions
{
    /// <summary>
    /// The Portuguese, with Ages I and II. Age I unlocks the Villager, the melee and ranged
    /// soldiers and every building a Player places; Age II unlocks the heavy soldier.
    /// </summary>
    public static Faction Portuguese { get; } = new(
        new FactionId(1),
        "FACTION_PORTUGUESE",
        [
            new FactionAge(
                "FACTION_PORTUGUESE_AGE_1",
                new Cost(0, 0, 0),
                0,
                [UnitKind.Villager, UnitKind.MeleeSoldier, UnitKind.RangedSoldier],
                [BuildingKind.House, BuildingKind.Storehouse, BuildingKind.Barracks]),
            new FactionAge(
                "FACTION_PORTUGUESE_AGE_2",
                Balance.SecondAgeAdvanceCost,
                Balance.SecondAgeAdvanceTime,
                [UnitKind.HeavySoldier],
                []),
        ],
        new Dictionary<UnitKind, string>
        {
            [UnitKind.Villager] = "FACTION_PORTUGUESE_VILLAGER",
            [UnitKind.MeleeSoldier] = "FACTION_PORTUGUESE_MELEE_SOLDIER",
            [UnitKind.RangedSoldier] = "FACTION_PORTUGUESE_RANGED_SOLDIER",
            [UnitKind.HeavySoldier] = "FACTION_PORTUGUESE_HEAVY_SOLDIER",
        });

    /// <summary>Every Faction of the game, in ascending ID order.</summary>
    public static IReadOnlyList<Faction> All { get; } = [Portuguese];
}

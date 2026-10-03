using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Production;

namespace Forzion.Simulation.Tests.Ages;

public class UnlockTests
{
    private static readonly PlayerId First = TestMatches.FirstPlayer;

    [Fact]
    public void Training_a_heavy_soldier_in_Age_I_is_rejected_and_changes_nothing()
    {
        Train.AssertRejected(RejectionReason.UnitLocked, match =>
            new TrainCommand(First, Train.Complete(match, First, BuildingKind.Barracks).Id, UnitKind.HeavySoldier));
    }

    [Fact]
    public void The_Portuguese_unlock_the_heavy_soldier_in_Age_II_and_nothing_else_in_Age_I_is_locked()
    {
        var portuguese = Factions.Portuguese;

        Assert.All(
            Enum.GetValues<UnitKind>(),
            kind => Assert.Equal(kind != UnitKind.HeavySoldier, portuguese.Unlocks(kind, 1)));
        Assert.All(Enum.GetValues<UnitKind>(), kind => Assert.True(portuguese.Unlocks(kind, 2)));
    }

    [Fact]
    public void In_a_Faction_of_three_Ages_a_unit_of_Age_III_stays_locked_in_Age_II_and_is_unlocked_in_Age_III()
    {
        var match = TestFactions.ThreeAgeMatch();
        Advance.ToNextAge(match, First);
        var barracks = Train.Complete(match, First, BuildingKind.Barracks);
        match.Enqueue(new TrainCommand(First, barracks.Id, UnitKind.HeavySoldier));
        match.Tick();

        Assert.Equal(RejectionReason.UnitLocked, Assert.IsType<CommandRejected>(Assert.Single(match.Events)).Reason);

        Advance.ToNextAge(match, First);
        Advance.Afford(match, First, Match.UnitCost(UnitKind.HeavySoldier));
        match.Enqueue(new TrainCommand(First, barracks.Id, UnitKind.HeavySoldier));
        match.Tick();

        Assert.Equal([UnitKind.HeavySoldier], barracks.TrainingQueue);
    }
}

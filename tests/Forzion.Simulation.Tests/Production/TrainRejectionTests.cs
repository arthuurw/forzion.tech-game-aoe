using Forzion.Simulation.Tests.Construction;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Production;

public class TrainRejectionTests
{
    [Fact]
    public void Training_at_a_building_that_does_not_exist_is_rejected_and_changes_nothing()
    {
        Train.AssertRejected(RejectionReason.UnknownBuilding, match =>
            new TrainCommand(TestMatches.FirstPlayer, match.State.ResourceSources[0].Id, UnitKind.Villager));
    }

    [Fact]
    public void Training_at_another_Players_building_is_rejected_and_changes_nothing()
    {
        Train.AssertRejected(RejectionReason.BuildingOfAnotherPlayer, match =>
            new TrainCommand(TestMatches.FirstPlayer, Train.TownCenter(match, TestMatches.SecondPlayer).Id, UnitKind.Villager));
    }

    [Fact]
    public void Training_at_a_construction_site_is_rejected_and_changes_nothing()
    {
        Train.AssertRejected(RejectionReason.BuildingNotComplete, match =>
        {
            var barracks = Site.Place(match, TestMatches.FirstPlayer, BuildingKind.Barracks, []);

            return new TrainCommand(TestMatches.FirstPlayer, barracks.Id, UnitKind.MeleeSoldier);
        });
    }

    [Theory]
    [InlineData(BuildingKind.TownCenter, UnitKind.MeleeSoldier)]
    [InlineData(BuildingKind.TownCenter, UnitKind.RangedSoldier)]
    [InlineData(BuildingKind.TownCenter, (UnitKind)99)]
    [InlineData(BuildingKind.Barracks, UnitKind.Villager)]
    [InlineData(BuildingKind.House, UnitKind.Villager)]
    [InlineData(BuildingKind.House, UnitKind.MeleeSoldier)]
    public void Training_a_unit_at_a_building_that_does_not_train_its_kind_is_rejected_and_changes_nothing(BuildingKind building, UnitKind kind)
    {
        Train.AssertRejected(RejectionReason.BuildingCannotTrainUnit, match =>
            new TrainCommand(TestMatches.FirstPlayer, Train.Complete(match, TestMatches.FirstPlayer, building).Id, kind));
    }

    [Fact]
    public void Training_a_unit_the_Player_cannot_afford_is_rejected_and_changes_nothing()
    {
        Train.AssertRejected(RejectionReason.NotEnoughResources, match =>
        {
            // A House first, so that population is not what runs out.
            Train.Complete(match, TestMatches.FirstPlayer, BuildingKind.House);
            var townCenter = Train.TownCenter(match, TestMatches.FirstPlayer);
            var player = match.State.Players[0];

            while (player.AmountOf(ResourceKind.Food) >= Match.UnitCost(UnitKind.Villager).Food)
            {
                match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));
                match.Tick();

                Assert.Empty(match.Events);
            }

            return new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager);
        });
    }
}

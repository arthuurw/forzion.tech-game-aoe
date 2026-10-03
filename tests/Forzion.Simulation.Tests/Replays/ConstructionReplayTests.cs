using Forzion.Simulation.Tests.Construction;
using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Replays;

public class ConstructionReplayTests
{
    private const ulong Seed = 2026;

    // Long enough for every site to be completed and its builders to be gathering again.
    private const int Ticks = 3000;

    /// <summary>
    /// Construction by both Players: Wood gathered for it, a House built by two Villagers and
    /// joined by a third, a Barracks left unfinished when its builder joins the House, a
    /// Storehouse built near the Wood, a build order for the other Player's site and the builders sent back to
    /// gathering. Unit, source and building IDs and the origins are read
    /// from a match of the same configuration.
    /// </summary>
    private static ScheduledCommand[] Commands()
    {
        var state = TestMatches.TwoPlayerMatch(Seed).State;
        var first = state.UnitsOf(TestMatches.FirstPlayer).Select(unit => unit.Id).ToList();
        var second = state.UnitsOf(TestMatches.SecondPlayer).ToList();
        var firstWood = Gather.NearestSource(state, state.Units[1].Position.Cell, ResourceKind.Wood);
        var secondWood = Gather.NearestSource(state, second[1].Position.Cell, ResourceKind.Wood);
        var house = Site.FreeOriginNear(state, state.Buildings[0].Origin, Match.BuildingSize(BuildingKind.House));
        var storehouse = Site.FreeOriginNear(state, secondWood.Cell, Match.BuildingSize(BuildingKind.Storehouse));
        var barracks = Site.FreeOriginNear(state, firstWood.Cell, Match.BuildingSize(BuildingKind.Barracks));

        // Entities take IDs in creation order: the sites are placed House, Barracks, Storehouse.
        var houseId = new EntityId(state.Units[^1].Id.Value + 1);
        var storehouseId = new EntityId(houseId.Value + 2);

        return
        [
            new(0, new GatherCommand(TestMatches.FirstPlayer, first, firstWood.Id)),
            new(0, new GatherCommand(TestMatches.SecondPlayer, second.Select(unit => unit.Id).ToList(), secondWood.Id)),
            new(600, new PlaceBuildingCommand(TestMatches.FirstPlayer, BuildingKind.House, house, [first[0], first[1]])),
            new(610, new PlaceBuildingCommand(TestMatches.FirstPlayer, BuildingKind.Barracks, barracks, [first[2]])),
            new(650, new BuildCommand(TestMatches.FirstPlayer, [first[2]], houseId)),
            new(700, new PlaceBuildingCommand(TestMatches.SecondPlayer, BuildingKind.Storehouse, storehouse, [second[0].Id])),
            new(900, new BuildCommand(TestMatches.FirstPlayer, [first[2]], storehouseId)),
            new(1000, new GatherCommand(TestMatches.FirstPlayer, first, firstWood.Id)),
            new(1500, new GatherCommand(TestMatches.SecondPlayer, [second[0].Id], secondWood.Id)),
        ];
    }

    [Fact]
    public void The_same_seed_and_construction_give_the_same_hash_at_every_tick()
    {
        var first = Replay.Run(TestMatches.TwoPlayerConfig(Seed), Commands(), Ticks);
        var second = Replay.Run(TestMatches.TwoPlayerConfig(Seed), Commands(), Ticks);

        Assert.Equal(first, second);
    }

    [Fact]
    public void A_rejected_build_order_does_not_change_the_hash_of_any_tick()
    {
        var commands = Commands();
        var rejected = commands.Single(scheduled => scheduled.Tick == 900);

        var withRejection = Replay.Run(TestMatches.TwoPlayerConfig(Seed), commands, Ticks);
        var without = Replay.Run(TestMatches.TwoPlayerConfig(Seed), commands.Except([rejected]).ToList(), Ticks);

        Assert.Equal(without, withRejection);
    }

    // The construction itself comes from this implementation; what an independent model of
    // the hash layout confirmed is that this is the hash of the final state as the public
    // interface shows it, with a complete House and Storehouse, the Barracks left unfinished
    // and both Players' Villagers gathering Wood again. When construction and combat met in
    // the hash, the model gave the value before from the same final state with the layout
    // before, and this one with combat state added, and it matched the match's own hash at
    // every tick. When Players began the match with starting Resources, the Barracks, refused
    // until then for want of Wood, was placed instead and its builder called off to the House;
    // the layout stayed the same and the model, which first reproduced the values before of
    // the other replays from their new states with the starting Resources taken out, gave
    // this one and matched the match's own hash at every tick. CI runs this on Windows, Linux
    // and macOS: every system must reach the same hash.
    // When training joined the hash, the model reproduced the value before from the same final
    // state with the layout before, and gave this one with training state added.
    // Likewise when the rally point joined it.
    private const ulong ExpectedFinalHash = 1348516145706742691UL;

    [Fact]
    public void A_recorded_replay_of_construction_reaches_the_recorded_final_hash()
    {
        var hashes = Replay.Run(TestMatches.TwoPlayerConfig(Seed), Commands(), Ticks);

        Assert.Equal(ExpectedFinalHash, hashes[^1]);
    }
}

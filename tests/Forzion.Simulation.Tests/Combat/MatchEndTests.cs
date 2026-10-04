using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Combat;

public class MatchEndTests
{
    private static readonly PlayerId First = TestMatches.FirstPlayer;
    private static readonly PlayerId Second = TestMatches.SecondPlayer;

    [Fact]
    public void A_match_is_not_over_while_every_Player_has_a_Town_Center()
    {
        var match = Battle.Siege();
        var townCenter = Battle.TownCenter(match, Second);

        var events = new List<MatchEvent>();
        TestMatches.TickUntil(match, () =>
        {
            events.AddRange(match.Events);

            return townCenter.HitPoints < townCenter.MaxHitPoints / 2;
        });

        Assert.False(match.State.IsOver);
        Assert.Null(match.State.Winner);
        Assert.All(match.State.Players, player => Assert.False(player.IsDefeated));
        Assert.DoesNotContain(events, matchEvent => matchEvent is PlayerDefeated or MatchEnded);
    }

    [Fact]
    public void Destroying_the_enemy_Town_Center_defeats_the_enemy_and_ends_the_match_in_the_same_tick()
    {
        var match = Battle.Siege();
        var townCenter = Battle.TownCenter(match, Second);

        TestMatches.TickUntil(match, () => townCenter.HitPoints <= 0);

        Assert.Equal(
            [new EntityDestroyed(townCenter.Id), new PlayerDefeated(Second), new MatchEnded(First)],
            match.Events);
        Assert.True(match.State.Players[1].IsDefeated);
        Assert.False(match.State.Players[0].IsDefeated);
        Assert.True(match.State.IsOver);
        Assert.Equal(First, match.State.Winner);
    }

    [Fact]
    public void A_match_ends_only_once()
    {
        var match = Battle.Siege();
        var townCenter = Battle.TownCenter(match, Second);
        TestMatches.TickUntil(match, () => townCenter.HitPoints <= 0);

        var later = Battle.Run(match, 200);

        Assert.DoesNotContain(later, matchEvent => matchEvent is PlayerDefeated or MatchEnded);
        Assert.Equal(First, match.State.Winner);
    }

    [Fact]
    public void A_defeated_Player_can_no_longer_give_commands()
    {
        var match = Battle.Siege();
        var townCenter = Battle.TownCenter(match, Second);
        TestMatches.TickUntil(match, () => townCenter.HitPoints <= 0);
        var villager = TestMatches.MiddleVillager(match, Second);
        var command = new MoveCommand(Second, [villager.Id], villager.Position.Cell with { X = villager.Position.Cell.X + 1 });

        match.Enqueue(command);
        match.Tick();

        Assert.Contains(new CommandRejected(command, RejectionReason.DefeatedPlayer), match.Events);
        Assert.False(villager.IsMoving);
    }

    [Fact]
    public void Town_Centers_destroyed_in_the_same_tick_defeat_both_Players_and_end_the_match_without_a_winner()
    {
        // Each Player besieges the other's Town Center from the mirrored Cells, so both fall together.
        var match = Battle.Create(
            first: plain => Flanks(plain, Second),
            second: plain => Flanks(plain, First));
        var firstSoldiers = match.State.Units.Where(unit => unit.Owner == First && unit.Kind == UnitKind.MeleeSoldier).ToList();
        var secondSoldiers = match.State.Units.Where(unit => unit.Owner == Second && unit.Kind == UnitKind.MeleeSoldier).ToList();
        var firstTownCenter = Battle.TownCenter(match, First);
        var secondTownCenter = Battle.TownCenter(match, Second);
        match.Enqueue(new AttackCommand(First, firstSoldiers.Select(unit => unit.Id).ToList(), secondTownCenter.Id));
        match.Enqueue(new AttackCommand(Second, secondSoldiers.Select(unit => unit.Id).ToList(), firstTownCenter.Id));

        TestMatches.TickUntil(match, () => firstTownCenter.HitPoints <= 0 || secondTownCenter.HitPoints <= 0);

        Assert.Empty(match.State.Buildings);
        Assert.All(match.State.Players, player => Assert.True(player.IsDefeated));
        Assert.True(match.State.IsOver);
        Assert.Null(match.State.Winner);
        Assert.Equal(
            [new PlayerDefeated(First), new PlayerDefeated(Second), new MatchEnded(null)],
            match.Events.Where(matchEvent => matchEvent is PlayerDefeated or MatchEnded));
    }

    [Fact]
    public void A_match_with_a_single_Player_does_not_end_on_its_own()
    {
        var match = Match.Create(TestMatches.SinglePlayerConfig());

        var events = Battle.Run(match, 10);

        Assert.False(match.State.IsOver);
        Assert.Empty(events);
    }

    private static List<StartingUnit> Flanks(Match plain, PlayerId besieged) =>
        [
            new(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, besieged, -2, 0)),
            new(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, besieged, 2, 0)),
        ];
}

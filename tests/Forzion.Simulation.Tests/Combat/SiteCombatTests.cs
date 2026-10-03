using Forzion.Simulation.Tests.Construction;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Combat;

/// <summary>Buildings placed by Players in combat, from the moment they are placed.</summary>
public class SiteCombatTests
{
    private static readonly PlayerId First = TestMatches.FirstPlayer;
    private static readonly PlayerId Second = TestMatches.SecondPlayer;

    public static TheoryData<BuildingKind> PlaceableKinds() => new() { BuildingKind.House, BuildingKind.Storehouse, BuildingKind.Barracks };

    [Theory]
    [MemberData(nameof(PlaceableKinds))]
    public void A_placed_building_starts_whole_with_the_hit_points_of_its_kind(BuildingKind kind)
    {
        var match = TestMatches.TwoPlayerMatch();
        var site = Site.Place(match, First, kind, []);

        Assert.Equal(kind, site.Kind);
        Assert.True(site.MaxHitPoints > 0);
        Assert.Equal(site.MaxHitPoints, site.HitPoints);
    }

    [Fact]
    public void Soldiers_ordered_to_attack_an_unfinished_site_wear_it_down()
    {
        var match = Battle.Raiders();
        var site = Site.Place(match, First, BuildingKind.Barracks, []);
        var attack = new AttackCommand(Second, Battle.RaidersOf(match).Select(unit => unit.Id).ToList(), site.Id);
        match.Enqueue(attack);
        match.Tick();

        Assert.DoesNotContain(match.Events, matchEvent => matchEvent is CommandRejected);

        Battle.TickUntil(match, () => site.HitPoints < site.MaxHitPoints);

        Assert.False(site.IsComplete);
        Assert.All(Battle.RaidersOf(match), soldier => Assert.Equal(site.Id, soldier.Target));
    }

    [Fact]
    public void An_idle_soldier_beside_an_enemy_site_does_not_attack_it_on_its_own()
    {
        var match = Battle.Raiders();
        var site = Site.Place(match, First, BuildingKind.House, []);
        var villagers = Site.VillagersOf(match, First);
        var soldier = Battle.RaidersOf(match)[0];

        // The first Player's Villagers leave for a far corner, out of the soldier's sight.
        match.Enqueue(new MoveCommand(First, villagers.Select(villager => villager.Id).ToList(), new CellPosition(63, 0)));
        match.Enqueue(new MoveCommand(Second, [soldier.Id], new CellPosition(site.Origin.X - 1, site.Origin.Y)));
        Battle.TickUntil(match, () => !soldier.IsMoving && villagers.All(villager => !villager.IsMoving));
        Battle.Run(match, 200);

        Assert.True(Battle.Distance(soldier.Position, MapPosition.CentreOf(site.Origin)) < 2);
        Assert.Null(soldier.Target);
        Assert.Equal(site.MaxHitPoints, site.HitPoints);
    }
}

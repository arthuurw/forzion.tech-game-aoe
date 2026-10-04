using Forzion.Simulation;
using static Forzion.Presentation.Tests.TestMatches;

namespace Forzion.Presentation.Tests;

/// <summary>
/// A match played from the start the way the person at the screen plays it: clicks and drags
/// on the map, the buttons of the HUD, and what the HUD shows to decide what to do next. The
/// match only hears from the person through <see cref="PlayerControl"/>; no command is
/// enqueued here.
/// </summary>
public class PlaythroughTests
{
    private readonly MatchDriver driver = NewDriver();
    private readonly PlayerControl control;

    public PlaythroughTests() => control = new PlayerControl(driver, FirstPlayer, TopDown, Sizes);

    private MatchState State => driver.State;

    [Fact]
    public void Playing_from_the_start_only_through_the_HUD_and_clicks_reaches_Age_II_with_an_army_that_has_a_heavy_soldier()
    {
        var townCenter = State.Buildings.First(building => building.Owner == FirstPlayer && building.Kind == BuildingKind.TownCenter);
        var builders = VillagersOfPlayer();

        // Two more Villagers from the Town Center, walking out to a rally point beside it.
        ClickOn(townCenter);
        var villagerChoice = Panel().Building!.UnitChoices.Single(choice => choice.Kind == UnitKind.Villager);
        Assert.False(villagerChoice.IsLocked);
        control.Train(villagerChoice.Kind);
        control.Train(villagerChoice.Kind);
        var rallyPoint = BesideFirstHome();
        control.OrderAt(Over(MapPosition.CentreOf(rallyPoint)));

        // A House from the panel's building choices, built by the three starting Villagers.
        DragBoxAround(builders);
        var populationLimit = Status().PopulationLimit;
        Place(BuildingKind.House);
        RunUntil(() => Status().PopulationLimit > populationLimit, 1_000, "the House to be complete");

        // A Barracks, by the same Villagers, still selected.
        Assert.Equal(builders.Select(unit => unit.Id), control.Selected);
        var barracks = Place(BuildingKind.Barracks);
        RunUntil(() => barracks.IsComplete, 2_000, "the Barracks to be complete");
        ClickOn(barracks);
        Assert.Equal(
            "FACTION_PORTUGUESE_AGE_2",
            Panel().Building!.UnitChoices.Single(choice => choice.Kind == UnitKind.HeavySoldier).LockedUntilAgeNameKey);

        // The trained Villagers wait together at the rally point: one box takes both to the Gold.
        RunUntil(
            () => VillagersOfPlayer().Count(unit => unit.Position.Cell == rallyPoint) == 2,
            1_000,
            "the trained Villagers to reach the rally point");
        var trained = VillagersOfPlayer().Where(unit => unit.Position.Cell == rallyPoint).ToList();
        DragBoxAround(trained);
        control.OrderAt(OverSourceNearHome(ResourceKind.Gold));

        // The builders, one click each where it is drawn, to the Food and the Wood. They
        // finish the Barracks standing on one Cell, drawn side by side.
        ClickOn(builders[0]);
        control.OrderAt(OverSourceNearHome(ResourceKind.Food));
        ClickOn(builders[1]);
        control.OrderAt(OverSourceNearHome(ResourceKind.Food));
        ClickOn(builders[2]);
        control.OrderAt(OverSourceNearHome(ResourceKind.Wood));
        Run(1);
        Assert.All(VillagersOfPlayer(), villager => Assert.NotNull(villager.GatherSource));

        // The Age Advance from the Town Center, as soon as the top bar shows enough to pay for it.
        ClickOn(townCenter);
        var advance = Panel().Building!.AgeAdvance!;
        RunUntil(() => CanAfford(advance.Cost), 6_000, "enough Resources for the Age Advance");
        control.AdvanceAge();
        Run(1);
        Assert.Equal(advance.AgeNameKey, Status().AgeAdvance?.AgeNameKey);
        RunUntil(() => Status().AgeNameKey == advance.AgeNameKey, 1_000, "the Age Advance to be done");

        // The army from the Barracks, with the soldier Age II has unlocked.
        ClickOn(barracks);
        var soldierChoices = Panel().Building!.UnitChoices;
        Assert.Contains(soldierChoices, choice => choice.Kind == UnitKind.HeavySoldier);
        Assert.All(soldierChoices, choice => Assert.False(choice.IsLocked));

        // A rally point of its own, away from the Villagers, so a box around the army takes no one else.
        var muster = FreeCellAwayFromUnits(State);
        control.OrderAt(Over(MapPosition.CentreOf(muster)));
        var armyCost = soldierChoices.Aggregate(new Cost(0, 0, 0), (sum, choice) =>
            new Cost(sum.Food + choice.Cost.Food, sum.Wood + choice.Cost.Wood, sum.Gold + choice.Cost.Gold));
        RunUntil(() => CanAfford(armyCost), 6_000, "enough Resources for the army");

        foreach (var choice in soldierChoices)
        {
            control.Train(choice.Kind);
        }

        RunUntil(
            () => SoldiersOfPlayer().Count(unit => unit.Position.Cell == muster) == soldierChoices.Count,
            3_000,
            "the army to reach its rally point");

        var army = SoldiersOfPlayer();
        DragBoxAround(army);
        Assert.Equal("FACTION_PORTUGUESE_AGE_2", Status().AgeNameKey);
        Assert.Equal(2, State.Players[0].Age);
        Assert.Equal(
            [UnitKind.MeleeSoldier, UnitKind.RangedSoldier, UnitKind.HeavySoldier],
            army.Select(unit => unit.Kind).Order());
        Assert.Contains("FACTION_PORTUGUESE_HEAVY_SOLDIER", Panel().Units.Select(unit => unit.NameKey));
    }

    private SelectionPanel Panel() => SelectionPanel.For(State, FirstPlayer, control.Selected);

    private PlayerStatus Status() => PlayerStatus.Of(State, FirstPlayer);

    /// <summary>Whether the top bar shows at least the cost of every Resource.</summary>
    private bool CanAfford(Cost cost) => Status().Resources.All(each => each.Amount >= cost.AmountOf(each.Kind));

    private List<UnitState> VillagersOfPlayer() =>
        State.Units.Where(unit => unit.Owner == FirstPlayer && unit.Kind == UnitKind.Villager).ToList();

    private List<UnitState> SoldiersOfPlayer() =>
        State.Units.Where(unit => unit.Owner == FirstPlayer && unit.CanAttack).ToList();

    /// <summary>Clicks where the unit is drawn, and checks it alone is selected.</summary>
    private void ClickOn(UnitState unit)
    {
        var point = Over(driver.PositionOf(unit));
        control.Select(point, point);

        Assert.Equal([unit.Id], control.Selected);
    }

    /// <summary>Clicks the building, and checks it alone is selected.</summary>
    private void ClickOn(BuildingState building)
    {
        control.Select(OverCentreOf(building), OverCentreOf(building));

        Assert.Equal([building.Id], control.Selected);
    }

    /// <summary>Drags a box around the units, and checks they and no others are selected.</summary>
    private void DragBoxAround(IReadOnlyList<UnitState> units)
    {
        var (from, to) = BoxAround(units.Select(unit => unit.Position));
        control.Select(from, to);

        Assert.Equal(units.Select(unit => unit.Id), control.Selected);
    }

    /// <summary>
    /// Presses the panel's button for the building, then clicks where its placement preview is
    /// valid near the Town Center. Returns the construction site placed.
    /// </summary>
    private BuildingState Place(BuildingKind kind)
    {
        var choice = Panel().BuildingChoices.Single(each => each.Kind == kind);
        Assert.False(choice.IsLocked);
        control.ChooseBuilding(choice.Kind);
        var spot = PlacementSpotNearHome(control, State, FirstPlayer);
        var preview = control.PlacementAt(spot)!;

        control.PlaceAt(spot);
        Run(1);

        return State.Buildings.Single(building => building.Origin == preview.Origin && building.Kind == kind);
    }

    /// <summary>On the screen, the point over the source of the Resource nearest the first Player's Town Center.</summary>
    private ScreenPoint OverSourceNearHome(ResourceKind kind)
    {
        var home = MapPoint.CentreOf(State.Buildings.First(building => building.Owner == FirstPlayer && building.Kind == BuildingKind.TownCenter));
        var source = State.ResourceSources
            .Where(each => each.Kind == kind)
            .OrderBy(each => Math.Abs(each.Cell.X + 0.5 - home.X) + Math.Abs(each.Cell.Y + 0.5 - home.Y))
            .ThenBy(each => each.Id.Value)
            .First();

        return Over(MapPosition.CentreOf(source.Cell));
    }

    private void Run(int ticks) => RunUntil(() => false, ticks, null);

    /// <summary>
    /// Lets the match run a tick at a time until the condition holds, failing if the match
    /// refuses any order or the condition still does not hold after the most ticks. With no
    /// description, runs the most ticks and stops there.
    /// </summary>
    private void RunUntil(Func<bool> condition, int mostTicks, string? awaited)
    {
        for (var tick = 0; tick < mostTicks; tick++)
        {
            var rejection = driver.Advance(1.0 / Match.TicksPerSecond).OfType<CommandRejected>().FirstOrDefault();
            Assert.True(rejection is null, $"The match refused an order: {rejection?.Reason}.");

            if (condition())
            {
                return;
            }
        }

        Assert.True(awaited is null, $"Still waiting for {awaited} after {mostTicks} ticks.");
    }
}

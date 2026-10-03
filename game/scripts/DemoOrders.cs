using Forzion.Simulation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// TEMPORARY DEMO, to be removed once units take orders from the Player (selection and orders
/// come in a later ticket). Makes movement visible without input: every few seconds it sends
/// each Player's units walking, alternately towards the centre of the map and back home,
/// through the same <see cref="MoveCommand"/> a Player would issue.
/// </summary>
public partial class DemoOrders : Node
{
    private double untilNextOrder = 1;
    private bool towardsCentre = true;

    /// <summary>The match whose units the demo walks.</summary>
    [Export]
    public MatchView MatchView { get; set; } = null!;

    /// <summary>Seconds between two rounds of orders.</summary>
    [Export]
    public double Interval { get; set; } = 8;

    public override void _Process(double delta)
    {
        untilNextOrder -= delta;

        if (untilNextOrder > 0)
        {
            return;
        }

        untilNextOrder = Interval;
        IssueOrders();
        towardsCentre = !towardsCentre;
    }

    private void IssueOrders()
    {
        var match = MatchView.Match;
        var state = match.State;
        var centre = new CellPosition(state.Map.Width / 2, state.Map.Height / 2);

        foreach (var player in state.Players)
        {
            var home = state.Buildings.FirstOrDefault(building => building.Owner == player.Id && building.Kind == BuildingKind.TownCenter);
            var units = state.Units.Where(unit => unit.Owner == player.Id).ToList();

            if (home is null)
            {
                continue;
            }

            var target = towardsCentre
                ? centre
                : new CellPosition(home.Origin.X + (home.Width / 2), home.Origin.Y + home.Height + 1);

            // One Cell apart, side by side, so the units do not end up on top of each other.
            for (var index = 0; index < units.Count; index++)
            {
                var destination = new CellPosition(target.X + index - (units.Count / 2), target.Y);
                match.Enqueue(new MoveCommand(player.Id, [units[index].Id], destination));
            }
        }
    }
}

namespace Forzion.Simulation;

/// <summary>
/// Places a construction site of the Player: a building of the given kind whose footprint
/// starts on <paramref name="Origin"/>. Its cost is paid in full at once, and its Cells are
/// blocked from then on.
/// </summary>
/// <param name="Kind">The kind of building to place.</param>
/// <param name="Origin">The Cell of the footprint with the lowest X and Y.</param>
public sealed record PlaceBuildingCommand(PlayerId Player, BuildingKind Kind, CellPosition Origin)
    : Command(Player)
{
    internal override void Execute(TickContext context, PlayerState issuer)
    {
        var size = Balance.BuildingSize(Kind);

        issuer.Pay(Balance.BuildingCost(Kind));
        context.State.AddBuilding(issuer.Id, Kind, Origin, size, size);
    }
}

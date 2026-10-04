namespace Forzion.Simulation;

/// <summary>
/// Takes a unit off the training queue of one of the Player's buildings and gives the Player
/// back its whole cost. Cancelling the unit in training throws away the training it had
/// received: the next unit of the queue starts from the beginning.
/// </summary>
/// <remarks>
/// The command is rejected, changing nothing, when the building is not in the match, belongs
/// to another Player or is a construction site, or when its queue has no unit at the position.
/// Positions count from the front of the queue, so a unit that finishes training before the
/// command is applied moves every position up by one.
/// </remarks>
/// <param name="Building">The building whose queue to cancel from.</param>
/// <param name="Position">
/// Where the unit stands in <see cref="BuildingState.TrainingQueue"/>: 0 for the unit in
/// training, 1 for the next, and so on.
/// </param>
public sealed record CancelTrainingCommand(PlayerId Player, EntityId Building, int Position) : Command(Player)
{
    internal override void Execute(TickContext context, PlayerState issuer)
    {
        if (OwnCompleteBuilding.FindOrReject(context, this, issuer, Building) is not { } building)
        {
            return;
        }

        if (Position < 0 || Position >= building.TrainingQueue.Count)
        {
            context.Reject(this, RejectionReason.NotInTrainingQueue);

            return;
        }

        var kind = building.CancelTraining(Position);
        issuer.Refund(Balance.Of(kind).Cost);
    }
}

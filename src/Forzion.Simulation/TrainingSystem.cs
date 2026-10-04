namespace Forzion.Simulation;

/// <summary>
/// Trains the first unit of every training queue: one tick of training per tick, and once the
/// unit's train time has passed it appears on the centre of a free Cell beside the building
/// and the next unit of the queue starts. Buildings are visited in ascending ID order, so the
/// units trained in one tick take their IDs in that order.
/// </summary>
/// <remarks>
/// The unit appears on the free Cell beside the building nearest to its rally point and walks
/// there on its own, finding its way as a moved unit does; a building without a rally point
/// sets it on the free Cell beside it nearest to the centre of the map, where it stands. While
/// every Cell beside the building is taken, a unit done training waits in the queue and
/// appears in the first tick a Cell is free.
/// </remarks>
internal sealed class TrainingSystem : ISystem
{
    public void Run(TickContext context)
    {
        var state = context.State;

        foreach (var building in state.Buildings)
        {
            if (building.TrainingQueue.Count == 0)
            {
                continue;
            }

            var kind = building.TrainingQueue[0];

            if (building.TrainingProgress < Balance.Of(kind).TrainTime)
            {
                building.TrainingProgress++;
            }

            if (building.TrainingProgress < Balance.Of(kind).TrainTime)
            {
                continue;
            }

            var toward = building.RallyPoint ?? new CellPosition(state.Map.Width / 2, state.Map.Height / 2);

            if (building.FreeCellBeside(state.Map, toward) is not { } cell)
            {
                continue;
            }

            var unit = state.AddUnit(building.Owner, kind, MapPosition.CentreOf(cell));
            building.FinishTraining();
            context.Emit(new UnitTrained(unit.Id, building.Id));

            if (building.RallyPoint is { } rallyPoint)
            {
                MovementSystem.WalkTo(state.Map, unit, rallyPoint);
            }
        }
    }
}

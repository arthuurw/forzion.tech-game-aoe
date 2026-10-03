namespace Forzion.Simulation;

/// <summary>
/// Trains the first unit of every training queue: one tick of training per tick, and once the
/// unit's train time has passed it appears on the centre of a free Cell beside the building
/// and the next unit of the queue starts.
/// </summary>
/// <remarks>
/// The unit appears on the free Cell beside the building nearest to the centre of the map.
/// While every Cell beside the building is taken, a unit done training waits in the queue
/// and appears in the first tick a Cell is free.
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

            if (building.TrainingProgress < Balance.TrainTime(kind))
            {
                building.TrainingProgress++;
            }

            if (building.TrainingProgress < Balance.TrainTime(kind))
            {
                continue;
            }

            var centre = new CellPosition(state.Map.Width / 2, state.Map.Height / 2);

            if (building.FreeCellBeside(state.Map, centre) is not { } cell)
            {
                continue;
            }

            var unit = state.AddUnit(building.Owner, kind, MapPosition.CentreOf(cell));
            building.FinishTraining();
            context.Emit(new UnitTrained(unit.Id, building.Id));
        }
    }
}

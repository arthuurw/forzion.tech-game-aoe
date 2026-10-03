namespace Forzion.Simulation;

/// <summary>What a building is. Its kind decides what the building does.</summary>
public enum BuildingKind
{
    TownCenter = 0,

    /// <summary>Raises its Player's population limit once complete.</summary>
    House = 1,

    /// <summary>A drop-off point once complete.</summary>
    Storehouse = 2,

    /// <summary>Trains military units once complete.</summary>
    Barracks = 3,
}

/// <summary>
/// A building. It occupies a rectangle of whole Cells from the moment it is placed, and is a
/// construction site until Villagers have built it to completion.
/// </summary>
public sealed class BuildingState
{
    private readonly List<UnitKind> trainingQueue = [];

    internal BuildingState(EntityId id, PlayerId owner, BuildingKind kind, CellPosition origin, int width, int height)
    {
        Id = id;
        Owner = owner;
        Kind = kind;
        Origin = origin;
        Width = width;
        Height = height;
        HitPoints = MaxHitPoints;
    }

    public EntityId Id { get; }

    public PlayerId Owner { get; }

    public BuildingKind Kind { get; }

    /// <summary>The Cell of the footprint with the lowest X and Y.</summary>
    public CellPosition Origin { get; }

    /// <summary>Width of the footprint in Cells.</summary>
    public int Width { get; }

    /// <summary>Height of the footprint in Cells.</summary>
    public int Height { get; }

    /// <summary>Hit points the building has when whole.</summary>
    public int MaxHitPoints => Balance.HitPoints(Kind);

    /// <summary>Hit points left. The building is destroyed when they reach zero.</summary>
    public int HitPoints { get; internal set; }

    /// <summary>Ticks of Villager work put into the building so far, up to <see cref="BuildTime"/>.</summary>
    public int BuildProgress { get; internal set; }

    /// <summary>
    /// Ticks of Villager work the building takes to complete. Each Villager building it adds
    /// one tick of work per tick, so two finish it in half the time.
    /// </summary>
    public int BuildTime => Balance.BuildTime(Kind);

    /// <summary>Whether the building is complete. Until then it is a construction site and does nothing but block its Cells.</summary>
    public bool IsComplete => BuildProgress == BuildTime;

    /// <summary>
    /// Whether Villagers deliver their loads here: a complete Town Center or Storehouse. Every
    /// kind of drop-off point takes every Resource.
    /// </summary>
    internal bool IsDropOffPoint => Kind is (BuildingKind.TownCenter or BuildingKind.Storehouse) && IsComplete;

    /// <summary>
    /// The units the building is to train, in the order it trains them. Only the first is in
    /// training; the others wait their turn. Each was paid for when it joined the queue. A
    /// building destroyed takes its queue with it: those units are never trained and their cost
    /// is not given back, as the cost of a destroyed construction site is not.
    /// </summary>
    public IReadOnlyList<UnitKind> TrainingQueue => trainingQueue;

    /// <summary>
    /// Ticks spent training the first unit of the training queue, up to its train time; zero
    /// while the queue is empty.
    /// </summary>
    public int TrainingProgress { get; internal set; }

    /// <summary>
    /// The Cell the units the building trains walk to on their own once trained, or null when
    /// they stand where they appear.
    /// </summary>
    public CellPosition? RallyPoint { get; internal set; }

    /// <summary>Puts a unit of the given kind at the end of the training queue.</summary>
    internal void QueueTraining(UnitKind kind) => trainingQueue.Add(kind);

    /// <summary>
    /// Takes the unit at the given position off the training queue and returns its kind. When
    /// it is the first, in training, the next one starts afresh.
    /// </summary>
    internal UnitKind CancelTraining(int position)
    {
        var kind = trainingQueue[position];
        trainingQueue.RemoveAt(position);

        if (position == 0)
        {
            TrainingProgress = 0;
        }

        return kind;
    }

    /// <summary>Takes the first unit off the training queue, done training, and starts the next afresh.</summary>
    internal void FinishTraining()
    {
        trainingQueue.RemoveAt(0);
        TrainingProgress = 0;
    }

    /// <summary>
    /// The free Cell beside the footprint, by a side or by a corner, nearest to
    /// <paramref name="toward"/>; between Cells equally near, the one with the lowest index.
    /// Null when every Cell beside the building is outside the map or taken.
    /// </summary>
    internal CellPosition? FreeCellBeside(MapState map, CellPosition toward)
    {
        CellPosition? nearest = null;
        var nearestDistance = long.MaxValue;

        // Row by row, from the lowest: ascending Cell index, so a strict comparison keeps the lowest index among the equally near.
        for (var y = Origin.Y - 1; y <= Origin.Y + Height; y++)
        {
            for (var x = Origin.X - 1; x <= Origin.X + Width; x++)
            {
                var cell = new CellPosition(x, y);

                if (!IsBeside(cell) || !map.IsFree(cell))
                {
                    continue;
                }

                var distance = ((long)(x - toward.X) * (x - toward.X)) + ((long)(y - toward.Y) * (y - toward.Y));

                if (distance < nearestDistance)
                {
                    nearest = cell;
                    nearestDistance = distance;
                }
            }
        }

        return nearest;
    }

    /// <summary>The Cell of the footprint nearest to the given Cell.</summary>
    internal CellPosition NearestCellTo(CellPosition cell) => new(
        Math.Clamp(cell.X, Origin.X, Origin.X + Width - 1),
        Math.Clamp(cell.Y, Origin.Y, Origin.Y + Height - 1));

    /// <summary>Whether the given Cell lies beside the footprint, by a side or by a corner.</summary>
    internal bool IsBeside(CellPosition cell)
    {
        var nearest = NearestCellTo(cell);

        return cell != nearest && Math.Abs(cell.X - nearest.X) <= 1 && Math.Abs(cell.Y - nearest.Y) <= 1;
    }

    internal void WriteTo(StateHasher hasher)
    {
        hasher.Write(Id.Value);
        hasher.Write(Owner.Value);
        hasher.Write((int)Kind);
        hasher.Write(Origin);
        hasher.Write(Width);
        hasher.Write(Height);
        hasher.Write(HitPoints);
        hasher.Write(BuildProgress);
        hasher.Write(trainingQueue.Count);

        foreach (var kind in trainingQueue)
        {
            hasher.Write((int)kind);
        }

        hasher.Write(TrainingProgress);

        // A building without a rally point writes false and the Cell (0, 0).
        hasher.Write(RallyPoint is not null);
        hasher.Write(RallyPoint ?? new CellPosition(0, 0));
    }
}

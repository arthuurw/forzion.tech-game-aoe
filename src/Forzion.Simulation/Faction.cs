namespace Forzion.Simulation;

/// <summary>
/// A playable people or power, as data: its Ages in order, what each of them unlocks, and the
/// keys of the texts that name it, its Ages and its units. The rules read a Faction and never
/// name one, so a Faction or an Age is added by adding data.
/// </summary>
/// <remarks>
/// Names are not stored as text but as keys of the game's translations, so each language shows
/// its own words.
/// </remarks>
public sealed class Faction
{
    /// <summary>Creates a Faction. The lists are copied, so changing them afterwards changes nothing.</summary>
    /// <param name="id">The ID Players name the Faction by in their configuration.</param>
    /// <param name="nameKey">Key of the text that names the Faction.</param>
    /// <param name="ages">The Faction's Ages in order: the first is Age I, where every Player starts.</param>
    /// <param name="unitNameKeys">Key of the text that names each kind of unit in this Faction.</param>
    /// <exception cref="ArgumentException">There is no Age, or an Age takes a negative time to advance to.</exception>
    public Faction(
        FactionId id,
        string nameKey,
        IReadOnlyList<FactionAge> ages,
        IReadOnlyDictionary<UnitKind, string> unitNameKeys)
    {
        ArgumentNullException.ThrowIfNull(nameKey);
        ArgumentNullException.ThrowIfNull(ages);
        ArgumentNullException.ThrowIfNull(unitNameKeys);

        if (ages.Count == 0)
        {
            throw new ArgumentException("A Faction has at least one Age.", nameof(ages));
        }

        if (ages.Any(age => age.AdvanceTime < 0))
        {
            throw new ArgumentException("No Age takes a negative time to advance to.", nameof(ages));
        }

        Id = id;
        NameKey = nameKey;
        Ages = ages.Select(age => age with { Units = [.. age.Units], Buildings = [.. age.Buildings] }).ToArray();
        UnitNameKeys = new Dictionary<UnitKind, string>(unitNameKeys);
    }

    /// <summary>The Faction's ID, by which each Player's configuration names the Faction it controls.</summary>
    public FactionId Id { get; }

    /// <summary>Key of the text that names the Faction.</summary>
    public string NameKey { get; }

    /// <summary>The Faction's Ages in order: Age I at index 0, Age II at index 1, and so on.</summary>
    public IReadOnlyList<FactionAge> Ages { get; }

    /// <summary>Key of the text that names each kind of unit in this Faction. A kind may have none.</summary>
    public IReadOnlyDictionary<UnitKind, string> UnitNameKeys { get; }

    /// <summary>
    /// Whether a Player of this Faction in the given Age may train units of the given kind:
    /// whether that Age or one before it unlocks them.
    /// </summary>
    /// <param name="kind">The kind of unit.</param>
    /// <param name="age">The number of the Age: 1 for Age I.</param>
    public bool Unlocks(UnitKind kind, int age) => AgesUpTo(age).Any(each => each.Units.Contains(kind));

    /// <summary>
    /// Whether a Player of this Faction in the given Age may place buildings of the given kind:
    /// whether that Age or one before it unlocks them.
    /// </summary>
    /// <param name="kind">The kind of building.</param>
    /// <param name="age">The number of the Age: 1 for Age I.</param>
    public bool Unlocks(BuildingKind kind, int age) => AgesUpTo(age).Any(each => each.Buildings.Contains(kind));

    /// <summary>
    /// Writes what the Faction changes in a match: the cost and time of each Age and what it
    /// unlocks. The text keys change nothing in a match and are left out. What an Age unlocks
    /// is written in ascending kind order, so lists alike but for their order hash alike, as
    /// they play alike.
    /// </summary>
    internal void WriteTo(StateHasher hasher)
    {
        hasher.Write(Ages.Count);

        foreach (var age in Ages)
        {
            hasher.Write(age.AdvanceCost.Food);
            hasher.Write(age.AdvanceCost.Wood);
            hasher.Write(age.AdvanceCost.Gold);
            hasher.Write(age.AdvanceTime);
            hasher.Write(age.Units.Count);

            foreach (var kind in age.Units.Order())
            {
                hasher.Write((int)kind);
            }

            hasher.Write(age.Buildings.Count);

            foreach (var kind in age.Buildings.Order())
            {
                hasher.Write((int)kind);
            }
        }
    }

    private IEnumerable<FactionAge> AgesUpTo(int age) => Ages.Take(Math.Max(age, 0));
}

/// <summary>One Age of a Faction: what the Faction calls it, what advancing to it takes and what it unlocks.</summary>
/// <param name="NameKey">Key of the text that names the Age in this Faction.</param>
/// <param name="AdvanceCost">
/// What the Age Advance to this Age costs, paid in full when it is ordered. Unused for Age I,
/// where every Player starts.
/// </param>
/// <param name="AdvanceTime">
/// Ticks the Age Advance to this Age takes, from the tick it is ordered. Unused for Age I.
/// </param>
/// <param name="Units">The kinds of unit this Age unlocks, on top of what the Ages before it unlocked.</param>
/// <param name="Buildings">The kinds of building this Age unlocks, on top of what the Ages before it unlocked.</param>
public sealed record FactionAge(
    string NameKey,
    Cost AdvanceCost,
    int AdvanceTime,
    IReadOnlyList<UnitKind> Units,
    IReadOnlyList<BuildingKind> Buildings);

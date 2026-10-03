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

    public FactionId Id { get; }

    /// <summary>Key of the text that names the Faction.</summary>
    public string NameKey { get; }

    /// <summary>The Faction's Ages in order: Age I at index 0, Age II at index 1, and so on.</summary>
    public IReadOnlyList<FactionAge> Ages { get; }

    /// <summary>Key of the text that names each kind of unit in this Faction. A kind may have none.</summary>
    public IReadOnlyDictionary<UnitKind, string> UnitNameKeys { get; }
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

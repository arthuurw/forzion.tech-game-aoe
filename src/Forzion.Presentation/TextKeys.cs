using System.Text;
using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// Keys of the game's texts for what the simulation names by an enum: buildings and Resources,
/// and units whose Faction gives them no name. The words live in the game's translations.
/// </summary>
public static class TextKeys
{
    /// <summary>The key of the text naming a kind of building, such as <c>BUILDING_TOWN_CENTER</c>. Every Faction shares them.</summary>
    public static string NameOf(BuildingKind kind) => "BUILDING_" + UpperSnakeCase(kind.ToString());

    /// <summary>The key of the text naming a Resource, such as <c>RESOURCE_FOOD</c>.</summary>
    public static string NameOf(ResourceKind kind) => "RESOURCE_" + UpperSnakeCase(kind.ToString());

    /// <summary>
    /// The key of the text naming a kind of unit in the Faction: the Faction's own, or the
    /// generic name, such as <c>UNIT_MELEE_SOLDIER</c>, when the Faction gives the kind none.
    /// </summary>
    public static string NameOf(Faction faction, UnitKind kind)
    {
        ArgumentNullException.ThrowIfNull(faction);

        return faction.UnitNameKeys.TryGetValue(kind, out var key) ? key : "UNIT_" + UpperSnakeCase(kind.ToString());
    }

    /// <summary>A name in Pascal case, such as <c>TownCenter</c>, in upper snake case: <c>TOWN_CENTER</c>.</summary>
    internal static string UpperSnakeCase(string name)
    {
        var key = new StringBuilder();

        for (var index = 0; index < name.Length; index++)
        {
            if (index > 0 && char.IsUpper(name[index]))
            {
                key.Append('_');
            }

            key.Append(char.ToUpperInvariant(name[index]));
        }

        return key.ToString();
    }
}

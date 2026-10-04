using Forzion.Simulation;

namespace Forzion.Presentation.Tests;

public class TextKeysTests
{
    [Fact]
    public void A_unit_is_named_by_its_Faction_or_else_by_its_generic_name()
    {
        Assert.Equal("TEST_VILLAGER", TextKeys.NameOf(TestMatches.ThreeAges, UnitKind.Villager));
        Assert.Equal("UNIT_MELEE_SOLDIER", TextKeys.NameOf(TestMatches.ThreeAges, UnitKind.MeleeSoldier));
    }

    // A kind added to the simulation without a text fails here instead of showing a bare key
    // on screen.
    [Fact]
    public void Every_building_Resource_and_generic_unit_name_has_a_Portuguese_text_in_the_game_translations()
    {
        var texts = TranslationTable.Load("pt_BR");
        var noUnitNames = new Faction(
            new FactionId(99), "TEST", [new FactionAge("TEST_AGE_1", new Cost(0, 0, 0), 0, [], [])], new Dictionary<UnitKind, string>());
        var keys = Enum.GetValues<BuildingKind>().Select(TextKeys.NameOf)
            .Concat(Enum.GetValues<ResourceKind>().Select(TextKeys.NameOf))
            .Concat(Enum.GetValues<UnitKind>().Select(kind => TextKeys.NameOf(noUnitNames, kind)));

        Assert.All(keys, key => Assert.True(texts.TryGetValue(key, out var text) && text.Length > 0, $"No Portuguese text for {key}."));
    }
}

using Forzion.Simulation;

namespace Forzion.Presentation.Tests;

public class FactionTextsTests
{
    // The Factions name themselves, their Ages and their units by keys of the game's
    // translations. A key added to a Faction without a text fails here instead of showing a
    // bare key on screen.
    [Fact]
    public void Every_name_of_every_Faction_has_a_Portuguese_text_in_the_game_translations()
    {
        var texts = TranslationTable.Load("pt_BR");
        var keys = Factions.All.SelectMany(faction =>
            new[] { faction.NameKey }
                .Concat(faction.Ages.Select(age => age.NameKey))
                .Concat(faction.UnitNameKeys.Values));

        Assert.All(keys, key => Assert.True(texts.TryGetValue(key, out var text) && text.Length > 0, $"No Portuguese text for {key}."));
    }
}

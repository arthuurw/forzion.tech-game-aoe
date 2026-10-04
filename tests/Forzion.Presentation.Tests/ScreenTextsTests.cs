using System.Reflection;

namespace Forzion.Presentation.Tests;

public class ScreenTextsTests
{
    // A text added to a screen without a translation fails here instead of showing a bare key.
    [Fact]
    public void Every_text_of_the_main_menu_the_pause_and_the_end_screen_has_a_Portuguese_text_in_the_game_translations()
    {
        var texts = TranslationTable.Load("pt_BR");
        var keys = typeof(ScreenTexts)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetValue(null)!)
            .ToList();

        Assert.NotEmpty(keys);
        Assert.All(keys, key => Assert.True(texts.TryGetValue(key, out var text) && text.Length > 0, $"No Portuguese text for {key}."));
    }

    [Fact]
    public void The_end_screen_reads_victory_or_defeat_and_why_in_Portuguese()
    {
        var texts = TranslationTable.Load("pt_BR");

        Assert.Equal("Vitória!", texts[ScreenTexts.TitleOf(MatchOutcome.Victory)]);
        Assert.Equal("Você destruiu o Centro inimigo.", texts[ScreenTexts.ReasonOf(MatchOutcome.Victory)]);
        Assert.Equal("Derrota", texts[ScreenTexts.TitleOf(MatchOutcome.Defeat)]);
        Assert.Equal("Seu Centro foi destruído.", texts[ScreenTexts.ReasonOf(MatchOutcome.Defeat)]);
    }
}

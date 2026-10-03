using System.Text;

namespace Forzion.Presentation.Tests;

/// <summary>
/// Reads one locale of the game's translations: the gettext file
/// <c>game/translations/&lt;locale&gt;.po</c> that Godot loads, whose message IDs are the keys
/// the code uses.
/// </summary>
internal static class TranslationTable
{
    /// <summary>The texts of the locale, by key. The header entry, with the empty key, is left out.</summary>
    public static Dictionary<string, string> Load(string locale)
    {
        var texts = new Dictionary<string, string>();
        string? key = null;
        StringBuilder? current = null;
        var inText = false;

        void Finish()
        {
            if (key is { Length: > 0 } && inText && current is not null)
            {
                texts[key] = current.ToString();
            }
        }

        foreach (var raw in File.ReadAllLines(PathOf(locale), Encoding.UTF8))
        {
            var line = raw.Trim();

            if (line.StartsWith("msgid ", StringComparison.Ordinal))
            {
                Finish();
                current = new StringBuilder(Unquote(line["msgid ".Length..]));
                inText = false;
            }
            else if (line.StartsWith("msgstr ", StringComparison.Ordinal))
            {
                key = current?.ToString();
                current = new StringBuilder(Unquote(line["msgstr ".Length..]));
                inText = true;
            }
            else if (line.StartsWith('"'))
            {
                // A string continued on the next line.
                current?.Append(Unquote(line));
            }
        }

        Finish();

        return texts;
    }

    private static string PathOf(string locale)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Forzion.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);

        return Path.Combine(directory.FullName, "game", "translations", $"{locale}.po");
    }

    private static string Unquote(string quoted) =>
        quoted.Trim().Trim('"').Replace("\\\"", "\"", StringComparison.Ordinal).Replace("\\n", "\n", StringComparison.Ordinal);
}

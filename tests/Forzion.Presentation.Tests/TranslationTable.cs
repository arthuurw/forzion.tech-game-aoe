using System.Text;

namespace Forzion.Presentation.Tests;

/// <summary>
/// Reads the game's translation table, <c>game/translations/texts.csv</c>: a CSV whose first
/// row is <c>keys</c> followed by one locale per column, the format Godot imports.
/// </summary>
internal static class TranslationTable
{
    /// <summary>The texts of one locale, by key.</summary>
    public static Dictionary<string, string> Load(string locale)
    {
        var rows = File.ReadAllLines(PathOfTable(), Encoding.UTF8)
            .Where(line => line.Length > 0)
            .Select(ParseRow)
            .ToList();

        var column = rows[0].IndexOf(locale);

        Assert.True(column > 0, $"The translation table has no {locale} column.");

        return rows.Skip(1).ToDictionary(row => row[0], row => column < row.Count ? row[column] : "");
    }

    private static string PathOfTable()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Forzion.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);

        return Path.Combine(directory.FullName, "game", "translations", "texts.csv");
    }

    // Fields are separated by commas; a field in double quotes may hold commas, and a doubled
    // quote inside it stands for one quote.
    private static List<string> ParseRow(string line)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        var quoted = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];

            if (quoted && character == '"' && index + 1 < line.Length && line[index + 1] == '"')
            {
                field.Append('"');
                index++;
            }
            else if (character == '"')
            {
                quoted = !quoted;
            }
            else if (character == ',' && !quoted)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(character);
            }
        }

        fields.Add(field.ToString());

        return fields;
    }
}

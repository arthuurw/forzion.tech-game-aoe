using Godot;

namespace Forzion.Game;

/// <summary>
/// The look shared by the screens that cover the whole window: the main menu, the pause and
/// the end screen. Each is a heading and a column of buttons in the middle of the window.
/// </summary>
public static class Overlays
{
    /// <summary>The solid background of the main menu.</summary>
    public static readonly Color Backdrop = new(0.11f, 0.11f, 0.12f);

    /// <summary>What covers the match behind the pause and the end screen: dark, but still see-through.</summary>
    public static readonly Color Veil = new(0, 0, 0, 0.6f);

    /// <summary>A screen-wide veil that stops the mouse from reaching what lies under it.</summary>
    public static ColorRect NewVeil()
    {
        var veil = new ColorRect { Name = "Veil", Color = Veil, MouseFilter = Control.MouseFilterEnum.Stop };
        veil.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        return veil;
    }

    /// <summary>The given controls one under another, in the middle of the window.</summary>
    public static CenterContainer NewColumn(params Control[] controls)
    {
        var centre = new CenterContainer { Name = "Centre", MouseFilter = Control.MouseFilterEnum.Ignore };
        centre.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var column = new VBoxContainer { Name = "Column" };
        column.AddThemeConstantOverride("separation", 16);
        centre.AddChild(column);

        foreach (var control in controls)
        {
            column.AddChild(control);
        }

        return centre;
    }

    /// <summary>A large line of text, centred.</summary>
    public static Label NewHeading(string text, Color colour) => NewText(text, 48, colour);

    /// <summary>A line of text, centred.</summary>
    public static Label NewText(string text, int size, Color colour)
    {
        var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", colour);
        label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0));
        label.AddThemeConstantOverride("outline_size", 6);

        return label;
    }

    /// <summary>A wide button of the column.</summary>
    public static Button NewButton(string text)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(360, 56) };
        button.AddThemeFontSizeOverride("font_size", 22);

        return button;
    }
}

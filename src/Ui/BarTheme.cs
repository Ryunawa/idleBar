using Godot;

namespace IdleBar.Ui;

public static class BarTheme
{
    private const string ButtonType = "Button";

    public static Theme Create()
    {
        Theme theme = new() { DefaultFontSize = 13 };
        theme.SetStylebox("normal", ButtonType, CreateButtonBox(BarPalette.ButtonNormal));
        theme.SetStylebox("hover", ButtonType, CreateButtonBox(BarPalette.ButtonHover));
        theme.SetStylebox("pressed", ButtonType, CreateButtonBox(BarPalette.ButtonPressed));
        theme.SetStylebox("disabled", ButtonType, CreateButtonBox(BarPalette.ButtonDisabled));
        theme.SetStylebox("focus", ButtonType, new StyleBoxEmpty());
        theme.SetColor("font_color", ButtonType, BarPalette.Text);
        theme.SetColor("font_hover_color", ButtonType, BarPalette.Text);
        theme.SetColor("font_pressed_color", ButtonType, BarPalette.Text);
        theme.SetColor("font_disabled_color", ButtonType, BarPalette.Muted);
        theme.SetColor("font_color", "Label", BarPalette.Text);
        return theme;
    }

    public static StyleBoxFlat CreateBackground()
    {
        StyleBoxFlat box = new() { BgColor = BarPalette.Background, BorderColor = BarPalette.Border, BorderWidthTop = 1 };
        return box;
    }

    private static StyleBoxFlat CreateButtonBox(Color color)
    {
        StyleBoxFlat box = new()
        {
            BgColor = color,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 2,
            ContentMarginBottom = 2,
        };
        box.SetCornerRadiusAll(4);
        return box;
    }
}

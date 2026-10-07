using Godot;

namespace IdleBar.Ui;

public static class BarLabels
{
    public static Label Create(int fontSize, Color color, HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        Label label = new()
        {
            HorizontalAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        label.AddThemeFontSizeOverride("font_size", PixelFont.Size(fontSize));
        label.AddThemeColorOverride("font_color", color);
        return label;
    }
}

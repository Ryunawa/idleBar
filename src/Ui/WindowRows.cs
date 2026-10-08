using Godot;

namespace IdleBar.Ui;

public static class WindowRows
{
    public static Label Heading(string text)
    {
        Label label = new() { Text = text };
        label.AddThemeColorOverride("font_color", BarPalette.Gold);
        return label;
    }

    public static Label Muted(string text)
    {
        Label label = new() { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeColorOverride("font_color", BarPalette.Muted);
        label.AddThemeFontSizeOverride("font_size", PixelFont.Size(12));
        return label;
    }

    public static PanelContainer Card(Control content)
    {
        PanelContainer card = new() { ThemeTypeVariation = WindowSkin.Card };
        card.AddChild(content);
        return card;
    }

    public static ScrollContainer Scroll(Control content)
    {
        ScrollContainer scroll = new()
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(content);
        return scroll;
    }

    public static void Clear(Node container)
    {
        foreach (Node child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }
}

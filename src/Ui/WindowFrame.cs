using Godot;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public static class WindowFrame
{
    private const int Margin = 18;
    private const int TopMargin = 10;
    private const int TitleGap = 6;
    private const int TitleSpace = WindowTitleBar.Height + TitleGap - (Margin - TopMargin);

    public static VBoxContainer Build(Window window, string title, int separation)
    {
        window.Title = title;
        window.Borderless = true;
        window.Unresizable = true;
        window.Transient = false;
        window.AlwaysOnTop = true;
        window.Visible = false;
        window.CloseRequested += window.Hide;

        window.Theme = WindowSkin.Create(WindowStyles.Default);
        window.CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest;
        Panel background = new();
        window.AddChild(background);
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        MarginContainer margin = new();
        foreach (string side in new[] { "margin_left", "margin_right", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, Margin);
        }

        margin.AddThemeConstantOverride("margin_top", TopMargin);
        window.AddChild(margin);
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        VBoxContainer frame = new();
        frame.AddThemeConstantOverride("separation", TitleGap);
        margin.AddChild(frame);
        frame.AddChild(new WindowTitleBar());

        VBoxContainer content = new() { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        content.AddThemeConstantOverride("separation", separation);
        frame.AddChild(content);
        return content;
    }

    public static void Present(Window window, Vector2I baseSize, float scale)
    {
        window.ContentScaleFactor = scale;
        window.Size = new Vector2I((int)(baseSize.X * scale), (int)((baseSize.Y + TitleSpace) * scale));
        window.Show();
        window.CurrentScreen = DisplayServer.WindowGetCurrentScreen();
        window.MoveToCenter();
        window.GrabFocus();
    }

    public static Label CreateMessage()
    {
        Label message = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        message.AddThemeColorOverride("font_color", BarPalette.Danger);
        return message;
    }
}

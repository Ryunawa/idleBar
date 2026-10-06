using Godot;

namespace IdleBar.Ui;

public static class WindowFrame
{
    private const int Margin = 18;

    public static VBoxContainer Build(Window window, string title, int separation)
    {
        window.Title = title;
        window.Unresizable = true;
        window.Transient = false;
        window.AlwaysOnTop = true;
        window.Visible = false;
        window.CloseRequested += window.Hide;

        Panel background = new();
        background.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = BarPalette.Background });
        window.AddChild(background);
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        MarginContainer margin = new();
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, Margin);
        }

        window.AddChild(margin);
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        VBoxContainer content = new();
        content.AddThemeConstantOverride("separation", separation);
        margin.AddChild(content);
        return content;
    }

    public static void Present(Window window, Vector2I baseSize, float scale)
    {
        window.ContentScaleFactor = scale;
        window.Size = new Vector2I((int)(baseSize.X * scale), (int)(baseSize.Y * scale));
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

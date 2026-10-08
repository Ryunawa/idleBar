using System;
using Godot;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public partial class ChatBox : Window
{
    private const int MaxLength = 120;
    private const int Gap = 8;
    private const int Margin = 6;
    private static readonly Vector2I BaseSize = new(380, 44);

    private LineEdit _line = null!;

    public event Action<string>? Submitted;

    public override void _Ready()
    {
        Title = "IdleBar · Parler";
        Borderless = true;
        Unresizable = true;
        Transient = false;
        AlwaysOnTop = true;
        Visible = false;
        Theme = WindowSkin.Create(WindowStyles.Default);
        Panel background = new();
        AddChild(background);
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        MarginContainer margin = new();
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, Margin);
        }

        AddChild(margin);
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _line = new LineEdit
        {
            MaxLength = MaxLength,
            PlaceholderText = "Dis quelque chose à la tablée… (Entrée pour envoyer)",
            CaretBlink = true,
        };
        _line.TextSubmitted += Send;
        margin.AddChild(_line);
        FocusExited += Hide;
        CloseRequested += Hide;
    }

    public void Open(Vector2I anchor, float scale)
    {
        ContentScaleFactor = scale;
        Size = new Vector2I((int)(BaseSize.X * scale), (int)(BaseSize.Y * scale));
        Rect2I usable = DisplayServer.ScreenGetUsableRect(DisplayServer.GetScreenFromRect(new Rect2(anchor, Vector2.One)));
        Position = new Vector2I(
            Math.Clamp(anchor.X - Size.X / 2, usable.Position.X, Math.Max(usable.Position.X, usable.End.X - Size.X)),
            Math.Clamp(anchor.Y - Size.Y - Gap, usable.Position.Y, Math.Max(usable.Position.Y, usable.End.Y - Size.Y)));
        Show();
        GrabFocus();
        _line.GrabFocus();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
        {
            Hide();
        }
    }

    private void Send(string text)
    {
        if (text.Trim().Length > 0)
        {
            Submitted?.Invoke(text.Trim());
        }

        _line.Text = string.Empty;
        Hide();
    }
}

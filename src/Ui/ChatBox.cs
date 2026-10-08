using System;
using Godot;

namespace IdleBar.Ui;

public partial class ChatBox : PopupPanel
{
    private const int MaxLength = 120;
    private const int Gap = 8;
    private static readonly Vector2I BaseSize = new(360, 34);

    private LineEdit _line = null!;

    public event Action<string>? Submitted;

    public override void _Ready()
    {
        _line = new LineEdit
        {
            MaxLength = MaxLength,
            PlaceholderText = "Dis quelque chose à la tablée… (Entrée pour envoyer)",
            CaretBlink = true,
        };
        _line.TextSubmitted += Send;
        AddChild(_line);
    }

    public void Open(Vector2I anchor, float scale)
    {
        Vector2I size = new((int)(BaseSize.X * scale), (int)(BaseSize.Y * scale));
        ContentScaleFactor = scale;
        Popup(new Rect2I(anchor.X - size.X / 2, anchor.Y - size.Y - Gap, size.X, size.Y));
        _line.GrabFocus();
    }

    public override void _UnhandledKeyInput(InputEvent @event)
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

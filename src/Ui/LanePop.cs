using Godot;

namespace IdleBar.Ui;

public sealed class LanePop
{
    public LanePop(string text, int x, Color color)
    {
        Text = text;
        X = x;
        Color = color;
    }

    public string Text { get; }

    public int X { get; }

    public Color Color { get; }

    public float Age { get; set; }
}

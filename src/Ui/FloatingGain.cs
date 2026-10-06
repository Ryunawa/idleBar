using Godot;

namespace IdleBar.Ui;

public sealed class FloatingGain
{
    public FloatingGain(Vector2 origin, string text)
    {
        Origin = origin;
        Text = text;
    }

    public Vector2 Origin { get; }

    public string Text { get; }

    public float Age { get; set; }
}

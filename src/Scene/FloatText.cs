namespace IdleBar.Scene;

public sealed class FloatText
{
    public FloatText(string text, float x, float y, Rgba color, float life, float rise)
    {
        Text = text;
        X = x;
        Y = y;
        Color = color;
        Life = life;
        Rise = rise;
    }

    public string Text { get; }

    public float X { get; }

    public float Y { get; set; }

    public Rgba Color { get; }

    public float Life { get; set; }

    public float Rise { get; }
}

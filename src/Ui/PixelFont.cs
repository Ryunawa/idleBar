using Godot;

namespace IdleBar.Ui;

public static class PixelFont
{
    public const int Body = 19;

    public const int Title = 28;

    private const string FontPath = "res://assets/fonts/Jersey10.ttf";

    public static Font Load() => GD.Load<Font>(FontPath);

    public static int Size(int requested) => requested switch
    {
        <= 13 => 16,
        <= 16 => Body,
        <= 20 => 22,
        _ => Title,
    };
}

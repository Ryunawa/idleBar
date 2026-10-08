using Godot;

namespace IdleBar.Pixel;

public static class WindowStyles
{
    public static WindowStyle Default { get; } = new(
        new Color("6e5034"), new Color("a07850"), new Color("33231a"),
        new Color("1e1a17"), new Color("161310"), new Color("2a2420"), new Color("f2c14e"));
}

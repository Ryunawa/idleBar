using System.Collections.Generic;
using Godot;

namespace IdleBar.Pixel;

public static class WindowStyles
{
    public static WindowStyle Default { get; } = new(
        new Color("6e5034"), new Color("a07850"), new Color("33231a"),
        new Color("1e1a17"), new Color("161310"), new Color("2a2420"), new Color("f2c14e"));

    private static readonly Dictionary<string, WindowStyle> Towns = new()
    {
        ["port-sable"] = new(
            new Color("8a6a44"), new Color("c9a76b"), new Color("3e2c1c"),
            new Color("172532"), new Color("111c26"), new Color("1f3244"), new Color("7fd1d4")),
        ["hautecombe"] = new(
            new Color("6e5236"), new Color("a8865a"), new Color("33251a"),
            new Color("1b2618"), new Color("141c12"), new Color("263421"), new Color("e3d39a")),
        ["vignelune"] = new(
            new Color("6b3f2a"), new Color("a5694a"), new Color("2e1812"),
            new Color("26131e"), new Color("1c0d16"), new Color("361b2a"), new Color("e08fb4")),
        ["bois-dormant"] = new(
            new Color("5a4430"), new Color("8c6c4a"), new Color("261c13"),
            new Color("142018"), new Color("0e1711"), new Color("1d2e22"), new Color("9bd46f")),
        ["brumeval"] = new(
            new Color("4f5a5a"), new Color("84918d"), new Color("202828"),
            new Color("152224"), new Color("0f191b"), new Color("1e3133"), new Color("b79be3")),
        ["rocheclaire"] = new(
            new Color("6e6c68"), new Color("a7a39c"), new Color("2c2b29"),
            new Color("1b1e21"), new Color("141619"), new Color("272b30"), new Color("eab872")),
        ["ferrenoire"] = new(
            new Color("4a4d52"), new Color("7d828a"), new Color("1b1c1f"),
            new Color("1b1817"), new Color("131110"), new Color("2a2422"), new Color("f0883e")),
        ["ambrevault"] = new(
            new Color("a0703c"), new Color("dcae6e"), new Color("4a2e14"),
            new Color("261a10"), new Color("1c130b"), new Color("372616"), new Color("f5bf4f")),
    };

    public static WindowStyle For(string? townId) =>
        townId is not null && Towns.TryGetValue(townId, out WindowStyle? style) ? style : Default;
}

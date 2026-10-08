using System.Collections.Generic;
using Godot;

namespace IdleBar.Pixel;

public static class RegularLooks
{
    public const string Ghost = "fantome";

    private static readonly Dictionary<string, PatronLook> Looks = new()
    {
        ["gaspard"] = new(4, new("f1c27d"), new("c9c9c9"), new("6b6f78"), new("7a2f2f")),
        ["melisande"] = new(1, new("ffdbac"), new("a0522d"), new("557f3f"), new("3f5a2f")),
        ["brindille"] = new(2, new("f1c27d"), new("1c1c22"), new("7d5a96"), new("5a3a7a")),
        ["odette"] = new(3, new("e0ac69"), new("5a3825"), new("40689c"), new("2f4f6f")),
        [Ghost] = new(0, new("dfe8f5"), new("f0f4fa"), new("c8d4e8"), new("c8d4e8")),
        ["bartholome"] = new(3, new("c68642"), new("2b1d14"), new("a3793f"), new("9c4040")),
        ["ysolde"] = new(3, new("e0ac69"), new("1c1c22"), new("2f4b7a"), new("1f2f4f")),
        ["pip"] = new(0, new("7fb24a"), new("2f5a1a"), new("8a6a44"), new("5a3a2a")),
        ["anselme"] = new(2, new("f1c27d"), new("5a3825"), new("6e5034"), new("6e5034")),
        ["lune"] = new(5, new("ffdbac"), new("c9c9c9"), new("2f3f7a"), new("f2c14e")),
        ["fennec"] = new(2, new("c68642"), new("2b1d14"), new("3a3a42"), new("2a2a30")),
        ["hugues"] = new(0, new("f1c27d"), new("e3c16f"), new("9c4040"), new("a3793f")),
    };

    public static PatronLook? For(string? regular) =>
        regular is not null && Looks.TryGetValue(regular, out PatronLook? look) ? look : null;
}

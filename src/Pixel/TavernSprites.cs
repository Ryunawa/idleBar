using System.Linq;

namespace IdleBar.Pixel;

public static class TavernSprites
{
    private static readonly string[] TeapotRows =
    [
        "....QQ....",
        "...QqqQ...",
        ".QQqqqqQ.Q",
        "Q.QqhqqqQQ",
        "Q.QqqqqqQ.",
        ".QQqqqqQ..",
        "...QQQQ...",
    ];

    public static PixelSprite Barrel { get; } = PixelSprite.Parse(
        "..BBBBBBBB...",
        ".BoZooooZoB..",
        "BooZooooZooB.",
        "BbbZbbbbZbbnn",
        "BbbZbbbbZbbBn",
        "BOOZOOOOZOOB.",
        ".BOZOOOOZOB..",
        "..BBBBBBBB...",
        ".B.B....B.B..",
        "BBBB....BBBB.");

    public static PixelSprite Teapot { get; } = PixelSprite.Parse(TeapotRows);

    public static PixelSprite TeapotHalo { get; } = PixelSprite.Parse(TeapotRows.Select(row => new string(row.Select(symbol => symbol == '.' ? '.' : 'L').ToArray())).ToArray());

    public static PixelSprite Brazier { get; } = PixelSprite.Parse(
        "ZZZZZZZ",
        ".ZaIaZ.",
        "..Z.Z..");

    public static PixelSprite BrazierGlow { get; } = PixelSprite.Parse(
        "ZZZZZZZ",
        ".ZIaIZ.",
        "..Z.Z..");

    public static PixelSprite Lantern { get; } = PixelSprite.Parse(
        "..Z..",
        ".ZZZ.",
        ".zLz.",
        ".zzz.",
        ".ZZZ.");

    public static PixelSprite BeerIcon { get; } = PixelSprite.Parse(
        "ffff.",
        "eeeen",
        "eeee.",
        "eeeen",
        "NNNN.");

    public static PixelSprite TeaIcon { get; } = PixelSprite.Parse(
        ".s.s.",
        "s.s..",
        "qjjq.",
        "qqqqq",
        ".qq..");

    public static PixelSprite[] Bottles { get; } =
    [
        PixelSprite.Parse(".n.", ".G.", "GGG", "GGG", "GGG"),
        PixelSprite.Parse(".n.", ".P.", "PPP", "PPP", "PPP"),
        PixelSprite.Parse("...", "nnn", "nNn", "nnn", "NNN"),
        PixelSprite.Parse(".n.", ".e.", "eee", "eee", "eee"),
    ];
}

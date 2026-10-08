namespace IdleBar.Pixel;

public static class FestivalSprites
{
    public static PixelSprite Pumpkin { get; } = PixelSprite.Parse("..V..", ".aaa.", "aaaaa", "aIaIa", ".aaa.");

    public static PixelSprite Lantern { get; } = PixelSprite.Parse("..V..", ".aaa.", "aLaLa", "aLLLa", ".aaa.");

    public static PixelSprite Fir { get; } = PixelSprite.Parse(
        "....g....",
        "....V....",
        "...VVV...",
        "..VrVVV..",
        "...VVV...",
        "..VVVgV..",
        ".VVVVVVV.",
        "..VqVVV..",
        ".VVVVVrV.",
        "VVVVVVVVV",
        "....B....",
        "...BBB...");

    public static PixelSprite Firewood { get; } = PixelSprite.Parse(
        "..OOOOOO..",
        ".bbbbbbbb.",
        "OOOOOOOOOO",
        "bbbbbbbbbb",
        "OOOOOOOOOO");

    public static PixelSprite Sunflower { get; } = PixelSprite.Parse(
        ".g.g.",
        "ggBgg",
        ".gBg.",
        "..V..",
        ".VV..",
        "..V..",
        "..VV.",
        ".rrr.",
        ".RrR.");

    public static PixelSprite Flowers { get; } = PixelSprite.Parse(
        "r.L.r",
        ".rLq.",
        "..V..",
        ".VVV.",
        "..V..",
        ".qqq.",
        ".QqQ.");

    public static PixelSprite Cobweb { get; } = PixelSprite.Parse(
        "WWWWWW",
        "WW.W..",
        "W.W...",
        "WW....",
        "W.....",
        "W.....");
}

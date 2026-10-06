namespace IdleBar.Pixel;

public static class TownSprites
{
    public static PixelSprite House { get; } = PixelSprite.Parse(
        "...rrrr...",
        "..rRRRRr..",
        ".rRRRRRRr.",
        "rrrrrrrrrr",
        ".yyyyyyyy.",
        ".yLLyyLLy.",
        ".yLLyyLLy.",
        ".yyyyyyyy.",
        ".yyyxxyyy.",
        ".yyyxxyyy.");

    public static PixelSprite TallHouse { get; } = PixelSprite.Parse(
        "...RR...",
        "..RrrR..",
        ".RrrrrR.",
        "RRRRRRRR",
        ".YyyyyY.",
        ".YLyyLY.",
        ".YyyyyY.",
        ".YLyyLY.",
        ".YyyyyY.",
        ".YyxxyY.",
        ".YyxxyY.");

    public static PixelSprite Stall { get; } = PixelSprite.Parse(
        "rwrwrwrwrw",
        "rwrwrwrwrw",
        ".b......b.",
        ".b.gaV..b.",
        ".bbbbbbbb.",
        ".b......b.",
        ".b......b.");

    public static PixelSprite Lamp { get; } = PixelSprite.Parse(
        ".L.",
        "kLk",
        ".k.",
        ".k.",
        ".k.",
        ".k.",
        ".k.",
        "kkk");
}

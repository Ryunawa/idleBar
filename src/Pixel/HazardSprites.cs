namespace IdleBar.Pixel;

public static class HazardSprites
{
    public static PixelSprite Bandit { get; } = PixelSprite.Parse(
        "..xx....",
        ".xxxx...",
        "..pp....",
        ".rrrr...",
        ".xxxxb..",
        ".xxxx.b.",
        ".xxxx...",
        ".xxxx...",
        "..k.k...",
        "..k.k...");

    public static PixelSprite Barrier { get; } = PixelSprite.Parse(
        "bb............",
        "bbrrwwrrwwrrww",
        "bb............",
        "bb............",
        "bb............",
        "bb............",
        "BB............");
}

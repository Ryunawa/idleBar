namespace IdleBar.Pixel;

public static class WorkerSprites
{
    private static readonly string[] Legs = ["..k.k...", "..k.k..."];

    private static readonly string[] Bench = [".bbbbb.", ".b...b.", ".b...b."];

    public static PixelSprite HammerRaised { get; } = PixelSprite.Parse(
    [
        ".....ss.",
        ".....b..",
        "..pp.b..",
        "..pp.b..",
        ".qqqqp..",
        ".qqqq...",
        ".qBBq...",
        ".qBBq...",
        .. Legs,
    ]);

    public static PixelSprite HammerStruck { get; } = PixelSprite.Parse(
    [
        "........",
        "........",
        "..pp....",
        "..pp....",
        ".qqqqpbb",
        ".qqqq.ss",
        ".qBBq...",
        ".qBBq...",
        .. Legs,
    ]);

    public static PixelSprite WeaverPulling { get; } = PixelSprite.Parse(
    [
        "..pp...",
        "..pp...",
        ".qqqqp.",
        ".qqqq..",
        ".qqqq..",
        .. Bench,
    ]);

    public static PixelSprite WeaverPushing { get; } = PixelSprite.Parse(
    [
        "..pp...",
        "..pp...",
        ".qqqq.p",
        ".qqqq..",
        ".qqqq..",
        .. Bench,
    ]);

    public static PixelSprite StirrerLow { get; } = PixelSprite.Parse(
    [
        "........",
        "........",
        "..pp....",
        "..pp....",
        ".qqqqpb.",
        ".qqqq..b",
        ".qVVq...",
        ".qVVq...",
        .. Legs,
    ]);

    public static PixelSprite StirrerHigh { get; } = PixelSprite.Parse(
    [
        "........",
        "........",
        "..pp....",
        "..pp.b..",
        ".qqqqpb.",
        ".qqqq.b.",
        ".qVVq...",
        ".qVVq...",
        .. Legs,
    ]);
}

namespace IdleBar.Pixel;

public static class CounterSprites
{
    public static PixelSprite StallTop { get; } = PixelSprite.Parse(
        "ggRRggRRggRRggRRggRR",
        "ggRRggRRggRRggRRggRR",
        "ggRRggRRggRRggRRggRR",
        "g.R.g.R.g.R.g.R.g.R.",
        ".b................b.",
        ".b................b.",
        ".b................b.",
        ".b................b.",
        ".b................b.",
        ".b................b.");

    public static PixelSprite CounterFront { get; } = PixelSprite.Parse(
        "oooooooooooooooooooo",
        ".ObbbbbbbbbbbbbbbbO.",
        ".ObBBbbbbBBbbbbBBbO.",
        ".ObbbbbbbbbbbbbbbbO.",
        ".OOOOOOOOOOOOOOOOOO.");

    public static PixelSprite Scales { get; } = PixelSprite.Parse(
        "...k...",
        "kkkkkkk",
        "k..k..k",
        "gg.k.gg",
        "..kkk..");

    public static PixelSprite Coins { get; } = PixelSprite.Parse(
        "g...",
        "a.g.",
        "g.a.",
        "a.g.");

    public static PixelSprite Sacks { get; } = PixelSprite.Parse(
        ".yy..yy.",
        "yYyy.yYy",
        "yyYyyyyY",
        "yyyyyyyy");
}

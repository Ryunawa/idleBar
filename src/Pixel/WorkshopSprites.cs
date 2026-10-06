namespace IdleBar.Pixel;

public static class WorkshopSprites
{
    public static PixelSprite Forge { get; } = PixelSprite.Parse(
        "...........tt.....",
        "...........St.....",
        "..RRRRRRRRRStRRR..",
        ".RRRRRRRRRRRRRRRR.",
        "RRRRRRRRRRRRRRRRRR",
        ".StSStSStSStSStSt.",
        ".tSSSSSSSSSSSSSSS.",
        ".SS.........SStSS.",
        ".St.........StSSt.",
        ".SS.........SSSSS.",
        ".St.........StSSt.",
        ".SS.........SSSSS.",
        ".SSSSSSSSSSSSSSSS.");

    public static PixelSprite Shed { get; } = PixelSprite.Parse(
        "....RRRRRRRR....",
        "...RrrrrrrrrR...",
        "..RrrrrrrrrrrR..",
        ".RRRRRRRRRRRRRR.",
        "..bBbBbBbBbBbB..",
        "..BbLLbBbBxxbB..",
        "..bBLLbBbBxxBb..",
        "..BbBbBbBbxxbB..",
        "..bBbBbBbBxxBb..");

    public static PixelSprite Anvil { get; } = PixelSprite.Parse(
        "SSSSSSS",
        ".SSSSS.",
        "..SSS..",
        ".SSSSS.");

    public static PixelSprite WheelStand { get; } = PixelSprite.Parse(
        "..bbb..",
        ".b.b.b.",
        "b..b..b",
        "bbbsbbb",
        "b..b..b",
        ".b.b.b.",
        "..bbb..");

    public static PixelSprite Loom { get; } = PixelSprite.Parse(
        "b........b",
        "bbbbbbbbbb",
        "b.wWwWwW.b",
        "b.WwWwWw.b",
        "b.wWwWwW.b",
        "bbbbbbbbbb",
        "b........b",
        "b........b");

    public static PixelSprite Cauldron { get; } = PixelSprite.Parse(
        ".kkkkk.",
        "kSSSSSk",
        "kSSSSSk",
        ".kSSSk.",
        "..k.k..",
        ".k...k.");

    public static PixelSprite OreAndCoal { get; } = PixelSprite.Parse(
        "..SS.....kk..",
        ".StSS...kxkk.",
        "SSStSS.kkxkkx");

    public static PixelSprite Logs { get; } = PixelSprite.Parse(
        ".bbbbbbB",
        "bbbbbbbB",
        ".bbbbbbB",
        "bbbbbbbB");

    public static PixelSprite WoolBales { get; } = PixelSprite.Parse(
        ".wwW....",
        "wwwWwwW.",
        "wwwWwwWw",
        "wwwWwwWw");

    public static PixelSprite HerbRack { get; } = PixelSprite.Parse(
        "bbbbbbb",
        "b.V.l.b",
        "b.V.l.b",
        "b.....b",
        "b.....b",
        "b.....b");
}

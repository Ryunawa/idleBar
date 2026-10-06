namespace IdleBar.Pixel;

public static class ScenerySprites
{
    public static PixelSprite Pine { get; } = PixelSprite.Parse(
        "...v...",
        "..vVv..",
        "..vVv..",
        ".vVVVv.",
        "..vVv..",
        ".vVlVv.",
        "vVVVVVv",
        ".vVVVv.",
        "vVVlVVv",
        "...B...",
        "...B...");

    public static PixelSprite LeafyTree { get; } = PixelSprite.Parse(
        "..vVv..",
        ".vVlVv.",
        "vVlVVVv",
        "vVVVlVv",
        ".vVVVv.",
        "..vVv..",
        "...B...",
        "...B...",
        "..BBB..");

    public static PixelSprite Bush { get; } = PixelSprite.Parse(
        ".vVv.",
        "vVlVv",
        "vVVVv");

    public static PixelSprite Rock { get; } = PixelSprite.Parse(
        "..kk..",
        ".ktSk.",
        "ktSSSk",
        "kSSSSk");

    public static PixelSprite Cactus { get; } = PixelSprite.Parse(
        "..c..",
        "..c..",
        "c.c..",
        "c.c.c",
        "cCc.c",
        "..cCc",
        "..c..",
        "..C..");

    public static PixelSprite Reeds { get; } = PixelSprite.Parse(
        "m...m",
        "m.m.m",
        "mmm.m",
        ".mmmm",
        "..mm.",
        "..m..");

    public static PixelSprite Palm { get; } = PixelSprite.Parse(
        ".VV.VV.",
        "VVvVvVV",
        "V..b..V",
        "...b...",
        "...b...",
        "....b..",
        "....b..",
        "...b...",
        "...b...",
        "..BBB..");

    public static PixelSprite Fence { get; } = PixelSprite.Parse(
        "b...b...",
        "bbbbbbbb",
        "b...b...",
        "bbbbbbbb");
}

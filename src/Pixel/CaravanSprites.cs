namespace IdleBar.Pixel;

public static class CaravanSprites
{
    private static readonly string[] Canvas =
    [
        "....kkkkkkkkkk....",
        "...kwwwwwwwwwwk...",
        "..kwwWwwwwwwWwwk..",
        "..kwwWwwwwwwWwwk..",
        "..kWWWWWWWWWWWWk..",
        ".kbbbbbbbbbbbbbbk.",
        ".kBBBBBBBBBBBBBBk.",
    ];

    private static readonly string[] OxBody =
    [
        "............hh..",
        "...kkkkkkkk.kk..",
        "..kooooooooookk.",
        "..kooooooooooook",
        "..kOooooooooOkk.",
        "...kOOOOOOOOk...",
    ];

    public static PixelSprite WagonRolling { get; } = PixelSprite.Parse(
    [
        .. Canvas,
        "...kBk......kBk...",
        "..kBsBk....kBsBk..",
        "...kBk......kBk...",
    ]);

    public static PixelSprite WagonTurning { get; } = PixelSprite.Parse(
    [
        .. Canvas,
        "...ksk......ksk...",
        "..ksBsk....ksBsk..",
        "...ksk......ksk...",
    ]);

    public static PixelSprite OxStepping { get; } = PixelSprite.Parse(
    [
        .. OxBody,
        "...k.k....k.k...",
        "..k...k..k...k..",
    ]);

    public static PixelSprite OxStriding { get; } = PixelSprite.Parse(
    [
        .. OxBody,
        "....k.k....k.k..",
        "....k.k....k.k..",
    ]);

    public static PixelSprite OxResting { get; } = PixelSprite.Parse(
    [
        .. OxBody,
        "...k.k....k.k...",
        "...k.k....k.k...",
    ]);
}

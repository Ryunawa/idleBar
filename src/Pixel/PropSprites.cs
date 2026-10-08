namespace IdleBar.Pixel;

public static class PropSprites
{
    public static PixelSprite Barrel { get; } = PixelSprite.Parse(
        ".OOOOO.",
        "ObbobbO",
        "ZZZZZZZ",
        "ObbobbO",
        "ObbobbO",
        "ObbobbO",
        "ZZZZZZZ",
        "ObbobbO",
        ".OOOOO.");

    public static PixelSprite Plant { get; } = PixelSprite.Parse(
        "..V.c..",
        ".cVcVl.",
        "lVcVcVc",
        ".cVVVc.",
        "..cVc..",
        "...V...",
        ".rrrrr.",
        ".RrrrR.",
        "..RRR..");

    public static PixelSprite Cat { get; } = PixelSprite.Parse(
        "a.a.....",
        "aaa.....",
        "aOaaaaa.",
        "aaaaaaaa",
        ".aaaaaaa");

    public static PixelSprite Lantern => TavernSprites.Lantern;

    public static PixelSprite Garlic { get; } = PixelSprite.Parse(
        ".Z.",
        ".y.",
        "wyw",
        ".w.",
        "wyw",
        ".w.",
        "wyw");

    public static PixelSprite Herbs { get; } = PixelSprite.Parse(
        ".Z.",
        ".Y.",
        ".Y.",
        "cVc",
        "VcV",
        "cVc",
        ".c.");

    public static PixelSprite Pot { get; } = PixelSprite.Parse(
        "..Z..",
        "..Z..",
        "aaaaa",
        "aIaIa",
        ".aaa.");

    public static PixelSprite Candle { get; } = PixelSprite.Parse(
        ".w.",
        ".w.",
        ".w.",
        "BBB");

    public static PixelSprite Bowl { get; } = PixelSprite.Parse(
        "nnnnn",
        ".NNN.");

    public static PixelSprite For(DecorKind kind) => kind switch
    {
        DecorKind.Plant => Plant,
        DecorKind.Lantern => Lantern,
        DecorKind.Garlic => Garlic,
        DecorKind.Herbs => Herbs,
        DecorKind.Pot => Pot,
        DecorKind.Candle => Candle,
        DecorKind.Bowl => Bowl,
        _ => Barrel,
    };
}

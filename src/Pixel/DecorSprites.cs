using System.Collections.Generic;
using Godot;

namespace IdleBar.Pixel;

public static class DecorSprites
{
    private static readonly string[] BannerRows =
    [
        "ZZZZZZZ",
        ".RRRRR.",
        ".RRgRR.",
        ".RgggR.",
        ".RRgRR.",
        ".RRRRR.",
        ".RgRgR.",
        ".RRRRR.",
        ".RR.RR.",
        ".R...R.",
    ];

    public static PixelSprite Painting { get; } = PixelSprite.Parse(
        "YggggggggggY",
        "gqqqqqqqqLLg",
        "gqqqqqqqqLLg",
        "gqqqqcqqqqqg",
        "gqqqcVcqqqqg",
        "gqqcVVVcqcVg",
        "gccVVVVVcVVg",
        "gVVVVVVVVVVg",
        "YggggggggggY");

    public static PixelSprite Portrait { get; } = PixelSprite.Parse(
        "YggggggY",
        "gxxxxxxg",
        "gxxBBxxg",
        "gxBppBxg",
        "gxBppBxg",
        "gxxppxxg",
        "gxRRRRxg",
        "gRRRRRRg",
        "gRRgRRRg",
        "YggggggY");

    public static PixelSprite Trophy { get; } = PixelSprite.Parse(
        "y.y.....y.y",
        ".yy.....yy.",
        "..y.y.y.y..",
        "...yOOOy...",
        "...OkOkO...",
        "....OOO....",
        "....OOO....",
        "....OkO....",
        "..bbbbbbb..",
        "...bbbbb...");

    public static PixelSprite Shield { get; } = PixelSprite.Parse(
        "B.........B",
        ".s.......s.",
        "..srrrrrs..",
        "..rRrgrRr..",
        "..rrgggrr..",
        "..rRrgrRr..",
        "..srrrrrs..",
        ".s..rrr..s.",
        "s.........s");

    public static PixelSprite Board { get; } = PixelSprite.Parse(
        "BBBBBBBBBBBB",
        "BoorwwoooooB",
        "BoowwwoyryoB",
        "BoowwwoyyyoB",
        "BooooooyyyoB",
        "BowrwooooooB",
        "BowwwoowwroB",
        "BoooooowwwoB",
        "BBBBBBBBBBBB");

    public static PixelSprite Banner { get; } = PixelSprite.Parse(BannerRows);

    public static PixelSprite BlueBanner { get; } = PixelSprite.Parse(new Dictionary<char, Color> { ['R'] = new("2f4b7a") }, BannerRows);

    public static PixelSprite Clock { get; } = PixelSprite.Parse(
        "..OOO..",
        ".OwwwO.",
        "OwwkwwO",
        "OwwkkwO",
        "OwwwwwO",
        ".OwwwO.",
        "..OOO..",
        ".OXXXO.",
        ".OXXXO.",
        ".OXXXO.",
        ".OOOOO.");

    public static PixelSprite Dartboard { get; } = PixelSprite.Parse(
        "..BBB..",
        ".BrwrB.",
        "BrwrwrB",
        "BwrRrwB",
        "BrwrwrB",
        ".BrwrB.",
        "..BBB..");

    public static PixelSprite For(DecorKind kind) => kind switch
    {
        DecorKind.Painting => Painting,
        DecorKind.Portrait => Portrait,
        DecorKind.Trophy => Trophy,
        DecorKind.Shield => Shield,
        DecorKind.Board => Board,
        DecorKind.Banner => Banner,
        DecorKind.BlueBanner => BlueBanner,
        DecorKind.Clock => Clock,
        DecorKind.Dartboard => Dartboard,
        _ => PropSprites.For(kind),
    };
}

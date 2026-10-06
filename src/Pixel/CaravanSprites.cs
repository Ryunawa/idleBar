using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Pixel;

public static class CaravanSprites
{
    private static readonly Dictionary<WagonStyle, PixelSprite> Wagons = [];

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

    private static readonly string[] RollingWheels =
    [
        "...kBk......kBk...",
        "..kBsBk....kBsBk..",
        "...kBk......kBk...",
    ];

    private static readonly string[] TurningWheels =
    [
        "...ksk......ksk...",
        "..ksBsk....ksBsk..",
        "...ksk......ksk...",
    ];

    public static PixelSprite Wagon(bool rolling, CaravanLook look)
    {
        WagonStyle style = new(rolling, look.Tarp, look.IronWheels);
        if (!Wagons.TryGetValue(style, out PixelSprite? sprite))
        {
            IEnumerable<string> canvas = Canvas.Select(row => look.Tarp ? row.Replace('w', 'm').Replace('W', 'C') : row);
            IEnumerable<string> wheels = (rolling ? RollingWheels : TurningWheels).Select(row => look.IronWheels ? row.Replace('B', 'S') : row);
            sprite = PixelSprite.Parse([.. canvas, .. wheels]);
            Wagons[style] = sprite;
        }

        return sprite;
    }

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

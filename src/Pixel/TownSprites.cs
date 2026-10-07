using System.Linq;

namespace IdleBar.Pixel;

public static class TownSprites
{
    public const int ChimneyColumn = 7;
    public const int LampHeadColumn = 1;

    private const char Glow = 'L';
    private const char DarkGlass = 'x';
    private const char ColdLamp = 's';

    private static readonly string[] HouseRows =
    [
        ".......x..",
        "...rrrrx..",
        "..rRRRRr..",
        ".rRRRRRRr.",
        "rrrrrrrrrr",
        ".yyyyyyyy.",
        ".yLLyyLLy.",
        ".yLLyyLLy.",
        ".yyyyyyyy.",
        ".yyyxxyyy.",
        ".yyyxxyyy.",
    ];

    private static readonly string[] TallHouseRows =
    [
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
        ".YyxxyY.",
    ];

    private static readonly string[] LampRows =
    [
        ".L.",
        "kLk",
        ".k.",
        ".k.",
        ".k.",
        ".k.",
        ".k.",
        "kkk",
    ];

    private static readonly PixelSprite LitHouse = PixelSprite.Parse(HouseRows);
    private static readonly PixelSprite DarkHouse = PixelSprite.Parse(Replace(HouseRows, DarkGlass));
    private static readonly PixelSprite LitTallHouse = PixelSprite.Parse(TallHouseRows);
    private static readonly PixelSprite DarkTallHouse = PixelSprite.Parse(Replace(TallHouseRows, DarkGlass));
    private static readonly PixelSprite LitLamp = PixelSprite.Parse(LampRows);
    private static readonly PixelSprite DarkLamp = PixelSprite.Parse(Replace(LampRows, ColdLamp));

    private static readonly PixelSprite Stall = PixelSprite.Parse(
        "rwrwrwrwrw",
        "rwrwrwrwrw",
        ".b......b.",
        ".b.gaV..b.",
        ".bbbbbbbb.",
        ".b......b.",
        ".b......b.");

    public static PixelSprite For(TownPiece piece, bool lit) => piece switch
    {
        TownPiece.House => lit ? LitHouse : DarkHouse,
        TownPiece.TallHouse => lit ? LitTallHouse : DarkTallHouse,
        TownPiece.Lamp => lit ? LitLamp : DarkLamp,
        _ => Stall,
    };

    private static string[] Replace(string[] rows, char unlit) => [.. rows.Select(row => row.Replace(Glow, unlit))];
}
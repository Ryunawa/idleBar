using System.Collections.Generic;
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

    private static readonly (char Light, char Dark)[] Roofs = [('r', 'R'), ('s', 'S'), ('o', 'B')];
    private static readonly Dictionary<(TownPiece Piece, bool Lit, int Roof), PixelSprite> Houses = [];

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

    public static PixelSprite For(TownPiece piece, bool lit, int roof = 0) => piece switch
    {
        TownPiece.House or TownPiece.TallHouse => House(piece, lit, roof % Roofs.Length),
        TownPiece.Lamp => lit ? LitLamp : DarkLamp,
        TownPiece.Tree => ScenerySprites.LeafyTree,
        _ => Stall,
    };

    private static PixelSprite House(TownPiece piece, bool lit, int roof)
    {
        if (!Houses.TryGetValue((piece, lit, roof), out PixelSprite? sprite))
        {
            string[] rows = piece == TownPiece.House ? HouseRows : TallHouseRows;
            (char light, char dark) = Roofs[roof];
            IEnumerable<string> painted = rows.Select(row => row.Replace('r', light).Replace('R', dark));
            sprite = PixelSprite.Parse([.. lit ? painted : Replace([.. painted], DarkGlass)]);
            Houses[(piece, lit, roof)] = sprite;
        }

        return sprite;
    }

    private static string[] Replace(string[] rows, char unlit) => [.. rows.Select(row => row.Replace(Glow, unlit))];
}
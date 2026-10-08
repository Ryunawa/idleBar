using System.Collections.Generic;

namespace IdleBar.Pixel;

public static class SouvenirSprites
{
    public const int Size = 5;

    private static readonly Dictionary<string, PixelSprite> Sprites = new()
    {
        ["gaspard"] = PixelSprite.Parse("....s", "...s.", "..s..", "gB...", "B.g.."),
        ["melisande"] = PixelSprite.Parse(".L.r.", "LcVcr", ".cVc.", "..V..", ".YYY."),
        ["brindille"] = PixelSprite.Parse(".l.l.", "lklkl", "lllll", "VlllV", ".V.V."),
        ["odette"] = PixelSprite.Parse(".....", "wwwww", "wWrWw", "wwWww", "wwwww"),
        ["fantome"] = PixelSprite.Parse("..L..", "..w..", "..w..", ".www.", "nnnnn"),
        ["bartholome"] = PixelSprite.Parse("....B", "...b.", ".ob..", "oBoo.", ".oo.."),
        ["ysolde"] = PixelSprite.Parse("..s..", ".sss.", "..s..", "s.s.s", ".sss."),
        ["pip"] = PixelSprite.Parse(".rrr.", "rwrwr", ".rrr.", "..w..", ".www."),
        ["anselme"] = PixelSprite.Parse("..Vc.", ".rr..", "rrrr.", "rrrR.", ".RR.."),
        ["lune"] = PixelSprite.Parse("..g..", ".ggg.", "ggggg", ".g.g.", "g...g"),
        ["fennec"] = PixelSprite.Parse("....s", "...s.", "..s..", ".BB..", "BB..."),
        ["hugues"] = PixelSprite.Parse("yyyyy", "yYyry", "yyryy", "yrYyy", "yyyyy"),
    };

    public static PixelSprite? For(string regular) => Sprites.GetValueOrDefault(regular);
}

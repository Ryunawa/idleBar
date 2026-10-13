using System.Collections.Generic;

namespace IdleBar.Scene;

public static class FighterGrids
{
    public static IReadOnlyList<string> Archer { get; } =
    [
        "..GGG...", ".GGGGG..", ".GGsssb.", ".GGsks.b", "..Gsss.b", ".gGGGgsb",
        ".ggggg.b", ".ggGgg.b", "..bbb.b.", "..g.g...", "..g.g...", ".ll.ll..",
    ];

    public static IReadOnlyList<string> Mage { get; } =
    [
        "...p....", "...pp...", "..ppPY..", ".ppPPPp.", "pppppppp", "..sss..c", "..ssk..b",
        ".ppppp.b", "pppppPsb", ".ppppp.b", ".ppPpp.b", ".ppppp.b", "..l.l..b",
    ];

    public static IReadOnlyList<string> ChickenUp { get; } =
    [
        ".r.......", "rr...yy..", "oYY.yYYy.", "YkYYYYy..", ".YYYYYYYy", "..YYYYYYy", "...yYYYy.", "....o.o..",
    ];

    public static IReadOnlyList<string> ChickenDown { get; } =
    [
        ".r.......", "rr.......", "oYY......", "YkYYYYYy.", ".YYyyyYYy", "..YyYYyYy", "...yyYYy.", "....o.o..",
    ];

    public static IReadOnlyList<string> Lock { get; } = [".aaa.", "a...a", "yyyyy", "yykyy", "yykyy", "yyyyy"];

    public static IReadOnlyList<string> Bread { get; } = [".TTT.", "TETET", ".TTT."];

    public static IReadOnlyList<string> Bolt { get; } =
    [
        "......Yy...", ".....YYy...", "....YYy....", "...YYy.....", "..YYYYYYy..", "......YYy..",
        ".....YYy...", "....YYy....", "...YYy.....", "...Yy......", "..Yy.......",
    ];

    public static IReadOnlyList<string> Flame { get; } =
    [
        ".....r.....", "....rr.....", "....rrr..r.", "...rrorr.r.", "..rrooorrr.", "..roooooor.",
        ".rrooYYoorr", ".rooYYYYoor", ".roYYwwYYor", "..roYwwYor.", "...rrrrrr..",
    ];

    public static IReadOnlyList<string> Coins { get; } =
    [
        "..y.....y..", "...........", "....yyy....", "...yYYYy...", "...yYwYy.y.", "y..yYYYy...",
        "....yyy....", ".yyy...yyy.", "yYYYy.yYYYy", "yYwYy.yYwYy", ".yyy...yyy.",
    ];
}

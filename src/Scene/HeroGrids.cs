using System.Collections.Generic;

namespace IdleBar.Scene;

public static class HeroGrids
{
    public const int HandX = 8;
    public const int HandY = 11;

    public static IReadOnlyList<IReadOnlyList<string>> Bodies { get; } =
    [
        [
            "", "", "",
            "...kkkk....", "..kSSSSk...", "..kSSSkSk..", "..kSSSSSk..", "...kSSSk...",
            "..kaOOak...", ".kaOOOOak..", ".kSOaaOkS..", ".kSOaaOkSS.", "..kyyyyk...",
            "..kQQQQk...", "..kQkkQk...", "..kQk.kQk..", ".kkk..kkk..",
        ],
        [
            "", "", "",
            "...kkkk....", "..kSSSSk...", "..kSSSkSk..", "..kSSSSSk..", "...kSSSk...",
            ".kaaaak.kk.", "kaAOOAakOOk", "kaAOOAakOyOk", "kSAaaAakyyyk", ".kyyyykOyOk",
            ".kQQQQkOOOk", ".kQkkQ.kOk.", ".kQk.kQ.k..", "kkk..kkk...",
        ],
        [
            "", "", "",
            "...kkkk....", "..kSSSSk..b", "..kSSSkSkb.", "..kSSSSSk.b", "...kSSSk..b",
            "..kOOOOk..b", ".kOOOOOOk.b", ".kSObOOSSSb", ".kQObOOk..b", "..kbbbbk.b.",
            "..kQQQQkb..", "..kQkkQk...", "..kQk.kQk..", ".kkk..kkk..",
        ],
        [
            "", "", "",
            "...kkkk....", "..kSSSSk.c.", "..kSSSkSkcc", "..kSSSSSkb.", "...kSSSk.b.",
            "..kOOOOk.b.", ".kOOyOOOkb.", ".kSOOOOOSb.", ".kOOOOOOkb.", ".kQOOOOQkb.",
            ".kQOOOOQkb.", "..kQOOQk.b.", "..kQQQQk.b.", ".kkkkkkk.b.",
        ],
        [
            "", "", "",
            "...kkkk....", "..kSSSSk.Y.", "..kSSSkSkYyY", "..kSSSSSk.y.", "...kSSSk..y.",
            "..kwOOwk..y.", ".kwwOOwwk.y.", ".kSwyywwSSy.", ".kwwyywwk.y.", ".kwwwwwwk.y.",
            ".kQwwwwQk.y.", "..kQwwQk..y.", "..kwwwwk..y.", ".kkkkkkk..y.",
        ],
    ];

    public static IReadOnlyList<IReadOnlyList<string>> Hairs { get; } =
    [
        ["", "", "", "...HHHH....", "..HHHHHH...", "..HH......."],
        ["", "", "", "...HHHH....", "..HHHHHH...", ".HHH.......", ".HH........", ".HH........", ".HH........"],
        ["", "", "", "...HHHH....", "..HHHHHH...", "HHHH.......", "HH.........", "H.........."],
    ];

    public static IReadOnlyList<string> MageHat { get; } =
    [
        ".....kk....", "....kOOk...", "....kOOOk..", "...kOOyOOk.", "..kkkkkkkkk",
    ];
}

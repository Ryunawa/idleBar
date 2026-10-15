using System.Collections.Generic;

namespace IdleBar.Scene;

public static class HeroGrids
{
    public const int HandX = 9;
    public const int HandY = 11;
    public const int Height = 16;

    public static IReadOnlyList<IReadOnlyList<string>> Bodies { get; } =
    [
        [
            "", "", "",
            "...0000.....", "..0SSSS0....", "..0SS0S0....", "..0SSSS0....", "..00SS00....",
            ".0XXOOXX0...", "0SXOOOOXS0..", "0G0OOOO0GG0.", ".0044440000.", "..0LLLL0....",
            "..0L00L0....", "..0F00F0....", ".000..000...",
        ],
        [
            "", "", "",
            "...0000.....", "..0SSSS0....", "..0SS0S0....", "..0SSSS0....", "..00SS00000.",
            ".0XXXXX0WWV0", "0SXYXXY0WaV0", "0GXXYXX0WWV0", ".0444440WWV0", "..0LLLL0WWV0",
            "..0L00L00WV0", "..0F00F0.00.", ".000..000...",
        ],
        [
            "", "", "",
            "...0000.....", "..0SSSS0.0..", "..0SS0S0.W0.", "..0SSSS0..W0", "..00SS00..W0",
            ".0OOOOO00.W0", "0SOQQQOSSGW0", "0G0OOOO0..W0", ".0044440..W0", "..0LLLL0.W0.",
            "..0L00L0.0..", "..0F00F0....", ".000..000...",
        ],
        [
            "", "", "",
            "...0000..00.", "..0SSSS00WV0", "..0SS0S00WW0", "..0SSSS0.00.", "..00SS00.40.",
            ".0OOOOOO040.", "0OOQOOOGG40.", "0OOOOOOO040.", "0OQOOOQO040.", "0OOQOOOO040.",
            "0OOOOQOO040.", ".0QQQQQ0040.", ".00000000...",
        ],
        [
            "", "", "",
            "...0000..0..", "..0SSSS00W0.", "..0SS0S0WaW0", "..0SSSS00W0.", "..00SS00.40.",
            ".0777O770.40", "0S77O7O7S040", "0G0777770G40", ".07779777040", ".0777777O040",
            ".0O77777O040", "..0777770040", ".0000000.0..",
        ],
    ];

    public static IReadOnlyList<IReadOnlyList<string>> Hairs { get; } =
    [
        ["", "", "", "...HHHH.....", "..HHHHHH....", "..HH........"],
        ["", "", "", "...HHHH.....", "..HHHHHH....", ".HHH........", ".HH.........", ".HH.........", ".H.........."],
        ["", "", "", "...HHHH.....", "..HHHHHH....", "HHHH........", "HH..........", "H..........."],
    ];

    public static IReadOnlyList<string> MageHat { get; } =
    [
        "....00......", "...0OO0.....", "...0OQO0....", "..0OOaOO0...", ".000000000..",
    ];
}

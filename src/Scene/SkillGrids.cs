using System.Collections.Generic;

namespace IdleBar.Scene;

public static class SkillGrids
{
    public static IReadOnlyList<string> Bolt { get; } =
    [
        "......a9...", ".....aa9...", "....aa9....", "...aa9.....", "..aaaaaa9..", "......aa9..",
        ".....aa9...", "....aa9....", "...aa9.....", "...a9......", "..a9.......",
    ];

    public static IReadOnlyList<string> Storm { get; } =
    [
        "....666....", ".666777666.", "67777777776", "d777777777d", ".ddddddddd.", "....a9..c..",
        "...aa9....c", "..aaaaa9...", "....aa9.c..", "...aa9....c", "...a9...c..",
    ];

    public static IReadOnlyList<string> Judgment { get; } =
    [
        "....a7a....", ".....9.....", ".a9999999a.", "...a777a...", "...a777a...", "....a7a....",
        "....a7a....", "....a7a....", ".....7.....", ".a...a...a.", "..a.aaa.a..",
    ];

    public static IReadOnlyList<string> Flame { get; } =
    [
        ".....8.....", "....88.....", "....888..8.", "...88988.8.", "..88999888.", "..89999998.",
        ".8899aa9988", ".899aaaa998", ".89aa77aa98", "..89a77a98.", "...888888..",
    ];

    public static IReadOnlyList<string> Shield { get; } =
    [
        ".999999999.", "9a7777777a9", "9a777a777a9", "9a77aaa77a9", "9a777a777a9", "9a777a777a9",
        ".9a77a77a9.", ".9a77777a9.", "..9a777a9..", "...9aaa9...", "....999....",
    ];

    public static IReadOnlyList<string> Banner { get; } =
    [
        ".a.........", ".4cccccc1..", ".4cccccc1.7", ".4caccca1..", ".4ccacac1.7", ".4cccacc1..",
        ".4cccccc1.7", ".4ccc.cc1..", ".4cc...c1..", ".4.........", "444........",
    ];

    public static IReadOnlyList<string> Coins { get; } =
    [
        "..9.....9..", "...........", "....999....", "...9aaa9...", "...9a7a9.9.", "9..9aaa9...",
        "....999....", ".999...999.", "9aaa9.9aaa9", "9a7a9.9a7a9", ".999...999.",
    ];

    public static IReadOnlyList<string> Sheaf { get; } =
    [
        "...a.a.a...", "..a9a9a9a..", "..9a9a9a9..", "...a9a9a...", "....999....", "....999....",
        "...44444...", "....999....", "...9.9.9...", "..9..9..9..", ".9...9...9.",
    ];

    public static IReadOnlyList<string> Lure { get; } =
    [
        "...8.8.....", "...888.....", "..aaaaa....", ".aa0aaa..7.", ".aaaaa99..7", ".aaaaa9..7.",
        "..aaaa8....", "..aaa......", ".9aaaa.....", "9aaaaaa....", ".999999....",
    ];
}

using System.Collections.Generic;

namespace IdleBar.Scene;

public static class ItemGrids
{
    public static IReadOnlyList<IReadOnlyList<string>> Armors { get; } =
    [
        [".00....00.", "0JJ0..0JJ0", "0JJJ00JJJ0", ".0JJJJJJ0.", ".0JZJJZJ0.", ".0JJZZJJ0.", ".0JJJJJJ0.", ".0ZJJJJZ0.", "..0JJJJ0..", "...0000..."],
        [".00....00.", "0ZJ0..0JZ0", "0JZJ00JZJ0", ".0ZJZJZJ0.", ".0JZJZJZ0.", ".0ZJZJZJ0.", ".0JZJZJZ0.", ".0ZJZJZJ0.", "..0JZJZ0..", "...0000..."],
        [".00....00.", "0JJ0..0JJ0", "0JJJ00JJJ0", ".0JaJJaJ0.", ".0JJJJJJ0.", ".0JaJJaJ0.", ".0JJJJJJ0.", ".0JaJJaJ0.", "..0JJJJ0..", "...0000..."],
        ["000....000", "0JJ0..0JJ0", "0JZJ00JZJ0", ".0JJJJJJ0.", ".0ZJaaJZ0.", ".0JJaaJJ0.", ".0ZJJJJZ0.", ".0JJJJJJ0.", "..0ZJJZ0..", "...0000..."],
        ["..00..00..", ".0JJ00JJ0.", "0JJJJJJJJ0", "0JZZZZZZJ0", "0JJJJJJJJ0", "00ZZZZZZ00", ".0JJJJJJ0.", ".0ZZZZZZ0.", ".0JJJJJJ0.", ".00000000."],
        ["..00..00..", ".0J0000J0.", ".0JJ00JJ0.", ".0JJJJJJ0.", ".0aaaaaa0.", ".0JJJJJJ0.", ".0JJJJJJ0.", "0JJZJJZJJ0", "0JZJJJJZJ0", "0000000000"],
        ["...0000...", "..0ZJJZ0..", "..0JaaJ0..", ".0JJ00JJ0.", ".0JJ00JJ0.", ".0JZ00ZJ0.", "0JJZ00ZJJ0", "0JJZ00ZJJ0", "0JZZ00ZZJ0", "0000..0000"],
        [".0......0.", "0J00..00J0", "0JJJ00JJJ0", "00JJJJJJ00", ".0ZZZZZZ0.", ".0JJJJJJ0.", ".0ZZZZZZ0.", ".0JJJJJJ0.", ".0aaaaaa0.", ".00000000."],
    ];

    public static IReadOnlyList<IReadOnlyList<string>> Helms { get; } =
    [
        ["...0000...", "..0JJJJ0..", ".0JJJJJJ0.", ".0JJJJJJ0.", ".0J0000J0.", ".0J0..0J0.", ".0J0..0J0.", ".0JJ00JJ0.", "..000000..", ".........."],
        ["..........", "..........", ".0.0.0.0..", ".0J0J0J0..", ".0JJJJJ0..", ".0J8JaJ0..", ".0JJJJJ0..", ".0000000..", "..........", ".........."],
        ["....00....", "...0JJ0...", "..0JJJJ0..", ".0JJ00JJ0.", ".0J0..0J0.", "0JJ0..0JJ0", "0JJ0..0JJ0", "0JJJ00JJJ0", ".00000000.", ".........."],
        ["..........", "...0000...", "..0JJJJ0..", "..0JJJJ0..", "..0JJJJ0..", "..0ZZZZ0..", "00JJJJJJ00", "0JJJJJJJJ0", ".00000000.", ".........."],
        ["..000000..", ".0JJJJJJ0.", ".0JJaaJJ0.", ".00000000.", ".0JJaaJJ0.", ".0J0aa0J0.", ".0JJaaJJ0.", ".0J0aa0J0.", ".00000000.", ".........."],
        ["..........", "0........0", "70.0000.07", "770JJJJ077", ".0JJJJJJ0.", ".0JZZZZJ0.", ".0JJJJJJ0.", ".0J0000J0.", ".00....00.", ".........."],
        ["......00..", ".....0J0..", "....0JJ0..", "...0JJJ0..", "...0JaJ0..", "..0JJJJ0..", "000ZZZZ000", "0JJJJJJJJ0", ".00000000.", ".........."],
        ["..........", "....00....", "...0JZ0...", "..0JJZJ0..", ".0JJJZJJ0.", "00JJJZJJ00", "J0ZZZZZZ0J", "0JJJJJJJJ0", ".00000000.", ".........."],
    ];

    public static IReadOnlyList<IReadOnlyList<string>> Worn { get; } =
    [
        ["", "", "", "..00000....", ".0JJJJJ0...", ".0J00JJ0...", ".0J0.0J0..."],
        ["", ".0.0.0.....", ".0J0J0J0...", ".0JJJJJ0..."],
        ["", "", "..0000.....", ".0JJJJ0....", "0JJJJJJ0...", "0JZ...J0...", "0J.....0...", "0J.........", ".0........."],
        ["", "", "...0000....", "..0JJJJ0...", "..0ZZZZ0...", "00JJJJJJ00."],
        ["", "", "..00000....", ".0JJJJJ0...", ".0JJaJJ0...", ".0J000J0...", ".0JJaJJ0...", ".0000000..."],
        ["0.......0..", "70.....07..", "700000007..", ".0JJJJJ0...", ".0ZZZZZ0..."],
        [".....00....", "....0J0....", "...0JJ0....", "..0JJaJ0...", "0JJJJJJJJ0.", ".00000000.."],
        ["....00.....", "...0JZ0....", "0.0JJZJ0.0.", "J00JJZJ00J.", "0JJJJJJJJ0."],
    ];
}

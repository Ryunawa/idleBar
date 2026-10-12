using System.Collections.Generic;

namespace IdleBar.Scene;

public static class MonsterGrids
{
    public static IReadOnlyList<string> Goblin { get; } =
    [
        "g........g", "gg.gggg.gg", ".gggggggg.", ".gkYgkYgg.", ".gggggggg.", "..gwkwkg..",
        "...gggg...", "..bbbbbb..", ".gbbBbbbg.", ".g.bbbb.g.", "...g..g...", "..ll..ll..",
    ];

    public static IReadOnlyList<string> Wolf { get; } =
    [
        "..k..k.........", "..lAkl.........", ".kllAll........", "klAdANAkk......", ".lldAdAAdlll...", "..lllAAddddAk..",
        "...lAAAAddddAk.", "...lAAAANlAdkN.", "....kkAl.lkdall", "....kAll..lldlk", "....kdbk..kaNl.", ".....lla.llANb.",
    ];

    public static IReadOnlyList<string> Mushroom { get; } =
    [
        "...rrrr...", ".rrwrrrrr.", "rrrrrrwrrr", "rwrrrrrrRr", "..tttttt..", "..tkttkt..",
        "..tttttt..", "..ttRRtt..", "...tttt...", "...t..t...", "..tt..tt..",
    ];

    public static IReadOnlyList<string> Scorpion { get; } =
    [
        ".......ee....", "......eoee...", ".....ee..Ee..", ".....ee...ye.", ".....B....ee.", ".........eye.", "......e.eyb..",
        "....eeeeyyb..", ".yo.Ryyyyybt.", "eoeeyyyyeeTe.", ".e.ebyebyeewy", ".....eyye..e.", "...eyyee.e.y.", "....ee...e...",
    ];

    public static IReadOnlyList<string> Mummy { get; } =
    [
        ".....bddd..", "....bwttwd.", "....bbbbdb.", "....bsbwbb.", "....bww..b.", ".bdbbAb.bw.", "bwtlwwbbA..",
        ".BT.bbwdwb.", "...mdwbb.l.", "...t.d.twl.", ".....sbbwtw", ".....b.wbb.", "....ab..dwb", "...aab...mb",
    ];

    public static IReadOnlyList<string> Yeti { get; } =
    [
        "...wwwww....", "..wwwwwwwa..", "..AkAAkAwa..", "..AAAAAAwa..", "..AwkkwAwa..",
        "..wAAAAwwa..", ".wwwwwwwwwa.", "wwwwwwwwwwwa", "ww.wwwwwww.a", "ww.wwwwwwwa.",
        "...wwwwwww..", "...ww..ww...", "..aaa..aaa..",
    ];

    public static IReadOnlyList<string> IceGolem { get; } =
    [
        "...uuuuu....", "..ucccccu...", "..cYccYcu...", "..ccccccu...", ".uuuuuuuuu..",
        "ucccwcccccu.", "ucccccccccu.", "cc.ccccccc.c", "cc.ccwcccc.c", "uu.cccccccuu",
        "...uuuuuuu..", "...cc...cc..", "...cc...cc..", "..uuu..uuu..",
    ];

    public static IReadOnlyList<string> Ogre { get; } =
    [
        ".....y.y.y......", ".....yyyyy......", "....mmmmmmm.....", "...mmmmmmmmm....",
        "...mkYmmkYmm....", "...mmmmmmmmm....", "...mwmmmmwmm....", "....mmmmmmm.....",
        "..mmmmmmmmmmm...", ".mmmmmmmmmmmmm..", ".mmmmmmmmmmmmmm.", "mm.mmmmmmmmmm.mm",
        "mm.mmmmmmmmmm.mm", "mm.bbbbbbbbbb.mm", "...bbbBbbBbbb...", "....mmm..mmm....",
        "....mmm..mmm....", "....mmm..mmm....", "...llll.llll....",
    ];

    public static IReadOnlyList<string> Dragon { get; } =
    [
        "........y.y.y.........", "........yyyyy.........", ".......rrrrrr.....R...", "......rkYrrrrr...RR...",
        "rrrrrrrrrrrrrr..RRR...", "wrwrrrrrrrrrrrRRRRR...", "..rrrrrrRRRRRRRRRR....", "....rrrrrrrrrRRRRR....",
        "...rrrrrrrrrrrrRR.....", "..rrryyyyyyrrrrrr.....", "..rryyyyyyyyrrrrrrr...", "...ryyyyyyyrrrrrrrrr..",
        "....rrrrrrrrr....rrrr.", "....rr..rr........rrr.", "....rR..rR..........r.", "...RRR.RRR............",
    ];

    public static IReadOnlyList<IReadOnlyList<string>?> Templates { get; } = [null, Goblin, Wolf, Mushroom, Scorpion, Mummy, Yeti, IceGolem];

    public static IReadOnlyList<IReadOnlyList<string>> Bosses { get; } = [Ogre, Dragon];
}

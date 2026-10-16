using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Frontier;

public static class Researches
{
    public const int PerBranch = 4;
    public const double CostGrowth = 1.6;
    public const double TimeGrowth = 1.5;
    public const int MembersStep = 5;
    public const int SealsPerHour = 6;

    public static IReadOnlyList<int> TitanSeals { get; } = [10, 25, 50];

    public static IReadOnlyList<ResearchInfo> All { get; } =
    [
        new(ResearchKind.CommonForge, "common_forge", "Forge commune", 5, 15, 2, 4),
        new(ResearchKind.Ramparts, "ramparts", "Remparts", 5, 22, 5, 5),
        new(ResearchKind.Veterans, "veterans", "Renforts aguerris", 3, 30, 10, 10),
        new(ResearchKind.FinalAssault, "final_assault", "Assaut final", 3, 40, 20, 6),
        new(ResearchKind.Caravans, "caravans", "Caravanes", 5, 15, 2, 4),
        new(ResearchKind.Schools, "schools", "Écoles", 5, 22, 5, 5),
        new(ResearchKind.HeavyConvoys, "heavy_convoys", "Convois lourds", 3, 30, 10, 15),
        new(ResearchKind.GuildMarket, "guild_market", "Marché de guilde", 3, 40, 20, 5),
        new(ResearchKind.WarCry, "war_cry", "Cri de guerre", 5, 15, 2, 10),
        new(ResearchKind.Sanctuary, "sanctuary", "Sanctuaire", 3, 22, 5, 4),
        new(ResearchKind.Messengers, "messengers", "Messagers", 3, 30, 10, 30),
        new(ResearchKind.Hospitality, "hospitality", "Hospitalité", 1, 40, 20, 1),
    ];

    public static IReadOnlyList<ResearchBranch> Branches { get; } = [ResearchBranch.War, ResearchBranch.Prosperity, ResearchBranch.Rally];

    public static ResearchInfo Of(ResearchKind kind) => All[(int)kind];

    public static IEnumerable<ResearchInfo> In(ResearchBranch branch) => All.Where(research => research.Branch == branch);

    public static ResearchKind? Parse(string? id) => All.FirstOrDefault(research => research.Id == id)?.Kind;

    public static ResearchBranch BranchOf(ResearchKind kind) => (ResearchBranch)((int)kind / PerBranch);

    public static int Position(ResearchKind kind) => (int)kind % PerBranch + 1;

    public static ResearchKind? Prerequisite(ResearchKind kind) => Position(kind) > 1 ? kind - 1 : null;

    public static string Name(ResearchBranch branch) => branch switch
    {
        ResearchBranch.War => "Guerre",
        ResearchBranch.Prosperity => "Prospérité",
        _ => "Ralliement",
    };

    public static int Cost(ResearchKind kind, int level, int members) =>
        (int)Math.Ceiling(Math.Round(Of(kind).Cost * Math.Pow(CostGrowth, level - 1) * Math.Max(1, members / (double)MembersStep), 6));

    public static double Hours(ResearchKind kind, int level) => Of(kind).Hours * Math.Pow(TimeGrowth, level - 1);
}

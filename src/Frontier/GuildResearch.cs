using System;

namespace IdleBar.Frontier;

public static class GuildResearch
{
    public const int BaseAllyMinutes = 30;
    public const int BaseConvoyGold = 50;
    public const int BaseCooldownMinutes = 240;
    public const int BaseFrontMax = 3;

    public static int Level(ResearchView? view, ResearchKind kind) =>
        view is null ? 0 : Math.Clamp(view.Level(kind), 0, Researches.Of(kind).MaxLevel);

    public static double Effect(ResearchView? view, ResearchKind kind) => Level(view, kind) * Researches.Of(kind).Effect;

    public static double DamageFactor(FrontierState state) => 1 + Share(state, ResearchKind.CommonForge);

    public static double HealthFactor(FrontierState state) => 1 + Share(state, ResearchKind.Ramparts);

    public static double BossFactor(FrontierState state) => 1 + Share(state, ResearchKind.FinalAssault);

    public static double GoldFactor(FrontierState state) => 1 + Share(state, ResearchKind.Caravans);

    public static double XpFactor(FrontierState state) => 1 + Share(state, ResearchKind.Schools);

    public static double Discount(FrontierState state) => 1 - Share(state, ResearchKind.GuildMarket);

    public static double RechargePace(FrontierState state) => 1 + Share(state, ResearchKind.Sanctuary);

    public static int AllyMinutes(ResearchView? view) => BaseAllyMinutes + (int)Effect(view, ResearchKind.Veterans);

    public static int ConvoyGold(ResearchView? view) => BaseConvoyGold + (int)Effect(view, ResearchKind.HeavyConvoys);

    public static int CooldownMinutes(ResearchView? view) => Math.Max(0, BaseCooldownMinutes - (int)Effect(view, ResearchKind.Messengers));

    public static int FrontMax(ResearchView? view) => BaseFrontMax + (int)Effect(view, ResearchKind.Hospitality);

    public static void Sync(FrontierState state, ResearchView? view, EventLog events)
    {
        if (state.Research is ResearchView known && view is not null)
        {
            foreach (ResearchInfo research in Researches.All)
            {
                for (int level = Level(known, research.Kind) + 1; level <= Level(view, research.Kind); level++)
                {
                    events.Add(new ResearchDone(research.Kind, level));
                }
            }
        }

        state.Research = view;
    }

    private static double Share(FrontierState state, ResearchKind kind) => Effect(state.Research, kind) / 100;
}

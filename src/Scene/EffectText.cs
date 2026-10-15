using System;
using System.Globalization;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class EffectText
{
    private static readonly NumberFormatInfo French = new() { NumberDecimalSeparator = ",", NumberGroupSeparator = " " };

    public static string Now(FrontierState state, BuildingKind kind)
    {
        int level = state.Level(kind);
        return kind switch
        {
            BuildingKind.Farm => $"poule dorée {Times(Economy.ChickenPace(state))} plus fréquente",
            BuildingKind.Mill => $"absence comptée jusqu'à {Hours(Economy.AbsenceCap(state))}",
            BuildingKind.Bakery => $"+{Percent(Balance.SupplyBonus * state.Supply)} de dégâts ({state.Supply}/{Balance.SupplyMax} livraisons), convois {Times(Economy.ConvoyPace(state))} plus fréquents",
            BuildingKind.Forge => $"+{Percent(Balance.ForgeBonus * level)} de dégâts et de PV du héros",
            BuildingKind.Market => $"+{Percent(Balance.MarketBonus * level)} d'or des monstres",
            BuildingKind.Tower => $"+{Percent(Balance.TowerBonus * level)} de dégâts du second compagnon",
            BuildingKind.Portal => $"+{Percent(Balance.PortalBonus * level)} de tout l'or",
            BuildingKind.Barracks => $"+{Percent(Balance.BarracksBonus * level)} de dégâts du premier compagnon et de PV des compagnons",
            BuildingKind.Chapel => $"compétences rechargées {Times(Economy.ChapelPace(state))} plus vite",
            BuildingKind.Tavern => $"+{Percent(Balance.TavernBonus * level)} de dégâts des clics",
            BuildingKind.Workshop => $"-{Percent(Math.Min(Balance.WorkshopMax, Balance.WorkshopDiscount * level))} sur l'équipement",
            _ => $"+{(Economy.BossSeconds(state) - Balance.BossSeconds).ToString("0.#", French)} s au chrono des boss",
        };
    }

    private static string Percent(double ratio) => $"{(ratio * 100).ToString("0.#", French)} %";

    private static string Times(double factor) => $"{factor.ToString("0.##", French)}×";

    private static string Hours(double seconds)
    {
        int minutes = (int)Math.Round(seconds / 60);
        return minutes % 60 == 0 ? $"{minutes / 60} h" : $"{minutes / 60} h {minutes % 60:00}";
    }
}

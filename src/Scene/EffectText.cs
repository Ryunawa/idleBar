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
            BuildingKind.Bakery => $"+{Percent(Balance.SupplyBonus * state.Supply)} de dégâts ({state.Supply}/{Balance.SupplyMax} livraisons)",
            BuildingKind.Forge => $"+{Percent(Balance.ForgeBonus * level)} de dégâts et de PV du héros",
            BuildingKind.Market => $"+{Percent(Balance.MarketBonus * level)} d'or des monstres",
            BuildingKind.Tower => $"+{Percent(Balance.TowerBonus * level)} de dégâts de la mage",
            BuildingKind.Portal => $"+{Percent(Balance.PortalBonus * level)} d'or de tous les emplacements",
            BuildingKind.Barracks => $"+{Percent(Balance.BarracksBonus * level)} de dégâts de l'archère et de PV des compagnons",
            BuildingKind.Chapel => $"compétences rechargées {Times(Skills.Of(SkillKind.Smite).Cooldown / Economy.SkillCooldown(state, SkillKind.Smite))} plus vite",
            BuildingKind.Tavern => $"+{Percent(Balance.TavernBonus * level)} de dégâts des clics",
            BuildingKind.Workshop => $"-{Percent(Math.Min(Balance.WorkshopMax, Balance.WorkshopDiscount * level))} sur le prix des combattants",
            _ => $"+{(Economy.BossSeconds(state) - Balance.BossSeconds).ToString("0", French)} s au chrono des boss",
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

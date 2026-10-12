using System;
using IdleBar.Frontier;
using IdleBar.Ui;

namespace IdleBar.Scene;

public static class LaneHints
{
    public static string? Describe(LaneTarget target, FrontierState state) => target.Kind switch
    {
        TargetKind.Chicken => "Poule dorée : clique, or ×7 pendant 20 s",
        TargetKind.Skill => Skill((SkillKind)target.Index, state),
        TargetKind.Monster => Monster(state),
        TargetKind.Fighter => Fighter((FighterKind)target.Index, state),
        TargetKind.Building => Plot(target.Index, state),
        TargetKind.Convoy => "Convoi de pain : +5 % de dégâts par livraison, 5 au plus",
        TargetKind.Crier => "Le crieur public et ses nouvelles du royaume",
        TargetKind.Fog => "Brouillard : chaque boss vaincu le fait reculer",
        TargetKind.BossCall => $"Défier le boss du niveau {state.Stage + 1} : 30 s pour le vaincre",
        _ => null,
    };

    public static string Wait(double seconds) =>
        seconds >= 60 ? $"{Math.Ceiling(seconds / 60)} min" : $"{Math.Ceiling(seconds)} s";

    private static string Skill(SkillKind kind, FrontierState state)
    {
        SkillInfo skill = Skills.Of(kind);
        double cooldown = state.Cooldowns[(int)kind];
        return $"{skill.Name} : {skill.Effect} " + (cooldown > 0 ? $"Prête dans {Wait(cooldown)}." : "Prête.");
    }

    private static string? Monster(FrontierState state)
    {
        if (state.Monster is not Monster monster)
        {
            return null;
        }

        string life = $"{NumberFormat.Amount(Math.Max(0, monster.Hp))} / {NumberFormat.Amount(monster.MaxHp)} PV";
        string boss = monster.Boss ? $" · {Math.Ceiling(Math.Max(0, monster.BossLeft))} s" : string.Empty;
        return $"{monster.Name} · {life}{boss} · un clic : {NumberFormat.Amount(Economy.ClickDamage(state))}";
    }

    private static string Fighter(FighterKind kind, FrontierState state)
    {
        FighterInfo fighter = Fighters.Of(kind);
        return $"{fighter.Name} niv {state.Level(kind)} · {NumberFormat.Rate(Economy.Dps(state, kind))} dégâts/s · clique pour l'améliorer";
    }

    private static string Plot(int plot, FrontierState state)
    {
        if (!Economy.Unlocked(state, plot))
        {
            return $"Terrain inexploré : bats le boss du niveau {Plots.Of(plot).Unlock - 1}";
        }

        if (state.Built[plot] is not BuildingKind kind)
        {
            return $"Terrain libre : choisis quoi y bâtir pour {NumberFormat.Gold(Economy.PlotCost(state, plot))} · clique pour gérer";
        }

        BuildingInfo building = Buildings.Of(kind);
        return $"{building.Name} niv {state.Levels[plot]} · +{NumberFormat.Rate(Economy.PlotGold(state, plot))} or/s · {building.Effect}";
    }
}

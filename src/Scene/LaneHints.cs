using System;
using IdleBar.Frontier;
using IdleBar.Ui;

namespace IdleBar.Scene;

public static class LaneHints
{
    public static string? Describe(LaneTarget target, FrontierState state) => target.Kind switch
    {
        TargetKind.Chicken => "Poule dorée : clique, or ×7 pendant 20 s",
        TargetKind.Skill => Slot(target.Index, state),
        TargetKind.Monster => Monster(state),
        TargetKind.Fighter => Fighter((FighterKind)target.Index, state),
        TargetKind.Ally => Reinforcement(target.Index, state),
        TargetKind.Titan => Titan(state),
        TargetKind.Building => Plot(target.Index, state),
        TargetKind.Convoy => "Convoi de pain : +5 % de dégâts par livraison pendant 25 s, 5 au plus",
        TargetKind.Crier => "Le crieur public et ses nouvelles du royaume",
        TargetKind.Fog => "Brouillard : chaque boss vaincu le fait reculer",
        TargetKind.BossCall => $"Défier le boss du niveau {state.Stage + 1} : {Economy.BossSeconds(state):0} s pour le vaincre",
        _ => null,
    };

    public static string Wait(double seconds) =>
        seconds >= 60 ? $"{Math.Ceiling(seconds / 60)} min" : $"{Math.Ceiling(seconds)} s";

    private static string? Slot(int slot, FrontierState state)
    {
        if (slot < 0 || slot >= FrontierState.SlotCount)
        {
            return null;
        }

        if (state.Slots[slot] is not SkillKind kind)
        {
            int free = SkillTree.Free(state);
            string points = free > 0 ? $" ({free} point{(free > 1 ? "s" : string.Empty)} à dépenser)" : string.Empty;
            string rule = SkillTree.CanRedistribute(state) ? string.Empty : " · une compétence déjà placée dans la partie y repart avec sa recharge complète";
            return $"Emplacement de compétence vide : clique pour en placer une depuis l'onglet Compétences{points}{rule}";
        }

        return $"{Skills.Of(kind).Name} : {SkillText.Now(state, kind)} {SkillText.Status(state, kind)}";
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
        bool hero = kind == FighterKind.Hero;
        string name = SceneText.Fighter(state, kind);
        string action = hero ? "clique pour le personnaliser" : "clique pour voir sa progression";
        if (!Vitality.Up(state, kind))
        {
            return $"{name} est à terre : de retour dans {Math.Ceiling(state.Downed[(int)kind])} s · {action}";
        }

        string life = $"{NumberFormat.Amount(Vitality.Hp(state, kind))} / {NumberFormat.Amount(Vitality.MaxHp(state, kind))} PV";
        return $"{name} niv {state.Level(kind)} · xp {Experience.Share(state, kind) * 100:0} % · {life} · {NumberFormat.Rate(Economy.Dps(state, kind))} dégâts/s · {action}";
    }

    private static string? Titan(FrontierState state) => state.Titan is TitanView titan
        ? $"{Titans.Name(titan.Kind)}, titan de la semaine : {Math.Ceiling(titan.Left * 100)} % de PV · ta part : {NumberFormat.Exact(titan.Mine)} coups · clique pour le détail"
        : null;

    private static string? Reinforcement(int slot, FrontierState state)
    {
        if (slot >= state.Allies.Count)
        {
            return null;
        }

        Ally ally = state.Allies[slot];
        string name = $"{ally.Look.Name} ({Roles.Of(ally.Role).Name}), renfort de ta guilde";
        if (ally.Downed > 0)
        {
            return $"{name}, à terre : de retour dans {Math.Ceiling(ally.Downed)} s";
        }

        string life = $"{NumberFormat.Amount(Vitality.AllyMaxHp(state, ally) * ally.Health)} PV";
        return $"{name} · {life} · {NumberFormat.Rate(Economy.AllyDps(state, ally))} dégâts/s · encore {Wait(ally.Left)}";
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
        return $"{building.Name} niv {state.Levels[plot]} · +{NumberFormat.Rate(Economy.PlotGold(state, plot))} or/s · {EffectText.Now(state, kind)}";
    }
}

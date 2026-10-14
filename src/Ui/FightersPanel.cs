using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Frontier;
using IdleBar.Scene;

namespace IdleBar.Ui;

public partial class FightersPanel : VBoxContainer
{
    private static readonly IReadOnlyList<OfferChoice> Classes = [.. Roles.All.Select(role => new OfferChoice((int)role.Role, role.Name, $"{role.Effect} {role.Training}"))];

    private readonly List<OfferRow> _rows = [];
    private readonly OfferRow _armor = new();

    public FrontierGame? Game { get; set; }

    public override void _Ready()
    {
        Name = "Combattants";
        VBoxContainer rows = new();
        rows.AddThemeConstantOverride("separation", 6);
        rows.AddChild(WindowRows.Muted($"Chaque classe a un passif pour toute l'équipe et un actif qui part tout seul dès qu'il est rechargé ; en compagnon, le passif compte moitié moins et l'actif se recharge deux fois moins vite. Tes combattants gagnent surtout leur xp avec leur actif ; leurs coups ordinaires en rapportent un peu. Chaque niveau demande plus d'xp que le précédent et augmente les dégâts et les PV ; tous les 10 niveaux, ils sont multipliés par {NumberFormat.Factor(Balance.MilestoneFactor)}. L'or sert au village, à l'armure et à l'équipement. Un repli remet l'xp à zéro."));
        foreach (FighterInfo fighter in Fighters.All)
        {
            OfferRow row = new();
            row.Chosen += id => Game?.Buy(new Recruitment(fighter.Kind, (HeroRole)id));
            rows.AddChild(row);
            _rows.Add(row);
        }

        _armor.Bought += count => Game?.BuyMany(new ArmorPurchase(), count);
        rows.AddChild(_armor);
        AddChild(WindowRows.Scroll(rows));
    }

    public void Refresh()
    {
        if (Game is not FrontierGame game)
        {
            return;
        }

        FrontierState state = game.State;
        foreach (FighterInfo fighter in Fighters.All)
        {
            Show(_rows[(int)fighter.Kind], state, fighter);
        }

        string armor = $"Protège toute l'équipe : +{Balance.ArmorBonus * 100:0} % de PV par niveau, soit ×{NumberFormat.Factor(Vitality.ArmorFactor(state.Armor))} aujourd'hui.";
        _armor.Show($"Armure de l'équipe · niv {state.Armor}", armor, $"Renforcer · {NumberFormat.Gold(Economy.ArmorCost(state))}", state, new ArmorPurchase());
    }

    private static void Show(OfferRow row, FrontierState state, FighterInfo fighter)
    {
        int level = state.Level(fighter.Kind);
        if (level == 0 && !Fighters.Unlocked(state, fighter.Kind))
        {
            row.ShowLocked($"{fighter.Name} · pas encore là", fighter.Effect, $"Après le boss {fighter.Unlock - 1}");
            return;
        }

        if (level == 0)
        {
            row.ShowChoice($"{fighter.Name} · prêt à rejoindre", "Choisis sa classe :", "Recruter", Classes, true, (int)fighter.Class);
            return;
        }

        RoleInfo role = Roles.Of(state.ClassOf(fighter.Kind));
        string name = fighter.Kind == FighterKind.Hero ? SceneText.Hero(state) : $"{role.Name} · {fighter.Name.ToLowerInvariant()}";
        string progress = $"xp {Experience.Share(state, fighter.Kind) * 100:0} % vers le niveau {level + 1}";
        string stats = $"{NumberFormat.Rate(Economy.Dps(state, fighter.Kind))} dégâts/s · {NumberFormat.Amount(Vitality.MaxHp(state, fighter.Kind))} PV";
        row.ShowInfo($"{name} · niv {level}", $"{progress} · {stats}. {role.Training}");
    }
}

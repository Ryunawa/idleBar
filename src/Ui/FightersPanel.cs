using System.Collections.Generic;
using Godot;
using IdleBar.Frontier;
using IdleBar.Scene;

namespace IdleBar.Ui;

public partial class FightersPanel : VBoxContainer
{
    private readonly List<OfferRow> _rows = [];
    private readonly OfferRow _armor = new();

    public FrontierGame? Game { get; set; }

    public override void _Ready()
    {
        Name = "Combattants";
        VBoxContainer rows = new();
        rows.AddThemeConstantOverride("separation", 6);
        rows.AddChild(WindowRows.Muted("Chaque niveau augmente les dégâts et les PV. Tous les 10 niveaux, ils sont multipliés par 1,5. Un personnage à terre revient après 30 s, 45 s pour le héros."));
        foreach (FighterInfo fighter in Fighters.All)
        {
            OfferRow row = new();
            row.Bought += count => Game?.BuyMany(new FighterPurchase(fighter.Kind), count);
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
            int level = state.Level(fighter.Kind);
            double cost = Economy.FighterCost(state, fighter.Kind);
            string name = fighter.Kind == FighterKind.Hero ? SceneText.Hero(state) : fighter.Name;
            string title = level > 0 ? $"{name} · niv {level}" : $"{name} · à recruter";
            string detail = level > 0
                ? $"{NumberFormat.Rate(Economy.Dps(state, fighter.Kind))} dégâts/s · {NumberFormat.Amount(Vitality.MaxHp(state, fighter.Kind))} PV. {fighter.Effect}"
                : fighter.Effect;
            string action = $"{(level > 0 ? "Améliorer" : "Recruter")} · {NumberFormat.Gold(cost)}";
            _rows[(int)fighter.Kind].Show(title, detail, action, state, new FighterPurchase(fighter.Kind));
        }

        string armor = $"+{Balance.ArmorBonus * 100:0} % de PV pour toute l'équipe par niveau, soit ×{Vitality.ArmorFactor(state.Armor):0.#} aujourd'hui.";
        _armor.Show($"Armure · niv {state.Armor}", armor, $"Renforcer · {NumberFormat.Gold(Economy.ArmorCost(state))}", state, new ArmorPurchase());
    }
}

using System.Collections.Generic;
using Godot;
using IdleBar.Frontier;

namespace IdleBar.Ui;

public partial class FightersPanel : VBoxContainer
{
    private readonly List<OfferRow> _rows = [];

    public FrontierGame? Game { get; set; }

    public override void _Ready()
    {
        Name = "Combattants";
        VBoxContainer rows = new();
        rows.AddThemeConstantOverride("separation", 6);
        rows.AddChild(WindowRows.Muted("Chaque niveau augmente les dégâts. Tous les 10 niveaux, les dégâts sont multipliés par 1,5."));
        foreach (FighterInfo fighter in Fighters.All)
        {
            OfferRow row = new();
            row.Bought += count => Game?.BuyMany(new FighterPurchase(fighter.Kind), count);
            rows.AddChild(row);
            _rows.Add(row);
        }

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
            string title = level > 0 ? $"{fighter.Name} · niv {level}" : $"{fighter.Name} · à recruter";
            string detail = level > 0
                ? $"{NumberFormat.Rate(Economy.Dps(state, fighter.Kind))} dégâts/s. {fighter.Effect}"
                : fighter.Effect;
            string action = $"{(level > 0 ? "Améliorer" : "Recruter")} · {NumberFormat.Gold(cost)}";
            _rows[(int)fighter.Kind].Show(title, detail, action, state.Gold >= cost, false);
        }
    }
}

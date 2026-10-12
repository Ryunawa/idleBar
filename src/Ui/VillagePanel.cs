using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Frontier;

namespace IdleBar.Ui;

public partial class VillagePanel : VBoxContainer
{
    private readonly List<OfferRow> _rows = [];

    public FrontierGame? Game { get; set; }

    public override void _Ready()
    {
        Name = "Village";
        VBoxContainer rows = new();
        rows.AddThemeConstantOverride("separation", 6);
        rows.AddChild(WindowRows.Muted("Les bâtiments produisent de l'or, même quand le jeu est fermé (8 h au plus). Aux niveaux 10, 25, 50, 100 et 250, un palier double leur production et les transforme."));
        foreach (BuildingInfo building in Buildings.All)
        {
            OfferRow row = new();
            row.Bought += count => Game?.BuyMany(new BuildingPurchase(building.Kind), count);
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
        foreach (BuildingInfo building in Buildings.All)
        {
            OfferRow row = _rows[(int)building.Kind];
            if (!Economy.Unlocked(state, building.Kind))
            {
                row.Show($"{building.Name} · inexploré", $"Bats le boss du niveau {building.Unlock - 1} pour conquérir ce terrain. {building.Effect}", $"Boss {building.Unlock - 1}", false, true);
                continue;
            }

            int level = state.Level(building.Kind);
            double cost = Economy.BuildingCost(state, building.Kind);
            string title = level > 0 ? $"{building.Name} · niv {level} · {Tier(level)}" : $"{building.Name} · à bâtir";
            string detail = level > 0 ? $"+{NumberFormat.Rate(Economy.BuildingGold(state, building.Kind))} or/s. {building.Effect}" : building.Effect;
            string action = $"{(level > 0 ? "Agrandir" : "Construire")} · {NumberFormat.Gold(cost)}";
            row.Show(title, detail, action, state.Gold >= cost, false);
        }
    }

    private static string Tier(int level)
    {
        int tier = Economy.Tier(level);
        int next = Balance.Tiers.FirstOrDefault(threshold => threshold > level);
        return next > 0 ? $"palier {tier}/{Balance.Tiers.Length} (prochain : niv {next})" : $"palier {tier}/{Balance.Tiers.Length}";
    }
}

using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Frontier;
using IdleBar.Scene;

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
        rows.AddChild(WindowRows.Muted("Chaque emplacement produit de l'or, même quand le jeu est fermé. Choisis le bâtiment à y construire : il y apporte son effet jusqu'au prochain repli. Aux niveaux 10, 25, 50, 100 et 250, un palier double la production."));
        foreach (PlotInfo plot in Plots.All)
        {
            OfferRow row = new();
            int index = plot.Index;
            row.Bought += count => Game?.BuyMany(new PlotPurchase(index), count);
            row.Chosen += id => Game?.Buy(new Construction(index, (BuildingKind)id));
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
        List<OfferChoice> free = [.. Buildings.All.Where(building => state.PlotOf(building.Kind) is null).Select(building => new OfferChoice((int)building.Kind, building.Name, building.Effect))];
        foreach (PlotInfo plot in Plots.All)
        {
            OfferRow row = _rows[plot.Index];
            string name = $"Emplacement {plot.Index + 1}";
            if (!Economy.Unlocked(state, plot.Index))
            {
                row.ShowLocked($"{name} · inexploré", $"Bats le boss du niveau {plot.Unlock - 1} pour conquérir ce terrain.", $"Boss {plot.Unlock - 1}");
                continue;
            }

            double cost = Economy.PlotCost(state, plot.Index);
            if (state.Built[plot.Index] is not BuildingKind kind)
            {
                string yield = NumberFormat.Rate(Economy.PlotGoldAt(state, plot.Index, 1));
                row.ShowChoice($"{name} · libre", $"Produit {yield} or/s au niveau 1.", $"Construire · {NumberFormat.Gold(cost)}", free, state.Gold >= cost);
                continue;
            }

            int level = state.Levels[plot.Index];
            BuildingInfo building = Buildings.Of(kind);
            string title = $"{building.Name} · {name.ToLowerInvariant()} · niv {level} · {Tier(level)}";
            string detail = $"+{NumberFormat.Rate(Economy.PlotGold(state, plot.Index))} or/s. {building.Effect} Actuellement : {EffectText.Now(state, kind)}.";
            row.Show(title, detail, $"Agrandir · {NumberFormat.Gold(cost)}", state, new PlotPurchase(plot.Index));
        }
    }

    private static string Tier(int level)
    {
        int tier = Economy.Tier(level);
        int next = Balance.Tiers.FirstOrDefault(threshold => threshold > level);
        return next > 0 ? $"palier {tier}/{Balance.Tiers.Length} (prochain : niv {next})" : $"palier {tier}/{Balance.Tiers.Length}";
    }
}

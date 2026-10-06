using System;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public static class FittingRows
{
    public static HBoxContainer Create(TownContext context, FittingInfo fitting, Action<TownCommand> request)
    {
        if (context.Snapshot.Fittings.Contains(fitting.Id))
        {
            return ActionRow.Create($"{fitting.Name} · installé", fitting.Description, string.Empty, true, () => { });
        }

        WorldData world = context.World;
        string costs = string.Join(", ", fitting.Costs.Select(cost =>
            $"{ExchangeText.Lot(world, cost.GoodId, cost.Quantity)} ({NumberFormat.Amount(context.Owned(cost.GoodId))} en cale)"));
        bool affordable = fitting.Costs.All(cost => context.Owned(cost.GoodId) >= cost.Quantity);
        string fittingId = fitting.Id;
        return ActionRow.Create(fitting.Name, $"{fitting.Description} Demande {costs}.", "Installer", !affordable,
            () => request(actions => actions.InstallFittingAsync(fittingId)));
    }
}

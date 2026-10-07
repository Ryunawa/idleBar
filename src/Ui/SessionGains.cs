using System;
using IdleBar.Trade;

namespace IdleBar.Ui;

public sealed class SessionGains
{
    private readonly GameSession _session;

    public SessionGains(GameSession session)
    {
        _session = session;
        _session.Applied += Compare;
    }

    public event Action<LaneGain>? Gained;

    private void Compare(GameSnapshot? previous, GameSnapshot current)
    {
        if (previous?.Player is not PlayerState before || current.Player is not PlayerState after || _session.World is not WorldData world)
        {
            return;
        }

        int delivered = current.BatchesDeliveredSince(previous);
        if (delivered > 0 && world.FindRecipe(previous.Workshop!.RecipeId!) is RecipeInfo recipe)
        {
            Gained?.Invoke(new LaneGain($"+{NumberFormat.Amount(delivered * recipe.OutputQuantity)}", BarPalette.Text, recipe.OutputGoodId));
        }

        if (after.Coins > before.Coins)
        {
            Gained?.Invoke(new LaneGain($"+{NumberFormat.Coins(after.Coins - before.Coins)}", BarPalette.Gold));
        }
    }
}
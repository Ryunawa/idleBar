using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using IdleBar.Trade;

namespace IdleBar.Ui;

public sealed class QuickRelaunch
{
    private const int MaxBatches = 99;

    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    private readonly GameSession _session;
    private readonly GameActions _actions;
    private readonly RecipeMemory _memory;

    public QuickRelaunch(GameSession session, GameActions actions, RecipeMemory memory)
    {
        _session = session;
        _actions = actions;
        _memory = memory;
        _session.Changed += RememberRecipe;
    }

    public RecipeInfo? Candidate
    {
        get
        {
            if (_session is not { Status: SessionStatus.Ready, World: WorldData world, Player: PlayerState player, Workshop.IsProducing: false })
            {
                return null;
            }

            RecipeInfo? remembered = _memory.LastRecipeId is string recipeId ? world.FindRecipe(recipeId) : null;
            return remembered?.CraftId == player.CraftId ? remembered : world.RecipesOf(player.CraftId).FirstOrDefault();
        }
    }

    public async Task<RelaunchOutcome> RelaunchAsync(RecipeInfo recipe)
    {
        string? error = await ActionFeedback.CaptureAsync(() => _actions.StartProductionAsync(recipe.Id, MaxBatches));
        if (error is not null)
        {
            return new RelaunchOutcome(false, error);
        }

        WorkshopState? workshop = _session.Workshop;
        string good = _session.World?.GoodName(recipe.OutputGoodId).ToLower(French) ?? recipe.OutputGoodId;
        int units = (workshop?.Queued ?? 0) * recipe.OutputQuantity;
        string finish = workshop?.FinishesAt is { } finishesAt ? $", fini à {DurationFormat.ClockTime(finishesAt)}" : string.Empty;
        return new RelaunchOutcome(true, $"Atelier relancé : {NumberFormat.Amount(units)} {good}{finish}");
    }

    private void RememberRecipe()
    {
        if (_session.Workshop?.RecipeId is string recipeId)
        {
            _memory.Remember(recipeId);
        }
    }
}

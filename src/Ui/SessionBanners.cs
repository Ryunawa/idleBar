using System;
using System.Linq;
using IdleBar.Trade;

namespace IdleBar.Ui;

public sealed class SessionBanners
{
    private const float ArrivalBannerSeconds = 8;
    private const float ProductionBannerSeconds = 6;
    private const float NewsBannerSeconds = 8;
    private const float DecisionBannerSeconds = 12;

    private readonly GameSession _session;

    public SessionBanners(GameSession session)
    {
        _session = session;
        _session.Applied += Announce;
    }

    public event Action<string, float>? Announced;

    private void Announce(GameSnapshot? previous, GameSnapshot current)
    {
        if (previous is null || _session.World is not WorldData world)
        {
            return;
        }

        if (current.EndsTripOf(previous) && world.FindTown(current.Caravan!.TownId) is TownInfo town)
        {
            Announced?.Invoke($"Ta caravane est arrivée à {town.Name}", ArrivalBannerSeconds);
        }

        int delivered = current.BatchesDeliveredSince(previous);
        if (delivered > 0 && world.FindRecipe(previous.Workshop!.RecipeId!) is RecipeInfo recipe)
        {
            Announced?.Invoke($"+{ExchangeText.Lot(world, recipe.OutputGoodId, delivered * recipe.OutputQuantity)} à l'entrepôt", ProductionBannerSeconds);
        }

        if (current.News.Total > previous.News.Total)
        {
            Announced?.Invoke(ExchangeText.News(current.News), NewsBannerSeconds);
        }

        if (previous.TripEvent is null && current.TripEvent is TripEventInfo tripEvent)
        {
            string name = world.FindEventKind(tripEvent.KindId)?.Name ?? tripEvent.KindId;
            Announced?.Invoke($"{name} sur la route ! Réponds avant {DurationFormat.ClockTime(tripEvent.DecideBy)}", DecisionBannerSeconds);
        }

        long known = previous.Journal.Select(entry => entry.Id).DefaultIfEmpty().Max();
        if (current.Journal.Where(entry => !entry.Seen && entry.Id > known).MaxBy(entry => entry.Id) is JournalEntry latest)
        {
            Announced?.Invoke($"{latest.Title} : {latest.Detail}", NewsBannerSeconds);
        }
    }
}

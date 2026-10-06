using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Trade;

namespace IdleBar.Ui;

public static class NewsSlot
{
    public static SlotContent? For(GameSnapshot snapshot, WorldData? world, DateTimeOffset now)
    {
        if (snapshot.TripEvent is TripEventInfo tripEvent)
        {
            string name = world?.FindEventKind(tripEvent.KindId)?.Name ?? tripEvent.KindId;
            string limit = DurationFormat.Moment(tripEvent.DecideBy, now);
            return new SlotContent(
                name,
                $"réponds avant {limit}",
                BarPalette.Danger,
                $"{name} : ta caravane est arrêtée. Clique pour répondre avant {limit}, sinon ta directive choisira pour toi.");
        }

        ExchangeNews news = snapshot.News;
        if (news.Total > 0)
        {
            return ForExchanges(news);
        }

        if (snapshot.Journal.Where(entry => !entry.Seen).MaxBy(entry => entry.Id) is not JournalEntry latest)
        {
            return null;
        }

        string detail = snapshot.JournalUnseen > 1 ? NumberFormat.Count(snapshot.JournalUnseen, "événement", "événements") : latest.Title;
        return new SlotContent(
            "Journal",
            detail,
            latest.Tone switch
            {
                EventTone.Bad => BarPalette.Warning,
                EventTone.Good => BarPalette.Success,
                _ => BarPalette.Gold,
            },
            $"{latest.Detail} Clique pour ouvrir le journal.");
    }

    private static SlotContent ForExchanges(ExchangeNews news)
    {
        List<string> parts = [];
        AddPart(parts, news.Concluded, "conclue", "conclues");
        AddPart(parts, news.Expired, "expirée", "expirées");
        AddPart(parts, news.Delivered, "livré", "livrés");
        AddPart(parts, news.Failed, "en retard", "en retard");
        string title = news switch
        {
            { Offers: > 0, Contracts: > 0 } => "Échanges",
            { Offers: > 0 } => "Comptoir",
            _ => "Contrats",
        };
        return new SlotContent(
            title,
            string.Join(" · ", parts),
            news.Failed > 0 ? BarPalette.Warning : BarPalette.Gold,
            $"{ExchangeText.News(news)}. Clique pour voir le détail.");
    }

    private static void AddPart(List<string> parts, int count, string one, string many)
    {
        if (count > 0)
        {
            parts.Add(NumberFormat.Count(count, one, many));
        }
    }
}

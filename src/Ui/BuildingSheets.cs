using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Pixel;
using IdleBar.Trade;

namespace IdleBar.Ui;

public static class BuildingSheets
{
    private const int RelaunchBatches = 99;
    private const int ShownLines = 5;
    private const int ShownSales = 2;
    private const int ShownRoutes = 4;
    private const int ShownNews = 3;
    private const string Merchant = "negociant";

    public static BuildingSheet? For(StreetBuilding building, GameSession session, RecipeInfo? relaunch)
    {
        if (session is not { World: WorldData world, Snapshot: GameSnapshot { Player: PlayerState player } snapshot })
        {
            return null;
        }

        DateTimeOffset now = session.Clock.Now;
        string townId = snapshot.Caravan?.TownId ?? snapshot.Workshop?.TownId ?? player.HomeTownId;
        SheetContext context = new(world, snapshot, player, townId, now);
        return building switch
        {
            StreetBuilding.Workshop when player.CraftId == Merchant => MerchantCounter(context),
            StreetBuilding.Workshop => Workshop(context, relaunch),
            StreetBuilding.Warehouse => Warehouse(context),
            StreetBuilding.Market => Market(context),
            StreetBuilding.Counter => Counter(context),
            StreetBuilding.Relay => Relay(context),
            StreetBuilding.Crier => Crier(context),
            StreetBuilding.Caravan when snapshot.Caravan?.IsTravelling(now) == true => Journey(context),
            StreetBuilding.Caravan => Caravan(context),
            _ => Signpost(context),
        };
    }

    private static BuildingSheet Workshop(SheetContext context, RecipeInfo? relaunch)
    {
        (WorldData world, GameSnapshot snapshot, PlayerState player, string _, DateTimeOffset now) = context;
        WorkshopState workshop = snapshot.Workshop!;
        List<SheetLine> lines = [];
        List<SheetAction> actions = [];
        string summary;
        if (workshop.IsPaused(now))
        {
            summary = $"En réparation jusqu'à {DurationFormat.ClockTime(workshop.PausedUntil!.Value)}.";
        }
        else if (workshop.IsProducing && world.FindRecipe(workshop.RecipeId!) is RecipeInfo recipe)
        {
            string lot = ExchangeText.Lot(world, recipe.OutputGoodId, workshop.RemainingBatches(now) * recipe.OutputQuantity);
            summary = $"Fabrique {lot}, fini à {DurationFormat.ClockTime(workshop.FinishesAt!.Value)}.";
            lines.Add(new SheetLine($"File : {workshop.RemainingBatches(now)}/{workshop.MaxQueue} fabrications", BarPalette.Muted));
        }
        else
        {
            summary = "À l'arrêt.";
            if (relaunch is not null)
            {
                string good = world.GoodNoun(relaunch.OutputGoodId, 2);
                string recipeId = relaunch.Id;
                actions.Add(new SheetAction($"Relancer : {good}", true, actions => actions.StartProductionAsync(recipeId, RelaunchBatches),
                    "Lance la recette autant de fois que tes matières premières le permettent."));
            }
        }

        if (snapshot.SpecialOrders.FirstOrDefault(order => order.Status == SpecialOrderStatus.Open) is SpecialOrderInfo special)
        {
            int held = snapshot.StorageAt(special.TownId).FirstOrDefault(line => line.GoodId == special.GoodId)?.Quantity ?? 0;
            string progress = special.Crafted ? $"fabriqué {special.Produced}/{special.Quantity}" : $"en stock {Math.Min(held, special.Quantity)}/{special.Quantity}";
            lines.Add(new SheetLine($"Commande : {ExchangeText.Lot(world, special.GoodId, special.Quantity)} à {NumberFormat.Coins(special.UnitPrice)} pièce, {progress}, avant {DurationFormat.ClockTime(special.Deadline)}", BarPalette.Gold, special.GoodId));
            long orderId = special.Id;
            if (special.IsReady(held))
            {
                actions.Add(new SheetAction("Livrer la commande", true, actions => actions.FulfillSpecialOrderAsync(orderId)));
            }
        }

        AddUpgrade(actions, workshop, player, "Agrandir");
        string craft = MasteryText.Title(snapshot.Mastery?.Rank, world.FindCraft(player.CraftId)?.Name ?? player.CraftId);
        return new BuildingSheet($"Atelier · niveau {workshop.Level}", $"{craft}. {summary}", lines, actions, TownTab.Workshop);
    }

    private static BuildingSheet MerchantCounter(SheetContext context)
    {
        (WorldData world, GameSnapshot snapshot, PlayerState player, string townId, DateTimeOffset _) = context;
        WorkshopState counter = snapshot.Workshop!;
        int offers = snapshot.MyOffers.Count(offer => offer.Status == OfferStatus.Open);
        List<SheetLine> lines =
        [
            new($"{NumberFormat.Count(offers, "offre ouverte", "offres ouvertes")} au comptoir", BarPalette.Text),
            new(snapshot.Branches.Count == 0
                ? "Aucune succursale pour l'instant."
                : $"Succursales : {string.Join(", ", snapshot.Branches.Select(world.TownName))}", BarPalette.Muted),
        ];
        List<SheetAction> actions = [];
        AddUpgrade(actions, counter, player, "Agrandir le comptoir");
        return new BuildingSheet($"Ton comptoir · niveau {counter.Level}", $"Négoce à {world.TownName(townId)}.", lines, actions, TownTab.Counter);
    }

    private static BuildingSheet Warehouse(SheetContext context)
    {
        (WorldData world, GameSnapshot snapshot, PlayerState _, string townId, DateTimeOffset _) = context;
        IReadOnlyList<StockLine> storage = snapshot.StorageAt(townId);
        int capacity = snapshot.Caravan is null ? snapshot.StorageCapacityAt(townId) : world.Rules.DepotCapacity + snapshot.Standing.StorageBonusAt(townId);
        List<SheetLine> lines = storage
            .OrderByDescending(line => line.Quantity)
            .Take(ShownLines)
            .Select(line => new SheetLine(ExchangeText.Lot(world, line.GoodId, line.Quantity), BarPalette.Text, line.GoodId))
            .ToList();
        if (storage.Count > ShownLines)
        {
            lines.Add(new SheetLine($"et {NumberFormat.Count(storage.Count - ShownLines, "autre marchandise", "autres marchandises")}", BarPalette.Muted));
        }

        string summary = storage.Count == 0 ? "Rien n'est entreposé ici." : $"{NumberFormat.Amount(storage.Sum(line => line.Quantity))} places occupées sur {NumberFormat.Amount(capacity)}.";
        return new BuildingSheet($"Entrepôt de {world.TownName(townId)}", summary, lines, [], TownTab.Warehouse);
    }

    private static BuildingSheet Market(SheetContext context)
    {
        (WorldData world, GameSnapshot snapshot, PlayerState _, string townId, DateTimeOffset _) = context;
        Dictionary<string, MarketQuote> quotes = snapshot.MarketAt(townId).ToDictionary(quote => quote.GoodId);
        IEnumerable<StockLine> sellable = snapshot.Caravan is null
            ? snapshot.StorageAt(townId).Where(line => world.IsCrafted(line.GoodId))
            : snapshot.Cargo.Concat(snapshot.StorageAt(townId));
        List<SheetLine> lines = [];
        List<SheetAction> actions = [];
        foreach (StockLine line in sellable.GroupBy(line => line.GoodId)
            .Select(group => new StockLine(group.Key, group.Sum(line => line.Quantity)))
            .Where(line => quotes.ContainsKey(line.GoodId))
            .OrderByDescending(line => line.Quantity * quotes[line.GoodId].SellPrice)
            .Take(ShownSales))
        {
            MarketQuote quote = quotes[line.GoodId];
            lines.Add(new SheetLine($"{world.GoodName(line.GoodId)} : vendu {NumberFormat.Rate(quote.SellPrice)} écus, tu en as {NumberFormat.Amount(line.Quantity)}", BarPalette.Text, line.GoodId));
            string goodId = line.GoodId;
            int quantity = line.Quantity;
            actions.Add(new SheetAction($"Vendre {ExchangeText.Lot(world, goodId, quantity)}", true, actions => actions.SellAsync(goodId, quantity, townId),
                $"Environ {NumberFormat.Coins(quantity * quote.SellPrice * 0.9)} : le prix baisse un peu à chaque unité vendue."));
        }

        string summary = lines.Count == 0 ? "Tu n'as rien à vendre ici pour l'instant." : "Vends d'un clic ce que tu as sous la main.";
        return new BuildingSheet($"Marché de {world.TownName(townId)}", summary, lines, actions, TownTab.Market);
    }

    private static BuildingSheet Counter(SheetContext context)
    {
        (WorldData world, GameSnapshot snapshot, PlayerState _, string townId, DateTimeOffset now) = context;
        OfferInfo[] others = snapshot.OffersAt(townId).ToArray();
        OfferInfo[] mine = snapshot.MyOffers.Where(offer => offer.TownId == townId && offer.Status == OfferStatus.Open).ToArray();
        List<SheetLine> lines = others.Take(3)
            .Select(offer => new SheetLine($"{ExchangeText.Offer(world, offer)} · {offer.Seller ?? "un marchand"}", BarPalette.Text, offer.GiveGoodId ?? offer.WantGoodId))
            .ToList();
        lines.Add(new SheetLine($"Tes offres ici : {NumberFormat.Amount(mine.Length)}", BarPalette.Muted));
        string summary = others.Length == 0 ? "Personne ne propose rien ici pour l'instant." : $"{NumberFormat.Count(others.Length, "offre", "offres")} d'autres marchands.";
        return new BuildingSheet($"Comptoir de {world.TownName(townId)}", summary, lines, [], TownTab.Counter);
    }

    private static BuildingSheet Relay(SheetContext context)
    {
        (WorldData world, GameSnapshot snapshot, PlayerState _, string townId, DateTimeOffset _) = context;
        List<SheetLine> lines;
        if (snapshot.Caravan is not null)
        {
            int contracts = snapshot.Contracts.Count(contract => contract.OriginTownId == townId);
            int orders = snapshot.Supply.Count(order => order.TownId == townId);
            int carried = snapshot.MyContracts.Count(contract => contract is { Role: ContractRole.Carrier, IsUnderway: true });
            lines =
            [
                new($"{NumberFormat.Count(contracts, "contrat", "contrats")} à prendre ici", BarPalette.Text),
                new($"{NumberFormat.Count(orders, "commande", "commandes")} à livrer ici", BarPalette.Text),
                new($"{NumberFormat.Count(carried, "contrat transporté", "contrats transportés")}", BarPalette.Muted),
            ];
        }
        else
        {
            int orders = snapshot.MyOffers.Count(offer => SupplyView.IsOrder(offer) && offer.Status == OfferStatus.Open);
            int shipments = snapshot.MyContracts.Count(contract => contract is { Role: ContractRole.Shipper, IsUnderway: true });
            lines =
            [
                new($"{NumberFormat.Count(orders, "commande en attente", "commandes en attente")} de livraison", BarPalette.Text),
                new($"{NumberFormat.Count(shipments, "envoi en cours", "envois en cours")}", BarPalette.Muted),
            ];
        }

        return new BuildingSheet("Relais des contrats", $"Transports et commandes à {world.TownName(townId)}.", lines, [], TownTab.Contracts);
    }

    private static BuildingSheet Crier(SheetContext context)
    {
        (WorldData _, GameSnapshot snapshot, PlayerState _, string _, DateTimeOffset _) = context;
        List<SheetLine> lines = snapshot.Journal
            .Take(ShownNews)
            .Select(entry => new SheetLine($"{entry.Title} : {entry.Detail}", entry.Seen ? BarPalette.Muted : ToneColor(entry.Tone)))
            .ToList();
        int unseen = snapshot.JournalUnseen + snapshot.News.Total;
        string summary = unseen == 0 ? "Rien de nouveau." : $"{NumberFormat.Count(unseen, "nouvelle", "nouvelles")} à lire.";
        return new BuildingSheet("Crieur de la ville", summary, lines, [], TownTab.Journal);
    }

    private static BuildingSheet Caravan(SheetContext context)
    {
        (WorldData world, GameSnapshot snapshot, PlayerState _, string _, DateTimeOffset _) = context;
        CaravanState caravan = snapshot.Caravan!;
        List<SheetLine> lines = snapshot.Cargo
            .OrderByDescending(line => line.Quantity)
            .Take(ShownLines)
            .Select(line => new SheetLine(ExchangeText.Lot(world, line.GoodId, line.Quantity), BarPalette.Text, line.GoodId))
            .ToList();
        string summary = $"Cale {NumberFormat.Amount(caravan.Load)}/{NumberFormat.Amount(caravan.Capacity)}"
            + (lines.Count == 0 ? ", vide." : ".");
        return new BuildingSheet($"Caravane · {NumberFormat.Count(caravan.Wagons, "chariot", "chariots")}", summary, lines, [], TownTab.Caravan);
    }

    private static BuildingSheet Journey(SheetContext context)
    {
        (WorldData world, GameSnapshot snapshot, PlayerState _, string _, DateTimeOffset now) = context;
        CaravanState caravan = snapshot.Caravan!;
        List<SheetLine> lines = [new($"Cale {NumberFormat.Amount(caravan.Load)}/{NumberFormat.Amount(caravan.Capacity)}", BarPalette.Muted)];
        List<SheetAction> actions = [];
        if (snapshot.TripEvent is TripEventInfo tripEvent && world.FindEventKind(tripEvent.KindId) is EventKindInfo kind)
        {
            lines.Insert(0, new SheetLine($"{kind.Name} : {kind.Description} Réponds avant {DurationFormat.ClockTime(tripEvent.DecideBy)}.", BarPalette.Warning));
            foreach (EventChoiceInfo choice in kind.Choices)
            {
                string choiceId = choice.Id;
                actions.Add(new SheetAction(choice.Name, true, actions => actions.AnswerEventAsync(choiceId), choice.Description));
            }
        }

        string arrival = caravan.ArrivesAt is DateTimeOffset arrives ? DurationFormat.ClockTime(arrives) : "bientôt";
        return new BuildingSheet($"En route vers {world.TownName(caravan.TownId)}", $"Arrivée vers {arrival}, dans {DurationFormat.Span(caravan.Remaining(now))}.", lines, actions, TownTab.Journal);
    }

    private static BuildingSheet Signpost(SheetContext context)
    {
        (WorldData world, GameSnapshot snapshot, PlayerState _, string townId, DateTimeOffset _) = context;
        double swift = snapshot.Mastery?.Bonus("swift") ?? 0;
        string? directive = snapshot.Caravan?.DirectiveId;
        List<SheetAction> actions = world.RoutesFrom(townId)
            .OrderBy(route => route.Seconds)
            .Take(ShownRoutes)
            .Select(route =>
            {
                string destinationId = route.ToTownId;
                TimeSpan duration = TimeSpan.FromSeconds(route.Seconds * (1 - swift));
                return new SheetAction($"Partir pour {world.TownName(destinationId)} · {DurationFormat.Span(duration)}", true,
                    actions => actions.DepartAsync(destinationId, directive), $"Route de {BiomeText.Describe(route.Biome)}.");
            })
            .ToList();
        return new BuildingSheet("Routes", "Choisis ta prochaine étape.", [], actions, TownTab.Routes);
    }

    private static void AddUpgrade(List<SheetAction> actions, WorkshopState workshop, PlayerState player, string label)
    {
        if (workshop.NextLevelPrice is int price)
        {
            actions.Add(new SheetAction($"{label} · {NumberFormat.Coins(price)}", player.Coins >= price, actions => actions.UpgradeWorkshopAsync(),
                player.Coins >= price ? "Plus de place à l'entrepôt et une file plus longue." : $"Il te faut {NumberFormat.Coins(price)}."));
        }
    }

    private static Godot.Color ToneColor(EventTone tone) => tone switch
    {
        EventTone.Bad => BarPalette.Warning,
        EventTone.Good => BarPalette.Success,
        _ => BarPalette.Text,
    };

    private sealed record SheetContext(WorldData World, GameSnapshot Snapshot, PlayerState Player, string TownId, DateTimeOffset Now);
}
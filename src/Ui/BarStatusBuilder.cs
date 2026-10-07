using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Pixel;
using IdleBar.Trade;

namespace IdleBar.Ui;

public static class BarStatusBuilder
{
    private const int ShownProducts = 12;
    private const string RainKind = "orage";
    private const string TarpFitting = "bachage";
    private const string IronWheelsFitting = "roues_cerclees";
    private const string MasterpieceIcon = "chef_oeuvre";

    private static readonly SlotContent SignInSlot =
        new("Compte", "Se connecter", BarPalette.Gold, "Connecte-toi ou crée un compte pour commencer.");

    private static readonly SlotContent ConnectingSlot =
        new("Compte", "Connexion…", BarPalette.Muted, "Connexion au serveur…");

    private static readonly SlotContent FoundingSlot =
        new("Métier", "S'installer", BarPalette.Gold, "Choisis ton métier et ta ville pour commencer.");

    private static readonly SlotContent OfflineSlot =
        new("Hors ligne", "Réessayer", BarPalette.Danger, "Serveur injoignable. Clique pour réessayer.");

    public static BarStatus Unavailable(string reason) =>
        Waiting(reason, new SlotContent("IdleBar", "Indisponible", BarPalette.Danger, reason), reason);

    public static BarStatus Describe(GameSession session, RecipeInfo? relaunch) => session.Status switch
    {
        SessionStatus.SignedOut => Waiting("Non connecté", SignInSlot, "Connecte-toi pour commencer"),
        SessionStatus.Connecting => Waiting("Connexion…", ConnectingSlot, string.Empty),
        SessionStatus.NeedsFounding => Waiting("Pas encore installé", FoundingSlot, "Choisis ton métier pour commencer"),
        SessionStatus.Offline when session.Player is null => Waiting("Hors ligne", OfflineSlot, "Serveur injoignable"),
        SessionStatus.Offline => DescribePlayer(session, null) with { Slot = OfflineSlot },
        _ => DescribePlayer(session, relaunch),
    };

    private static BarStatus Waiting(string situation, SlotContent slot, string caption) =>
        new("IdleBar", situation, $"IdleBar · {situation}", slot, LaneScene.Idle(caption));

    private static BarStatus DescribePlayer(GameSession session, RecipeInfo? relaunch)
    {
        PlayerState player = session.Player!;
        string coins = $"{NumberFormat.Coins(player.Coins)}";
        BarStatus status = session switch
        {
            { Caravan: CaravanState caravan } => DescribeCaravan(session, caravan, coins),
            _ when session.World?.FindCraft(player.CraftId)?.OpensBranches == true => CounterStatus.Describe(session, session.Workshop!, coins),
            _ => DescribeWorkshop(session, session.Workshop!, coins, relaunch),
        };
        SlotContent? news = NewsSlot.For(session.Snapshot!, session.World, session.Clock.Now);
        string compact = $"{player.Name} · {coins} · {status.Situation}";
        return status with { Compact = news is null ? compact : $"{compact} · {news.Title} {news.Detail}", News = news };
    }

    private static BarStatus DescribeCaravan(GameSession session, CaravanState caravan, string coins)
    {
        WorldData? world = session.World;
        DateTimeOffset now = session.Clock.Now;
        string town = world?.TownName(caravan.TownId) ?? caravan.TownId;

        if (caravan.IsTravelling(now))
        {
            GameSnapshot snapshot = session.Snapshot!;
            string arrival = DurationFormat.ClockTime(caravan.ArrivesAt!.Value);
            Biome roadBiome = world?.FindRoute(caravan.FromTownId ?? string.Empty, caravan.TownId)?.Biome ?? Biome.Plain;
            bool raining = snapshot.HadOnThisTrip(RainKind) || snapshot.TripEvent?.KindId == RainKind;
            string? hazard = snapshot.TripEvent is TripEventInfo tripEvent ? world?.FindEventKind(tripEvent.KindId)?.Name ?? tripEvent.KindId : null;
            string situation = hazard is null ? $"vers {town} · {DurationFormat.Span(caravan.Remaining(now))}" : $"arrêtée vers {town}";
            LaneScene scene = snapshot.TripEvent is TripEventInfo halt
                ? LaneScene.Halted(roadBiome, snapshot.TripProgress(now), raining, LookOf(snapshot), halt.KindId, $"{hazard} · réponds avant {DurationFormat.ClockTime(halt.DecideBy)}")
                : LaneScene.Travelling(roadBiome, caravan.Progress(now), raining, LookOf(snapshot));
            return new BarStatus(
                coins,
                situation,
                situation,
                new SlotContent("En route", $"arrivée {arrival}", BarPalette.Muted, $"Ta caravane arrive à {town} vers {arrival}."),
                scene);
        }

        string rank = MasteryText.RankName(session.Snapshot!.Mastery?.Rank ?? MasteryRank.Apprenti);
        string inTown = $"{rank} à {town} · cale {caravan.Load}/{caravan.Capacity}";
        return new BarStatus(
            coins,
            inTown,
            inTown,
            new SlotContent(town, "Ouvrir la ville", BarPalette.Success, $"Ta caravane est à {town}. Clique pour commercer ou repartir."),
            LaneScene.InTown(world?.FindTown(caravan.TownId)?.Biome ?? Biome.Plain, town, LookOf(session.Snapshot!)));
    }

    private static BarStatus DescribeWorkshop(GameSession session, WorkshopState workshop, string coins, RecipeInfo? relaunch)
    {
        WorldData? world = session.World;
        GameSnapshot snapshot = session.Snapshot!;
        DateTimeOffset now = session.Clock.Now;
        string town = world?.TownName(workshop.TownId) ?? workshop.TownId;
        string craft = MasteryText.Title(snapshot.Mastery?.Rank, world?.FindCraft(snapshot.Player!.CraftId)?.Name ?? snapshot.Player!.CraftId);
        string situation = $"{craft} à {town} · entrepôt {snapshot.HoldingsLoadAt(workshop.TownId)}/{snapshot.StorageCapacityAt(workshop.TownId)}";
        IReadOnlyList<string> products = Enumerable.Repeat(MasterpieceIcon, snapshot.Masterpieces.Count)
            .Concat(ListProducts(world, snapshot.StorageAt(workshop.TownId)))
            .Take(ShownProducts)
            .ToList();
        Biome biome = world?.FindTown(workshop.TownId)?.Biome ?? Biome.Plain;

        if (workshop.IsPaused(now))
        {
            string resume = DurationFormat.ClockTime(workshop.PausedUntil!.Value);
            return new BarStatus(
                coins,
                situation,
                situation,
                new SlotContent("Atelier", $"en réparation · {resume}", BarPalette.Warning, $"Une panne arrête ton atelier jusqu'à {resume}."),
                LaneScene.Workshop(biome, snapshot.Player.CraftId, false, 0, products, $"{town} · atelier en réparation"));
        }

        if (workshop.IsProducing && world?.FindRecipe(workshop.RecipeId!) is RecipeInfo recipe)
        {
            int units = workshop.RemainingBatches(now) * recipe.OutputQuantity;
            string lot = ExchangeText.Lot(world, recipe.OutputGoodId, units);
            string finish = DurationFormat.ClockTime(workshop.FinishesAt!.Value);
            return new BarStatus(
                coins,
                situation,
                situation,
                new SlotContent("Atelier", $"{lot} · {finish}", BarPalette.Success, $"Ton atelier fabrique encore {lot}, fini à {finish}."),
                LaneScene.Workshop(biome, snapshot.Player.CraftId, true, workshop.BatchProgress(now), products, town));
        }

        return new BarStatus(
            coins,
            situation,
            situation,
            DescribeIdleSlot(world, relaunch),
            LaneScene.Workshop(biome, snapshot.Player.CraftId, false, 0, products, $"{town} · atelier à l'arrêt"));
    }

    private static SlotContent DescribeIdleSlot(WorldData? world, RecipeInfo? relaunch)
    {
        if (world is null || relaunch is null)
        {
            return new SlotContent("Atelier", "À l'arrêt", BarPalette.Warning, "Ton atelier ne produit rien. Clique pour lancer une fabrication.");
        }

        string good = world.GoodNoun(relaunch.OutputGoodId, 2);
        return new SlotContent(
            "Atelier à l'arrêt",
            $"Relancer : {good}",
            BarPalette.Gold,
            $"Clique pour relancer {good} autant que tes matières premières le permettent. Clique sur le paysage pour ouvrir l'atelier.");
    }

    private static CaravanLook LookOf(GameSnapshot snapshot) =>
        new(snapshot.Fittings.Contains(TarpFitting), snapshot.Fittings.Contains(IronWheelsFitting));

    private static IReadOnlyList<string> ListProducts(WorldData? world, IReadOnlyList<StockLine> storage) =>
        storage
            .Where(line => world?.IsCrafted(line.GoodId) == true)
            .SelectMany(line => Enumerable.Repeat(line.GoodId, line.Quantity))
            .Take(ShownProducts)
            .ToList();
}

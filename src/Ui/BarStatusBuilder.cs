using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IdleBar.Trade;

namespace IdleBar.Ui;

public static class BarStatusBuilder
{
    private const int ShownProducts = 12;

    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

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

    public static BarStatus Describe(GameSession session) => session.Status switch
    {
        SessionStatus.SignedOut => Waiting("Non connecté", SignInSlot, "Connecte-toi pour commencer"),
        SessionStatus.Connecting => Waiting("Connexion…", ConnectingSlot, string.Empty),
        SessionStatus.NeedsFounding => Waiting("Pas encore installé", FoundingSlot, "Choisis ton métier pour commencer"),
        SessionStatus.Offline when session.Player is null => Waiting("Hors ligne", OfflineSlot, "Serveur injoignable"),
        SessionStatus.Offline => DescribePlayer(session) with { Slot = OfflineSlot },
        _ => DescribePlayer(session),
    };

    private static BarStatus Waiting(string situation, SlotContent slot, string caption) =>
        new("IdleBar", situation, $"IdleBar · {situation}", slot, LaneScene.Idle(caption));

    private static BarStatus DescribePlayer(GameSession session)
    {
        PlayerState player = session.Player!;
        string coins = $"{NumberFormat.Amount(player.Coins)} écus";
        BarStatus status = session.Caravan is CaravanState caravan
            ? DescribeCaravan(session, caravan, coins)
            : DescribeWorkshop(session, session.Workshop!, coins);
        return status with { Compact = $"{player.Name} · {coins} · {status.Situation}" };
    }

    private static BarStatus DescribeCaravan(GameSession session, CaravanState caravan, string coins)
    {
        WorldData? world = session.World;
        DateTimeOffset now = session.Clock.Now;
        string town = world?.TownName(caravan.TownId) ?? caravan.TownId;

        if (caravan.IsTravelling(now))
        {
            string arrival = DurationFormat.ClockTime(caravan.ArrivesAt!.Value);
            string situation = $"vers {town} · {DurationFormat.Span(caravan.Remaining(now))}";
            Biome roadBiome = world?.FindRoute(caravan.FromTownId ?? string.Empty, caravan.TownId)?.Biome ?? Biome.Plain;
            return new BarStatus(
                coins,
                situation,
                situation,
                new SlotContent("En route", $"arrivée {arrival}", BarPalette.Muted, $"Ta caravane arrive à {town} à {arrival}."),
                LaneScene.Travelling(roadBiome, caravan.Progress(now)));
        }

        string inTown = $"à {town} · cale {session.Snapshot!.HoldingsLoad}/{caravan.Capacity}";
        return new BarStatus(
            coins,
            inTown,
            inTown,
            new SlotContent(town, "Ouvrir la ville", BarPalette.Success, $"Ta caravane est à {town}. Clique pour commercer ou repartir."),
            LaneScene.InTown(world?.FindTown(caravan.TownId)?.Biome ?? Biome.Plain, town));
    }

    private static BarStatus DescribeWorkshop(GameSession session, WorkshopState workshop, string coins)
    {
        WorldData? world = session.World;
        GameSnapshot snapshot = session.Snapshot!;
        DateTimeOffset now = session.Clock.Now;
        string town = world?.TownName(workshop.TownId) ?? workshop.TownId;
        string craft = world?.FindCraft(snapshot.Player!.CraftId)?.Name ?? snapshot.Player!.CraftId;
        string situation = $"{craft} à {town} · entrepôt {snapshot.HoldingsLoad}/{workshop.StorageCapacity}";
        IReadOnlyList<string> products = ListProducts(world, snapshot.Storage);
        Biome biome = world?.FindTown(workshop.TownId)?.Biome ?? Biome.Plain;

        if (workshop.IsProducing && world?.FindRecipe(workshop.RecipeId!) is RecipeInfo recipe)
        {
            int units = workshop.RemainingBatches(now) * recipe.OutputQuantity;
            string good = world.GoodName(recipe.OutputGoodId).ToLower(French);
            string finish = DurationFormat.ClockTime(workshop.FinishesAt!.Value);
            return new BarStatus(
                coins,
                situation,
                situation,
                new SlotContent("Atelier", $"{units} {good} · {finish}", BarPalette.Success, $"Ton atelier fabrique encore {units} {good}, fini à {finish}."),
                LaneScene.Workshop(biome, snapshot.Player.CraftId, true, workshop.BatchProgress(now), products, town));
        }

        return new BarStatus(
            coins,
            situation,
            situation,
            new SlotContent("Atelier", "À l'arrêt", BarPalette.Warning, "Ton atelier ne produit rien. Clique pour lancer une fabrication."),
            LaneScene.Workshop(biome, snapshot.Player.CraftId, false, 0, products, $"{town} · atelier à l'arrêt"));
    }

    private static IReadOnlyList<string> ListProducts(WorldData? world, IReadOnlyList<StockLine> storage) =>
        storage
            .Where(line => world?.IsCrafted(line.GoodId) == true)
            .SelectMany(line => Enumerable.Repeat(line.GoodId, line.Quantity))
            .Take(ShownProducts)
            .ToList();
}

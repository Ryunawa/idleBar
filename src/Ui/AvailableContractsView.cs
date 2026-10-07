using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class AvailableContractsView : VBoxContainer, ITownPanel
{
    private const string Help = "Charge la marchandise ici en déposant une caution, puis livre-la avant la fin du délai : tu gagnes la récompense et récupères ta caution. En retard, la caution revient à l'expéditeur et tu gardes la marchandise.";

    private VBoxContainer _list = null!;

    public event Action<TownCommand>? Requested;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 6);
        AddChild(ActionRow.Note(Help));
        _list = ContractRow.AddScrollList(this);
    }

    public void Refresh(TownContext context)
    {
        ActionRow.Clear(_list);
        ContractInfo[] available = context.Snapshot.Contracts
            .OrderByDescending(contract => contract.OriginTownId == context.TownId)
            .ThenBy(contract => contract.TakeoverAt)
            .ToArray();
        if (available.Length == 0)
        {
            _list.AddChild(ActionRow.Note("Aucun marchand ne cherche de caravane pour l'instant."));
            return;
        }

        foreach (ContractInfo contract in available)
        {
            string? blocker = FindBlocker(context, contract);
            ContractRowContent content = new(
                contract.GoodId,
                ExchangeText.Journey(context.World, contract),
                ListFacts(context, contract),
                blocker ?? $"Proposé par {contract.Shipper ?? "un marchand"}.",
                blocker is null ? BarPalette.Muted : BarPalette.Warning,
                null);
            long contractId = contract.Id;
            _list.AddChild(ContractRow.Create(content, "Accepter", blocker is null,
                () => Requested?.Invoke(actions => actions.AcceptContractAsync(contractId))));
        }
    }

    private static IReadOnlyList<string> ListFacts(TownContext context, ContractInfo contract)
    {
        WorldData world = context.World;
        TimeSpan road = TimeSpan.FromSeconds(world.FindJourney(contract.OriginTownId, contract.DestinationTownId)?.Seconds ?? 0);
        TimeSpan allowed = road + TimeSpan.FromSeconds(world.Rules.ContractSlackSeconds);
        return
        [
            $"Gain +{NumberFormat.Coins(contract.Reward)}",
            $"Caution {NumberFormat.Coins(contract.Deposit)}",
            $"{DurationFormat.Span(road)} de route",
            $"Délai {DurationFormat.Span(allowed)}",
            $"Libre jusqu'à {DurationFormat.Moment(contract.TakeoverAt, context.Clock.Now)}",
        ];
    }

    private static string? FindBlocker(TownContext context, ContractInfo contract)
    {
        GameSnapshot snapshot = context.Snapshot;
        int free = snapshot.HoldingsCapacity - snapshot.HoldingsLoadAt(context.TownId);
        if (contract.OriginTownId != context.TownId)
        {
            return $"À charger à {context.World.TownName(contract.OriginTownId)} : va d'abord là-bas.";
        }

        if (free < contract.Quantity)
        {
            return $"Ta cale n'a pas la place : il faut {NumberFormat.Count(contract.Quantity, "place", "places")}, il en reste {NumberFormat.Amount(Math.Max(free, 0))}.";
        }

        if (context.Player.Coins < contract.Deposit)
        {
            return $"Il te faut {NumberFormat.Coins(contract.Deposit)} pour la caution, tu en as {NumberFormat.Amount(context.Player.Coins)}.";
        }

        return null;
    }
}

using System;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class CarriedContractsView : VBoxContainer
{
    private const string Help = "Ces marchandises sont dans ta cale. Arrive à destination avant la fin du délai : la livraison se fait toute seule à l'arrivée.";

    private VBoxContainer _list = null!;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 6);
        AddChild(ActionRow.Note(Help));
        _list = ContractRow.AddScrollList(this);
    }

    public void Refresh(TownContext context)
    {
        ActionRow.Clear(_list);
        DateTimeOffset now = context.Clock.Now;
        ContractInfo[] carried = context.Snapshot.MyContracts.Where(contract => contract.Role == ContractRole.Carrier).ToArray();
        ContractInfo[] underway = carried.Where(contract => contract.IsUnderway).ToArray();
        if (underway.Length == 0)
        {
            _list.AddChild(ActionRow.Note("Tu ne transportes rien pour l'instant : choisis un contrat dans « À prendre »."));
        }

        foreach (ContractInfo contract in underway)
        {
            _list.AddChild(ContractRow.Create(DescribeUnderway(context, contract, now)));
        }

        ContractInfo[] finished = carried.Where(contract => !contract.IsUnderway).ToArray();
        if (finished.Length > 0)
        {
            _list.AddChild(ActionRow.Heading("Terminés ces dernières 48 h"));
        }

        foreach (ContractInfo contract in finished)
        {
            Color color = contract.Status == ContractStatus.Delivered ? BarPalette.Success : BarPalette.Warning;
            _list.AddChild(ContractRow.Create(new ContractRowContent(
                contract.GoodId, ExchangeText.Journey(context.World, contract), [], ExchangeText.ContractState(contract, now), color, null)));
        }
    }

    private static ContractRowContent DescribeUnderway(TownContext context, ContractInfo contract, DateTimeOffset now)
    {
        WorldData world = context.World;
        DateTimeOffset start = contract.CarriedAt ?? now;
        DateTimeOffset deadline = contract.Deadline ?? now;
        double total = (deadline - start).TotalSeconds;
        double used = total <= 0 ? 1 : Math.Clamp((now - start).TotalSeconds / total, 0, 1);
        TimeSpan remaining = deadline > now ? deadline - now : TimeSpan.Zero;
        TimeSpan road = TimeSpan.FromSeconds(world.FindJourney(context.TownId, contract.DestinationTownId)?.Seconds ?? 0);
        string destination = world.TownName(contract.DestinationTownId);
        bool late = road > remaining;
        string note = late
            ? $"Trop tard pour {destination} : il faut {DurationFormat.Span(road)} de route et il ne reste que {DurationFormat.Span(remaining)}. La caution ira à l'expéditeur."
            : $"Pars vers {destination} : {DurationFormat.Span(road)} de route, il te reste {DurationFormat.Span(remaining)}.";
        return new ContractRowContent(
            contract.GoodId,
            ExchangeText.Journey(world, contract),
            [
                $"Gain +{NumberFormat.Coins(contract.Reward)}",
                $"Caution {NumberFormat.Coins(contract.Deposit)} rendue à la livraison",
                $"Avant {DurationFormat.Moment(deadline, now)}",
            ],
            note,
            late ? BarPalette.Warning : BarPalette.Muted,
            used);
    }
}

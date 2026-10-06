using System;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class ContractsPanel : VBoxContainer, ITownPanel
{
    private VBoxContainer _list = null!;
    private ContractForm _form = null!;
    private string _townId = string.Empty;

    public event Action<TownCommand>? Requested;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);
        ScrollContainer scroll = new() { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_list);
        AddChild(scroll);

        AddChild(new HSeparator());
        _form = new ContractForm();
        _form.Submitted += draft => Requested?.Invoke(actions => actions.PostContractAsync(draft, _townId));
        AddChild(_form);
    }

    public void Refresh(TownContext context)
    {
        _townId = context.TownId;
        _form.Refresh(context);
        ActionRow.Clear(_list);
        if (context.Itinerant)
        {
            AddAvailable(context);
        }

        _list.AddChild(ActionRow.Heading("Mes contrats"));
        if (context.Snapshot.MyContracts.Count == 0)
        {
            _list.AddChild(ActionRow.Note("Aucun contrat en cours."));
        }

        DateTimeOffset now = context.Clock.Now;
        foreach (ContractInfo contract in context.Snapshot.MyContracts)
        {
            long contractId = contract.Id;
            bool cancellable = contract is { Status: ContractStatus.Open, Role: ContractRole.Shipper };
            _list.AddChild(ActionRow.Create(ExchangeText.Journey(context.World, contract), ExchangeText.ContractState(contract, now), cancellable ? "Annuler" : string.Empty, false,
                () => Requested?.Invoke(actions => actions.CancelContractAsync(contractId))));
        }
    }

    private void AddAvailable(TownContext context)
    {
        WorldData world = context.World;
        _list.AddChild(ActionRow.Heading("Contrats à prendre"));
        ContractInfo[] available = context.Snapshot.Contracts.OrderByDescending(contract => contract.OriginTownId == context.TownId).ToArray();
        if (available.Length == 0)
        {
            _list.AddChild(ActionRow.Note("Aucun marchand ne cherche de caravane pour l'instant."));
        }

        foreach (ContractInfo contract in available)
        {
            bool here = contract.OriginTownId == context.TownId;
            int seconds = world.FindJourney(contract.OriginTownId, contract.DestinationTownId)?.Seconds ?? 0;
            string detail = $"{contract.Shipper} · {NumberFormat.Amount(contract.Reward)} écus · caution {NumberFormat.Amount(contract.Deposit)} · "
                + $"{DurationFormat.Span(TimeSpan.FromSeconds(seconds))} de route"
                + (here ? string.Empty : $" · à charger à {world.TownName(contract.OriginTownId)}");
            long contractId = contract.Id;
            _list.AddChild(ActionRow.Create(ExchangeText.Journey(world, contract), detail, "Accepter", !here || context.Player.Coins < contract.Deposit,
                () => Requested?.Invoke(actions => actions.AcceptContractAsync(contractId))));
        }
    }
}

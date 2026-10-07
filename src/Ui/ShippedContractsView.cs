using System;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class ShippedContractsView : VBoxContainer, ITownPanel
{
    private const string Help = "Envoie des marchandises de ton entrepôt vers une autre ville. Un caravanier peut les transporter contre ta récompense ; si personne ne les prend à temps, le transporteur du jeu s'en charge, plus lentement.";

    private ContractForm _form = null!;
    private VBoxContainer _list = null!;
    private string _townId = string.Empty;

    public event Action<TownCommand>? Requested;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 6);
        AddChild(ActionRow.Note(Help));
        _form = new ContractForm();
        _form.Submitted += draft => Requested?.Invoke(actions => actions.PostContractAsync(draft, _townId));
        AddChild(_form);
        AddChild(new HSeparator());
        AddChild(ActionRow.Heading("Mes envois"));
        _list = ContractRow.AddScrollList(this);
    }

    public void Refresh(TownContext context)
    {
        _townId = context.TownId;
        _form.Refresh(context);
        ActionRow.Clear(_list);
        DateTimeOffset now = context.Clock.Now;
        ContractInfo[] shipped = context.Snapshot.MyContracts
            .Where(contract => contract.Role == ContractRole.Shipper)
            .OrderByDescending(contract => contract.IsUnderway)
            .ToArray();
        if (shipped.Length == 0)
        {
            _list.AddChild(ActionRow.Note("Aucun envoi pour l'instant."));
        }

        foreach (ContractInfo contract in shipped)
        {
            ContractRowContent content = new(
                contract.GoodId,
                ExchangeText.Journey(context.World, contract),
                [$"Récompense {NumberFormat.Coins(contract.Reward)}", $"Caution demandée {NumberFormat.Coins(contract.Deposit)}"],
                ExchangeText.ContractState(contract, now),
                ColorOf(contract.Status),
                null);
            long contractId = contract.Id;
            _list.AddChild(contract.Status == ContractStatus.Open
                ? ContractRow.Create(content, "Annuler", true, () => Requested?.Invoke(actions => actions.CancelContractAsync(contractId)))
                : ContractRow.Create(content));
        }
    }

    private static Color ColorOf(ContractStatus status) => status switch
    {
        ContractStatus.Delivered => BarPalette.Success,
        ContractStatus.Failed => BarPalette.Gold,
        _ => BarPalette.Muted,
    };
}

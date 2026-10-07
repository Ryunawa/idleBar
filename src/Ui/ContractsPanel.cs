using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class ContractsPanel : VBoxContainer, ITownPanel
{
    private readonly AvailableContractsView _available = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
    private readonly CarriedContractsView _carried = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
    private readonly ShippedContractsView _shipped = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
    private readonly SupplyView _supply = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
    private readonly Dictionary<ContractView, Button> _buttons = [];
    private readonly ButtonGroup _group = new();
    private HBoxContainer _bar = null!;
    private ContractView? _chosen;

    public event Action<TownCommand>? Requested;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);
        _bar = new HBoxContainer();
        _bar.AddThemeConstantOverride("separation", 6);
        AddChild(_bar);
        foreach (ContractView view in Enum.GetValues<ContractView>())
        {
            Button button = new() { ToggleMode = true, ButtonGroup = _group, FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            button.AddThemeColorOverride("font_pressed_color", BarPalette.Gold);
            button.Pressed += () => Show(view);
            _bar.AddChild(button);
            _buttons[view] = button;
        }

        _available.Requested += command => Requested?.Invoke(command);
        _shipped.Requested += command => Requested?.Invoke(command);
        _supply.Requested += command => Requested?.Invoke(command);
        AddChild(_available);
        AddChild(_carried);
        AddChild(_shipped);
        AddChild(_supply);
    }

    public void Refresh(TownContext context)
    {
        IReadOnlyList<ContractInfo> mine = context.Snapshot.MyContracts;
        _buttons[ContractView.Available].Text = $"À prendre · {context.Snapshot.Contracts.Count}";
        _buttons[ContractView.Carried].Text = $"Je transporte · {mine.Count(contract => contract is { Role: ContractRole.Carrier, IsUnderway: true })}";
        _buttons[ContractView.Shipped].Text = $"J'expédie · {mine.Count(contract => contract is { Role: ContractRole.Shipper, IsUnderway: true })}";
        _buttons[ContractView.Supply].Text = context.Itinerant
            ? $"Commandes · {context.Snapshot.Supply.Count}"
            : $"Mes commandes · {context.Snapshot.MyOffers.Count(offer => SupplyView.IsOrder(offer) && offer.Status == OfferStatus.Open)}";
        ContractView[] views = ViewsFor(context);
        foreach ((ContractView view, Button button) in _buttons)
        {
            button.Visible = views.Contains(view);
        }

        _bar.Visible = views.Length > 1;
        _available.Refresh(context);
        _carried.Refresh(context);
        _shipped.Refresh(context);
        _supply.Refresh(context);
        Show(_chosen is ContractView chosen && views.Contains(chosen) ? chosen : views[0]);
    }

    private static ContractView[] ViewsFor(TownContext context)
    {
        if (context.Itinerant)
        {
            return [ContractView.Available, ContractView.Carried, ContractView.Shipped, ContractView.Supply];
        }

        return context.World.FindCraft(context.Player.CraftId)?.OpensBranches == true
            ? [ContractView.Shipped, ContractView.Supply]
            : [ContractView.Supply];
    }

    private void Show(ContractView view)
    {
        _chosen = view;

        foreach ((ContractView each, Button button) in _buttons)
        {
            button.SetPressedNoSignal(each == view);
        }

        _available.Visible = view == ContractView.Available;
        _carried.Visible = view == ContractView.Carried;
        _shipped.Visible = view == ContractView.Shipped;
        _supply.Visible = view == ContractView.Supply;
    }
}

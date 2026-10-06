using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class WarehousePanel : VBoxContainer, ITownPanel
{
    private const int MaxQuantity = 1000;

    private static readonly int[] Quantities = [1, 10, MaxQuantity];

    private Label _summary = null!;
    private HBoxContainer _picker = null!;
    private VBoxContainer _list = null!;
    private Label _elsewhere = null!;
    private int _quantity = 1;

    public event Action<TownCommand>? Requested;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);
        _summary = BarLabels.Create(14, BarPalette.Text);
        AddChild(_summary);
        _picker = BatchPicker.Create("Quantité par clic :", Quantities, MaxQuantity, quantity => _quantity = quantity);
        AddChild(_picker);

        ScrollContainer scroll = new() { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_list);
        AddChild(scroll);

        _elsewhere = ActionRow.Note(string.Empty);
        AddChild(_elsewhere);
    }

    public void Refresh(TownContext context)
    {
        WorldData world = context.World;
        IReadOnlyList<StockLine> storage = context.Storage;
        int capacity = context.Itinerant ? world.Rules.DepotCapacity : context.Snapshot.StorageCapacity;
        _summary.Text = $"Entrepôt à {context.TownName} · {NumberFormat.Amount(storage.Sum(line => line.Quantity))}/{NumberFormat.Amount(capacity)}";
        _picker.Visible = context.Itinerant;
        ActionRow.Clear(_list);

        if (context.Itinerant)
        {
            AddCaravanRows(context, storage);
        }
        else
        {
            foreach (StockLine line in storage)
            {
                _list.AddChild(ActionRow.Create(world.GoodName(line.GoodId), $"{NumberFormat.Amount(line.Quantity)} en stock", string.Empty, true, () => { }));
            }
        }

        if (_list.GetChildCount() == 0)
        {
            _list.AddChild(ActionRow.Note("Rien n'est entreposé ici."));
        }

        string[] towns = context.Snapshot.Warehouses.Select(line => line.TownId).Where(townId => townId != context.TownId).Distinct().ToArray();
        _elsewhere.Text = towns.Length == 0
            ? "Rien d'entreposé dans d'autres villes."
            : "Ailleurs : " + string.Join(" · ", towns.Select(townId => $"{world.TownName(townId)} ({Describe(world, context.Snapshot.StorageAt(townId))})"));
    }

    private static string Describe(WorldData world, IReadOnlyList<StockLine> lines) =>
        string.Join(", ", lines.Select(line => ExchangeText.Lot(world, line.GoodId, line.Quantity)));

    private void AddCaravanRows(TownContext context, IReadOnlyList<StockLine> storage)
    {
        WorldData world = context.World;
        IEnumerable<string> goodIds = storage.Select(line => line.GoodId).Union(context.Snapshot.Cargo.Select(line => line.GoodId));
        foreach (string goodId in world.Goods.Select(good => good.Id).Where(goodIds.Contains))
        {
            int stored = storage.FirstOrDefault(line => line.GoodId == goodId)?.Quantity ?? 0;
            int carried = context.Owned(goodId);
            HBoxContainer row = ActionRow.Create(world.GoodName(goodId), $"entrepôt {NumberFormat.Amount(stored)} · cale {NumberFormat.Amount(carried)}", "Déposer", carried == 0,
                () => Requested?.Invoke(actions => actions.DepositAsync(goodId, _quantity)));
            Button withdraw = new() { Text = "Retirer", Disabled = stored == 0, FocusMode = FocusModeEnum.None, SizeFlagsVertical = SizeFlags.ShrinkCenter };
            withdraw.Pressed += () => Requested?.Invoke(actions => actions.WithdrawAsync(goodId, _quantity));
            row.AddChild(withdraw);
            _list.AddChild(row);
        }
    }
}

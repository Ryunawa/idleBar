using System;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class CaravanPanel : VBoxContainer, ITownPanel
{
    private const string WagonGood = "chariot";

    private Label _summary = null!;
    private Label _cargo = null!;
    private Button _wagon = null!;
    private Button _attach = null!;
    private VBoxContainer _fittings = null!;

    public event Action<TownCommand>? Requested;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);

        _summary = BarLabels.Create(14, BarPalette.Text);
        AddChild(_summary);

        _cargo = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _cargo.AddThemeColorOverride("font_color", BarPalette.Muted);
        AddChild(_cargo);

        HBoxContainer wagons = new();
        wagons.AddThemeConstantOverride("separation", 8);
        _wagon = new Button { FocusMode = FocusModeEnum.None };
        _wagon.Pressed += () => Requested?.Invoke(actions => actions.BuyWagonAsync());
        _attach = new Button { FocusMode = FocusModeEnum.None };
        _attach.Pressed += () => Requested?.Invoke(actions => actions.AttachWagonAsync());
        wagons.AddChild(_wagon);
        wagons.AddChild(_attach);
        AddChild(wagons);
        AddChild(ActionRow.Note("Chaque chariot ajoute 20 places dans la cale. Un chariot du charron s'attelle sans payer le prix fort de la ville : il est pris d'abord dans l'entrepôt de la ville, puis dans la cale."));

        AddChild(ActionRow.Heading("Équipements fabriqués par les artisans"));
        ScrollContainer scroll = new() { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _fittings = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _fittings.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_fittings);
        AddChild(scroll);
    }

    public void Refresh(TownContext context)
    {
        GameSnapshot snapshot = context.Snapshot;
        CaravanState caravan = snapshot.Caravan!;
        PlayerState player = context.Player;
        int carried = snapshot.MyContracts.Where(contract => contract is { Role: ContractRole.Carrier, Status: ContractStatus.Carried }).Sum(contract => contract.Quantity);
        string wagons = caravan.Wagons > 1 ? $"{caravan.Wagons} chariots" : "1 chariot";
        _summary.Text = $"Caravane de {player.Name} · {wagons} · cale {caravan.Load}/{caravan.Capacity}";
        string cargo = snapshot.Cargo.Count == 0
            ? "La cale est vide."
            : "Cargaison : " + string.Join(", ", snapshot.Cargo.Select(line => $"{context.World.GoodName(line.GoodId)} × {NumberFormat.Amount(line.Quantity)}"));
        _cargo.Text = carried > 0 ? $"{cargo} Contrats : {NumberFormat.Count(carried, "place", "places")}." : cargo;

        bool full = caravan.NextWagonPrice is not int;
        _wagon.Text = caravan.NextWagonPrice is int price ? $"Acheter un chariot · {NumberFormat.Coins(price)}" : "Nombre maximal de chariots atteint";
        _wagon.Disabled = caravan.NextWagonPrice is not int cost || player.Coins < cost;
        int loaded = context.Owned(WagonGood);
        int stored = context.Storage.FirstOrDefault(line => line.GoodId == WagonGood)?.Quantity ?? 0;
        _attach.Text = $"Atteler un chariot du charron · {NumberFormat.Amount(loaded)} en cale, {NumberFormat.Amount(stored)} à l'entrepôt";
        _attach.Disabled = full || loaded + stored == 0;

        ActionRow.Clear(_fittings);
        foreach (FittingInfo fitting in context.World.Fittings)
        {
            _fittings.AddChild(FittingRows.Create(context, fitting, command => Requested?.Invoke(command)));
        }
    }
}

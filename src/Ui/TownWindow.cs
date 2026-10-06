using System;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class TownWindow : Window
{
    private const int WorkshopTab = 0;
    private const int MarketTab = 1;
    private const int RoutesTab = 2;
    private const int CaravanTab = 3;

    private static readonly Vector2I BaseSize = new(640, 500);

    private Label _town = null!;
    private Label _purse = null!;
    private TabContainer _tabs = null!;
    private WorkshopPanel _workshop = null!;
    private MarketPanel _market = null!;
    private RoutesPanel _routes = null!;
    private CaravanPanel _caravan = null!;
    private Label _message = null!;
    private Control _blocker = null!;

    public event Action<string, int>? BuyRequested;

    public event Action<string, int>? SellRequested;

    public event Action<string>? DepartRequested;

    public event Action? WagonRequested;

    public event Action<string, int>? ProductionRequested;

    public event Action? WorkshopUpgradeRequested;

    public override void _Ready()
    {
        VBoxContainer content = WindowFrame.Build(this, "IdleBar · Ville", 8);

        HBoxContainer header = new();
        _town = BarLabels.Create(18, BarPalette.Gold);
        _town.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _purse = BarLabels.Create(13, BarPalette.Text, HorizontalAlignment.Right);
        header.AddChild(_town);
        header.AddChild(_purse);
        content.AddChild(header);

        _tabs = new TabContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _workshop = new WorkshopPanel { Name = "Atelier" };
        _market = new MarketPanel { Name = "Marché" };
        _routes = new RoutesPanel { Name = "Routes" };
        _caravan = new CaravanPanel { Name = "Caravane" };
        _tabs.AddChild(_workshop);
        _tabs.AddChild(_market);
        _tabs.AddChild(_routes);
        _tabs.AddChild(_caravan);
        content.AddChild(_tabs);

        _workshop.ProductionRequested += (recipeId, batches) => ProductionRequested?.Invoke(recipeId, batches);
        _workshop.UpgradeRequested += () => WorkshopUpgradeRequested?.Invoke();
        _market.BuyRequested += (goodId, quantity) => BuyRequested?.Invoke(goodId, quantity);
        _market.SellRequested += (goodId, quantity) => SellRequested?.Invoke(goodId, quantity);
        _routes.DepartRequested += destinationId => DepartRequested?.Invoke(destinationId);
        _caravan.WagonRequested += () => WagonRequested?.Invoke();

        _message = WindowFrame.CreateMessage();
        content.AddChild(_message);

        _blocker = new Control { MouseFilter = Control.MouseFilterEnum.Stop, Visible = false };
        AddChild(_blocker);
        _blocker.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    public void Open(float scale, bool itinerant)
    {
        _message.Text = string.Empty;
        SetBusy(false);
        _tabs.SetTabHidden(WorkshopTab, itinerant);
        _tabs.SetTabHidden(RoutesTab, !itinerant);
        _tabs.SetTabHidden(CaravanTab, !itinerant);
        _tabs.CurrentTab = itinerant ? MarketTab : WorkshopTab;
        WindowFrame.Present(this, BaseSize, scale);
    }

    public void Refresh(WorldData world, GameSnapshot snapshot, ServerClock clock)
    {
        PlayerState player = snapshot.Player!;
        string townId = snapshot.Caravan?.TownId ?? snapshot.Workshop!.TownId;
        string town = world.TownName(townId);
        string storage = snapshot.Caravan is null ? "entrepôt" : "cale";
        Title = $"IdleBar · {town}";
        _town.Text = snapshot.Caravan is null ? $"Atelier de {player.Name} · {town}" : town;
        _purse.Text = $"{NumberFormat.Amount(player.Coins)} écus · {storage} {snapshot.HoldingsLoad}/{snapshot.HoldingsCapacity}";
        _market.Refresh(world, snapshot);

        if (snapshot.Caravan is null)
        {
            _workshop.Refresh(world, snapshot, clock);
            return;
        }

        _routes.Refresh(world, townId);
        _caravan.Refresh(world, snapshot);
    }

    public void ShowError(string message) => _message.Text = message;

    public void SetBusy(bool busy)
    {
        _blocker.Visible = busy;
        if (busy)
        {
            _message.Text = string.Empty;
        }
    }
}

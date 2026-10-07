using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class TownWindow : Window
{
    private static readonly Vector2I BaseSize = new(680, 540);

    private readonly List<string> _townIds = [];
    private Label _town = null!;
    private OptionButton _townPicker = null!;
    private Label _purse = null!;
    private Label _standing = null!;
    private TabContainer _tabs = null!;
    private ITownPanel[] _panels = [];
    private Label _message = null!;
    private Control _blocker = null!;
    private TownAccess _access = new(false, false, false, false);
    private TownContext? _context;

    public event Action<TownCommand>? Requested;

    public event Action<TownTab>? TabViewed;

    public TownTab CurrentTab => (TownTab)_tabs.CurrentTab;

    public override void _Ready()
    {
        VBoxContainer content = WindowFrame.Build(this, "IdleBar · Ville", 8);

        HBoxContainer header = new();
        header.AddThemeConstantOverride("separation", 10);
        _town = BarLabels.Create(18, BarPalette.Gold);
        _town.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        _townPicker = new OptionButton { FocusMode = Control.FocusModeEnum.None, Visible = false };
        _townPicker.ItemSelected += index => ChooseTown(_townIds[(int)index]);
        _purse = BarLabels.Create(13, BarPalette.Text, HorizontalAlignment.Right);
        _purse.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(_town);
        header.AddChild(_townPicker);
        header.AddChild(_purse);
        content.AddChild(header);
        _standing = BarLabels.Create(12, BarPalette.Muted);
        content.AddChild(_standing);

        _tabs = new TabContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        AddPanel(new WorkshopPanel { Name = "Atelier" });
        AddPanel(new MarketPanel { Name = "Marché" });
        AddPanel(new CounterPanel { Name = "Comptoir" });
        AddPanel(new WarehousePanel { Name = "Entrepôt" });
        AddPanel(new ContractsPanel { Name = "Contrats" });
        AddPanel(new RoutesPanel { Name = "Routes" });
        AddPanel(new CaravanPanel { Name = "Caravane" });
        AddPanel(new BranchesPanel { Name = "Succursales" });
        AddPanel(new JournalPanel { Name = "Journal" });
        AddPanel(new OrdersPanel { Name = "Consignes" });
        AddPanel(new MasteryPanel { Name = "Maîtrise" });
        _tabs.TabChanged += _ => ReportTabViewed();
        content.AddChild(_tabs);

        _message = WindowFrame.CreateMessage();
        content.AddChild(_message);

        _blocker = new Control { MouseFilter = Control.MouseFilterEnum.Stop, Visible = false };
        AddChild(_blocker);
        _blocker.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    public void Open(float scale, TownAccess access, TownTab? tab)
    {
        _message.Text = string.Empty;
        SetBusy(false);
        ApplyAccess(access);
        TownTab chosen = tab is TownTab wanted && access.Shows(wanted) ? wanted : access.DefaultTab;
        _tabs.CurrentTab = (int)chosen;
        WindowFrame.Present(this, BaseSize, scale);
        ReportTabViewed();
    }

    public void Refresh(WorldData world, GameSnapshot snapshot, ServerClock clock)
    {
        ApplyAccess(TownAccess.For(world, snapshot, clock.Now));
        IReadOnlyList<string> present = snapshot.PresentTownsAt(clock.Now);
        string townId = _context is { } previous && present.Contains(previous.TownId) ? previous.TownId : present.FirstOrDefault() ?? snapshot.DefaultTownId!;
        RefreshTownPicker(world, present, townId);
        Display(new TownContext(world, snapshot, clock, townId));
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

    private void ApplyAccess(TownAccess access)
    {
        if (access == _access && Visible)
        {
            return;
        }

        _access = access;
        foreach (TownTab each in Enum.GetValues<TownTab>())
        {
            _tabs.SetTabHidden((int)each, !access.Shows(each));
        }

        if (!access.Shows(CurrentTab))
        {
            _tabs.CurrentTab = (int)access.DefaultTab;
        }
    }

    private void AddPanel<TPanel>(TPanel panel)
        where TPanel : Control, ITownPanel
    {
        panel.Requested += command => Requested?.Invoke(command);
        _tabs.AddChild(panel);
        _panels = [.. _panels, panel];
    }

    private void RefreshTownPicker(WorldData world, IReadOnlyList<string> present, string townId)
    {
        _townPicker.Visible = present.Count > 1;
        _town.Visible = !_townPicker.Visible;
        if (!_townIds.SequenceEqual(present))
        {
            _townIds.Clear();
            _townIds.AddRange(present);
            _townPicker.Clear();
            foreach (string id in present)
            {
                _townPicker.AddItem(world.TownName(id));
            }
        }

        _townPicker.Select(_townIds.IndexOf(townId));
    }

    private void ChooseTown(string townId)
    {
        if (_context is { } context)
        {
            _message.Text = string.Empty;
            Display(context with { TownId = townId });
        }
    }

    private void Display(TownContext context)
    {
        _context = context;
        PlayerState player = context.Player;
        GameSnapshot snapshot = context.Snapshot;
        string storage = context.Itinerant ? "cale" : "entrepôt";
        Title = $"IdleBar · {context.TownName}";
        _town.Text = _access switch
        {
            { Travelling: true } => $"En route vers {context.TownName}",
            { Itinerant: true } => context.TownName,
            { Branches: true } => $"Comptoir de {player.Name} · {context.TownName}",
            _ => $"Atelier de {player.Name} · {context.TownName}",
        };
        _purse.Text = $"{NumberFormat.Coins(player.Coins)} · {storage} {snapshot.HoldingsLoadAt(context.TownId)}/{snapshot.HoldingsCapacityAt(context.TownId)}";
        _standing.Visible = snapshot.Standing.Tiers.Count > 0;
        _standing.Text = $"Réputation à {context.TownName} : {ReputationText.Standing(snapshot.Standing, context.TownId)}";
        foreach (TownTab tab in Enum.GetValues<TownTab>().Where(_access.Shows))
        {
            _panels[(int)tab].Refresh(context);
        }
    }

    private void ReportTabViewed()
    {
        if (Visible)
        {
            TabViewed?.Invoke(CurrentTab);
        }
    }
}

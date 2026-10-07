using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Pixel;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class TownWindow : Window
{
    private static readonly Vector2I BaseSize = new(624, 374);

    private readonly List<string> _townIds = [];
    private readonly Dictionary<string, Theme> _skins = [];
    private TownBanner _banner = null!;
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

        _banner = new TownBanner();
        content.AddChild(_banner);
        _banner.Picker.ItemSelected += index => ChooseTown(_townIds[(int)index]);

        _tabs = new TabContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, ClipTabs = true };
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
        SetMessage(string.Empty);
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

    public void ShowError(string message) => SetMessage(message);

    public void SetBusy(bool busy)
    {
        _blocker.Visible = busy;
        if (busy)
        {
            SetMessage(string.Empty);
        }
    }

    private void SetMessage(string message)
    {
        _message.Text = message;
        _message.Visible = message.Length > 0;
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
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        ScrollContainer scroll = new() { Name = panel.Name, FollowFocus = true };
        scroll.AddChild(panel);
        _tabs.AddChild(scroll);
        _panels = [.. _panels, panel];
    }

    private void RefreshTownPicker(WorldData world, IReadOnlyList<string> present, string townId)
    {
        _banner.Picker.Visible = present.Count > 1;
        _banner.Heading.Visible = !_banner.Picker.Visible;
        if (!_townIds.SequenceEqual(present))
        {
            _townIds.Clear();
            _townIds.AddRange(present);
            _banner.Picker.Clear();
            foreach (string id in present)
            {
                _banner.Picker.AddItem(world.TownName(id));
            }
        }

        _banner.Picker.Select(_townIds.IndexOf(townId));
    }

    private void ChooseTown(string townId)
    {
        if (_context is { } context)
        {
            SetMessage(string.Empty);
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
        Theme = SkinFor(context.TownId);
        _banner.SetTown(context.World, context.TownId);
        _banner.Heading.Text = _access switch
        {
            { Travelling: true } => $"En route vers {context.TownName}",
            { Itinerant: true } => context.TownName,
            { Branches: true } => $"Comptoir de {player.Name} · {context.TownName}",
            _ => $"Atelier de {player.Name} · {context.TownName}",
        };
        _banner.Purse.Text = $"{NumberFormat.Coins(player.Coins)} · {storage} {snapshot.HoldingsLoadAt(context.TownId)}/{snapshot.HoldingsCapacityAt(context.TownId)}";
        _banner.Standing.Visible = snapshot.Standing.Tiers.Count > 0;
        _banner.Standing.Text = $"Réputation à {context.TownName} : {ReputationText.Standing(snapshot.Standing, context.TownId)}";
        foreach (TownTab tab in Enum.GetValues<TownTab>().Where(_access.Shows))
        {
            _panels[(int)tab].Refresh(context);
        }
    }

    private Theme SkinFor(string townId)
    {
        if (!_skins.TryGetValue(townId, out Theme? skin))
        {
            skin = WindowSkin.Create(WindowStyles.For(townId));
            _skins[townId] = skin;
        }

        return skin;
    }

    private void ReportTabViewed()
    {
        if (Visible)
        {
            TabViewed?.Invoke(CurrentTab);
        }
    }
}

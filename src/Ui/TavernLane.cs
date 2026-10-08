using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Inn;
using IdleBar.Online;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public partial class TavernLane : Control
{
    private const int BannerMargin = 16;

    private readonly LaneOverlay _overlay = new();
    private Tavern? _tavern;
    private float _artScale = 1;
    private int _top;
    private float _time;
    private int _hovered = -1;
    private Patron? _hoveredPatron;
    private bool _hoveredHelper;
    private IReadOnlyList<int> _windows = [];
    private int _pressed = -1;

    public string? Hint => _tavern is not null && _hovered >= 0 && _hovered < _tavern.Stations.Count
        ? _tavern.Stations[_hovered].Hint
        : _hoveredPatron is not null ? Describe?.Invoke(_hoveredPatron)
        : _hoveredHelper && _tavern is not null ? LaneHost.DescribeHelper(_tavern.Helper.Level) : null;

    public DecorSet Decor { get; set; } = DecorSet.Bare;

    public IReadOnlyList<string> Souvenirs { get; set; } = [];

    public Func<Patron, string?>? Describe { get; set; }

    public Func<Patron, PatronLook?> LookOf { get; set; } = patron => RegularLooks.For(patron.Regular);

    public Func<Patron, NameTag?> TagOf { get; set; } = patron => patron.Guest is string name ? new NameTag(name, BarPalette.Gold, false) : null;

    public Func<Patron, Guid?> PlayerOf { get; set; } = _ => null;

    public PatronLook? HostLook { get; set; }

    public string? HostName { get; set; }

    public Guid? HostId { get; set; }

    public event Action<Patron>? PatronClicked;

    public event Action<Patron, Vector2I>? PlayerMenuRequested;

    public event Action<PasserbyData, Vector2I>? PasserbyClicked;

    public Street? Street { get; set; }

    public void Pop(string text, int x, Color color) => _overlay.Pop(text, x, color);

    public void Announce(string text, float seconds) => _overlay.Announce(text, seconds);

    public void Speak(Guid author, string name, string text) => _overlay.Speak(author, name, text);

    public void Attach(Tavern tavern, bool payments = true)
    {
        if (_tavern is not null)
        {
            _tavern.Paid -= _overlay.Pay;
            _tavern.Prepared -= OnPrepared;
        }

        _tavern = tavern;
        _hovered = -1;
        _pressed = -1;
        _hoveredPatron = null;
        if (payments)
        {
            tavern.Paid += _overlay.Pay;
        }

        tavern.Prepared += OnPrepared;
    }

    public override void _Ready()
    {
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Stop;
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        _overlay.Advance((float)delta);
        Street?.Update((float)delta, _windows);
        if (IsVisibleInTree())
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_tavern is null)
        {
            return;
        }

        float contentScale = GetWindow().ContentScaleFactor;
        float physicalPixel = Math.Max(1, MathF.Floor(Size.Y * contentScale / TavernRows.Height));
        _artScale = physicalPixel / contentScale;
        PixelCanvas canvas = new(this, _artScale, Size);
        _top = canvas.Height - TavernRows.Height;
        if (_tavern.Layout.Width != Math.Max(canvas.Width, TavernLayout.MinWidth))
        {
            _tavern.Arrange(canvas.Width);
        }

        Outdoors outdoors = Outdoors.At(DateTime.Now);
        RoomPainter.Paint(canvas, _top, new RoomView(_tavern.Layout, Decor, Souvenirs, outdoors, LaneHits.DoorOpen(_tavern)), _time);
        DecorPlan plan = RoomPainter.PlanFor(_tavern.Layout, canvas.Width, Souvenirs.Count);
        _windows = plan.Wall.Where(piece => piece.Kind == DecorKind.Window && piece.X + WindowPainter.Width < canvas.Width).Select(piece => piece.X).ToList();
        if (Street?.Current is StreetWalk walk)
        {
            PasserbyPainter.Paint(canvas, _top, walk.WindowX, VisitDesk.Look(walk.Passerby.Avatar), walk.Progress, _time);
        }

        LaneHost.PaintBody(canvas, _top, _tavern, HostLook);
        HelperPainter.PaintBody(canvas, _top, _tavern.Helper, _time);
        PatronPainter.PaintBodies(canvas, _top, _tavern, _time, LookOf);
        CounterPainter.Paint(canvas, _top);
        PropPainter.PaintCounter(canvas, _top, plan, _time);
        FestivalPainter.PaintCounter(canvas, _top, plan, outdoors, _time);
        if (_hovered >= 0 && _hovered < _tavern.Stations.Count)
        {
            CounterPainter.Highlight(canvas, _top, StationPainter.Bounds(_tavern.Stations[_hovered], _top));
        }

        foreach (Station station in _tavern.Stations)
        {
            StationPainter.Paint(canvas, _top, station, _time);
        }

        CounterPainter.PaintDrinks(canvas, _top, _tavern);
        PatronPainter.PaintGlasses(canvas, _top, _tavern, _time);
        PatronPainter.PaintBubbles(canvas, _top, _tavern, _time);
        LaneOverlay.PaintNames(canvas, GetThemeDefaultFont(), _top, _tavern.Patrons, TagOf);
        LaneHost.PaintName(canvas, GetThemeDefaultFont(), _top, _tavern, HostLook, HostName);
        int bannerCenter = (_tavern.Layout.SeatXs[^1] + BannerMargin + canvas.Width) / 2;
        _overlay.Paint(canvas, GetThemeDefaultFont(), _top, bannerCenter);
        _overlay.PaintSpeech(canvas, GetThemeDefaultFont(), _top, author => LaneHost.Anchor(_tavern, author, PlayerOf, HostId) ?? bannerCenter);
    }

    private void OnPrepared(Preparation preparation)
    {
        if (preparation.Drink.Perfect)
        {
            _overlay.Pop("Parfait !", preparation.X, BarPalette.Gold);
        }
    }
}

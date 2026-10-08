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
    private IReadOnlyList<int> _windows = [];
    private int _pressed = -1;

    public string? Hint => _tavern is not null && _hovered >= 0 && _hovered < _tavern.Stations.Count
        ? _tavern.Stations[_hovered].Hint
        : _hoveredPatron is not null ? Describe?.Invoke(_hoveredPatron) : null;

    public DecorSet Decor { get; set; } = DecorSet.Bare;

    public IReadOnlyList<string> Souvenirs { get; set; } = [];

    public Func<Patron, string?>? Describe { get; set; }

    public Func<Patron, PatronLook?> LookOf { get; set; } = patron => RegularLooks.For(patron.Regular);

    public event Action<Patron>? PatronClicked;

    public event Action<PasserbyData, Vector2I>? PasserbyClicked;

    public Street? Street { get; set; }

    public void Pop(string text, int x, Color color) => _overlay.Pop(text, x, color);

    public void Announce(string text, float seconds) => _overlay.Announce(text, seconds);

    public void Attach(Tavern tavern)
    {
        _tavern = tavern;
        tavern.Paid += _overlay.Pay;
        tavern.Prepared += preparation =>
        {
            if (preparation.Drink.Perfect)
            {
                _overlay.Pop("Parfait !", preparation.X, BarPalette.Gold);
            }
        };
    }

    public override void _Ready()
    {
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Stop;
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (_tavern is null)
        {
            return;
        }

        switch (@event)
        {
            case InputEventMouseMotion motion:
                _hovered = StationAt(motion.Position);
                _hoveredPatron = _hovered < 0 ? PatronAt(motion.Position) : null;
                MouseDefaultCursorShape = _hovered >= 0 ? CursorShape.PointingHand : CursorShape.Arrow;
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press when WalkAt(press.Position) is StreetWalk walk:
                AcceptEvent();
                PasserbyClicked?.Invoke(walk.Passerby, ScreenPoint(press.Position));
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press when StationAt(press.Position) < 0 && PatronAt(press.Position) is Patron clicked:
                AcceptEvent();
                PatronClicked?.Invoke(clicked);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press when StationAt(press.Position) >= 0:
                AcceptEvent();
                _pressed = StationAt(press.Position);
                _tavern.Press(_pressed);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } when _pressed >= 0:
                AcceptEvent();
                _tavern.Release(_pressed);
                _pressed = -1;
                break;
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationMouseExit)
        {
            _hovered = -1;
            _hoveredPatron = null;
        }
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
        _overlay.Paint(canvas, GetThemeDefaultFont(), _top, (_tavern.Layout.SeatXs[^1] + BannerMargin + canvas.Width) / 2);
    }

    private Vector2I ScreenPoint(Vector2 position) =>
        GetWindow().Position + (Vector2I)((GetGlobalPosition() + position) * GetWindow().ContentScaleFactor);

    private Vector2I ArtPixel(Vector2 position) => new((int)(position.X / _artScale), (int)(position.Y / _artScale));

    private StreetWalk? WalkAt(Vector2 position) => LaneHits.Walk(Street, ArtPixel(position), _top);

    private int StationAt(Vector2 position) => LaneHits.Station(_tavern!, ArtPixel(position), _top);

    private Patron? PatronAt(Vector2 position) => LaneHits.Patron(_tavern!, ArtPixel(position), _top);
}

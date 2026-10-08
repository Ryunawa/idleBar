using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Inn;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public partial class TavernLane : Control
{
    private const float DoorReach = 7f;
    private const int BannerMargin = 16;

    private readonly LaneOverlay _overlay = new();
    private Tavern? _tavern;
    private float _artScale = 1;
    private int _top;
    private float _time;
    private int _hovered = -1;
    private Patron? _hoveredPatron;
    private int _pressed = -1;

    public string? Hint => _tavern is not null && _hovered >= 0 && _hovered < _tavern.Stations.Count
        ? _tavern.Stations[_hovered].Hint
        : _hoveredPatron is not null ? Describe?.Invoke(_hoveredPatron) : null;

    public DecorSet Decor { get; set; } = DecorSet.Bare;

    public IReadOnlyList<string> Souvenirs { get; set; } = [];

    public Func<Patron, string?>? Describe { get; set; }

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

        RoomPainter.Paint(canvas, _top, new RoomView(_tavern.Layout, Decor, Souvenirs, Outdoors.At(DateTime.Now), DoorOpen()), _time);
        PatronPainter.PaintBodies(canvas, _top, _tavern, _time);
        CounterPainter.Paint(canvas, _top);
        PropPainter.PaintCounter(canvas, _top, RoomPainter.PlanFor(_tavern.Layout, canvas.Width, Souvenirs.Count), _time);
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

    private bool DoorOpen() =>
        _tavern!.Patrons.Any(patron =>
            patron.Phase is PatronPhase.Entering or PatronPhase.Leaving && Math.Abs(patron.X - _tavern.Layout.DoorCenter) < DoorReach);

    private Vector2I ArtPixel(Vector2 position) => new((int)(position.X / _artScale), (int)(position.Y / _artScale));

    private int StationAt(Vector2 position)
    {
        Vector2I pixel = ArtPixel(position);
        for (int index = 0; index < _tavern!.Stations.Count; index++)
        {
            if (StationPainter.Bounds(_tavern.Stations[index], _top).HasPoint(pixel))
            {
                return index;
            }
        }

        return -1;
    }

    private Patron? PatronAt(Vector2 position)
    {
        Vector2I pixel = ArtPixel(position);
        return _tavern!.Patrons.FirstOrDefault(patron =>
            new Rect2I((int)MathF.Round(patron.X) - PatronSprites.Width / 2, _top + TavernRows.PatronTop, PatronSprites.Width, TavernRows.CounterTop - TavernRows.PatronTop).HasPoint(pixel));
    }
}

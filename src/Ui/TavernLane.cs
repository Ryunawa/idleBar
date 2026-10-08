using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Inn;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public partial class TavernLane : Control
{
    private const int PopFontSize = 12;
    private const float PopSeconds = 1.8f;
    private const float PopRise = 1.5f;
    private const float DoorReach = 7f;

    private static readonly Color PlainPop = new("efe6d2");

    private readonly List<LanePop> _pops = [];
    private Tavern? _tavern;
    private float _artScale = 1;
    private int _top;
    private float _time;
    private int _hovered = -1;
    private int _pressed = -1;

    public string? Hint => _tavern is not null && _hovered >= 0 ? _tavern.Stations[_hovered].Hint : null;

    public void Attach(Tavern tavern)
    {
        _tavern = tavern;
        tavern.Paid += payment => _pops.Add(new LanePop($"+{payment.Amount}", payment.X, PopColor(payment)));
        tavern.Prepared += preparation =>
        {
            if (preparation.Drink.Perfect)
            {
                _pops.Add(new LanePop("Parfait !", preparation.X, BarPalette.Gold));
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
        }
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        foreach (LanePop pop in _pops)
        {
            pop.Age += (float)delta;
        }

        _pops.RemoveAll(pop => pop.Age > PopSeconds);
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

        RoomPainter.Paint(canvas, _top, _tavern.Layout, SkyLight.At(DateTime.Now), _time, DoorOpen());
        PatronPainter.PaintBodies(canvas, _top, _tavern, _time);
        CounterPainter.Paint(canvas, _top);
        PropPainter.PaintCounter(canvas, _top, RoomPainter.PlanFor(_tavern.Layout, canvas.Width), _time);
        if (_hovered >= 0)
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
        PaintPops(canvas);
    }

    private bool DoorOpen() =>
        _tavern!.Patrons.Any(patron =>
            patron.Phase is PatronPhase.Entering or PatronPhase.Leaving && Math.Abs(patron.X - _tavern.Layout.DoorCenter) < DoorReach);

    private int StationAt(Vector2 position)
    {
        Vector2I pixel = new((int)(position.X / _artScale), (int)(position.Y / _artScale));
        for (int index = 0; index < _tavern!.Stations.Count; index++)
        {
            if (StationPainter.Bounds(_tavern.Stations[index], _top).HasPoint(pixel))
            {
                return index;
            }
        }

        return -1;
    }

    private void PaintPops(PixelCanvas canvas)
    {
        Font font = GetThemeDefaultFont();
        int fontSize = PixelFont.Size(PopFontSize);
        foreach (LanePop pop in _pops)
        {
            float alpha = Math.Clamp(Math.Min(pop.Age * 6, (PopSeconds - pop.Age) * 2), 0, 1);
            int x = Math.Max(1, pop.X - canvas.TextWidth(font, fontSize, pop.Text) / 2);
            int y = _top + 2 - (int)(pop.Age * PopRise);
            canvas.Text(font, fontSize, pop.Text, x, y, pop.Color with { A = alpha }, BarPalette.Shadow with { A = alpha * 0.9f });
        }
    }

    private static Color PopColor(Payment payment) =>
        payment.Perfect ? BarPalette.Gold : payment.Amount > DrinkMenu.Parting ? PlainPop : BarPalette.Muted;
}

using System;
using System.Collections.Generic;
using Godot;

namespace IdleBar.Ui;

public partial class MineLane : Control
{
    private const int MaxVisibleMiners = 40;
    private const int MaxVisibleDrills = 8;
    private const float GroundThickness = 5f;
    private const float MineLeft = 5f;
    private const float MineWidth = 32f;
    private const float ChestWidth = 26f;
    private const float ChestMargin = 6f;
    private const float DrillSpacing = 15f;
    private const float MinerSpeedMin = 22f;
    private const float MinerSpeedRange = 18f;
    private const float FloatingGainLifetime = 1.2f;
    private const float FloatingGainRise = 22f;
    private const int FloatingGainFontSize = 13;
    private const int CaptionFontSize = 12;
    private const float CaptionBaseline = 14f;
    private const string IdleHint = "Clique ici pour miner de l'or";

    private readonly List<FloatingGain> _floatingGains = [];
    private readonly StyleBoxFlat _backgroundBox = CreateBackgroundBox();
    private float _time;
    private int _miners;
    private int _drills;
    private string _banner = string.Empty;
    private float _bannerRemaining;
    private string _notice = string.Empty;

    public event Action<Vector2>? Mined;

    private float MineCenterX => MineLeft + MineWidth / 2;

    private float DrillsStartX => MineLeft + MineWidth + 6;

    private float ChestLeft => Size.X - ChestWidth - ChestMargin;

    public void SetCrew(int miners, int drills)
    {
        _miners = Math.Min(miners, MaxVisibleMiners);
        _drills = Math.Min(drills, MaxVisibleDrills);
    }

    public void ShowGain(Vector2 origin, string text) => _floatingGains.Add(new FloatingGain(origin, text));

    public void ShowBanner(string text, float seconds)
    {
        _banner = text;
        _bannerRemaining = seconds;
    }

    public void SetNotice(string notice) => _notice = notice;

    public override void _Ready()
    {
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.PointingHand;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
        {
            Mined?.Invoke(click.Position);
            AcceptEvent();
        }
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree())
        {
            return;
        }

        float step = (float)delta;
        _time += step;
        _bannerRemaining = Math.Max(0, _bannerRemaining - step);
        foreach (FloatingGain gain in _floatingGains)
        {
            gain.Age += step;
        }

        _floatingGains.RemoveAll(gain => gain.Age >= FloatingGainLifetime);
        QueueRedraw();
    }

    public override void _Draw()
    {
        float groundY = Size.Y - GroundThickness;
        Font font = GetThemeDefaultFont();

        DrawStyleBox(_backgroundBox, new Rect2(Vector2.Zero, Size));
        DrawRect(new Rect2(0, groundY, Size.X, GroundThickness), BarPalette.Ground);
        DrawMine(groundY);
        DrawChest(groundY);
        DrawDrills(groundY);
        DrawMiners(groundY);
        DrawCaption(font);
        DrawFloatingGains(font);
    }

    private static StyleBoxFlat CreateBackgroundBox()
    {
        StyleBoxFlat box = new() { BgColor = BarPalette.LaneBackground };
        box.SetCornerRadiusAll(4);
        return box;
    }

    private static float Hash(int seed)
    {
        float value = Mathf.Sin(seed * 12.9898f) * 43758.547f;
        return value - Mathf.Floor(value);
    }

    private void DrawMine(float groundY)
    {
        DrawCircle(new Vector2(MineCenterX, groundY - 14), MineWidth / 2, BarPalette.Rock);
        DrawRect(new Rect2(MineLeft, groundY - 14, MineWidth, 14), BarPalette.Rock);
        DrawRect(new Rect2(MineCenterX - 8, groundY - 16, 16, 16), BarPalette.MineOpening);
        DrawRect(new Rect2(MineCenterX - 10, groundY - 19, 20, 3), BarPalette.Wood);
        DrawRect(new Rect2(MineCenterX - 10, groundY - 16, 3, 16), BarPalette.Wood);
        DrawRect(new Rect2(MineCenterX + 7, groundY - 16, 3, 16), BarPalette.Wood);
    }

    private void DrawChest(float groundY)
    {
        DrawCircle(new Vector2(ChestLeft + 8, groundY - 16), 3, BarPalette.Gold);
        DrawCircle(new Vector2(ChestLeft + 14, groundY - 17), 3.5f, BarPalette.Gold);
        DrawCircle(new Vector2(ChestLeft + 19, groundY - 16), 2.5f, BarPalette.Gold);
        DrawRect(new Rect2(ChestLeft, groundY - 13, ChestWidth, 13), BarPalette.Wood);
        DrawRect(new Rect2(ChestLeft, groundY - 15, ChestWidth, 3), BarPalette.GoldDark);
        DrawRect(new Rect2(ChestLeft + ChestWidth / 2 - 2, groundY - 10, 4, 5), BarPalette.Gold);
    }

    private void DrawDrills(float groundY)
    {
        for (int index = 0; index < _drills; index++)
        {
            float x = DrillsStartX + index * DrillSpacing;
            float jitter = Mathf.Sin(_time * 40 + index * 1.7f) * 1.2f;
            DrawRect(new Rect2(x + 4, groundY - 22, 2, 7), BarPalette.Steel);
            DrawRect(new Rect2(x, groundY - 16, 10, 9), BarPalette.Steel);
            DrawRect(new Rect2(x + 2, groundY - 14, 2, 2), BarPalette.Gold);
            DrawColoredPolygon(
            [
                new Vector2(x + 2, groundY - 7 + jitter),
                new Vector2(x + 8, groundY - 7 + jitter),
                new Vector2(x + 5, groundY + 2 + jitter),
            ],
            BarPalette.Muted);
        }
    }

    private void DrawMiners(float groundY)
    {
        float start = MineCenterX;
        float end = ChestLeft - 6;
        float length = Math.Max(end - start, 1);

        for (int index = 0; index < _miners; index++)
        {
            float speed = MinerSpeedMin + Hash(index) * MinerSpeedRange;
            float travel = (Hash(index + 1000) * 2 * length + _time * speed) % (2 * length);
            bool carrying = travel < length;
            float x = carrying ? start + travel : end - (travel - length);
            DrawMiner(new Vector2(x, groundY), carrying ? 1 : -1, carrying, index);
        }
    }

    private void DrawMiner(Vector2 feet, float direction, bool carrying, int index)
    {
        float cycle = _time * 9 + index;
        float bob = Mathf.Abs(Mathf.Sin(cycle)) * 1.5f;
        float stride = Mathf.Sin(cycle) * 2.5f;
        Vector2 hip = feet + new Vector2(0, -6 - bob);
        Vector2 head = hip + new Vector2(0, -10);

        DrawLine(hip, feet + new Vector2(stride, 0), BarPalette.OverallsDark, 1.5f);
        DrawLine(hip, feet + new Vector2(-stride, 0), BarPalette.OverallsDark, 1.5f);
        DrawRect(new Rect2(hip.X - 3, hip.Y - 7, 6, 7), BarPalette.Overalls);
        DrawCircle(head, 3, BarPalette.Skin);
        DrawRect(new Rect2(head.X - 3.5f, head.Y - 4, 7, 2.5f), BarPalette.Gold);
        DrawCircle(head + new Vector2(direction * 3.5f, -2.5f), 1, Colors.White);

        if (carrying)
        {
            DrawCircle(hip + new Vector2(direction * 5, -4), 2.5f, BarPalette.Gold);
            return;
        }

        DrawLine(hip + new Vector2(-direction * 2, -3), hip + new Vector2(-direction * 6, -11), BarPalette.Wood, 1.5f);
        DrawLine(hip + new Vector2(-direction * 8, -10), hip + new Vector2(-direction * 4, -13), BarPalette.Steel, 1.5f);
    }

    private void DrawCaption(Font font)
    {
        if (_bannerRemaining > 0)
        {
            DrawCenteredText(font, _banner, BarPalette.Gold with { A = Math.Min(1, _bannerRemaining) });
            return;
        }

        if (_notice.Length > 0)
        {
            DrawCenteredText(font, _notice, BarPalette.Warning);
            return;
        }

        if (_miners == 0 && _drills == 0)
        {
            DrawCenteredText(font, IdleHint, BarPalette.Muted);
        }
    }

    private void DrawCenteredText(Font font, string text, Color color)
    {
        Vector2 size = font.GetStringSize(text, HorizontalAlignment.Left, -1, CaptionFontSize);
        DrawString(font, new Vector2((Size.X - size.X) / 2, CaptionBaseline), text, HorizontalAlignment.Left, -1, CaptionFontSize, color);
    }

    private void DrawFloatingGains(Font font)
    {
        foreach (FloatingGain gain in _floatingGains)
        {
            float progress = gain.Age / FloatingGainLifetime;
            Vector2 size = font.GetStringSize(gain.Text, HorizontalAlignment.Left, -1, FloatingGainFontSize);
            Vector2 position = gain.Origin + new Vector2(-size.X / 2, -FloatingGainRise * progress);
            DrawString(font, position, gain.Text, HorizontalAlignment.Left, -1, FloatingGainFontSize, BarPalette.Gold with { A = 1 - progress });
        }
    }
}

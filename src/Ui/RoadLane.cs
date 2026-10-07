using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public partial class RoadLane : Control
{
    private const float TargetArtRows = 23f;
    private const float ScrollSpeed = 7f;
    private const int CaptionFontSize = 12;
    private const float CaptionHeight = 18f;
    private const float GainSeconds = 2.8f;
    private const float GainStagger = 0.45f;
    private const float GainFadeIn = 0.15f;
    private const float GainFadeOut = 0.7f;
    private const float GainRise = 5f;
    private const float SparkSeconds = 0.6f;
    private const int Sparks = 6;
    private const int LabelLift = 7;

    private readonly List<FloatingGain> _gains = [];
    private LaneScene _scene = LaneScene.Idle(string.Empty);
    private Label _caption = null!;
    private float _time;
    private float _distance;
    private string _banner = string.Empty;
    private float _bannerRemaining;
    private ProductPop _pop = ProductPop.None;
    private IReadOnlyList<StreetPlot> _plots = [];
    private StreetBuilding? _hovered;
    private float _artScale = 1;
    private int _ground;

    public event Action? Pressed;

    public event Action<StreetBuilding, Vector2I>? BuildingPressed;

    public LaneScene Scene => _scene;

    public void SetScene(LaneScene scene)
    {
        if (scene.Mode == LaneMode.Workshop && _scene.Mode == LaneMode.Workshop && scene.Products.Count > _scene.Products.Count)
        {
            _pop = new ProductPop(_scene.Products.Count, 0);
        }

        _scene = scene;
    }

    public void ShowBanner(string text, float seconds)
    {
        _banner = text;
        _bannerRemaining = seconds;
    }

    public void ShowGain(LaneGain gain)
    {
        int waiting = _gains.Count(floating => floating.Age < GainStagger);
        _gains.Add(new FloatingGain(gain, -waiting * GainStagger));
    }

    public override void _Ready()
    {
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        TextureFilter = TextureFilterEnum.Nearest;

        _caption = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
            TextureFilter = TextureFilterEnum.Linear,
        };
        _caption.AddThemeFontSizeOverride("font_size", PixelFont.Size(CaptionFontSize));
        _caption.AddThemeColorOverride("font_outline_color", BarPalette.Shadow with { A = 0.8f });
        _caption.AddThemeConstantOverride("outline_size", 4);
        AddChild(_caption);
        _caption.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        _caption.OffsetBottom = CaptionHeight;
    }

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseMotion motion:
                _hovered = BuildingAt(motion.Position);
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click:
                AcceptEvent();
                if (BuildingAt(click.Position) is StreetBuilding building)
                {
                    BuildingPressed?.Invoke(building, ScreenAnchor(building));
                }
                else
                {
                    Pressed?.Invoke();
                }

                break;
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationMouseExit)
        {
            _hovered = null;
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
        if (_scene.Mode == LaneMode.Travelling)
        {
            _distance += step * ScrollSpeed;
        }

        _bannerRemaining = Math.Max(0, _bannerRemaining - step);
        _pop = _pop with { Age = _pop.Age + step };
        foreach (FloatingGain floating in _gains)
        {
            floating.Age += step;
        }

        _gains.RemoveAll(floating => floating.Age > GainSeconds);
        RefreshCaption();
        QueueRedraw();
    }

    public override void _Draw()
    {
        float contentScale = GetWindow().ContentScaleFactor;
        float physicalPixel = Math.Max(1, MathF.Round(Size.Y * contentScale / TargetArtRows, MidpointRounding.AwayFromZero));
        _artScale = physicalPixel / contentScale;
        PixelCanvas canvas = new(this, _artScale, Size);
        _ground = LandscapePainter.GroundTop(canvas);
        Ambience ambience = new(_time, SkyLight.At(DateTime.Now));
        BiomeStyle style = BiomeStyles.For(_scene.Biome);
        CaravanLook look = _scene.Look ?? CaravanLook.Plain;
        bool withProps = _scene.Mode is LaneMode.Idle or LaneMode.Travelling;
        LandscapePainter.Paint(canvas, style, _distance, withProps, ambience);
        if (!_scene.Raining)
        {
            FolkPainter.PaintBirds(canvas, ambience);
        }

        _plots = [];
        switch (_scene.Mode)
        {
            case LaneMode.Travelling:
                FolkPainter.PaintTraffic(canvas, _time, ScrollSpeed);
                CaravanPainter.PaintTravelling(canvas, _time, _scene.Progress, look);
                _plots = [StreetPainter.TravelPlot(canvas, look)];
                break;
            case LaneMode.Halted:
                CaravanPainter.PaintHalted(canvas, _scene.Progress, look, _scene.Hazard, _time);
                _plots = [StreetPainter.TravelPlot(canvas, look)];
                break;
            case LaneMode.InTown when _scene.Street is StreetState street:
                _plots = StreetPainter.Layout(canvas, street, look, 0);
                StreetPainter.Paint(canvas, _plots, new StreetScene(street, false, _scene.Errands, [], ProductPop.None, look), ambience);
                break;
            case LaneMode.Workshop when _scene.Street is StreetState street:
                _plots = StreetPainter.Layout(canvas, street, null, _scene.Products.Count);
                StreetPainter.Paint(canvas, _plots, new StreetScene(street, _scene.Busy, _scene.Errands, _scene.Products, _pop, null), ambience);
                if (_scene.Busy && _scene.Progress > 0)
                {
                    TownPainter.PaintProgress(canvas, _scene.Progress);
                }

                break;
        }

        FolkPainter.PaintFireflies(canvas, style, _distance, ambience);
        if (_scene.Raining)
        {
            WeatherPainter.PaintRain(canvas, _time);
        }

        PaintHover(canvas);
        PaintGains(canvas);
    }

    private Vector2I ScreenAnchor(StreetBuilding building)
    {
        StreetPlot plot = _plots.FirstOrDefault(each => each.Building == building);
        Window window = GetWindow();
        float x = (GetGlobalPosition().X + (plot.X + plot.Width / 2f) * _artScale) * window.ContentScaleFactor;
        return new Vector2I(window.Position.X + (int)x, window.Position.Y);
    }

    private StreetBuilding? BuildingAt(Vector2 position)
    {
        int x = (int)(position.X / _artScale);
        int y = (int)(position.Y / _artScale);
        foreach (StreetPlot plot in _plots)
        {
            if (plot.Contains(x, y, _ground))
            {
                return plot.Building;
            }
        }

        return null;
    }

    private void PaintHover(PixelCanvas canvas)
    {
        if (_hovered is not StreetBuilding hovered || _plots.FirstOrDefault(plot => plot.Building == hovered) is not { Width: > 0 } plot)
        {
            return;
        }

        canvas.Fill(plot.X, _ground + LandscapePainter.GroundRows - 2, plot.Width, 1, BarPalette.Gold);
        Font font = GetThemeDefaultFont();
        int fontSize = PixelFont.Size(CaptionFontSize);
        string name = StreetNames.Of(hovered, _scene);
        int width = canvas.TextWidth(font, fontSize, name);
        int x = Math.Clamp(plot.X + (plot.Width - width) / 2, 1, Math.Max(canvas.Width - width - 1, 1));
        int y = Math.Max(_ground - plot.Height - LabelLift, 0);
        canvas.Text(font, fontSize, name, x, y, BarPalette.Gold, BarPalette.Shadow);
    }

    private void PaintGains(PixelCanvas canvas)
    {
        if (_gains.Count == 0)
        {
            return;
        }

        Font font = GetThemeDefaultFont();
        int fontSize = PixelFont.Size(CaptionFontSize);
        int anchor = GainAnchor(canvas);
        int baseline = LandscapePainter.GroundTop(canvas) - 14;
        foreach (FloatingGain floating in _gains.Where(floating => floating.Age >= 0))
        {
            float age = floating.Age;
            float alpha = Math.Clamp(Math.Min(age / GainFadeIn, (GainSeconds - age) / GainFadeOut), 0, 1);
            int top = baseline - (int)(age * GainRise);
            LaneGain gain = floating.Gain;
            PixelSprite? icon = gain.GoodId is string goodId ? GoodIcons.For(goodId) : null;
            int textWidth = canvas.TextWidth(font, fontSize, gain.Text);
            int x = anchor - (textWidth + (icon is null ? 0 : icon.Width + 1)) / 2;
            if (icon is not null)
            {
                canvas.Draw(icon, x, top + 1, Colors.White with { A = alpha });
                x += icon.Width + 1;
            }

            canvas.Text(font, fontSize, gain.Text, x, top - 1, gain.Color with { A = alpha }, BarPalette.Shadow with { A = alpha * 0.9f });
            PaintSparks(canvas, anchor, top + 2, age);
        }
    }

    private static void PaintSparks(PixelCanvas canvas, int x, int y, float age)
    {
        if (age >= SparkSeconds)
        {
            return;
        }

        float reach = 3 + age * 14;
        for (int spark = 0; spark < Sparks; spark++)
        {
            float angle = spark * MathF.Tau / Sparks + 0.4f;
            int sx = x + (int)MathF.Round(MathF.Cos(angle) * reach * 1.5f);
            int sy = y + (int)MathF.Round(MathF.Sin(angle) * reach * 0.6f);
            canvas.Fill(sx, sy, 1, 1, BarPalette.Gold with { A = 1 - age / SparkSeconds });
        }
    }

    private int GainAnchor(PixelCanvas canvas) =>
        _plots.Count > 0 ? _plots[0].X + _plots[0].Width / 2 : canvas.Width / 2;

    private void RefreshCaption()
    {
        bool showBanner = _bannerRemaining > 0;
        _caption.Text = showBanner ? _banner : _scene.Caption;
        _caption.Modulate = showBanner ? BarPalette.Gold with { A = Math.Min(1, _bannerRemaining) } : BarPalette.Text;
    }
}

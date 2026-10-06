using System;
using Godot;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public partial class RoadLane : Control
{
    private const float TargetArtRows = 23f;
    private const float ScrollSpeed = 7f;
    private const int CaptionFontSize = 12;
    private const float CaptionHeight = 18f;

    private LaneScene _scene = LaneScene.Idle(string.Empty);
    private Label _caption = null!;
    private float _time;
    private float _distance;
    private string _banner = string.Empty;
    private float _bannerRemaining;

    public event Action? Pressed;

    public void SetScene(LaneScene scene) => _scene = scene;

    public void ShowBanner(string text, float seconds)
    {
        _banner = text;
        _bannerRemaining = seconds;
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
        _caption.AddThemeFontSizeOverride("font_size", CaptionFontSize);
        _caption.AddThemeColorOverride("font_shadow_color", BarPalette.Shadow with { A = 0.8f });
        _caption.AddThemeConstantOverride("shadow_offset_x", 1);
        _caption.AddThemeConstantOverride("shadow_offset_y", 1);
        AddChild(_caption);
        _caption.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        _caption.OffsetBottom = CaptionHeight;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            AcceptEvent();
            Pressed?.Invoke();
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
        RefreshCaption();
        QueueRedraw();
    }

    public override void _Draw()
    {
        float contentScale = GetWindow().ContentScaleFactor;
        float physicalPixel = Math.Max(1, MathF.Round(Size.Y * contentScale / TargetArtRows, MidpointRounding.AwayFromZero));
        PixelCanvas canvas = new(this, physicalPixel / contentScale, Size);
        bool withProps = _scene.Mode is LaneMode.Idle or LaneMode.Travelling;
        LandscapePainter.Paint(canvas, BiomeStyles.For(_scene.Biome), _distance, withProps);

        switch (_scene.Mode)
        {
            case LaneMode.Travelling:
                CaravanPainter.PaintTravelling(canvas, _time, _scene.Progress, _scene.Look ?? CaravanLook.Plain);
                break;
            case LaneMode.Halted:
                CaravanPainter.PaintHalted(canvas, _scene.Progress, _scene.Look ?? CaravanLook.Plain, _scene.Hazard);
                break;
            case LaneMode.InTown:
                CaravanPainter.PaintInTown(canvas, _scene.Look ?? CaravanLook.Plain);
                break;
            case LaneMode.Workshop:
                WorkshopPainter.Paint(canvas, _scene.CraftId, _time, _scene.Busy, _scene.Products);
                if (_scene.Busy && _scene.Progress > 0)
                {
                    TownPainter.PaintProgress(canvas, _scene.Progress);
                }

                break;
        }

        if (_scene.Raining)
        {
            WeatherPainter.PaintRain(canvas, _time);
        }
    }

    private void RefreshCaption()
    {
        bool showBanner = _bannerRemaining > 0;
        _caption.Text = showBanner ? _banner : _scene.Caption;
        _caption.Modulate = showBanner ? BarPalette.Gold with { A = Math.Min(1, _bannerRemaining) } : BarPalette.Text;
    }
}

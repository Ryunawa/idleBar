using System;
using System.Collections.Generic;
using Godot;
using IdleBar.Frontier;
using IdleBar.Pixel;
using IdleBar.Scene;

namespace IdleBar.Ui;

public partial class FrontierLane : Control
{
    private const double LightRefreshSeconds = 1;

    private readonly ScenePainter _painter = new();
    private readonly FrontierScene _scene = new(new Random());
    private readonly List<FrontierEvent> _pending = [];
    private Image? _image;
    private ImageTexture? _texture;
    private SceneLight _light = SceneLight.Noon;
    private double _sinceLight = LightRefreshSeconds;
    private float _artScale = 1;
    private FrontierLayout _layout = FrontierLayout.For(1, FrontierLayout.Rows);

    public event Action? GoldArrived;

    public FrontierGame? Game { get; set; }

    public string? Hint { get; private set; }

    public FrontierScene Scene => _scene;

    public FrontierLayout Layout => _layout;

    public float ArtScale => _artScale;

    public int HoveredSkill { get; set; } = -1;

    public override void _Ready()
    {
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Stop;
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public void Receive(IReadOnlyList<FrontierEvent> events) => _pending.AddRange(events);

    public override void _Process(double delta)
    {
        _sinceLight += delta;
        if (_sinceLight >= LightRefreshSeconds)
        {
            _sinceLight = 0;
            _light = Daylight(DateTime.Now);
        }

        if (Game is not FrontierGame game || !IsVisibleInTree())
        {
            _pending.Clear();
            return;
        }

        if (_scene.Update((float)delta, game.State, _pending, _layout, _light.Night) > 0)
        {
            GoldArrived?.Invoke();
        }

        _pending.Clear();
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Game is not FrontierGame game || Size.X < 1 || Size.Y < 1)
        {
            return;
        }

        float contentScale = GetWindow().ContentScaleFactor;
        float physicalPixel = Math.Max(1, MathF.Floor(Size.Y * contentScale / FrontierLayout.Rows));
        _artScale = physicalPixel / contentScale;
        int width = (int)MathF.Ceiling(Size.X / _artScale);
        int height = (int)MathF.Floor(Size.Y / _artScale);
        if (_layout.Width != width || _layout.Top != Math.Max(0, height - FrontierLayout.Rows))
        {
            _layout = FrontierLayout.For(width, height);
        }

        ArtRaster frame = _painter.Paint(game.State, _layout, _scene, _light, HoveredSkill);
        Upload(frame);
        if (_texture is not null)
        {
            DrawTextureRect(_texture, new Rect2(0, Size.Y - frame.Height * _artScale, frame.Width * _artScale, frame.Height * _artScale), false);
        }
    }

    public void Describe(string? hint) => Hint = hint;

    private void Upload(ArtRaster frame)
    {
        if (_image is null || _image.GetWidth() != frame.Width || _image.GetHeight() != frame.Height || _texture is null)
        {
            _image = Image.CreateFromData(frame.Width, frame.Height, false, Image.Format.Rgba8, frame.Data);
            _texture = ImageTexture.CreateFromImage(_image);
            return;
        }

        _image.SetData(frame.Width, frame.Height, false, Image.Format.Rgba8, frame.Data);
        _texture.Update(_image);
    }

    private static SceneLight Daylight(DateTime now)
    {
        SkyLight sky = SkyLight.At(now);
        Precipitation weather = Weather.At(now);
        return new SceneLight(sky.Daylight, sky.Warmth, sky.SunCourse, sky.MoonCourse, weather == Precipitation.Rain, weather == Precipitation.Snow);
    }
}

using System;
using Godot;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public partial class TipJarButton : HBoxContainer
{
    private const int IconSize = 16;

    private static readonly Color Glow = new(1.6f, 1.5f, 1.2f);

    private Label _amount = null!;
    private bool _full;
    private bool _ready;
    private bool _hovered;

    public event Action? Pressed;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        AddThemeConstantOverride("separation", 3);
        AddChild(new TextureRect
        {
            Texture = IconSprites.Jar.Texture,
            CustomMinimumSize = new Vector2(IconSize, IconSize),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        });
        _amount = BarLabels.Create(12, BarPalette.Gold);
        _amount.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        AddChild(_amount);
        MouseEntered += () => _hovered = true;
        MouseExited += () => _hovered = false;
        ClickBinding.OnLeftPress(this, () =>
        {
            if (_ready)
            {
                Pressed?.Invoke();
            }
        });
    }

    public void Display(TipJarView? view)
    {
        Visible = view is not null;
        if (view is null)
        {
            return;
        }

        _amount.Text = NumberFormat.Amount(view.Amount);
        TooltipText = view.Tooltip;
        _ready = view.Amount >= 1;
        _full = view.Full;
    }

    public void Pulse(float phase)
    {
        Modulate = _full ? Colors.White.Lerp(Glow, 0.5f + 0.5f * MathF.Sin(phase)) : Colors.White;
        _amount.AddThemeColorOverride("font_color", !_ready ? BarPalette.Muted : _hovered ? BarPalette.Text : BarPalette.Gold);
    }
}

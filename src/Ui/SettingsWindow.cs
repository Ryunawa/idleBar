using System;
using System.Collections.Generic;
using Godot;
using IdleBar.Desktop;

namespace IdleBar.Ui;

public partial class SettingsWindow : Window
{
    private static readonly Vector2I BaseSize = new(380, 270);

    private readonly List<DisplayScreen> _screens = [];
    private Label _size = null!;
    private Button _shrink = null!;
    private Button _grow = null!;
    private OptionButton _screen = null!;
    private float _currentSize = 1f;

    public event Action<float>? SizeChosen;

    public event Action<string>? ScreenChosen;

    public override void _Ready()
    {
        VBoxContainer content = WindowFrame.Build(this, "IdleBar · Réglages", 8);

        content.AddChild(CreateHeading("Taille de la barre"));
        HBoxContainer sizeRow = new() { Alignment = BoxContainer.AlignmentMode.Center };
        sizeRow.AddThemeConstantOverride("separation", 12);
        _shrink = new Button { Text = "−", CustomMinimumSize = new Vector2(36, 0), FocusMode = Control.FocusModeEnum.None };
        _shrink.Pressed += () => StepSize(-1);
        _size = BarLabels.Create(16, BarPalette.Gold, HorizontalAlignment.Center);
        _size.CustomMinimumSize = new Vector2(70, 0);
        _grow = new Button { Text = "+", CustomMinimumSize = new Vector2(36, 0), FocusMode = Control.FocusModeEnum.None };
        _grow.Pressed += () => StepSize(1);
        sizeRow.AddChild(_shrink);
        sizeRow.AddChild(_size);
        sizeRow.AddChild(_grow);
        content.AddChild(sizeRow);

        content.AddChild(CreateHeading("Écran"));
        _screen = new OptionButton();
        _screen.ItemSelected += index => ScreenChosen?.Invoke(_screens[(int)index].Device);
        content.AddChild(_screen);

        Label hint = new()
        {
            Text = "La barre se place en bas de l'écran choisi. Les fenêtres du jeu s'ouvrent sur le même écran.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        hint.AddThemeColorOverride("font_color", BarPalette.Muted);
        hint.AddThemeFontSizeOverride("font_size", 12);
        content.AddChild(hint);

        Button close = new() { Text = "Fermer" };
        close.Pressed += Hide;
        content.AddChild(close);
    }

    public void Open(float scale, float size, string? screenDevice, IReadOnlyList<DisplayScreen> screens)
    {
        _currentSize = BarSizes.Nearest(size);
        RefreshSize();

        _screens.Clear();
        _screens.AddRange(screens);
        _screen.Clear();
        int selected = 0;
        for (int index = 0; index < _screens.Count; index++)
        {
            DisplayScreen screen = _screens[index];
            string primary = screen.Primary ? " · principal" : string.Empty;
            _screen.AddItem($"Écran {index + 1} · {screen.Width} × {screen.Height}{primary}");
            if (screen.Device == screenDevice || (screenDevice is null && screen.Primary))
            {
                selected = index;
            }
        }

        _screen.Disabled = _screens.Count < 2;
        if (_screens.Count > 0)
        {
            _screen.Select(selected);
        }

        WindowFrame.Present(this, BaseSize, scale);
    }

    private static Label CreateHeading(string text)
    {
        Label heading = BarLabels.Create(13, BarPalette.Text);
        heading.Text = text;
        return heading;
    }

    private void StepSize(int direction)
    {
        float next = BarSizes.Step(_currentSize, direction);
        if (next == _currentSize)
        {
            return;
        }

        _currentSize = next;
        RefreshSize();
        SizeChosen?.Invoke(next);
    }

    private void RefreshSize()
    {
        _size.Text = BarSizes.Describe(_currentSize);
        _shrink.Disabled = !BarSizes.CanShrink(_currentSize);
        _grow.Disabled = !BarSizes.CanGrow(_currentSize);
    }
}

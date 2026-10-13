using System;
using System.Collections.Generic;
using Godot;
using IdleBar.Cloud;
using IdleBar.Desktop;

namespace IdleBar.Ui;

public partial class SettingsWindow : Window
{
    private static readonly Vector2I BaseSize = new(380, 268);

    private readonly List<DisplayScreen> _screens = [];
    private OptionButton _screen = null!;
    private Button _signOut = null!;

    public event Action<string>? ScreenChosen;

    public event Action? FaqRequested;

    public event Action? SignOutRequested;

    public override void _Ready()
    {
        VBoxContainer content = WindowFrame.Build(this, $"IdleBar {GameVersion.Name} · Réglages", 8);

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
        hint.AddThemeFontSizeOverride("font_size", PixelFont.Size(12));
        content.AddChild(hint);

        content.AddChild(CreateHeading("Compte"));
        _signOut = new Button { Text = "Se déconnecter" };
        _signOut.Pressed += () => SignOutRequested?.Invoke();
        content.AddChild(_signOut);

        HBoxContainer buttons = new();
        buttons.AddThemeConstantOverride("separation", 8);
        Button faq = new() { Text = "Questions fréquentes", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        faq.Pressed += () => FaqRequested?.Invoke();
        Button close = new() { Text = "Fermer", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        close.Pressed += Hide;
        buttons.AddChild(faq);
        buttons.AddChild(close);
        content.AddChild(buttons);
    }

    public void Open(float scale, string? screenDevice, IReadOnlyList<DisplayScreen> screens, bool signedIn)
    {
        _signOut.Disabled = !signedIn;
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
}

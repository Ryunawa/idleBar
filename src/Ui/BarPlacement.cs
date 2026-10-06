using System;
using Godot;
using IdleBar.Desktop;

namespace IdleBar.Ui;

public sealed class BarPlacement : IDisposable
{
    private const int ExpandedHeight = 56;
    private const int CollapsedHeight = 24;
    private const int ActiveFramesPerSecond = 30;
    private const int BackgroundFramesPerSecond = 5;

    private readonly Window _window;
    private readonly BarPreferences _preferences;
    private readonly AppBar? _appBar;

    public BarPlacement(Window window, BarPreferences preferences)
    {
        _window = window;
        _preferences = preferences;
        if (!OperatingSystem.IsWindows())
        {
            GD.PushWarning("La barre réservée n'est disponible que sous Windows.");
            return;
        }

        AppBar appBar = new(new IntPtr(DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle)));
        appBar.Docked += () => _window.ContentScaleFactor = appBar.Scale;
        appBar.FullscreenAppChanged += _ => UpdateFrameRate();
        appBar.Place(preferences.ScreenDevice, preferences.Size);
        _appBar = appBar;
    }

    public event Action<bool>? CollapsedChanged;

    public bool Collapsed => _preferences.Collapsed;

    public float Size => _preferences.Size;

    public string? ScreenDevice => _preferences.ScreenDevice;

    public float DialogScale => _appBar?.DpiScale ?? 1f;

    public void Update() => _appBar?.Update();

    public void ApplyCollapsed(bool collapsed)
    {
        _preferences.Collapsed = collapsed;
        _appBar?.Dock(collapsed ? CollapsedHeight : ExpandedHeight);
        UpdateFrameRate();
        CollapsedChanged?.Invoke(collapsed);
    }

    public void ToggleCollapsed()
    {
        ApplyCollapsed(!_preferences.Collapsed);
        _preferences.Save();
    }

    public void Resize(float size)
    {
        _preferences.Size = BarSizes.Nearest(size);
        _preferences.Save();
        _appBar?.Place(_preferences.ScreenDevice, _preferences.Size);
    }

    public void MoveTo(string screenDevice)
    {
        _preferences.ScreenDevice = screenDevice;
        _preferences.Save();
        _appBar?.Place(screenDevice, _preferences.Size);
    }

    public void Dispose() => _appBar?.Dispose();

    private void UpdateFrameRate()
    {
        bool inBackground = _preferences.Collapsed || _appBar?.FullscreenAppActive == true;
        Engine.MaxFps = inBackground ? BackgroundFramesPerSecond : ActiveFramesPerSecond;
    }
}

using System;
using System.Collections.Generic;
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
    private readonly IBarDock _dock;

    public BarPlacement(Window window, BarPreferences preferences)
    {
        _window = window;
        _preferences = preferences;
        _dock = OperatingSystem.IsWindows()
            ? new AppBar(new IntPtr(DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle)))
            : new FloatingDock(window);
        _dock.Docked += () => _window.ContentScaleFactor = _dock.Scale;
        _dock.FullscreenAppChanged += _ => UpdateFrameRate();
        _dock.Place(preferences.ScreenDevice);
    }

    public event Action<bool>? CollapsedChanged;

    public bool Collapsed => _preferences.Collapsed;

    public string? ScreenDevice => _preferences.ScreenDevice;

    public float DialogScale => _dock.ScreenScale;

    public IReadOnlyList<DisplayScreen> DetectScreens() => _dock.DetectScreens();

    public void Update() => _dock.Update();

    public void ApplyCollapsed(bool collapsed)
    {
        _preferences.Collapsed = collapsed;
        _dock.Dock(collapsed ? CollapsedHeight : ExpandedHeight);
        UpdateFrameRate();
        CollapsedChanged?.Invoke(collapsed);
    }

    public void ToggleCollapsed()
    {
        ApplyCollapsed(!_preferences.Collapsed);
        _preferences.Save();
    }

    public void MoveTo(string screenDevice)
    {
        _preferences.ScreenDevice = screenDevice;
        _preferences.Save();
        _dock.Place(screenDevice);
    }

    public void Dispose() => _dock.Dispose();

    private void UpdateFrameRate()
    {
        bool inBackground = _preferences.Collapsed || _dock.FullscreenAppActive;
        Engine.MaxFps = inBackground ? BackgroundFramesPerSecond : ActiveFramesPerSecond;
    }
}

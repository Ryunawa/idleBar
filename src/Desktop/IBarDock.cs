using System;
using System.Collections.Generic;

namespace IdleBar.Desktop;

public interface IBarDock : IDisposable
{
    event Action? Docked;

    event Action<bool>? FullscreenAppChanged;

    float DpiScale { get; }

    float Scale { get; }

    bool FullscreenAppActive { get; }

    IReadOnlyList<DisplayScreen> DetectScreens();

    void Dock(int logicalHeight);

    void Place(string? screenDevice, float sizeFactor);

    void Update();
}
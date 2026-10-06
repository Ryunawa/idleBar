using Godot;

namespace IdleBar.Pixel;

public sealed record SilhouetteLayer(
    Color Fill,
    Color Rim,
    int Height,
    int Amplitude,
    float Period,
    int Detail,
    float DetailPeriod,
    float Parallax);

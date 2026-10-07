using System.Collections.Generic;
using Godot;

namespace IdleBar.Pixel;

public sealed record BiomeStyle(
    Color Sky,
    Color Stars,
    SilhouetteLayer Far,
    SilhouetteLayer Near,
    Color Ground,
    Color Road,
    Color RoadMark,
    IReadOnlyList<PixelSprite> Props,
    int PropSpacing,
    Color? Tuft = null,
    bool Fireflies = false);

using System.Collections.Generic;

namespace IdleBar.Scene;

public sealed class Banner
{
    public Banner(IReadOnlyList<string> lines, float duration)
    {
        Lines = lines;
        Duration = duration;
    }

    public IReadOnlyList<string> Lines { get; }

    public float Duration { get; }

    public float Time { get; set; }
}

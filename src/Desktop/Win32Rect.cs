using System.Runtime.InteropServices;

namespace IdleBar.Desktop;

[StructLayout(LayoutKind.Sequential)]
internal struct Win32Rect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public readonly int Width => Right - Left;

    public readonly int Height => Bottom - Top;

    public readonly bool SameAs(Win32Rect other) =>
        Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
}

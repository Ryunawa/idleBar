using System;
using System.Runtime.InteropServices;

namespace IdleBar.Desktop;

[StructLayout(LayoutKind.Sequential)]
internal struct AppBarData
{
    public uint Size;
    public IntPtr Window;
    public uint CallbackMessage;
    public uint Edge;
    public Win32Rect Bounds;
    public IntPtr Param;
}

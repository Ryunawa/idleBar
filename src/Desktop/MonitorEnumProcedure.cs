using System;
using System.Runtime.InteropServices;

namespace IdleBar.Desktop;

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
[return: MarshalAs(UnmanagedType.Bool)]
internal delegate bool MonitorEnumProcedure(IntPtr monitor, IntPtr deviceContext, ref Win32Rect bounds, IntPtr data);

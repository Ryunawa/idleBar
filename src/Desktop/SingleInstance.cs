using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace IdleBar.Desktop;

public sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\IdleBar.SingleInstance";

    private static readonly TimeSpan GracefulExitTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan ForcedExitTimeout = TimeSpan.FromSeconds(3);

    private readonly Mutex _mutex;
    private bool _owned;

    private SingleInstance(Mutex mutex, bool owned)
    {
        _mutex = mutex;
        _owned = owned;
    }

    public static SingleInstance ReplaceRunningInstance(string processIdPath)
    {
        Mutex mutex = new(false, MutexName);
        bool owned = TryWait(mutex, TimeSpan.Zero);
        if (!owned)
        {
            using Process? previous = FindPreviousInstance(processIdPath);
            owned = RequestClose(previous) && TryWait(mutex, GracefulExitTimeout);
            if (!owned && previous is not null)
            {
                previous.Kill();
                owned = TryWait(mutex, ForcedExitTimeout);
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(processIdPath)!);
        File.WriteAllText(processIdPath, Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
        return new SingleInstance(mutex, owned);
    }

    public void Dispose()
    {
        if (_owned)
        {
            _owned = false;
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }

    private static bool TryWait(Mutex mutex, TimeSpan timeout)
    {
        try
        {
            return mutex.WaitOne(timeout);
        }
        catch (AbandonedMutexException)
        {
            return true;
        }
    }

    private static Process? FindPreviousInstance(string processIdPath)
    {
        if (!File.Exists(processIdPath)
            || !int.TryParse(File.ReadAllText(processIdPath).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int processId)
            || processId == Environment.ProcessId)
        {
            return null;
        }

        Process process;
        try
        {
            process = Process.GetProcessById(processId);
        }
        catch (ArgumentException)
        {
            return null;
        }

        using Process current = Process.GetCurrentProcess();
        if (process.ProcessName == current.ProcessName)
        {
            return process;
        }

        process.Dispose();
        return null;
    }

    private static bool RequestClose(Process? previous)
    {
        if (previous is null || !OperatingSystem.IsWindows())
        {
            return false;
        }

        IntPtr window = previous.MainWindowHandle;
        return window != IntPtr.Zero && Win32.PostMessage(window, Win32.WmClose, IntPtr.Zero, IntPtr.Zero);
    }
}

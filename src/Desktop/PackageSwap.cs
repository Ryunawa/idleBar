using System;
using System.Collections.Generic;
using System.IO;

namespace IdleBar.Desktop;

public static class PackageSwap
{
    public static void Apply(string staging, string install, string retired)
    {
        string[] files = Directory.GetFiles(staging, "*", SearchOption.AllDirectories);
        Array.Sort(files, StringComparer.Ordinal);
        List<string> placed = [];
        List<string> setAside = [];
        try
        {
            foreach (string source in files)
            {
                string relative = Path.GetRelativePath(staging, source);
                string target = Path.Combine(install, relative);
                if (File.Exists(target))
                {
                    MoveInto(target, Path.Combine(retired, relative));
                    setAside.Add(relative);
                }

                MoveInto(source, target);
                placed.Add(relative);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Restore(install, retired, placed, setAside);
            throw;
        }
    }

    private static void MoveInto(string source, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Move(source, destination);
    }

    private static void Restore(string install, string retired, List<string> placed, List<string> setAside)
    {
        foreach (string relative in placed)
        {
            File.Delete(Path.Combine(install, relative));
        }

        foreach (string relative in setAside)
        {
            File.Move(Path.Combine(retired, relative), Path.Combine(install, relative));
        }
    }
}

using System;
using System.IO;
using Godot;

namespace IdleBar.Online;

public static class DeviceId
{
    public static Guid Read(string path)
    {
        try
        {
            if (File.Exists(path) && Guid.TryParse(File.ReadAllText(path).Trim(), out Guid known))
            {
                return known;
            }

            Guid created = Guid.NewGuid();
            File.WriteAllText(path, created.ToString());
            return created;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Identifiant de ce PC illisible : {exception.Message}");
            return Guid.NewGuid();
        }
    }
}

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Godot;

namespace IdleBar.Cloud;

public sealed class SessionStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("IdleBar.Session");

    private readonly string _path;

    public SessionStore(string path)
    {
        _path = path;
    }

    public AuthSession? Load()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("La session est chiffrée avec DPAPI, disponible uniquement sous Windows.");
        }

        if (!File.Exists(_path))
        {
            return null;
        }

        try
        {
            byte[] json = ProtectedData.Unprotect(File.ReadAllBytes(_path), Entropy, DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize<AuthSession>(json);
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException)
        {
            GD.PushWarning($"Session de synchronisation illisible, reconnexion nécessaire : {exception.Message}");
            File.Delete(_path);
            return null;
        }
    }

    public void Save(AuthSession session)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("La session est chiffrée avec DPAPI, disponible uniquement sous Windows.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(session);
        string temporaryPath = $"{_path}.tmp";
        File.WriteAllBytes(temporaryPath, ProtectedData.Protect(json, Entropy, DataProtectionScope.CurrentUser));
        File.Move(temporaryPath, _path, overwrite: true);
    }

    public void Clear()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }
}
